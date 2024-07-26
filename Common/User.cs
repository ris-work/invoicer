using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class User
{
    public long Userid { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }
}
