using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VatCategory
{
    public long VatCategoryId { get; set; }

    public double VatPercentage { get; set; }

    public string VatName { get; set; } = null!;

    public bool Active { get; set; }
}
