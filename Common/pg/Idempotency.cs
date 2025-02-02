using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Idempotency
{
    public string Key { get; set; } = null!;

    public string? Request { get; set; }

    public DateTimeOffset TimeTai { get; set; }
}
