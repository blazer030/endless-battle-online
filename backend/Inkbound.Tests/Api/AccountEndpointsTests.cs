using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inkbound.Tests.Support;

namespace Inkbound.Tests.Api;

public class AccountEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_ThenLogin_GrantsAccessToProtectedEndpoint()
    {
        await using var factory = new InkboundApiFactory(database);
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
        await using var factory = new InkboundApiFactory(database);
        var client = factory.CreateClient();
        var username = ApiClientExtensions.NewUsername();

        await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" }, CancellationToken);
        var duplicate = await client.PostAsJsonAsync("/api/accounts/register", new { username = username.ToUpperInvariant(), password = "password123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("ab", "password123")]
    [InlineData("has space", "password123")]
    [InlineData("valid_name", "short")]
    public async Task Register_Returns400_ForInvalidInput(string username, string password)
    {
        await using var factory = new InkboundApiFactory(database);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/accounts/register", new { username, password }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns401_WithSameDetail_ForWrongPasswordAndUnknownUser()
    {
        await using var factory = new InkboundApiFactory(database);
        var client = factory.CreateClient();
        var username = ApiClientExtensions.NewUsername();
        await client.PostAsJsonAsync("/api/accounts/register", new { username, password = "password123" }, CancellationToken);

        var wrongPassword = await client.PostAsJsonAsync("/api/accounts/login", new { username, password = "wrong-password" }, CancellationToken);
        var unknownUser = await client.PostAsJsonAsync("/api/accounts/login", new { username = ApiClientExtensions.NewUsername(), password = "password123" }, CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        var wrongPasswordDetail = (await wrongPassword.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("detail").GetString();
        var unknownUserDetail = (await unknownUser.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("detail").GetString();
        Assert.Equal(wrongPasswordDetail, unknownUserDetail);
    }

    [Fact]
    public async Task ProtectedEndpoint_Returns401_WithoutToken()
    {
        await using var factory = new InkboundApiFactory(database);
        var response = await factory.CreateClient().GetAsync("/api/accounts/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
