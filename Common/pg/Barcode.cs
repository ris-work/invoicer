using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Barcode
{
    public string Code { get; set; } = null!;

    public long? Itemcode { get; set; }

    public long? Batchcode { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ModifiedAt { get; set; }
}
