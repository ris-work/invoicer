using Microsoft.EntityFrameworkCore;
using MyAOTFriendlyExtensions;
using RV.InvNew.Common;
using System.Text.Json;

namespace InvoicerBackend
{
    public class FlowRequest
    {
        public string FlowId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        
    }
    public static class FlowRequestEndpoints {
        public static WebApplication AddFlowEndpoints(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<FlowRequest>(
            "SetFlowData",
            async (DataIn, LoginInfo) =>
                {
                    var req = (FlowRequest)DataIn;
                    FlowService.Set(req.FlowId, req.Key, req.Value);
                    return true;
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<FlowRequest>(
                "GetFlowData",
                async (DataIn, LoginInfo) =>
                {
                    var req = (FlowRequest)DataIn;
                    var val = FlowService.Get(req.FlowId, req.Key);
                    return new { Value = val };
                },
                "Refresh"
            );
            return app;
        }
    }
    public class BatchSearchRequest
    {
        public long ItemCode { get; set; }
    }
    public class BatchGetRequest
    {
        public long ItemCode { get; set; }
        public long BatchCode { get; set; }
    }
    public static class BatchEditors
    {
        public static WebApplication AddBatchEditors(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<BatchSearchRequest>(
            "SearchBatchesWeb",
                 async (DataIn, LoginInfo) =>
                {
                    var req = (BatchSearchRequest)DataIn;
                        using (var ctx = new NewinvContext())
                {
                    return await ctx.Inventories
                    .Where(i => i.Itemcode == req.ItemCode)
                    .OrderByDescending(i => i.Batchcode)
                    .ToListAsync();
                    }
                },
            "Refresh"
        );

            app.AddAsyncEndpointWithBearerAuth<BatchGetRequest>(
            "GetBatchWeb",
            async (DataIn, LoginInfo) =>
            {
                var req = (BatchGetRequest)DataIn;
                using (var ctx = new NewinvContext())
                {
                    return await ctx.Inventories
                        .FirstOrDefaultAsync(i => i.Itemcode == req.ItemCode && i.Batchcode == req.BatchCode);
                }
            },
            "Refresh"
        );

            // 3. Edit Batch (Patch)
            // Filtered keys: Itemcode, Batchcode, Units, CostPrice, MarkedPrice, SellingPrice (if needed), etc.
            // Allowed: MfgDate, ExpDate, Remarks, Tags, RefLink, VolumeDiscounts, UserDiscounts, Multiplicative/Additive Discount.
            app.AddAsyncPatchEndpointWithBearerAuth<string>(
                "EditBatchWeb",
                async (DataIn, LoginInfo) =>
                {
                    var Patch = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>((string)DataIn);
                    long batchCode = Patch["Batchcode"].GetInt64();

                    using (var ctx = new NewinvContext())
                    {
                        var ToBePatched = ctx.Inventories
                            .Where(x => x.Batchcode == batchCode)
                            .First();

                        // Filter out Accounting/Objective fields
                        string[] removalKeys = new string[] {
                            "Itemcode", "Units", "CostPrice", "MarkedPrice",
                            "Suppliercode", "LastCountedAt"
                        };

                        var Patched = ToBePatched.ApplyChangesExceptFilteredFromJson(removalKeys, (string)DataIn);

                        ctx.Entry(ToBePatched).CurrentValues.SetValues(Patched);
                        await ctx.SaveChangesAsync();
                    }
                    return true;
                },
                new string[] { "Itemcode", "Units", "CostPrice", "MarkedPrice", "Suppliercode", "LastCountedAt" },
                "Refresh"
            );

            return app;
        }
    }
}