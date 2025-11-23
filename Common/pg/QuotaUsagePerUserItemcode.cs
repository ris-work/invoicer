using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class QuotaUsagePerUserItemcode
{
    public long QuotaId { get; set; }

    public long Itemcode { get; set; }

    public long Pii { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public double Quantity { get; set; }
}
