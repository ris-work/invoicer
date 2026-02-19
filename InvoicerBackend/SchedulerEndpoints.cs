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
            app.AddAsyncEndpointWithBearerAuth<AddScheduledEntryRequest>(
                "AddScheduledEntryWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (AddScheduledEntryRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
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
                                PaymentMethod = req.PaymentMethod ?? "Manual", // Fix for NULL constraint
                                JournalNo = req.JournalNo
                            };
                            ctx.ScheduledPayments.Add(entry);
                        }
                        else
                        {
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
                                PaymentMethod = req.PaymentMethod ?? "Manual", // Fix for NULL constraint
                                JournalNo = req.JournalNo
                            };
                            ctx.ScheduledReceipts.Add(entry);
                        }

                        await ctx.SaveChangesAsync();
                        return true;
                    }
                },
                "Refresh"
            );

            return app;
        }
    }
}
