using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class AnalyticsEndpoints
    {
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
            return app;
        }
    }
}
