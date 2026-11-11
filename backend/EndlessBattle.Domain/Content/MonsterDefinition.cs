using EndlessBattle.Domain.Battle;

namespace EndlessBattle.Domain.Content;

// 欄位對應 WOG 怪物資料：Element 0 為無屬性、1～6 對應六屬性；Meet 為遇敵率（0～100）
// PlaceId 0 為全域怪；SubPlaces 為所屬子地區（1～4），空的表示在該地區每個子地區都會出現；全域怪與無子地區的地區一定是空的
// MissionId 非 null 為任務怪（WOG m_mission）：只在該任務進行中才會遇到
// Strength、Intelligence、Vitality、Faith 只給技能的「能力比對手高」條件比較；自訂怪物沒寫時為 0
public sealed record MonsterDefinition(
    string Id,
    string Name,
    int Level,
    int HealthPoints,
    int ManaPoints,
    int Attack,
    int MagicAttack,
    int Defense,
    int MagicDefense,
    int Agility,
    int Charisma,
    int Element,
    long JobExperience,
    int Meet,
    int PlaceId = 0,
    IReadOnlyList<int>? SubPlaces = null,
    IReadOnlyList<DropEntry>? Drops = null,
    string? MissionId = null,
    int Strength = 0,
    int Intelligence = 0,
    int Vitality = 0,
    int Faith = 0)
{
    public IReadOnlyList<int> SubPlaces { get; init; } = SubPlaces ?? [];

    public IReadOnlyList<DropEntry> Drops { get; init; } = Drops ?? [];

    // NPC（WOG m_npc）：遇到時不戰鬥，只推進任務；等級、HP、敏捷可以是 0。NpcMessage 是遭遇時顯示的原版訊息，可以沒有
    public bool Npc { get; init; }

    public string? NpcMessage { get; init; }

    // 技能與發動率來自 monster-skills.json，載入時解析好；沒有技能時為空
    public IReadOnlyList<EquippedSkill> Skills { get; init; } = [];
}

// Topr 對應 WOG 的掉落分母基數：實際分母為 ⌊Topr × 0.9⌋，越大越難掉
public sealed record DropEntry(string ItemId, int Topr);
