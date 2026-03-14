using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class TaxJurisdiction
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string? Name { get; set; }

    public bool? IsDefault { get; set; }
}
