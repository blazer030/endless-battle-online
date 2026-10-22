using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterPet
{
    public long Id { get; set; }

    public long CharacterId { get; set; }

    public string MonsterId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public sbyte Personality { get; set; }

    public int Attack { get; set; }

    public int Magic { get; set; }

    public int Defense { get; set; }

    public int Spirit { get; set; }

    public int Satiety { get; set; }

    public int Intimacy { get; set; }

    public bool Carried { get; set; }

    public DateTime BornAt { get; set; }

    public DateTime HungerSince { get; set; }

    public virtual Character Character { get; set; } = null!;
}
