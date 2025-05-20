using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Of the form [object].[field] or [field]
/// </summary>
public partial class DefaultDenyField
{
    public string Field { get; set; } = null!;

    public DateTimeOffset? CreatedAt { get; set; }
}
