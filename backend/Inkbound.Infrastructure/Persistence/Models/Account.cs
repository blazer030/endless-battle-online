using System;
using System.Collections.Generic;

namespace Inkbound.Infrastructure.Persistence.Models;

public partial class Account
{
    public long Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Character? Character { get; set; }
}
