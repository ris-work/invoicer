using System;
using System.Collections.Generic;

namespace AuthManager;

public partial class Credential
{
    public long Userid { get; set; }

    public string Username { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTime ValidUntil { get; set; }

    public DateTime Modified { get; set; }

    public string Pubkey { get; set; }

    public string PasswordPbkdf2 { get; set; }
}
