using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class Pii
{
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsCompany { get; set; }

    public string? Email { get; set; }

    public string? Telephone { get; set; }

    public string? Mobile { get; set; }

    public string? Title { get; set; }

    public string? Address { get; set; }

    public string? Fax { get; set; }

    public string? Im { get; set; }

    public string? Sip { get; set; }

    public string? Gender { get; set; }
}
