using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class PermissionsExtendedApiCall
{
    public long UserId { get; set; }

    public string ApiCall { get; set; } = null!;

    public string? AllowedAttributes { get; set; }
}
