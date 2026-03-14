using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvoicerBackend
{
    // Result Container
    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<SimulateItemResult> Items { get; set; }
        public List<PaymentResult> PaymentResults { get; set; }
        public List<JournalEntryResult> AccountingEntries { get; set; }
        public double GrandTotal { get; set; }
        public double TotalPaid { get; set; }
        public double Balance { get; set; }
        public double LoyaltyPointsFinal { get; set; }
        public double TotalTax { get; set; }
        public List<ProposedLpRedemption> LpProposedRedemptions { get; set; } = new();
    }

    public class ProposedLpRedemption
    {
        public long BucketId { get; set; }
        public double Amount { get; set; }
    }

    public static class InvoiceProcessingService
    {
        // Main Entry Point: Validation + Calculation
        public static async Task<ProcessResult> ProcessInvoice(
            NewinvContext ctx,
            long piiId,
            List<SaleOrderLineItem> items,
            List<PaymentEntry> payments)
        {
            var result = new ProcessResult { Success = true, Message = "OK" };

            // 1. Load Dependencies
            var pii = await ctx.Piis.FirstOrDefaultAsync(p => p.Id == piiId);
            if (pii == null) throw new ArgumentException("PII not found");

            var itemCodes = items.Select(i => i.ItemCode).Distinct().ToList();

            // 2. Inventory Reservation (Virtual)
            var allBatchesRaw = await ctx.VBatchSelectionWindows
                .FromSqlRaw(@"SELECT * FROM public.v_batch_selection_window WHERE itemcode = ANY({0})", itemCodes.ToArray())
                .ToListAsync();

            var initialInventory = allBatchesRaw.ToDictionary(b => b.Batchcode ?? 0, b => b.Units ?? 0);
            var virtualInventory = new Dictionary<long, double>(initialInventory);

            var matrixDataRaw = await ctx.VComprehensiveSalesFinalMatrices
                .Where(m => itemCodes.Contains(m.Itemcode ?? 0) && m.PiiId == piiId)
                .ToListAsync();
            var matrixByItem = matrixDataRaw.GroupBy(m => m.Itemcode ?? 0).ToDictionary(g => g.Key, g => g.ToList());

            // 3. Tax Resolution
            string jurisdictionCode = "HOME"; // Default to Source

            // 4. Process Items (Batch Selection)
            result.Items = new List<SimulateItemResult>();
            double totalRevenue = 0;
            double totalTax = 0;

            // SORTING LOGIC (Precise first, then low price first)
            var sortedItems = items.Select(item =>
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

            foreach (var itemReq in sortedItems)
            {
                var processResult = ProcessItem(itemReq, allBatchesRaw, virtualInventory, initialInventory, matrixByItem, ctx, jurisdictionCode);
                result.Items.Add(processResult);

                if (!processResult.Success) result.Success = false;

                totalRevenue += processResult.SelectedBatches.Sum(b => b.Quantity * b.UnitPrice);
                totalTax += processResult.SelectedBatches.Sum(b => b.TaxAmount);
            }

            // =====================================================================
            // 4. LP VIRTUAL STATE INITIALIZATION
            // =====================================================================

            // 1. Fetch Real Buckets
            var rawLpBuckets = LoyaltyPointsManager.GetValidNonEmptyPointsBuckets(ctx, piiId).ToList();

            // 2. Create Virtual State (BucketID -> RemainingAmount)
            var virtualLpState = rawLpBuckets.ToDictionary(b => b.Point.PointsId, b => b.RemainingPoints);

            // Container for proposed redemptions (to be returned to UI)
            var proposedRedemptions = new List<ProposedLpRedemption>();



            // 5. Process Payments
            // =====================================================================
            // PROCESS PAYMENTS
            // =====================================================================

            result.PaymentResults = new List<PaymentResult>();
            result.AccountingEntries = new List<JournalEntryResult>();

            double totalPaid = 0;
            double totalSurcharges = 0;
            double totalLpIssued = 0; // For LP Issuance calculation later
            double baseLpRate = pii.LoyaltyPointsRateAdditivePercentage + pii.LoyaltyPointsRateMultiplicativePercentage;

            // Resolve Accounts needed for payments
            long accReceivable = await EnsureAccountExists(ctx, "Accounts Receivable", 1, "RECV_TRADE");
            long accLpLiability = await EnsureAccountExists(ctx, "Loyalty Points Liability", 2, "PROV_CUR");
            long accBankCharges = await EnsureAccountExists(ctx, "Bank Charges", 5, "EXP_ADMIN");

            foreach (var pay in payments)
            {
                // =================================================================
                // BRANCH A: LOYALTY POINTS (REDEMPTION)
                // =================================================================
                if (pay.Type == "LP")
                {
                    double pointsToRedeem = pay.PointsRedeem ?? 0;
                    if (pointsToRedeem <= 0) continue;

                    // 1. Virtual Redemption
                    var (success, actualRedeemedAmount, simulationEntries) = VirtualRedeemLp(rawLpBuckets, virtualLpState, pointsToRedeem);

                    if (!success)
                    {
                        result.Success = false;
                        result.Message = $"Insufficient valid points. Needed: {pointsToRedeem}, Found: {actualRedeemedAmount}";
                        return result;
                    }

                    proposedRedemptions.AddRange(simulationEntries);
                    double redemptionValue = GetLpMonetaryValue(actualRedeemedAmount);

                    // 2. Accounting Entry: Dr Liability, Cr AR (Clearing the debt)
                    result.AccountingEntries.Add(new JournalEntryResult
                    {
                        DebitAccount = accLpLiability,
                        DebitAccountName = "Loyalty Points Liability",
                        CreditAccount = accReceivable,
                        CreditAccountName = "Accounts Receivable",
                        Amount = redemptionValue,
                        Narrative = $"Loyalty Redemption ({actualRedeemedAmount} pts)"
                    });

                    // 3. UI Result
                    result.PaymentResults.Add(new PaymentResult
                    {
                        AccountNo = 0,
                        AccountName = "Loyalty Points",
                        AmountTendered = redemptionValue,
                        PointsRedeemed = actualRedeemedAmount,
                        LpBucketsUsed = simulationEntries,
                        LpEarned = 0
                    });

                    totalPaid += redemptionValue;
                }

                // =================================================================
                // BRANCH B: CASH / BANK
                // =================================================================
                else if (pay.Type == "CASH" || pay.Type == "BANK")
                {
                    var acc = await ctx.AccountsInformations.FirstOrDefaultAsync(a => a.AccountNo == pay.AccountNo);
                    if (acc == null) continue;

                    double implicitCharge = pay.Amount * (acc.AccountsUsageNonTransparentChargePercentage / 100.0);
                    double surcharge = (pay.Amount * (acc.AccountSurchargesMultiplicativePercentage / 100.0)) + acc.AccountSurchargesAdditiveFee;
                    double netDeposit = pay.Amount - implicitCharge;

                    // LP Issuance Calculation (Cash/Bank generates LP)
                    double effectiveLpRate = baseLpRate * (1 + (acc.LoyaltyBaseMultiplicativePointsPercentage / 100.0));
                    double lpEarned = (pay.Amount * effectiveLpRate) / 100.0;
                    totalLpIssued += lpEarned;

                    // 1. Receipt Entry: Dr Bank, Cr AR
                    result.AccountingEntries.Add(new JournalEntryResult
                    {
                        DebitAccount = pay.AccountNo,
                        DebitAccountName = acc.AccountName,
                        CreditAccount = accReceivable,
                        CreditAccountName = "Accounts Receivable",
                        Amount = netDeposit,
                        Narrative = "Payment Received"
                    });

                    // 2. Implicit Charge Entry (Card Fees)
                    if (implicitCharge > 0)
                    {
                        long chargeAccId = acc.AccountsSurchargesTransferredToDuringSalePayment;
                        if (chargeAccId == 0) chargeAccId = accBankCharges;

                        result.AccountingEntries.Add(new JournalEntryResult
                        {
                            DebitAccount = chargeAccId,
                            DebitAccountName = "Bank Charges",
                            CreditAccount = pay.AccountNo,
                            CreditAccountName = acc.AccountName,
                            Amount = implicitCharge,
                            Narrative = "Card Fee Deduction"
                        });
                    }

                    result.PaymentResults.Add(new PaymentResult
                    {
                        AccountNo = pay.AccountNo,
                        AccountName = acc.AccountName,
                        AmountTendered = pay.Amount,
                        Surcharge = surcharge,
                        ImplicitCharge = implicitCharge,
                        NetDeposit = netDeposit,
                        LpEarned = lpEarned
                    });

                    totalPaid += netDeposit;
                    totalSurcharges += surcharge;
                }

                // =================================================================
                // BRANCH C: ACCOUNT (CREDIT SALE / ON ACCOUNT)
                // =================================================================
                else if (pay.Type == "ACCOUNT")
                {
                    // "On Account" means we are NOT paying now.
                    // We verify the customer is allowed credit (PII check stub).
                    // No Accounting Entry is generated here. The AR generated by the sale remains open.

                    // UI Result (Shows $0.00 paid, balance remains)
                    result.PaymentResults.Add(new PaymentResult
                    {
                        AccountNo = piiId, // Link to Customer
                        AccountName = "On Account",
                        AmountTendered = 0,
                        LpEarned = 0
                    });

                    // totalPaid += 0; 
                }

                // =================================================================
                // BRANCH D: VOUCHER / PREPAID (FUTURE IMPLEMENTATION)
                // =================================================================
                else if (pay.Type == "VOUCHER")
                {
                    // TODO: Logic to validate voucher code
                    // Accounting: Dr Deferred Revenue (Voucher Liability), Cr AR
                    // For now, just a placeholder
                    result.PaymentResults.Add(new PaymentResult
                    {
                        AccountNo = 0,
                        AccountName = "Voucher",
                        AmountTendered = pay.Amount,
                        LpEarned = 0
                    });
                    totalPaid += pay.Amount;
                }
            }

            result.GrandTotal = totalRevenue + totalTax + totalSurcharges;
            result.Balance = totalPaid - result.GrandTotal;
            result.TotalPaid = totalPaid;
            result.LoyaltyPointsFinal = totalLpIssued;
            result.TotalTax = totalTax;

            // 6. Generate Accounting Entries (NO HARDCODING)
            result.AccountingEntries = new List<JournalEntryResult>();

            // Get Dynamic Accounts
            //long accReceivable = await EnsureAccountExists(ctx, "Accounts Receivable", 1, "RECV_TRADE");
            long accRevenue = await EnsureAccountExists(ctx, "Sales Revenue", 4, "REV_SALES");
            long accTax = await EnsureAccountExists(ctx, "Tax Payable", 2, "TAX_CUR");

            // 6a. Revenue Entry
            if (totalRevenue > 0)
            {
                result.AccountingEntries.Add(new JournalEntryResult
                {
                    DebitAccount = accReceivable,
                    DebitAccountName = "Accounts Receivable",
                    CreditAccount = accRevenue,
                    CreditAccountName = "Sales Revenue",
                    Amount = totalRevenue,
                    Narrative = "Sales Revenue"
                });
            }

            // 6b. Tax Entry
            if (totalTax > 0)
            {
                result.AccountingEntries.Add(new JournalEntryResult
                {
                    DebitAccount = accReceivable,
                    DebitAccountName = "Accounts Receivable",
                    CreditAccount = accTax,
                    CreditAccountName = "Tax Payable",
                    Amount = totalTax,
                    Narrative = "Tax Liability"
                });
            }

            // 6c. Payments & Surcharges
            foreach (var pr in result.PaymentResults)
            {
                // Receipt Entry
                result.AccountingEntries.Add(new JournalEntryResult
                {
                    DebitAccount = pr.AccountNo,
                    DebitAccountName = pr.AccountName,
                    CreditAccount = accReceivable,
                    CreditAccountName = "Accounts Receivable",
                    Amount = pr.NetDeposit,
                    Narrative = "Payment Received"
                });

                // Implicit Charge Entry (if applicable)
                if (pr.ImplicitCharge > 0)
                {
                    // Use the configured transfer account from the payment method, or fallback to "Bank Charges"
                    long chargeAccId = pr.AccountNo; // Fallback to same account (contra)
                    var accConfig = await ctx.AccountsInformations.FirstOrDefaultAsync(a => a.AccountNo == pr.AccountNo);
                    if (accConfig != null && accConfig.AccountsSurchargesTransferredToDuringSalePayment != 0)
                    {
                        chargeAccId = accConfig.AccountsSurchargesTransferredToDuringSalePayment;
                    }
                    else
                    {
                        chargeAccId = await EnsureAccountExists(ctx, "Bank Charges", 5, "EXP_ADMIN");
                    }

                    result.AccountingEntries.Add(new JournalEntryResult
                    {
                        DebitAccount = chargeAccId,
                        DebitAccountName = "Bank Charges", // Name lookup ideally
                        CreditAccount = pr.AccountNo,
                        CreditAccountName = pr.AccountName,
                        Amount = pr.ImplicitCharge,
                        Narrative = "Card Fee Deduction"
                    });
                }
            }

            return result;
        }

        // --- CORE ITEM PROCESSOR (FULL LOGIC) ---
        public static SimulateItemResult ProcessItem(
            SaleOrderLineItem req,
            List<VBatchSelectionWindow> allBatchesRaw,
            Dictionary<long, double> virtualInventory,
            Dictionary<long, double> initialInventory,
            Dictionary<long, List<VComprehensiveSalesFinalMatrix>> matrixByItem,
            NewinvContext ctx,
            string jurisdictionCode)
        {
            double remainingQty = req.Quantity;
            var selectedBatches = new List<SelectedBatchInfo>();
            var allBatchDebug = new List<BatchDebugInfo>();

            bool isSuggested = req.TargetPrice.HasValue && !req.IsManualPrice;

            // Get batches for this item
            var itemBatches = allBatchesRaw.Where(b => b.Itemcode == req.ItemCode).ToList();
            var itemMatrix = matrixByItem.ContainsKey(req.ItemCode) ? matrixByItem[req.ItemCode] : new List<VComprehensiveSalesFinalMatrix>();

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
                    dbg.Status = "Selected";
                    dbg.AvailableQty = virtualInventory[batchId];
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
                Message = remainingQty > 0 ? "Insufficient stock" : "OK",
                SelectedBatches = selectedBatches,
                AllBatches = allBatchDebug
            };
        }

        // --- HELPERS ---

        private static (double Rate, double Amount, string Source) CalculateTax(NewinvContext ctx, string jurisdiction, long itemCode, double unitPrice, double qty)
        {
            try
            {
                var category = ctx.Catalogues.Where(c => c.Itemcode == itemCode).Select(c => c.DefaultVatCategory).FirstOrDefault();
                var rateInfo = ctx.VTaxResolutions.FirstOrDefault(t => t.JurisdictionCode == jurisdiction && t.VatCategoryId == category);

                if (rateInfo != null)
                {
                    double taxableAmount = unitPrice * qty;
                    double taxAmount = taxableAmount * ((rateInfo.EffectiveRatePercentage ?? 0) / 100.0);
                    return (rateInfo.EffectiveRatePercentage ?? 0, taxAmount, rateInfo.RateSource);
                }
            }
            catch { /* Ignore */ }

            return (0, 0, "ERROR");
        }

        // Helper to ensure account exists AND has a balance record
        private static async Task<long> EnsureAccountExists(
        NewinvContext ctx,
        string accountName,
        int accountType,
        string? ifrsCode = null)
        {
            ctx.EnsureSerializableTransaction();

            // 1. SMART DEFAULTS: Infer IFRS Code if not provided
            if (string.IsNullOrEmpty(ifrsCode))
            {
                ifrsCode = InferIfrsCodeFromName(accountName, accountType);
            }

            // 2. Resolve IFRS Category from DB
            var ifrsCategory = await ctx.IfrsCategories
                .FirstOrDefaultAsync(c => c.Code == ifrsCode);

            // Fallback: If code not found, try to find generic 'UNMAP' for the type
            if (ifrsCategory == null)
            {
                ifrsCategory = await ctx.IfrsCategories
                    .FirstOrDefaultAsync(c => c.ValidAccountType == accountType && c.Code.StartsWith("UNMAP"));
            }

            // 3. Check/Create AccountsInformation
            var account = await ctx.AccountsInformations
                .FirstOrDefaultAsync(a => a.AccountName == accountName && a.AccountType == accountType);

            long accountNo;

            if (account != null)
            {
                accountNo = account.AccountNo;

                // Update IFRS Category if we found a better match
                if (ifrsCategory != null && account.IfrsCategoryId != ifrsCategory.Id)
                {
                    account.IfrsCategoryId = ifrsCategory.Id;
                    // No IsCurrent to update here anymore
                }
            }
            else
            {
                var newAccount = new AccountsInformation
                {
                    AccountName = accountName,
                    AccountType = accountType,

                    // Standard Limits
                    AccountMin = -1000000000,
                    AccountMax = 1000000000,

                    // Human Friendly ID
                    HumanFriendlyId = $"{accountName.ToUpper().Replace(" ", "_").Replace("-", "_")}_{accountType}",

                    // IFRS Mapping (The only column we need now)
                    IfrsCategoryId = ifrsCategory?.Id ?? 1
                };

                ctx.AccountsInformations.Add(newAccount);
                await ctx.SaveChangesAsync();
                accountNo = newAccount.AccountNo;
            }

            // 4. Check/Create AccountsBalance
            var balance = await ctx.AccountsBalances
                .FirstOrDefaultAsync(b => b.AccountType == accountType && b.AccountNo == accountNo);

            if (balance == null)
            {
                var newBalance = new AccountsBalance
                {
                    AccountType = accountType,
                    AccountNo = accountNo,
                    Amount = 0
                };
                ctx.AccountsBalances.Add(newBalance);
                await ctx.SaveChangesAsync();
            }

            return accountNo;
        }

        /// <summary>
        /// Converts Loyalty Points to Monetary Value.
        /// TODO: Move rate to Database/GlobalConfig.
        /// </summary>
        private static double GetLpMonetaryValue(double points)
        {
            // Standard: 100 Points = $1.00
            const double conversionRate = 1;

            return points * conversionRate;
        }

        /// <summary>
        /// Pure function to simulate LP Redemption without DB side-effects.
        /// Uses FEFO (First-Expiry-First-Out) logic based on the sorted bucket list.
        /// </summary>
        /// <param name="sortedBuckets">The list of valid buckets sorted by Expiry (earliest first).</param>
        /// <param name="virtualState">The mutable dictionary tracking currently available points in memory.</param>
        /// <param name="amountNeeded">The total points requested for this payment.</param>
        /// <returns>Tuple: Success, Actual Redeemed Amount, List of Redemption Details</returns>
        private static (bool Success, double TotalRedeemed, List<ProposedLpRedemption> Entries) VirtualRedeemLp(
            List<(LoyaltyPoint Point, double RemainingPoints)> sortedBuckets,
            Dictionary<long, double> virtualState,
            double amountNeeded)
        {
            double remainingNeed = amountNeeded;
            var entries = new List<ProposedLpRedemption>();

            // Iterate through buckets (Assumed sorted by Expiry Date from LoyaltyPointsManager)
            foreach (var bucket in sortedBuckets)
            {
                if (remainingNeed <= 0) break;

                long bucketId = bucket.Point.PointsId;

                // Check current availability in Virtual State (not the original DB amount)
                if (!virtualState.ContainsKey(bucketId)) continue;

                double availableInVirtual = virtualState[bucketId];
                if (availableInVirtual <= 0) continue;

                // Calculate how much to take from this bucket
                double takeAmount = Math.Min(availableInVirtual, remainingNeed);

                // 1. Update Virtual State (Mutates the dictionary for subsequent payments)
                virtualState[bucketId] -= takeAmount;

                // 2. Record the Proposal
                entries.Add(new ProposedLpRedemption
                {
                    BucketId = bucketId,
                    Amount = takeAmount
                });

                remainingNeed -= takeAmount;
            }

            double totalRedeemed = amountNeeded - remainingNeed;
            bool success = remainingNeed <= 0; // Success if we fulfilled the full request

            return (success, totalRedeemed, entries);
        }

        /// <summary>
        /// Heuristic to determine IFRS Code from Account Name.
        /// </summary>
        private static string InferIfrsCodeFromName(string name, int type)
        {
            string upper = name.ToUpperInvariant();

            // Type 1: Assets
            if (type == 1)
            {
                if (upper.Contains("CASH") || upper.Contains("BANK")) return "CASH";
                if (upper.Contains("RECEIVABLE") || upper.Contains("DEBTOR")) return "RECV_TRADE";
                if (upper.Contains("INVENTORY") || upper.Contains("STOCK")) return "INVENTORY";
                if (upper.Contains("EQUIPMENT") || upper.Contains("FURNITURE") || upper.Contains("MACHINERY")) return "PPE_PLANT";
                if (upper.Contains("BUILDING") || upper.Contains("LAND")) return "PPE_LAND";
                if (upper.Contains("INTANGIBLE") || upper.Contains("GOODWILL")) return "INTANG";
                return "UNMAP_ASSET";
            }

            // Type 2: Liabilities
            if (type == 2)
            {
                if (upper.Contains("PAYABLE") || upper.Contains("CREDITOR")) return "PAY_TRADE";
                if (upper.Contains("TAX")) return "TAX_CUR";
                if (upper.Contains("LOAN") || upper.Contains("BORROW")) return "LOAN_NC";
                if (upper.Contains("PROVISION")) return "PROV_NC";
                return "UNMAP_LIAB";
            }

            // Type 3: Equity
            if (type == 3)
            {
                if (upper.Contains("CAPITAL") || upper.Contains("SHARE")) return "EQ_CAPITAL";
                if (upper.Contains("RETAINED")) return "EQ_RETAINED";
                if (upper.Contains("RESERVE")) return "EQ_REVAL";
                return "UNMAP_EQUITY";
            }

            // Type 4: Income
            if (type == 4)
            {
                if (upper.Contains("SALE") || upper.Contains("REVENUE")) return "REV_SALES";
                if (upper.Contains("INTEREST") || upper.Contains("DIVIDEND")) return "FIN_INC";
                return "UNMAP_INC";
            }

            // Type 5: Expenses
            if (type == 5)
            {
                if (upper.Contains("COST OF SALES") || upper.Contains("COST OF GOODS") || upper.Contains("COGS")) return "COGS";
                if (upper.Contains("DEPRECIATION")) return "EXP_DEPR";
                if (upper.Contains("INTEREST") || upper.Contains("FINANCE")) return "EXP_FIN";
                if (upper.Contains("TAX")) return "EXP_TAX";
                if (upper.Contains("RENT") || upper.Contains("SALARY") || upper.Contains("ADMIN")) return "EXP_ADMIN";
                return "UNMAP_EXP";
            }

            return "NONE";
        }
    }
}