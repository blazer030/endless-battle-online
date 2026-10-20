using Inkbound.Infrastructure.Persistence.Models;
using Inkbound.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace Inkbound.Tests.Infrastructure;

public class DatabaseSchemaTests(MySqlDatabaseFixture database)
{
    [Fact]
    public async Task Migrations_CreateAccountsAndCharacters()
    {
        var characterId = await CreateCharacterAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, TestContext.Current.CancellationToken);
        Assert.Equal(0, stored.Experience);
        Assert.Equal(0, stored.Version);
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

        characterA.SpiritStones = 10;
        characterA.Version += 1;
        await contextA.SaveChangesAsync(cancellationToken);

        characterB.SpiritStones = 20;
        characterB.Version += 1;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync(cancellationToken));
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
