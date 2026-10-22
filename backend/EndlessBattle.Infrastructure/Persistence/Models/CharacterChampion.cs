using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterChampion
{
    public long CharacterId { get; set; }

    public string Name { get; set; } = null!;

    public int Level { get; set; }

    public sbyte Element { get; set; }

    public string SpecialAttackName { get; set; } = null!;

    public int Hp { get; set; }

    public int HpMax { get; set; }

    public int Sp { get; set; }

    public int SpMax { get; set; }

    public int At { get; set; }

    public int Mat { get; set; }

    public int Df { get; set; }

    public int Mdf { get; set; }

    public int Agi { get; set; }

    public int Au { get; set; }

    public bool IsPlayer { get; set; }

    public int Defenses { get; set; }

    public DateTime? CrownedAt { get; set; }

    public DateTime? LockedUntil { get; set; }

    public virtual Character Character { get; set; } = null!;
}
