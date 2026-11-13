using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Inventory;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Rest;

namespace EndlessBattle.Application.Rest;

// Lodging：已到期或房型不在內容裡 → null
public sealed record InnView(InnDefinition Inn, int Level, Lodging? Lodging);

public sealed record InnBookResult(Character Character, Lodging Lodging, long Cost);

public sealed class InnService(
    ICharacterRepository characters,
    ILodgingRepository lodgings,
    IGameContent content,
    AdventureService adventure,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<InnView> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var lodging = await lodgings.FindAsync(character.Id, cancellationToken);
        var active = lodging is not null && lodging.EndsAt > clock.UtcNow && Inn.ToBuff(lodging, content.Inn) is not null;

        return new InnView(content.Inn, character.Level, active ? lodging : null);
    }

    // 房型加成只對入住之後開打的場次生效：先以舊的房型把經過的時間結算到現在（同喝密藥），
    // 結算中可能升級或自動住宿花錢，所以用結算後的角色重算價格再檢查一次；不夠就整筆回滾
    public Task<InnBookResult> BookAsync(long accountId, string roomId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var inn = content.Inn;
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var room = inn.Rooms.FirstOrDefault(candidate => candidate.Id == roomId)
                ?? throw new InputValidationException("沒有這個房間", ["unknownRoom"]);
            EnsureCanBook(character, Inn.Price(inn, room, character.Level));

            character = (await adventure.AdvanceAsync(accountId, cancellationToken)).Character;
            EnsureCanBook(character, Inn.Price(inn, room, character.Level));

            var booking = Inn.Book(character, inn, room, await lodgings.FindAsync(character.Id, cancellationToken), clock.UtcNow);
            var updated = await characters.UpdateAsync(booking.Character, cancellationToken);
            await lodgings.SaveAsync(updated.Id, booking.Booked, cancellationToken);
            return new InnBookResult(updated, booking.Booked, booking.Cost);
        }, cancellationToken);

    private static void EnsureCanBook(Character character, long price)
    {
        var reasons = Inn.CanBook(character, price);
        if (reasons.Count > 0)
        {
            throw new InputValidationException("錢不夠", reasons);
        }
    }

    private async Task<Character> FindCharacterAsync(long accountId, CancellationToken cancellationToken) =>
        await characters.FindByAccountAsync(accountId, cancellationToken) ?? throw new NotFoundException("尚未建立角色");
}
