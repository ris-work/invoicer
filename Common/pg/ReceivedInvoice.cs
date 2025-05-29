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

    public double EffectiveDiscountAbsoluteTotal { get; set; }

    public double DefaultVatPercentage { get; set; }

    public long DefaultVatCategory { get; set; }

    public double EffectiveDiscountPercentageTotal { get; set; }

    public bool IsSettled { get; set; }

    public string DefaultVatCategoryName { get; set; } = null!;

    public double WholeInvoiceDiscountAbsolute { get; set; }

    public double WholeInvoiceDiscountPercentage { get; set; }

    public double EffectiveDiscountPercentageFromEnteredItems { get; set; }

    public double EffectiveDiscountAbsoluteFromEnteredItems { get; set; }

    public double VatTotal { get; set; }

    public double EffectiveVatPercentage { get; set; }
}
