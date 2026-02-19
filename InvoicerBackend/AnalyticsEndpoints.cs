using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class AnalyticsEndpoints
    {
        // 2. Search Bin Card (Inventory Movements)
        public class BinCardSearchRequest
        {
            public string[] Tags { get; set; }
            public long? ItemCode { get; set; }
            public long? BatchCode { get; set; }
        }
        public static WebApplication AddAnalyticsEndpoints(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<GetInventoryMovementsRequest>(
                "GetInventoryMovements",
                async (reqr, loginInfo) =>
                {
                    var req = (GetInventoryMovementsRequest)reqr;
                    using var ctx = new NewinvContext();

                    // Start with all movements for this item
                    var query = ctx
                        .InventoryMovements.Where(e => e.Itemcode == req.ItemCode)
                        .AsQueryable();

                    // Apply optional date filters
                    if (req.StartDate.HasValue)
                        query = query.Where(e =>
                            e.EnteredTime >= req.StartDate.Value.ToUniversalTime()
                        );

                    if (req.EndDate.HasValue)
                        query = query.Where(e =>
                            e.EnteredTime <= req.EndDate.Value.ToUniversalTime()
                        );

                    // Execute and return
                    var movements = await query.OrderBy(e => e.EnteredTime).ToListAsync();

                    return movements;
                },
                "Refresh"
            );
            app.AddAsyncEndpointWithBearerAuth<BinCardSearchRequest>(
                "SearchBinCardWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (BinCardSearchRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        // Step 1: Resolve ItemCodes based on Tags and Implications
                        List<long> filteredItemCodes = null;

                        if (req.Tags != null && req.Tags.Length > 0)
                        {
                            // A. Find items directly tagged
                            var directItems = ctx.Inventories.Where(i => i.Tags != null);
                            foreach (var tag in req.Tags)
                            {
                                // Pipe-separated search: |tag|
                                directItems = directItems.Where(i => i.Tags.Contains($"|{tag}|"));
                            }
                            var directIds = await directItems.Select(i => i.Itemcode).ToListAsync();

                            // B. Find items via Implications (If we search for Tag T, and T is implied by Source S, items with S should show up)
                            // Logic: Find SourceTags where TransitiveTag is in our search list
                            var impliedSources = await ctx.ItemTagImplications
                                .Where(imp => req.Tags.Contains(imp.TransitiveTag))
                                .Select(imp => imp.SourceTag)
                                .Distinct()
                                .ToListAsync();

                            var impliedItems = ctx.Inventories.Where(i => i.Tags != null);
                            foreach (var tag in impliedSources)
                            {
                                impliedItems = impliedItems.Where(i => i.Tags.Contains($"|{tag}|"));
                            }
                            var impliedIds = await impliedItems.Select(i => i.Itemcode).ToListAsync();

                            // Combine
                            filteredItemCodes = directIds.Union(impliedIds).ToList();

                            if (filteredItemCodes.Count == 0) return new List<InventoryMovement>(); // Empty result
                        }

                        // Step 2: Query InventoryMovement (Raw SQL because it has no PK)
                        // We cannot use LINQ directly on Keyless entity easily for complex filters without PK.
                        // But since it's a view/table, we can use FromSqlRaw.

                        // However, for simplicity and safety, let's assume we can query it via LINQ if we treat it as a set.
                        // EF Core might complain. Let's use a raw SQL query builder approach or plain LINQ if context allows.
                        // Since InventoryMovement has no PK defined in EF, ctx.InventoryMovements might be problematic to track.
                        // We will use a DTO projection or AsNoTracking.

                        var query = ctx.InventoryMovements.AsNoTracking();

                        if (filteredItemCodes != null)
                        {
                            query = query.Where(m => filteredItemCodes.Contains(m.Itemcode));
                        }

                        if (req.ItemCode.HasValue)
                        {
                            query = query.Where(m => m.Itemcode == req.ItemCode.Value);
                        }

                        if (req.BatchCode.HasValue)
                        {
                            query = query.Where(m => m.Batchcode == req.BatchCode.Value);
                        }

                        return await query.OrderByDescending(m => m.EnteredTime).Take(100).ToListAsync();
                    }
                },
                "Refresh"
            );

            // 3. Get All Tags (for dropdown)
            app.AddAsyncEndpointWithBearerAuth<object>(
                "GetAllTags",
                async (DataIn, LoginInfo) =>
                {
                    using (var ctx = new NewinvContext())
                    {
                        return await ctx.AllTags.OrderBy(t => t.Tag).ToListAsync();
                    }
                },
                "Refresh"
            );


            return app;
        }
    }
}
