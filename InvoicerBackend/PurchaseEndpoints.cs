using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoicerBackend
{
    public static class PurchaseEndpoints
    {
        public static WebApplication AddPurchaseEndpoints(this WebApplication app)
        {
            // 1. Simulate (Validation & Calculation)
            app.AddAsyncEndpointWithBearerAuth<SimulatePurchaseRequest, PurchaseProcessResult>(
                "SimulatePurchase",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SimulatePurchaseRequest)ReqI;
                    using var ctx = new NewinvContext();
                    // No DB transaction needed for pure simulation, but context needed for Lookups
                    return PurchaseProcessingService.ProcessPurchase(ctx, Req.Header, Req.Items, Req.Expenses, Req.Payments);
                },
                "Refresh"
            );

            // 2. Save Draft
            app.AddAsyncEndpointWithBearerAuth<SavePurchaseDraftRequest, long>(
                "SavePurchaseDraft",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SavePurchaseDraftRequest)ReqI;
                    using var ctx = new NewinvContext();

                    var entity = new TempReceivedInvoice
                    {
                        InvoiceContents = Req.Payload,
                        Posted = false,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UserId = (long)LoginInfo.UserId,
                        RequestId = LoginInfo.RequestId,
                        RequestIds = " " + LoginInfo.RequestId
                    };

                    ctx.TempReceivedInvoices.Add(entity);
                    await ctx.SaveChangesAsync();
                    return entity.TempInvoiceRunNo;
                },
                "Refresh"
            );

            // 3. Load Draft
            app.AddAsyncEndpointWithBearerAuth<long, RestoreDraftResponse>(
                "LoadPurchaseDraft",
                async (TempIdI, LoginInfo) =>
                {
                    var TempId = (long)TempIdI;
                    using var ctx = new NewinvContext();
                    var entity = await ctx.TempReceivedInvoices
                        .FirstOrDefaultAsync(t => t.TempInvoiceRunNo == TempId && t.UserId == (long)LoginInfo.UserId);

                    if (entity == null) throw new ArgumentException("Draft not found.");
                    return new RestoreDraftResponse { TempId = entity.TempInvoiceRunNo, Payload = entity.InvoiceContents };
                },
                "Refresh"
            );

            // 4. Post Purchase
            app.AddAsyncEndpointWithBearerAuth<PostPurchaseRequest, PostPurchaseResponse>(
                "PostPurchase",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (PostPurchaseRequest)ReqI;
                    using var ctx = new NewinvContext();
                    using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                    //long accPayable = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "Accounts Payable", 2, "PAY_TRADE");

                    try
                    {
                        var JSOptions = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            PropertyNameCaseInsensitive = true
                        };

                        // Add this line to your existing options setup
                        JSOptions.Converters.Add(new FlexibleDateTimeOffsetConverter());
                        var data = JsonSerializer.Deserialize<SimulatePurchaseRequest>(Req.Payload, JSOptions);
                        var result = PurchaseProcessingService.ProcessPurchase(ctx, data.Header, data.Items, data.Expenses, data.Payments);

                        if (!result.Success) return new PostPurchaseResponse { Success = false, Message = result.Message };

                        var invoice = data.Header;
                        invoice.IsPosted = true;
                        invoice.InvoiceTime = DateTime.SpecifyKind(invoice.InvoiceTime, DateTimeKind.Utc);
                        invoice.LastSavedAt = DateTime.SpecifyKind(invoice.LastSavedAt, DateTimeKind.Utc);
                        invoice.PostedAt = DateTime.UtcNow;
                        invoice.CreatedAt = DateTime.UtcNow;
                        invoice.TransportCharges = result.TotalExpenses; // NEW: Save expenses to header
                        invoice.TotalAmountDue = result.Header.TotalAmountDue;

                        ctx.ReceivedInvoices.Add(invoice);
                        await ctx.SaveChangesAsync(); // Get ID

                        var accountingEntries = new List<JournalEntryResult>();
                        long accInventory = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "Inventory Asset", 1, "INVENTORY");
                        long accPayable = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "Accounts Payable", 2, "PAY_TRADE");
                        long accVatInput = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "VAT Input", 1, "RECV_OTHER");

                        // Process Items
                        foreach (var item in data.Items)
                        {
                            // 1. Create Batch
                            var newBatch = new Inventory
                            {
                                Itemcode = item.Itemcode,
                                Batchcode = 0, // Auto-gen
                                Units = item.TotalUnits,
                                CostPrice = item.NetCostPerUnit, // Important: Net cost
                                SellingPrice = item.SellingPrice,
                                MarkedPrice = item.SellingPrice,
                                Suppliercode = invoice.SupplierId,
                                ExpDate = item.ExpiryDate?.UtcDateTime,
                                MfgDate = item.ManufacturingDate?.UtcDateTime,
                                LastCountedAt = DateTime.UtcNow,
                                BatchEnabled = true,
                                MeasurementUnit = "EA", // Default or fetch from catalogue
                                PackedSize = item.PackSize
                            };
                            ctx.Inventories.Add(newBatch);
                            await ctx.SaveChangesAsync(); // Generate BatchCode

                            // 2. Create Purchase Record
                            var purchase = item;
                            purchase.ReceivedInvoiceId = invoice.ReceivedInvoiceNo;
                            ctx.Purchases.Add(purchase);

                            // 3. Accounting Entries
                            // Dr Inventory (Net Cost)
                            accountingEntries.Add(new JournalEntryResult
                            {
                                DebitAccount = accInventory,
                                DebitAccountName = "Inventory Asset",
                                CreditAccount = accPayable,
                                CreditAccountName = "Accounts Payable",
                                Amount = item.NetTotalCost,
                                Narrative = $"Purchase Inv {invoice.ReceivedInvoiceNo} - Item {item.Itemcode}"
                            });

                            // Dr VAT Input (If reclaimable)
                            if (!item.IsVatADisallowedInputTax && item.VatAbsolute > 0)
                            {
                                accountingEntries.Add(new JournalEntryResult
                                {
                                    DebitAccount = accVatInput,
                                    DebitAccountName = "VAT Input",
                                    CreditAccount = accPayable,
                                    CreditAccountName = "Accounts Payable",
                                    Amount = item.VatAbsolute,
                                    Narrative = $"VAT on Purchase Inv {invoice.ReceivedInvoiceNo}"
                                });
                            }

                            // 4. Inventory Movement (Bin Card)
                            await ctx.Database.ExecuteSqlAsync($@"
                                INSERT INTO inventory_movements 
                                (itemcode, batchcode, from_units, to_units, units, entered_time, last_counted_at, 
                                 reference, remarks, is_one_off, cost_price, selling_price, marked_price, suppliercode, 
                                 volume_discounts, user_discounts, measurement_unit, packed_size, mfg_date, exp_date, batch_enabled) 
                                VALUES 
                                ({newBatch.Itemcode}, {newBatch.Batchcode}, 0, {newBatch.Units}, {newBatch.Units}, {DateTime.UtcNow}, 
                                 {DateTime.UtcNow}, {"purchase:" + invoice.ReceivedInvoiceNo.ToString()}, {"Purchase"}, false, 
                                 {newBatch.CostPrice}, {newBatch.SellingPrice}, {newBatch.MarkedPrice}, {newBatch.Suppliercode}, 
                                 false, false, {newBatch.MeasurementUnit}, {newBatch.PackedSize}, {newBatch.MfgDate}, {newBatch.ExpDate}, true)
                            ");
                        }

                        // --- NEW: PROCESS EXPENSES ---
                        if (result.TotalExpenses > 0)
                        {
                            // Resolve Account for Shipping/Misc Expenses
                            long accShipping = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "Shipping & Freight", 5, "EXP_DIST");

                            // Create Journal Entry: Debit Expense, Credit AP
                            ctx.AccountsJournalEntries.Add(new AccountsJournalEntry
                            {
                                TimeAsEntered = DateTime.UtcNow,
                                JournalNo = 2, // Purchase Journal
                                DebitAccountNo = accShipping,
                                DebitAccountName = "Shipping & Freight",
                                CreditAccountNo = accPayable, // Use the AP account defined earlier
                                CreditAccountName = "Accounts Payable",
                                Amount = result.TotalExpenses,
                                Description = $"Invoice #{invoice.ReceivedInvoiceNo} - Expenses",
                                Ref = invoice.ReceivedInvoiceNo.ToString(),
                                PrincipalId = (long)LoginInfo.UserId
                            });
                        }

                        // --- NEW: PROCESS PAYMENTS ---
                        if (data.Payments != null && data.Payments.Count > 0)
                        {
                            foreach (var pay in data.Payments)
                            {
                                // 1. Create Receipt Record
                                ctx.Receipts.Add(new Receipt
                                {
                                    InvoiceId = invoice.ReceivedInvoiceNo,
                                    AccountId = pay.AccountNo,
                                    Amount = pay.Amount,
                                    TimeReceived = DateTimeOffset.UtcNow
                                });

                                // 2. Create Journal Entry: Debit AP, Credit Cash/Bank/Account
                                ctx.AccountsJournalEntries.Add(new AccountsJournalEntry
                                {
                                    TimeAsEntered = DateTime.UtcNow,
                                    JournalNo = 2,
                                    DebitAccountNo = accPayable,
                                    DebitAccountName = "Accounts Payable",
                                    CreditAccountNo = pay.AccountNo,
                                    CreditAccountName = pay.AccountName,
                                    Amount = pay.Amount,
                                    Description = $"Payment for Invoice #{invoice.ReceivedInvoiceNo}",
                                    Ref = invoice.ReceivedInvoiceNo.ToString(),
                                    PrincipalId = (long)LoginInfo.UserId
                                });
                            }
                        }

                        await ctx.SaveChangesAsync();
                        await tx.CommitAsync();

                        return new PostPurchaseResponse
                        {
                            Success = true,
                            InvoiceId = invoice.ReceivedInvoiceNo,
                            AccountingEntries = accountingEntries
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

            // DTO is just the primitive dictionary, so we don't need a specific class.
            // Input: List<long>, Output: Dictionary<long, double>

            app.AddAsyncEndpointWithBearerAuth<List<long>, Dictionary<long, double>>(
                "GetStockInfo",
                async (ItemCodes, LoginInfo) =>
                {
                    var codes = (List<long>)ItemCodes;
                    if (codes == null || codes.Count == 0) return new Dictionary<long, double>();

                    using var ctx = new NewinvContext();

                    // Efficient aggregation at DB level
                    var stockData = await ctx.Inventories
                        .Where(i => codes.Contains(i.Itemcode))
                        .GroupBy(i => i.Itemcode)
                        .Select(g => new { Itemcode = g.Key, Stock = g.Sum(x => x.Units) })
                        .ToDictionaryAsync(x => x.Itemcode, x => x.Stock);

                    return stockData;
                },
                "Refresh"
            );

            // Add inside AddPurchaseEndpoints
            app.AddAsyncEndpointWithBearerAuth<long, SupplierAccountResponse>(
                "GetSupplierAccount",
                async (PiiIdI, LoginInfo) =>
                {
                    var PiiId = (long)PiiIdI;
                    using var ctx = new NewinvContext();

                    // 1. Try to find an existing AP account linked to this PII
                    var account = await ctx.AccountsInformations
                        .FirstOrDefaultAsync(a => a.AccountPii == PiiId && a.AccountType == 2); // Type 2 = Liability

                    if (account != null)
                    {
                        return new SupplierAccountResponse { AccountNo = account.AccountNo, AccountName = account.AccountName };
                    }

                    // 2. If not found, fallback to a generic "Accounts Payable" or create one?
                    // For safety, we'll return a "Suggestion" that the UI can use.
                    // Ideally, the backend creates it if needed, but let's just return the default "Accounts Payable" account.
                    var defaultAp = await ctx.AccountsInformations
                        .FirstOrDefaultAsync(a => a.AccountName == "Accounts Payable");

                    if (defaultAp != null)
                        return new SupplierAccountResponse { AccountNo = defaultAp.AccountNo, AccountName = defaultAp.AccountName };

                    // 3. If nothing exists, return 0 (UI should block posting or force creation)
                    return new SupplierAccountResponse { AccountNo = 0, AccountName = "No AP Account Configured" };
                },
                "Refresh"
            );



            return app;
        }
    }

    // DTOs

    // DTO
    public class SupplierAccountResponse
    {
        public long AccountNo { get; set; }
        public string AccountName { get; set; }
    }
    public class SimulatePurchaseRequest
    {
        public ReceivedInvoice Header { get; set; }
        public List<Purchase> Items { get; set; }

        public List<PurchaseExpense> Expenses { get; set; } // NEW
        public List<PaymentEntry> Payments { get; set; }    // NEW (reuse from Sales)
    }

    public class PurchaseExpense
    {
        public string Description { get; set; } // e.g. "Shipping"
        public double Amount { get; set; }
    }
    public class SavePurchaseDraftRequest { public string Payload { get; set; } }
    public class PostPurchaseRequest { public string Payload { get; set; } }
    public class PostPurchaseResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long InvoiceId { get; set; }
        public List<JournalEntryResult> AccountingEntries { get; set; }
    }
}