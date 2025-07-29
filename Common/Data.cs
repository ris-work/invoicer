using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RV.InvNew.Common
{
    [JsonSerializable(typeof(PosRefresh))]
    [JsonSerializable(typeof(List<PosBatch>))]
    [JsonSerializable(typeof(List<PosCatalogue>))]
    [JsonSerializable(typeof(List<VatCategory>))]
    [JsonSourceGenerationOptions(IncludeFields = true)]
    //[JsonSourceGenerationOptions(WriteIndented =true, IncludeFields =true)]
    public class PosRefresh
    {
        [JsonInclude]
        public List<PosCatalogue> Catalogue;

        [JsonInclude]
        public List<PosBatch> Batches;

        [JsonInclude]
        public List<VatCategory> VatCategories;
    }

    [JsonSerializable(typeof(PosCatalogue))]
    [JsonSourceGenerationOptions(IncludeFields = true)]
    //[JsonSourceGenerationOptions(WriteIndented =true, IncludeFields =true)]
    public class PosCatalogue
    {
        [JsonInclude]
        public long itemcode;

        [JsonInclude]
        public string itemdesc;

        [JsonInclude]
        public bool VatCategoryAdjustable;

        [JsonInclude]
        public bool VatDependsOnUser;

        [JsonInclude]
        public bool ManualPrice;

        [JsonInclude]
        public bool EnforceAboveCost;

        [JsonInclude]
        public long DefaultVatCategory;

        public string[] ToStringArray()
        {
            var r = new Random();
            return new string[]
            {
                itemcode.ToString(),
                itemdesc,
                DefaultVatCategory.ToString(),
                //itemdesc.Split(" ")[0],
                //itemdesc.Split(" ")[1],
                //itemdesc.Split(" ")[2],
                //r.NextInt64().ToString(),
            };
        }
    }

    [JsonSerializable(typeof(PosBatch))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public class PosBatch
    {
        [JsonInclude]
        public long itemcode;

        [JsonInclude]
        public long batchcode;

        [JsonInclude]
        public double selling;

        [JsonInclude]
        public double marked;

        [JsonInclude]
        public DateOnly expireson;

        [JsonInclude]
        public double SIH;
    }

    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public class PosSaleEntry
    {
        [JsonInclude]
        public string state;

        [JsonInclude]
        public long itemcode;

        [JsonInclude]
        public long batchcode;

        [JsonInclude]
        public double uselling;

        [JsonInclude]
        public double umarked;

        [JsonInclude]
        public double quantity;

        [JsonInclude]
        public long VatCategory;

        [JsonInclude]
        public double VatPercent;

        [JsonInclude]
        public double VatAmount;

        [JsonInclude]
        public double Total;

        public (bool, string) IsValid(
            List<PosCatalogue> PC,
            List<PosBatch> PB,
            List<VatCategory> VC
        )
        {
            if (
                PB.Where(e => e.itemcode == itemcode && e.batchcode == batchcode).First().SIH
                < quantity
            )
                return (false, "Qty > SIH");
            return (true, "Is valid");
        }
    }

    public class PosSaleEntryCollection
    {
        [JsonInclude]
        public List<PosSaleEntry> entries;

        public (bool, string) IsValid(
            List<PosCatalogue> PC,
            List<PosBatch> PB,
            List<VatCategory> VC
        )
        {
            bool AggBool = false;
            foreach (PosSaleEntry PE in entries)
            {
                (bool, string) EntryValidity = PE.IsValid(PC, PB, VC);
                if (EntryValidity.Item1 == false)
                    return EntryValidity;
                AggBool = AggBool && EntryValidity.Item1;
            }
            return (AggBool, "Validation successful!");
        }
    }

    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public class Account
    {
        public int AccountType;
        public int? AccountNo;
        public string AccountName;
    }

    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public class JournalEntry
    {
        public int JournalNo { get; set; }
        public string? RefNo { get; set; }
        public double Amount { get; set; }
        public int DebitAccountType { get; set; }
        public long DebitAccountNo { get; set; }
        public int CreditAccountType { get; set; }
        public long CreditAccountNo { get; set; }
        public string? Description { get; set; }
        public DateTime TimeAsEntered { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public class SingleValueString
    {
        public string response { get; set; }
    }

    [JsonSerializable(typeof(List<PosSaleEntry>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class PosSaleEntryCollectionSerialize : JsonSerializerContext { }

    [JsonSerializable(typeof(SingleValueString))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class SingleValueStringContext : JsonSerializerContext { }

    [JsonSerializable(typeof(List<PosBatch>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class PosBatchSerialize : JsonSerializerContext { }

    [JsonSerializable(typeof(List<PosCatalogue>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class PosCatalogueSerialize : JsonSerializerContext { }

    [JsonSerializable(typeof(List<VatCategory>))]
    [JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true)]
    public partial class VatCategoriesSerialize : JsonSerializerContext { }

    [JsonSerializable(typeof(List<NotificationTransfer>))]
    public class NotificationTransfer
    {
        public long NotifId { get; set; }

        public string NotifTarget { get; set; }
        public DateTime TimeTai { get; set; }

        public DateTime? TimeExpiresTai { get; set; }

        public string NotifContents { get; set; } = null!;

        public int? NotifPriority { get; set; }
    }

    [JsonSerializable(typeof(List<CatalogueTransfer>))]
    public partial class CatalogueTransfer
    {
        public long Itemcode { get; set; }

        public string Description { get; set; } = null!;

        public bool Active { get; set; }

        public DateTime CreatedOn { get; set; }

        public string DescriptionPos { get; set; } = null!;

        public string DescriptionWeb { get; set; } = null!;

        public long? DescriptionsOtherLanguages { get; set; }

        public long DefaultVatCategory { get; set; }

        public bool VatDependsOnUser { get; set; }

        public bool VatCategoryAdjustable { get; set; }

        public bool PriceManual { get; set; }

        public bool EnforceAboveCost { get; set; }

        public bool ActiveWeb { get; set; }

        public bool ExpiryTrackingEnabled { get; set; }

        public long PermissionsCategory { get; set; }

        //public long CategoriesBitmask { get; set; }
        public Dictionary<string, (string, object, string?)> ToDict()
        {
            return new Dictionary<string, (string, object, string?)>()
            {
                { "Itemcode", ("Item code", this.Itemcode, null) },
                { "Description", ("Description", this.Description, null) },
                { "Active", ("Active", this.Active, null) },
                { "DecriptionPOS", ("Description POS", this.DescriptionPos, null) },
                { "DecriptionWeb", ("Description Web", this.DescriptionWeb, null) },
                {
                    "DescriptionOtherLanguages",
                    (
                        "Description (Other Languages)",
                        this.DescriptionsOtherLanguages,
                        "LangChooser"
                    )
                },
                {
                    "DefaultVatCategory",
                    ("Default VAT/GST Category", this.DefaultVatCategory, "VatChooser")
                },
                { "VatDependsOnUser", ("VAT/GST is user-dependent", this.VatDependsOnUser, null) },
                {
                    "VatCategoryAdjustable",
                    ("VAT/GST category is adjustable", this.VatCategoryAdjustable, null)
                },
                { "ManualPrice", ("Allow Manual Price", this.PriceManual, null) },
                { "EnforceAboveCost", ("Enforce Above Cost", this.EnforceAboveCost, null) },
                { "ActiveWeb", ("Active (Web)", this.ActiveWeb, null) },
            };
        }

        public static CatalogueTransfer FromDictionary(Dictionary<string, (string, object)> DIn)
        {
            return new CatalogueTransfer
            {
                Itemcode = (long)DIn["Itemcode"].Item2,
                Description = (string)DIn["Description"].Item2,
                Active = (bool)DIn["Active"].Item2,
                DescriptionPos = (string)DIn["DescriptionPOS"].Item2,
                DescriptionWeb = (string)DIn["DescriptionWeb"].Item2,
                DescriptionsOtherLanguages = (long)DIn["DescriptionOtherLanguages"].Item2,
                DefaultVatCategory = (long)DIn["DefaultVatCategory"].Item2,
                VatDependsOnUser = (bool)DIn["VatDependsOnUser"].Item2,
                VatCategoryAdjustable = (bool)DIn["VatCategoryAdjustable"].Item2,
                PriceManual = (bool)DIn["ManualPrice"].Item2,
                EnforceAboveCost = (bool)DIn["EnforceAboveCost"].Item2,
                ActiveWeb = (bool)DIn["ActiveWeb"].Item2,
            };
        }
    }

    public class JournalEntryDto
    {
        public long JournalNo { get; set; }
        public string RefNo { get; set; }
        public decimal Amount { get; set; }
        public int DebitType { get; set; }
        public long DebitNo { get; set; }
        public int CreditType { get; set; }
        public long CreditNo { get; set; }
        public string Description { get; set; }
        public DateTime TimeAsEntered { get; set; }
        public string Ref { get; set; }
    }

    public static class Helpers
    {
        public static AccountsJournalEntry ToEntity(this JournalEntryDto d) =>
            d is null
                ? throw new ArgumentNullException(nameof(d))
                : new AccountsJournalEntry
                {
                    JournalNo = (int)d.JournalNo,
                    RefNo = d.RefNo,
                    Amount = (double)d.Amount,
                    DebitAccountType = d.DebitType,
                    DebitAccountNo = d.DebitNo,
                    CreditAccountType = d.CreditType,
                    CreditAccountNo = d.CreditNo,
                    Description = d.Description,
                    TimeAsEntered = d.TimeAsEntered,
                    Ref = d.Ref,
                };

        public static JournalEntryDto ToDto(this AccountsJournalEntry e) =>
            e is null
                ? throw new ArgumentNullException(nameof(e))
                : new JournalEntryDto
                {
                    JournalNo = e.JournalNo,
                    RefNo = e.RefNo,
                    Amount = (decimal)e.Amount,
                    DebitType = e.DebitAccountType,
                    DebitNo = e.DebitAccountNo,
                    CreditType = e.CreditAccountType,
                    CreditNo = e.CreditAccountNo,
                    Description = e.Description,
                    TimeAsEntered = e.TimeAsEntered,
                    Ref = e.Ref,
                };
    }

    public readonly struct TimePeriod
    {
        public DateTime? From { get; }
        public DateTime? To { get; }

        public TimePeriod(DateTime? from, DateTime? to)
        {
            if (from.HasValue && to.HasValue && from > to)
                throw new ArgumentException("From must be ≤ To", nameof(from));
            From = from;
            To = to;
        }

        public bool Contains(DateTime x) =>
            (!From.HasValue || x >= From) && (!To.HasValue || x < To);
    }

    /// <summary>
    /// Defines the parameters for fetching inventory movements
    /// over a specified date range for a given item code.
    /// </summary>
    public class GetInventoryMovementsRequest
    {
        /// <summary>
        /// The item code to filter movements by.
        /// </summary>
        public long ItemCode { get; set; }

        /// <summary>
        /// Optional inclusive start date (UTC).
        /// If null, no lower bound is applied.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optional inclusive end date (UTC).
        /// If null, no upper bound is applied.
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}
