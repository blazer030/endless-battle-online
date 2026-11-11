using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        Assert.Equal(0, body.GetProperty("experience").GetInt64());
        Assert.Equal(1, body.GetProperty("placeId").GetInt32());
        Assert.Equal(1, body.GetProperty("subPlace").GetInt32());
        Assert.False(body.TryGetProperty("currentStageId", out _));

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(body.ToString(), me.ToString());
    }

    // 規格範例：配點 4/2/3/1 的新角色
    [Fact]
    public async Task GetMe_UsesCompleteFieldNames()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = new
        {
            name = UniqueName("全名"),
            sex = 1,
            element = 3,
            birth = 1,
            jobId = "warrior",
            allocation = new { strength = 4, intelligence = 2, life = 3, agility = 1 },
        };
        Assert.Equal(HttpStatusCode.Created, (await client.CreateCharacterAsync(body)).StatusCode);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);

        Assert.Equal(50, me.GetProperty("healthPoints").GetInt32());
        Assert.Equal(25, me.GetProperty("manaPoints").GetInt32());
        Assert.Equal(0, me.GetProperty("experience").GetInt64());
        Assert.Equal(1000, me.GetProperty("nextLevelExperience").GetInt64());
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""{"strength":14,"intelligence":12,"life":13,"vitality":10,"agility":11,"charisma":10,"faith":10}"""),
            JsonNode.Parse(me.GetProperty("baseStats").GetRawText())));
        var stats = me.GetProperty("stats");
        foreach (var name in new[]
                 {
                     "strength", "intelligence", "life", "vitality", "agility", "charisma", "faith",
                     "attack", "magicAttack", "defense", "magicDefense", "maxHealthPoints", "maxManaPoints",
                 })
        {
            Assert.True(stats.TryGetProperty(name, out _), name);
        }

        var settings = me.GetProperty("settings");
        foreach (var name in new[]
                 {
                     "potionHealthPointsPercent", "potionManaPointsPercent", "restHealthPointsPercent", "restManaPointsPercent",
                     "escapeHealthPointsPercent",
                 })
        {
            Assert.True(settings.TryGetProperty(name, out _), name);
        }

        var abbreviated = new[] { "hp", "sp", "exp", "nextExp", "str", "smart", "vit", "agi", "au", "be", "at", "mat", "df", "mdf", "hpMax", "spMax" };
        Assert.DoesNotContain(abbreviated, name => me.TryGetProperty(name, out _) || stats.TryGetProperty(name, out _));
    }

    [Fact]
    public async Task Create_TakesCompleteAllocationNames()
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
            allocation = new { strength = 1, intelligence = 1, life = 1, agility = 7 },
        };

        var created = await client.CreateCharacterAsync(body);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var character = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(11, character.GetProperty("baseStats").GetProperty("strength").GetInt32());
        Assert.Equal(17, character.GetProperty("baseStats").GetProperty("agility").GetInt32());
    }

    [Fact]
    public async Task Create_WithoutSpecialAttackName_CreatesCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var body = new
        {
            name = UniqueName("無名"),
            sex = 1,
            element = 3,
            birth = 1,
            jobId = "warrior",
            allocation = new { strength = 2, intelligence = 4, life = 2, agility = 2 },
        };

        var created = await client.CreateCharacterAsync(body);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var character = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.False(character.TryGetProperty("specialAttackName", out _));
    }

    // 戰士、配點 4/2/3/1 → Base (14,12,13,10,11,10,10)
    [Fact]
    public async Task Create_ReturnsFullCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = UniqueName("全貌");

        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name, strength: 4, intelligence: 2, life: 3, agility: 1));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(2000, body.GetProperty("money").GetInt64());
        Assert.Equal("warrior", body.GetProperty("jobId").GetString());
        Assert.Equal("測試戰士", body.GetProperty("jobName").GetString());
        Assert.Equal(3, body.GetProperty("element").GetInt32());
        var stats = body.GetProperty("stats");
        Assert.Equal(14, stats.GetProperty("attack").GetInt32());
        Assert.Equal(12, stats.GetProperty("magicAttack").GetInt32());
        Assert.Equal(10, stats.GetProperty("defense").GetInt32());
        Assert.Equal(10, stats.GetProperty("magicDefense").GetInt32());
        Assert.Equal(13, stats.GetProperty("life").GetInt32());
        Assert.Equal(375, stats.GetProperty("maxHealthPoints").GetInt32());
        Assert.Equal(91, stats.GetProperty("maxManaPoints").GetInt32());
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
    public async Task Create_ReturnsNoProficiency()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("體力")));

        var body = await created.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Empty(body.GetProperty("jobProficiency").EnumerateArray());
    }

    public static TheoryData<object?> InvalidAllocations => new()
    {
        new { str = 5, smart = 2, life = 2, agi = 2 },
        new { str = 0, smart = 4, life = 3, agi = 3 },
        new { str = -1, smart = 4, life = 4, agi = 3 },
        new { str = int.MaxValue, smart = int.MaxValue, life = 1, agi = 1 },
        // 以下兩筆其餘欄位補到 10 點，400 只會來自非整數、缺欄位
        new { str = 2.5, smart = 4, life = 2, agi = 2 },
        new { str = 4, smart = 3, life = 3 },
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
            ["allocation"] = new { str = 2, smart = 4, life = 2, agi = 2 },
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
    public async Task Create_Returns400_WhenPointsUnspent()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        // 1/1/1/3 只花了 6 點
        var response = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("留點"), strength: 1, intelligence: 1, life: 1, agility: 3));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("能力點數必須剛好分完 10 點", problem.GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/characters/me", CancellationToken)).StatusCode);
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
            allocation = new { strength = 2, intelligence = 4, life = 2, agility = 2 },
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
        var (detail, reasons) = await ConflictOf(second);
        Assert.Equal("此帳號已有角色", detail);
        Assert.Equal(["accountHasCharacter"], reasons);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.StartsWith("初", me.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Create_Returns409_WhenNameTakenInOtherLetterCase()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var firstClient = await factory.CreateAuthenticatedClientAsync();
        var secondClient = await factory.CreateAuthenticatedClientAsync();
        var name = $"Allen{Random.Shared.Next(100000, 999999)}";

        (await firstClient.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name))).EnsureSuccessStatusCode();
        var duplicate = await secondClient.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var (detail, reasons) = await ConflictOf(duplicate);
        Assert.Equal("角色名稱已被使用", detail);
        Assert.Equal(["characterNameTaken"], reasons);
    }

    // spec 名字長度範例表的「艾倫」與「12 個中文字」列；名字要唯一，用隨機漢字
    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    public async Task Create_AcceptsNameLengthBounds(int length)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        var name = string.Concat(Enumerable.Range(0, length).Select(_ => (char)Random.Shared.Next(0x4E00, 0x9FA6)));

        var response = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(name));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(1, 1, 1, 3, "能力點數必須剛好分完 10 點")]
    [InlineData(4, 2, 3, 2, "能力點數必須剛好分完 10 點")]
    [InlineData(0, 4, 3, 3, "每項能力至少要分配 1 點")]
    public async Task Create_Returns400_WithAllocationDetail(int str, int smart, int life, int agi, string detail)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("配點"), strength: str, intelligence: smart, life: life, agility: agi));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(detail, await DetailOf(response));
    }

    [Fact]
    public async Task Create_Returns400_ForAdvancedJob()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("轉職"), jobId: "berserker"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("只能選擇初階職業", await DetailOf(response));
    }

    private static async Task<string?> DetailOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("detail").GetString();

    // 409 的訊息與原因代碼（回應內容只能讀一次）
    private static async Task<(string? Detail, string?[] Reasons)> ConflictOf(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        return (problem.GetProperty("detail").GetString(),
            problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()).ToArray());
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

    // spec 名字長度範例表的「艾」列；另外兩列是超過 12 字與只有空白
    [Theory]
    [InlineData("艾")]
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

    private static object SettingsBody(string? attack = "Magic", int potionHp = 30, int potionSp = -1, int restHp = 20, int restSp = -1, int escapeHp = 10) =>
        new
        {
            attack,
            potionHealthPointsPercent = potionHp,
            potionManaPointsPercent = potionSp,
            restHealthPointsPercent = restHp,
            restManaPointsPercent = restSp,
            escapeHealthPointsPercent = escapeHp,
        };

    [Fact]
    public async Task Settings_UpdateAndReadBack()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("設定")))).EnsureSuccessStatusCode();
        var created = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(
            """{"attack":null,"potionHealthPointsPercent":15,"potionManaPointsPercent":100,"restHealthPointsPercent":15,"restManaPointsPercent":15,"escapeHealthPointsPercent":0}""",
            created.GetProperty("settings").GetRawText());

        var response = await client.PutAsJsonAsync("/api/characters/me/settings", SettingsBody(), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(
            """{"attack":"Magic","potionHealthPointsPercent":30,"potionManaPointsPercent":-1,"restHealthPointsPercent":20,"restManaPointsPercent":-1,"escapeHealthPointsPercent":10}""",
            me.GetProperty("settings").GetRawText());
        Assert.Equal(body.GetProperty("settings").GetRawText(), me.GetProperty("settings").GetRawText());
    }

    [Fact]
    public async Task UpdateSettings_WithoutSpecialAttackName_SavesSettings()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("設定")))).EnsureSuccessStatusCode();

        var saved = await client.PutAsJsonAsync("/api/characters/me/settings", SettingsBody(potionHp: 42), CancellationToken);
        var refused = await client.PutAsJsonAsync("/api/characters/me/settings", SettingsBody(potionHp: 101), CancellationToken);

        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var body = await saved.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(42, body.GetProperty("settings").GetProperty("potionHealthPointsPercent").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal("invalidPercent", problem.GetProperty("reasons")[0].GetString());
    }

    // 舊客戶端還會送這個欄位：直接忽略，不回錯誤
    [Fact]
    public async Task UpdateSettings_WithOldSpecialAttackNameField_IgnoresIt()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody(UniqueName("設定")))).EnsureSuccessStatusCode();
        var oldClientBody = new
        {
            attack = "Magic",
            potionHealthPointsPercent = 42,
            potionManaPointsPercent = -1,
            restHealthPointsPercent = 20,
            restManaPointsPercent = -1,
            escapeHealthPointsPercent = 10,
            specialAttackName = "破軍",
        };

        var response = await client.PutAsJsonAsync("/api/characters/me/settings", oldClientBody, CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
            elixir = await new InventoryRepository(context, TestGameContent.Instance).AddAsync(characterId, TestItems.Entry(0, "test-exp-elixir"), CancellationToken);
            await new BuffRepository(context).SaveAsync(
                characterId, new CharacterBuff(BuffKind.Attack, 1.2, now.AddHours(-1), now.AddSeconds(-1)), CancellationToken);
        }

        var used = await client.PostAsJsonAsync("/api/inventory/use", new { inventoryId = elixir }, CancellationToken);
        used.EnsureSuccessStatusCode();
        var buff = (await used.Content.ReadFromJsonAsync<JsonElement>(CancellationToken)).GetProperty("buff");
        Assert.Equal(("Exp", 1.5), (buff.GetProperty("kind").GetString(), buff.GetProperty("multiplier").GetDouble()));
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.True(me.GetProperty("experience").GetInt64() > 0);

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
