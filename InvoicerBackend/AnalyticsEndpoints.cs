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

        // --- Bin Card Summary Endpoint ---

        public class BinCardSummaryRequest
        {
            public string[] Tags { get; set; }
            public DateTime? From { get; set; }
            public DateTime? To { get; set; }
            public string PeriodType { get; set; } // "daily", "monthly", "yearly"
        }

        public class BinCardSummaryItem
        {
            public long Itemcode { get; set; }
            public string ItemName { get; set; }
            public string Period { get; set; } // Formatted date string
            public string ActionType { get; set; }
            public double StartQty { get; set; }
            public double EndQty { get; set; }
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
            app.AddAsyncEndpointWithBearerAuth<BinCardSummaryRequest>(
                "GetBinCardSummary",
                async (DataIn, LoginInfo) =>
                {
                    var req = (BinCardSummaryRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        // 1. Resolve ItemCodes from Tags (Reuse logic)
                        List<long> filteredItemCodes = null;
                        if (req.Tags != null && req.Tags.Length > 0)
                        {
                            var directItems = ctx.Inventories.Where(i => i.Tags != null);
                            foreach (var tag in req.Tags) directItems = directItems.Where(i => i.Tags.Contains($"|{tag}|"));
                            var directIds = await directItems.Select(i => i.Itemcode).ToListAsync();

                            var impliedSources = await ctx.ItemTagImplications
                                .Where(imp => req.Tags.Contains(imp.TransitiveTag))
                                .Select(imp => imp.SourceTag).Distinct().ToListAsync();

                            var impliedItems = ctx.Inventories.Where(i => i.Tags != null);
                            foreach (var tag in impliedSources) impliedItems = impliedItems.Where(i => i.Tags.Contains($"|{tag}|"));
                            var impliedIds = await impliedItems.Select(i => i.Itemcode).ToListAsync();

                            filteredItemCodes = directIds.Union(impliedIds).ToList();
                            if (filteredItemCodes.Count == 0) return new List<BinCardSummaryItem>();
                        }

                        // 2. Build Query
                        var query = ctx.InventoryMovements.AsNoTracking();

                        if (filteredItemCodes != null) query = query.Where(m => filteredItemCodes.Contains(m.Itemcode));
                        if (req.From.HasValue) query = query.Where(m => m.EnteredTime >= req.From.Value.ToUniversalTime());
                        if (req.To.HasValue) query = query.Where(m => m.EnteredTime <= req.To.Value.ToUniversalTime());

                        // 3. GroupBy and Select based on PeriodType
                        // We use a composite key for grouping
                        if (req.PeriodType == "daily")
                        {
                            var grouped = await query
                                .GroupBy(m => new { m.Itemcode, m.ActionType, Year = m.EnteredTime.Year, Month = m.EnteredTime.Month, Day = m.EnteredTime.Day })
                                .Select(g => new BinCardSummaryItem
                                {
                                    Itemcode = g.Key.Itemcode,
                                    ActionType = g.Key.ActionType,
                                    Period = g.Key.Year + "-" + g.Key.Month.ToString("00") + "-" + g.Key.Day.ToString("00"),
                                    StartQty = g.Min(x => x.FromUnits),
                                    EndQty = g.Max(x => x.ToUnits)
                                })
                                .ToListAsync();

                            return await EnrichWithItemName(ctx, grouped);
                        }
                        else if (req.PeriodType == "yearly")
                        {
                            var grouped = await query
                                .GroupBy(m => new { m.Itemcode, m.ActionType, Year = m.EnteredTime.Year })
                                .Select(g => new BinCardSummaryItem
                                {
                                    Itemcode = g.Key.Itemcode,
                                    ActionType = g.Key.ActionType,
                                    Period = g.Key.Year.ToString(),
                                    StartQty = g.Min(x => x.FromUnits),
                                    EndQty = g.Max(x => x.ToUnits)
                                })
                                .ToListAsync();

                            return await EnrichWithItemName(ctx, grouped);
                        }
                        else // Default: Monthly
                        {
                            var grouped = await query
                                .GroupBy(m => new { m.Itemcode, m.ActionType, Year = m.EnteredTime.Year, Month = m.EnteredTime.Month })
                                .Select(g => new BinCardSummaryItem
                                {
                                    Itemcode = g.Key.Itemcode,
                                    ActionType = g.Key.ActionType,
                                    Period = g.Key.Year + "-" + g.Key.Month.ToString("00"),
                                    StartQty = g.Min(x => x.FromUnits),
                                    EndQty = g.Max(x => x.ToUnits)
                                })
                                .ToListAsync();

                            return await EnrichWithItemName(ctx, grouped);
                        }
                    }
                },
                "Refresh"
            );


            return app;
        }
        // Helper to attach Item Names
        private static async Task<List<BinCardSummaryItem>> EnrichWithItemName(NewinvContext ctx, List<BinCardSummaryItem> items)
        {
            var ids = items.Select(i => i.Itemcode).Distinct().ToList();
            var names = await ctx.Catalogues.Where(c => ids.Contains(c.Itemcode))
                .ToDictionaryAsync(c => c.Itemcode, c => c.Description);

            foreach (var item in items)
            {
                names.TryGetValue(item.Itemcode, out var desc);
                item.ItemName = desc ?? "Unknown Item";
            }

            // Sort for display
            return items.OrderBy(i => i.ItemName).ThenBy(i => i.Period).ThenBy(i => i.ActionType).ToList();
        }
    }
}
