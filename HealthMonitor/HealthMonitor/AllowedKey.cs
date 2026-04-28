using System;
using System.Collections.Generic;

namespace HealthMonitor;

public partial class AllowedKey
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Sha256Fingerprint { get; set; } = null!;

    public string AddedTime { get; set; } = null!;

    public int IsActive { get; set; }
}
