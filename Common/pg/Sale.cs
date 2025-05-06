using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Sales data go here
/// </summary>
public partial class Sale
{
    public long SaleId { get; set; }

    public long InvoiceId { get; set; }

    public DateTime EnteredAt { get; set; }

    public long Itemcode { get; set; }

    public long Batchcode { get; set; }

    public double Quantity { get; set; }

    public double SellingPrice { get; set; }

    public long VatCategory { get; set; }

    public double VatRatePercentage { get; set; }

    public double DiscountRate { get; set; }

    public double Discount { get; set; }

    public double VatAsCharged { get; set; }

    public double TotalEffectiveSellingPrice { get; set; }

    public string Remarks { get; set; } = null!;

    public DateTime? ClientRecordedTimeOpening { get; set; }

    public DateTime? ClientRecordedTimeClosing { get; set; }

    public string? SalesHumanFriendly { get; set; }

    public double LoyalityPointsPercentage { get; set; }

    public double LoyalityPointsIssued { get; set; }
}
