using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Stamina;

namespace EndlessBattle.Domain.Adventure;

public sealed record AdvanceSummary(int Fights, int Wins, int Losses, int Timeouts, long Exp, long Money, long JobExp, int LevelsGained)
{
    public static AdvanceSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);
}

// 熟練度與技能點的增量，由呼叫端寫入熟練度帳
public sealed record JobGain(string JobId, long Proficiency, long SkillPoints);

// Events 只含最後 CatchUpWindowSeconds 秒內開始的場次；EventsCoverAll 表示沒有任何戰鬥被略去（略去的住宿不算）。
// ConsumedSeconds 是實際用掉的時間，最後一場會打完，所以可能超過可用時間
public sealed record AdventureRun(
    IReadOnlyList<BattleEvent> Events,
    AdvanceSummary Summary,
    Character Character,
    StaminaState Stamina,
    IReadOnlyList<JobGain> JobGains,
    double ConsumedSeconds,
    bool EventsCoverAll);
