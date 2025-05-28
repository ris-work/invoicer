using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Purchase
{
    public long ReceivedInvoiceId { get; set; }

    public long Itemcode { get; set; }

    public long PackSize { get; set; }

    public long PackQuantity { get; set; }

    public double ReceivedAsUnitQuantity { get; set; }

    public long FreePacks { get; set; }

    public double FreeUnits { get; set; }

    public DateTimeOffset ExpiryDate { get; set; }

    public DateTimeOffset? ManufacturingDate { get; set; }

    public string? ManufacturerBatchId { get; set; }

    public string ProductName { get; set; } = null!;

    public DateTimeOffset AddedDate { get; set; }

    public double DiscountPercentage { get; set; }

    public double DiscountAbsolute { get; set; }

    public double GrossProfitPercentage { get; set; }

    public double GrossProfitAbsolute { get; set; }

    public double CostPerUnit { get; set; }

    public double CostPerPack { get; set; }

    public double GrossCostPerUnit { get; set; }

    public double SellingPrice { get; set; }

    public double VatPercentage { get; set; }

    public long VatCategory { get; set; }

    public double VatAbsolute { get; set; }

    public string VatCategoryName { get; set; } = null!;

    public double TotalUnits { get; set; }

    public double NetTotalPrice { get; set; }

    public double TotalAmountDue { get; set; }

    public double GrossTotal { get; set; }

    public double NetTotal { get; set; }

    public double GrossMarkupPercentage { get; set; }

    public double GrossMarkupAbsolute { get; set; }

    public bool IsVatADisallowedInputTax { get; set; }

    public double NetCostPerUnit { get; set; }
}
