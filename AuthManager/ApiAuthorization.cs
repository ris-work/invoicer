using System;
using System.Collections.Generic;

namespace AuthManager;

public partial class ApiAuthorization
{
    public long Userid { get; set; }

    public string Pubkey { get; set; }

    public string Authorization { get; set; }
}
