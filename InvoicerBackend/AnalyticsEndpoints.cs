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
        // --- Bin Card Summary Endpoint ---

        public class BinCardSummaryResponse
        {
            public List<BinCardPeriodBoundary> Periods { get; set; }
            public List<BinCardActionDelta> Actions { get; set; }
        }

        public class BinCardPeriodBoundary
        {
            public long Itemcode { get; set; }
            public string ItemName { get; set; }
            public string Period { get; set; }
            public double StartQty { get; set; }
            public double EndQty { get; set; }
        }

        public class BinCardActionDelta
        {
            public long Itemcode { get; set; }
            public string ItemName { get; set; }
            public string Period { get; set; }
            public string ActionType { get; set; }
            public double TotalUnits { get; set; } // Sum of 'Units'
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
                        // 1. Resolve ItemCodes from Tags
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
                            if (filteredItemCodes.Count == 0) return new BinCardSummaryResponse { Periods = new List<BinCardPeriodBoundary>(), Actions = new List<BinCardActionDelta>() };
                        }

                        // 2. Build Base Query with Join to Catalogue for Names
                        var query = from m in ctx.InventoryMovements.AsNoTracking()
                                    join c in ctx.Catalogues on m.Itemcode equals c.Itemcode
                                    select new { Movement = m, Name = c.Description };

                        if (filteredItemCodes != null) query = query.Where(x => filteredItemCodes.Contains(x.Movement.Itemcode));
                        if (req.From.HasValue) query = query.Where(x => x.Movement.EnteredTime >= req.From.Value.ToUniversalTime());
                        if (req.To.HasValue) query = query.Where(x => x.Movement.EnteredTime <= req.To.Value.ToUniversalTime());

                        // 3. Execute Queries based on PeriodType
                        List<BinCardPeriodBoundary> periods = new List<BinCardPeriodBoundary>();
                        List<BinCardActionDelta> actions = new List<BinCardActionDelta>();

                        if (req.PeriodType == "daily")
                        {
                            // Daily Grouping
                            var pGroup = query.GroupBy(x => new { x.Movement.Itemcode, x.Name, Year = x.Movement.EnteredTime.Year, Month = x.Movement.EnteredTime.Month, Day = x.Movement.EnteredTime.Day });

                            periods = await pGroup.Select(g => new BinCardPeriodBoundary
                            {
                                Itemcode = g.Key.Itemcode,
                                ItemName = g.Key.Name,
                                Period = g.Key.Year + "-" + g.Key.Month.ToString("00") + "-" + g.Key.Day.ToString("00"),
                                StartQty = g.OrderBy(x => x.Movement.EnteredTime).FirstOrDefault().Movement.FromUnits,
                                EndQty = g.OrderByDescending(x => x.Movement.EnteredTime).FirstOrDefault().Movement.ToUnits
                            }).ToListAsync();

                            var aGroup = query.GroupBy(x => new { x.Movement.Itemcode, x.Name, x.Movement.ActionType, Year = x.Movement.EnteredTime.Year, Month = x.Movement.EnteredTime.Month, Day = x.Movement.EnteredTime.Day });

                            actions = await aGroup.Select(g => new BinCardActionDelta
                            {
                                Itemcode = g.Key.Itemcode,
                                ItemName = g.Key.Name,
                                ActionType = g.Key.ActionType,
                                Period = g.Key.Year + "-" + g.Key.Month.ToString("00") + "-" + g.Key.Day.ToString("00"),
                                TotalUnits = g.Sum(x => x.Movement.Units)
                            }).ToListAsync();
                        }
                        else if (req.PeriodType == "yearly")
                        {
                            // Yearly Grouping
                            var pGroup = query.GroupBy(x => new { x.Movement.Itemcode, x.Name, Year = x.Movement.EnteredTime.Year });

                            periods = await pGroup.Select(g => new BinCardPeriodBoundary
                            {
                                Itemcode = g.Key.Itemcode,
                                ItemName = g.Key.Name,
                                Period = g.Key.Year.ToString(),
                                StartQty = g.OrderBy(x => x.Movement.EnteredTime).FirstOrDefault().Movement.FromUnits,
                                EndQty = g.OrderByDescending(x => x.Movement.EnteredTime).FirstOrDefault().Movement.ToUnits
                            }).ToListAsync();

                            var aGroup = query.GroupBy(x => new { x.Movement.Itemcode, x.Name, x.Movement.ActionType, Year = x.Movement.EnteredTime.Year });

                            actions = await aGroup.Select(g => new BinCardActionDelta
                            {
                                Itemcode = g.Key.Itemcode,
                                ItemName = g.Key.Name,
                                ActionType = g.Key.ActionType,
                                Period = g.Key.Year.ToString(),
                                TotalUnits = g.Sum(x => x.Movement.Units)
                            }).ToListAsync();
                        }
                        else // Monthly (Default)
                        {
                            // Monthly Grouping
                            var pGroup = query.GroupBy(x => new { x.Movement.Itemcode, x.Name, Year = x.Movement.EnteredTime.Year, Month = x.Movement.EnteredTime.Month });

                            periods = await pGroup.Select(g => new BinCardPeriodBoundary
                            {
                                Itemcode = g.Key.Itemcode,
                                ItemName = g.Key.Name,
                                Period = g.Key.Year + "-" + g.Key.Month.ToString("00"),
                                StartQty = g.OrderBy(x => x.Movement.EnteredTime).FirstOrDefault().Movement.FromUnits,
                                EndQty = g.OrderByDescending(x => x.Movement.EnteredTime).FirstOrDefault().Movement.ToUnits
                            }).ToListAsync();

                            var aGroup = query.GroupBy(x => new { x.Movement.Itemcode, x.Name, x.Movement.ActionType, Year = x.Movement.EnteredTime.Year, Month = x.Movement.EnteredTime.Month });

                            actions = await aGroup.Select(g => new BinCardActionDelta
                            {
                                Itemcode = g.Key.Itemcode,
                                ItemName = g.Key.Name,
                                ActionType = g.Key.ActionType,
                                Period = g.Key.Year + "-" + g.Key.Month.ToString("00"),
                                TotalUnits = g.Sum(x => x.Movement.Units)
                            }).ToListAsync();
                        }

                        return new BinCardSummaryResponse
                        {
                            Periods = periods,
                            Actions = actions
                        };
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
