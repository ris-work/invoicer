using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class JournalEntries
    {
        public static void AddJournalEntry(NewinvContext ctx, JournalEntry AccJE)
        {
            ctx.AccountsJournalEntries.Add(
                new AccountsJournalEntry
                {
                    Amount = AccJE.Amount,
                    CreditAccountType = AccJE.CreditAccountType,
                    CreditAccountNo = AccJE.CreditAccountNo,
                    DebitAccountType = AccJE.DebitAccountType,
                    DebitAccountNo = AccJE.DebitAccountNo,
                    Description = AccJE.Description,
                    TimeAsEntered = AccJE.TimeAsEntered,
                }
            );
            ctx
                .AccountsBalances.Where(a =>
                    a.AccountType == AccJE.CreditAccountType && a.AccountNo == AccJE.CreditAccountNo
                )
                .First()
                .Amount -= AccJE.Amount;
            ctx
                .AccountsBalances.Where(a =>
                    a.AccountType == AccJE.DebitAccountType && a.AccountNo == AccJE.DebitAccountNo
                )
                .First()
                .Amount += AccJE.Amount;
        }

        public static void ReverseJournalEntry() { }
    }
}
