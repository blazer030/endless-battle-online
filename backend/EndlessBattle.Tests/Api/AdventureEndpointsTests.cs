using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api;

public class AdventureEndpointsTests(MySqlDatabaseFixture database)
{

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await client.PostAsJsonAsync("/api/characters", ApiClientExtensions.ValidCharacterBody($"行者{Random.Shared.Next(100000, 999999)}"), CancellationToken);
        created.EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<JsonElement> AdvanceAsync(HttpClient client)
    {
        var response = await client.PostAsync("/api/adventure/advance", null, CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    [Fact]
    public async Task Advance_AfterTenSeconds_ReturnsEventsAndPersistsGains()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var body = await AdvanceAsync(client);

        // 新角色 Hp 50／350 低於自動住宿門檻，先住宿 5 秒再開打
        var events = body.GetProperty("events").EnumerateArray().ToList();
        Assert.Equal("rest", events[0].GetProperty("type").GetString());
        Assert.Equal("encounter", events[1].GetProperty("type").GetString());
        Assert.Contains(events, battleEvent => battleEvent.GetProperty("type").GetString() == "round");
        Assert.Contains(events, battleEvent => battleEvent.GetProperty("type").GetString() == "attack");
        Assert.True(body.GetProperty("eventsCoverAll").GetBoolean());
        var summary = body.GetProperty("summary");
        Assert.True(summary.GetProperty("fights").GetInt32() >= 1);
        Assert.Equal(
            events.Count(battleEvent => battleEvent.GetProperty("type").GetString() == "encounter"),
            summary.GetProperty("fights").GetInt32());

        var character = body.GetProperty("character");
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        Assert.Equal(character.ToString(), me.ToString());
    }

    private static List<JsonElement> EventsOfType(JsonElement body, string type) => body.GetProperty("events").EnumerateArray()
        .Where(battleEvent => battleEvent.GetProperty("type").GetString() == type)
        .ToList();

    // 直接寫入 300 技能點（burst lv1 的門檻）再走 API 學習與裝備；新角色先住宿回滿 SP，之後每場開頭必施放 burst（耗 5）
    [Fact]
    public async Task Advance_UsesEquippedSkills()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using (var scope = factory.Services.CreateScope())
        {
            var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
                .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
            await scope.ServiceProvider.GetRequiredService<IJobProficiencyRepository>()
                .AddAsync(character!.Id, "warrior", 0, 300, CancellationToken);
        }

        (await client.PostAsJsonAsync("/api/skills/learn", new { skillId = "burst" }, CancellationToken)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync("/api/skills/slots", new { slots = new[] { new { slot = 1, skillId = "burst" } } }, CancellationToken))
            .EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromSeconds(20));

        var body = await AdvanceAsync(client);

        var casts = EventsOfType(body, "skill");
        Assert.NotEmpty(casts);
        Assert.All(casts, cast => Assert.Equal("burst", cast.GetProperty("skillId").GetString()));
        Assert.Equal(casts.Count, body.GetProperty("summary").GetProperty("skillsCast").GetInt32());
        var result = body.GetProperty("character");
        Assert.True(result.GetProperty("manaPoints").GetInt32() < result.GetProperty("stats").GetProperty("maxManaPoints").GetInt32());
    }

    [Fact]
    public async Task Advance_WithoutSkills_StillWorks()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(20));

        var body = await AdvanceAsync(client);

        Assert.NotEmpty(EventsOfType(body, "encounter"));
        Assert.Empty(EventsOfType(body, "skill"));
        Assert.Equal(0, body.GetProperty("summary").GetProperty("skillsCast").GetInt32());
    }

    [Fact]
    public async Task Advance_UsesCharacterStats()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var strong = await factory.CreateAuthenticatedClientAsync();
        var weak = await factory.CreateAuthenticatedClientAsync();
        (await strong.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"力士{Random.Shared.Next(100000, 999999)}", strength: 7, intelligence: 1, life: 1, agility: 1))).EnsureSuccessStatusCode();
        (await weak.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"命士{Random.Shared.Next(100000, 999999)}", strength: 1, intelligence: 1, life: 7, agility: 1))).EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromSeconds(10));

        var strongBody = await AdvanceAsync(strong);
        var weakBody = await AdvanceAsync(weak);

        Assert.True(MaxPlayerDamage(strongBody) > MaxPlayerDamage(weakBody));
    }

    private static int MaxPlayerDamage(JsonElement body) => body.GetProperty("events").EnumerateArray()
        .Where(battleEvent => battleEvent.GetProperty("type").GetString() == "attack"
            && string.Equals(battleEvent.GetProperty("attacker").GetString(), "player", StringComparison.OrdinalIgnoreCase))
        .Max(battleEvent => battleEvent.GetProperty("damage").GetInt32());

    [Fact]
    public async Task Advance_ReturnsNothing_WhenClockIsBehindLastSettlement()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(-5));

        var body = await AdvanceAsync(client);

        Assert.Empty(body.GetProperty("events").EnumerateArray());
        Assert.Equal(0, body.GetProperty("summary").GetProperty("fights").GetInt32());
        Assert.Equal(0, body.GetProperty("summary").GetProperty("exp").GetInt64());
    }

    // 離線 6 小時：上萬場一次算完；事件只含最後 30 秒開始的場次，結算到最後一場打完，馬上再推進是空的
    [Fact]
    public async Task Advance_SixHours_ReturnsSummaryQuickly()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var created = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        factory.Clock.Advance(TimeSpan.FromHours(6));

        var stopwatch = Stopwatch.StartNew();
        var body = await AdvanceAsync(client);
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 2000, $"推進花了 {stopwatch.ElapsedMilliseconds} ms");
        const double totalSeconds = 6 * 3600;
        var events = body.GetProperty("events").EnumerateArray().ToList();
        Assert.NotEmpty(events);
        Assert.All(events, battleEvent => Assert.True(battleEvent.GetProperty("atSeconds").GetDouble() >= totalSeconds - 30));
        Assert.False(body.GetProperty("eventsCoverAll").GetBoolean());
        var summary = body.GetProperty("summary");
        Assert.True(summary.GetProperty("fights").GetInt32() > 1000);
        Assert.True(summary.GetProperty("levelsGained").GetInt32() > 0);
        Assert.True(body.GetProperty("character").GetProperty("level").GetInt32() > 1);
        string[] baseStats = ["strength", "intelligence", "life", "vitality", "agility", "charisma", "faith"];
        var statsBefore = created.GetProperty("stats");
        var statsAfter = body.GetProperty("character").GetProperty("stats");
        Assert.Contains(baseStats, stat => statsAfter.GetProperty(stat).GetInt32() > statsBefore.GetProperty(stat).GetInt32());

        // 回應不含 lastSettledAt：馬上再推進是空的 → 已結算到現在之後；再過 60 秒有新的一場 → 結算點不晚於現在 + 60 秒
        var immediatelyAfter = await AdvanceAsync(client);
        Assert.Empty(immediatelyAfter.GetProperty("events").EnumerateArray());
        factory.Clock.Advance(TimeSpan.FromSeconds(60));
        var minuteLater = await AdvanceAsync(client);
        Assert.Contains(minuteLater.GetProperty("events").EnumerateArray(), battleEvent => battleEvent.GetProperty("type").GetString() is "encounter" or "rest");
    }

    [Fact]
    public async Task Advance_RecordsJobProficiency()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        factory.Clock.Advance(TimeSpan.FromSeconds(60));

        var body = await AdvanceAsync(client);

        var jobExp = body.GetProperty("summary").GetProperty("jobExp").GetInt64();
        Assert.True(jobExp > 0);
        var me = await client.GetFromJsonAsync<JsonElement>("/api/characters/me", CancellationToken);
        var proficiency = me.GetProperty("jobProficiency").EnumerateArray().Single();
        Assert.Equal("warrior", proficiency.GetProperty("jobId").GetString());
        Assert.Equal(jobExp, proficiency.GetProperty("proficiency").GetInt64());
        Assert.Equal(jobExp, proficiency.GetProperty("skillPoints").GetInt64());
    }

    // 接取 test-hunt（擊敗測試雪精 ×3）後推進 20 秒：進度事件以 "mission" 為 discriminator 序列化，摘要帶 missionKills
    [Fact]
    public async Task Advance_SerializesMissionProgress()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        (await client.PostAsJsonAsync("/api/missions/accept", new { missionId = "test-hunt" }, CancellationToken)).EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromSeconds(20));

        var body = await AdvanceAsync(client);

        var progress = EventsOfType(body, "mission");
        Assert.NotEmpty(progress);
        Assert.All(progress, mission => Assert.Equal(
            ("test-hunt", "測試討伐", "test-sprite", 3),
            (mission.GetProperty("missionId").GetString(), mission.GetProperty("title").GetString(),
                mission.GetProperty("monsterId").GetString(), mission.GetProperty("required").GetInt32())));
        Assert.Equal(Enumerable.Range(1, progress.Count), progress.Select(mission => mission.GetProperty("progress").GetInt32()));
        Assert.True(body.GetProperty("eventsCoverAll").GetBoolean());
        Assert.Equal(progress.Count, body.GetProperty("summary").GetProperty("missionKills").GetInt32());
    }

    [Fact]
    public async Task Advance_NpcEncounter_SavesProgress()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        (await client.PostAsJsonAsync("/api/missions/accept", new { missionId = "test-npc-visit" }, CancellationToken)).EnsureSuccessStatusCode();
        factory.Clock.Advance(TimeSpan.FromMinutes(10));

        var body = await AdvanceAsync(client);

        Assert.Equal(1, body.GetProperty("summary").GetProperty("missionKills").GetInt32());
        var board = await client.GetFromJsonAsync<JsonElement>("/api/missions", CancellationToken);
        var mission = board.GetProperty("active").EnumerateArray().Single(entry => entry.GetProperty("id").GetString() == "test-npc-visit");
        Assert.Equal(1, mission.GetProperty("kills").EnumerateArray().Single().GetProperty("progress").GetInt32());
        Assert.True(mission.GetProperty("ready").GetBoolean());
    }

    // 技能不足時擊殺滿了也不能回報，戰報要知道；Lv2 剛好達標
    [Theory]
    [InlineData(0, true)]
    [InlineData(2, false)]
    public async Task Advance_MissionEvent_ReportsSkillTooLow(int learned, bool tooLow)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        if (learned > 0)
        {
            var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
            using var scope = factory.Services.CreateScope();
            var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
                .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
            await scope.ServiceProvider.GetRequiredService<ISkillBookRepository>()
                .UpsertAsync(character!.Id, new LearnedSkill("burst", learned), CancellationToken);
        }

        (await client.PostAsJsonAsync("/api/missions/accept", new { missionId = "test-skill-gate" }, CancellationToken)).EnsureSuccessStatusCode();
        JsonElement? progress = null;
        for (var attempt = 0; attempt < 60 && progress is null; attempt++)
        {
            factory.Clock.Advance(TimeSpan.FromSeconds(10));
            var found = EventsOfType(await AdvanceAsync(client), "mission");
            progress = found.Count > 0 ? found[0] : null;
        }

        Assert.NotNull(progress);
        Assert.True(progress.Value.GetProperty("completed").GetBoolean());
        Assert.Equal(tooLow, progress.Value.GetProperty("skillTooLow").GetBoolean());
    }

    // 剛接取就遇到 NPC 的那批事件：npc 以 "npc" 為 discriminator 序列化，帶名稱與訊息
    [Fact]
    public async Task Advance_SerializesNpcEncounter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        (await client.PostAsJsonAsync("/api/missions/accept", new { missionId = "test-npc-visit" }, CancellationToken)).EnsureSuccessStatusCode();
        JsonElement? npc = null;
        for (var attempt = 0; attempt < 60 && npc is null; attempt++)
        {
            factory.Clock.Advance(TimeSpan.FromSeconds(10));
            var found = EventsOfType(await AdvanceAsync(client), "npc");
            npc = found.Count > 0 ? found[0] : null;
        }

        Assert.NotNull(npc);
        Assert.Equal(
            ("test-herald", "測試信使", "信送到了。"),
            (npc.Value.GetProperty("monsterId").GetString(), npc.Value.GetProperty("name").GetString(), npc.Value.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Advance_Returns404_WhenNoCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/adventure/advance", null, CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocument_DescribesPolymorphicEvents()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var document = await factory.CreateClient().GetStringAsync("/openapi/v1.json", CancellationToken);

        Assert.Contains("/api/adventure/advance", document);
        Assert.Contains("\"encounter\"", document);
        Assert.Contains("\"attack\"", document);
    }
}
