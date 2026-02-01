using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class MappedLocation
{
    public long Id { get; set; }

    public long MapId { get; set; }

    public string Name { get; set; } = null!;

    public long HorizontalSection { get; set; }

    public long VerticalSection { get; set; }
}
