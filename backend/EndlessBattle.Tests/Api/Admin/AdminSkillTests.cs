using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminSkillTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string OfficialContent => Path.Combine(BackendPaths.BackendDirectory, "content");

    [Fact]
    public async Task Skills_UseCompleteFieldNames()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);

        var skill = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/skills/skill-1", CancellationToken);

        Assert.Equal("Active", skill.GetProperty("slotType").GetString());
        Assert.Equal("BeforeAttack", skill.GetProperty("timing").GetString());
        Assert.Equal("job-6", skill.GetProperty("jobIds")[0].GetString());
        Assert.Equal(JsonValueKind.Null, skill.GetProperty("prerequisite").ValueKind);
        var level = skill.GetProperty("levels")[0];
        Assert.Equal(500, level.GetProperty("learningCost").GetInt32());
        Assert.Equal(35, level.GetProperty("proficiencyRequired").GetInt32());
        var condition = level.GetProperty("conditions")[0];
        Assert.Equal("Chance", condition.GetProperty("kind").GetString());
        Assert.Equal(40, condition.GetProperty("percent").GetInt32());
        var effect = level.GetProperty("effects")[0];
        Assert.Equal("StatChange", effect.GetProperty("kind").GetString());
        Assert.Equal("Attack", effect.GetProperty("stat").GetString());
        Assert.False(effect.TryGetProperty("amount", out _));
    }

    [Fact]
    public async Task Skills_RejectUnknownSlotType()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var skill = await client.GetFromJsonAsync<JsonObject>("/admin/content/skills/burst", CancellationToken);
        skill!["slotType"] = "Sometimes";

        var response = await client.PutAsJsonAsync("/admin/content/skills/burst", skill, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("slotType", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Skills_RejectUnknownEffectKindAndFieldOfAnotherKind()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var skill = await client.GetFromJsonAsync<JsonObject>("/admin/content/skills/burst", CancellationToken);
        skill!["levels"]![0]!["effects"] = new JsonArray(new JsonObject { ["kind"] = "Teleport", ["rounds"] = 1 });

        var unknownKind = await client.PutAsJsonAsync("/admin/content/skills/burst", skill, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, unknownKind.StatusCode);
        var kindError = await unknownKind.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.StartsWith("levels[0].effects[0]", kindError.GetProperty("field").GetString(), StringComparison.Ordinal);

        skill["levels"]![0]!["effects"] = new JsonArray(new JsonObject { ["kind"] = "Silence", ["chance"] = 10, ["rounds"] = 1, ["stat"] = "Attack" });

        var extraField = await client.PutAsJsonAsync("/admin/content/skills/burst", skill, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, extraField.StatusCode);
        var fieldError = await extraField.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("levels[0].effects[0].stat", fieldError.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Skills_RejectMissingJob()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var skill = await client.GetFromJsonAsync<JsonObject>("/admin/content/skills/burst", CancellationToken);
        skill!["jobIds"] = new JsonArray("no-such-job");

        var response = await client.PutAsJsonAsync("/admin/content/skills/burst", skill, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("jobIds[0]", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Skills_SaveJobsWithTheSkill()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var officialRows = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "job-skills.json"), CancellationToken);
        var officialSkills = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "skills.json"), CancellationToken);
        var skill = await client.GetFromJsonAsync<JsonObject>("/admin/content/skills/skill-1", CancellationToken);
        skill!["jobIds"] = new JsonArray("job-7");
        skill["name"] = "改名";

        var response = await client.PutAsJsonAsync("/admin/content/skills/skill-1", skill, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        var rows = await File.ReadAllLinesAsync(Path.Combine(directories.Draft, "job-skills.json"), CancellationToken);
        var changedRow = Assert.Single(rows.Where((row, index) => row != officialRows[index]));
        Assert.Equal("""    {"jobId": "job-7", "skillId": "skill-1"},""", changedRow);
        var skills = await File.ReadAllLinesAsync(Path.Combine(directories.Draft, "skills.json"), CancellationToken);
        var changedSkill = Assert.Single(skills.Where((row, index) => row != officialSkills[index]));
        Assert.Contains("\"name\": \"改名\"", changedSkill);
    }

    [Fact]
    public async Task Skills_EffectThatBreaksARuleIsSavedAndReported()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var skill = await client.GetFromJsonAsync<JsonObject>("/admin/content/skills/burst", CancellationToken);
        var effect = new JsonObject { ["kind"] = "StatChange", ["target"] = "Caster", ["stat"] = "Attack", ["percent"] = 1, ["amount"] = 2, ["rounds"] = 1 };
        skill!["levels"]![0]!["effects"] = new JsonArray(effect);

        var response = await client.PutAsJsonAsync("/admin/content/skills/burst", skill, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.False(status.GetProperty("valid").GetBoolean());
        Assert.Contains("percent、amount、scaling", status.GetProperty("error").GetString());
    }
}
