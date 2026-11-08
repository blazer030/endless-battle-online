using System.Text.Json.Serialization;
using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Infrastructure.Content;

// monsters.json 與 monster-skills.json ↔ MonsterRecord；關聯表寫回時依怪物順序重新產生
public sealed class MonsterCodec : IContentCodec<MonsterRecord>
{
    private const string MonstersFile = "monsters.json";
    private const string SkillsFile = "monster-skills.json";

    public IReadOnlyList<string> FileNames { get; } = [MonstersFile, SkillsFile];

    public IReadOnlyList<MonsterRecord> Read(IReadOnlyDictionary<string, string> files)
    {
        var monsters = ContentFileJson.ReadArray<MonsterEntry>(files[MonstersFile], MonstersFile, "monsters");
        var rows = ContentFileJson.ReadArray<MonsterSkillEntry>(files[SkillsFile], SkillsFile, "monsterSkills");
        var orphan = rows.FirstOrDefault(row => monsters.All(monster => monster.Id != row.MonsterId));
        if (orphan is not null)
        {
            throw new GameContentException($"{SkillsFile} 引用了不存在的怪物：{orphan.MonsterId}");
        }

        var skillsByMonster = rows.ToLookup(row => row.MonsterId);
        return monsters.Select(monster => ToRecord(monster, skillsByMonster[monster.Id])).ToList();
    }

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<MonsterRecord> records, IReadOnlyDictionary<string, string> files) =>
        new Dictionary<string, string>
        {
            [MonstersFile] = ContentFileJson.WriteArray("monsters", records.Select(ToEntry)),
            [SkillsFile] = ContentFileJson.WriteArray(
                "monsterSkills",
                records.SelectMany(record => record.Skills.Select(skill => new MonsterSkillEntry
                {
                    MonsterId = record.Id,
                    SkillId = skill.SkillId,
                    Level = skill.Level,
                    Rate = skill.ActivationRate,
                }))),
        };

    private static MonsterRecord ToRecord(MonsterEntry entry, IEnumerable<MonsterSkillEntry> skills) => new()
    {
        Id = entry.Id,
        Name = entry.Name,
        Level = entry.Level,
        HitPoints = entry.Hp,
        SkillPoints = entry.Sp,
        Attack = entry.At,
        MagicAttack = entry.Mat,
        Defense = entry.Df,
        MagicDefense = entry.Mdf,
        Strength = entry.Str,
        Intelligence = entry.Smart,
        Vitality = entry.Vit,
        Agility = entry.Agi,
        Charisma = entry.Au,
        Faith = entry.Be,
        Element = ContentFileJson.ToElement(entry.Element, $"怪物 {entry.Id}"),
        JobExperience = entry.JobExp,
        EncounterRate = entry.Meet,
        PlaceId = entry.PlaceId,
        SubPlaceIndexes = entry.SubPlaces,
        Drops = entry.Drops.Select(drop => new MonsterDropRecord { ItemId = drop.ItemId, DropRarity = drop.Topr }).ToList(),
        Skills = skills.Select(skill => new MonsterSkillRecord { SkillId = skill.SkillId, Level = skill.Level, ActivationRate = skill.Rate }).ToList(),
        ExclusiveMissionId = entry.MissionId,
        IsNpc = entry.Npc == true,
        NpcMessage = entry.NpcMessage,
    };

    private static MonsterEntry ToEntry(MonsterRecord record) => new()
    {
        Id = record.Id,
        Name = record.Name,
        Level = record.Level,
        Hp = record.HitPoints,
        Sp = record.SkillPoints,
        At = record.Attack,
        Mat = record.MagicAttack,
        Df = record.Defense,
        Mdf = record.MagicDefense,
        Agi = record.Agility,
        Au = record.Charisma,
        Element = (int)record.Element,
        JobExp = record.JobExperience,
        Meet = record.EncounterRate,
        PlaceId = record.PlaceId,
        SubPlaces = record.SubPlaceIndexes,
        Drops = record.Drops.Select(drop => new MonsterDropEntry { ItemId = drop.ItemId, Topr = drop.DropRarity }).ToList(),
        Str = record.Strength,
        Smart = record.Intelligence,
        Vit = record.Vitality,
        Be = record.Faith,
        MissionId = record.ExclusiveMissionId,
        Npc = record.IsNpc ? true : null,
        NpcMessage = record.NpcMessage,
    };

    // 屬性的宣告順序就是檔案的欄位順序
    private sealed record MonsterEntry
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public required int Level { get; init; }

        public required int Hp { get; init; }

        public required int Sp { get; init; }

        public required int At { get; init; }

        public required int Mat { get; init; }

        public required int Df { get; init; }

        public required int Mdf { get; init; }

        public required int Agi { get; init; }

        public required int Au { get; init; }

        public required int Element { get; init; }

        public required long JobExp { get; init; }

        public required int Meet { get; init; }

        public required int PlaceId { get; init; }

        public required IReadOnlyList<int> SubPlaces { get; init; }

        public required IReadOnlyList<MonsterDropEntry> Drops { get; init; }

        public required int Str { get; init; }

        public required int Smart { get; init; }

        public required int Vit { get; init; }

        public required int Be { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? MissionId { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Npc { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? NpcMessage { get; init; }
    }

    private sealed record MonsterDropEntry
    {
        public required string ItemId { get; init; }

        public required int Topr { get; init; }
    }

    private sealed record MonsterSkillEntry
    {
        public required string MonsterId { get; init; }

        public required string SkillId { get; init; }

        public required int Level { get; init; }

        public required int Rate { get; init; }
    }
}
