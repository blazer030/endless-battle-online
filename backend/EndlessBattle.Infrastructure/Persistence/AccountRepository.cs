using EndlessBattle.Application.Accounts;
using EndlessBattle.Application.Common;
using EndlessBattle.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Infrastructure.Persistence;

public sealed class AccountRepository(EndlessBattleDbContext context) : IAccountRepository
{
    public async Task<long> AddAsync(string username, string passwordHash, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        var account = new Account { Username = username, PasswordHash = passwordHash, CreatedAt = createdAt.UtcDateTime };
        context.Accounts.Add(account);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (MySqlErrors.IsDuplicateKey(exception))
        {
            throw new ConflictException("帳號已被使用");
        }

        return account.Id;
    }

    public async Task<AccountCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
        await context.Accounts
            .Where(account => account.Username == username)
            .Select(account => new AccountCredentials(account.Id, account.Username, account.PasswordHash))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<AccountCredentials?> FindByIdAsync(long accountId, CancellationToken cancellationToken) =>
        await context.Accounts
            .Where(account => account.Id == accountId)
            .Select(account => new AccountCredentials(account.Id, account.Username, account.PasswordHash))
            .SingleOrDefaultAsync(cancellationToken);
}
