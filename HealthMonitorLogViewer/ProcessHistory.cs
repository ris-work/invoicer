using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class ProcessHistory
{
    public string TimeNow { get; set; } = null!;

    public int Pid { get; set; }

    public string? ProcessName { get; set; }

    public int? ThreadCount { get; set; }

    public string? VirtualMemoryUse { get; set; }

    public string? PagedMemoryUse { get; set; }

    public string? PrivateMemoryUse { get; set; }

    public string? WorkingSet { get; set; }

    public string? MainWindowTitle { get; set; }

    public string? Started { get; set; }

    public int? SystemTime { get; set; }

    public int? UserTime { get; set; }

    public int? TotalTime { get; set; }
}
