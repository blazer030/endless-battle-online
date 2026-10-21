using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterJobProficiency
{
    public long CharacterId { get; set; }

    public string JobId { get; set; } = null!;

    public long Proficiency { get; set; }

    public long SkillPoints { get; set; }

    public virtual Character Character { get; set; } = null!;
}
