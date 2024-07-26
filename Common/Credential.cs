using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Credential
{
    public long Userid { get; set; }

    public string Username { get; set; } = null!;

    public DateTime ValidUntil { get; set; }

    public DateTime Modified { get; set; }

    public string? Pubkey { get; set; }

    public string PasswordPbkdf2 { get; set; } = null!;

    public DateTime CreatedTime { get; set; }

    public bool Active { get; set; }
}
