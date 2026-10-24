using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class CharacterRepositoryTests(MySqlDatabaseFixture database)
{
    private static string UniqueName(string prefix) => $"{prefix}{Random.Shared.Next(100000, 999999)}";

    [Fact]
    public async Task UpdateAsync_RejectsStaleVersion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        await using var setupContext = database.CreateDbContext();
        var accountId = await new AccountRepository(setupContext).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        await new CharacterRepository(setupContext).AddAsync(
            TestCharacters.Default(accountId, UniqueName("樂觀"), 1, 1, now), cancellationToken);

        await using var contextA = database.CreateDbContext();
        await using var contextB = database.CreateDbContext();
        var repositoryA = new CharacterRepository(contextA);
        var repositoryB = new CharacterRepository(contextB);
        var snapshotA = (await repositoryA.FindByAccountAsync(accountId, cancellationToken))!;
        var snapshotB = (await repositoryB.FindByAccountAsync(accountId, cancellationToken))!;

        var updated = await repositoryA.UpdateAsync(snapshotA with { Money = 10 }, cancellationToken);
        Assert.Equal(1, updated.Version);
        Assert.Equal(10, updated.Money);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() => repositoryB.UpdateAsync(snapshotB with { Money = 20 }, cancellationToken));
        Assert.IsType<DbUpdateConcurrencyException>(conflict.InnerException);
    }

    // 帳號名稱與「一個帳號一個角色」撞唯一鍵：409，原本的資料庫錯誤留在 InnerException
    [Fact]
    public async Task AddAsync_DuplicateConflictsKeepDatabaseError()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        var username = ApiClientExtensions.NewUsername();
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(username, "hash", now, cancellationToken);
        await new CharacterRepository(context).AddAsync(TestCharacters.Default(accountId, UniqueName("重複"), 1, 1, now), cancellationToken);

        await using var accountContext = database.CreateDbContext();
        var account = await Assert.ThrowsAsync<ConflictException>(
            () => new AccountRepository(accountContext).AddAsync(username, "hash", now, cancellationToken));
        await using var characterContext = database.CreateDbContext();
        var character = await Assert.ThrowsAsync<ConflictException>(() => new CharacterRepository(characterContext).AddAsync(
            TestCharacters.Default(accountId, UniqueName("重複"), 1, 1, now), cancellationToken));

        Assert.IsType<DbUpdateException>(account.InnerException, exactMatch: false);
        Assert.IsType<DbUpdateException>(character.InnerException, exactMatch: false);
    }

    // 每個欄位都用和資料庫預設值不同的值，漏對應的欄位才會被抓到
    [Fact]
    public async Task AddAsync_RoundTripsAllColumns()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero).AddTicks(1234560);
        await using var setupContext = database.CreateDbContext();
        var accountId = await new AccountRepository(setupContext).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        var character = new Character(
            Id: 0,
            AccountId: accountId,
            Name: UniqueName("往返"),
            Level: 2,
            Exp: 345,
            NextExp: 2184,
            Money: 2000,
            Sex: 2,
            Element: 3,
            Birth: 4,
            JobId: "sorcerer",
            SpecialAttackName: "冰封千里",
            Base: new BaseStats(Str: 14, Smart: 15, Life: 16, Vit: 17, Agi: 18, Au: 19, Be: 20),
            Hp: 37,
            Sp: 19,
            PlaceId: 16,
            SubPlace: 3,
            LastSettledAt: now,
            Bank: 0,
            BagBonus: 0,
            DepotBonus: 0,
            Version: 0);

        var added = await new CharacterRepository(setupContext).AddAsync(character, cancellationToken);

        await using var readContext = database.CreateDbContext();
        var stored = await new CharacterRepository(readContext).FindByAccountAsync(accountId, cancellationToken);
        Assert.Equal(character with { Id = added.Id }, stored);
    }

    [Fact]
    public async Task Character_RoundTripsBankAndBonuses()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        var repository = new CharacterRepository(context);
        var added = await repository.AddAsync(
            TestCharacters.Default(accountId, UniqueName("銀行"), 1, 1, now) with { Bank = 123456789012, BagBonus = 4, DepotBonus = 6 }, cancellationToken);

        var updated = await repository.UpdateAsync(added with { Bank = 99, BagBonus = 5, DepotBonus = 7 }, cancellationToken);

        Assert.Equal((123456789012, 4, 6), (added.Bank, added.BagBonus, added.DepotBonus));
        await using var readContext = database.CreateDbContext();
        var stored = await new CharacterRepository(readContext).FindByAccountAsync(accountId, cancellationToken);
        Assert.Equal((99, 5, 7), (stored!.Bank, stored.BagBonus, stored.DepotBonus));
        Assert.Equal(updated, stored);
    }

    [Fact]
    public async Task UpdateAsync_RoundTripsAllColumns()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        await using var setupContext = database.CreateDbContext();
        var accountId = await new AccountRepository(setupContext).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        var added = await new CharacterRepository(setupContext).AddAsync(
            TestCharacters.Default(accountId, UniqueName("更新"), 1, 1, now), cancellationToken);
        var changed = added with
        {
            Level = 3,
            Exp = 77,
            NextExp = 3500,
            Money = 1500,
            Sex = 2,
            Element = 6,
            Birth = 3,
            JobId = "sorcerer",
            SpecialAttackName = "雷鳴",
            Base = new BaseStats(Str: 21, Smart: 22, Life: 23, Vit: 24, Agi: 25, Au: 26, Be: 27),
            Hp = 12,
            Sp = 8,
            PlaceId = 7,
            SubPlace = 4,
            LastSettledAt = now.AddMinutes(5),
        };

        await using var updateContext = database.CreateDbContext();
        await new CharacterRepository(updateContext).UpdateAsync(changed, cancellationToken);

        await using var readContext = database.CreateDbContext();
        var stored = await new CharacterRepository(readContext).FindByAccountAsync(accountId, cancellationToken);
        Assert.Equal(changed with { Version = 1 }, stored);
    }

    // 新增帶非預設值；更新含 0 與 -1 兩個邊界；攻擊方式 null（自動）也要能存回去
    [Fact]
    public async Task Settings_RoundTrip()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        await using var setupContext = database.CreateDbContext();
        var accountId = await new AccountRepository(setupContext).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        var initial = new AdventureSettings(AttackKind.Physical, 30, 50, -1, 20, 10);
        var added = await new CharacterRepository(setupContext).AddAsync(
            TestCharacters.Default(accountId, UniqueName("設定"), 1, 1, now) with { Settings = initial }, cancellationToken);
        Assert.Equal(initial, (await ReadAsync(accountId))!.Settings);

        var magic = new AdventureSettings(AttackKind.Magic, 0, -1, 100, 0, 100);
        await using (var context = database.CreateDbContext())
        {
            added = await new CharacterRepository(context).UpdateAsync(added with { Settings = magic }, cancellationToken);
        }

        Assert.Equal(magic, (await ReadAsync(accountId))!.Settings);

        await using (var context = database.CreateDbContext())
        {
            await new CharacterRepository(context).UpdateAsync(added with { Settings = magic with { Attack = null } }, cancellationToken);
        }

        Assert.Equal(magic with { Attack = null }, (await ReadAsync(accountId))!.Settings);

        async Task<Character?> ReadAsync(long account)
        {
            await using var readContext = database.CreateDbContext();
            return await new CharacterRepository(readContext).FindByAccountAsync(account, cancellationToken);
        }
    }

    // 沒有子地區的地區（20 黑曜神廟）存 NULL；移動到有子地區的地區後再寫回數值
    [Fact]
    public async Task Repository_RoundTripsPlace()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
        await using var setupContext = database.CreateDbContext();
        var accountId = await new AccountRepository(setupContext).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, cancellationToken);
        var added = await new CharacterRepository(setupContext).AddAsync(
            TestCharacters.Default(accountId, UniqueName("地區"), placeId: 20, subPlace: null, now), cancellationToken);

        await using var firstRead = database.CreateDbContext();
        var stored = (await new CharacterRepository(firstRead).FindByAccountAsync(accountId, cancellationToken))!;
        Assert.Equal((20, (int?)null), (stored.PlaceId, stored.SubPlace));

        await using var updateContext = database.CreateDbContext();
        await new CharacterRepository(updateContext).UpdateAsync(added with { PlaceId = 3, SubPlace = 2 }, cancellationToken);

        await using var secondRead = database.CreateDbContext();
        var moved = (await new CharacterRepository(secondRead).FindByAccountAsync(accountId, cancellationToken))!;
        Assert.Equal((3, (int?)2), (moved.PlaceId, moved.SubPlace));
    }
}
