using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VSalesFinalOutputSuggested
{
    public long? Batchcode { get; set; }

    public long? Itemcode { get; set; }

    public long? PiiId { get; set; }

    public double? ISuggestedPrice { get; set; }

    public double? ISellingPrice { get; set; }

    public double? IMinPrice { get; set; }

    public double? OAdjustedMinPrice { get; set; }

    public double? ORawDiscountPercentage { get; set; }

    public double? OEffectiveLpRate { get; set; }

    public double? ORawDiscountAmt { get; set; }

    public double? ORawPrice { get; set; }

    public double? OEffectiveSellingPricePerUnit { get; set; }

    public double? OEffectiveDiscountPerUnit { get; set; }

    public bool? IsClamped { get; set; }

    public string? ExplanationFinal { get; set; }
}
