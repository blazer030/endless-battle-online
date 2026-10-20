using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Inkbound.Tests.Support;

public static class ApiClientExtensions
{
    public static string NewUsername() => $"user_{Guid.NewGuid():N}"[..20];

    // 用隨機帳號註冊並登入，回傳已帶 Bearer token 的 client
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this InkboundApiFactory factory)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var client = factory.CreateClient();
        var credentials = new { username = NewUsername(), password = "password123" };
        (await client.PostAsJsonAsync("/api/accounts/register", credentials, cancellationToken)).EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/api/accounts/login", credentials, cancellationToken);
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("accessToken").GetString());
        return client;
    }
}
