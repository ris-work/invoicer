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
            // Start with the movements
            var query = ctx.InventoryMovements.AsNoTracking();

            // Filter by Tags using the View (One RTT)
            if (req.Tags != null && req.Tags.Length > 0)
            {
                // Join with ItemTagImplication to find items that have the tag (transitively)
                // This translates to a single SQL query
                query = from m in query
                        join imp in ctx.ItemTagImplications
                        on m.Itemcode equals imp.Itemcode
                        where req.Tags.Contains(imp.TransitiveTag)
                        select m;

                // Use Distinct because an item might match multiple tags in the search list
                // or have multiple implication paths, duplicating the movement row
                query = query.Distinct();
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
                        return await ctx.AllTags.Distinct().OrderBy(t => t.Tag).ToListAsync();
                    }
                },
                "Refresh"
            );


            return app;
        }
    }
}
