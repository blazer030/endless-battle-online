using EndlessBattle.Domain.Rest;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class LodgingRepositoryTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset Moment = new DateTimeOffset(2026, 10, 29, 1, 2, 3, TimeSpan.Zero).AddTicks(1230);

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"旅館{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    [Fact]
    public async Task Find_NullWhenNeverBooked()
    {
        var characterId = await CreateCharacterAsync();

        await using var context = database.CreateDbContext();
        Assert.Null(await new LodgingRepository(context).FindAsync(characterId, CancellationToken));
    }

    // 整筆取代：房間與窗口都換成新的，仍然只有一列
    [Fact]
    public async Task Save_ReplacesExistingLodging()
    {
        var characterId = await CreateCharacterAsync();

        await using (var context = database.CreateDbContext())
        {
            var repository = new LodgingRepository(context);
            await repository.SaveAsync(characterId, new Lodging("apprentice-loft", Moment, Moment.AddHours(8)), CancellationToken);
            Assert.Equal(new Lodging("apprentice-loft", Moment, Moment.AddHours(8)), await repository.FindAsync(characterId, CancellationToken));
        }

        await using (var context = database.CreateDbContext())
        {
            await new LodgingRepository(context).SaveAsync(
                characterId, new Lodging("quiet-chapel", Moment.AddHours(2), Moment.AddHours(10)), CancellationToken);
        }

        await using var reader = database.CreateDbContext();
        Assert.Equal(
            new Lodging("quiet-chapel", Moment.AddHours(2), Moment.AddHours(10)),
            await new LodgingRepository(reader).FindAsync(characterId, CancellationToken));
        Assert.Equal(1, await reader.CharacterLodgings.CountAsync(row => row.CharacterId == characterId, CancellationToken));
    }

    private static readonly DateTimeOffset Day = new DateTimeOffset(2026, 10, 29, 0, 0, 0, TimeSpan.Zero).AddTicks(1230);

    private static Lodging Stay(string roomId, double fromHour, double toHour) => new(roomId, Day.AddHours(fromHour), Day.AddHours(toHour));

    private async Task SaveAsync(long characterId, Lodging booked, Lodging? ended, DateTimeOffset pruneBefore)
    {
        await using var context = database.CreateDbContext();
        await new LodgingRepository(context).SaveBookingAsync(characterId, booked, ended, pruneBefore, CancellationToken);
    }

    private async Task<IReadOnlyList<Lodging>> ListAsync(long characterId, DateTimeOffset after)
    {
        await using var context = database.CreateDbContext();
        return await new LodgingRepository(context).ListEndingAfterAsync(characterId, after, CancellationToken);
    }

    // 規格 inn：Switching rooms——鬥士通鋪 06:00–14:00，10:00 訂學徒閣樓：兩列都留著，舊列截到 10:00
    [Fact]
    public async Task SaveBooking_KeepsBothStays()
    {
        var characterId = await CreateCharacterAsync();
        await SaveAsync(characterId, Stay("fighters-bunk", 6, 14), null, Day.AddDays(-1));

        await SaveAsync(characterId, Stay("apprentice-loft", 10, 18), Stay("fighters-bunk", 6, 10), Day.AddDays(-1));

        Assert.Equal([Stay("fighters-bunk", 6, 10), Stay("apprentice-loft", 10, 18)], await ListAsync(characterId, Day));
    }

    // 截斷只動入住時還沒到期的那一列（以開始時刻對上），其他列不動
    [Fact]
    public async Task SaveBooking_EndsThePreviousStay()
    {
        var characterId = await CreateCharacterAsync();
        await SaveAsync(characterId, Stay("quiet-chapel", 1, 5), null, Day.AddDays(-1));
        await SaveAsync(characterId, Stay("fighters-bunk", 6, 14), null, Day.AddDays(-1));

        await SaveAsync(characterId, Stay("fighters-bunk", 10, 18), Stay("fighters-bunk", 6, 10), Day.AddDays(-1));

        Assert.Equal(
            [Stay("quiet-chapel", 1, 5), Stay("fighters-bunk", 6, 10), Stay("fighters-bunk", 10, 18)],
            await ListAsync(characterId, Day));
    }

    // 規格 inn：Pruning at booking time——第 2 天 10:00 入住：第 1 天 09:59 結束的刪掉、10:30 結束的留著
    [Fact]
    public async Task SaveBooking_PrunesStaysEndedADayBefore()
    {
        var characterId = await CreateCharacterAsync();
        var endedEarly = new Lodging("quiet-chapel", Day.AddHours(2), Day.AddHours(9).AddMinutes(59));
        var endedLate = new Lodging("bulwark-room", Day.AddHours(9).AddMinutes(59), Day.AddHours(10).AddMinutes(30));
        await SaveAsync(characterId, endedEarly, null, Day.AddDays(-1));
        await SaveAsync(characterId, endedLate, null, Day.AddDays(-1));

        await SaveAsync(characterId, Stay("apprentice-loft", 34, 42), null, Day.AddHours(10));

        Assert.Equal([endedLate, Stay("apprentice-loft", 34, 42)], await ListAsync(characterId, Day.AddDays(-1)));
    }

    // 目前生效的是結束時刻晚於現在的那列；都結束了就是 null
    [Fact]
    public async Task FindActive_ReturnsTheStayEndingAfterNow()
    {
        var characterId = await CreateCharacterAsync();
        await SaveAsync(characterId, Stay("fighters-bunk", 6, 10), null, Day.AddDays(-1));
        await SaveAsync(characterId, Stay("apprentice-loft", 10, 18), null, Day.AddDays(-1));

        await using var context = database.CreateDbContext();
        var repository = new LodgingRepository(context);
        Assert.Equal(Stay("fighters-bunk", 6, 10), await repository.FindActiveAsync(characterId, Day.AddHours(9), CancellationToken));
        Assert.Equal(Stay("apprentice-loft", 10, 18), await repository.FindActiveAsync(characterId, Day.AddHours(11), CancellationToken));
        Assert.Null(await repository.FindActiveAsync(characterId, Day.AddHours(18), CancellationToken));
    }

    // 只列結束時刻晚於 after 的，依開始時刻排序
    [Fact]
    public async Task ListEndingAfter_OrdersByStart()
    {
        var characterId = await CreateCharacterAsync();
        await SaveAsync(characterId, Stay("apprentice-loft", 10, 18), null, Day.AddDays(-1));
        await SaveAsync(characterId, Stay("fighters-bunk", 6, 10), null, Day.AddDays(-1));
        await SaveAsync(characterId, Stay("quiet-chapel", 1, 5), null, Day.AddDays(-1));

        Assert.Equal([Stay("fighters-bunk", 6, 10), Stay("apprentice-loft", 10, 18)], await ListAsync(characterId, Day.AddHours(5)));
    }
}
