using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class SuggestedPrice
{
    public long Id { get; set; }

    public long Itemcode { get; set; }

    public double Price { get; set; }

    public long CreatedBy { get; set; }

    public long RequestId { get; set; }

    public string AllRequestIds { get; set; } = null!;
}
