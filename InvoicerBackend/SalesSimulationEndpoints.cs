using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/*
 * ==============================================================================
 * SALES SIMULATION & BATCH SELECTION ENGINE
 * ==============================================================================
 * 
 * PURPOSE:
 * Provides endpoints for simulating sales orders and selecting batches based on
 * complex pricing rules and inventory availability. It supports both "Sale Order"
 * (Auto-allocation) and "Precise Batch" (Manual selection) modes.
 *
 * CORE CONCEPT: VIRTUAL INVENTORY
 * -------------------------------
 * To prevent race conditions within a single order containing multiple line items,
 * this engine uses a "Virtual Inventory" approach.
 * 1. The engine fetches the current Real Inventory state from `v_batch_selection_window`.
 * 2. It creates a mutable copy (Virtual Inventory) specific to the current API request.
 * 3. As line items are processed, stock is "reserved" in the Virtual Inventory.
 * 4. Subsequent line items in the same request see the reduced availability.
 * 5. The Virtual Inventory is discarded after the response is sent. It is never cached.
 *
 * PROCESSING ORDER (CRITICAL FOR FAIRNESS):
 * -----------------------------------------
 * Incoming line items are sorted before processing to maximize fulfillment likelihood:
 * 1. PRIORITY: "Precise Batch" requests are processed FIRST. (Hard Constraints)
 * 2. PRIORITY: "Sale Order" requests are processed SECOND. (Soft Constraints)
 * 3. SUB-SORT: Within each group, items are sorted by Price (Ascending).
 *    Rationale: Lower prices (often discounts/manual overrides) are "harder" to fulfill
 *    or represent higher value to the customer, so they get dibs on stock before
 *    standard high-margin sales.
 *
 * PRICING LOGIC:
 * --------------
 * - Manual Price: Uses user input price. Checks MinPrice constraints.
 * - Suggested Price: Matches `ISuggestedPrice` in the matrix.
 * - Standard Price: Matches rows where `ISuggestedPrice` is NULL/0.
 * - Loyalty Points: Calculated based on the Matrix's `OEffectiveLpRate`.
 *
 * ENDPOINTS:
 * ----------
 * 1. GetPricingContext (ItemCode)
 *    - Returns pricing flags (Manual, Suggestions), default prices, and constraints.
 *    - Used by the UI to render the Price Selection screen.
 * 
 * 2. SimulateSaleOrder (PiiId, List<SaleOrderLineItem>)
 *    - The main engine. Accepts a mixed list of orders.
 *    - Returns `SimulateItemResult` for each item:
 *      - Selected Batches (The allocation plan).
 *      - Debug Info (Real vs Virtual Inventory snapshots).
 *
 * VIEWS DEPENDENCY:
 * -----------------
 * - `public.v_batch_selection_window`: Must contain sorted batches (FEFO) with columns:
 *   itemcode, batchcode, units, selling_price, min_price, exp_date, cumulative_quantity.
 * - `public.v_comprehensive_sales_final_matrix`: Must contain pricing/lp data:
 *   itemcode, batchcode, pii_id, i_suggested_price, o_effective_selling_price_per_unit, etc.
 *
 * DTOs:
 * -----
 * - SaleOrderLineItem: The input structure (ItemCode, Quantity, TargetPrice, BatchCode?).
 * - BatchDebugInfo: The detailed snapshot for UI debugging (Initial, VI Before, VI After).
 * 
 */

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

                    var initialInventory = allBatchesRaw.ToDictionary(b => b.Batchcode ?? 0, b => b.Units ?? 0);
                    var virtualInventory = new Dictionary<long, double>(initialInventory);

                    // 2. Matrix
                    var matrixDataRaw = await ctx.VComprehensiveSalesFinalMatrices
                        .Where(m => itemCodes.Contains(m.Itemcode ?? 0) && m.PiiId == Req.PiiId)
                        .ToListAsync();

                    var matrixByItem = matrixDataRaw.GroupBy(m => m.Itemcode ?? 0)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // --- SORTING LOGIC ---
                    var sortedItems = Req.Items.Select(item =>
                    {
                        double estimatedPrice = double.MaxValue;
                        if (item.TargetPrice.HasValue) estimatedPrice = item.TargetPrice.Value;
                        else
                        {
                            var firstBatch = allBatchesRaw.FirstOrDefault(b => b.Itemcode == item.ItemCode);
                            if (firstBatch != null) estimatedPrice = firstBatch.SellingPrice ?? 0;
                        }
                        return new { Item = item, SortPrice = estimatedPrice };
                    })
                    .OrderBy(x => x.Item.BatchCode.HasValue ? 0 : 1)
                    .ThenBy(x => x.SortPrice)
                    .Select(x => x.Item)
                    .ToList();

                    // --- TAX RESOLUTION SETUP ---
                    string jurisdictionCode = "HOME"; // Default to Source
                    // Future Logic: if (!string.IsNullOrEmpty(pii.Country)) jurisdictionCode = pii.Country;

                    var itemResults = new List<SimulateItemResult>();
                    double grandTotalTax = 0;

                    // --- PROCESS ITEMS ---
                    foreach (var itemReq in sortedItems)
                    {
                        var itemBatches = allBatchesRaw.Where(b => b.Itemcode == itemReq.ItemCode).ToList();
                        var itemMatrix = matrixByItem.GetValueOrDefault(itemReq.ItemCode, new List<VComprehensiveSalesFinalMatrix>());

                        var result = ProcessItem(itemReq, itemBatches, virtualInventory, initialInventory, itemMatrix, ctx, jurisdictionCode);

                        grandTotalTax += result.SelectedBatches.Sum(b => b.TaxAmount);
                        itemResults.Add(result);
                    }

                    double totalRevenue = itemResults.Sum(r => r.SelectedBatches.Sum(b => b.Quantity * b.UnitPrice));
                    double grandTotal = totalRevenue + grandTotalTax;

                    return new SimulateOrderResponse
                    {
                        Success = itemResults.All(r => r.Success),
                        Items = itemResults,
                        CurrentLoyaltyPoints = LoyaltyPointsManager.GetTotalValidPoints(ctx, Req.PiiId),
                        TotalTax = grandTotalTax,
                        TaxJurisdiction = jurisdictionCode,

                        // NEW INITIALIZATION
                        GrandTotal = grandTotal,
                        TotalPaid = 0, // Initial state
                        Balance = -grandTotal, // Initial state (Amount Due)
                        PaymentResults = new List<PaymentResult>(), // Empty list
                        LoyaltyPointsFinal = 0
                    };
                },
                "Refresh"
            );

            // 2. Simulate (Use Shared Service)
            app.AddAsyncEndpointWithBearerAuth<SimulatePaymentRequest, SimulatePaymentResponse>(
                "SimulateSaleWithPayments",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SimulatePaymentRequest)ReqI;
                    using var ctx = new NewinvContext();

                    // Call Shared Logic
                    var result = await InvoiceProcessingService.ProcessInvoice(ctx, Req.PiiId, Req.Items, Req.Payments);

                    // Convert to Response
                    return new SimulatePaymentResponse
                    {
                        Success = result.Success,
                        Message = result.Message,
                        Items = result.Items,
                        PaymentResults = result.PaymentResults,
                        AccountingEntries = result.AccountingEntries,
                        GrandTotal = result.GrandTotal,
                        TotalPaid = result.TotalPaid,
                        Balance = result.Balance,
                        LoyaltyPointsFinal = result.LoyaltyPointsFinal
                    };
                },
                "Refresh"
            );

            // 3. Post (Use Shared Service + Commit)
            app.AddAsyncEndpointWithBearerAuth<PostInvoiceRequest, PostInvoiceResponse>(
                "PostInvoice",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (PostInvoiceRequest)ReqI;
                    using var ctx = new NewinvContext();
                    using var tx = await ctx.Database.BeginTransactionAsync();

                    try
                    {
                        // 1. Deserialize
                        var invoiceData = JsonSerializer.Deserialize<SimulatePaymentRequest>(Req.Payload);

                        // 2. VALIDATE (RE-RUN)
                        var result = await InvoiceProcessingService.ProcessInvoice(ctx, invoiceData.PiiId, invoiceData.Items, invoiceData.Payments);

                        if (!result.Success)
                        {
                            return new PostInvoiceResponse { Success = false, Message = result.Message };
                        }

                        // 3. COMMIT
                        // Here you would write the actual DB inserts for Sales, Payments, JournalEntries.
                        // foreach(var entry in result.AccountingEntries) { ... }

                        // 4. Mark Temp Posted
                        if (Req.TempId.HasValue)
                        {
                            var temp = await ctx.TempIssuedInvoices.FindAsync(Req.TempId.Value);
                            if (temp != null) { temp.Posted = true; temp.ModifiedAt = DateTimeOffset.UtcNow; }
                        }

                        await ctx.SaveChangesAsync();
                        await tx.CommitAsync();

                        return new PostInvoiceResponse { Success = true, Message = "Posted" };
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                },
                "Refresh"
            );

            return app;
        }

        /// <summary>
        /// Full Logic for Processing a Single Line Item (Now with Tax)
        /// </summary>
        private static SimulateItemResult ProcessItem(
            SaleOrderLineItem req,
            List<VBatchSelectionWindow> itemBatches,
            Dictionary<long, double> virtualInventory,
            Dictionary<long, double> initialInventory,
            List<VComprehensiveSalesFinalMatrix> itemMatrix,
            NewinvContext ctx, // ADDED Context
            string jurisdictionCode) // ADDED Jurisdiction
        {
            double remainingQty = req.Quantity;
            var selectedBatches = new List<SelectedBatchInfo>();
            var allBatchDebug = new List<BatchDebugInfo>();

            bool isSuggested = req.TargetPrice.HasValue && !req.IsManualPrice;

            // PRECISE BATCH SELECTION
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

                    // --- TAX CALCULATION ---
                    var taxInfo = CalculateTax(ctx, jurisdictionCode, (long)batch.Itemcode, unitPrice, req.Quantity);

                    selectedBatches.Add(new SelectedBatchInfo
                    {
                        Batchcode = batchId,
                        Quantity = req.Quantity,
                        UnitPrice = unitPrice,
                        TaxRate = taxInfo.Rate,
                        TaxAmount = taxInfo.Amount,
                        TaxSource = taxInfo.Source
                    });

                    virtualInventory[batchId] -= req.Quantity;
                    remainingQty = 0;
                    dbg.Status = "Selected"; dbg.AvailableQty = virtualInventory[batchId];
                }
                allBatchDebug.Add(dbg);
            }
            // AUTO ALLOCATION
            else
            {
                foreach (var batch in itemBatches)
                {
                    var batchId = batch.Batchcode ?? 0;
                    var virtualQty = virtualInventory.ContainsKey(batchId) ? virtualInventory[batchId] : 0;
                    var initialQty = initialInventory.ContainsKey(batchId) ? initialInventory[batchId] : 0;

                    var priceInfo = itemMatrix.FirstOrDefault(m =>
                        m.Batchcode == batchId &&
                        ((isSuggested && m.ISuggestedPrice == req.TargetPrice) || (!isSuggested && (m.ISuggestedPrice == null || m.ISuggestedPrice == 0)))
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

                        // --- TAX CALCULATION ---
                        var taxInfo = CalculateTax(ctx, jurisdictionCode, (long)batch.Itemcode, unitPrice, takeQty);

                        selectedBatches.Add(new SelectedBatchInfo
                        {
                            Batchcode = batchId,
                            Quantity = takeQty,
                            UnitPrice = unitPrice,
                            UnitDiscount = unitDiscount,
                            LpRate = lpRate,
                            TaxRate = taxInfo.Rate,
                            TaxAmount = taxInfo.Amount,
                            TaxSource = taxInfo.Source
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


        private static (double Rate, double Amount, string Source) CalculateTax(NewinvContext ctx, string jurisdiction, long itemCode, double unitPrice, double qty)
        {
            try
            {
                // 1. Get Item's Tax Category from Catalogue
                // Note: In a high-perf scenario, fetch Catalogue data once in the main loop.
                // Doing it here for simplicity of patching.
                var category = ctx.Catalogues.Where(c => c.Itemcode == itemCode).Select(c => c.DefaultVatCategory).FirstOrDefault();

                // 2. Query Resolution View
                var rateInfo = ctx.VTaxResolutions
                    .FirstOrDefault(t => t.JurisdictionCode == jurisdiction && t.VatCategoryId == (category));

                if (rateInfo != null)
                {
                    double taxableAmount = unitPrice * qty;
                    double taxAmount = taxableAmount * ((rateInfo.EffectiveRatePercentage??0) / 100.0);
                    return (rateInfo.EffectiveRatePercentage??0, taxAmount, rateInfo.RateSource);
                }
            }
            catch
            {
                // If view doesn't exist or error, fallback to 0
                Console.WriteLine($"WARN: Tax lookup failed for item {itemCode} in {jurisdiction}");
            }

            return (0, 0, "ERROR");
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
        public double TotalTax { get; set; }
        public string TaxJurisdiction { get; set; }

        // NEW: Ensure these are always present
        public double GrandTotal { get; set; }
        public double TotalPaid { get; set; } = 0;
        public double Balance { get; set; } = 0;
        public List<PaymentResult> PaymentResults { get; set; } = new List<PaymentResult>();
        public double LoyaltyPointsFinal { get; set; } = 0;
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
        // TAX FIELDS
        public double TaxRate { get; set; }
        public double TaxAmount { get; set; }
        public string TaxSource { get; set; } // "SOURCE_DEFAULT" or "OVERRIDE"
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
    // Add inside AddSalesSimulationEndpoints

    // 3. Simulate Sale With Payments (Full Cycle)
    public class SimulatePaymentRequest
    {
        public long PiiId { get; set; }
        public List<SaleOrderLineItem> Items { get; set; }
        public List<PaymentEntry> Payments { get; set; }
    }

    public class PaymentEntry
    {
        public long AccountNo { get; set; }
        public double Amount { get; set; } // Amount the customer pays
    }

    public class SimulatePaymentResponse : SimulateOrderResponse
    {
        public double GrandTotal { get; set; }
        public double TotalPaid { get; set; }
        public double Balance { get; set; } // +/- 
        public List<PaymentResult> PaymentResults { get; set; }
        public List<JournalEntryResult> AccountingEntries { get; set; }
        public double LoyaltyPointsFinal { get; set; }
    }

    public class PaymentResult
    {
        public long AccountNo { get; set; }
        public string AccountName { get; set; }
        public double AmountTendered { get; set; }
        public double Surcharge { get; set; }
        public double ImplicitCharge { get; set; }
        public double NetDeposit { get; set; }
        public double LpEarned { get; set; }
    }

    public class JournalEntryResult
    {
        public long DebitAccount { get; set; }
        public string DebitAccountName { get; set; }
        public long CreditAccount { get; set; }
        public string CreditAccountName { get; set; }
        public double Amount { get; set; }
        public string Narrative { get; set; }
    }

}