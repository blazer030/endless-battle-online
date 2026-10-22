using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterMercenary
{
    public long CharacterId { get; set; }

    public string MercenaryId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Remaining { get; set; }

    public int? PlaceId { get; set; }

    public DateTime? NextFightAt { get; set; }

    public DateTime HiredAt { get; set; }

    public virtual Character Character { get; set; } = null!;
}
