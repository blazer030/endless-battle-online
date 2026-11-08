using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EndlessBattle.Api.Auth;

// token 的有效期用真實時間（TimeProvider），不用遊戲時鐘 IClock，因為驗證 token 的一方也是看真實時間
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler handler = new();

    public string Issue(long accountId)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = accountId.ToString() },
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddDays(settings.LifetimeDays),
            SigningCredentials = new SigningCredentials(CreateSigningKey(settings), SecurityAlgorithms.HmacSha256),
        };
        return handler.CreateToken(descriptor);
    }

    public static SymmetricSecurityKey CreateSigningKey(JwtOptions settings) =>
        new(Encoding.UTF8.GetBytes(settings.SigningKey));
}
