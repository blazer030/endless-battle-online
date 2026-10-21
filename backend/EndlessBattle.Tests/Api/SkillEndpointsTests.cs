using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api;

// 新戰士 2000 錢、0 技能點；burst lv1 要 500 錢、300 技能點
public class SkillEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"習技{Random.Shared.Next(100000, 999999)}"));
        created.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<long> CharacterIdAsync(EndlessBattleApiFactory factory, HttpClient client)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        return character!.Id;
    }

    // 直接寫入技能點：取得技能點要靠戰鬥，這裡跳過
    private static async Task GrantSkillPointsAsync(EndlessBattleApiFactory factory, HttpClient client, string jobId, long skillPoints)
    {
        var characterId = await CharacterIdAsync(factory, client);
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IJobProficiencyRepository>()
            .AddAsync(characterId, jobId, 0, skillPoints, CancellationToken);
    }

    private static async Task<JsonElement> ReadOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static async Task<IReadOnlyList<string>> ReadReasonsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        return problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()!).ToList();
    }

    private static Task<HttpResponseMessage> PutSlotsAsync(HttpClient client, params object[] slots) =>
        client.PutAsJsonAsync("/api/skills/slots", new { slots }, CancellationToken);

    [Fact]
    public async Task Overview_ListsCurrentJobSkillsWithNextLevel()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var body = await client.GetFromJsonAsync<JsonElement>("/api/skills", CancellationToken);

        Assert.Equal("warrior", body.GetProperty("jobId").GetString());
        Assert.Equal(0, body.GetProperty("skillPoints").GetInt64());
        Assert.Empty(body.GetProperty("slots").EnumerateArray());
        var skills = body.GetProperty("skills").EnumerateArray().ToList();
        Assert.Equal(["burst", "iron-skin"], skills.Select(skill => skill.GetProperty("id").GetString()));
        var burst = skills[0];
        Assert.Equal("爆氣", burst.GetProperty("name").GetString());
        Assert.Equal("Active", burst.GetProperty("type").GetString());
        Assert.Equal(0, burst.GetProperty("level").GetInt32());
        Assert.Equal(5, burst.GetProperty("maxLevel").GetInt32());
        var next = burst.GetProperty("next");
        Assert.Equal(1, next.GetProperty("level").GetInt32());
        Assert.Equal(500, next.GetProperty("money").GetInt64());
        Assert.Equal(300, next.GetProperty("proficiency").GetInt64());
        Assert.False(next.GetProperty("canLearn").GetBoolean());
        Assert.Equal(["notEnoughProficiency"], next.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
        Assert.Equal(30, skills[1].GetProperty("next").GetProperty("chance").GetInt32());
    }

    // 滿級時 next 為 null；直接寫入技能書，跳過五次學習
    [Fact]
    public async Task Overview_ShowsNoNextLevel_AtMaxLevel()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var characterId = await CharacterIdAsync(factory, client);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<ISkillBookRepository>()
                .UpsertAsync(characterId, new LearnedSkill("burst", 5), CancellationToken);
        }

        var body = await client.GetFromJsonAsync<JsonElement>("/api/skills", CancellationToken);

        var burst = body.GetProperty("skills").EnumerateArray().First(skill => skill.GetProperty("id").GetString() == "burst");
        Assert.Equal(5, burst.GetProperty("level").GetInt32());
        Assert.Equal(JsonValueKind.Null, burst.GetProperty("next").ValueKind);
    }

    [Fact]
    public async Task Learn_ReturnsUpdatedOverview()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GrantSkillPointsAsync(factory, client, "warrior", 300);

        var body = await ReadOkAsync(await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "burst" }, CancellationToken));

        Assert.Equal(0, body.GetProperty("skillPoints").GetInt64());
        var burst = body.GetProperty("skills").EnumerateArray().First(skill => skill.GetProperty("id").GetString() == "burst");
        Assert.Equal(1, burst.GetProperty("level").GetInt32());
        Assert.Equal(2, burst.GetProperty("next").GetProperty("level").GetInt32());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(1500, me.GetProperty("money").GetInt64());
        var proficiency = Assert.Single(me.GetProperty("jobProficiency").EnumerateArray());
        Assert.Equal(0, proficiency.GetProperty("skillPoints").GetInt64());
        Assert.Equal(0, proficiency.GetProperty("proficiency").GetInt64());
    }

    [Fact]
    public async Task Learn_Returns400WithReasons_WhenNotAllowed()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var response = await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "burst" }, CancellationToken);

        Assert.Equal(["notEnoughProficiency"], await ReadReasonsAsync(response));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(2000, me.GetProperty("money").GetInt64());
    }

    [Theory]
    [InlineData("""{"skillId":"no-such-skill"}""")]
    [InlineData("""{}""")]
    public async Task Learn_Returns400_WhenSkillUnknown(string json)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);

        var response = await client.PostAsync(
            "/api/skills/learn", new StringContent(json, Encoding.UTF8, "application/json"), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 已學 burst（主動）與 iron-skin（被動）；stun 未學
    [Theory]
    [InlineData("""[{"slot":4,"skillId":"burst","chance":50}]""", "activeInPassiveSlot")]
    [InlineData("""[{"slot":1,"skillId":"burst","chance":50},{"slot":2,"skillId":"burst","chance":50}]""", "duplicateSkill")]
    [InlineData("""[{"slot":1,"skillId":"iron-skin","chance":50}]""", "passiveInActiveSlot")]
    [InlineData("""[{"slot":1,"skillId":"stun","chance":50}]""", "notLearned")]
    [InlineData("""[{"slot":1,"skillId":"burst","chance":55}]""", "chanceOutOfRange")]
    public async Task Slots_RejectInvalidLoadout_AndKeepExisting(string slotsJson, string reason)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GrantSkillPointsAsync(factory, client, "warrior", 600);
        await ReadOkAsync(await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "burst" }, CancellationToken));
        await ReadOkAsync(await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "iron-skin" }, CancellationToken));
        var saved = await ReadOkAsync(await PutSlotsAsync(client, new { slot = 1, skillId = "burst", chance = 100 }));
        Assert.Single(saved.GetProperty("slots").EnumerateArray());

        var rejected = await client.PutAsync(
            "/api/skills/slots", new StringContent($$"""{"slots":{{slotsJson}}}""", Encoding.UTF8, "application/json"), CancellationToken);

        Assert.Contains(reason, await ReadReasonsAsync(rejected));
        var body = await client.GetFromJsonAsync<JsonElement>("/api/skills", CancellationToken);
        var slot = Assert.Single(body.GetProperty("slots").EnumerateArray());
        Assert.Equal(1, slot.GetProperty("slot").GetInt32());
        Assert.Equal("burst", slot.GetProperty("skillId").GetString());
        Assert.Equal(100, slot.GetProperty("chance").GetInt32());
    }

    [Fact]
    public async Task Slots_ClampChanceToStep()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GrantSkillPointsAsync(factory, client, "warrior", 300);
        await ReadOkAsync(await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "burst" }, CancellationToken));

        var response = await PutSlotsAsync(client, new { slot = 1, skillId = "burst", chance = 55 });

        Assert.Equal(["chanceOutOfRange"], await ReadReasonsAsync(response));
    }

    // 省略 chance 時主動欄用預設 50；省略的欄位＝清空
    [Fact]
    public async Task Slots_DefaultChanceAndClearOmitted()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        await GrantSkillPointsAsync(factory, client, "warrior", 300);
        await ReadOkAsync(await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "burst" }, CancellationToken));

        var body = await ReadOkAsync(await PutSlotsAsync(client, new { slot = 2, skillId = "burst" }));
        Assert.Equal(50, Assert.Single(body.GetProperty("slots").EnumerateArray()).GetProperty("chance").GetInt32());

        var cleared = await ReadOkAsync(await PutSlotsAsync(client));
        Assert.Empty(cleared.GetProperty("slots").EnumerateArray());
    }

    // 先找角色再找技能：沒有角色時一律 404
    [Fact]
    public async Task Learn_Returns404_WhenNoCharacter_EvenIfSkillUnknown()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "no-such-skill" }, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Overview_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/skills", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Overview_RequiresAuthentication()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/skills", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
