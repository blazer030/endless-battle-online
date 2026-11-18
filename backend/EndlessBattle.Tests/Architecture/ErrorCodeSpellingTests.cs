using System.Reflection;
using System.Text.RegularExpressions;
using EndlessBattle.Domain;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Architecture;

// 原因代碼只寫在 Domain 的 ErrorCodes.cs；其他後端原始碼一律用常數，不寫代碼字串。
// 掃每個字串字面值：等於某個代碼、以「代碼:」開頭，或是全大寫含底線的字串，都算散寫。
// scaffold 產生的 Persistence/Models 與 EndlessBattleDbContext.cs 不手改，不掃
public partial class ErrorCodeSpellingTests
{
    [GeneratedRegex(@"""((?:[^""\\\r\n]|\\.)*)""")]
    private static partial Regex StringLiteral();

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+(:.*)?$")]
    private static partial Regex UpperSnakeWithUnderscore();

    private static readonly string[] Codes = typeof(ErrorCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToArray();

    private static bool LooksLikeCode(string literal) =>
        UpperSnakeWithUnderscore().IsMatch(literal)
        || Codes.Any(code => literal == code || literal.StartsWith(code + ":", StringComparison.Ordinal));

    [Fact]
    public void ErrorCodesAreWrittenOnlyInErrorCodes()
    {
        string[] folders = ["EndlessBattle.Domain", "EndlessBattle.Application", "EndlessBattle.Infrastructure", "EndlessBattle.Api"];
        var separator = Path.DirectorySeparatorChar;
        var offenders = folders
            .SelectMany(folder => Directory.EnumerateFiles(Path.Combine(BackendPaths.BackendDirectory, folder), "*.cs", SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{separator}obj{separator}") && !file.Contains($"{separator}bin{separator}"))
            .Where(file => !file.Contains($"{separator}Persistence{separator}Models{separator}"))
            .Where(file => Path.GetFileName(file) is not ("ErrorCodes.cs" or "EndlessBattleDbContext.cs"))
            .SelectMany(file => File.ReadLines(file)
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .SelectMany(line => StringLiteral().Matches(line).Select(match => match.Groups[1].Value))
                .Where(LooksLikeCode)
                .Select(literal => $"{Path.GetFileName(file)}: {literal}"))
            .ToList();

        Assert.Empty(offenders);
    }
}
