using System.Security.Claims;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Common;
using EndlessBattle.Application.Content;
using EndlessBattle.Application.Economy;

namespace EndlessBattle.Api.Endpoints;

public static class BankEndpoints
{
    public static IEndpointRouteBuilder MapBankEndpoints(this IEndpointRouteBuilder app)
    {
        var bank = app.MapGroup("/api/bank").WithTags("Bank").RequireAuthorization();

        bank.MapPost("/deposit", async (
            BankAmountRequest request, ClaimsPrincipal user, BankService service, CharacterService characters, IGameContent content, CancellationToken cancellationToken) =>
        {
            var amount = request.Amount ?? throw new InputValidationException("缺少金額");
            await service.DepositAsync(user.GetAccountId(), amount, cancellationToken);
            return TypedResults.Ok(await ProfileAsync(user, characters, content, cancellationToken));
        });

        bank.MapPost("/withdraw", async (
            BankAmountRequest request, ClaimsPrincipal user, BankService service, CharacterService characters, IGameContent content, CancellationToken cancellationToken) =>
        {
            var amount = request.Amount ?? throw new InputValidationException("缺少金額");
            await service.WithdrawAsync(user.GetAccountId(), amount, cancellationToken);
            return TypedResults.Ok(await ProfileAsync(user, characters, content, cancellationToken));
        });

        return app;
    }

    private static async Task<CharacterResponse> ProfileAsync(
        ClaimsPrincipal user, CharacterService characters, IGameContent content, CancellationToken cancellationToken)
    {
        var profile = await characters.GetAsync(user.GetAccountId(), cancellationToken);
        return CharacterEndpoints.ToResponse(profile.Character, profile.JobProficiency, profile.Equipment, content);
    }
}
