using System.Text.RegularExpressions;

namespace EndlessBattle.Tests.Support;

// 原版匯入的紀錄 id 是「種類-原版編號」；其餘是保留的自訂內容（gaps.md「保留的自訂內容」）
public static partial class CustomContent
{
    public static bool IsCustom(string id) => !OriginalId().IsMatch(id);

    [GeneratedRegex(@"^(item|monster|job|skill|mission|recipe|honour|mercenary)-\d+$")]
    private static partial Regex OriginalId();
}
