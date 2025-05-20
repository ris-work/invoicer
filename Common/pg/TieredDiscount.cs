using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class TieredDiscount
{
    public long Itemcode { get; set; }

    public double MinQty { get; set; }

    public double DiscountPercentage { get; set; }
}
