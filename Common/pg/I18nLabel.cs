using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class I18nLabel
{
    public long Id { get; set; }

    public string Lang { get; set; } = null!;

    public string? Value { get; set; }
}
