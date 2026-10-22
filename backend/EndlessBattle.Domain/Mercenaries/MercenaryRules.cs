using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Domain.Mercenaries;

public static class MercenaryRules
{
    // WOG 前端每 100 秒輪詢一次 fire_mercenary
    public const double FightIntervalSeconds = 100;
    public const int NameMaxLength = 8;

    // PHP round：.5 遠離零
    public static long Discount(long value, int percent) =>
        (long)Math.Round(value * (double)percent / 100, MidpointRounding.AwayFromZero);

    // PHP get_item：寶物% > 1 時分母 × (2 − 寶物% / 100)
    public static double DropDenominatorFactor(int dropPercent) => dropPercent > 1 ? 2 - dropPercent / 100.0 : 1;

    // 勳章所的傭兵（Price 為 null）沒有金錢價格，也一併回報 notEnoughMoney
    public static IReadOnlyList<string> CanHire(Character character, MercenaryDefinition? definition, MercenaryContract? current)
    {
        if (definition is null)
        {
            return ["mercenaryUnknown"];
        }

        var reasons = new List<string>();
        if (definition.Price is null)
        {
            reasons.Add("honourOnly");
        }

        if (current is not null)
        {
            reasons.Add("mercenaryHired");
        }

        if (definition.Price is not { } price || character.Money < price)
        {
            reasons.Add("notEnoughMoney");
        }

        return reasons;
    }

    // 僱用與勳章兌換共用：休息中、次數為目錄值
    public static MercenaryContract NewContract(MercenaryDefinition definition, DateTimeOffset hiredAt) =>
        new(definition.Id, definition.Name, definition.Fights, PlaceId: null, NextFightAt: null, hiredAt);

    public static IReadOnlyList<string> CanDeploy(PlaceDefinition? place) =>
        place is null ? ["placeUnknown"] : place.Restricted ? ["placeRestricted"] : [];

    // 去頭尾空白後 1～8 字（WOG 輸入框 maxlength 8）；以字碼計算
    public static IReadOnlyList<string> CanRename(string? name)
    {
        var length = name?.Trim().EnumerateRunes().Count() ?? 0;
        return length is >= 1 and <= NameMaxLength ? [] : ["nameInvalid"];
    }
}
