using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class MaxAvgWorkingSet
{
    public string ProcessName { get; set; }

    public double? AvgWorkingSetValue { get; set; }

    public double? MaxWorkingSetValue { get; set; }
}
