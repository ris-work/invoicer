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
        public static WebApplication AddInventoryAdjustmentsEndpoints(WebApplication app)
        {
            app.AddAsyncEndpointWithBearerAuth<AdjustmentAddToBatchRequest>(
    "AdjustmentAddToBatch",
    async (DataIn, LoginInfo) =>
    {
        var req = (AdjustmentAddToBatchRequest)DataIn;

        using (var ctx = new NewinvContext())
        {
            // Start Serializable Transaction
            using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // 1. Fetch Batch
                var batch = await ctx.Inventories
                    .FirstOrDefaultAsync(i => i.Itemcode == req.ItemCode && i.Batchcode == req.BatchCode);

                if (batch == null) throw new ArgumentException("Batch not found.");

                double oldQty = batch.Units;
                double newQty = oldQty + req.Difference;

                if (newQty < 0) throw new ArgumentException("Resulting quantity cannot be negative.");

                // 2. Create Adjustment Record (As-Is)
                long countLong = (long)req.Difference;

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

                // 3. Update Inventory
                batch.Units = newQty;
                batch.LastCountedAt = DateTime.UtcNow;

                // 4. Add to Bin Card (InventoryMovement)
                // Mapping to the provided schema
                var movement = new InventoryMovement
                {
                    Itemcode = batch.Itemcode,
                    Batchcode = batch.Batchcode,

                    // Quantities
                    FromUnits = oldQty,
                    ToUnits = newQty,
                    Units = newQty, // Current state snapshot

                    // Timestamps
                    EnteredTime = DateTime.UtcNow,
                    LastCountedAt = DateTime.UtcNow,

                    // References
                    Reference = $"adjustment:{req.DocumentNumber}",
                    Remarks = req.Reason,
                    IsOneOff = true, // Marking as a manual adjustment

                    // Snapshot data from Batch
                    CostPrice = batch.CostPrice,
                    SellingPrice = batch.SellingPrice,
                    MarkedPrice = batch.MarkedPrice,
                    Suppliercode = batch.Suppliercode,
                    VolumeDiscounts = batch.VolumeDiscounts,
                    UserDiscounts = batch.UserDiscounts,
                    MeasurementUnit = batch.MeasurementUnit,
                    PackedSize = batch.PackedSize,
                    MfgDate = batch.MfgDate,
                    ExpDate = batch.ExpDate,
                    BatchEnabled = batch.BatchEnabled
                };
                ctx.InventoryMovements.Add(movement);

                // 5. Accounting Entry
                double valueChange = Math.Abs(req.Difference) * batch.CostPrice;

                if (valueChange > 0)
                {
                    // Ensure Accounts Exist
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
                        JournalNo = 1, // General Journal
                        InternalReference = $"ADJ-{req.DocumentNumber}"
                    };

                    if (req.Difference > 0)
                    {
                        // Increase Inventory: Debit Asset, Credit Adj (Gain)
                        journalEntry.DebitAccountNo = assetAccountNo;
                        journalEntry.DebitAccountType = 0;
                        journalEntry.CreditAccountNo = adjAccountNo;
                        journalEntry.CreditAccountType = 3;
                    }
                    else
                    {
                        // Decrease Inventory: Debit Adj (Loss), Credit Asset
                        journalEntry.DebitAccountNo = adjAccountNo;
                        journalEntry.DebitAccountType = 3;
                        journalEntry.CreditAccountNo = assetAccountNo;
                        journalEntry.CreditAccountType = 0;
                    }

                    // Resolve Names
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
            // Start Serializable Transaction
            using var tx = await ctx.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // 1. Fetch the Adjustment Record
                var adjustment = await ctx.InventoryAdjustments
                    .FirstOrDefaultAsync(a => a.EntryId == req.EntryId);

                if (adjustment == null) throw new ArgumentException("Adjustment record not found.");
                if (adjustment.Posted) throw new InvalidOperationException("Adjustment is already posted.");

                // 2. Fetch Batch
                var batch = await ctx.Inventories
                    .FirstOrDefaultAsync(i => i.Itemcode == adjustment.Itemcode && i.Batchcode == adjustment.Batchcode);

                if (batch == null) throw new ArgumentException("Batch associated with adjustment not found.");

                // 3. Concurrency Check: Ensure stock hasn't changed since adjustment was created
                // The 'BeforeQty' in adjustment must match current 'Units' in batch
                if (batch.Units != adjustment.BeforeQty)
                {
                    throw new InvalidOperationException($"Stock changed! Current stock is {batch.Units}, but adjustment expects {adjustment.BeforeQty}. Please discard and create a new adjustment.");
                }

                double newQty = adjustment.AfterQty;
                double difference = adjustment.Count; // Count is the difference

                if (newQty < 0) throw new ArgumentException("Resulting quantity cannot be negative.");

                // 4. Update Inventory
                batch.Units = newQty;
                batch.LastCountedAt = DateTime.UtcNow;

                // 5. Add to Bin Card (InventoryMovement)
                var movement = new InventoryMovement
                {
                    Itemcode = batch.Itemcode,
                    Batchcode = batch.Batchcode,
                    FromUnits = adjustment.BeforeQty,
                    ToUnits = newQty,
                    Units = newQty,
                    EnteredTime = DateTime.UtcNow,
                    LastCountedAt = DateTime.UtcNow,
                    Reference = $"adjustment:{adjustment.ReferenceCode}",
                    Remarks = adjustment.Reason,
                    IsOneOff = true,
                    CostPrice = batch.CostPrice,
                    SellingPrice = batch.SellingPrice,
                    MarkedPrice = batch.MarkedPrice,
                    Suppliercode = batch.Suppliercode,
                    VolumeDiscounts = batch.VolumeDiscounts,
                    UserDiscounts = batch.UserDiscounts,
                    MeasurementUnit = batch.MeasurementUnit,
                    PackedSize = batch.PackedSize,
                    MfgDate = batch.MfgDate,
                    ExpDate = batch.ExpDate,
                    BatchEnabled = batch.BatchEnabled
                };
                ctx.InventoryMovements.Add(movement);

                // 6. Accounting Entry
                double valueChange = adjustment.NetValue;

                if (valueChange > 0)
                {
                    long assetAccountNo = await EnsureAccountExists(ctx, "Inventory Asset", 0);
                    long adjAccountNo = await EnsureAccountExists(ctx, "Inventory Adjustments", 3);

                    var journalEntry = new AccountsJournalEntry
                    {
                        TimeAsEntered = DateTime.UtcNow,
                        TimeTai = DateTime.UtcNow,
                        PrincipalId = (long)LoginInfo.UserId, // The user posting
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

                // 7. Mark Adjustment as Posted
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
        private static async Task<long> EnsureAccountExists(NewinvContext ctx, string accountName, int accountType)
        {
            var account = await ctx.AccountsInformations
                .FirstOrDefaultAsync(a => a.AccountName == accountName);

            if (account != null) return account.AccountNo;

            var newAccount = new AccountsInformation
            {
                AccountName = accountName,
                AccountType = accountType,
                AccountMin = -1000000000,
                AccountMax = 1000000000,
                HumanFriendlyId = accountName.ToUpper().Replace(" ", "_")
            };

            ctx.AccountsInformations.Add(newAccount);
            await ctx.SaveChangesAsync(); // Save to get the generated ID

            return newAccount.AccountNo;
        }
    }
}
