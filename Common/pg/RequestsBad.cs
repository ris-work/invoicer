using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class RequestsBad
{
    public DateTime TimeTai { get; set; }

    public long? Principal { get; set; }

    public string Token { get; set; } = null!;

    public string? RequestBody { get; set; }
}
