using EndlessBattle.Domain.Items;

namespace EndlessBattle.Infrastructure;

// 密藥種類在內容檔與資料庫裡的代碼，是儲存格式、與 Domain 的命名脫鉤；讀取不分大小寫
public static class BuffKindCodes
{
    private static readonly IReadOnlyDictionary<BuffKind, string> Codes = new Dictionary<BuffKind, string>
    {
        [BuffKind.Experience] = "Exp",
        [BuffKind.JobExperience] = "JobExp",
        [BuffKind.Attack] = "Attack",
        [BuffKind.Magic] = "Magic",
        [BuffKind.Defense] = "Defense",
        [BuffKind.MagicDefense] = "MagicDefense",
    };

    private static readonly IReadOnlyDictionary<string, BuffKind> Kinds =
        Codes.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public static string Of(BuffKind kind) => Codes[kind];

    public static bool TryParse(string? code, out BuffKind kind) => Kinds.TryGetValue(code ?? "", out kind);

    // 資料庫裡出現不認識的代碼表示資料損毀，不靜默略過
    public static BuffKind Parse(string code) =>
        TryParse(code, out var kind) ? kind : throw new InvalidOperationException($"無法辨識的密藥種類代碼：{code}");
}
