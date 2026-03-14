using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class IfrsCategory
{
    public long Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ReportType { get; set; } = null!;

    public bool IsCurrent { get; set; }

    public int ValidAccountType { get; set; }

    public int? SortOrder { get; set; }
}
