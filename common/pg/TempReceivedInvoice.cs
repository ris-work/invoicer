using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class TempReceivedInvoice
{
    public long TempInvoiceRunNo { get; set; }

    public string InvoiceContents { get; set; } = null!;

    public bool Posted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public long RequestId { get; set; }

    public string RequestIds { get; set; } = null!;

    public long UserId { get; set; }
}
