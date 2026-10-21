using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Economy;

namespace EndlessBattle.Application.Economy;

public sealed class BankService(ICharacterRepository characters)
{
    public async Task<Character> DepositAsync(long accountId, long amount, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        Check(Bank.CanDeposit(character, amount), "無法存款");
        return await characters.UpdateAsync(character with { Money = character.Money - amount, Bank = character.Bank + amount }, cancellationToken);
    }

    public async Task<Character> WithdrawAsync(long accountId, long amount, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        Check(Bank.CanWithdraw(character, amount), "無法提款");
        return await characters.UpdateAsync(character with { Money = character.Money + amount, Bank = character.Bank - amount }, cancellationToken);
    }

    private static void Check(IReadOnlyList<string> reasons, string message)
    {
        if (reasons.Count > 0)
        {
            throw new InputValidationException(message, reasons);
        }
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
