using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Content;

// 欄位對應 WOG 怪物資料：Element 0 為無屬性、1～6 對應六屬性；Meet 為遇敵率（0～100）
// PlaceId 0 為全域怪；SubPlaces 為所屬子地區（1～4），空的表示在該地區每個子地區都會出現；全域怪與無子地區的地區一定是空的
// MissionId 非 null 為任務怪（WOG m_mission）：只在該任務進行中才會遇到
// Str、Smart、Vit、Be 只給技能的「能力比對手高」條件比較；自訂怪物沒寫時為 0
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
    int Meet,
    int PlaceId = 0,
    IReadOnlyList<int>? SubPlaces = null,
    IReadOnlyList<DropEntry>? Drops = null,
    string? MissionId = null,
    int Str = 0,
    int Smart = 0,
    int Vit = 0,
    int Be = 0)
{
    public IReadOnlyList<int> SubPlaces { get; init; } = SubPlaces ?? [];

    public IReadOnlyList<DropEntry> Drops { get; init; } = Drops ?? [];

    // 技能與發動率來自 monster-skills.json，載入時解析好；沒有技能時為空
    public IReadOnlyList<EquippedSkill> Skills { get; init; } = [];
}

// Topr 對應 WOG 的掉落分母基數：實際分母為 ⌊Topr × 0.9⌋，越大越難掉
public sealed record DropEntry(string ItemId, int Topr);
