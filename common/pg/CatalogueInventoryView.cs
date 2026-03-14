using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class CatalogueInventoryView
{
    public long? Itemcode { get; set; }

    public string? Description { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? DescriptionPos { get; set; }

    public string? DescriptionWeb { get; set; }

    public long? DescriptionsOtherLanguages { get; set; }

    public long? DefaultVatCategory { get; set; }

    public bool? VatDependsOnUser { get; set; }

    public bool? VatCategoryAdjustable { get; set; }

    public bool? PriceManual { get; set; }

    public bool? EnforceAboveCost { get; set; }

    public bool? ActiveWeb { get; set; }

    public bool? ExpiryTrackingEnabled { get; set; }

    public long? PermissionsCategory { get; set; }

    public long? CategoriesBitmask { get; set; }

    public bool? ProcessDiscounts { get; set; }

    public double? MaxPerInvoice { get; set; }

    public double? MinPerInvoice { get; set; }

    public double? MaxPerPerson { get; set; }

    public double? HeightM { get; set; }

    public double? LengthM { get; set; }

    public double? WidthM { get; set; }

    public double? WeightPerUnitKg { get; set; }

    public bool? AllowPriceSuggestions { get; set; }

    public string? Remarks { get; set; }

    public double? QuotaPerQuotaPeriod { get; set; }

    public bool? TimeBasedQuotaEnabled { get; set; }

    public double? QuotaPerInvoice { get; set; }

    public bool? PerInvoiceQuotaEnabled { get; set; }

    public bool? DiscountMethodIsMaximum { get; set; }

    public bool? IsLossLeader { get; set; }

    public string? Tags { get; set; }

    public string? ExtraStructured { get; set; }

    public string? RefLink { get; set; }

    public long? RefDocId { get; set; }

    public double? ValidStockQuantity { get; set; }

    public double? LowestAvailablePrice { get; set; }
}
