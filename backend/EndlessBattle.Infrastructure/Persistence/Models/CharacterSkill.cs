using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterSkill
{
    public long CharacterId { get; set; }

    public string SkillId { get; set; } = null!;

    public int Level { get; set; }

    public virtual Character Character { get; set; } = null!;
}
