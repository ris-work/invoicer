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

            // 4. Post Invoice (FULL LOGIC)
            app.AddAsyncEndpointWithBearerAuth<PostInvoiceRequest, PostInvoiceResponse>(
                "PostInvoice",
                async (ReqI, LoginInfo) =>
                {
                    var Req = (PostInvoiceRequest)ReqI;
                    using var ctx = new NewinvContext();
                    using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    try
                    {
                        // 1. Deserialize & Validate
                        var invoiceData = JsonSerializer.Deserialize<SimulatePaymentRequest>(Req.Payload);
                        if (invoiceData == null) throw new ArgumentException("Invalid payload.");

                        // Re-run full logic (Inventory, Pricing, Tax, Accounts)
                        var result = await InvoiceProcessingService.ProcessInvoice(ctx, invoiceData.PiiId, invoiceData.Items, invoiceData.Payments);

                        if (!result.Success)
                        {
                            return new PostInvoiceResponse { Success = false, Message = result.Message };
                        }

                        // 2. Discrepancy Check (Backend Validation)
                        // We allow a tiny tolerance for floating point math.
                        bool isSettled = result.Balance >= -0.01;
                        double discrepancy = result.Balance;

                        if (result.Balance < -0.01)
                        {
                            // TRANSACTION ROLLBACK IS HANDLED BY THE CATCH BLOCK OR EXPLICIT RETURN
                            // We return a specific error so the UI stays on the payment screen.
                            return new PostInvoiceResponse
                            {
                                Success = false,
                                Message = $"Payment validation failed: Invoice is unpaid by {Math.Abs(result.Balance):F2}."
                            };
                        }

                        // 3. Deduct Inventory
                        foreach (var item in result.Items)
                        {
                            foreach (var batch in item.SelectedBatches)
                            {
                                var dbBatch = await ctx.Inventories.FirstOrDefaultAsync(b => b.Batchcode == batch.Batchcode);
                                if (dbBatch == null) throw new Exception($"Batch {batch.Batchcode} disappeared during transaction.");
                                if (dbBatch.Units < batch.Quantity) throw new Exception($"Race condition: Insufficient stock for Batch {batch.Batchcode}.");

                                dbBatch.Units -= batch.Quantity;
                                dbBatch.LastCountedAt = DateTime.UtcNow;
                            }
                        }

                        // 4. Persist LP Redemptions
                        if (result.LpProposedRedemptions != null)
                        {
                            foreach (var prop in result.LpProposedRedemptions)
                            {
                                ctx.LoyaltyPointsRedemptions.Add(new LoyaltyPointsRedemption
                                {
                                    LoyalityPointsId = prop.BucketId,
                                    Amount = prop.Amount,
                                    CustId = invoiceData.PiiId,
                                    InvoiceId = 0, // Updated after Invoice Save
                                    RedeemedFor = "Invoice Payment",
                                    TimeIssued = DateTimeOffset.UtcNow
                                });
                            }
                        }

                        // 5. Create Invoice Header
                        var invoice = new IssuedInvoice
                        {
                            InvoiceTime = DateTime.UtcNow,
                            Customer = invoiceData.PiiId,
                            IssuedValue = result.GrandTotal,
                            IsSettled = isSettled,
                            PaidValue = result.TotalPaid,
                            SubTotal = result.GrandTotal - result.TotalTax,
                            DiscountTotal = result.Items.Sum(i => i.SelectedBatches.Sum(b => b.UnitDiscount * b.Quantity)),
                            TaxTotal = result.TotalTax,
                            GrandTotal = result.GrandTotal,
                            IsPosted = true
                        };
                        ctx.IssuedInvoices.Add(invoice);
                        await ctx.SaveChangesAsync(); // Generates InvoiceId

                        // 6. Create Sales Records
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
                                    VatAsCharged = batch.TaxAmount,
                                    EnteredAt = DateTime.UtcNow,
                                    VatCategory = 0, // TODO: Map from batch/item if needed
                                    VatRatePercentage = batch.TaxRate,
                                    TotalEffectiveSellingPrice = batch.Quantity * batch.UnitPrice
                                };
                                ctx.Sales.Add(sale);
                            }
                        }
                        await ctx.SaveChangesAsync();

                        // 7. Create Payment Records (Receipts)
                        foreach (var pay in result.PaymentResults)
                        {
                            var receipt = new Receipt
                            {
                                InvoiceId = invoice.InvoiceId,
                                AccountId = pay.AccountNo,
                                Amount = pay.NetDeposit,
                                TimeReceived = DateTimeOffset.UtcNow
                            };
                            ctx.Receipts.Add(receipt);
                        }
                        await ctx.SaveChangesAsync();

                        foreach (var lpPayment in result.PaymentResults.Where(p => p.Type == "LP"))
                        {
                            // A. CHECK BALANCE (Fresh from DB)
                            double currentBalance = LoyaltyPointsManager.GetTotalValidPoints(ctx, invoiceData.PiiId);

                            if (currentBalance < lpPayment.PointsRedeemed)
                            {
                                throw new InvalidOperationException($"Insufficient Loyalty Points. " +
                                    $"Requested: {lpPayment.PointsRedeemed}, Available: {currentBalance}");
                            }

                            // B. REDEEM (Generate Records)
                            var redemptions = LoyaltyPointsManager.Redeem(
                                ctx,
                                lpPayment.PointsRedeemed,
                                invoiceData.PiiId,
                                invoice.InvoiceId,
                                "Invoice Payment"
                            );

                            // C. APPLY TO CONTEXT
                            ctx.LoyaltyPointsRedemptions.AddRange(redemptions);

                            // D. SAVE IMMEDIATELY
                            // This ensures the next iteration (or the Issuing step) sees the updated balance
                            // reflected in the database.
                            await ctx.SaveChangesAsync();
                        }


                        // 8. Create Journal Entries
                        foreach (var entry in result.AccountingEntries)
                        {
                            var je = new AccountsJournalEntry
                            {
                                TimeAsEntered = DateTime.UtcNow,
                                TimeTai = DateTime.UtcNow,
                                PrincipalId = (long)LoginInfo.UserId,
                                PrincipalName = LoginInfo.Principal,
                                Description = entry.Narrative,
                                Ref = invoice.InvoiceId.ToString(),
                                Amount = entry.Amount,
                                DebitAccountNo = entry.DebitAccount,
                                DebitAccountName = entry.DebitAccountName,
                                CreditAccountNo = entry.CreditAccount,
                                CreditAccountName = entry.CreditAccountName,
                                JournalNo = 2 // Sales Journal
                            };
                            ctx.AccountsJournalEntries.Add(je);
                        }
                        await ctx.SaveChangesAsync();

                        //BIN CARD
                        string binCardRef = $"sales:{invoice.InvoiceId}";

                        foreach (var item in result.Items)
                        {
                            foreach (var batch in item.SelectedBatches)
                            {
                                var dbBatch = await ctx.Inventories.FirstOrDefaultAsync(b => b.Batchcode == batch.Batchcode);
                                if (dbBatch == null) throw new Exception($"Batch {batch.Batchcode} disappeared during transaction.");
                                if (dbBatch.Units < batch.Quantity) throw new Exception($"Race condition: Insufficient stock for Batch {batch.Batchcode}.");

                                double oldQty = dbBatch.Units;
                                dbBatch.Units -= batch.Quantity;
                                double newQty = dbBatch.Units;

                                // LOG TO BIN CARD (Append-Only)
                                // Using interpolated SQL for readability and safety (parameters generated automatically)
                                await ctx.Database.ExecuteSqlAsync($@"
                                    INSERT INTO inventory_movements 
                                    (itemcode, batchcode, from_units, to_units, units, entered_time, last_counted_at, 
                                     reference, remarks, is_one_off, cost_price, selling_price, marked_price, suppliercode, 
                                     volume_discounts, user_discounts, measurement_unit, packed_size, mfg_date, exp_date, batch_enabled) 
                                    VALUES 
                                    ({dbBatch.Itemcode}, {dbBatch.Batchcode}, {oldQty}, {newQty}, {newQty}, {DateTime.UtcNow}, 
                                     {DateTime.UtcNow}, {binCardRef}, 'Sale', {false}, {dbBatch.CostPrice}, {dbBatch.SellingPrice}, 
                                     {dbBatch.MarkedPrice}, {dbBatch.Suppliercode}, {dbBatch.VolumeDiscounts}, {dbBatch.UserDiscounts}, 
                                     {dbBatch.MeasurementUnit}, {dbBatch.PackedSize}, {dbBatch.MfgDate}, {dbBatch.ExpDate}, 
                                     {dbBatch.BatchEnabled})
                                ");
                            }
                        }

                        // 8. ISSUE LOYALTY POINTS (NEW ISSUANCE)
                        if (result.LoyaltyPointsFinal > 0)
                        {
                            // Determine expiry (e.g., 1 year from now, or null for no expiry)
                            // Using DateTime.UtcNow.AddYears(1) for standard expiry policy.
                            var newPoints = new LoyaltyPoint
                            {
                                CustId = invoiceData.PiiId,
                                Amount = result.LoyaltyPointsFinal,
                                ValidUntil = DateTime.UtcNow.AddYears(1),
                                ValidFrom = DateTime.UtcNow,
                                InvoiceId = invoice.InvoiceId,
                                SourceType = "INVOICE"
                            };
                            ctx.LoyaltyPoints.Add(newPoints);
                        }

                        await ctx.SaveChangesAsync();
                        double lpValue = result.LoyaltyPointsFinal;

                        // Resolve Accounts
                        long accRevenue = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "Sales Revenue", 4, "REV_SALES");
                        long accLpLiability = await SalesSimulationEndpoints.EnsureAccountExists(ctx, "Loyalty Points Liability", 2, "PROV_CUR");

                        var lpJournal = new AccountsJournalEntry
                        {
                            TimeAsEntered = DateTime.UtcNow,
                            TimeTai = DateTime.UtcNow,
                            Amount = lpValue,
                            JournalNo = 2, // Sales Journal

                            // DEBIT: Sales Revenue (Reduces recognized revenue)
                            DebitAccountNo = accRevenue,
                            DebitAccountType = 4,
                            DebitAccountName = "Sales Revenue",

                            // CREDIT: Loyalty Points Liability (Increases obligation)
                            CreditAccountNo = accLpLiability,
                            CreditAccountType = 2,
                            CreditAccountName = "Loyalty Points Liability",

                            Description = $"Loyalty Points Issued - Invoice #{invoice.InvoiceId}",
                            Ref = invoice.InvoiceId.ToString(),
                            PrincipalId = (long)LoginInfo.UserId,
                            PrincipalName = LoginInfo.Principal
                        };
                        ctx.AccountsJournalEntries.Add(lpJournal);

                        // 9. Finalize
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

                        return new PostInvoiceResponse
                        {
                            Success = true,
                            Message = "Posted",
                            InvoiceId = invoice.InvoiceId,
                            AccountingEntries = result.AccountingEntries,
                            GrandTotal = result.GrandTotal,
                            TotalPaid = result.TotalPaid,
                            Discrepancy = discrepancy,
                            IsSettled = isSettled,
                            LoyaltyPointsFinal = result.LoyaltyPointsFinal
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
    }

    // DTOs
    public class SaveDraftRequest { public string Payload { get; set; } }
    public class RestoreDraftResponse { public long TempId { get; set; } public string Payload { get; set; } }
    public class PostInvoiceRequest { public string Payload { get; set; } public long? TempId { get; set; } }
    // UPDATED DTO
    public class PostInvoiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long InvoiceId { get; set; }
        public List<JournalEntryResult> AccountingEntries { get; set; }
        public double GrandTotal { get; set; }
        public double TotalPaid { get; set; }
        public double Discrepancy { get; set; } // NEW: The balance (negative = unpaid)
        public bool IsSettled { get; set; }     // NEW: True if paid in full
        public double LoyaltyPointsFinal { get; set; }
    }
}