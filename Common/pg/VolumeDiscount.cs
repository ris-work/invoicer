using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VolumeDiscount
{
    public long Itemcode { get; set; }

    public long StartFrom { get; set; }

    public double DiscountPercentage { get; set; }

    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public long RequestId { get; set; }

    public string AllRequestIds { get; set; } = null!;
}
