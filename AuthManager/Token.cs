using System;
using System.Collections.Generic;

namespace AuthManager;

public partial class Token
{
    public long Userid { get; set; }

    public long Tokenid { get; set; }

    public string Tokenvalue { get; set; }

    public string Tokensecret { get; set; }

    public DateTime NotValidAfter { get; set; }
}
