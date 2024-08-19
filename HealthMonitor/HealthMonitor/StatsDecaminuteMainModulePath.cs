using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class StatsDecaminuteMainModulePath
{
    public string Decaminute { get; set; }

    public string MainModulePath { get; set; }

    public long? AvgWorkingSet { get; set; }

    public string MaxWorkingSetForOneInstance { get; set; }

    public long? CpuDiff { get; set; }

    public double? CpuPercent { get; set; }

    public double? TimeDiff { get; set; }

    public double? ThreadCount { get; set; }
}
