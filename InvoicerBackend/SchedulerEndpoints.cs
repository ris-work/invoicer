using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class SchedulerEndpoints
    {
        // --- Future View Endpoints ---

        // --- Updated Future View Endpoints ---

        public class FutureEntryDto
        {
            public string Source { get; set; } // "Journal", "Payment", "Receipt"
            public string Status { get; set; } // "Posted", "Overdue", "Due Today", "Scheduled", "Processing"
            public DateTime Date { get; set; } // Unified Date for sorting
            public double Amount { get; set; }
            public string DebitAccountName { get; set; }
            public string CreditAccountName { get; set; }
            public string? Description { get; set; }
            public bool IsAutomatic { get; set; }
            public long? ReferenceId { get; set; } // ID of the source record
        }

        // DTO Update
        public class AccountFutureRequest
        {
            public long AccountNo { get; set; }
            public DateOnly? UntilDate { get; set; } // Optional filter
        }

        // DTO Update
        public class AddScheduledEntryRequest
        {
            public string Type { get; set; } // "Payment" or "Receipt"
            public long DebitAccountId { get; set; }
            public long CreditAccountId { get; set; }
            public double Amount { get; set; }
            public string Description { get; set; }
            public string Frequency { get; set; } // "Once", "Daily", "Weekly", "Monthly"
            public DateOnly NextRunDate { get; set; }
            public bool IsAutomatic { get; set; }
            public string PaymentMethod { get; set; } // Added
            public int JournalNo { get; set; } // Added
        }

        // --- Cheque Management Endpoints (JIT Approach) ---

        // 1. Create Cheque Book (No pre-population)
        public class CreateChequeBookRequest
        {
            public long AccountId { get; set; }
            public long StartNumber { get; set; }
            public long EndNumber { get; set; }
        }

        // 2. Get Available Cheque Leaves (Calculates holes dynamically)
        public class GetChequeLeavesRequest { public long AccountId { get; set; } }

        // 3. Updated AddScheduledEntryWeb to handle JIT Leaf Creation
        public class AddScheduledEntryRequestEx
        {
            public string Type { get; set; }
            public long DebitAccountId { get; set; }
            public long CreditAccountId { get; set; }
            public double Amount { get; set; }
            public string Description { get; set; }
            public string Frequency { get; set; }
            public DateOnly NextRunDate { get; set; }
            public bool IsAutomatic { get; set; }
            public string PaymentMethod { get; set; }
            public int JournalNo { get; set; }

            // Cheque Specifics
            public long? ChequeBookId { get; set; }
            public long? ChequeLeafNumber { get; set; }
        }

        public static WebApplication AddSchedulerEndpoints(this WebApplication app)
        {
            // 1. Get Future Journal Overview (All Pending Scheduled Items)
            app.AddAsyncEndpointWithBearerAuth<object>(
                "GetFutureJournalOverview",
                async (DataIn, LoginInfo) =>
                {
                    using (var ctx = new NewinvContext())
                    {
                        var today = DateOnly.FromDateTime(DateTime.UtcNow);

                        // Payments
                        var payments = await ctx.ScheduledPayments
                            .Where(p => p.IsPending) // All pending, regardless of date
                            .Join(ctx.AccountsInformations, p => p.DebitAccountId, a => a.AccountNo, (p, a) => new { p, DebitName = a.AccountName })
                            .Join(ctx.AccountsInformations, x => x.p.CreditAccountId, a => a.AccountNo, (x, a) => new FutureEntryDto
                            {
                                Source = "Payment",
                                Date = x.p.NextRunDate.ToDateTime(TimeOnly.MinValue),
                                Amount = x.p.Amount,
                                DebitAccountName = x.DebitName,
                                CreditAccountName = a.AccountName,
                                Description = x.p.Description,
                                IsAutomatic = x.p.IsAutomaticClear,
                                ReferenceId = x.p.Id
                            }).ToListAsync();

                        // Receipts
                        var receipts = await ctx.ScheduledReceipts
                            .Where(r => r.IsPending)
                            .Join(ctx.AccountsInformations, r => r.DebitAccountId, a => a.AccountNo, (r, a) => new { r, DebitName = a.AccountName })
                            .Join(ctx.AccountsInformations, x => x.r.CreditAccountId, a => a.AccountNo, (x, a) => new FutureEntryDto
                            {
                                Source = "Receipt",
                                Date = x.r.NextRunDate.ToDateTime(TimeOnly.MinValue),
                                Amount = x.r.Amount,
                                DebitAccountName = x.DebitName,
                                CreditAccountName = a.AccountName,
                                Description = x.r.Description,
                                IsAutomatic = x.r.IsAutomaticClear,
                                ReferenceId = x.r.Id
                            }).ToListAsync();

                        var combined = payments.Concat(receipts).ToList();

                        // Determine Status
                        foreach (var item in combined)
                        {
                            var runDate = DateOnly.FromDateTime(item.Date);
                            if (runDate < today) item.Status = "Overdue";
                            else if (runDate == today) item.Status = "Due Today";
                            else item.Status = "Scheduled";
                        }

                        return combined.OrderBy(x => x.Date).ToList();
                    }
                },
                "Refresh"
            );
            app.AddAsyncEndpointWithBearerAuth<AccountFutureRequest>(
                "GetAccountFuture",
                async (DataIn, LoginInfo) =>
                {
                    var req = (AccountFutureRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var today = DateOnly.FromDateTime(DateTime.UtcNow);
                        var results = new List<FutureEntryDto>();

                        // Determine the cutoff date (Either the filter or MaxValue)
                        var cutoffDate = req.UntilDate ?? DateOnly.MaxValue;
                        var cutoffDateTime = cutoffDate.ToDateTime(TimeOnly.MaxValue);

                        // 1. History (Journal Entries)
                        var historyQuery = ctx.AccountsJournalEntries
                            .Where(j => j.CreditAccountNo == req.AccountNo || j.DebitAccountNo == req.AccountNo);

                        if (req.UntilDate.HasValue)
                        {
                            // If filter is set, filter by date
                            historyQuery = historyQuery.Where(j => j.TimeTai <= cutoffDateTime);
                        }

                        var history = await historyQuery
                            .OrderByDescending(j => j.TimeTai)
                            .Take(50) // Keep reasonable limit
                            .ToListAsync();

                        foreach (var h in history)
                        {
                            results.Add(new FutureEntryDto
                            {
                                Source = "Journal",
                                Status = "Posted",
                                Date = h.TimeTai,
                                Amount = h.Amount,
                                DebitAccountName = h.DebitAccountName,
                                CreditAccountName = h.CreditAccountName,
                                Description = h.Description,
                                IsAutomatic = false,
                                ReferenceId = h.JournalUnivSeq
                            });
                        }

                        // 2. Scheduled Payments
                        var paymentsQuery = ctx.ScheduledPayments
                            .Where(p => p.IsPending && (p.DebitAccountId == req.AccountNo || p.CreditAccountId == req.AccountNo));

                        if (req.UntilDate.HasValue)
                        {
                            paymentsQuery = paymentsQuery.Where(p => p.NextRunDate <= cutoffDate);
                        }

                        var payments = await paymentsQuery
                            .Join(ctx.AccountsInformations, p => p.DebitAccountId, a => a.AccountNo, (p, a) => new { p, DebitName = a.AccountName })
                            .Join(ctx.AccountsInformations, x => x.p.CreditAccountId, a => a.AccountNo, (x, a) => new FutureEntryDto
                            {
                                Source = "Payment",
                                Date = x.p.NextRunDate.ToDateTime(TimeOnly.MinValue),
                                Amount = x.p.Amount,
                                DebitAccountName = x.DebitName,
                                CreditAccountName = a.AccountName,
                                Description = x.p.Description,
                                IsAutomatic = x.p.IsAutomaticClear,
                                ReferenceId = x.p.Id
                            }).ToListAsync();

                        // 3. Scheduled Receipts
                        var receiptsQuery = ctx.ScheduledReceipts
                            .Where(r => r.IsPending && (r.DebitAccountId == req.AccountNo || r.CreditAccountId == req.AccountNo));

                        if (req.UntilDate.HasValue)
                        {
                            receiptsQuery = receiptsQuery.Where(r => r.NextRunDate <= cutoffDate);
                        }

                        var receipts = await receiptsQuery
                            .Join(ctx.AccountsInformations, r => r.DebitAccountId, a => a.AccountNo, (r, a) => new { r, DebitName = a.AccountName })
                            .Join(ctx.AccountsInformations, x => x.r.CreditAccountId, a => a.AccountNo, (x, a) => new FutureEntryDto
                            {
                                Source = "Receipt",
                                Date = x.r.NextRunDate.ToDateTime(TimeOnly.MinValue),
                                Amount = x.r.Amount,
                                DebitAccountName = x.DebitName,
                                CreditAccountName = a.AccountName,
                                Description = x.r.Description,
                                IsAutomatic = x.r.IsAutomaticClear,
                                ReferenceId = x.r.Id
                            }).ToListAsync();

                        var scheduled = payments.Concat(receipts).ToList();

                        // Determine Status
                        foreach (var item in scheduled)
                        {
                            var runDate = DateOnly.FromDateTime(item.Date);
                            if (runDate < today) item.Status = "Overdue";
                            else if (runDate == today) item.Status = "Due Today";
                            else item.Status = "Scheduled";
                        }

                        results.AddRange(scheduled);
                        return results.OrderBy(x => x.Date).ToList();
                    }
                },
                "Refresh"
            );
            app.AddAsyncEndpointWithBearerAuth<AddScheduledEntryRequestEx>(
                "AddScheduledEntryWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (AddScheduledEntryRequestEx)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        long entryId = 0;

                        if (req.Type == "Payment")
                        {
                            var entry = new ScheduledPayment
                            {
                                DebitAccountId = req.DebitAccountId,
                                CreditAccountId = req.CreditAccountId,
                                Amount = req.Amount,
                                Description = req.Description,
                                Frequency = req.Frequency,
                                NextRunDate = req.NextRunDate,
                                IsAutomaticClear = req.IsAutomatic,
                                IsPending = true,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = (long)LoginInfo.UserId,
                                CompanyId = 1,
                                PaymentReference = $"SCH-{DateTime.UtcNow.Ticks}",
                                Currency = "LKR",
                                ExchangeRate = 1.0,
                                PaymentMethod = req.PaymentMethod ?? "Manual",
                                JournalNo = req.JournalNo
                            };
                            ctx.ScheduledPayments.Add(entry);
                            await ctx.SaveChangesAsync();
                            entryId = entry.Id;

                            // JIT Cheque Leaf Handling
                            if (req.PaymentMethod == "Cheque" && req.ChequeBookId.HasValue && req.ChequeLeafNumber.HasValue)
                            {
                                // Check if leaf somehow already exists (race condition safety)
                                var existingLeaf = await ctx.ChequeLeaves.FirstOrDefaultAsync(l =>
                                    l.ChequeBookId == req.ChequeBookId.Value && l.LeafNumber == req.ChequeLeafNumber.Value);

                                if (existingLeaf != null) throw new Exception($"Cheque leaf #{req.ChequeLeafNumber} is already issued.");

                                // Create the leaf record now
                                var newLeaf = new ChequeLeaf
                                {
                                    ChequeBookId = req.ChequeBookId.Value,
                                    LeafNumber = req.ChequeLeafNumber.Value,
                                    Status = "Issued",
                                    PayeeName = req.Description,
                                    Amount = req.Amount,
                                    IssuedAt = DateTime.UtcNow,
                                    TxId = $"scheduledpayment:{entryId}",
                                    UpdatedAt = DateTimeOffset.UtcNow
                                };
                                ctx.ChequeLeaves.Add(newLeaf);

                                // Update Book's NextNumber pointer if this was the expected next
                                var book = await ctx.ChequeBooks.FindAsync(req.ChequeBookId.Value);
                                if (book != null && book.NextNumber == req.ChequeLeafNumber.Value)
                                {
                                    book.NextNumber++;
                                    book.UpdatedAt = DateTime.UtcNow;
                                }

                                await ctx.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            // Receipt logic (same as before)
                            var entry = new ScheduledReceipt
                            {
                                DebitAccountId = req.DebitAccountId,
                                CreditAccountId = req.CreditAccountId,
                                Amount = req.Amount,
                                Description = req.Description,
                                Frequency = req.Frequency,
                                NextRunDate = req.NextRunDate,
                                IsAutomaticClear = req.IsAutomatic,
                                IsPending = true,
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = (long)LoginInfo.UserId,
                                CompanyId = 1,
                                PaymentReference = $"SCH-{DateTime.UtcNow.Ticks}",
                                Currency = "LKR",
                                ExchangeRate = 1.0,
                                PaymentMethod = req.PaymentMethod ?? "Manual",
                                JournalNo = req.JournalNo
                            };
                            ctx.ScheduledReceipts.Add(entry);
                            await ctx.SaveChangesAsync();
                            entryId = entry.Id;
                        }

                        return true;
                    }
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<GetChequeLeavesRequest>(
                "GetAvailableChequeLeaves",
                async (DataIn, LoginInfo) =>
                {
                    var req = (GetChequeLeavesRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        // Find active books
                        var books = await ctx.ChequeBooks
                            .Where(b => b.AccountId == req.AccountId && b.IsOpen && !b.IsCancelled)
                            .ToListAsync();

                        if (!books.Any()) return new { Leaves = new List<object>(), Remaining = 0 };

                        var availableLeaves = new List<object>();
                        int totalRemaining = 0;

                        foreach (var book in books)
                        {
                            // Get used/skipped numbers for this book
                            var usedNumbers = await ctx.ChequeLeaves
                                .Where(l => l.ChequeBookId == book.Id)
                                .Select(l => l.LeafNumber)
                                .ToListAsync();

                            // Find gaps
                            for (long i = book.StartNumber; i <= book.EndNumber; i++)
                            {
                                if (!usedNumbers.Contains(i))
                                {
                                    totalRemaining++;
                                    if (availableLeaves.Count < 20)
                                    {
                                        availableLeaves.Add(new
                                        {
                                            BookId = book.Id,
                                            LeafNumber = i,
                                            Display = $"#{i} (Book {book.Id})"
                                        });
                                    }
                                }
                            }
                        }

                        return new { Leaves = availableLeaves, Remaining = totalRemaining };
                    }
                },
                "Refresh"
            );

            app.AddAsyncEndpointWithBearerAuth<CreateChequeBookRequest>(
                "CreateChequeBook",
                async (DataIn, LoginInfo) =>
                {
                    var req = (CreateChequeBookRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        var acc = await ctx.AccountsInformations.FirstOrDefaultAsync(a => a.AccountNo == req.AccountId);
                        if (acc == null || !acc.IsBank) throw new ArgumentException("Selected account is not a Bank Account.");

                        long count = req.EndNumber - req.StartNumber + 1;

                        var book = new ChequeBook
                        {
                            AccountId = req.AccountId,
                            StartNumber = req.StartNumber,
                            EndNumber = req.EndNumber,
                            NextNumber = req.StartNumber,
                            IsOpen = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        ctx.ChequeBooks.Add(book);
                        await ctx.SaveChangesAsync();

                        return new { BookId = book.Id, LeafCount = count, Warning = count > 200 ? "Warning: Book is large (>200 leaves)." : "" };
                    }
                },
                "Refresh"
            );


            return app;
        }
    }
}
