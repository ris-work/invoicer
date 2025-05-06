using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class LoyaltyPoint
{
    public long PointsId { get; set; }

    public long InvoiceId { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime ValidUntil { get; set; }

    public long CustId { get; set; }
}
