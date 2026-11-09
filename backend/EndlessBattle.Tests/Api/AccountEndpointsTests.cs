using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Tests.Support;
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
        Assert.Equal("帳號已被使用", await DetailOf(duplicate));
    }

    private const string InvalidUsername = "帳號必須是 3～32 個英數字或底線";

    [Theory]
    [InlineData("ab", "abcdefgh", InvalidUsername)]
    [InlineData("has space", "abcdefgh", InvalidUsername)]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefg", "abcdefgh", InvalidUsername)]
    [InlineData("valid_name", "abcdefg", "密碼至少要 8 個字元")]
    public async Task Register_Returns400_ForInvalidInput(string username, string password, string detail)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/accounts/register", new { username, password }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(detail, await DetailOf(response));
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

    private static async Task<string?> DetailOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("detail").GetString();

    [Fact]
    public async Task Login_Returns401_WithSameDetail_ForWrongPasswordAndUnknownUser()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var username = ApiClientExtensions.NewUsername();
        await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" }, CancellationToken);

        var wrongPassword = await client.PostAsJsonAsync("/api/accounts/login", new { username, password = "wrong-password" }, CancellationToken);
        var unknownUser = await client.PostAsJsonAsync("/api/accounts/login", new { username = ApiClientExtensions.NewUsername(), password = "password123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        var wrongPasswordDetail = (await wrongPassword.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("detail").GetString();
        var unknownUserDetail = (await unknownUser.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("detail").GetString();
        Assert.Equal("帳號或密碼錯誤", wrongPasswordDetail);
        Assert.Equal(wrongPasswordDetail, unknownUserDetail);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithoutToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var response = await factory.CreateClient().GetAsync("/api/accounts/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var character = await factory.CreateClient().GetAsync("/api/characters/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, character.StatusCode);
    }
}
