using InvoicerBackend;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class JournalEndpoints
    {
        public static WebApplication AddJournalEndpoints(this WebApplication app)
        {
            Func<int, Task<int>> AddJournalEntry = async (a) =>
            {
                return 0;
            };
            app.AddEndpointWithBearerAuth<AccountsJournalEntry>(
                "AddJournalEntry",
                (AS, LoginInfo) =>
                {
                    var Entry = (AccountsJournalEntry)AS;
                    System.Console.WriteLine(
                        $"AddJournalEntry: {LoginInfo.UserId}, {LoginInfo.Principal}"
                    );
                    Entry.PrincipalId = (long)LoginInfo.UserId;
                    Entry.PrincipalName = LoginInfo.Principal;

                    using (var ctx = new NewinvContext())
                    {
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
                                e.TimeTai >= TP.From && e.TimeTai <= TP.To
                            )
                            .ToList();
                    }
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
            return app;
        }
    }
}
