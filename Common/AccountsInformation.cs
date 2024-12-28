using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class AccountsInformation
{
    public int AccountType { get; set; }

    public long AccountNo { get; set; }

    public string AccountName { get; set; } = null!;

    public long? AccountPii { get; set; }

    public long? AccountI18nLabel { get; set; }

    public double AccountMin { get; set; }

    public double AccountMax { get; set; }

    public string? HumanFriendlyId { get; set; }
}
