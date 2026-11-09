using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Api.Auth;
using EndlessBattle.Application.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;
using Microsoft.AspNetCore.Hosting;

namespace EndlessBattle.Tests.Api;

public class PublicEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PublicSettings_RespondsWithoutToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/public/settings", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var settings = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["1:男", "2:女"], Options(settings, "sexes"));
        Assert.Equal(["1:地", "2:水", "3:火", "4:木", "5:風", "6:毒"], Options(settings, "elements"));
        Assert.Equal(["1:中央大陸", "2:魔法王國", "3:熱帶雨林", "4:末日王城"], Options(settings, "births"));
    }

    [Fact]
    public async Task PublicSettings_IgnoresExpiredToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var anonymous = await factory.CreateClient().GetStringAsync("/api/public/settings", CancellationToken);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ExpiredToken());

        // 先確認這張 token 真的被拒絕
        var protectedResponse = await client.GetAsync("/api/accounts/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);

        var response = await client.GetAsync("/api/public/settings", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(anonymous, await response.Content.ReadAsStringAsync(CancellationToken));
    }

    [Fact]
    public async Task PublicSettings_FireIsStrongAgainstWoodAndWeakAgainstWater()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var settings = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/settings", CancellationToken);

        var fire = settings.GetProperty("elements").EnumerateArray().Single(element => element.GetProperty("id").GetInt32() == 3);
        Assert.Equal("火", fire.GetProperty("name").GetString());
        Assert.Equal(4, fire.GetProperty("strongAgainst").GetInt32());
        Assert.Equal(2, fire.GetProperty("weakAgainst").GetInt32());
    }

    [Fact]
    public async Task PublicSettings_ElementAdvantageFollowsTheCycle()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var settings = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/settings", CancellationToken);

        // 地 → 水 → 火 → 木 → 風 → 毒 → 地：前者剋後者
        Assert.Equal(
            ["1>2<6", "2>3<1", "3>4<2", "4>5<3", "5>6<4", "6>1<5"],
            settings.GetProperty("elements").EnumerateArray().Select(element =>
                $"{element.GetProperty("id").GetInt32()}>{element.GetProperty("strongAgainst").GetInt32()}<{element.GetProperty("weakAgainst").GetInt32()}"));
    }

    [Fact]
    public async Task PublicJobs_RespondsWithoutToken()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/public/jobs", CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var jobs = (await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).EnumerateArray().ToList();
        Assert.Equal(["warrior", "sorcerer", "berserker", "thief", "bard", "martialist", "beastmaster"], jobs.Select(job => job.GetProperty("id").GetString()));
        var berserker = jobs[2];
        Assert.Equal(2, berserker.GetProperty("tier").GetInt32());
        Assert.Equal("warrior", berserker.GetProperty("parentId").GetString());
        Assert.Equal(30, berserker.GetProperty("unlockLevel").GetInt32());
        var requirement = Assert.Single(berserker.GetProperty("statRequirements").EnumerateArray());
        Assert.Equal("strength", requirement.GetProperty("stat").GetString());
        Assert.Equal(60, requirement.GetProperty("minimum").GetInt32());
        Assert.Equal(JsonValueKind.Null, jobs[0].GetProperty("parentId").ValueKind);
    }

    // TestContent 測試戰士：str [2,3]、smart [0,1]、life [1,2]、vit [1,2]、agi [1,2]、au [0,1]、be [0,1]
    [Fact]
    public async Task PublicJobs_GrowthUsesFullStatNames()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var jobs = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/jobs", CancellationToken);

        var growth = jobs.EnumerateArray().First().GetProperty("growth");
        Assert.Equal(
            ["strength 2-3", "intelligence 0-1", "life 1-2", "vitality 1-2", "agility 1-2", "charisma 0-1", "faith 0-1"],
            growth.EnumerateObject().Select(stat => $"{stat.Name} {stat.Value.GetProperty("minimum").GetInt32()}-{stat.Value.GetProperty("maximum").GetInt32()}"));
    }

    [Fact]
    public async Task PublicJobs_WarriorListsSkillsInTeachingOrder()
    {
        await using var official = new EndlessBattleApiFactory(database);
        var factory = official.WithWebHostBuilder(builder =>
            builder.UseSetting("Content:Directory", Path.Combine(BackendPaths.BackendDirectory, "content")));

        var jobs = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/jobs", CancellationToken);

        var warrior = jobs.EnumerateArray().Single(job => job.GetProperty("id").GetString() == "job-6");
        var skills = warrior.GetProperty("skills").EnumerateArray().ToList();
        Assert.Equal(["爆氣", "防禦心得", "破滅一擊", "嗜血"], skills.Select(skill => skill.GetProperty("name").GetString()));
        Assert.Equal(["skill-1", "skill-61", "skill-71", "skill-77"], skills.Select(skill => skill.GetProperty("id").GetString()));

        var burst = skills[0];
        Assert.Equal(10, burst.GetProperty("maxLevel").GetInt32());
        Assert.Equal(JsonValueKind.Null, burst.GetProperty("prerequisite").ValueKind);
        Assert.Equal(Enumerable.Range(1, 10), burst.GetProperty("levels").EnumerateArray().Select(level => level.GetProperty("level").GetInt32()));

        var defense = skills[1];
        Assert.Equal(5, defense.GetProperty("maxLevel").GetInt32());
        var prerequisite = defense.GetProperty("prerequisite");
        Assert.Equal("skill-1", prerequisite.GetProperty("skillId").GetString());
        Assert.Equal("爆氣", prerequisite.GetProperty("name").GetString());
        Assert.Equal(3, prerequisite.GetProperty("level").GetInt32());
        Assert.Equal(5, defense.GetProperty("levels").GetArrayLength());
        Assert.Equal(JsonValueKind.String, defense.GetProperty("description").ValueKind);
    }

    // 職業介紹冊只顯示名稱、入門或前置與描述，第一階職業教的每一招都要有描述
    [Fact]
    public async Task PublicJobs_StarterSkillsCarryDescriptions()
    {
        await using var official = new EndlessBattleApiFactory(database);
        var factory = official.WithWebHostBuilder(builder =>
            builder.UseSetting("Content:Directory", Path.Combine(BackendPaths.BackendDirectory, "content")));

        var jobs = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/public/jobs", CancellationToken);

        var starterSkills = jobs.EnumerateArray()
            .Where(job => job.GetProperty("tier").GetInt32() == 1)
            .SelectMany(job => job.GetProperty("skills").EnumerateArray())
            .ToList();
        Assert.Equal(16, starterSkills.Count);
        Assert.All(starterSkills, skill => Assert.False(
            string.IsNullOrWhiteSpace(skill.GetProperty("description").GetString()),
            $"{skill.GetProperty("name").GetString()} 沒有描述"));
        var burst = starterSkills.First(skill => skill.GetProperty("id").GetString() == "skill-1");
        Assert.Equal("爆發體內的鬥氣，接下來幾回合物攻提升。", burst.GetProperty("description").GetString());
    }

    [Fact]
    public async Task PublicJobs_LevelSummaryMatchesSkillPage()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(ApiClientExtensions.NewUsername()[..12]))).EnsureSuccessStatusCode();

        // 技能頁：還沒學的技能，下一級（Lv1）的效果文字
        var skillPage = await client.GetFromJsonAsync<JsonElement>("/api/skills", CancellationToken);
        var pageSummaries = skillPage.GetProperty("skills").EnumerateArray()
            .ToDictionary(skill => skill.GetProperty("id").GetString()!, skill => skill.GetProperty("next").GetProperty("summary").GetString());
        var jobs = await client.GetFromJsonAsync<JsonElement>("/api/public/jobs", CancellationToken);
        var warrior = jobs.EnumerateArray().Single(job => job.GetProperty("id").GetString() == "warrior");

        var jobSummaries = warrior.GetProperty("skills").EnumerateArray()
            .ToDictionary(skill => skill.GetProperty("id").GetString()!, skill => skill.GetProperty("levels")[0].GetProperty("summary").GetString());
        Assert.NotEmpty(jobSummaries);
        Assert.Equal(pageSummaries.Where(entry => jobSummaries.ContainsKey(entry.Key)).OrderBy(entry => entry.Key), jobSummaries.OrderBy(entry => entry.Key));

        // 其餘各級：技能頁升級後顯示的文字同樣來自 SkillSummary
        var content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));
        foreach (var skill in warrior.GetProperty("skills").EnumerateArray())
        {
            var definition = content.GetSkill(skill.GetProperty("id").GetString()!);
            Assert.Equal(
                definition.Levels.Select(SkillSummary.Of),
                skill.GetProperty("levels").EnumerateArray().Select(level => level.GetProperty("summary").GetString()));
        }
    }

    private static IEnumerable<string> Options(JsonElement settings, string name) =>
        settings.GetProperty(name).EnumerateArray().Select(option => $"{option.GetProperty("id").GetInt32()}:{option.GetProperty("name").GetString()}");

    // 用測試簽章金鑰簽一張 60 天前發的 token（有效期 30 天，已過期）
    private static string ExpiredToken()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new JwtOptions { SigningKey = "test-only-signing-key-0123456789abcdef0123" });
        return new JwtTokenIssuer(options, new ShiftedTimeProvider(TimeSpan.FromDays(-60))).Issue(1);
    }

    private sealed class ShiftedTimeProvider(TimeSpan offset) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + offset;
    }
}
