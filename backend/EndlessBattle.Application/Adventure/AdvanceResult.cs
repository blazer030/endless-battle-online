using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Progression;

namespace EndlessBattle.Application.Adventure;

// Mercenary 只在這次推進有傭兵戰鬥時才有值
public sealed record AdvanceResult(
    IReadOnlyList<BattleEvent> Events,
    AdvanceSummary Summary,
    bool EventsCoverAll,
    Character Character,
    IReadOnlyList<JobProficiency> JobProficiency,
    EquipmentBonus Equipment,
    MercenaryReport? Mercenary = null);

// Remaining 在解除僱用時為 0；Exp、Money 是打折後的合計
public sealed record MercenaryReport(
    string Name, IReadOnlyList<MercenaryFight> Fights, long Exp, long Money, int LevelsGained, int Remaining, bool Dismissed);
