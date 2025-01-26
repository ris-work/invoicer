using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Notification
{
    public long NotifId { get; set; }

    public string NotifType { get; set; } = null!;

    public string NotifOtherStatus { get; set; } = null!;

    public bool NotifIsDone { get; set; }

    public string NotifTarget { get; set; } = null!;

    public DateTime TimeTai { get; set; }

    public DateTime? TimeExpiresTai { get; set; }

    public string NotifContents { get; set; } = null!;

    public int? NotifPriority { get; set; }

    public string NotifFrom { get; set; } = null!;

    public string NotifSource { get; set; } = null!;
}
