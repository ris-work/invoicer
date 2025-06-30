using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;

namespace InvoicerBackend
{
    public static class JournalEntries
    {
        public static void AddJournalEntry(NewinvContext ctx, AccountsJournalEntry AccJE)
        {
            ctx.AccountsJournalEntries.Add(
                /*new AccountsJournalEntry
                {
                    Amount = AccJE.Amount,
                    CreditAccountType = AccJE.CreditAccountType,
                    CreditAccountNo = AccJE.CreditAccountNo,
                    DebitAccountType = AccJE.DebitAccountType,
                    DebitAccountNo = AccJE.DebitAccountNo,
                    Description = AccJE.Description,
                    TimeAsEntered = AccJE.TimeAsEntered,
                }*/
                AccJE
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

        public static async void ReverseJournalEntry(long JournalEntryId)
        {
            using (var ctx = new NewinvContext())
            {
                var tx = await ctx.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable
                );
                AccountsJournalEntry JE;
                JE = ctx
                    .AccountsJournalEntries.Where(e => e.JournalUnivSeq == JournalEntryId)
                    .Single();
                AddJournalEntry(
                    ctx,
                    new AccountsJournalEntry()
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
                await tx.CommitAsync();
            }
        }
    }
}
