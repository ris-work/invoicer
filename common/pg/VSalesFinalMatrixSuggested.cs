using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VSalesFinalMatrixSuggested
{
    public long? Batchcode { get; set; }

    public long? Itemcode { get; set; }

    public long? PiiId { get; set; }

    public long? VolStartFrom { get; set; }

    public bool? ProcessDiscounts { get; set; }

    public bool? DiscountMethodIsMaximum { get; set; }

    public bool? HasVolDiscFlag { get; set; }

    public bool? HasUserDiscFlag { get; set; }

    public double? ISellingPrice { get; set; }

    public double? IMinPrice { get; set; }

    public double? IVolDiscPct { get; set; }

    public double? IInvMultRate { get; set; }

    public double? IInvAddRate { get; set; }

    public double? IPiiMultRate { get; set; }

    public double? IPiiAddRate { get; set; }

    public double? ISuggestedPrice { get; set; }

    public double? OEffectiveLpRate { get; set; }

    public double? ORawDiscountPercentage { get; set; }

    public double? OAdjustedMinPrice { get; set; }

    public string? ExplanationFinal { get; set; }
}
