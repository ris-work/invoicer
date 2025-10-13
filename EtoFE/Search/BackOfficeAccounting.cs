using CommonUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using RV.InvNew.Common;

namespace EtoFE.Search
{
    public static class BackOfficeAccounting
    {
        // Existing search methods
        public static string[]? SearchAccounts(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.AccInfo, Owner, false, null, ["AccountNo"]);
        public static string[]? SearchJournals(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.AccJI, Owner, false, null, ["JournalId"]);
        public static string[]? SearchAccountTypes(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.AccTypes, Owner, false, null, ["AccountType"]);
        public static string[]? SearchReceivedInvoices(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.RInv, Owner, false, null, ["ReceivedInvoiceNo"]);
        public static string[]? SearchIssuedInvoices(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.IInv, Owner, false, null, ["InvoiceId"]);
        public static string[]? SearchScheduledPayment(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.PSchd, Owner, false, null, ["Id"]);
        public static string[]? SearchScheduledReceipt(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.RSchd, Owner, false, null, ["Id"]);
        public static string[]? SearchItems(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.Cat, Owner, false, null, ["Itemcode"]);
        public static string[]? SearchBatches(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.Inv, Owner, false, null, ["Itemcode", "Batchcode"]);
        public static string[]? SearchBatches(Control Owner, long Itemcode) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.Inv.Where(e => e.Itemcode == Itemcode).ToList(), Owner, false, null, ["Itemcode", "Batchcode"]);
        public static string[]? SearchVatCategories(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.VCat, Owner, false, null, ["VatCategoryId"]);
        public static string[]? SearchCustomers(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.PersonalInfo, Owner, false, null, ["Id"]);
        public static string[]? SearchUsers(Control Owner) => SearchPanelUtility.GenerateSearchDialog(GlobalState.BAT.UserAccounts, Owner, false, null, ["Id"]);

        // New search methods with enhanced filtering options
        public static string[]? SearchAccountsByType(Control Owner, long accountType)
        {
            var filteredAccounts = GlobalState.BAT.AccInfo.Where(a => a.AccountType == accountType).ToList();
            return SearchPanelUtility.GenerateSearchDialog(filteredAccounts, Owner, false, null, ["AccountNo"]);
        }

        public static string[]? SearchAccountsByName(Control Owner, string namePattern)
        {
            var filteredAccounts = GlobalState.BAT.AccInfo.Where(a =>
                a.AccountName.Contains(namePattern, StringComparison.OrdinalIgnoreCase)).ToList();
            return SearchPanelUtility.GenerateSearchDialog(filteredAccounts, Owner, false, null, ["AccountNo"]);
        }

        public static string[]? SearchItemsByDescription(Control Owner, string descriptionPattern)
        {
            var filteredItems = GlobalState.BAT.Cat.Where(c =>
                c.Description.Contains(descriptionPattern, StringComparison.OrdinalIgnoreCase)).ToList();
            return SearchPanelUtility.GenerateSearchDialog(filteredItems, Owner, false, null, ["Itemcode"]);
        }

        public static string[]? SearchBatchesByExpiry(Control Owner, DateTime beforeDate)
        {
            var beforeDateOnly = DateOnly.FromDateTime(beforeDate);
            var filteredBatches = GlobalState.BAT.Inv.Where(b =>
                b.ExpDate.HasValue && DateOnly.FromDateTime(b.ExpDate.Value) < beforeDateOnly).ToList();
            return SearchPanelUtility.GenerateSearchDialog(filteredBatches, Owner, false, null, ["Itemcode", "Batchcode"]);
        }

        public static string[]? SearchInvoicesByDateRange(Control Owner, DateTime startDate, DateTime endDate, bool isReceived = true)
        {
            if (isReceived)
            {
                var filteredInvoices = GlobalState.BAT.RInv.Where(i =>
                    i.CreatedAt.Date >= startDate.Date && i.CreatedAt.Date <= endDate.Date).ToList();
                return SearchPanelUtility.GenerateSearchDialog(filteredInvoices, Owner, false, null, ["ReceivedInvoiceNo"]);
            }
            else
            {
                var filteredInvoices = GlobalState.BAT.IInv.Where(i =>
                    i.CreatedAt.Date >= startDate.Date && i.CreatedAt.Date <= endDate.Date).ToList();
                return SearchPanelUtility.GenerateSearchDialog(filteredInvoices, Owner, false, null, ["InvoiceId"]);
            }
        }

        public static string[]? SearchInvoicesByAccount(Control Owner, long accountNo, bool isReceived = true)
        {
            if (isReceived)
            {
                var filteredInvoices = GlobalState.BAT.RInv.Where(i => i.SupplierId == accountNo).ToList();
                return SearchPanelUtility.GenerateSearchDialog(filteredInvoices, Owner, false, null, ["ReceivedInvoiceNo"]);
            }
            else
            {
                var filteredInvoices = GlobalState.BAT.IInv.Where(i => i.Customer == accountNo).ToList();
                return SearchPanelUtility.GenerateSearchDialog(filteredInvoices, Owner, false, null, ["InvoiceId"]);
            }
        }

        // Advanced search with multiple criteria
        public static string[]? AdvancedSearchItems(Control Owner, string? descriptionPattern = null, bool? active = null, bool? webActive = null)
        {
            var query = GlobalState.BAT.Cat.AsQueryable();

            if (!string.IsNullOrEmpty(descriptionPattern))
                query = query.Where(c =>
                    c.Description.Contains(descriptionPattern, StringComparison.OrdinalIgnoreCase));

            if (active.HasValue)
                query = query.Where(c => c.Active == active.Value);

            if (webActive.HasValue)
                query = query.Where(c => c.ActiveWeb == webActive.Value);

            var filteredItems = query.ToList();
            return SearchPanelUtility.GenerateSearchDialog(filteredItems, Owner, false, null, ["Itemcode"]);
        }

        public static string[]? AdvancedSearchBatches(Control Owner, long? itemcode = null,
            DateTime? expiryBefore = null, DateTime? mfgAfter = null)
        {
            var query = GlobalState.BAT.Inv.AsQueryable();

            if (itemcode.HasValue)
                query = query.Where(b => b.Itemcode == itemcode.Value);

            if (expiryBefore.HasValue)
            {
                var beforeDateOnly = DateOnly.FromDateTime(expiryBefore.Value);
                query = query.Where(b => b.ExpDate.HasValue && DateOnly.FromDateTime(b.ExpDate.Value) < beforeDateOnly);
            }

            var filteredBatches = query.ToList();
            return SearchPanelUtility.GenerateSearchDialog(filteredBatches, Owner, false, null, ["Itemcode", "Batchcode"]);
        }

        // Existing lookup methods
        public static string? LookupAccountType(long id) => GlobalState.BAT.AccTypes.Where(e => e.AccountType == id).Select(e => e.AccountTypeName).FirstOrDefault("Unknown Type");
        public static string? LookupAccount(long id) => GlobalState.BAT.AccInfo.Where(a => a.AccountNo == id).Select(a => a.AccountName).FirstOrDefault("Unknown Account");
        public static string? LookupJournalNo(long id) => GlobalState.BAT.AccJI.Where(j => j.JournalId == id).Select(j => j.JournalName).FirstOrDefault("Unknown Journal");
        public static string? LookupUser(long id) => GlobalState.BAT.UserAccounts.Where(u => u.Userid == id).Select(u => u.Name).FirstOrDefault("Unknown User");
        public static long LookupAccountByName(string CriticalAccountName) => GlobalState.BAT.AccInfo.Where(e => e.AccountName.ToLowerInvariant() == CriticalAccountName.ToLowerInvariant()).Select(i => i.AccountNo).First();

        // New lookup methods
        public static string? LookupItem(long id) => GlobalState.BAT.Cat.Where(c => c.Itemcode == id).Select(c => c.Description).FirstOrDefault("Unknown Item");
        public static string? LookupBatch(long itemcode, long batchcode) =>
            GlobalState.BAT.Inv.Where(b => b.Itemcode == itemcode && b.Batchcode == batchcode)
                .Select(b => $"Batch {b.Batchcode} - {b.Units} units")
                .FirstOrDefault("Unknown Batch");
        public static string? LookupVatCategory(long id) => GlobalState.BAT.VCat.Where(v => v.VatCategoryId == id)
            .Select(v => v.VatName).FirstOrDefault("Unknown VAT Category");
        public static string? LookupCustomerName(long id) => GlobalState.BAT.PersonalInfo.Where(v => v.Id == id)
            .Select(v => v.Name).FirstOrDefault("Unknown Customer");
        public static string? LookupCustomerImage(long id) => GlobalState.BAT.PersonalImages.Where(v => v.PiiId == id)
            .Select(v => v.Image).FirstOrDefault(string.Empty);
    }
}