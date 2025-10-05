using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class ChequeBook
{
    public long Id { get; set; }

    public long AccountId { get; set; }

    public long StartNumber { get; set; }

    public long EndNumber { get; set; }

    public long NextNumber { get; set; }

    public bool IsOpen { get; set; }

    public bool IsCancelled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
