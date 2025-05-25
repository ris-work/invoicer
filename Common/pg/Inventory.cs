using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Internal inventory management functions
/// </summary>
public partial class Inventory
{
    public long Itemcode { get; set; }

    public long Batchcode { get; set; }

    public bool BatchEnabled { get; set; }

    public DateTime? MfgDate { get; set; }

    public DateTime? ExpDate { get; set; }

    public float PackedSize { get; set; }

    public double Units { get; set; }

    public string MeasurementUnit { get; set; } = null!;

    public double MarkedPrice { get; set; }

    public double SellingPrice { get; set; }

    public double CostPrice { get; set; }

    public bool VolumeDiscounts { get; set; }

    public long Suppliercode { get; set; }

    public bool UserDiscounts { get; set; }

    public DateTime LastCountedAt { get; set; }

    public string Remarks { get; set; } = null!;
}
