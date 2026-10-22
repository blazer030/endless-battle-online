using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class MissionRepositoryTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"任務{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    private static MissionRecord Active(string missionId, DateOnly? dailyPeriod = null) =>
        new(missionId, MissionState.Active, 0, dailyPeriod, new DateTimeOffset(2026, 10, 22, 1, 2, 3, TimeSpan.Zero).AddTicks(1230), null);

    private async Task AddAsync(long characterId, MissionRecord record)
    {
        await using var context = database.CreateDbContext();
        await new MissionRepository(context).AddAsync(characterId, record, CancellationToken);
    }

    private async Task<IReadOnlyList<MissionRecord>> ListAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new MissionRepository(context).ListAsync(characterId, CancellationToken);
    }

    [Fact]
    public async Task Missions_RoundTrip()
    {
        var characterId = await CreateCharacterAsync();
        var record = Active("daily_hunt", new DateOnly(2026, 10, 22));

        await AddAsync(characterId, record);

        var stored = Assert.Single(await ListAsync(characterId));
        Assert.Equal(record, stored);
        Assert.Equal(TimeSpan.Zero, stored.AcceptedAt.Offset);
    }

    [Fact]
    public async Task Missions_RejectDuplicate()
    {
        var characterId = await CreateCharacterAsync();
        await AddAsync(characterId, Active("m1"));

        await Assert.ThrowsAsync<ConflictException>(() => AddAsync(characterId, Active("m1")));
    }

    [Fact]
    public async Task Missions_UpdateAndCompleteScopedToCharacter()
    {
        var characterId = await CreateCharacterAsync();
        var otherId = await CreateCharacterAsync();
        await AddAsync(characterId, Active("m1"));
        await AddAsync(otherId, Active("m1"));
        var completedAt = new DateTimeOffset(2026, 10, 23, 4, 5, 6, TimeSpan.Zero).AddTicks(450);

        await using (var context = database.CreateDbContext())
        {
            var repository = new MissionRepository(context);
            await repository.UpdateProgressAsync(characterId, "m1", 7, CancellationToken);
            await repository.CompleteAsync(characterId, "m1", completedAt, CancellationToken);
            await repository.UpdateProgressAsync(characterId, "missing", 1, CancellationToken);
        }

        var changed = Assert.Single(await ListAsync(characterId));
        Assert.Equal((MissionState.Completed, 7, completedAt), (changed.State, changed.Progress, changed.CompletedAt));
        var untouched = Assert.Single(await ListAsync(otherId));
        Assert.Equal((MissionState.Active, 0, (DateTimeOffset?)null), (untouched.State, untouched.Progress, untouched.CompletedAt));
    }

    [Fact]
    public async Task Missions_RemoveThenAddAgain()
    {
        var characterId = await CreateCharacterAsync();
        await AddAsync(characterId, Active("m1"));

        await using (var context = database.CreateDbContext())
        {
            await new MissionRepository(context).RemoveAsync(characterId, "m1", CancellationToken);
        }

        Assert.Empty(await ListAsync(characterId));
        await AddAsync(characterId, Active("m1"));
        Assert.Single(await ListAsync(characterId));
    }
}
