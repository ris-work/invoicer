using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Issued invoices only
/// </summary>
public partial class IssuedInvoice
{
    public long InvoiceId { get; set; }

    public DateTime InvoiceTime { get; set; }

    public long? Customer { get; set; }

    public double IssuedValue { get; set; }

    public bool IsSettled { get; set; }

    public double PaidValue { get; set; }

    public string? InvoiceHumanFriendly { get; set; }

    public DateTimeOffset InvoiceTimePosted { get; set; }

    public bool IsPosted { get; set; }

    public double SubTotal { get; set; }

    public double DiscountTotal { get; set; }

    public double EffectiveDiscountPercentage { get; set; }

    public double TaxTotal { get; set; }

    public double GrandTotal { get; set; }

    public long SalesPersonId { get; set; }

    public string? CurrencyCode { get; set; }

    public DateTime CreatedAt { get; set; }
}
