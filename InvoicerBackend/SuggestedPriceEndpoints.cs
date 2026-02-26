using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    // 2. Set Suggested Prices (Handles Add/Remove/Edit with Audit)
    public class SetSuggestedPricesRequest
    {
        public long ItemCode { get; set; }
        public List<double> Prices { get; set; }
    }
    public static class SuggestedPriceEndpoints
    {
        public static WebApplication AddSuggestedPriceEndpoints(this WebApplication app)
        {
            // 1. Get Suggested Prices
            app.AddAsyncEndpointWithBearerAuth<long, List<SuggestedPrice>>(
                "GetSuggestedPrices",
                async (ItemCodeIn, LoginInfo) =>
                {
                    long itemCode = (long)ItemCodeIn;
                    using (var ctx = new NewinvContext())
                    {
                        return await ctx.SuggestedPrices
                            .Where(p => p.Itemcode == itemCode)
                            .OrderBy(p => p.Price)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );
            app.AddAsyncEndpointWithBearerAuth<SetSuggestedPricesRequest, bool>(
                "SetSuggestedPrices",
                async (DataIn, LoginInfo) =>
                {
                    var req = (SetSuggestedPricesRequest)DataIn;
                    var currentRequestId = LoginInfo.RequestId;

                    using (var ctx = new NewinvContext())
                    {
                        // 1. Fetch existing prices
                        var existing = await ctx.SuggestedPrices
                            .Where(p => p.Itemcode == req.ItemCode)
                            .ToListAsync();

                        var existingPrices = existing.Select(p => p.Price).ToHashSet();
                        var newPrices = req.Prices != null ? new HashSet<double>(req.Prices) : new HashSet<double>();

                        // 2. Delete removed prices
                        var toDelete = existing.Where(p => !newPrices.Contains(p.Price)).ToList();
                        ctx.SuggestedPrices.RemoveRange(toDelete);

                        // 3. Add new prices
                        var toAdd = newPrices.Where(p => !existingPrices.Contains(p)).ToList();
                        foreach (var price in toAdd)
                        {
                            ctx.SuggestedPrices.Add(new SuggestedPrice
                            {
                                Itemcode = req.ItemCode,
                                Price = price,
                                CreatedBy = (long)LoginInfo.UserId,
                                RequestId = currentRequestId,
                                AllRequestIds = " " + currentRequestId // Start with space
                            });
                        }

                        // 4. Update existing prices (Append Audit Trail)
                        // The requirement: "whenever we have a request, add a space and then request id"
                        // This applies to prices that remain in the list.
                        var toUpdate = existing.Where(p => newPrices.Contains(p.Price)).ToList();
                        foreach (var priceEntity in toUpdate)
                        {
                            priceEntity.RequestId = currentRequestId;
                            priceEntity.AllRequestIds += " " + currentRequestId;
                        }

                        await ctx.SaveChangesAsync();
                        return true;
                    }
                },
                "Refresh"
            );
            return app;
        }
    }
}
