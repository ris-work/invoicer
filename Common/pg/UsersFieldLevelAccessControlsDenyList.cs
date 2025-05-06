using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class UsersFieldLevelAccessControlsDenyList
{
    public long UserId { get; set; }

    public string DeniedField { get; set; } = null!;
}
