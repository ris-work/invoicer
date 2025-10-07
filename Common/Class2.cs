using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RV.InvNew.Common;

namespace common
{
    [JsonSerializable(typeof(BackOfficeAccountingDataTransfer))]
    [JsonSerializable(typeof(List<AccountsInformation>))]
    [JsonSerializable(typeof(List<AccountsType>))]
    [JsonSerializable(typeof(List<ReceivedInvoice>))]
    [JsonSerializable(typeof(List<IssuedInvoice>))]
    [JsonSerializable(typeof(List<ScheduledReceipt>))]
    [JsonSerializable(typeof(List<ScheduledPayment>))]
    [JsonSerializable(typeof(List<ChequeBook>))]
    [JsonSerializable(typeof(List<AccountsJournalEntry>))]
    [JsonSerializable(typeof(List<AccountsJournalInformation>))]
    [JsonSerializable(typeof(List<User>))]
    [JsonSerializable(typeof(List<Inventory>))]
    [JsonSerializable(typeof(List<Catalogue>))]
    [JsonSerializable(typeof(List<VatCategory>))]
    [JsonSourceGenerationOptions(IncludeFields = true)]
    public class BackOfficeAccountingDataTransfer
    {
        [JsonInclude]
        public List<AccountsInformation> AccInfo;

        [JsonInclude]
        public List<AccountsType> AccTypes;

        [JsonInclude]
        public List<ReceivedInvoice> RInv;

        [JsonInclude]
        public List<IssuedInvoice> IInv;

        [JsonInclude]
        public List<ScheduledReceipt> RSchd;

        [JsonInclude]
        public List<ScheduledPayment> PSchd;

        [JsonInclude]
        public List<ChequeBook> ChequeBooks;

        [JsonInclude]
        public List<AccountsJournalEntry> AccJE;

        [JsonInclude]
        public List<AccountsJournalInformation> AccJI;
        [JsonInclude]
        public List<User> UserAccounts;
        [JsonInclude]
        public List<Catalogue> Cat;
        [JsonInclude]
        public List<Inventory> Inv;
        [JsonInclude]
        public List<VatCategory> VCat;
    }
}
