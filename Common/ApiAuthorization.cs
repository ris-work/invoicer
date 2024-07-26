using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class ApiAuthorization
{
    public long Userid { get; set; }

    public string? Pubkey { get; set; }

    public string Authorization { get; set; } = null!;
}
