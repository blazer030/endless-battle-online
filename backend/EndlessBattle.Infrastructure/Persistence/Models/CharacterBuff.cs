using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterBuff
{
    public long CharacterId { get; set; }

    public string Kind { get; set; } = null!;

    public string? ItemId { get; set; }

    public decimal Multiplier { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public virtual Character Character { get; set; } = null!;
}
