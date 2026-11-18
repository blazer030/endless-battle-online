using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api;

// spec game-api-contract：Error responses carry only reason codes——各種錯誤的樣本請求，回應只有大寫底線的代碼、沒有 detail
public partial class ErrorCodeFormatTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*(:.+)?$")]
    private static partial Regex ReasonCode();

    [Fact]
    public async Task SampleErrors_CarryOnlyUpperSnakeReasonCodes()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var anonymous = factory.CreateClient();
        var noCharacter = await factory.CreateAuthenticatedClientAsync();
        var player = await factory.CreateAuthenticatedClientAsync();
        (await player.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"樣本{Random.Shared.Next(1000, 9999)}"))).EnsureSuccessStatusCode();

        HttpResponseMessage[] responses =
        [
            await anonymous.PostAsJsonAsync("/api/accounts/register", new { username = "ab", password = "abcdefgh" }, CancellationToken),
            await anonymous.PostAsJsonAsync("/api/accounts/login", new { username = ApiClientExtensions.NewUsername(), password = "wrong-password" }, CancellationToken),
            await noCharacter.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody("樣本", strength: 1, intelligence: 1, life: 1, agility: 3)),
            await noCharacter.GetAsync("/api/inventory", CancellationToken),
            await player.PostAsJsonAsync("/api/missions/accept", new { missionId = "no-such-mission" }, CancellationToken),
            await player.PostAsJsonAsync("/api/inventory/equip", new { }, CancellationToken),
            await player.PostAsJsonAsync("/api/skills/learn", new { skillId = "no-such-skill" }, CancellationToken),
        ];

        foreach (var response in responses)
        {
            Assert.False(response.IsSuccessStatusCode, $"{response.RequestMessage?.RequestUri} 應該失敗");
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
            Assert.False(body.TryGetProperty("detail", out _), $"{response.RequestMessage?.RequestUri} 不該有 detail");
            var reasons = body.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString() ?? "").ToList();
            Assert.NotEmpty(reasons);
            Assert.All(reasons, reason => Assert.Matches(ReasonCode(), reason));
        }
    }
}
