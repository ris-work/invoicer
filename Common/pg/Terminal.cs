using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Terminal
{
    public string TerminalId { get; set; } = null!;

    public long DefaultBank { get; set; }

    public long DefaultCash { get; set; }
}
