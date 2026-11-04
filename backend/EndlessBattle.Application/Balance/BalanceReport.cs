using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Application.Balance;

public enum BalanceSkillLevel
{
    First,
    Max,
}

public sealed record BalanceReportOptions(BalanceSkillLevel SkillLevel = BalanceSkillLevel.Max);

// 報告採用的參考角色假設；和遊戲規則無關
public sealed record BalanceAssumptions(StatAllocation Allocation, int Seed, double SimulatedHours, int MaxReportedLevel, BalanceSkillLevel SkillLevel);

// Required：該商店等級這項能力的最低需求，沒有需求時為 null；Level：達到需求的最低等級，超過上限時為 null
public sealed record WearLevelCell(int ShopLevel, int? Required, int? Level);

// Stat 是 str、smart、agi、au 之一
public sealed record WearLevelRow(string JobId, string JobName, string Stat, IReadOnlyList<WearLevelCell> Cells);

// NextTierHours：比身上武器高一階的整套商店裝備要賺幾小時；沒有下一階或淨收入不是正數時為 null，後者 NextTierUnaffordable 為 true
public sealed record AdventureResult(
    int Fights,
    int Wins,
    double WinRate,
    int Losses,
    int Timeouts,
    double ExpPerHour,
    double GrossMoneyPerHour,
    double NetMoneyPerHour,
    int LevelsGained,
    double? NextTierHours,
    bool NextTierUnaffordable);

public sealed record AdventureRow(
    int PlaceId,
    string PlaceName,
    bool Restricted,
    int Level,
    string JobId,
    string JobName,
    IReadOnlyList<int> EquipmentShopLevels,
    AdventureResult WithoutSkills);

public sealed record BalanceReport(
    BalanceAssumptions Assumptions,
    IReadOnlyList<int> ShopLevels,
    IReadOnlyList<WearLevelRow> WearLevels,
    IReadOnlyList<AdventureRow> Adventures);
