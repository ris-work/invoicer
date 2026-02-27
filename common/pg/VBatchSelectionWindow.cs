using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class VBatchSelectionWindow
{
    public long? Itemcode { get; set; }

    public long? Batchcode { get; set; }

    public double? Units { get; set; }

    public double? SellingPrice { get; set; }

    public double? MinPrice { get; set; }

    public DateTime? MfgDate { get; set; }

    public DateTime? ExpDate { get; set; }

    public double? CumulativeQuantity { get; set; }

    public double? PrevCumulativeQuantity { get; set; }
}
