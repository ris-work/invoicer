using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class ReceivedInvoice
{
    public long ReceivedInvoiceNo { get; set; }

    public bool IsPosted { get; set; }

    public long SupplierId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string Remarks { get; set; } = null!;

    public string Reference { get; set; } = null!;

    public double GrossTotal { get; set; }

    public double TransportCharges { get; set; }

    public double Discount { get; set; }

    public double DefaultVatPercentage { get; set; }

    public long DefaultVatCategory { get; set; }

    public double DiscountPercentage { get; set; }

    public bool IsSettled { get; set; }

    public string DefaultVatCategoryName { get; set; } = null!;
}
