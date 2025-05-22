using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class CustomerDiscount
{
    public long CustomerId { get; set; }

    public double RecommendedDiscountPercent { get; set; }

    public double LoyaltyRate { get; set; }

    public double LoyaltyPaidToAccountId { get; set; }
}
