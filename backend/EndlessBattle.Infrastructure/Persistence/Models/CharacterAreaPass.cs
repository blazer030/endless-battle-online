using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterAreaPass
{
    public long CharacterId { get; set; }

    public int PlaceId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public virtual Character Character { get; set; } = null!;
}
