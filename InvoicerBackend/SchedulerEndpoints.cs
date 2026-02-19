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

                        // 1. History (Last 50 Journal Entries)
                        var history = await ctx.AccountsJournalEntries
                            .Where(j => j.CreditAccountNo == req.AccountNo || j.DebitAccountNo == req.AccountNo)
                            .OrderByDescending(j => j.TimeTai)
                            .Take(50)
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
                                IsAutomatic = false, // Journal entries are posted acts
                                ReferenceId = h.JournalUnivSeq
                            });
                        }

                        // 2. Scheduled Payments (Pending)
                        var payments = await ctx.ScheduledPayments
                            .Where(p => p.IsPending && (p.DebitAccountId == req.AccountNo || p.CreditAccountId == req.AccountNo))
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

                        // 3. Scheduled Receipts (Pending)
                        var receipts = await ctx.ScheduledReceipts
                            .Where(r => r.IsPending && (r.DebitAccountId == req.AccountNo || r.CreditAccountId == req.AccountNo))
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

                        // Determine Status for Scheduled
                        foreach (var item in scheduled)
                        {
                            var runDate = DateOnly.FromDateTime(item.Date);
                            if (runDate < today) item.Status = "Overdue";
                            else if (runDate == today) item.Status = "Due Today";
                            else item.Status = "Scheduled";
                        }

                        // Combine and Sort (All Ascending for Timeline view)
                        results.AddRange(scheduled);

                        return results.OrderBy(x => x.Date).ToList();
                    }
                },
                "Refresh"
            );

            return app;
        }
    }
}
