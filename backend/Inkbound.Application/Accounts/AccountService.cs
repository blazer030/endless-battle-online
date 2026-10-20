using System.Text.RegularExpressions;
using Inkbound.Application.Common;

namespace Inkbound.Application.Accounts;

public sealed partial class AccountService(IAccountRepository accounts, IPasswordHashing passwordHashing, IClock clock)
{
    private const int MinimumPasswordLength = 8;

    [GeneratedRegex("^[A-Za-z0-9_]{3,32}$")]
    private static partial Regex UsernamePattern();

    public async Task<long> RegisterAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (!UsernamePattern().IsMatch(username))
        {
            throw new InputValidationException("帳號必須是 3～32 個英數字或底線");
        }

        if (password.Length < MinimumPasswordLength)
        {
            throw new InputValidationException($"密碼至少要 {MinimumPasswordLength} 個字元");
        }

        return await accounts.AddAsync(username, passwordHashing.Hash(password), clock.UtcNow, cancellationToken);
    }

    // 帳號不存在與密碼錯誤回傳同一個錯誤，避免被用來探測帳號是否存在
    public async Task<long> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByUsernameAsync(username, cancellationToken);
        if (account is null || !passwordHashing.Verify(account.PasswordHash, password))
        {
            throw new AuthenticationFailedException();
        }

        return account.Id;
    }

    public async Task<AccountCredentials> GetAsync(long accountId, CancellationToken cancellationToken) =>
        await accounts.FindByIdAsync(accountId, cancellationToken)
        ?? throw new NotFoundException("找不到帳號");
}
