using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

/// <summary>
/// Photos of people, companies - any person, including non-natural persons.
/// </summary>
public partial class PiiImage
{
    public long PiiId { get; set; }

    public long ImageNo { get; set; }

    public string? Image { get; set; }
}
