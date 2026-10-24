using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterSkillSlot
{
    public long CharacterId { get; set; }

    public sbyte Slot { get; set; }

    public string SkillId { get; set; } = null!;

    public virtual Character Character { get; set; } = null!;
}
