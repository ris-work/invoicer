using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class ItemTagImplication
{
    public long? Itemcode { get; set; }

    public string? SourceTag { get; set; }

    public string? TransitiveTag { get; set; }

    public string? RuleChain { get; set; }
}
