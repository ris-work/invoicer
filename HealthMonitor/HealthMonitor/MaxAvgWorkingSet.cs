using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class MaxAvgWorkingSet
{
    public string? ProcessName { get; set; }

    public byte[]? AvgWorkingSetValue { get; set; }

    public byte[]? MaxWorkingSetValue { get; set; }
}
