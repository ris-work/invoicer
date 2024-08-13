using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class StatsHourly
{
    public string Hour { get; set; }

    public string ProcessName { get; set; }

    public double? AvgWorkingSet { get; set; }

    public string MaxWorkingSetForOneInstance { get; set; }

    public int? CpuDiff { get; set; }

    public byte[] TimeDiff { get; set; }

    public double? ThreadCount { get; set; }
}
