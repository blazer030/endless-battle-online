using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Application.Adventure;

public sealed record AdvanceResult(
    IReadOnlyList<BattleEvent> Events,
    AdvanceSummary Summary,
    bool EventsCoverAll,
    Character Character,
    IReadOnlyList<JobProficiency> JobProficiency);
