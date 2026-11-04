namespace EndlessBattle.Application.Admin;

// admin 可編輯的內容種類：對應的內容檔與頂層陣列名稱
public sealed record ContentKind(string Name, string FileName, string ArrayProperty)
{
    public static IReadOnlyList<ContentKind> All { get; } =
    [
        new("items", "items.json", "items"),
        new("monsters", "monsters.json", "monsters"),
        new("jobs", "jobs.json", "jobs"),
        new("skills", "skills.json", "skills"),
        new("areas", "world.json", "areas"),
        new("places", "world.json", "places"),
    ];

    public static ContentKind? Find(string name) => All.FirstOrDefault(kind => kind.Name == name);
}
