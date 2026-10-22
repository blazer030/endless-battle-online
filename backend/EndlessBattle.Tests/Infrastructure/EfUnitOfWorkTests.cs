using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class EfUnitOfWorkTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        return await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"交易{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
    }

    private async Task<IReadOnlyList<InventoryEntry>> ListInventoryAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new InventoryRepository(context).ListAsync(characterId, CancellationToken);
    }

    private static InventoryEntry Plain(string itemId) => new(0, itemId, 1, 0, StatBonus.Zero, [], 0);

    // 同一筆交易裡先寫背包、再撞上版本衝突（別的請求已先更新角色）：背包的寫入跟著回滾
    [Fact]
    public async Task Transaction_RollsBackInventoryWhenCharacterUpdateConflicts()
    {
        var character = await CreateCharacterAsync();
        await using (var otherRequest = database.CreateDbContext())
        {
            await new CharacterRepository(otherRequest).UpdateAsync(character, CancellationToken);
        }

        await using var context = database.CreateDbContext();
        var inventory = new InventoryRepository(context);
        var characters = new CharacterRepository(context);

        await Assert.ThrowsAsync<ConflictException>(() => new EfUnitOfWork(context).RunAsync(async cancellationToken =>
        {
            await inventory.AddAsync(character.Id, Plain("sword"), cancellationToken);
            return await characters.UpdateAsync(character, cancellationToken);
        }, CancellationToken));

        Assert.Empty(await ListInventoryAsync(character.Id));
    }

    // 巢狀呼叫沿用外層交易：內層正常結束不會提前提交，外層失敗時一起回滾
    [Fact]
    public async Task Transaction_NestedCallJoinsOuterTransaction()
    {
        var character = await CreateCharacterAsync();
        await using var context = database.CreateDbContext();
        IUnitOfWork unitOfWork = new EfUnitOfWork(context);
        var inventory = new InventoryRepository(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.RunAsync(async cancellationToken =>
        {
            await unitOfWork.RunAsync(token => inventory.AddAsync(character.Id, Plain("sword"), token), cancellationToken);
            throw new InvalidOperationException("外層失敗");
        }, CancellationToken));

        Assert.Empty(await ListInventoryAsync(character.Id));
    }

    [Fact]
    public async Task Transaction_CommitsWhenWorkSucceeds()
    {
        var character = await CreateCharacterAsync();
        await using var context = database.CreateDbContext();
        var inventory = new InventoryRepository(context);

        await new EfUnitOfWork(context).RunAsync(cancellationToken => inventory.AddAsync(character.Id, Plain("sword"), cancellationToken), CancellationToken);

        Assert.Single(await ListInventoryAsync(character.Id));
    }
}
