using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class StatsHourly
{
    public byte[]? Hour { get; set; }

    public string? ProcessName { get; set; }

    public byte[]? AvgWorkingSet { get; set; }

    public byte[]? MaxWorkingSetForOneInstance { get; set; }

    public byte[]? CpuDiff { get; set; }

    public byte[]? CpuPercent { get; set; }

    public byte[]? TimeDiff { get; set; }

    public byte[]? ThreadCount { get; set; }
}
