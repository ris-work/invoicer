using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class CodesCatalogue
{
    public string Code { get; set; } = null!;

    public long Itemcode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool Enabled { get; set; }
}
