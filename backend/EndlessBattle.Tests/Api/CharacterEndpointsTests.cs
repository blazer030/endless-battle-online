using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Domain.Items;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Api;

public class CharacterEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // 名稱在資料庫中唯一，加上隨機後綴避免測試之間互相影響
    private static string UniqueName(string prefix) => $"{prefix}{Random.Shared.Next(100000, 999999)}";

    [Fact]
    public async Task Create_ReturnsCharacterAtStartingPlace()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("墨客");

        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.Equal(0, body.GetProperty("exp").GetInt64());
        Assert.Equal(1, body.GetProperty("placeId").GetInt32());
        Assert.Equal(1, body.GetProperty("subPlace").GetInt32());
        Assert.False(body.TryGetProperty("currentStageId", out _));

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(body.ToString(), me.ToString());
    }

    // 戰士、配點 4/2/3/1 → Base (14,12,13,10,11,10,10)
    [Fact]
    public async Task Create_ReturnsFullCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("全貌");

        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name, str: 4, smart: 2, life: 3, agi: 1));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(2000, body.GetProperty("money").GetInt64());
        Assert.Equal("warrior", body.GetProperty("jobId").GetString());
        Assert.Equal("測試戰士", body.GetProperty("jobName").GetString());
        Assert.Equal("烈焰斬", body.GetProperty("specialAttackName").GetString());
        Assert.Equal(3, body.GetProperty("element").GetInt32());
        var stats = body.GetProperty("stats");
        Assert.Equal(14, stats.GetProperty("at").GetInt32());
        Assert.Equal(12, stats.GetProperty("mat").GetInt32());
        Assert.Equal(10, stats.GetProperty("df").GetInt32());
        Assert.Equal(10, stats.GetProperty("mdf").GetInt32());
        Assert.Equal(13, stats.GetProperty("life").GetInt32());
        Assert.Equal(375, stats.GetProperty("hpMax").GetInt32());
        Assert.Equal(91, stats.GetProperty("spMax").GetInt32());
    }

    // TestContent 的起始地：測試大陸・測試平原・甲地（正式內容為 中央大陸・中央平原・香茅綠地）
    [Fact]
    public async Task Me_ReturnsPlaceNames()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("地名")));

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);

        Assert.Equal(1, me.GetProperty("placeId").GetInt32());
        Assert.Equal("測試平原", me.GetProperty("placeName").GetString());
        Assert.Equal(1, me.GetProperty("subPlace").GetInt32());
        Assert.Equal("甲地", me.GetProperty("subPlaceName").GetString());
        Assert.Equal("測試大陸", me.GetProperty("areaName").GetString());
    }

    [Fact]
    public async Task Create_ReturnsFullStaminaAndNoProficiency()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("體力")));

        var body = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(50, body.GetProperty("actNum").GetInt32());
        Assert.Equal(50, body.GetProperty("actNumMax").GetInt32());
        Assert.Empty(body.GetProperty("jobProficiency").EnumerateArray());
    }

    public static TheoryData<object?> InvalidAllocations => new()
    {
        new { str = 5, smart = 2, life = 2, agi = 2 },
        new { str = 0, smart = 4, life = 3, agi = 3 },
        new { str = -1, smart = 4, life = 4, agi = 3 },
        new { str = int.MaxValue, smart = int.MaxValue, life = 1, agi = 1 },
        new { str = 2.5, smart = 2, life = 2, agi = 2 },
        new { str = 2, smart = 2, life = 2 },
        (object?)null,
    };

    [Theory]
    [MemberData(nameof(InvalidAllocations))]
    public async Task Create_RejectsInvalidAllocation(object? allocation)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = new
        {
            name = UniqueName("配點"),
            sex = 1,
            element = 3,
            birth = 1,
            jobId = "warrior",
            allocation,
            specialAttackName = "烈焰斬",
        };

        var response = await client.CreateCharacterAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("sex")]
    [InlineData("element")]
    [InlineData("birth")]
    [InlineData("jobId")]
    [InlineData("specialAttackName")]
    [InlineData("allocation")]
    public async Task Create_Returns400_WhenAnyFieldIsMissing(string missingField)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = new Dictionary<string, object?>
        {
            ["name"] = UniqueName("缺欄"),
            ["sex"] = 1,
            ["element"] = 3,
            ["birth"] = 1,
            ["jobId"] = "warrior",
            ["allocation"] = new { str = 2, smart = 2, life = 2, agi = 2 },
            ["specialAttackName"] = "烈焰斬",
        };
        body.Remove(missingField);

        var response = await client.CreateCharacterAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns400_ForMalformedJson()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        using var content = new StringContent("{", System.Text.Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/characters", content, CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_AllowsUnderspentPoints()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        // 2/2/2/2 只花了 8 點
        var response = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("留點")));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(3, 3, 1, "warrior")]
    [InlineData(1, 7, 1, "warrior")]
    [InlineData(1, 3, 0, "warrior")]
    [InlineData(1, 3, 1, "ghost")]
    [InlineData(1, 3, 1, "berserker")]
    public async Task Create_RejectsInvalidChoices(int sex, int element, int birth, string jobId)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = new
        {
            name = UniqueName("選項"),
            sex,
            element,
            birth,
            jobId,
            allocation = new { str = 2, smart = 2, life = 2, agi = 2 },
            specialAttackName = "烈焰斬",
        };

        var response = await client.CreateCharacterAsync(body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/characters/me", CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_Returns409_WhenAccountAlreadyHasCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody(UniqueName("初")), CancellationToken);

        var second = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody(UniqueName("再")), CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Create_Returns409_WhenNameTaken()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var firstClient = await factory.CreateAuthenticatedClientAsync();
        var secondClient = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("同名");

        await firstClient.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody(name), CancellationToken);
        var duplicate = await secondClient.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody(name), CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("墨")]
    [InlineData("一二三四五六七八九十甲乙丙")]
    [InlineData("   ")]
    public async Task Create_Returns400_ForInvalidName(string name)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var response = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody(name), CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 6 個文字單位（通過字數檢查），但有 15 個 code point，超過資料庫 VARCHAR(12)
    [Fact]
    public async Task Create_Returns400_ForNameExceedingStorageLength()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        const string meditator = "\U0001F9D8‍♂️";

        var response = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody($"修仙者{meditator}{meditator}{meditator}"), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_TrimsSurroundingWhitespace()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("劍心");

        var response = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody($"  {name}  "), CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(name, body.GetProperty("name").GetString());
    }

    private static object SettingsBody(string? attack = "Magic", int potionHp = 30, int potionSp = -1, int restHp = 20, int restSp = -1, int escapeHp = 10, string specialAttackName = "破軍") =>
        new { attack, potionHpPercent = potionHp, potionSpPercent = potionSp, restHpPercent = restHp, restSpPercent = restSp, escapeHpPercent = escapeHp, specialAttackName };

    [Fact]
    public async Task Settings_UpdateAndReadBack()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("設定")))).EnsureSuccessStatusCode();
        var created = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(
            """{"attack":null,"potionHpPercent":15,"potionSpPercent":100,"restHpPercent":15,"restSpPercent":15,"escapeHpPercent":0}""",
            created.GetProperty("settings").GetRawText());

        var response = await client.PutAsJsonAsync("/api/characters/me/settings", SettingsBody(), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(
            """{"attack":"Magic","potionHpPercent":30,"potionSpPercent":-1,"restHpPercent":20,"restSpPercent":-1,"escapeHpPercent":10}""",
            me.GetProperty("settings").GetRawText());
        Assert.Equal(body.GetProperty("settings").GetRawText(), me.GetProperty("settings").GetRawText());
        Assert.Equal("破軍", me.GetProperty("specialAttackName").GetString());
    }

    [Fact]
    public async Task Settings_Returns400_WithReasons()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("設定")))).EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync("/api/characters/me/settings", SettingsBody(potionHp: 101), CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("invalidPercent", problem.GetProperty("reasons")[0].GetString());
    }

    [Fact]
    public async Task Settings_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/characters/me/settings", SettingsBody(), CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Endpoints_Require_Authentication()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody("無名氏"), CancellationToken);
        var me = await client.GetAsync("/api/characters/me", CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }

    // 建角 10 秒後喝經驗密藥（同一筆交易裡先結算這 10 秒）：使用結果帶出效果；列表只含 now < endsAt 的密藥（已到期的物攻密藥不列），1 小時後全部到期
    [Fact]
    public async Task Buffs_ListsOnlyActive()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("密藥");
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name))).EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromSeconds(10));
        var now = factory.Clock.UtcNow;
        long elixir;
        await using (var context = database.CreateDbContext())
        {
            var characterId = await context.Characters.Where(character => character.Name == name).Select(character => character.Id).SingleAsync(CancellationToken);
            elixir = await new InventoryRepository(context).AddAsync(characterId, TestItems.Entry(0, "test-exp-elixir"), CancellationToken);
            await new BuffRepository(context).SaveAsync(
                characterId, new CharacterBuff(BuffKind.Attack, 1.2, now.AddHours(-1), now.AddSeconds(-1)), CancellationToken);
        }

        var used = await client.PostAsJsonAsync("/api/inventory/use", new { inventoryId = elixir }, CancellationToken);
        used.EnsureSuccessStatusCode();
        var buff = (await used.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("buff");
        Assert.Equal(("Exp", 1.5), (buff.GetProperty("kind").GetString(), buff.GetProperty("multiplier").GetDouble()));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.True(me.GetProperty("exp").GetInt64() > 0);

        var buffs = await client.GetFromJsonAsync<JsonElement>("/api/characters/me/buffs", CancellationToken);
        var active = Assert.Single(buffs.EnumerateArray());
        Assert.Equal("Exp", active.GetProperty("kind").GetString());
        Assert.Equal(1.5, active.GetProperty("multiplier").GetDouble());
        Assert.Equal(now.AddHours(1), active.GetProperty("endsAt").GetDateTimeOffset());

        factory.Clock.Advance(TimeSpan.FromHours(1));
        Assert.Empty((await client.GetFromJsonAsync<JsonElement>("/api/characters/me/buffs", CancellationToken)).EnumerateArray());
    }

    [Fact]
    public async Task Buffs_Returns404WithoutCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/characters/me/buffs", CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
