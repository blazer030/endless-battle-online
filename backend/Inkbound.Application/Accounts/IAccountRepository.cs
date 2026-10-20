namespace Inkbound.Application.Accounts;

public interface IAccountRepository
{
    // 帳號重複時丟 ConflictException
    Task<long> AddAsync(string username, string passwordHash, DateTimeOffset createdAt, CancellationToken cancellationToken);

    Task<AccountCredentials?> FindByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<AccountCredentials?> FindByIdAsync(long accountId, CancellationToken cancellationToken);
}
