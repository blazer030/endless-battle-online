using EndlessBattle.Application.Common;
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

    // 多個請求同時寫入同一列時，每筆差值都要累加進去
    [Fact]
    public async Task AddAsync_AccumulatesConcurrentDeltas()
    {
        var characterId = await CreateCharacterAsync();

        await Task.WhenAll(Enumerable.Range(0, 10).Select(async _ =>
        {
            await using var context = database.CreateDbContext();
            await new JobProficiencyRepository(context).AddAsync(characterId, "warrior", 3, 1, CancellationToken);
        }));

        await using var readContext = database.CreateDbContext();
        var stored = await new JobProficiencyRepository(readContext).ListAsync(characterId, CancellationToken);
        Assert.Equal([new JobProficiency("warrior", 30, 10)], stored);
    }

    // 最小重現：同一個 context 先寫過一次，另一個 context 再累加；第一個 context 不可拿手上的舊值覆寫
    [Fact]
    public async Task AddAsync_KeepsDeltasWrittenByAnotherContext()
    {
        var characterId = await CreateCharacterAsync();
        await using var first = database.CreateDbContext();
        await using var second = database.CreateDbContext();

        await new JobProficiencyRepository(first).AddAsync(characterId, "warrior", 5, 1, CancellationToken);
        await new JobProficiencyRepository(second).AddAsync(characterId, "warrior", 7, 1, CancellationToken);
        await new JobProficiencyRepository(first).AddAsync(characterId, "warrior", 1, 1, CancellationToken);

        await using var readContext = database.CreateDbContext();
        var stored = await new JobProficiencyRepository(readContext).ListAsync(characterId, CancellationToken);
        Assert.Equal([new JobProficiency("warrior", 13, 3)], stored);
    }

    [Fact]
    public async Task SpendSkillPoints_DeductsOnlySkillPoints()
    {
        var characterId = await CreateCharacterAsync();
        await using var context = database.CreateDbContext();
        var repository = new JobProficiencyRepository(context);
        await repository.AddAsync(characterId, "warrior", 40, 300, CancellationToken);

        await repository.SpendSkillPointsAsync(characterId, "warrior", 300, CancellationToken);

        Assert.Equal([new JobProficiency("warrior", 40, 0)], await repository.ListAsync(characterId, CancellationToken));
    }

    // 讀取後被別的請求花掉時，條件式更新不扣成負數，也不動到別的職業或沒有紀錄的職業
    [Fact]
    public async Task SpendSkillPoints_RefusesWhenInsufficient()
    {
        var characterId = await CreateCharacterAsync();
        await using var context = database.CreateDbContext();
        var repository = new JobProficiencyRepository(context);
        await repository.AddAsync(characterId, "warrior", 40, 299, CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() => repository.SpendSkillPointsAsync(characterId, "warrior", 300, CancellationToken));
        await Assert.ThrowsAsync<ConflictException>(() => repository.SpendSkillPointsAsync(characterId, "thief", 1, CancellationToken));

        Assert.Equal([new JobProficiency("warrior", 40, 299)], await repository.ListAsync(characterId, CancellationToken));
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
