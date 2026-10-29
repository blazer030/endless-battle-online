using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterLodging
{
    public long CharacterId { get; set; }

    public string RoomId { get; set; } = null!;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public virtual Character Character { get; set; } = null!;
}
