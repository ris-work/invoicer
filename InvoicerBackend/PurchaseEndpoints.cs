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
                    return PurchaseProcessingService.ProcessPurchase(ctx, Req.Header, Req.Items);
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

                    try
                    {
                        var data = JsonSerializer.Deserialize<SimulatePurchaseRequest>(Req.Payload);
                        var result = PurchaseProcessingService.ProcessPurchase(ctx, data.Header, data.Items);

                        if (!result.Success) return new PostPurchaseResponse { Success = false, Message = result.Message };

                        var invoice = data.Header;
                        invoice.IsPosted = true;
                        invoice.PostedAt = DateTime.UtcNow;
                        invoice.CreatedAt = DateTime.UtcNow;

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
                                ExpDate = item.ExpiryDate.DateTime,
                                MfgDate = item.ManufacturingDate?.DateTime,
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

            return app;
        }
    }

    // DTOs
    public class SimulatePurchaseRequest
    {
        public ReceivedInvoice Header { get; set; }
        public List<Purchase> Items { get; set; }
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