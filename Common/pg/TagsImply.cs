using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class TagsImply
{
    public long Id { get; set; }

    public string Tag { get; set; } = null!;

    public string Implies { get; set; } = null!;

    public DateTime RecordedAt { get; set; }

    public string Description { get; set; } = null!;

    public long CreatedBy { get; set; }
}
