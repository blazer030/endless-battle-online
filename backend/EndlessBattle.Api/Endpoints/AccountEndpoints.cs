using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Accounts;

namespace EndlessBattle.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var accounts = app.MapGroup("/api/accounts").WithTags("Accounts");

        accounts.MapPost("/register", async (CredentialsRequest request, AccountService service, CancellationToken cancellationToken) =>
        {
            var accountId = await service.RegisterAsync(request.Username ?? "", request.Password ?? "", cancellationToken);
            return TypedResults.Created("/api/accounts/me", new RegisterResponse(accountId));
        });

        accounts.MapPost("/login", async (CredentialsRequest request, AccountService service, JwtTokenIssuer tokenIssuer, CancellationToken cancellationToken) =>
        {
            var accountId = await service.LoginAsync(request.Username ?? "", request.Password ?? "", cancellationToken);
            return TypedResults.Ok(new LoginResponse(tokenIssuer.Issue(accountId)));
        });

        accounts.MapGet("/me", async (ClaimsPrincipal user, AccountService service, CancellationToken cancellationToken) =>
        {
            var account = await service.GetAsync(user.GetAccountId(), cancellationToken);
            return TypedResults.Ok(new AccountResponse(account.Id, account.Username));
        }).RequireAuthorization();

        return app;
    }
}
