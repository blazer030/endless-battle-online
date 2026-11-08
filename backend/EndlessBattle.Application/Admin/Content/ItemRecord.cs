using System.Text.Json.Serialization;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Application.Admin.Content;

public enum ContentElixirKind
{
    Exp,
    JobExp,
    Attack,
    Magic,
    Defense,
    MagicDefense,
}

// 哪些分類有哪些欄位由內容載入程式檢查；沒有的欄位不會出現在回應裡
public sealed record ItemRecord : IContentRecord
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required ItemCategory Category { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConsumableKind? ConsumableKind { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemStatsRecord? Bonus { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SupplyHitPoints { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SupplySkillPoints { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Amount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemRequirementRecord? Requires { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentElement? Element { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Holes { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RefineMax { get; init; }

    public required long Price { get; init; }

    public required int ShopLevel { get; init; }

    public required bool ShopSellable { get; init; }

    public required bool Bound { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemRefineRecord? Refine { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemStoneRecord? Stone { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemKeyRecord? Key { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemElixirRecord? Elixir { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemWeaponSkillRecord? WeaponSkill { get; init; }

    [JsonIgnore]
    public string RecordId => Id;
}

// 加成與魔石的數值；沒有的能力是 0
public sealed record ItemStatsRecord
{
    public required int Strength { get; init; }

    public required int Intelligence { get; init; }

    public required int Life { get; init; }

    public required int Vitality { get; init; }

    public required int Agility { get; init; }

    public required int Charisma { get; init; }

    public required int Faith { get; init; }

    public required int Attack { get; init; }

    public required int MagicAttack { get; init; }

    public required int Defense { get; init; }

    public required int MagicDefense { get; init; }

    public required int MaxHitPoints { get; init; }
}

// 裝備需求只比對這四項；沒有的能力是 0
public sealed record ItemRequirementRecord
{
    public required int Strength { get; init; }

    public required int Intelligence { get; init; }

    public required int Agility { get; init; }

    public required int Charisma { get; init; }
}

public sealed record ItemRefineRecord
{
    public required int Level { get; init; }

    // 系列，內容裡是 1～18，意義尚未確認
    public required int Kind { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Attack { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? MagicAttack { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Defense { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? MagicDefense { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Strength { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Intelligence { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Life { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Vitality { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Agility { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Charisma { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRange? Faith { get; init; }
}

// 隨機的魔石有 minimum 與 maximum，其他有 fixed
public sealed record ItemStoneRecord
{
    public required bool Random { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemStatsRecord? Fixed { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemStatsRecord? Minimum { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemStatsRecord? Maximum { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BoxClass { get; init; }
}

public sealed record ItemKeyRecord
{
    public required int PlaceId { get; init; }

    public required int Seconds { get; init; }
}

public sealed record ItemElixirRecord
{
    public required ContentElixirKind Kind { get; init; }

    public required double Multiplier { get; init; }

    public required int Seconds { get; init; }
}

public sealed record ItemWeaponSkillRecord
{
    public required string SkillId { get; init; }

    public required int Level { get; init; }

    public required int ActivationRate { get; init; }
}
