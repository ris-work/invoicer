using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class AccountsInformation
{
    public int AccountType { get; set; }

    public string AccountName { get; set; } = null!;

    public long? AccountPii { get; set; }

    public long? AccountI18nLabel { get; set; }

    public double AccountMin { get; set; }

    public double AccountMax { get; set; }

    public string? HumanFriendlyId { get; set; }

    public bool AllowCreditOnPos { get; set; }

    public bool AllowDebitOnPos { get; set; }

    public bool IsBank { get; set; }

    public bool IsCash { get; set; }

    public bool IsReserve { get; set; }

    public bool IsReconcilable { get; set; }

    public bool IsInventoryTracked { get; set; }

    public bool IsDefaultCashRegister { get; set; }

    public long AccountNo { get; set; }

    public double LoyaltyBaseMultiplicativePointsPercentage { get; set; }
}
