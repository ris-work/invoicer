using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class AccountsJournalInformation
{
    public long JournalId { get; set; }

    public string JournalName { get; set; } = null!;

    public long? JournalI18nLabel { get; set; }
}
