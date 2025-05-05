using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class RequestsBad
{
    public DateTime TimeTai { get; set; }

    public long? Principal { get; set; }

    public string Token { get; set; } = null!;

    public string? RequestBody { get; set; }

    public string? Type { get; set; }

    public string? RequestedAction { get; set; }

    public string? RequestedPrivilegeLevel { get; set; }

    public string? Endpoint { get; set; }

    public string? ProvidedPrivilegeLevels { get; set; }
}
