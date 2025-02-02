using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class CategoriesBitmask
{
    public long Bitmask { get; set; }

    public string Name { get; set; } = null!;

    public long? I18nLabel { get; set; }
}
