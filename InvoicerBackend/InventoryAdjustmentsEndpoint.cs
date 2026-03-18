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
            app.AddAsyncEndpointWithBearerAuth<AdjustmentAddToBatchRequest, object>(
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
                    ReferenceCode = req.DocumentNumber, // User input stored here
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

                // MUST SAVE FIRST to get the generated EntryId
                await ctx.SaveChangesAsync();

                // 2. Update Inventory
                batch.Units = newQty;
                batch.LastCountedAt = DateTime.UtcNow;

                // 3. Insert Bin Card using the new EntryId
                string binCardRef = $"adjustment:{adjustment.EntryId}";

                await ctx.Database.ExecuteSqlRawAsync(
                    @"INSERT INTO inventory_movements 
                                (itemcode, batchcode, from_units, to_units, units, entered_time, last_counted_at, 
                                 reference, remarks, is_one_off, cost_price, selling_price, marked_price, suppliercode, 
                                 volume_discounts, user_discounts, measurement_unit, packed_size, mfg_date, exp_date, batch_enabled) 
                                VALUES 
                                ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20})",
                    batch.Itemcode, batch.Batchcode, oldQty, newQty, newQty, DateTime.UtcNow,
                    DateTime.UtcNow, binCardRef, req.Reason, true,
                    batch.CostPrice, batch.SellingPrice, batch.MarkedPrice, batch.Suppliercode,
                    batch.VolumeDiscounts, batch.UserDiscounts, batch.MeasurementUnit, batch.PackedSize,
                    batch.MfgDate, batch.ExpDate, batch.BatchEnabled
                );

                // 4. Accounting
                double valueChange = Math.Abs(req.Difference) * batch.CostPrice;

                if (valueChange > 0)
                {
                    long assetAccountNo = await EnsureAccountExists(ctx, "Inventory Asset", 1, "INVENTORY");
                    long adjAccountNo = await EnsureAccountExists(ctx, "Inventory Adjustments", 5, "COGS");

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
                        InternalReference = $"ADJ-{adjustment.EntryId}"
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
                    System.Console.WriteLine($"Adjustment: AssetAccountNo: {assetAccountNo} AdjAccountNo: {adjAccountNo}");

                    JournalEntries.AddJournalEntry(ctx, journalEntry);
                }

                // Commit Transaction
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

            app.AddAsyncEndpointWithBearerAuth<CreateAdjustmentRequest, object>(
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


            app.AddAsyncEndpointWithBearerAuth<PostAdjustmentRequest, object>(
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

                            // Insert Bin Card using the EXISTING EntryId
                            string binCardRef = $"adjustment:{adjustment.EntryId}";

                            await ctx.Database.ExecuteSqlRawAsync(
                                @"INSERT INTO inventory_movements 
                                (itemcode, batchcode, from_units, to_units, units, entered_time, last_counted_at, 
                                 reference, remarks, is_one_off, cost_price, selling_price, marked_price, suppliercode, 
                                 volume_discounts, user_discounts, measurement_unit, packed_size, mfg_date, exp_date, batch_enabled) 
                                VALUES 
                                ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, {20})",
                                batch.Itemcode, batch.Batchcode, adjustment.BeforeQty, newQty, newQty, DateTime.UtcNow,
                                DateTime.UtcNow, binCardRef, adjustment.Reason, true,
                                batch.CostPrice, batch.SellingPrice, batch.MarkedPrice, batch.Suppliercode,
                                batch.VolumeDiscounts, batch.UserDiscounts, batch.MeasurementUnit, batch.PackedSize,
                                batch.MfgDate, batch.ExpDate, batch.BatchEnabled
                            );

                            double valueChange = adjustment.NetValue;

                            if (valueChange > 0)
                            {
                                long assetAccountNo = await EnsureAccountExists(ctx, "Inventory Asset", 1, "INVENTORY");
                                long adjAccountNo = await EnsureAccountExists(ctx, "Inventory Adjustments", 5, "COGS");

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
