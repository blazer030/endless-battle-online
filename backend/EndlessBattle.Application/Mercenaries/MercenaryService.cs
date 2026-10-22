using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Mercenaries;

namespace EndlessBattle.Application.Mercenaries;

public sealed record MercenaryOverview(IReadOnlyList<MercenaryDefinition> Catalogue, MercenaryContract? Contract);

// 每個寫入型用例一筆交易：讀取與驗證 → 角色版本鎖（僱用時一併扣錢）→ 寫合約。
// 沒有合約時 Deploy／Rest／Rename／Dismiss 一律 noMercenary
public sealed class MercenaryService(
    ICharacterRepository characters,
    IMercenaryRepository mercenaries,
    IGameContent content,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    public async Task<MercenaryOverview> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        return Overview(await mercenaries.FindAsync(character.Id, cancellationToken));
    }

    public Task<MercenaryOverview> HireAsync(long accountId, string mercenaryId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var definition = content.AllMercenaries.FirstOrDefault(mercenary => mercenary.Id == mercenaryId);
            Check(MercenaryRules.CanHire(character, definition, await mercenaries.FindAsync(character.Id, cancellationToken)), "無法僱用");

            var contract = new MercenaryContract(definition!.Id, definition.Name, definition.Fights, PlaceId: null, NextFightAt: null, clock.UtcNow);
            await characters.UpdateAsync(character with { Money = character.Money - definition.Price!.Value }, cancellationToken);
            await mercenaries.AddAsync(character.Id, contract, cancellationToken);
            return Overview(contract);
        }, cancellationToken);

    // 已在出擊中也重新排程：尚未結算的場次不補
    public Task<MercenaryOverview> DeployAsync(long accountId, int placeId, CancellationToken cancellationToken) =>
        WriteAsync(accountId, contract =>
        {
            Check(MercenaryRules.CanDeploy(content.AllPlaces.FirstOrDefault(place => place.Id == placeId)), "無法出擊");
            return contract with { PlaceId = placeId, NextFightAt = clock.UtcNow.AddSeconds(MercenaryRules.FightIntervalSeconds) };
        }, cancellationToken);

    public Task<MercenaryOverview> RestAsync(long accountId, CancellationToken cancellationToken) =>
        WriteAsync(accountId, contract => contract with { PlaceId = null, NextFightAt = null }, cancellationToken);

    public Task<MercenaryOverview> RenameAsync(long accountId, string? name, CancellationToken cancellationToken) =>
        WriteAsync(accountId, contract =>
        {
            Check(MercenaryRules.CanRename(name), "名字必須是 1～8 個字");
            return contract with { Name = name!.Trim() };
        }, cancellationToken);

    public Task<MercenaryOverview> DismissAsync(long accountId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            await FindContractAsync(character, cancellationToken);
            await characters.UpdateAsync(character, cancellationToken);
            await mercenaries.RemoveAsync(character.Id, cancellationToken);
            return Overview(null);
        }, cancellationToken);

    private Task<MercenaryOverview> WriteAsync(
        long accountId, Func<MercenaryContract, MercenaryContract> change, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var changed = change(await FindContractAsync(character, cancellationToken));
            await characters.UpdateAsync(character, cancellationToken);
            await mercenaries.UpdateAsync(character.Id, changed, cancellationToken);
            return Overview(changed);
        }, cancellationToken);

    private MercenaryOverview Overview(MercenaryContract? contract) => new(content.AllMercenaries, contract);

    private async Task<MercenaryContract> FindContractAsync(Character character, CancellationToken cancellationToken) =>
        await mercenaries.FindAsync(character.Id, cancellationToken) ?? throw new InputValidationException("沒有僱用傭兵", ["noMercenary"]);

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
