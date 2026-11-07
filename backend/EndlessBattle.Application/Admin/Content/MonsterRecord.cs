using System.Text.Json.Serialization;

namespace EndlessBattle.Application.Admin.Content;

public sealed record MonsterRecord : IContentRecord
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required int Level { get; init; }

    public required int HitPoints { get; init; }

    public required int SkillPoints { get; init; }

    public required int Attack { get; init; }

    public required int MagicAttack { get; init; }

    public required int Defense { get; init; }

    public required int MagicDefense { get; init; }

    public required int Strength { get; init; }

    public required int Intelligence { get; init; }

    public required int Vitality { get; init; }

    public required int Agility { get; init; }

    public required int Charisma { get; init; }

    public required int Faith { get; init; }

    public required ContentElement Element { get; init; }

    public required long JobExperience { get; init; }

    public required int EncounterRate { get; init; }

    // 0 是全域怪
    public required int PlaceId { get; init; }

    // 空的代表該地區每個子地區都會出現
    public required IReadOnlyList<int> SubPlaceIndexes { get; init; }

    public required IReadOnlyList<MonsterDropRecord> Drops { get; init; }

    public required IReadOnlyList<MonsterSkillRecord> Skills { get; init; }

    public required string? ExclusiveMissionId { get; init; }

    public required bool IsNpc { get; init; }

    public required string? NpcMessage { get; init; }

    [JsonIgnore]
    public string RecordId => Id;
}

public sealed record MonsterDropRecord
{
    public required string ItemId { get; init; }

    // 越大越難掉
    public required int DropRarity { get; init; }
}

public sealed record MonsterSkillRecord
{
    public required string SkillId { get; init; }

    public required int Level { get; init; }

    public required int ActivationRate { get; init; }
}

// 唯讀：只給怪物的「限定任務」選單用，任務本身不開放編輯
public sealed record MissionSummaryRecord : IContentRecord
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required bool Daily { get; init; }

    [JsonIgnore]
    public string RecordId => Id;
}
