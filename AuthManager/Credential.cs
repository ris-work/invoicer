using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthManager;

public partial class Credential
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Userid { get; set; }

    public string Username { get; set; }

    public DateTime ValidUntil { get; set; }

    public DateTime Modified { get; set; }

    public string Pubkey { get; set; }

    public string PasswordPbkdf2 { get; set; }

    public DateTime CreatedTime { get; set; }
}
