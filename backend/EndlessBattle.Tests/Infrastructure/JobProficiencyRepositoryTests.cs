using EndlessBattle.Domain.Progression;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class JobProficiencyRepositoryTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"熟練{Random.Shared.Next(100000, 999999)}", "stage", now), CancellationToken);
        return character.Id;
    }

    [Fact]
    public async Task AddAsync_InsertsThenAccumulatesDeltas()
    {
        var characterId = await CreateCharacterAsync();

        foreach (var (proficiency, skillPoints) in new[] { (0L, 0L), (12L, 1L), (18L, 2L) })
        {
            await using var context = database.CreateDbContext();
            await new JobProficiencyRepository(context).AddAsync(characterId, "warrior", proficiency, skillPoints, CancellationToken);
        }

        await using var readContext = database.CreateDbContext();
        var stored = await new JobProficiencyRepository(readContext).ListAsync(characterId, CancellationToken);
        Assert.Equal([new JobProficiency("warrior", 30, 3)], stored);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmpty_WhenNoProficiency()
    {
        var characterId = await CreateCharacterAsync();

        await using var context = database.CreateDbContext();
        var stored = await new JobProficiencyRepository(context).ListAsync(characterId, CancellationToken);

        Assert.Empty(stored);
    }

    [Fact]
    public async Task ListAsync_OrdersByJobId_AndOnlyReturnsOwnRows()
    {
        var characterId = await CreateCharacterAsync();
        var otherCharacterId = await CreateCharacterAsync();
        await using (var context = database.CreateDbContext())
        {
            var repository = new JobProficiencyRepository(context);
            await repository.AddAsync(characterId, "warrior", 5, 0, CancellationToken);
            await repository.AddAsync(characterId, "sorcerer", 7, 1, CancellationToken);
            await repository.AddAsync(otherCharacterId, "berserker", 9, 2, CancellationToken);
        }

        await using var readContext = database.CreateDbContext();
        var stored = await new JobProficiencyRepository(readContext).ListAsync(characterId, CancellationToken);

        Assert.Equal([new JobProficiency("sorcerer", 7, 1), new JobProficiency("warrior", 5, 0)], stored);
    }
}
