using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Economy;

public static class Bank
{
    public const int MinimumLevel = 15;

    // 原因代碼：LEVEL_TOO_LOW、INVALID_AMOUNT、NOT_ENOUGH_MONEY
    public static IReadOnlyList<string> CanDeposit(Character character, long amount)
    {
        var reasons = new List<string>();
        if (character.Level < MinimumLevel)
        {
            reasons.Add("LEVEL_TOO_LOW");
        }

        if (amount <= 0)
        {
            reasons.Add("INVALID_AMOUNT");
        }
        else if (amount > character.Money)
        {
            reasons.Add("NOT_ENOUGH_MONEY");
        }

        return reasons;
    }

    // 領出不看等級；原因代碼：INVALID_AMOUNT、NOT_ENOUGH_BANK
    public static IReadOnlyList<string> CanWithdraw(Character character, long amount) =>
        amount <= 0 ? ["INVALID_AMOUNT"] : amount > character.Bank ? ["NOT_ENOUGH_BANK"] : [];
}
