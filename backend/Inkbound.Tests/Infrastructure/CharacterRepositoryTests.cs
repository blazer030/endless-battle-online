using Inkbound.Application.Common;
using Inkbound.Domain.Characters;
using Inkbound.Infrastructure.Persistence;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Infrastructure;

public class CharacterRepositoryTests(MySqlDatabaseFixture database)
{
    [Fact]
    public async Task UpdateAsync_RejectsStaleVersion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        await using var setupContext = database.CreateDbContext();
        var accountId = await new AccountRepository(setupContext).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        await new CharacterRepository(setupContext).AddAsync(
            new Character(0, accountId, $"樂觀{Random.Shared.Next(100000, 999999)}", 0, 0, "stage", now, 0), cancellationToken);

        await using var contextA = database.CreateDbContext();
        await using var contextB = database.CreateDbContext();
        var repositoryA = new CharacterRepository(contextA);
        var repositoryB = new CharacterRepository(contextB);
        var snapshotA = (await repositoryA.FindByAccountAsync(accountId, cancellationToken))!;
        var snapshotB = (await repositoryB.FindByAccountAsync(accountId, cancellationToken))!;

        var updated = await repositoryA.UpdateAsync(snapshotA with { SpiritStones = 10 }, cancellationToken);
        Assert.Equal(1, updated.Version);
        Assert.Equal(10, updated.SpiritStones);

        await Assert.ThrowsAsync<ConflictException>(() => repositoryB.UpdateAsync(snapshotB with { SpiritStones = 20 }, cancellationToken));
    }
}
