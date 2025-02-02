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
                itemdesc.Split(" ")[0],
                itemdesc.Split(" ")[1],
                itemdesc.Split(" ")[2],
                r.NextInt64().ToString(),
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
}
