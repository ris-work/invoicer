using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvoicerBackend
{
    public static class SalesSimulationEndpoints
    {
        public static WebApplication AddSalesSimulationEndpoints(this WebApplication app)
        {
            // 1. Get Pricing Context
            app.AddAsyncEndpointWithBearerAuth<long, PricingContextResponse>(
                "GetPricingContext",
                async (ItemCodeI, LoginInfo) =>
                {
                    var ItemCode = (long)ItemCodeI;
                    using var ctx = new NewinvContext();
                    var item = await ctx.Catalogues.FirstOrDefaultAsync(c => c.Itemcode == ItemCode);
                    if (item == null) throw new ArgumentException("Item not found");

                    var inv = await ctx.Inventories
                        .Where(i => i.Itemcode == ItemCode && i.Units > 0)
                        .OrderBy(i => i.ExpDate ?? DateTime.MaxValue)
                        .FirstOrDefaultAsync();

                    var resp = new PricingContextResponse
                    {
                        PriceManual = item.PriceManual,
                        AllowPriceSuggestions = item.AllowPriceSuggestions,
                        DefaultSellingPrice = inv?.SellingPrice ?? 0,
                        MinPrice = inv?.MinPrice ?? 0,
                        EnforceMinPrice = inv?.EnforceMinPrice ?? true
                    };

                    if (item.AllowPriceSuggestions)
                    {
                        resp.SuggestedPrices = await ctx.SuggestedPrices
                            .Where(s => s.Itemcode == ItemCode)
                            .Select(s => s.Price)
                            .ToListAsync();
                    }
                    return resp;
                },
                "Refresh"
            );

            // 2. Simulate Order
            app.AddAsyncEndpointWithBearerAuth<SimulateOrderRequest, SimulateOrderResponse>(
                "SimulateSaleOrder",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SimulateOrderRequest)ReqI;
                    using var ctx = new NewinvContext();

                    var pii = await ctx.Piis.FirstOrDefaultAsync(p => p.Id == Req.PiiId);
                    if (pii == null) throw new ArgumentException("PII not found");

                    var itemCodes = Req.Items.Select(i => i.ItemCode).Distinct().ToList();

                    // --- FETCH DATA ---

                    // 1. Batches
                    var allBatchesRaw = await ctx.VBatchSelectionWindows
                        .FromSqlRaw(@"SELECT * FROM public.v_batch_selection_window WHERE itemcode = ANY({0})", itemCodes.ToArray())
                        .ToListAsync();

                    Console.WriteLine($"[SimulateSaleOrder] Fetched {allBatchesRaw.Count} batches from View.");

                    // Snapshots
                    var initialInventory = allBatchesRaw.ToDictionary(b => b.Batchcode ?? 0, b => b.Units ?? 0);
                    var virtualInventory = new Dictionary<long, double>(initialInventory);

                    // 2. Matrix
                    var matrixDataRaw = await ctx.VComprehensiveSalesFinalMatrices
                        .Where(m => itemCodes.Contains(m.Itemcode ?? 0) && m.PiiId == Req.PiiId)
                        .ToListAsync();

                    var matrixByItem = matrixDataRaw.GroupBy(m => m.Itemcode ?? 0)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // --- SORTING LOGIC (NEW) ---
                    // Strategy: 
                    // 1. Precise Batch (HasBatchCode) = Priority 1
                    // 2. Sale Order (Auto) = Priority 2
                    // Sub-sort: Effective Price (Ascending) -> "Unlikely/Rare" deals first.

                    var sortedItems = Req.Items.Select(item =>
                    {
                        double estimatedPrice = double.MaxValue;

                        if (item.TargetPrice.HasValue)
                        {
                            estimatedPrice = item.TargetPrice.Value;
                        }
                        else
                        {
                            // Estimate standard price from the first batch of this item
                            var firstBatch = allBatchesRaw.FirstOrDefault(b => b.Itemcode == item.ItemCode);
                            if (firstBatch != null) estimatedPrice = firstBatch.SellingPrice ?? 0;
                        }

                        return new { Item = item, SortPrice = estimatedPrice };
                    })
                    .OrderBy(x => x.Item.BatchCode.HasValue ? 0 : 1) // Precise First
                    .ThenBy(x => x.SortPrice)                        // Low Price First
                    .Select(x => x.Item)
                    .ToList();

                    var itemResults = new List<SimulateItemResult>();

                    // --- PROCESS ITEMS ---
                    foreach (var itemReq in sortedItems)
                    {
                        var itemBatches = allBatchesRaw.Where(b => b.Itemcode == itemReq.ItemCode).ToList();
                        var itemMatrix = matrixByItem.GetValueOrDefault(itemReq.ItemCode, new List<VComprehensiveSalesFinalMatrix>());

                        var result = ProcessItem(itemReq, itemBatches, virtualInventory, initialInventory, itemMatrix);
                        itemResults.Add(result);
                    }

                    return new SimulateOrderResponse
                    {
                        Success = itemResults.All(r => r.Success),
                        Items = itemResults, // Returns in the optimized processing order
                        CurrentLoyaltyPoints = LoyaltyPointsManager.GetTotalValidPoints(ctx, Req.PiiId)
                    };
                },
                "Refresh"
            );

            return app;
        }

        private static SimulateItemResult ProcessItem(
            SaleOrderLineItem req,
            List<VBatchSelectionWindow> itemBatches,
            Dictionary<long, double> virtualInventory,
            Dictionary<long, double> initialInventory,
            List<VComprehensiveSalesFinalMatrix> itemMatrix)
        {
            double remainingQty = req.Quantity;
            var selectedBatches = new List<SelectedBatchInfo>();
            var allBatchDebug = new List<BatchDebugInfo>();

            bool isSuggested = req.TargetPrice.HasValue && !req.IsManualPrice;

            // --- BRANCH 1: PRECISE BATCH SELECTION ---
            if (req.BatchCode.HasValue)
            {
                var batchId = req.BatchCode.Value;
                var batch = itemBatches.FirstOrDefault(b => b.Batchcode == batchId);
                var virtualQty = virtualInventory.ContainsKey(batchId) ? virtualInventory[batchId] : 0;
                var initialQty = initialInventory.ContainsKey(batchId) ? initialInventory[batchId] : 0;
                var priceInfo = itemMatrix.FirstOrDefault(m => m.Batchcode == batchId);

                var dbg = new BatchDebugInfo
                {
                    Batchcode = batchId,
                    InitialQty = initialQty,
                    ViInitialQty = virtualQty,
                    AvailableQty = virtualQty,
                    EffectivePrice = priceInfo?.OEffectiveSellingPricePerUnit,
                    Status = "Skipped"
                };

                if (batch == null) dbg.Status = "Batch Not Found";
                else if (virtualQty < req.Quantity) dbg.Status = "Insufficient Stock";
                else if (priceInfo == null) dbg.Status = "No Price Data";
                else
                {
                    double unitPrice = req.IsManualPrice && req.TargetPrice.HasValue
                        ? req.TargetPrice.Value
                        : (priceInfo.OEffectiveSellingPricePerUnit ?? 0);

                    selectedBatches.Add(new SelectedBatchInfo
                    {
                        Batchcode = batchId,
                        Quantity = req.Quantity,
                        UnitPrice = unitPrice
                    });

                    virtualInventory[batchId] -= req.Quantity;
                    remainingQty = 0;
                    dbg.Status = "Selected";
                    dbg.AvailableQty = virtualInventory[batchId];
                }
                allBatchDebug.Add(dbg);
            }
            // --- BRANCH 2: AUTO ALLOCATION (SALE ORDER) ---
            else
            {
                foreach (var batch in itemBatches)
                {
                    var batchId = batch.Batchcode ?? 0;
                    var virtualQty = virtualInventory.ContainsKey(batchId) ? virtualInventory[batchId] : 0;
                    var initialQty = initialInventory.ContainsKey(batchId) ? initialInventory[batchId] : 0;

                    var priceInfo = itemMatrix.FirstOrDefault(m =>
                        m.Batchcode == batchId &&
                        (
                            (isSuggested && m.ISuggestedPrice == req.TargetPrice) ||
                            (!isSuggested && (m.ISuggestedPrice == null || m.ISuggestedPrice == 0))
                        )
                    );

                    var dbg = new BatchDebugInfo
                    {
                        Batchcode = batchId,
                        InitialQty = initialQty,
                        ViInitialQty = virtualQty,
                        AvailableQty = virtualQty,
                        EffectivePrice = priceInfo?.OEffectiveSellingPricePerUnit,
                        Status = "Skipped"
                    };

                    if (virtualQty <= 0) { dbg.Status = "Empty/Reserved"; }
                    else if (remainingQty <= 0) { dbg.Status = "Demand Satisfied"; }
                    else if (priceInfo == null) { dbg.Status = "Price Mismatch"; }
                    else
                    {
                        double takeQty = Math.Min(virtualQty, remainingQty);

                        double unitPrice;
                        double unitDiscount = 0;
                        double lpRate;

                        if (req.IsManualPrice && req.TargetPrice.HasValue)
                        {
                            unitPrice = req.TargetPrice.Value;
                            unitDiscount = (priceInfo.ISellingPrice ?? 0) - unitPrice;
                            lpRate = priceInfo.OEffectiveLpRate ?? 0;
                        }
                        else
                        {
                            unitPrice = priceInfo.OEffectiveSellingPricePerUnit ?? 0;
                            unitDiscount = priceInfo.OEffectiveDiscountPerUnit ?? 0;
                            lpRate = priceInfo.OEffectiveLpRate ?? 0;
                        }

                        selectedBatches.Add(new SelectedBatchInfo
                        {
                            Batchcode = batchId,
                            Quantity = takeQty,
                            UnitPrice = unitPrice,
                            UnitDiscount = unitDiscount,
                            LpRate = lpRate
                        });

                        virtualInventory[batchId] -= takeQty;
                        remainingQty -= takeQty;

                        dbg.Status = "Selected";
                        dbg.AvailableQty = virtualInventory[batchId];
                    }
                    allBatchDebug.Add(dbg);
                }
            }

            return new SimulateItemResult
            {
                ItemCode = req.ItemCode,
                Success = remainingQty <= 0,
                Message = remainingQty > 0 ? $"Insufficient stock" : "OK",
                SelectedBatches = selectedBatches,
                AllBatches = allBatchDebug
            };
        }
    }

    // DTOs
    public class PricingContextResponse
    {
        public bool PriceManual { get; set; }
        public bool AllowPriceSuggestions { get; set; }
        public double DefaultSellingPrice { get; set; }
        public double MinPrice { get; set; }
        public bool EnforceMinPrice { get; set; }
        public List<double> SuggestedPrices { get; set; } = new List<double>();
    }

    public class SimulateOrderRequest
    {
        public long PiiId { get; set; }
        public List<SaleOrderLineItem> Items { get; set; }
    }

    public class SaleOrderLineItem
    {
        public long ItemCode { get; set; }
        public double Quantity { get; set; }
        public double? TargetPrice { get; set; }
        public bool IsManualPrice { get; set; }
        public long? BatchCode { get; set; }
    }

    public class SimulateOrderResponse
    {
        public bool Success { get; set; }
        public List<SimulateItemResult> Items { get; set; }
        public double CurrentLoyaltyPoints { get; set; }
    }

    public class SimulateItemResult
    {
        public long ItemCode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<SelectedBatchInfo> SelectedBatches { get; set; }
        public List<BatchDebugInfo> AllBatches { get; set; }
    }

    public class SelectedBatchInfo
    {
        public long Batchcode { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double UnitDiscount { get; set; }
        public double LpRate { get; set; }
    }

    public class BatchDebugInfo
    {
        public long Batchcode { get; set; }
        public double InitialQty { get; set; }
        public double ViInitialQty { get; set; }
        public double AvailableQty { get; set; }
        public double? EffectivePrice { get; set; }
        public string Status { get; set; }
    }
}