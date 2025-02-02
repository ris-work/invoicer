using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Positive is debit
/// </summary>
public partial class AccountsBalance
{
    public int AccountType { get; set; }

    public long AccountNo { get; set; }

    public double Amount { get; set; }

    public DateTime TimeTai { get; set; }

    public DateTime TimeAsEntered { get; set; }

    public bool DoneWith { get; set; }
}
