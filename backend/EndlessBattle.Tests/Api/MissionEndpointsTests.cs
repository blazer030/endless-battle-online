using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Missions;
using EndlessBattle.Application.Pets;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api;

// API 主機的時鐘停在 2026-01-01T00:00Z（台北 08:00，週期 2025-12-31），下一次每日重置是 01:00Z
public class MissionEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        (await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"任務{Random.Shared.Next(100000, 999999)}"))).EnsureSuccessStatusCode();
        return client;
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string action, string missionId) =>
        client.PostAsJsonAsync($"/api/missions/{action}", new { missionId }, CancellationToken);

    // 直接寫入擊殺進度：計數要靠冒險推進，這裡跳過
    private static async Task SetProgressAsync(EndlessBattleApiFactory factory, HttpClient client, string missionId, params int[] progress)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        await scope.ServiceProvider.GetRequiredService<IMissionRepository>()
            .UpdateProgressAsync(character!.Id, missionId, progress, CancellationToken);
    }

    // 直接寫入寵物：取得寵物要靠捕捉，這裡跳過
    private static async Task GivePetAsync(EndlessBattleApiFactory factory, HttpClient client, string monsterId, bool carried)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        await scope.ServiceProvider.GetRequiredService<IPetRepository>()
            .AddAsync(character!.Id, TestPets.Create(monsterId: monsterId, carried: carried), CancellationToken);
    }

    private static JsonElement Mission(JsonElement board, string list, string missionId) =>
        board.GetProperty(list).EnumerateArray().Single(mission => mission.GetProperty("id").GetString() == missionId);

    private static List<string?> Ids(JsonElement board, string list) =>
        board.GetProperty(list).EnumerateArray().Select(mission => mission.GetProperty("id").GetString()).ToList();

    [Fact]
    public async Task Missions_GetBoard()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);

        var board = await ReadAsync(await client.GetAsync("/api/missions", CancellationToken));

        Assert.Contains("test-hunt", Ids(board, "available"));
        Assert.Contains("test-daily", Ids(board, "available"));
        Assert.Empty(Ids(board, "active"));
        Assert.Equal(3, board.GetProperty("activeLimit").GetInt32());
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 1, 0, 0, TimeSpan.Zero), board.GetProperty("nextDailyReset").GetDateTimeOffset());
        var hunt = board.GetProperty("available").EnumerateArray().Single(mission => mission.GetProperty("id").GetString() == "test-hunt");
        Assert.Equal("測試平原", hunt.GetProperty("placeName").GetString());
        var kill = Assert.Single(hunt.GetProperty("kills").EnumerateArray());
        Assert.Equal(("測試雪精", "測試平原", 3, 0), (
            kill.GetProperty("monsterName").GetString(), kill.GetProperty("placeName").GetString(),
            kill.GetProperty("count").GetInt32(), kill.GetProperty("progress").GetInt32()));
        Assert.Equal(190, hunt.GetProperty("rewards").GetProperty("exp").GetInt64());
    }

    [Fact]
    public async Task Missions_AcceptThenAbandon()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);

        var accepted = await ReadAsync(await PostAsync(client, "accept", "test-delivery"));

        Assert.Equal(["test-delivery"], Ids(accepted, "active"));
        Assert.DoesNotContain("test-delivery", Ids(accepted, "available"));
        var active = accepted.GetProperty("active")[0];
        Assert.False(active.GetProperty("ready").GetBoolean());
        var deliver = Assert.Single(active.GetProperty("deliver").EnumerateArray());
        Assert.Equal(("test-ore", 3, 0), (deliver.GetProperty("itemId").GetString(), deliver.GetProperty("quantity").GetInt32(), deliver.GetProperty("owned").GetInt32()));

        var abandoned = await ReadAsync(await PostAsync(client, "abandon", "test-delivery"));

        Assert.Empty(Ids(abandoned, "active"));
        Assert.Contains("test-delivery", Ids(abandoned, "available"));
    }

    [Fact]
    public async Task Missions_FourthAcceptReturnsTooMany()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        foreach (var missionId in new[] { "test-hunt", "test-sprite-bounty", "test-delivery" })
        {
            (await PostAsync(client, "accept", missionId)).EnsureSuccessStatusCode();
        }

        var response = await PostAsync(client, "accept", "test-chief-hunt");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["tooMany"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    private static async Task LearnAsync(EndlessBattleApiFactory factory, HttpClient client, string skillId, int level)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        await scope.ServiceProvider.GetRequiredService<ISkillBookRepository>()
            .UpsertAsync(character!.Id, new LearnedSkill(skillId, level), CancellationToken);
    }

    [Fact]
    public async Task Accept_WithoutRequiredSkill_Succeeds()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);

        var board = await ReadAsync(await PostAsync(client, "accept", "test-skill-gate"));

        var requiredSkill = Mission(board, "active", "test-skill-gate").GetProperty("requiredSkill");
        Assert.Equal(("burst", 2, false), (
            requiredSkill.GetProperty("skillId").GetString(), requiredSkill.GetProperty("level").GetInt32(), requiredSkill.GetProperty("met").GetBoolean()));
        Assert.False(string.IsNullOrEmpty(requiredSkill.GetProperty("name").GetString()));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task Complete_SkillBelowRequired_ReturnsSkillTooLow(int learned, bool completes)
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        if (learned > 0)
        {
            await LearnAsync(factory, client, "burst", learned);
        }

        (await PostAsync(client, "accept", "test-skill-gate")).EnsureSuccessStatusCode();
        await SetProgressAsync(factory, client, "test-skill-gate", 1);

        var response = await PostAsync(client, "complete", "test-skill-gate");

        var board = await client.GetFromJsonAsync<JsonElement>("/api/missions", CancellationToken);
        if (completes)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(["test-skill-gate"], Ids(board, "completed"));
            return;
        }

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("skillTooLow", await response.Content.ReadAsStringAsync(CancellationToken));
        Assert.Equal(["test-skill-gate"], Ids(board, "active"));
    }

    [Fact]
    public async Task Complete_RegularPetMission_RemovesCarriedPet()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        await GivePetAsync(factory, client, "test-wolf", carried: true);
        (await PostAsync(client, "accept", "test-pet-delivery")).EnsureSuccessStatusCode();

        var completion = await ReadAsync(await PostAsync(client, "complete", "test-pet-delivery"));

        Assert.Equal(["test-pet-delivery"], Ids(completion.GetProperty("board"), "completed"));
        var pets = await client.GetFromJsonAsync<JsonElement>("/api/pets", CancellationToken);
        Assert.Empty(pets.GetProperty("pets").EnumerateArray());
    }

    [Fact]
    public async Task Complete_RegularPetMission_StabledPetFails()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        await GivePetAsync(factory, client, "test-wolf", carried: false);
        (await PostAsync(client, "accept", "test-pet-delivery")).EnsureSuccessStatusCode();

        var response = await PostAsync(client, "complete", "test-pet-delivery");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("petMissing", await response.Content.ReadAsStringAsync(CancellationToken));
        var pets = await client.GetFromJsonAsync<JsonElement>("/api/pets", CancellationToken);
        Assert.Single(pets.GetProperty("pets").EnumerateArray());
        var board = await client.GetFromJsonAsync<JsonElement>("/api/missions", CancellationToken);
        Assert.Equal(["test-pet-delivery"], Ids(board, "active"));
    }

    [Fact]
    public async Task Complete_MultiKillMission_RequiresEveryTarget()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        (await PostAsync(client, "accept", "test-double-hunt")).EnsureSuccessStatusCode();
        await SetProgressAsync(factory, client, "test-double-hunt", 2, 0);

        var unfinished = await PostAsync(client, "complete", "test-double-hunt");
        Assert.Equal(HttpStatusCode.BadRequest, unfinished.StatusCode);
        Assert.Contains("objectiveIncomplete", await unfinished.Content.ReadAsStringAsync(CancellationToken));
        var board = await client.GetFromJsonAsync<JsonElement>("/api/missions", CancellationToken);
        var active = board.GetProperty("active").EnumerateArray().Single(mission => mission.GetProperty("id").GetString() == "test-double-hunt");
        Assert.Equal(
            [("test-sprite", 2, 2), ("test-wolf", 1, 0)],
            active.GetProperty("kills").EnumerateArray().Select(kill => (
                kill.GetProperty("monsterId").GetString(), kill.GetProperty("count").GetInt32(), kill.GetProperty("progress").GetInt32())));

        await SetProgressAsync(factory, client, "test-double-hunt", 2, 1);
        var completion = await ReadAsync(await PostAsync(client, "complete", "test-double-hunt"));

        Assert.Equal(["test-double-hunt"], Ids(completion.GetProperty("board"), "completed"));
    }

    [Fact]
    public async Task Missions_CompleteReturnsRewardsAndCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        var before = await ReadAsync(await client.GetAsync("/api/characters/me", CancellationToken));
        (await PostAsync(client, "accept", "test-hunt")).EnsureSuccessStatusCode();
        await SetProgressAsync(factory, client, "test-hunt", 3);

        var completion = await ReadAsync(await PostAsync(client, "complete", "test-hunt"));

        var character = completion.GetProperty("character");
        Assert.Equal(before.GetProperty("money").GetInt64() + 50, character.GetProperty("money").GetInt64());
        Assert.Equal(190, character.GetProperty("exp").GetInt64());
        Assert.Equal("謝謝你。", completion.GetProperty("completionMessage").GetString());
        Assert.Equal((190L, 50L, 0), (
            completion.GetProperty("exp").GetInt64(), completion.GetProperty("money").GetInt64(), completion.GetProperty("levelsGained").GetInt32()));
        Assert.Empty(completion.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, completion.GetProperty("skill").ValueKind);
        Assert.False(completion.GetProperty("skillGranted").GetBoolean());
        Assert.Equal(["test-hunt"], Ids(completion.GetProperty("board"), "completed"));
    }

    [Fact]
    public async Task Missions_CompleteIncompleteReturnsReason()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        (await PostAsync(client, "accept", "test-hunt")).EnsureSuccessStatusCode();

        var response = await PostAsync(client, "complete", "test-hunt");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["objectiveIncomplete"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Missions_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);

        var response = await factory.CreateClient().GetAsync("/api/missions", CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Missions_PetDailyShowsSpecies()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);

        var board = await ReadAsync(await client.GetAsync("/api/missions", CancellationToken));

        var available = Mission(board, "available", "test-daily-pet");
        Assert.Empty(available.GetProperty("kills").EnumerateArray());
        var deliverPet = available.GetProperty("deliverPet");
        var species = deliverPet.GetProperty("monsterId").GetString()!;
        var names = new Dictionary<string, string> { ["test-sprite"] = "測試雪精", ["test-wolf"] = "測試寒狼" };
        Assert.Equal(names[species], deliverPet.GetProperty("name").GetString());
        Assert.False(deliverPet.GetProperty("carried").GetBoolean());
        Assert.Equal(JsonValueKind.Null, Mission(board, "available", "test-hunt").GetProperty("deliverPet").ValueKind);

        await GivePetAsync(factory, client, species, carried: true);
        var accepted = await ReadAsync(await PostAsync(client, "accept", "test-daily-pet"));

        var active = Mission(accepted, "active", "test-daily-pet");
        Assert.True(active.GetProperty("deliverPet").GetProperty("carried").GetBoolean());
        Assert.True(active.GetProperty("ready").GetBoolean());
    }

    [Fact]
    public async Task Missions_CompleteReturnsPetReward()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateCharacterAsync(factory);
        var board = await ReadAsync(await client.GetAsync("/api/missions", CancellationToken));
        var rewardPet = Mission(board, "available", "test-pet-reward").GetProperty("rewards").GetProperty("pet");
        Assert.Equal(("test-wolf", "測試寒狼", 2), (
            rewardPet.GetProperty("monsterId").GetString(), rewardPet.GetProperty("name").GetString(), rewardPet.GetProperty("level").GetInt32()));
        (await PostAsync(client, "accept", "test-pet-reward")).EnsureSuccessStatusCode();
        await SetProgressAsync(factory, client, "test-pet-reward", 1);

        var completion = await ReadAsync(await PostAsync(client, "complete", "test-pet-reward"));

        Assert.Equal("測試寒狼", completion.GetProperty("pet").GetProperty("name").GetString());
        var pets = await ReadAsync(await client.GetAsync("/api/pets", CancellationToken));
        var pet = Assert.Single(pets.GetProperty("pets").EnumerateArray());
        Assert.Equal(("test-wolf", false), (pet.GetProperty("monsterId").GetString(), pet.GetProperty("carried").GetBoolean()));
    }
}
