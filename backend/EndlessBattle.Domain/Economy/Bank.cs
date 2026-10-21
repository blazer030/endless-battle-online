using EndlessBattle.Domain.Characters;

namespace EndlessBattle.Domain.Economy;

public static class Bank
{
    public const int MinimumLevel = 15;

    // 原因代碼：levelTooLow、invalidAmount、notEnoughMoney
    public static IReadOnlyList<string> CanDeposit(Character character, long amount)
    {
        var reasons = new List<string>();
        if (character.Level < MinimumLevel)
        {
            reasons.Add("levelTooLow");
        }

        if (amount <= 0)
        {
            reasons.Add("invalidAmount");
        }
        else if (amount > character.Money)
        {
            reasons.Add("notEnoughMoney");
        }

        return reasons;
    }

    // 領出不看等級；原因代碼：invalidAmount、notEnoughBank
    public static IReadOnlyList<string> CanWithdraw(Character character, long amount) =>
        amount <= 0 ? ["invalidAmount"] : amount > character.Bank ? ["notEnoughBank"] : [];
}
