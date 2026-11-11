using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class BuffRepositoryTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset Moment = new DateTimeOffset(2026, 10, 22, 1, 2, 3, TimeSpan.Zero).AddTicks(1230);

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"密藥{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    // 同種第二次寫入覆蓋倍率與窗口；別種另存一列，列表依種類
    [Fact]
    public async Task Save_OverwritesSameKind()
    {
        var characterId = await CreateCharacterAsync();

        await using (var context = database.CreateDbContext())
        {
            var repository = new BuffRepository(context);
            Assert.Empty(await repository.ListAsync(characterId, CancellationToken));
            await repository.SaveAsync(characterId, new CharacterBuff(BuffKind.MagicDefense, 1.2, Moment, Moment.AddHours(1)), CancellationToken);
            await repository.SaveAsync(characterId, new CharacterBuff(BuffKind.Experience, 1.5, Moment, Moment.AddHours(1)), CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            await new BuffRepository(context).SaveAsync(
                characterId, new CharacterBuff(BuffKind.MagicDefense, 1.25, Moment.AddHours(2), Moment.AddHours(3)), CancellationToken);
        }

        await using var reader = database.CreateDbContext();
        Assert.Equal(
            [
                new CharacterBuff(BuffKind.Experience, 1.5, Moment, Moment.AddHours(1)),
                new CharacterBuff(BuffKind.MagicDefense, 1.25, Moment.AddHours(2), Moment.AddHours(3)),
            ],
            await new BuffRepository(reader).ListAsync(characterId, CancellationToken));
        Assert.Equal(2, await reader.CharacterBuffs.CountAsync(row => row.CharacterId == characterId, CancellationToken));
    }

    // 規格 Stored formats stay readable：資料庫的經驗密藥代碼是 Exp，讀成 Experience
    [Fact]
    public async Task Buffs_ReadStoredLegacyKind()
    {
        var characterId = await CreateCharacterAsync();
        await using (var context = database.CreateDbContext())
        {
            context.CharacterBuffs.Add(new EndlessBattle.Infrastructure.Persistence.Models.CharacterBuff
            {
                CharacterId = characterId,
                Kind = "Exp",
                Multiplier = 1.5m,
                StartsAt = Moment.UtcDateTime,
                EndsAt = Moment.AddHours(1).UtcDateTime,
            });
            await context.SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateDbContext();
        Assert.Equal(
            [new CharacterBuff(BuffKind.Experience, 1.5, Moment, Moment.AddHours(1))],
            await new BuffRepository(reader).ListAsync(characterId, CancellationToken));
    }

    // 寫入仍用舊代碼，舊版本讀得回來
    [Fact]
    public async Task Buffs_WriteLegacyKind()
    {
        var characterId = await CreateCharacterAsync();
        await using (var context = database.CreateDbContext())
        {
            var repository = new BuffRepository(context);
            await repository.SaveAsync(characterId, new CharacterBuff(BuffKind.Experience, 1.5, Moment, Moment.AddHours(1)), CancellationToken);
            await repository.SaveAsync(characterId, new CharacterBuff(BuffKind.JobExperience, 1.2, Moment, Moment.AddHours(1)), CancellationToken);
        }

        await using var reader = database.CreateDbContext();
        var kinds = await reader.CharacterBuffs.Where(row => row.CharacterId == characterId).Select(row => row.Kind).OrderBy(kind => kind).ToListAsync(CancellationToken);
        Assert.Equal(["Exp", "JobExp"], kinds);
    }
}
