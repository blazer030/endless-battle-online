using System.Text.RegularExpressions;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Architecture;

// spec game-api-contract：原因代碼一律全大寫底線。代碼散寫在 Domain、Application、Persistence 的字串裡，
// 只靠樣本請求會漏（倉庫的 STACK_LIMIT 就漏過），所以直接掃原始碼裡放代碼的位置：
// Rejected("…")、.Add("…")、清單開頭 ["…"]；只看英文字母組成的字串（中文訊息、內容 id 不在這些位置）
public partial class ReasonCodeSpellingTests
{
    [GeneratedRegex(@"(?:Rejected|\.Add)\(""([A-Za-z_]+)(?::[^""]*)?""|\[\s*""([A-Za-z_]+)(?::[^""]*)?""")]
    private static partial Regex CodePosition();

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$")]
    private static partial Regex UpperSnake();

    [Fact]
    public void ReasonCodesInSourceAreUpperSnakeCase()
    {
        string[] folders = ["EndlessBattle.Domain", "EndlessBattle.Application", Path.Combine("EndlessBattle.Infrastructure", "Persistence")];
        var offenders = folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(BackendPaths.BackendDirectory, folder), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(file => CodePosition().Matches(File.ReadAllText(file))
                .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value)
                .Where(code => !UpperSnake().IsMatch(code))
                .Select(code => $"{Path.GetFileName(file)}: {code}"))
            .ToList();

        Assert.Empty(offenders);
    }
}
