using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VSalesStep1Inherent
{
    public long? Itemcode { get; set; }

    public long? Batchcode { get; set; }

    public double? ISellingPrice { get; set; }

    public double? IMinPrice { get; set; }

    public double? IInvMultRate { get; set; }

    public double? IInvAddRate { get; set; }

    public bool? ProcessDiscounts { get; set; }

    public bool? DiscountMethodIsMaximum { get; set; }

    public bool? HasVolDiscFlag { get; set; }

    public bool? HasUserDiscFlag { get; set; }

    public double? ISuggestedPrice { get; set; }

    public string? ExplanationStep1 { get; set; }
}
