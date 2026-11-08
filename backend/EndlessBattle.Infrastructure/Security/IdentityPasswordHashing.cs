using EndlessBattle.Application.Accounts;
using Microsoft.AspNetCore.Identity;

namespace EndlessBattle.Infrastructure.Security;

// 只借用 ASP.NET Core Identity 的密碼雜湊（PBKDF2），不使用 Identity 的資料表
public sealed class IdentityPasswordHashing : IPasswordHashing
{
    private static readonly PasswordOwner Owner = new();
    private readonly PasswordHasher<PasswordOwner> hasher = new();

    public string Hash(string password) => hasher.HashPassword(Owner, password);

    public bool Verify(string passwordHash, string password) =>
        hasher.VerifyHashedPassword(Owner, passwordHash, password) != PasswordVerificationResult.Failed;

    private sealed class PasswordOwner;
}
