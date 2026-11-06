using System;
using System.Collections.Generic;

namespace EndlessBattle.Infrastructure.Persistence.Models;

public partial class CharacterMission
{
    public long Id { get; set; }

    public long CharacterId { get; set; }

    public string MissionId { get; set; } = null!;

    public string State { get; set; } = null!;

    public string KillProgress { get; set; } = null!;

    public DateTime? DailyPeriod { get; set; }

    public DateTime AcceptedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Character Character { get; set; } = null!;
}
