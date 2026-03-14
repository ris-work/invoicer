using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoicerBackend
{
    public static class InvoicePersistenceEndpoints
    {
        public static WebApplication AddInvoicePersistenceEndpoints(this WebApplication app)
        {
            // 1. Save Draft (WIP)
            app.AddAsyncEndpointWithBearerAuth<SaveDraftRequest, long>(
                "SaveDraftInvoice",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (SaveDraftRequest)ReqI;
                    using var ctx = new NewinvContext();

                    // We just save the payload as-is. 
                    // Validation happens on Post/Simulate.
                    var json = Req.Payload;
                    var userId = (long)LoginInfo.UserId;

                    var entity = new TempIssuedInvoice
                    {
                        InvoiceContents = json,
                        Posted = false,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UserId = userId,
                        RequestId = LoginInfo.RequestId,
                        RequestIds = " " + LoginInfo.RequestId
                    };

                    ctx.TempIssuedInvoices.Add(entity);
                    await ctx.SaveChangesAsync();

                    return entity.TempInvoiceRunNo;
                },
                "Refresh"
            );

            // 2. Load Draft
            app.AddAsyncEndpointWithBearerAuth<long, RestoreDraftResponse>(
                "LoadDraftInvoice",
                async (TempIdI, LoginInfo) =>
                {
                    var TempId = (long)TempIdI;
                    using var ctx = new NewinvContext();

                    var entity = await ctx.TempIssuedInvoices
                        .FirstOrDefaultAsync(t => t.TempInvoiceRunNo == TempId && t.UserId == (long)LoginInfo.UserId);

                    if (entity == null) throw new ArgumentException("Draft not found or access denied.");

                    return new RestoreDraftResponse
                    {
                        TempId = entity.TempInvoiceRunNo,
                        Payload = entity.InvoiceContents
                    };
                },
                "Refresh"
            );

            // 3. List Unposted
            app.AddAsyncEndpointWithBearerAuth<string, List<TempIssuedInvoice>>(
                "ListUnpostedInvoices",
                async (_, LoginInfo) =>
                {
                    using var ctx = new NewinvContext();

                    return await ctx.TempIssuedInvoices
                        .Where(t => t.UserId == (long)LoginInfo.UserId && !t.Posted)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(100)
                        .ToListAsync();
                },
                "Refresh"
            );

            // 4. Post Invoice (Strict Re-Validation + Commit)
            app.AddAsyncEndpointWithBearerAuth<PostInvoiceRequest, PostInvoiceResponse>(
                "PostInvoice",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (PostInvoiceRequest)ReqI;
                    using var ctx = new NewinvContext();
                    // Use Transaction for Atomicity
                    using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    try
                    {
                        // 1. Deserialize Payload
                        var invoiceData = JsonSerializer.Deserialize<SimulatePaymentRequest>(Req.Payload);
                        if (invoiceData == null) throw new ArgumentException("Invalid payload.");

                        // 2. RE-RUN VALIDATION (CRITICAL)
                        // This ensures inventory, tax, and payments are valid RIGHT NOW.
                        var result = await InvoiceProcessingService.ProcessInvoice(ctx, invoiceData.PiiId, invoiceData.Items, invoiceData.Payments);

                        if (!result.Success)
                        {
                            return new PostInvoiceResponse { Success = false, Message = result.Message };
                        }

                        // 3. DEDUCT REAL INVENTORY
                        // We use the SelectedBatches from the result to perform actual DB deductions
                        foreach (var item in result.Items)
                        {
                            foreach (var batch in item.SelectedBatches)
                            {
                                var dbBatch = await ctx.Inventories.FirstOrDefaultAsync(b => b.Batchcode == batch.Batchcode);
                                if (dbBatch == null) throw new Exception($"Batch {batch.Batchcode} disappeared during transaction.");
                                if (dbBatch.Units < batch.Quantity) throw new Exception($"Race condition: Insufficient stock for Batch {batch.Batchcode}.");

                                dbBatch.Units -= batch.Quantity;
                            }
                        }

                        if (result.LpProposedRedemptions.Any())
                        {
                            foreach (var prop in result.LpProposedRedemptions)
                            {
                                // Create the actual DB record
                                ctx.LoyaltyPointsRedemptions.Add(new LoyaltyPointsRedemption
                                {
                                    LoyalityPointsId = prop.BucketId,
                                    Amount = prop.Amount,
                                    CustId = invoiceData.PiiId, // Resolve from context if needed
                                    InvoiceId = 0, // Update with actual Invoice ID if generated
                                    RedeemedFor = "Invoice Payment",
                                    TimeIssued = DateTimeOffset.UtcNow
                                });
                            }
                        }

                        // 4. CREATE ISSUED INVOICE HEADER
                        var invoice = new IssuedInvoice
                        {
                            InvoiceTime = DateTime.UtcNow,
                            Customer = invoiceData.PiiId,
                            IssuedValue = result.GrandTotal,
                            IsSettled = false, // Depends on Balance
                            PaidValue = result.TotalPaid,
                            // ... other fields
                            IsPosted = true
                        };
                        ctx.IssuedInvoices.Add(invoice);
                        await ctx.SaveChangesAsync(); // Get InvoiceId

                        // 5. CREATE SALES RECORDS
                        foreach (var item in result.Items)
                        {
                            foreach (var batch in item.SelectedBatches)
                            {
                                var sale = new Sale
                                {
                                    InvoiceId = invoice.InvoiceId,
                                    Itemcode = item.ItemCode,
                                    Batchcode = batch.Batchcode,
                                    Quantity = batch.Quantity,
                                    SellingPrice = batch.UnitPrice,
                                    Discount = batch.UnitDiscount,
                                    // Map other fields...
                                    VatAsCharged = batch.TaxAmount, // Approx mapping
                                    EnteredAt = DateTime.UtcNow
                                };
                                ctx.Sales.Add(sale);
                            }
                        }
                        await ctx.SaveChangesAsync();

                        // 6. CREATE PAYMENT RECORDS (Receipts)
                        foreach (var pay in result.PaymentResults)
                        {
                            var receipt = new Receipt
                            {
                                InvoiceId = invoice.InvoiceId,
                                AccountId = pay.AccountNo,
                                Amount = pay.NetDeposit, // Actual cash received
                                TimeReceived = DateTimeOffset.UtcNow
                            };
                            ctx.Receipts.Add(receipt);
                        }
                        await ctx.SaveChangesAsync();

                        // 7. CREATE JOURNAL ENTRIES
                        foreach (var entry in result.AccountingEntries)
                        {
                            var je = new AccountsJournalEntry
                            {
                                TimeAsEntered = DateTime.UtcNow,
                                TimeTai = DateTime.UtcNow,
                                PrincipalId = (long)LoginInfo.UserId,
                                PrincipalName = LoginInfo.Principal,
                                Description = entry.Narrative,
                                Ref = invoice.InvoiceId.ToString(), // Link to Invoice
                                Amount = entry.Amount,
                                DebitAccountNo = entry.DebitAccount,
                                CreditAccountNo = entry.CreditAccount,
                                // ... Set DebitAccountName, etc from entry object
                            };
                            // Use helper: JournalEntries.AddJournalEntry(ctx, je);
                            // For now, direct add:
                            ctx.AccountsJournalEntries.Add(je);
                        }
                        await ctx.SaveChangesAsync();

                        // 8. MARK TEMP INVOICE AS POSTED
                        if (Req.TempId.HasValue)
                        {
                            var temp = await ctx.TempIssuedInvoices.FindAsync(Req.TempId.Value);
                            if (temp != null)
                            {
                                temp.Posted = true;
                                temp.ModifiedAt = DateTime.UtcNow;
                            }
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
    }

    // DTOs
    public class SaveDraftRequest { public string Payload { get; set; } }
    public class RestoreDraftResponse { public long TempId { get; set; } public string Payload { get; set; } }
    public class PostInvoiceRequest { public string Payload { get; set; } public long? TempId { get; set; } }
    public class PostInvoiceResponse { public bool Success { get; set; } public string Message { get; set; } }
}