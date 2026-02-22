using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class ChequeLeaf
{
    public long Id { get; set; }

    public long ChequeBookId { get; set; }

    public long LeafNumber { get; set; }

    public string Status { get; set; } = null!;

    public string PayeeName { get; set; } = null!;

    public double Amount { get; set; }

    public DateTime IssuedAt { get; set; }

    public string? Notes { get; set; }

    public string? TxId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public long IssuedBy { get; set; }
}
