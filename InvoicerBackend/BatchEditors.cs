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
    
    public class BatchSearchRequest
    {
        public long ItemCode { get; set; }
    }
    public class BatchGetRequest
    {
        public long ItemCode { get; set; }
        public long BatchCode { get; set; }
    }

    // DTO for Filter
    public class AdjustmentFilterRequest
    {
        public bool? Posted { get; set; } // null = All, true = Posted, false = Unposted
    }

    // DTO for Split
    public class SplitBatchRequest
    {
        public long ItemCode { get; set; }
        public long BatchCode { get; set; }
        public double NewQuantityForOriginal { get; set; } // The amount to LEAVE in the original batch
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

            app.AddAsyncEndpointWithBearerAuth<AdjustmentFilterRequest>(
                "GetInventoryAdjustments",
                async (DataIn, LoginInfo) =>
                {
                    var req = (AdjustmentFilterRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var query = ctx.InventoryAdjustments.AsQueryable();

                        if (req.Posted.HasValue)
                        {
                            query = query.Where(a => a.Posted == req.Posted.Value);
                        }

                        return await query
                            .OrderByDescending(a => a.CreatedAt)
                            .Take(100)
                            .ToListAsync();
                    }
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<SplitBatchRequest>(
                "SplitBatchWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (SplitBatchRequest)DataIn;

                    using (var ctx = new NewinvContext())
                    {
                        // 1. Get Original Batch
                        var original = await ctx.Inventories
                            .FirstOrDefaultAsync(i => i.Itemcode == req.ItemCode && i.Batchcode == req.BatchCode);

                        if (original == null) throw new ArgumentException("Batch not found.");
                        if (req.NewQuantityForOriginal >= original.Units) throw new ArgumentException("New quantity must be less than current total.");

                        double remainingQty = original.Units - req.NewQuantityForOriginal;

                        // 2. Create New Batch (Copy of original)
                        // Generate new Batchcode (Max + 1)
                        // Assuming Batchcode is not auto-increment in all setups, we calculate it.
                        long maxBatch = await ctx.Inventories.AnyAsync() ? await ctx.Inventories.MaxAsync(i => i.Batchcode) : 0;

                        var newBatch = new Inventory
                        {
                            Itemcode = original.Itemcode,
                            Batchcode = maxBatch + 1,
                            BatchEnabled = true,
                            MfgDate = original.MfgDate,
                            ExpDate = original.ExpDate,
                            PackedSize = original.PackedSize,
                            Units = remainingQty, // The split-off amount
                            MeasurementUnit = original.MeasurementUnit,
                            MarkedPrice = original.MarkedPrice,
                            SellingPrice = original.SellingPrice,
                            CostPrice = original.CostPrice,
                            VolumeDiscounts = original.VolumeDiscounts,
                            Suppliercode = original.Suppliercode,
                            UserDiscounts = original.UserDiscounts,
                            Remarks = $"Split from Batch {original.Batchcode}",
                            MinPrice = original.MinPrice,
                            MultiplicativeDiscountPercentage = original.MultiplicativeDiscountPercentage,
                            AdditiveDiscountPercentage = original.AdditiveDiscountPercentage,
                            Tags = original.Tags,
                            RefLink = original.RefLink,
                            // RefDocId = null // Start with no image or copy if desired
                        };

                        // 3. Update Original
                        original.Units = req.NewQuantityForOriginal;

                        ctx.Inventories.Add(newBatch);
                        await ctx.SaveChangesAsync();

                        return new { NewBatchCode = newBatch.Batchcode, NewUnits = newBatch.Units };
                    }
                },
                "Refresh"
            );

            return app;
        }
    }
}