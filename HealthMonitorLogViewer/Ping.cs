using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class Ping
{
    public string Dest { get; set; }

    public string TimeNow { get; set; }

    public int? Latency { get; set; }

    public int? WasItOkNotCorrupt { get; set; }

    public int? DidItSucceed { get; set; }
}
