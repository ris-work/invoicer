using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class AccountsJournalEntry
{
    public long JournalUnivSeq { get; set; }

    public int JournalNo { get; set; }

    public string? RefNo { get; set; }

    public double Amount { get; set; }

    public int DebitAccountType { get; set; }

    public long DebitAccountNo { get; set; }

    public int CreditAccountType { get; set; }

    public long CreditAccountNo { get; set; }

    public string? Description { get; set; }

    public DateTime TimeTai { get; set; }

    public DateTime TimeAsEntered { get; set; }

    public string? Ref { get; set; }
}
