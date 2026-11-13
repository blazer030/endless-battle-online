using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Rest;

namespace EndlessBattle.Application.Rest;

// Lodging：已到期或房型不在內容裡 → null；RestFee：自動住宿一次的最低費用
public sealed record InnView(InnDefinition Inn, int Level, Lodging? Lodging, long RestFee);

public sealed record InnBookResult(Character Character, Lodging Lodging, long Cost);

public sealed class InnService(
    ICharacterRepository characters,
    ILodgingRepository lodgings,
    IGameContent content,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<InnView> GetAsync(long accountId, CancellationToken cancellationToken)
    {
        var character = await FindCharacterAsync(accountId, cancellationToken);
        var lodging = await lodgings.FindActiveAsync(character.Id, clock.UtcNow, cancellationToken);
        var known = lodging is not null && Inn.ToBuff(lodging, content.Inn) is not null;

        return new InnView(content.Inn, character.Level, known ? lodging : null, Hotel.MinimumCost(character.Level));
    }

    // 不結算冒險時間：每筆住宿各有時段，之後結算時每場依開打時刻找所在那筆；價格用存檔的等級
    public Task<InnBookResult> BookAsync(long accountId, string roomId, CancellationToken cancellationToken) =>
        unitOfWork.RunAsync(async cancellationToken =>
        {
            var inn = content.Inn;
            var character = await FindCharacterAsync(accountId, cancellationToken);
            var room = inn.Rooms.FirstOrDefault(candidate => candidate.Id == roomId)
                ?? throw new InputValidationException("沒有這個房間", ["unknownRoom"]);
            EnsureCanBook(character, Inn.Price(inn, room, character.Level));

            var now = clock.UtcNow;
            var booking = Inn.Book(character, inn, room, await lodgings.FindActiveAsync(character.Id, now, cancellationToken), now);
            var updated = await characters.UpdateAsync(booking.Character, cancellationToken);
            await lodgings.SaveBookingAsync(updated.Id, booking.Booked, booking.Ended, Inn.PruneBefore(now), cancellationToken);
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
