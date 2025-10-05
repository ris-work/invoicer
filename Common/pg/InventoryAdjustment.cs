using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class InventoryAdjustment
{
    public long EntryId { get; set; }

    public long AdjustmentBatch { get; set; }

    public long Itemcode { get; set; }

    public long Batchcode { get; set; }

    public long Count { get; set; }

    public double PerItemValue { get; set; }

    public double NetValue { get; set; }

    public bool Posted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTimeOffset EditedAt { get; set; }

    public string ReferenceCode { get; set; } = null!;

    public long ProcessedBy { get; set; }

    public long CreatedBy { get; set; }

    public long EditedBy { get; set; }

    public string Reason { get; set; } = null!;

    public double BeforeQty { get; set; }

    public double AfterQty { get; set; }
}
