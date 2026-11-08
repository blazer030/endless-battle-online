using System.Text.Json.Nodes;
using EndlessBattle.Application.Admin.Content;

namespace EndlessBattle.Infrastructure.Content;

// missions.json → 任務摘要，只讀不寫；其他欄位不屬於這份紀錄，所以不用嚴格讀取
public sealed class MissionSummaryCodec : IContentCodec<MissionSummaryRecord>
{
    private const string MissionsFile = "missions.json";

    public IReadOnlyList<string> FileNames { get; } = [MissionsFile];

    public IReadOnlyList<MissionSummaryRecord> Read(IReadOnlyDictionary<string, string> files)
    {
        var missions = ContentFileJson.ParseRoot(files[MissionsFile], MissionsFile)["missions"] as JsonArray
            ?? throw new GameContentException($"{MissionsFile} 沒有 missions 陣列");
        return missions.Select(mission => new MissionSummaryRecord
        {
            Id = mission!["id"]!.GetValue<string>(),
            Title = mission["title"]!.GetValue<string>(),
            Daily = mission["daily"]!.GetValue<bool>(),
        }).ToList();
    }

    public IReadOnlyDictionary<string, string> Write(IReadOnlyList<MissionSummaryRecord> records, IReadOnlyDictionary<string, string> files) =>
        throw new NotSupportedException("任務不開放編輯");
}
