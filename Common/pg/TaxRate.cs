using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class TaxRate
{
    public long Id { get; set; }

    public string JurisdictionCode { get; set; } = null!;

    public int VatCategoryId { get; set; }

    public double RatePercentage { get; set; }
}
