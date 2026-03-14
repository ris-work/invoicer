using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VTaxResolution
{
    public string? JurisdictionCode { get; set; }

    public string? JurisdictionName { get; set; }

    public bool? IsDefault { get; set; }

    public long? VatCategoryId { get; set; }

    public string? VatName { get; set; }

    public double? EffectiveRatePercentage { get; set; }

    public string? RateSource { get; set; }
}
