using System.Text.RegularExpressions;

namespace EndlessBattle.Tests.Support;

// 原版匯入的紀錄 id 是「種類-原版編號」；匯入的擴充期間，釘住自訂內容表的測試只看自訂紀錄
public static partial class CustomContent
{
    public static bool IsCustom(string id) => !OriginalId().IsMatch(id);

    [GeneratedRegex(@"^(item|monster|job|skill|mission|recipe|honour|mercenary)-\d+$")]
    private static partial Regex OriginalId();
}
