using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    // DTO for Adjust
    public class AdjustmentAddToBatchRequest
    {
        public long ItemCode { get; set; }
        public long BatchCode { get; set; }
        public double Difference { get; set; } // Canonical: positive = add, negative = remove
        public string Reason { get; set; }
        public string DocumentNumber { get; set; } // Maps to ReferenceCode
    }

    // DTO for Create
    public class CreateAdjustmentRequest
    {
        public long ItemCode { get; set; }
        public long BatchCode { get; set; }
        public double Difference { get; set; }
        public string Reason { get; set; }
        public string DocumentNumber { get; set; }
    }


    // DTO for Post
    public class PostAdjustmentRequest
    {
        public long EntryId { get; set; }
    }
    public static class InventoryAdjustmentsEndpoint
    {
        public static WebApplication AddInventoryAdjustmentsEndpoints(this WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<AdjustmentAddToBatchRequest>(
    "AdjustmentAddToBatch",
    async (DataIn, LoginInfo) =>
    {
        var req = (AdjustmentAddToBatchRequest)DataIn;

        using (var ctx = new NewinvContext())
        {
            using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var batch = await ctx.Inventories
                    .FirstOrDefaultAsync(i => i.Itemcode == req.ItemCode && i.Batchcode == req.BatchCode);

                if (batch == null) throw new ArgumentException("Batch not found.");

                double oldQty = batch.Units;
                double newQty = oldQty + req.Difference;
                long countLong = (long)req.Difference;

                if (newQty < 0) throw new ArgumentException("Resulting quantity cannot be negative.");

                // 1. Create Adjustment Record
                var adjustment = new InventoryAdjustment
                {
                    Itemcode = req.ItemCode,
                    Batchcode = req.BatchCode,
                    ReferenceCode = req.DocumentNumber,
                    BeforeQty = oldQty,
                    AfterQty = newQty,
                    Count = countLong,
                    PerItemValue = batch.CostPrice,
                    NetValue = Math.Abs(countLong) * batch.CostPrice,
                    Reason = req.Reason,
                    CreatedAt = DateTime.UtcNow,
                    EditedAt = DateTimeOffset.UtcNow,
                    CreatedBy = (long)LoginInfo.UserId,
                    ProcessedBy = (long)LoginInfo.UserId,
                    EditedBy = (long)LoginInfo.UserId,
                    Posted = true,
                    AdjustmentBatch = 0
                };
                ctx.InventoryAdjustments.Add(adjustment);

                // 2. Update Inventory
                batch.Units = newQty;
                batch.LastCountedAt = DateTime.UtcNow;

                // 3. Insert Bin Card (Safe Raw SQL)
                string refString = "adjustment:" + req.DocumentNumber;
                await ctx.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO inventory_movements 
                                (itemcode, batchcode, from_units, to_units, units, entered_time, last_counted_at, 
                                 reference, remarks, is_one_off, cost_price, selling_price, marked_price, suppliercode, 
                                 volume_discounts, user_discounts, measurement_unit, packed_size, mfg_date, exp_date, batch_enabled) 
                                VALUES 
                                ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20})",
                    batch.Itemcode, batch.Batchcode, oldQty, newQty, newQty, DateTime.UtcNow,
                    DateTime.UtcNow, refString, req.Reason, true,
                    batch.CostPrice, batch.SellingPrice, batch.MarkedPrice, batch.Suppliercode,
                    batch.VolumeDiscounts, batch.UserDiscounts, batch.MeasurementUnit, batch.PackedSize,
                    batch.MfgDate, batch.ExpDate, batch.BatchEnabled
                );

                // 4. Accounting
                double valueChange = Math.Abs(req.Difference) * batch.CostPrice;

                if (valueChange > 0)
                {
                    long assetAccountNo = await EnsureAccountExists(ctx, "Inventory Asset", 0);
                    long adjAccountNo = await EnsureAccountExists(ctx, "Inventory Adjustments", 3);

                    var journalEntry = new AccountsJournalEntry
                    {
                        TimeAsEntered = DateTime.UtcNow,
                        TimeTai = DateTime.UtcNow,
                        PrincipalId = (long)LoginInfo.UserId,
                        PrincipalName = LoginInfo.Principal,
                        Description = $"Adjustment Doc: {req.DocumentNumber} - {req.Reason}",
                        Ref = req.DocumentNumber,
                        Amount = valueChange,
                        JournalNo = 1,
                        InternalReference = $"ADJ-{req.DocumentNumber}"
                    };

                    if (req.Difference > 0)
                    {
                        journalEntry.DebitAccountNo = assetAccountNo;
                        journalEntry.DebitAccountType = 0;
                        journalEntry.CreditAccountNo = adjAccountNo;
                        journalEntry.CreditAccountType = 3;
                    }
                    else
                    {
                        journalEntry.DebitAccountNo = adjAccountNo;
                        journalEntry.DebitAccountType = 3;
                        journalEntry.CreditAccountNo = assetAccountNo;
                        journalEntry.CreditAccountType = 0;
                    }

                    journalEntry.DebitAccountName = (await ctx.AccountsInformations.FindAsync(journalEntry.DebitAccountNo))?.AccountName;
                    journalEntry.CreditAccountName = (await ctx.AccountsInformations.FindAsync(journalEntry.CreditAccountNo))?.AccountName;

                    JournalEntries.AddJournalEntry(ctx, journalEntry);
                }

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();

                return new { Success = true, AdjustmentId = adjustment.EntryId, NewQuantity = newQty };
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    },
    "Refresh"
);

            app.AddAsyncEndpointWithBearerAuth<CreateAdjustmentRequest>(
    "CreateUnpostedAdjustment",
    async (DataIn, LoginInfo) =>
    {
        var req = (CreateAdjustmentRequest)DataIn;

        using (var ctx = new NewinvContext())
        {
            // 1. Fetch Batch to get current state and cost
            var batch = await ctx.Inventories
                .FirstOrDefaultAsync(i => i.Itemcode == req.ItemCode && i.Batchcode == req.BatchCode);

            if (batch == null) throw new ArgumentException("Batch not found.");

            double oldQty = batch.Units;
            double newQty = oldQty + req.Difference;

            // We allow creating the record even if it results in negative, 
            // but posting will fail. This allows supervisors to review logic.

            long countLong = (long)req.Difference;

            // 2. Create Adjustment Record (Unposted)
            var adjustment = new InventoryAdjustment
            {
                Itemcode = req.ItemCode,
                Batchcode = req.BatchCode,
                ReferenceCode = req.DocumentNumber,
                BeforeQty = oldQty,
                AfterQty = newQty,
                Count = countLong,
                PerItemValue = batch.CostPrice,
                NetValue = Math.Abs(countLong) * batch.CostPrice,
                Reason = req.Reason,
                CreatedAt = DateTime.UtcNow,
                EditedAt = DateTimeOffset.UtcNow,
                CreatedBy = (long)LoginInfo.UserId,
                ProcessedBy = 0, // Not processed yet
                EditedBy = (long)LoginInfo.UserId,
                Posted = false, // Unposted
                AdjustmentBatch = 0
            };

            ctx.InventoryAdjustments.Add(adjustment);
            await ctx.SaveChangesAsync();

            return new { Success = true, AdjustmentId = adjustment.EntryId, Status = "Unposted" };
        }
    },
    "Refresh"
);


            app.AddAsyncEndpointWithBearerAuth<PostAdjustmentRequest>(
    "PostAdjustment",
    async (DataIn, LoginInfo) =>
    {
        var req = (PostAdjustmentRequest)DataIn;

        using (var ctx = new NewinvContext())
        {
            using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var adjustment = await ctx.InventoryAdjustments.FirstOrDefaultAsync(a => a.EntryId == req.EntryId);
                if (adjustment == null) throw new ArgumentException("Adjustment record not found.");
                if (adjustment.Posted) throw new InvalidOperationException("Adjustment is already posted.");

                var batch = await ctx.Inventories.FirstOrDefaultAsync(i => i.Itemcode == adjustment.Itemcode && i.Batchcode == adjustment.Batchcode);
                if (batch == null) throw new ArgumentException("Batch associated with adjustment not found.");

                if (batch.Units != adjustment.BeforeQty)
                    throw new InvalidOperationException($"Stock changed! Current stock is {batch.Units}, but adjustment expects {adjustment.BeforeQty}.");

                double newQty = adjustment.AfterQty;
                double difference = adjustment.Count;

                if (newQty < 0) throw new ArgumentException("Resulting quantity cannot be negative.");

                batch.Units = newQty;
                batch.LastCountedAt = DateTime.UtcNow;

                // Insert Bin Card (Safe Raw SQL)
                string refString = "adjustment:" + adjustment.ReferenceCode;
                await ctx.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO inventory_movements 
                                (itemcode, batchcode, from_units, to_units, units, entered_time, last_counted_at, 
                                 reference, remarks, is_one_off, cost_price, selling_price, marked_price, suppliercode, 
                                 volume_discounts, user_discounts, measurement_unit, packed_size, mfg_date, exp_date, batch_enabled) 
                                VALUES 
                                ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20})",
                    batch.Itemcode, batch.Batchcode, adjustment.BeforeQty, newQty, newQty, DateTime.UtcNow,
                    DateTime.UtcNow, refString, adjustment.Reason, true,
                    batch.CostPrice, batch.SellingPrice, batch.MarkedPrice, batch.Suppliercode,
                    batch.VolumeDiscounts, batch.UserDiscounts, batch.MeasurementUnit, batch.PackedSize,
                    batch.MfgDate, batch.ExpDate, batch.BatchEnabled
                );

                double valueChange = adjustment.NetValue;

                if (valueChange > 0)
                {
                    long assetAccountNo = await EnsureAccountExists(ctx, "Inventory Asset", 0);
                    long adjAccountNo = await EnsureAccountExists(ctx, "Inventory Adjustments", 3);

                    var journalEntry = new AccountsJournalEntry
                    {
                        TimeAsEntered = DateTime.UtcNow,
                        TimeTai = DateTime.UtcNow,
                        PrincipalId = (long)LoginInfo.UserId,
                        PrincipalName = LoginInfo.Principal,
                        Description = $"Posted Adjustment: {adjustment.ReferenceCode} - {adjustment.Reason}",
                        Ref = adjustment.ReferenceCode,
                        Amount = valueChange,
                        JournalNo = 1,
                        InternalReference = $"ADJ-POST-{adjustment.EntryId}"
                    };

                    if (difference > 0)
                    {
                        journalEntry.DebitAccountNo = assetAccountNo;
                        journalEntry.DebitAccountType = 0;
                        journalEntry.CreditAccountNo = adjAccountNo;
                        journalEntry.CreditAccountType = 3;
                    }
                    else
                    {
                        journalEntry.DebitAccountNo = adjAccountNo;
                        journalEntry.DebitAccountType = 3;
                        journalEntry.CreditAccountNo = assetAccountNo;
                        journalEntry.CreditAccountType = 0;
                    }

                    journalEntry.DebitAccountName = (await ctx.AccountsInformations.FindAsync(journalEntry.DebitAccountNo))?.AccountName;
                    journalEntry.CreditAccountName = (await ctx.AccountsInformations.FindAsync(journalEntry.CreditAccountNo))?.AccountName;

                    JournalEntries.AddJournalEntry(ctx, journalEntry);
                }

                adjustment.Posted = true;
                adjustment.ProcessedBy = (long)LoginInfo.UserId;
                adjustment.EditedAt = DateTimeOffset.UtcNow;
                adjustment.EditedBy = (long)LoginInfo.UserId;

                await ctx.SaveChangesAsync();
                await tx.CommitAsync();

                return new { Success = true, NewQuantity = newQty };
            }
            catch (Exception)
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    },
    "Refresh"
);
            return app;
        }
        // Helper to ensure account exists (Let PG handle ID generation)
        // Helper to ensure account exists with specific Name AND Type
        // Helper to ensure account exists AND has a balance record
        private static async Task<long> EnsureAccountExists(NewinvContext ctx, string accountName, int accountType)
        {
            // 1. Check/Create AccountsInformation
            var account = await ctx.AccountsInformations
                .FirstOrDefaultAsync(a => a.AccountName == accountName && a.AccountType == accountType);

            long accountNo;

            if (account != null)
            {
                accountNo = account.AccountNo;
            }
            else
            {
                var newAccount = new AccountsInformation
                {
                    AccountName = accountName,
                    AccountType = accountType,
                    AccountMin = -1000000000,
                    AccountMax = 1000000000,
                    HumanFriendlyId = $"{accountName.ToUpper().Replace(" ", "_")}_{accountType}"
                };

                ctx.AccountsInformations.Add(newAccount);
                await ctx.SaveChangesAsync();
                accountNo = newAccount.AccountNo;
            }

            // 2. Check/Create AccountsBalance (Crucial for JournalEntries.AddJournalEntry)
            var balance = await ctx.AccountsBalances
                .FirstOrDefaultAsync(b => b.AccountType == accountType && b.AccountNo == accountNo);

            if (balance == null)
            {
                var newBalance = new AccountsBalance
                {
                    AccountType = accountType,
                    AccountNo = accountNo,
                    Amount = 0 // Initialize at zero
                };
                ctx.AccountsBalances.Add(newBalance);
                await ctx.SaveChangesAsync();
            }

            return accountNo;
        }
    }
}
