using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class NotificationType
{
    public long NotificationTypeId { get; set; }

    public string NotificationTypeName { get; set; } = null!;

    public int NotificationServicerType { get; set; }

    public string NotificationService { get; set; } = null!;

    public string NotificationServiceOtherArgs { get; set; } = null!;
}
