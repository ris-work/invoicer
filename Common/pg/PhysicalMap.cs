using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class PhysicalMap
{
    public long MapId { get; set; }

    public string MapName { get; set; } = null!;

    public string MapType { get; set; } = null!;

    public string Map { get; set; } = null!;

    public long VerticalGridlines { get; set; }

    public long HorizontalGridlines { get; set; }
}
