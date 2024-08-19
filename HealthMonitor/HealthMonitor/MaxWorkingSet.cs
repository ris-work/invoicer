using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class MaxWorkingSet
{
    public string ProcessName { get; set; }

    public double? MaxWorkingSetValue { get; set; }
}
