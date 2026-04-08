using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class StatsDecaminute
{
    public byte[]? Decaminute { get; set; }

    public string? ProcessName { get; set; }

    public byte[]? AvgWorkingSet { get; set; }

    public byte[]? MaxWorkingSetForOneInstance { get; set; }

    public byte[]? CpuDiff { get; set; }

    public byte[]? CpuPercent { get; set; }

    public byte[]? TimeDiff { get; set; }

    public byte[]? ThreadCount { get; set; }
}
