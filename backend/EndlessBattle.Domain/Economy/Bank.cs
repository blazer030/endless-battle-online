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
            reasons.Add(ReasonCodes.LevelTooLow);
        }

        if (amount <= 0)
        {
            reasons.Add(ReasonCodes.InvalidAmount);
        }
        else if (amount > character.Money)
        {
            reasons.Add(ReasonCodes.NotEnoughMoney);
        }

        return reasons;
    }

    // 領出不看等級；原因代碼：INVALID_AMOUNT、NOT_ENOUGH_BANK
    public static IReadOnlyList<string> CanWithdraw(Character character, long amount) =>
        amount <= 0 ? [ReasonCodes.InvalidAmount] : amount > character.Bank ? [ReasonCodes.NotEnoughBank] : [];
}
