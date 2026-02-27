using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class SuggestedPricesToBeRemoved
{
    public long Itemcode { get; set; }

    public double Price { get; set; }

    public DateTime AddedAt { get; set; }

    public long AddedBy { get; set; }
}
