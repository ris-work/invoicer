using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Always these four _real_ accounts
/// </summary>
public partial class AccountsType
{
    public int AccountType { get; set; }

    public string AccountTypeName { get; set; } = null!;

    public long? AccountTypeI18nLabel { get; set; }
}
