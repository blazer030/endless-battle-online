using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Api.Auth;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace EndlessBattle.Tests.Api;

public class AccountEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // spec 的 Valid registration 情境（ethan_01／abcdefgh）；帳號要唯一，用隨機帳號
    [Fact]
    public async Task Register_ThenLogin_GrantsAccessToProtectedEndpoint()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var username = ApiClientExtensions.NewUsername();

        var register = await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" }, CancellationToken);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var accountId = (await register.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("accountId").GetInt64();
        Assert.True(accountId > 0);

        var login = await client.PostAsJsonAsync("/api/accounts/login", new { username, password = "password123" }, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        Assert.Equal(username, me.GetProperty("username").GetString());
        Assert.Equal(accountId, me.GetProperty("accountId").GetInt64());
    }

    [Fact]
    public async Task Register_Returns409_ForDuplicateUsernameIgnoringCase()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var username = ApiClientExtensions.NewUsername();

        await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" }, CancellationToken);
        var duplicate = await client.PostAsJsonAsync("/api/accounts/register", new { username = username.ToUpperInvariant(), password = "password123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal(["USERNAME_TAKEN"], await duplicate.ReadReasonsAsync());
    }

    // spec registration input 表的 400 列
    [Theory]
    [InlineData("ab", "abcdefgh", "USERNAME_INVALID")]
    [InlineData("has space", "abcdefgh", "USERNAME_INVALID")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefg", "abcdefgh", "USERNAME_INVALID")]
    [InlineData("valid_name", "abcdefg", "PASSWORD_TOO_SHORT")]
    public async Task Register_Returns400_ForInvalidInput(string username, string password, string reason)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/accounts/register", new { username, password }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal([reason], await response.ReadReasonsAsync());
    }

    // spec 註冊範例表的「abc」列（帳號要唯一，用隨機 3 個字）與上限 32 個字元；密碼剛好 8 個字元
    [Theory]
    [InlineData(3)]
    [InlineData(32)]
    public async Task Register_AcceptsUsernameLengthBounds(int length)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var username = Guid.NewGuid().ToString("N")[..length].PadRight(length, '_');

        var response = await factory.CreateClient().PostAsJsonAsync("/api/accounts/register", new { username, password = "abcdefgh" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_DoesNotSignIn()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        var register = await client.PostAsJsonAsync("/api/accounts/register", new { username = ApiClientExtensions.NewUsername(), password = "password123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var body = await register.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["accountId"], body.EnumerateObject().Select(property => property.Name));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/accounts/me", CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Login_TokenExpiresThirtyDaysAfterIssue()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var credentials = new { username = ApiClientExtensions.NewUsername(), password = "password123" };
        await client.PostAsJsonAsync("/api/accounts/register", credentials, CancellationToken);

        var login = await client.PostAsJsonAsync("/api/accounts/login", credentials, CancellationToken);

        var token = new JsonWebToken((await login.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("accessToken").GetString());
        Assert.Equal(TimeSpan.FromDays(30), token.ValidTo - token.IssuedAt);
    }

    // 權杖有效但帳號已不存在（例如換資料庫）
    [Fact]
    public async Task Me_Returns404_WithAccountNotFound_ForUnknownAccount()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var token = factory.Services.GetRequiredService<JwtTokenIssuer>().Issue(long.MaxValue);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/accounts/me", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(["ACCOUNT_NOT_FOUND"], await response.ReadReasonsAsync());
    }

    [Fact]
    public async Task Login_Returns401_WithSameReason_ForWrongPasswordAndUnknownUser()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var username = ApiClientExtensions.NewUsername();
        await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" }, CancellationToken);

        var wrongPassword = await client.PostAsJsonAsync("/api/accounts/login", new { username, password = "wrong-password" }, CancellationToken);
        var unknownUser = await client.PostAsJsonAsync("/api/accounts/login", new { username = ApiClientExtensions.NewUsername(), password = "password123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(["AUTHENTICATION_FAILED"], await wrongPassword.ReadReasonsAsync());
        Assert.Equal(["AUTHENTICATION_FAILED"], await unknownUser.ReadReasonsAsync());
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithoutToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var response = await factory.CreateClient().GetAsync("/api/accounts/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal(["AUTHENTICATION_REQUIRED"], await response.ReadReasonsAsync());
        var character = await factory.CreateClient().GetAsync("/api/characters/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, character.StatusCode);
    }

    // 權杖過期或簽章不對也一樣：AUTHENTICATION_REQUIRED（跟登入時密碼錯的 AUTHENTICATION_FAILED 分開）
    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithInvalidToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");

        var response = await client.GetAsync("/api/accounts/me", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(["AUTHENTICATION_REQUIRED"], await response.ReadReasonsAsync());
    }
}
