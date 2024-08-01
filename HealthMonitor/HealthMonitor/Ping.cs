using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class Ping
{
    public string Dest { get; set; } = null!;

    public string TimeNow { get; set; } = null!;

    public int? Latency { get; set; }

    public int? Corrupt { get; set; }
}
