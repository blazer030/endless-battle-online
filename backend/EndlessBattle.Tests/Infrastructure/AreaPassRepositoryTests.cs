using EndlessBattle.Domain.World;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class AreaPassRepositoryTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset Moment = new DateTimeOffset(2026, 10, 22, 1, 2, 3, TimeSpan.Zero).AddTicks(1230);

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"禁區{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    // 同地區第二次寫入覆蓋到期時刻；別的地區另存一列，列表依地區 id
    [Fact]
    public async Task Save_OverwritesSamePlace()
    {
        var characterId = await CreateCharacterAsync();

        await using (var context = database.CreateDbContext())
        {
            var repository = new AreaPassRepository(context);
            Assert.Null(await repository.FindAsync(characterId, 18, CancellationToken));
            await repository.SaveAsync(characterId, new AreaPass(18, Moment), CancellationToken);
            await repository.SaveAsync(characterId, new AreaPass(17, Moment.AddHours(1)), CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            await new AreaPassRepository(context).SaveAsync(characterId, new AreaPass(18, Moment.AddHours(6)), CancellationToken);
        }

        await using var reader = database.CreateDbContext();
        var passes = new AreaPassRepository(reader);
        Assert.Equal(new AreaPass(18, Moment.AddHours(6)), await passes.FindAsync(characterId, 18, CancellationToken));
        Assert.Equal(
            [new AreaPass(17, Moment.AddHours(1)), new AreaPass(18, Moment.AddHours(6))],
            await passes.ListAsync(characterId, CancellationToken));
        Assert.Equal(2, await reader.CharacterAreaPasses.CountAsync(row => row.CharacterId == characterId, CancellationToken));
    }
}
