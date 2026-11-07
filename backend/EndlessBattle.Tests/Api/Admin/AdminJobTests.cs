using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminJobTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Jobs_UseCompleteFieldNames()
    {
        using var directories = new AdminContentDirectories(Path.Combine(BackendPaths.BackendDirectory, "content"));
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);

        var berserker = await client.GetFromJsonAsync<JsonElement>("/admin/content/jobs/job-10", CancellationToken);
        var warrior = await client.GetFromJsonAsync<JsonElement>("/admin/content/jobs/job-6", CancellationToken);

        Assert.Equal("job-6", berserker.GetProperty("parentJobId").GetString());
        Assert.False(berserker.TryGetProperty("parentId", out _));
        Assert.Equal(JsonValueKind.Null, warrior.GetProperty("parentJobId").ValueKind);
        var strength = warrior.GetProperty("growth").GetProperty("strength");
        Assert.Equal(1, strength.GetProperty("minimum").GetInt32());
        Assert.Equal(3, strength.GetProperty("maximum").GetInt32());
    }

    [Fact]
    public async Task Jobs_StatRequirementUsesTheStatName()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var jobs = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/jobs", CancellationToken);

        var requirement = jobs.EnumerateArray().SelectMany(job => job.GetProperty("statRequirements").EnumerateArray()).First();

        Assert.Contains(requirement.GetProperty("stat").GetString(), new[] { "Strength", "Intelligence", "Life", "Vitality", "Agility", "Charisma", "Faith" });
    }

    [Fact]
    public async Task Jobs_SaveWritesOnlyThatJob()
    {
        using var directories = new AdminContentDirectories(Path.Combine(BackendPaths.BackendDirectory, "content"));
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var official = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "jobs.json"), CancellationToken);
        var warrior = await client.GetFromJsonAsync<JsonObject>("/admin/content/jobs/job-6", CancellationToken);
        warrior!["growth"]!["strength"] = new JsonObject { ["minimum"] = 2, ["maximum"] = 4 };

        var response = await client.PutAsJsonAsync("/admin/content/jobs/job-6", warrior, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        var draft = await File.ReadAllLinesAsync(Path.Combine(directories.Draft, "jobs.json"), CancellationToken);
        var changed = Assert.Single(draft.Where((line, index) => line != official[index]));
        Assert.Contains("\"str\": [2, 4]", changed);
    }

    [Fact]
    public async Task Jobs_RejectUnknownStat()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var job = await client.GetFromJsonAsync<JsonObject>("/admin/content/jobs/warrior", CancellationToken);
        job!["statRequirements"] = new JsonArray(new JsonObject { ["stat"] = "Luck", ["minimum"] = 1 });

        var response = await client.PutAsJsonAsync("/admin/content/jobs/warrior", job, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("statRequirements[0].stat", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }
}
