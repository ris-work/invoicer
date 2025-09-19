using Microsoft.EntityFrameworkCore;
using RV.InvNew.Common;
using System.Data;
using System.Text.Json;

namespace InvoicerBackend
{
    public static class BackOfficeAccountingEndpoints
    {
        public static WebApplication AddBackOfficeAccountingEndpoints(this WebApplication app) {
            app.AddEndpointWithBearerAuth<string>("BackOfficeAccountingRefresh", (AS, LoginInfo) => {
                common.BackOfficeAccountingDataTransfer BAT = new();
                using (var ctx = new NewinvContext())
                {
                    BAT.AccInfo = ctx.AccountsInformations.ToList();
                    BAT.RInv = ctx.ReceivedInvoices.ToList();
                    BAT.IInv = ctx.IssuedInvoices.ToList();
                    BAT.AccTypes = ctx.AccountsTypes.ToList();
                    BAT.AccJE = ctx.AccountsJournalEntries.ToList();
                    BAT.PSchd = ctx.ScheduledPayments.ToList();
                    BAT.RSchd = ctx.ScheduledReceipts.ToList();
                }
                Console.WriteLine($"===== {JsonSerializer.Serialize(BAT)}");
                return BAT;
            }, "Refresh");
            app.AddAsyncEndpointWithBearerAuth<string>("AutoProcessScheduledPayments", async (AS, LoginInfo) =>
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var tomorrow = today.AddDays(1);
                List<ScheduledPayment> Schd;
                using (var ctx = new NewinvContext())
                {
                    using var tx = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    var ToProcessList = ctx.ScheduledPayments.Where(e => e.IsPending == true &&
                        e.NextRunDate >= today &&
                        e.NextRunDate < tomorrow
                    ).ToList();
                    Schd = ToProcessList;
                    foreach (var entry in ToProcessList) { 
                        JournalEntries.AddJournalEntry(ctx, new AccountsJournalEntry { CreditAccountNo = entry.CreditAccountId, DebitAccountNo = entry.DebitAccountId, Amount = entry.Amount, DebitAccountType = (int)entry.DebitAccountType, CreditAccountType = (int)entry.CreditAccountType, JournalNo = entry.JournalNo, InternalReference = $"scheduledpayment:{entry.Id}" });
                        entry.IsPending = false;
                    }
                    ctx.SaveChanges();
                    await tx.CommitAsync();
                }
                return Schd;
            }, "Refresh");
            app.AddAsyncEndpointWithBearerAuth<string>("AutoProcessScheduledReceipt", async (AS, LoginInfo) =>
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var tomorrow = today.AddDays(1);
                List<ScheduledReceipt> Schd;
                using (var ctx = new NewinvContext())
                {
                    using var tx = await ctx.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    var ToProcessList = ctx.ScheduledReceipts.Where(e => e.IsPending == true &&
                        e.NextRunDate >= today &&
                        e.NextRunDate < tomorrow
                    ).ToList();
                    Schd = ToProcessList;
                    foreach (var entry in ToProcessList)
                    {
                        JournalEntries.AddJournalEntry(ctx, new AccountsJournalEntry { CreditAccountNo = entry.CreditAccountId, DebitAccountNo = entry.DebitAccountId, Amount = entry.Amount, DebitAccountType = (int)entry.DebitAccountType, CreditAccountType = (int)entry.CreditAccountType, JournalNo = entry.JournalNo, InternalReference = $"scheduledreceipt:{entry.Id}" });
                        entry.IsPending = false;
                    }
                    ctx.SaveChanges();
                    await tx.CommitAsync();
                }
                return Schd;
            }, "Refresh");
            return app;
        }
    }
}
