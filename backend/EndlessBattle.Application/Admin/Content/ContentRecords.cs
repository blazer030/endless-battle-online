using System.Text.Json.Serialization;

namespace EndlessBattle.Application.Admin.Content;

// admin API 的內容紀錄：欄位一律用完整名稱，內容檔的縮寫只存在 Infrastructure 的 codec
public interface IContentRecord
{
    // 路由用的 id；地區的 id 是數字，一律以文字比對
    [JsonIgnore]
    string RecordId { get; }
}

// 數值與內容檔的屬性編號相同（0 無屬性；地 1 → 水 2 → 火 3 → 木 4 → 風 5 → 毒 6）
public enum ContentElement
{
    None = 0,
    Earth = 1,
    Water = 2,
    Fire = 3,
    Wood = 4,
    Wind = 5,
    Poison = 6,
}

// 400、409 的回應本體：Field 是出問題的欄位路徑，例如 skills[0].skillId
public sealed record ContentFieldError(string Field, string Message);

// 七項能力；列舉值與內容檔的縮寫對照在 Infrastructure 的 ContentFileJson
public enum ContentStat
{
    Strength,
    Intelligence,
    Life,
    Vitality,
    Agility,
    Charisma,
    Faith,
}

public sealed record ContentRange
{
    public required int Minimum { get; init; }

    public required int Maximum { get; init; }
}
