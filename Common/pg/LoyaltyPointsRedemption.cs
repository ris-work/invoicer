using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class LoyaltyPointsRedemption
{
    public long RedemptionId { get; set; }

    public long CustId { get; set; }

    public long InvoiceId { get; set; }

    public double Amount { get; set; }

    public DateTimeOffset TimeIssued { get; set; }
}
