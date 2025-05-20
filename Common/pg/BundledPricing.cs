using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class BundledPricing
{
    public long BundleId { get; set; }

    public long Itemcode { get; set; }

    public double Discount { get; set; }
}
