using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

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
        return await new InventoryRepository(context, TestGameContent.Instance).ListAsync(characterId, CancellationToken);
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
        var inventory = new InventoryRepository(context, TestGameContent.Instance);
        var characters = new CharacterRepository(context);

        await Assert.ThrowsAsync<ConflictException>(() => new EfUnitOfWork(context).RunAsync(async cancellationToken =>
        {
            await inventory.AddAsync(character.Id, Plain("test-sword"), cancellationToken);
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
        var inventory = new InventoryRepository(context, TestGameContent.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.RunAsync(async cancellationToken =>
        {
            await unitOfWork.RunAsync(token => inventory.AddAsync(character.Id, Plain("test-sword"), token), cancellationToken);
            throw new InvalidOperationException("外層失敗");
        }, CancellationToken));

        Assert.Empty(await ListInventoryAsync(character.Id));
    }

    [Fact]
    public async Task Transaction_CommitsWhenWorkSucceeds()
    {
        var character = await CreateCharacterAsync();
        await using var context = database.CreateDbContext();
        var inventory = new InventoryRepository(context, TestGameContent.Instance);

        await new EfUnitOfWork(context).RunAsync(cancellationToken => inventory.AddAsync(character.Id, Plain("test-sword"), cancellationToken), CancellationToken);

        Assert.Single(await ListInventoryAsync(character.Id));
    }

    // READ COMMITTED：交易中再讀一次會看到別的連線剛提交的值（REPEATABLE READ 會一直看到第一次讀的快照）。
    // MySql.Data 以 SET TRANSACTION（只作用於下一筆交易）設定隔離等級，@@transaction_isolation 仍是連線的預設值，所以用行為驗證
    [Fact]
    public async Task Transaction_UsesReadCommitted()
    {
        var character = await CreateCharacterAsync();
        await using var context = database.CreateDbContext();

        var (first, second) = await new EfUnitOfWork(context).RunAsync(async cancellationToken =>
        {
            var before = await MoneyAsync(context, character.Id, cancellationToken);
            await using (var otherRequest = database.CreateDbContext())
            {
                await TouchAsync(otherRequest, character.Id, cancellationToken);
            }

            return (before, await MoneyAsync(context, character.Id, cancellationToken));
        }, CancellationToken);

        Assert.Equal(first + 1, second);
    }

    private static Task<long> MoneyAsync(EndlessBattleDbContext context, long characterId, CancellationToken cancellationToken) =>
        context.Database.SqlQueryRaw<long>("SELECT money AS Value FROM characters WHERE id = {0}", characterId).SingleAsync(cancellationToken);

    // 兩條連線交叉更新兩個角色列（A 鎖甲、B 鎖乙、A 等乙、B 等甲）：MySQL 選一個犧牲者回滾（1213），它收到 409（保留原本的 MySqlException），
    // 另一個照常提交：兩列都只留下勝者的 +1
    [Fact]
    public async Task Transaction_DeadlockVictimGetsConflict()
    {
        var first = await CreateCharacterAsync();
        var second = await CreateCharacterAsync();
        using var firstLocked = new SemaphoreSlim(0);
        using var secondLocked = new SemaphoreSlim(0);

        async Task<ConflictException?> RunAsync(long lockFirst, long lockSecond, SemaphoreSlim signal, SemaphoreSlim wait)
        {
            await using var context = database.CreateDbContext();
            try
            {
                return await new EfUnitOfWork(context).RunAsync<ConflictException?>(async cancellationToken =>
                {
                    await TouchAsync(context, lockFirst, cancellationToken);
                    signal.Release();
                    Assert.True(await wait.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken), "另一條連線沒有在 20 秒內鎖住第一列");
                    await TouchAsync(context, lockSecond, cancellationToken);
                    return null;
                }, CancellationToken);
            }
            catch (ConflictException conflict)
            {
                return conflict;
            }
        }

        var results = await Task.WhenAll(
            RunAsync(first.Id, second.Id, firstLocked, secondLocked),
            RunAsync(second.Id, first.Id, secondLocked, firstLocked));

        var conflict = Assert.Single(results.OfType<ConflictException>());
        Assert.Equal(1213, Assert.IsType<MySqlException>(conflict.InnerException).Number);
        await using var reader = database.CreateDbContext();
        Assert.Equal(first.Money + 1, await MoneyAsync(reader, first.Id, CancellationToken));
        Assert.Equal(second.Money + 1, await MoneyAsync(reader, second.Id, CancellationToken));
    }

    private static Task<int> TouchAsync(EndlessBattleDbContext context, long characterId, CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync("UPDATE characters SET money = money + 1 WHERE id = {0}", [characterId], cancellationToken);

    // 工作中連線斷了：回滾本身也會失敗，但呼叫端看到的是原本的例外
    [Fact]
    public async Task Transaction_RollbackFailureKeepsOriginalException()
    {
        await using var context = database.CreateDbContext();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => new EfUnitOfWork(context).RunAsync<int>(async cancellationToken =>
        {
            await context.Database.GetDbConnection().CloseAsync();
            throw new InvalidOperationException("original");
        }, CancellationToken));

        Assert.Equal("original", exception.Message);
    }
}
