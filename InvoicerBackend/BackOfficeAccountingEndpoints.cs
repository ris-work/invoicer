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
            return app;
        }
    }
}
