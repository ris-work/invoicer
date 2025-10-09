using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class CycleCount
{
    public long Id { get; set; }

    public long Itemcode { get; set; }

    public long SeqNo { get; set; }

    public double RecordedQty { get; set; }

    public double ActualQty { get; set; }

    public DateTime CountDate { get; set; }

    public long PrincipalId { get; set; }

    public string PrincipalName { get; set; } = null!;

    public string? Location { get; set; }

    public string? Notes { get; set; }
}
