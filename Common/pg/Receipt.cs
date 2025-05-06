using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Receipt
{
    public long ReceiptId { get; set; }

    public long InvoiceId { get; set; }

    public long AccountId { get; set; }

    public double Amount { get; set; }

    public DateTimeOffset TimeReceived { get; set; }
}
