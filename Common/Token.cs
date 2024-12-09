using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Token
{
    public long Userid { get; set; }

    public string Tokenvalue { get; set; } = null!;

    public string Tokensecret { get; set; } = null!;

    public DateTime NotValidAfter { get; set; }

    public string Tokenid { get; set; } = null!;

    public bool Active { get; set; }

    public string Privileges { get; set; } = null!;

    public long CategoriesBitmask { get; set; }
}
