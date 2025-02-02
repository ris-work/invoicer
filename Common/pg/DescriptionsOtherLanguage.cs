using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class DescriptionsOtherLanguage
{
    public long Id { get; set; }

    public string Language { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string DescriptionPos { get; set; } = null!;

    public string DescriptionWeb { get; set; } = null!;
}
