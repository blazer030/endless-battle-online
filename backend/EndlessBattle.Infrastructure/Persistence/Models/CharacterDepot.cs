using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterDepot
{
    public long Id { get; set; }

    public long CharacterId { get; set; }

    public string ItemId { get; set; } = null!;

    public int Quantity { get; set; }

    public int RefineLevel { get; set; }

    public string? RefineBonus { get; set; }

    public string? Sockets { get; set; }

    public int Holes { get; set; }

    public virtual Character Character { get; set; } = null!;
}
