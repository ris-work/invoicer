using CommonUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;

namespace EtoFE.Search
{
    public static class BackOfficeAccounting
    {
        public static string[] SearchAccounts(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.AccInfo, Owner, false, null, ["AccountNo"]);
        public static string[] SearchJournals(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.AccJI, Owner, false, null, ["JournalId"]);
        public static string[] SearchAccountTypes(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.AccTypes, Owner, false, null, ["AccountType"]);
        public static string[] SearchReceivedInvoices(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.RInv, Owner, false, null, ["ReceivedInvoiceNo"]);
        public static string[] SearchIssuedInvoices(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.IInv, Owner, false, null, ["InvoiceId"]);
        public static string[] SearchScheduledPayment(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.PSchd, Owner, false, null, ["Id"]);
        public static string[] SearchScheduledReceipt(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.RSchd, Owner, false, null, ["Id"]);
        public static string LookupAccountType(long id) => GlobalState.BAT.AccTypes.Where(e => e.AccountType == id).Select(e => e.AccountTypeName).FirstOrDefault("Unknown Type");
        public static string LookupAccount(long id) => GlobalState.BAT.AccInfo.Where(a => a.AccountNo == id).Select(a => a.AccountName).FirstOrDefault("Unknown Account");
        public static string LookupJournalNo(long id) => GlobalState.BAT.AccJI.Where(j => j.JournalId == id).Select(j => j.JournalName).FirstOrDefault("Unknown Journal");
    }
}
