using RV.InvNew.Common;
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
            app.AddEndpointWithBearerAuth<string>("AutoProcessScheduledPayments", (AS, LoginInfo) =>
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var tomorrow = today.AddDays(1);
                using (var ctx = new NewinvContext())
                {
                    var ToProcessList = ctx.ScheduledPayments.Where(e => e.IsPending == true &&
                        e.NextRunDate >= today &&
                        e.NextRunDate < tomorrow
                    ).ToList();
                    foreach (var entry in ToProcessList) { 
                    JournalEntries.AddJournalEntry(ctx, new AccountsJournalEntry { });
                        }
                }
                return 0;
            }, "Refresh");
            return app;
        }
    }
}
