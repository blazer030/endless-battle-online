using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;

namespace EndlessBattle.Domain.Adventure;

public sealed record AdvanceSummary(int Fights, int Wins, int Losses, int Timeouts, long Experience, long Money, long JobExperience, int LevelsGained, int SkillsCast, int Drops = 0, int PotionsUsed = 0, int MissionKills = 0, int PetsCaptured = 0, int Escapes = 0)
{
    public static AdvanceSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
}

// Drops 是進了背包的件數（被丟棄的不算）；模擬器不知道背包，固定為 0，由 AdventureService 依背包結果填入
// MissionKills 是整段推進中任務進度增加的總次數（含窗口外的場次）；Escapes 是逃跑成功的場數（已計入 Losses）
// 熟練度與技能點的增量，由呼叫端寫入熟練度帳
public sealed record JobGain(string JobId, long Proficiency, long SkillPoints);

// Events 只含最後 CatchUpWindowSeconds 秒內開始的場次；EventsCoverAll 表示沒有任何戰鬥被略去（略去的住宿不算）。
// DroppedItemIds 是所有勝場（含窗口外）擲到的道具，依順序，尚未套用到背包。
// ConsumedSeconds 是實際用掉的時間，最後一場會打完，所以可能超過可用時間
// SupplyRemaining 是推進結束時補給欄（補給品或捕捉道具）的剩餘數量，補給欄空著時為 null；Missions 是推進結束時各擊殺任務的狀態；
// Captured 是這次捕到的寵物，依捕捉順序
public sealed record AdventureRun(
    IReadOnlyList<BattleEvent> Events,
    AdvanceSummary Summary,
    Character Character,
    IReadOnlyList<JobGain> JobGains,
    double ConsumedSeconds,
    bool EventsCoverAll,
    IReadOnlyList<string> DroppedItemIds,
    int? SupplyRemaining = null,
    IReadOnlyList<ActiveMission>? Missions = null,
    IReadOnlyList<CapturedPet>? Captured = null)
{
    public IReadOnlyList<ActiveMission> Missions { get; init; } = Missions ?? [];

    public IReadOnlyList<CapturedPet> Captured { get; init; } = Captured ?? [];
}
