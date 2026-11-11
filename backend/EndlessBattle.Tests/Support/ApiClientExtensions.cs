using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EndlessBattle.Tests.Support;

public static class ApiClientExtensions
{
    public static string NewUsername() => $"user_{Guid.NewGuid():N}"[..20];

    // 用隨機帳號註冊並登入，回傳已帶 Bearer token 的 client
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(this EndlessBattleApiFactory factory)
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

    // 合法的創角 body（戰士、配點 2/4/2/2，剛好 10 點）；可覆寫個別欄位
    public static object ValidCharacterBody(
        string name,
        string jobId = "warrior",
        int strength = 2,
        int intelligence = 4,
        int life = 2,
        int agility = 2) => new
        {
            name,
            sex = 1,
            element = 3,
            birth = 1,
            jobId,
            allocation = new { strength, intelligence, life, agility },
        };

    public static async Task<HttpResponseMessage> CreateCharacterAsync(this HttpClient client, object body) =>
        await client.PostAsJsonAsync("/api/characters", body, TestContext.Current.CancellationToken);
}
