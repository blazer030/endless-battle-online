using System;
using System.Collections.Generic;

namespace Inkbound.Infrastructure.Persistence.Models;

public partial class Character
{
    public long Id { get; set; }

    public long AccountId { get; set; }

    public string Name { get; set; } = null!;

    public long Experience { get; set; }

    public long SpiritStones { get; set; }

    public string CurrentStageId { get; set; } = null!;

    public DateTime LastSettledAt { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Account Account { get; set; } = null!;
}
