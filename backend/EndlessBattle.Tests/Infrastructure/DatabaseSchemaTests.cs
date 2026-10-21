using EndlessBattle.Infrastructure.Persistence.Models;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class DatabaseSchemaTests(MySqlDatabaseFixture database)
{
    [Fact]
    public async Task Migrations_CreateAccountsAndCharacters()
    {
        var characterId = await CreateCharacterAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, TestContext.Current.CancellationToken);
        Assert.Equal(0, stored.Exp);
        Assert.Equal(0, stored.Version);
    }

    [Fact]
    public async Task Migration_GivesExistingRowsDefaults()
    {
        var characterId = await CreateCharacterAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, TestContext.Current.CancellationToken);
        Assert.Equal(1, stored.Level);
        Assert.Equal(1000, stored.NextExp);
        Assert.Equal("warrior", stored.JobId);
        Assert.Equal(10, stored.BaseStr);
        Assert.Equal(50, stored.Hp);
        Assert.Equal(50, stored.ActNum);
        Assert.Null(stored.ActNumTime);
        Assert.Equal(0, stored.Money);
        Assert.Equal(0, stored.Exp);
    }

    [Fact]
    public async Task Accounts_RejectDuplicateUsername_CaseInsensitively()
    {
        var username = $"dup_{Guid.NewGuid():N}"[..20];
        await using var context = database.CreateDbContext();
        context.Accounts.Add(new Account { Username = username, PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var second = database.CreateDbContext();
        second.Accounts.Add(new Account { Username = username.ToUpperInvariant(), PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Characters_RejectStaleVersion()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var contextA = database.CreateDbContext();
        await using var contextB = database.CreateDbContext();
        var characterA = await contextA.Characters.SingleAsync(character => character.Id == characterId, cancellationToken);
        var characterB = await contextB.Characters.SingleAsync(character => character.Id == characterId, cancellationToken);

        characterA.Money = 10;
        characterA.Version += 1;
        await contextA.SaveChangesAsync(cancellationToken);

        characterB.Money = 20;
        characterB.Version += 1;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task Schema_HasSkillTables()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var context = database.CreateDbContext();
        context.CharacterSkills.Add(new CharacterSkill { CharacterId = characterId, SkillId = "slash", Level = 1 });
        context.CharacterSkillSlots.Add(new CharacterSkillSlot { CharacterId = characterId, Slot = 1, SkillId = "slash", Chance = 30 });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        Assert.Equal(1, (await reader.CharacterSkills.SingleAsync(row => row.CharacterId == characterId, cancellationToken)).Level);
        Assert.Equal(30, (await reader.CharacterSkillSlots.SingleAsync(row => row.CharacterId == characterId, cancellationToken)).Chance);
    }

    private async Task<long> CreateCharacterAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = database.CreateDbContext();
        var account = new Account { Username = $"schema_{Guid.NewGuid():N}"[..32], PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        var character = new Character
        {
            AccountId = account.Id,
            Name = $"角{account.Id}",
            CurrentStageId = "stage",
            LastSettledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Characters.Add(character);
        await context.SaveChangesAsync(cancellationToken);
        return character.Id;
    }
}
