using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class AvgWorkingSet
{
    public string? ProcessName { get; set; }

    public byte[]? AvgWorkingSetValue { get; set; }
}
