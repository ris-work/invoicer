using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Payment
{
    public long Id { get; set; }

    public long? ScheduledPaymentId { get; set; }

    public long CompanyId { get; set; }

    public long? VendorId { get; set; }

    public long? InvoiceId { get; set; }

    public string PaymentReference { get; set; } = null!;

    public string? Description { get; set; }

    public string Currency { get; set; } = null!;

    public double Amount { get; set; }

    public double ExchangeRate { get; set; }

    public long DebitAccountId { get; set; }

    public long CreditAccountId { get; set; }

    public long BankAccountId { get; set; }

    public string? BeneficiaryName { get; set; }

    public string? BeneficiaryBankName { get; set; }

    public string? BeneficiaryBranch { get; set; }

    public string? BeneficiaryAccountNo { get; set; }

    public string? BeneficiaryRoutingNo { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    public string? ExternalPaymentId { get; set; }

    public double? FeeAmount { get; set; }

    public double? NetAmount { get; set; }

    public bool IsReconciled { get; set; }

    public bool IsExcluded { get; set; }

    public DateOnly? ReconciliationDate { get; set; }

    public string? ReconciliationRef { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long VersionNumber { get; set; }

    public bool AutoApply { get; set; }
}
