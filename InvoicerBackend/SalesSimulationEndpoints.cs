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
            // 1. Get Pricing Context (Flags, Suggestions)
            app.AddAsyncEndpointWithBearerAuth<long, PricingContextResponse>(
                "GetPricingContext",
                async (ItemCodeI, LoginInfo) =>
                {
                    var ItemCode = (long)ItemCodeI;
                    using var ctx = new NewinvContext();

                    var item = await ctx.Catalogues.FirstOrDefaultAsync(c => c.Itemcode == ItemCode);
                    if (item == null) throw new ArgumentException("Item not found");

                    var inv = await ctx.Inventories.Where(i => i.Itemcode == ItemCode && i.Units > 0)
                        .OrderBy(i => i.ExpDate ?? DateTime.MaxValue).FirstOrDefaultAsync();

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

            // 2. Simulate Sale (Batch Selection & Calculation)
            app.AddAsyncEndpointWithBearerAuth<SimulateSaleRequest, SimulateSaleResponse>(
                "SimulateSale",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SimulateSaleRequest)ReqI;
                    using var ctx = new NewinvContext();

                    // A. Get PII Info
                    var pii = await ctx.Piis.FirstOrDefaultAsync(p => p.Id == Req.PiiId);
                    if (pii == null) throw new ArgumentException("PII not found");

                    // B. Get Valid Batches (Window View)
                    var batches = await ctx.VBatchSelectionWindows
                        .FromSqlRaw(@"SELECT * FROM public.v_batch_selection_window WHERE itemcode = {0}", Req.ItemCode)
                        .ToListAsync();

                    // C. Get Discount Matrix (Filtered!)
                    // RULE: 
                    // 1. If TargetPrice is set and NOT manual -> It's a Suggestion. Filter by i_suggested_price.
                    // 2. Else -> It's Standard. Filter by i_suggested_price IS NULL.

                    IQueryable<VComprehensiveSalesFinalMatrix> matrixQuery = ctx.VComprehensiveSalesFinalMatrices
                        .Where(m => m.Itemcode == Req.ItemCode && m.PiiId == Req.PiiId);

                    bool isSuggestedPrice = Req.TargetPrice.HasValue && !Req.IsManualPrice;

                    if (isSuggestedPrice)
                    {
                        // Select the specific suggested price row
                        matrixQuery = matrixQuery.Where(m => m.ISuggestedPrice == Req.TargetPrice.Value);
                    }
                    else
                    {
                        // Select the standard row (Suggested Price is usually null or 0 for standard entries based on view def)
                        // Checking for "Source: STANDARD" in ExplanationFinal is safer if NULL logic varies
                        // But assuming i_suggested_price is the discriminator:
                        matrixQuery = matrixQuery.Where(m => m.ISuggestedPrice == null || m.ISuggestedPrice == 0);
                    }

                    // Execute
                    var matrixData = await matrixQuery.ToDictionaryAsync(m => m.Batchcode ?? 0);

                    // Log to Console
                    Console.WriteLine($"[SimulateSale] Item: {Req.ItemCode}, PII: {Req.PiiId}, Qty: {Req.Quantity}");
                    Console.WriteLine($"[SimulateSale] Mode: {(isSuggestedPrice ? "SUGGESTED" : (Req.IsManualPrice ? "MANUAL" : "STANDARD"))}");
                    Console.WriteLine($"[SimulateSale] Found {batches.Count} batches, {matrixData.Count} matrix entries.");

                    double remainingQty = Req.Quantity;
                    double subtotal = 0;
                    double totalDiscount = 0;
                    double totalLp = 0;

                    var selectedBatches = new List<SelectedBatchInfo>();
                    var logs = new List<string>();

                    foreach (var batch in batches)
                    {
                        if (remainingQty <= 0) break;

                        if (!matrixData.TryGetValue((long)batch.Batchcode, out var priceInfo))
                        {
                            logs.Add($"Batch {batch.Batchcode}: Skipped (No pricing data in matrix for this mode).");
                            continue;
                        }

                        double unitPrice;
                        double unitDiscount = 0;
                        double lpRate;

                        if (Req.IsManualPrice && Req.TargetPrice.HasValue)
                        {
                            // MANUAL OVERRIDE
                            unitPrice = Req.TargetPrice.Value;
                            unitDiscount = (priceInfo.ISellingPrice ?? 0) - unitPrice; // Approximate discount
                            lpRate = priceInfo.OEffectiveLpRate ?? (pii.LoyaltyPointsRateAdditivePercentage + pii.LoyaltyPointsRateMultiplicativePercentage);

                            // Validation: Check Min Price
                            if (unitPrice < (priceInfo.OAdjustedMinPrice ?? 0))
                            {
                                logs.Add($"Batch {batch.Batchcode}: WARN - Manual price {unitPrice} < Adjusted Min Price {priceInfo.OAdjustedMinPrice}");
                            }
                        }
                        else
                        {
                            // STANDARD OR SUGGESTED (Matrix Calculated)
                            unitPrice = priceInfo.OEffectiveSellingPricePerUnit ?? 0;
                            unitDiscount = priceInfo.OEffectiveDiscountPerUnit ?? 0;
                            lpRate = priceInfo.OEffectiveLpRate ?? 0;
                        }

                        // Quantity Logic
                        double takeQty = Math.Min((double)batch.Units, remainingQty);

                        selectedBatches.Add(new SelectedBatchInfo
                        {
                            Batchcode = batch.Batchcode ?? 0,
                            Quantity = takeQty,
                            UnitPrice = unitPrice,
                            UnitDiscount = unitDiscount,
                            Cumulative = batch.CumulativeQuantity ?? 0,
                            PrevCumulative = batch.PrevCumulativeQuantity ?? 0
                        });

                        subtotal += takeQty * unitPrice;
                        totalDiscount += takeQty * unitDiscount;
                        totalLp += (takeQty * unitPrice) * (lpRate / 100.0);

                        logs.Add($"Batch {batch.Batchcode}: Took {takeQty} @ {unitPrice:F2}. Rem: {remainingQty - takeQty}");

                        remainingQty -= takeQty;
                    }

                    if (remainingQty > 0)
                    {
                        logs.Add($"ERROR: Insufficient stock. Short by {remainingQty}");
                    }

                    return new SimulateSaleResponse
                    {
                        Success = remainingQty <= 0,
                        Message = remainingQty > 0 ? "Insufficient stock" : "OK",
                        SelectedBatches = selectedBatches,
                        SubTotal = subtotal + totalDiscount,
                        TotalDiscount = totalDiscount,
                        GrandTotal = subtotal,
                        LoyaltyPointsGained = totalLp,
                        CurrentLoyaltyPoints = LoyaltyPointsManager.GetTotalValidPoints(ctx, Req.PiiId),
                        Logs = logs
                    };
                },
                "Refresh"
            );


            return app;
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
        public List<double> SuggestedPrices { get; set; }
    }

    public class SimulateSaleRequest
    {
        public long ItemCode { get; set; }
        public long PiiId { get; set; }
        public double Quantity { get; set; }
        public double? TargetPrice { get; set; } // Used for Manual or Selected Suggestion
        public bool IsManualPrice { get; set; }
    }

    public class SimulateSaleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<SelectedBatchInfo> SelectedBatches { get; set; }
        public double SubTotal { get; set; }
        public double TotalDiscount { get; set; }
        public double GrandTotal { get; set; }
        public double LoyaltyPointsGained { get; set; }
        public double CurrentLoyaltyPoints { get; set; }
        public List<string> Logs { get; set; } // Added
    }

    public class SelectedBatchInfo
    {
        public long Batchcode { get; set; }
        public double Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double UnitDiscount { get; set; }
        public double Cumulative { get; set; }
        public double PrevCumulative { get; set; }
    }
}