using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class ProcessHistory
{
    public string TimeNow { get; set; } = null!;

    public int Pid { get; set; }

    public string? ProcessName { get; set; }

    public int? ThreadCount { get; set; }

    public int? VirtualMemoryUse { get; set; }

    public int? PagedMemoryUse { get; set; }

    public int? PrivateMemoryUse { get; set; }

    public int? WorkingSet { get; set; }

    public int? MainWindowTitle { get; set; }

    public int? Started { get; set; }

    public int? SystemTime { get; set; }

    public int? UserTime { get; set; }

    public int? TotalTime { get; set; }
}
