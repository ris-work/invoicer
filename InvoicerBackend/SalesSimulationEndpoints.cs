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

            // 2. Simulate Order (Re-using Shared Logic)
            app.AddAsyncEndpointWithBearerAuth<SimulateOrderRequest, SimulateOrderResponse>(
                "SimulateSaleOrder",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SimulateOrderRequest)ReqI;
                    ProcessResult processResult;
                    using var ctx = new NewinvContext();
                    using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    try
                    {

                        // Call the Unified Processor with Empty Payments
                        processResult = await InvoiceProcessingService.ProcessInvoice(
                            ctx,
                            Req.PiiId,
                            Req.Items,
                            new List<PaymentEntry>() // Payments are empty for simulation
                        );
                    }
                    catch { await tx.RollbackAsync(); throw; }

                    // Map ProcessResult to SimulateOrderResponse
                    return new SimulateOrderResponse
                    {
                        Success = processResult.Success,
                        Message = processResult.Message,
                        Items = processResult.Items,
                        CurrentLoyaltyPoints = LoyaltyPointsManager.GetTotalValidPoints(ctx, Req.PiiId),
                        TotalTax = processResult.TotalTax,
                        TaxJurisdiction = "HOME", // ProcessResult uses default if not passed
                        GrandTotal = processResult.GrandTotal,
                        TotalPaid = processResult.TotalPaid, // Will be 0
                        Balance = processResult.Balance,
                        PaymentResults = processResult.PaymentResults, // Empty
                        LoyaltyPointsFinal = processResult.LoyaltyPointsFinal
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
                    ProcessResult result;
                    using var ctx = new NewinvContext();
                    using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    // Call Shared Logic
                    try
                    {
                        result = await InvoiceProcessingService.ProcessInvoice(ctx, Req.PiiId, Req.Items, Req.Payments);
                    }
                    catch { throw; }

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

            
            // Job: Expire Loyalty Points
            app.AddAsyncEndpointWithBearerAuth<string, int>(
                "ProcessExpiredLoyaltyPoints",
                async (DataIn, LoginInfo) =>
                {
                    using var ctx = new NewinvContext();
                    using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    try
                    {
                        // 1. Find expired buckets that still have remaining balance
                        // Logic: Bucket is expired (ValidUntil < Now) AND (Amount - Sum(Redemptions)) > 0

                        var now = DateTime.UtcNow;

                        // Fetch candidates (simplified, might need raw SQL for performance on large datasets)
                        var expiredBuckets = await ctx.LoyaltyPoints
                            .Where(lp => lp.ValidUntil < now)
                            .ToListAsync();

                        int processedCount = 0;
                        long accLpLiability = await EnsureAccountExists(ctx, "Loyalty Points Liability", 2, "PROV_CUR");
                        long accBreakage = await EnsureAccountExists(ctx, "Breakage Income", 4, "REV_OTHER");

                        foreach (var bucket in expiredBuckets)
                        {
                            // Calculate remaining physically
                            var redeemed = await ctx.LoyaltyPointsRedemptions
                                .Where(r => r.LoyalityPointsId == bucket.PointsId)
                                .SumAsync(r => r.Amount);

                            double remaining = bucket.Amount - redeemed;

                            if (remaining > 0)
                            {
                                // 1. "Burn" the remaining points physically by creating a final redemption record
                                // This prevents re-processing.
                                var burnRecord = new LoyaltyPointsRedemption
                                {
                                    CustId = bucket.CustId,
                                    InvoiceId = 0, // System
                                    Amount = remaining,
                                    TimeIssued = DateTimeOffset.UtcNow,
                                    LoyalityPointsId = bucket.PointsId,
                                    RedeemedFor = "EXPIRATION"
                                };
                                ctx.LoyaltyPointsRedemptions.Add(burnRecord);

                                // 2. Create Accounting Entry
                                double value = GetLpMonetaryValue(remaining);

                                var journal = new AccountsJournalEntry
                                {
                                    TimeAsEntered = now,
                                    Amount = value,
                                    JournalNo = 8, // Adjusting Journal
                                    DebitAccountNo = accLpLiability,
                                    DebitAccountType = 2,
                                    CreditAccountNo = accBreakage,
                                    CreditAccountType = 4,
                                    Description = $"Expired Loyalty Points - Bucket {bucket.PointsId}",
                                    PrincipalName = "SYSTEM_EXPIRY_JOB"
                                };
                                JournalEntries.AddJournalEntry(ctx, journal);

                                processedCount++;
                            }
                        }

                        await ctx.SaveChangesAsync();
                        await tx.CommitAsync();
                        return processedCount;
                    }
                    catch
                    {
                        await tx.RollbackAsync();
                        throw;
                    }
                },
                "Refresh" // Should probably be a higher privilege like "Admin"
            );

            // Get Default Terminal Accounts for Quick Pay
            app.AddAsyncEndpointWithBearerAuth<TerminalAccountsRequest, TerminalAccountsResponse>(
    "GetTerminalAccounts",
    async (DataIn, LoginInfo) =>
    {
        var req = (TerminalAccountsRequest)DataIn;
        var terminalId = req.TerminalId ?? Environment.MachineName;

        using var ctx = new NewinvContext();
        // MANDATORY: Serializable for auto-create logic
        using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        try
        {
            var terminal = await ctx.Terminals.FirstOrDefaultAsync(t => t.TerminalId == terminalId);

            // 1. Resolve Cash
            long cashNo = terminal?.DefaultCash ?? 0;
            if (cashNo == 0)
            {
                // CREATE if missing
                cashNo = await EnsureAccountExists(ctx, $"Cash - {terminalId}", 1, "CASH");
                // Update terminal object
                if (terminal == null)
                {
                    terminal = new Terminal { TerminalId = terminalId };
                    ctx.Terminals.Add(terminal);
                }
                terminal.DefaultCash = cashNo;
            }

            // 2. Resolve Bank
            long bankNo = terminal?.DefaultBank ?? 0;
            if (bankNo == 0)
            {
                bankNo = await EnsureAccountExists(ctx, $"Bank - {terminalId}", 1, "CASH");
                terminal.DefaultBank = bankNo;
            }

            // 3. Save Terminal changes if we created accounts
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();

            // 4. Fetch Names for UI
            var cashAcc = await ctx.AccountsInformations.FindAsync(cashNo);
            var bankAcc = await ctx.AccountsInformations.FindAsync(bankNo);

            return new TerminalAccountsResponse
            {
                CashAccountNo = cashNo,
                CashAccountName = cashAcc?.AccountName ?? "Cash",
                BankAccountNo = bankNo,
                BankAccountName = bankAcc?.AccountName ?? "Bank"
            };
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

        private static double GetLpMonetaryValue(double points)
        {
            // Example: 100 points = $1.00 => Rate 0.01
            const double rate = 1;
            return points * rate;
        }

        


        

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
                }
            }
            else
            {
                var newAccount = new AccountsInformation
                {
                    AccountName = accountName,
                    AccountType = accountType,
                    AccountMin = -1000000000,
                    AccountMax = 1000000000,
                    HumanFriendlyId = $"{accountName.ToUpper().Replace(" ", "_").Replace("-", "_")}_{accountType}",
                    IfrsCategoryId = ifrsCategory?.Id ?? 1,
                    // Set flags based on type/name
                    IsCash = accountType == 1 && accountName.ToLower().Contains("cash"),
                    IsBank = accountType == 1 && accountName.ToLower().Contains("bank")
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

        private static string InferIfrsCodeFromName(string name, int type)
        {
            string upper = name.ToUpperInvariant();

            // Type 1: Assets
            if (type == 1)
            {
                if (upper.Contains("CASH") || upper.Contains("BANK")) return "CASH";
                if (upper.Contains("RECEIVABLE") || upper.Contains("DEBTOR")) return "RECV_TRADE";
                if (upper.Contains("INVENTORY") || upper.Contains("STOCK")) return "INVENTORY";
                return "UNMAP_ASSET";
            }

            // Type 2: Liabilities
            if (type == 2)
            {
                if (upper.Contains("PAYABLE") || upper.Contains("CREDITOR")) return "PAY_TRADE";
                if (upper.Contains("TAX")) return "TAX_CUR";
                if (upper.Contains("LOYALTY") || upper.Contains("POINTS")) return "PROV_CUR"; // Loyalty usually current provision
                return "UNMAP_LIAB";
            }

            // Type 3: Equity
            if (type == 3)
            {
                if (upper.Contains("CAPITAL") || upper.Contains("SHARE")) return "EQ_CAPITAL";
                return "UNMAP_EQUITY";
            }

            // Type 4: Income
            if (type == 4)
            {
                if (upper.Contains("SALE") || upper.Contains("REVENUE")) return "REV_SALES";
                return "UNMAP_INC";
            }

            // Type 5: Expenses
            if (type == 5)
            {
                if (upper.Contains("COST OF SALES") || upper.Contains("COGS")) return "COGS";
                return "UNMAP_EXP";
            }

            return "NONE";
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
        public string Message { get; set; }

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
        public string? AccountName { get; set; } // ADDED
        public double Amount { get; set; }
        public string Type { get; set; } = "CASH";
        public double? PointsRedeem { get; set; }
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

        // NEW FIELDS for LP and Type Support
        public string Type { get; set; } // "CASH", "BANK", "LP", "ACCOUNT"
        public double PointsRedeemed { get; set; } // Total points redeemed in this transaction
        public List<ProposedLpRedemption> LpBucketsUsed { get; set; } // Detailed breakdown
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
    public class TerminalAccountsRequest
    {
        public string? TerminalId { get; set; }
    }

    public class TerminalAccountsResponse
    {
        public long CashAccountNo { get; set; }
        public string CashAccountName { get; set; }
        public long BankAccountNo { get; set; }
        public string BankAccountName { get; set; }
    }
    public class LoyaltyPointsBalanceResponse
    {
        public double Balance { get; set; }
    }


}