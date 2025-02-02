using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VolumeDiscount
{
    public long Itemcode { get; set; }

    public long StartFrom { get; set; }

    public double DiscountPerUnit { get; set; }
}
