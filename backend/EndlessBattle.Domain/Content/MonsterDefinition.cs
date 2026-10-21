using EndlessBattle.Domain.Combat;

namespace EndlessBattle.Domain.Content;

// 欄位對應 WOG 怪物資料：Element 0 為無屬性、1～6 對應六屬性；Meet 為遇敵率（0～100）
public sealed record MonsterDefinition(
    string Id,
    string Name,
    int Level,
    int Hp,
    int Sp,
    int At,
    int Mat,
    int Df,
    int Mdf,
    int Agi,
    int Au,
    int Element,
    long JobExp,
    int Meet)
{
    // 計畫 4 Task 2／5 移除：WOG 戰鬥與獎勵公式上線前，暫時接到舊的即時戰鬥模型
    public CombatStats LegacyStats => new(
        MaxHealth: Hp,
        Attack: At,
        Defense: Df,
        CritChance: 0.10,
        CritMultiplier: 1.2,
        AttacksPerSecond: 1.0);

    // 計畫 4 Task 5 移除
    public long LegacyExperienceReward => Level * 2L;

    // 計畫 4 Task 5 移除
    public long LegacySpiritStoneReward => Level;
}
