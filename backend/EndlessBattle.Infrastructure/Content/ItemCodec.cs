using System.Text.Json.Serialization;
using EndlessBattle.Application.Admin.Content;
using EndlessBattle.Domain.Items;

namespace EndlessBattle.Infrastructure.Content;

// items.json ↔ ItemRecord；數值為 0 的能力不寫，沒有的巢狀物件不寫
public sealed class ItemCodec : IContentCodec<ItemRecord>
{
    private const string ItemsFile = "items.json";

    public IReadOnlyList<string> FileNames { get; } = [ItemsFile];

    public IReadOnlyList<ItemRecord> Read(IReadOnlyDictionary<string, string> files) =>
        ContentFileJson.ReadArray<ItemEntry>(files[ItemsFile], ItemsFile, "items").Select(ToRecord).ToList();

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<ItemRecord> records, IReadOnlyDictionary<string, string> files) =>
        new Dictionary<string, string> { [ItemsFile] = ContentFileJson.WriteArray("items", records.Select(ToEntry)) };

    private static ItemRecord ToRecord(ItemEntry entry) => new()
    {
        Id = entry.Id,
        Name = entry.Name,
        Category = entry.Category,
        ConsumableKind = entry.Consumable,
        Bonus = ToStats(entry.Bonus),
        SupplyHitPoints = entry.SupplyHp,
        SupplySkillPoints = entry.SupplySp,
        Amount = entry.Amount,
        Requires = entry.Requires is { } requires
            ? new ItemRequirementRecord
            {
                Strength = requires.Str ?? 0,
                Intelligence = requires.Smart ?? 0,
                Agility = requires.Agi ?? 0,
                Charisma = requires.Au ?? 0,
            }
            : null,
        Element = entry.Element is { } element ? ContentFileJson.ToElement(element, $"道具 {entry.Id}") : null,
        Holes = entry.Holes,
        RefineMax = entry.RefineMax,
        Price = entry.Price,
        ShopLevel = entry.ShopLevel,
        ShopSellable = entry.ShopSellable,
        Bound = entry.Bound == true,
        Refine = entry.Refine is { } refine
            ? new ItemRefineRecord
            {
                Level = refine.Level,
                Kind = refine.Kind,
                Attack = ToRange(refine.At, entry.Id),
                MagicAttack = ToRange(refine.Mat, entry.Id),
                Defense = ToRange(refine.Df, entry.Id),
                MagicDefense = ToRange(refine.Mdf, entry.Id),
                Strength = ToRange(refine.Str, entry.Id),
                Intelligence = ToRange(refine.Smart, entry.Id),
                Life = ToRange(refine.Life, entry.Id),
                Vitality = ToRange(refine.Vit, entry.Id),
                Agility = ToRange(refine.Agi, entry.Id),
                Charisma = ToRange(refine.Au, entry.Id),
                Faith = ToRange(refine.Be, entry.Id),
            }
            : null,
        Stone = entry.Stone is { } stone
            ? new ItemStoneRecord
            {
                Random = stone.Random,
                Fixed = ToStats(stone.Fixed),
                Minimum = ToStats(stone.Min),
                Maximum = ToStats(stone.Max),
                BoxClass = stone.BoxClass,
            }
            : null,
        Key = entry.Key,
        Elixir = entry.Elixir,
        WeaponSkill = entry.WeaponSkill is { } skill
            ? new ItemWeaponSkillRecord { SkillId = skill.SkillId, Level = skill.Level, ActivationRate = skill.Rate }
            : null,
    };

    private static ItemEntry ToEntry(ItemRecord record) => new()
    {
        Id = record.Id,
        Name = record.Name,
        Category = record.Category,
        Consumable = record.ConsumableKind,
        Bonus = ToStatsEntry(record.Bonus),
        SupplyHp = record.SupplyHitPoints,
        SupplySp = record.SupplySkillPoints,
        Amount = record.Amount,
        Requires = record.Requires is { } requires
            ? new RequirementEntry
            {
                Str = OmitZero(requires.Strength),
                Smart = OmitZero(requires.Intelligence),
                Agi = OmitZero(requires.Agility),
                Au = OmitZero(requires.Charisma),
            }
            : null,
        Element = record.Element is { } element ? (int)element : null,
        Holes = record.Holes,
        RefineMax = record.RefineMax,
        Price = record.Price,
        ShopLevel = record.ShopLevel,
        ShopSellable = record.ShopSellable,
        Bound = record.Bound ? true : null,
        Refine = record.Refine is { } refine
            ? new RefineEntry
            {
                Level = refine.Level,
                Kind = refine.Kind,
                At = FromRange(refine.Attack),
                Mat = FromRange(refine.MagicAttack),
                Df = FromRange(refine.Defense),
                Mdf = FromRange(refine.MagicDefense),
                Str = FromRange(refine.Strength),
                Smart = FromRange(refine.Intelligence),
                Life = FromRange(refine.Life),
                Vit = FromRange(refine.Vitality),
                Agi = FromRange(refine.Agility),
                Au = FromRange(refine.Charisma),
                Be = FromRange(refine.Faith),
            }
            : null,
        Stone = record.Stone is { } stone
            ? new StoneEntry
            {
                Random = stone.Random,
                Fixed = ToStatsEntry(stone.Fixed),
                Min = ToStatsEntry(stone.Minimum),
                Max = ToStatsEntry(stone.Maximum),
                BoxClass = stone.BoxClass,
            }
            : null,
        Key = record.Key,
        Elixir = record.Elixir,
        WeaponSkill = record.WeaponSkill is { } skill
            ? new WeaponSkillEntry { SkillId = skill.SkillId, Level = skill.Level, Rate = skill.ActivationRate }
            : null,
    };

    private static int? OmitZero(int value) => value == 0 ? null : value;

    private static ContentRange? ToRange(int[]? range, string itemId) =>
        range is null ? null : ContentFileJson.ToRange(range, $"道具 {itemId} 的精鍊");

    private static int[]? FromRange(ContentRange? range) => range is null ? null : ContentFileJson.FromRange(range);

    private static ItemStatsRecord? ToStats(StatsEntry? stats) => stats is null
        ? null
        : new ItemStatsRecord
        {
            Strength = stats.Str ?? 0,
            Intelligence = stats.Smart ?? 0,
            Life = stats.Life ?? 0,
            Vitality = stats.Vit ?? 0,
            Agility = stats.Agi ?? 0,
            Charisma = stats.Au ?? 0,
            Faith = stats.Be ?? 0,
            Attack = stats.At ?? 0,
            MagicAttack = stats.Mat ?? 0,
            Defense = stats.Df ?? 0,
            MagicDefense = stats.Mdf ?? 0,
            MaxHitPoints = stats.HpMax ?? 0,
        };

    private static StatsEntry? ToStatsEntry(ItemStatsRecord? stats) => stats is null
        ? null
        : new StatsEntry
        {
            Str = OmitZero(stats.Strength),
            Smart = OmitZero(stats.Intelligence),
            Life = OmitZero(stats.Life),
            Vit = OmitZero(stats.Vitality),
            Agi = OmitZero(stats.Agility),
            Au = OmitZero(stats.Charisma),
            Be = OmitZero(stats.Faith),
            At = OmitZero(stats.Attack),
            Mat = OmitZero(stats.MagicAttack),
            Df = OmitZero(stats.Defense),
            Mdf = OmitZero(stats.MagicDefense),
            HpMax = OmitZero(stats.MaxHitPoints),
        };

    // 屬性的宣告順序就是檔案的欄位順序
    private sealed record ItemEntry
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required ItemCategory Category { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ConsumableKind? Consumable { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StatsEntry? Bonus { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SupplyHp { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? SupplySp { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Amount { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RequirementEntry? Requires { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Element { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Holes { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RefineMax { get; init; }

        public required long Price { get; init; }

        public required int ShopLevel { get; init; }

        public required bool ShopSellable { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Bound { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public RefineEntry? Refine { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StoneEntry? Stone { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ItemKeyRecord? Key { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ItemElixirRecord? Elixir { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public WeaponSkillEntry? WeaponSkill { get; init; }
    }

    private sealed record StatsEntry
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Str { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Smart { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Life { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Vit { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Agi { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Au { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Be { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? At { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Mat { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Df { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Mdf { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? HpMax { get; init; }
    }

    private sealed record RequirementEntry
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Str { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Smart { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Agi { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Au { get; init; }
    }

    // 欄位順序與內容檔相同：先七項能力，再攻防
    private sealed record RefineEntry
    {
        public required int Level { get; init; }

        public required int Kind { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Str { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Smart { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Life { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Vit { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Agi { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Au { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Be { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? At { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Mat { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Df { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int[]? Mdf { get; init; }
    }

    private sealed record StoneEntry
    {
        public required bool Random { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StatsEntry? Fixed { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StatsEntry? Min { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StatsEntry? Max { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? BoxClass { get; init; }
    }

    private sealed record WeaponSkillEntry
    {
        public required string SkillId { get; init; }

        public required int Level { get; init; }

        public required int Rate { get; init; }
    }
}
