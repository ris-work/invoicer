using System.Data;
using InvoicerBackend;
using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{

    // --- Live View Endpoints ---

    // 1. Search Journal Entries
    public class JournalSearchRequest
    {
        public int? JournalNo { get; set; }
        public long? AccountNo { get; set; } // Filter by Credit OR Debit
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
    public class AddJournalEntryRequest
    {
        public int JournalNo { get; set; }
        public long DebitAccountNo { get; set; }
        public long CreditAccountNo { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }
        public string RefNo { get; set; }
    }


    public static class JournalEndpoints
    {
        public static WebApplication AddJournalEndpoints(this WebApplication app)
        {
            Func<int, Task<int>> AddJournalEntry = async (a) =>
            {
                return 0;
            };
            app.AddAsyncEndpointWithBearerAuth<AccountsJournalEntry>(
                "AddJournalEntry",
                async (AS, LoginInfo) =>
                {
                    var Entry = (AccountsJournalEntry)AS;
                    System.Console.WriteLine(
                        $"AddJournalEntry: {LoginInfo.UserId}, {LoginInfo.Principal}"
                    );
                    Entry.PrincipalId = (long)LoginInfo.UserId;
                    Entry.PrincipalName = LoginInfo.Principal;

                    using (var ctx = new NewinvContext())
                    {
                        var tx = await ctx.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable
                        );
                        Entry.CreditAccountName = ctx
                            .AccountsInformations.Where(e =>
                                e.AccountType == Entry.CreditAccountType
                                && e.AccountNo == Entry.CreditAccountNo
                            )
                            .Single()
                            .AccountName;
                        Entry.DebitAccountName = ctx
                            .AccountsInformations.Where(e =>
                                e.AccountType == Entry.DebitAccountType
                                && e.AccountNo == Entry.DebitAccountNo
                            )
                            .Single()
                            .AccountName;
                        JournalEntries.AddJournalEntry(ctx, Entry);
                        ctx.SaveChanges();
                        await tx.CommitAsync();
                    }
                    return 0;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<long>(
                "ReverseJournalEntry",
                (AS, LoginInfo) =>
                {
                    JournalEntries.ReverseJournalEntry((long)AS);
                    return 0;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
                "GetAllJournalEntries",
                (AS, LoginInfo) =>
                {
                    List<AccountsJournalEntry> AccJEList;
                    using (var ctx = new NewinvContext())
                    {
                        AccJEList = ctx.AccountsJournalEntries.ToList();
                    }
                    return AccJEList;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
    "GetNJournalEntries",
    (AS, LoginInfo) =>
    {
        List<AccountsJournalEntry> AccJEList;
        using (var ctx = new NewinvContext())
        {
            AccJEList = ctx.AccountsJournalEntries.OrderByDescending(e => e.TimeTai).Take(100).ToList();
        }
        return AccJEList;
    },
    "Refresh"
    );
            app.AddEndpointWithBearerAuth<TimePeriod>(
                "GetAllJournalEntriesWithinTimePeriod",
                (AS, LoginInfo) =>
                {
                    var TP = (TimePeriod)AS;
                    List<AccountsJournalEntry> AccJEList;
                    using (var ctx = new NewinvContext())
                    {
                        AccJEList = ctx
                            .AccountsJournalEntries.Where(e =>
                                e.TimeTai >= TP.From.Value.ToUniversalTime() && e.TimeTai <= TP.To.Value.ToUniversalTime()
                            )
                            .ToList();
                    }
                    System.Console.WriteLine($"From, To: {TP.From}, {TP.To}");
                    return AccJEList;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
                "GetAccountsInformation",
                (AS, LoginInfo) =>
                {
                    List<AccountsInformation> AI;
                    using (var ctx = new NewinvContext())
                    {
                        AI = ctx.AccountsInformations.ToList();
                    }
                    return AI;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
                "GetAccountsTypes",
                (AS, LoginInfo) =>
                {
                    List<AccountsType> AI;
                    using (var ctx = new NewinvContext())
                    {
                        AI = ctx.AccountsTypes.ToList();
                    }
                    return AI;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
                "GetAccountsBalances",
                (AS, LoginInfo) =>
                {
                    List<AccountsBalance> AB;
                    using (var ctx = new NewinvContext())
                    {
                        AB = ctx.AccountsBalances.ToList();
                    }
                    return AB;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
                "GetAccountsJournalInformation",
                (AS, LoginInfo) =>
                {
                    List<AccountsJournalInformation> AB;
                    using (var ctx = new NewinvContext())
                    {
                        AB = ctx.AccountsJournalInformations.ToList();
                    }
                    return AB;
                },
                "Refresh"
            );
            app.AddEndpointWithBearerAuth<string>(
                "GetAccountBalances",
                (AS, LoginInfo) =>
                {
                    List<AccountsBalance> AB;
                    using (var ctx = new NewinvContext())
                    {
                        AB = ctx.AccountsBalances.ToList();
                    }
                    return AB;
                },
                "Refresh"
            );
            app.AddAsyncEndpointWithBearerAuth<JournalSearchRequest>(
    "SearchJournalEntriesWeb",
    async (DataIn, LoginInfo) =>
    {
        var req = (JournalSearchRequest)DataIn;
        using (var ctx = new NewinvContext())
        {
            var query = ctx.AccountsJournalEntries.AsQueryable();

            if (req.JournalNo.HasValue)
                query = query.Where(e => e.JournalNo == req.JournalNo.Value);

            if (req.AccountNo.HasValue)
                query = query.Where(e => e.CreditAccountNo == req.AccountNo.Value || e.DebitAccountNo == req.AccountNo.Value);

            if (req.From.HasValue)
                query = query.Where(e => e.TimeTai >= req.From.Value.ToUniversalTime());

            if (req.To.HasValue)
                query = query.Where(e => e.TimeTai <= req.To.Value.ToUniversalTime());

            return await query.OrderByDescending(e => e.TimeTai).Take(100).ToListAsync();
        }
    },
    "Refresh"
);
            app.AddAsyncEndpointWithBearerAuth<AddJournalEntryRequest>(
                "AddJournalEntryWeb",
                async (DataIn, LoginInfo) =>
                {
                    var req = (AddJournalEntryRequest)DataIn;
                    using (var ctx = new NewinvContext())
                    {
                        // Validate Accounts exist
                        var debitAcc = await ctx.AccountsInformations.FindAsync(req.DebitAccountNo);
                        var creditAcc = await ctx.AccountsInformations.FindAsync(req.CreditAccountNo);

                        if (debitAcc == null || creditAcc == null) throw new ArgumentException("Invalid Account Number(s).");

                        var entry = new AccountsJournalEntry
                        {
                            JournalNo = req.JournalNo,
                            DebitAccountNo = req.DebitAccountNo,
                            DebitAccountType = debitAcc.AccountType,
                            DebitAccountName = debitAcc.AccountName,
                            CreditAccountNo = req.CreditAccountNo,
                            CreditAccountType = creditAcc.AccountType,
                            CreditAccountName = creditAcc.AccountName,
                            Amount = req.Amount,
                            Description = req.Description,
                            RefNo = req.RefNo,
                            TimeAsEntered = DateTime.UtcNow,
                            TimeTai = DateTime.UtcNow,
                            PrincipalId = (long)LoginInfo.UserId,
                            PrincipalName = LoginInfo.Principal
                        };

                        JournalEntries.AddJournalEntry(ctx, entry);
                        await ctx.SaveChangesAsync();
                        return entry;
                    }
                },
                "Refresh"
            );

            return app;
        }
    }
}
