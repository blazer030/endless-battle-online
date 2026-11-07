using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Api.Admin;

public class AdminMonsterTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string OfficialContent => Path.Combine(BackendPaths.BackendDirectory, "content");

    private static string ReadDraft(AdminContentDirectories directories, string file) =>
        File.ReadAllText(Path.Combine(directories.Draft, file));

    private static async Task<JsonObject> GetMonsterAsync(HttpClient client, string id) =>
        (await client.GetFromJsonAsync<JsonObject>($"/admin/content/monsters/{id}", CancellationToken))!;

    [Fact]
    public async Task Monsters_UseCompleteFieldNames()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);

        var monster = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/monsters/monster-1", CancellationToken);

        Assert.Equal(19, monster.GetProperty("attack").GetInt32());
        Assert.Equal(5, monster.GetProperty("magicDefense").GetInt32());
        Assert.Equal(0, monster.GetProperty("encounterRate").GetInt32());
        Assert.Equal(32, monster.GetProperty("hitPoints").GetInt32());
        Assert.Equal("Wind", monster.GetProperty("element").GetString());
        Assert.Equal(1, monster.GetProperty("subPlaceIndexes")[0].GetInt32());
        var drop = monster.GetProperty("drops")[0];
        Assert.Equal("item-204", drop.GetProperty("itemId").GetString());
        Assert.Equal(90, drop.GetProperty("dropRarity").GetInt32());
        var skill = monster.GetProperty("skills")[0];
        Assert.Equal("skill-1", skill.GetProperty("skillId").GetString());
        Assert.Equal(2, skill.GetProperty("level").GetInt32());
        Assert.Equal(10, skill.GetProperty("activationRate").GetInt32());
        Assert.False(monster.TryGetProperty("at", out _));
    }

    [Fact]
    public async Task Monsters_ListNpcFields()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);

        var monsters = await directories.CreateClient(factory).GetFromJsonAsync<JsonElement>("/admin/content/monsters", CancellationToken);

        var npc = monsters.EnumerateArray().Single(monster => monster.GetProperty("isNpc").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, npc.GetProperty("exclusiveMissionId").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, npc.GetProperty("npcMessage").ValueKind);
    }

    [Theory]
    [InlineData("level", "\"high\"", "level")]
    [InlineData("element", "\"Lightning\"", "element")]
    [InlineData("element", "2", "element")]
    [InlineData("at", "19", "at")]
    public async Task Monsters_RejectBodyWithWrongShape(string field, string value, string named)
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster[field] = JsonNode.Parse(value);

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(named, error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Monsters_RejectBodyMissingARequiredField()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster.Remove("level");

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("level", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Monsters_RejectNestedFieldWithItsPath()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["drops"]![0]!.AsObject().Remove("dropRarity");

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("drops[0].dropRarity", error.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Monsters_RejectMissingSkill()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["skills"] = new JsonArray(new JsonObject { ["skillId"] = "no-such-skill", ["level"] = 1, ["activationRate"] = 10 });

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("skills[0].skillId", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Monsters_CreateRejectsMissingSkill()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["id"] = "test-copy";
        monster["skills"] = new JsonArray(
            new JsonObject { ["skillId"] = "burst", ["level"] = 1, ["activationRate"] = 10 },
            new JsonObject { ["skillId"] = "no-such-skill", ["level"] = 1, ["activationRate"] = 10 });

        var response = await client.PostAsJsonAsync("/admin/content/monsters", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("skills[1].skillId", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Monsters_SaveSkillsWithMonster()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var officialRows = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "monster-skills.json"), CancellationToken);
        var officialMonsters = await File.ReadAllLinesAsync(Path.Combine(directories.Official, "monsters.json"), CancellationToken);
        var monster = await GetMonsterAsync(client, "monster-1");
        monster["level"] = 3;
        monster["skills"] = new JsonArray(new JsonObject { ["skillId"] = "skill-1", ["level"] = 3, ["activationRate"] = 20 });

        var response = await client.PutAsJsonAsync("/admin/content/monsters/monster-1", monster, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        var rows = ReadDraft(directories, "monster-skills.json").TrimEnd('\n').Split('\n');
        Assert.Equal(officialRows.Length, rows.Length);
        var changed = Assert.Single(rows.Where((row, index) => row != officialRows[index]));
        Assert.Equal("""    {"monsterId": "monster-1", "skillId": "skill-1", "level": 3, "rate": 20},""", changed);
        var monsters = ReadDraft(directories, "monsters.json").TrimEnd('\n').Split('\n');
        var changedMonster = Assert.Single(monsters.Where((row, index) => row != officialMonsters[index]));
        Assert.Contains("\"id\": \"monster-1\"", changedMonster);
        Assert.Contains("\"level\": 3", changedMonster);
    }

    [Fact]
    public async Task Monsters_RemovingEverySkillRemovesTheRows()
    {
        using var directories = new AdminContentDirectories(OfficialContent);
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "monster-1");
        monster["skills"] = new JsonArray();

        var response = await client.PutAsJsonAsync("/admin/content/monsters/monster-1", monster, CancellationToken);

        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}");
        Assert.DoesNotContain("\"monsterId\": \"monster-1\"", ReadDraft(directories, "monster-skills.json"));
        Assert.Empty((await GetMonsterAsync(client, "monster-1"))["skills"]!.AsArray());
    }

    [Fact]
    public async Task Monsters_WriteFailureLeavesDraftUnchanged()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var host = directories.Configure(factory);
        var client = host.CreateClient();
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["level"] = 3;
        (await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken)).EnsureSuccessStatusCode();
        var monstersBefore = ReadDraft(directories, "monsters.json");
        var skillsBefore = ReadDraft(directories, "monster-skills.json");
        // 第二個檔案的暫存路徑被目錄佔住：寫到它時一定失敗
        var blocked = Path.Combine(directories.Draft, "monster-skills.json.writing");
        Directory.CreateDirectory(blocked);
        monster["level"] = 4;
        monster["skills"] = new JsonArray(new JsonObject { ["skillId"] = "burst", ["level"] = 1, ["activationRate"] = 10 });

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(monstersBefore, ReadDraft(directories, "monsters.json"));
        Assert.Equal(skillsBefore, ReadDraft(directories, "monster-skills.json"));
        Assert.False(File.Exists(Path.Combine(directories.Draft, "monsters.json.writing")));
    }

    [Fact]
    public async Task Monsters_InvalidPlaceStillSaved()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["placeId"] = 9;

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-sprite", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.False(status.GetProperty("valid").GetBoolean());
        Assert.Equal("怪物 test-sprite 引用了不存在的地區：9", status.GetProperty("error").GetString());
        Assert.Equal(9, (await GetMonsterAsync(client, "test-sprite"))["placeId"]!.GetValue<int>());
    }

    [Fact]
    public async Task Monsters_CreateAddsTheRecordAndItsSkillsLast()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["id"] = "test-new-monster";
        monster["skills"] = new JsonArray(new JsonObject { ["skillId"] = "burst", ["level"] = 1, ["activationRate"] = 10 });

        var response = await client.PostAsJsonAsync("/admin/content/monsters", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/admin/content/monsters/test-new-monster", response.Headers.Location?.ToString());
        Assert.Contains("""{"monsterId": "test-new-monster", "skillId": "burst", "level": 1, "rate": 10}""", ReadDraft(directories, "monster-skills.json"));
        var duplicate = await client.PostAsJsonAsync("/admin/content/monsters", monster, CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Monsters_UpdateIdDifferentFromPathReturnsBadRequestNamingTheId()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");

        var response = await client.PutAsJsonAsync("/admin/content/monsters/test-wolf", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("id", error.GetProperty("field").GetString());
        Assert.False(Directory.Exists(directories.Draft));
    }

    [Fact]
    public async Task Monsters_CreateWithBlankIdIsRejected()
    {
        using var directories = new AdminContentDirectories();
        await using var factory = new EndlessBattleApiFactory(database);
        var client = directories.CreateClient(factory);
        var monster = await GetMonsterAsync(client, "test-sprite");
        monster["id"] = "  ";

        var response = await client.PostAsJsonAsync("/admin/content/monsters", monster, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(Directory.Exists(directories.Draft));
    }
}
