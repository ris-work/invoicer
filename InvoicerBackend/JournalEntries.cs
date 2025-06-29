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

        public static void ReverseJournalEntry(long JournalEntryId)
        {
            using (var ctx = new NewinvContext())
            {
                AccountsJournalEntry JE;
                JE = ctx
                    .AccountsJournalEntries.Where(e => e.JournalUnivSeq == JournalEntryId)
                    .Single();
                AddJournalEntry(
                    ctx,
                    new JournalEntry()
                    {
                        Amount = -JE.Amount,
                        CreditAccountType = JE.CreditAccountType,
                        CreditAccountNo = JE.CreditAccountNo,
                        DebitAccountType = JE.DebitAccountType,
                        DebitAccountNo = JE.DebitAccountNo,
                        Description = JE.Description,
                        TimeAsEntered = JE.TimeAsEntered,
                    }
                );
                ctx.SaveChanges();
            }
        }
    }
}
