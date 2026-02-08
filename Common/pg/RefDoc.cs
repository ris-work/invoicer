using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class RefDoc
{
    public long RefId { get; set; }

    public string RefText { get; set; } = null!;

    public string RefImage { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public long AuthoredBy { get; set; }

    public string RefExtraData { get; set; } = null!;

    public string RefUrl { get; set; } = null!;

    public bool IsInventoryImage { get; set; }
}
