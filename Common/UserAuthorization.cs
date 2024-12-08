using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// user_cap: Comma-separated
/// user_default_cap: Comma-separated
/// </summary>
public partial class UserAuthorization
{
    public long Userid { get; set; }

    public string UserCap { get; set; } = null!;

    public string? UserDefaultCap { get; set; }
}
