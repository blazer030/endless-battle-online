using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EndlessBattle.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    public static long GetAccountId(this ClaimsPrincipal user) =>
        long.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("token 缺少 sub"));
}
