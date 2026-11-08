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
}
