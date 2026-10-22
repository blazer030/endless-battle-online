using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EndlessBattle.Application.Characters;
using EndlessBattle.Application.Pets;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace EndlessBattle.Tests.Api;

// 新戰士 2000 錢；TestContent 的 test-sprite 名為「測試雪精」
public class PetEndpointsTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static async Task<HttpClient> CreateClientWithCharacterAsync(EndlessBattleApiFactory factory)
    {
        var client = await factory.CreateAuthenticatedClientAsync();
        var created = await client.CreateCharacterAsync(ApiClientExtensions.ValidCharacterBody($"養寵{Random.Shared.Next(100000, 999999)}"));
        created.EnsureSuccessStatusCode();
        return client;
    }

    // 直接寫入寵物：取得寵物要靠捕捉，這裡跳過
    private static async Task<long> GivePetAsync(EndlessBattleApiFactory factory, HttpClient client, Pet pet)
    {
        var account = await client.GetFromJsonAsync<JsonElement>("/api/accounts/me", CancellationToken);
        using var scope = factory.Services.CreateScope();
        var character = await scope.ServiceProvider.GetRequiredService<ICharacterRepository>()
            .FindByAccountAsync(account.GetProperty("accountId").GetInt64(), CancellationToken);
        return await scope.ServiceProvider.GetRequiredService<IPetRepository>().AddAsync(character!.Id, pet, CancellationToken);
    }

    private static async Task<JsonElement> ReadOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
    }

    [Fact]
    public async Task Pets_GetListsAgeAndFoods()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var now = factory.Clock.UtcNow;
        var id = await GivePetAsync(factory, client, TestPets.Create(name: "小雪", monsterId: "test-sprite") with
        {
            BornAt = now.AddDays(-15),
            HungerSince = now.AddSeconds(-4),
        });

        var body = await ReadOkAsync(await client.GetAsync("/api/pets", CancellationToken));

        Assert.Equal(3, body.GetProperty("maxPets").GetInt32());
        var foods = body.GetProperty("foods").EnumerateArray().ToList();
        Assert.Equal(8, foods.Count);
        Assert.Equal(
            ("fruit-vigor-small", "初級活力果實", "Attack", 2, 20L),
            (foods[0].GetProperty("id").GetString(), foods[0].GetProperty("name").GetString(), foods[0].GetProperty("stat").GetString(),
                foods[0].GetProperty("amount").GetInt32(), foods[0].GetProperty("cost").GetInt64()));
        var pet = Assert.Single(body.GetProperty("pets").EnumerateArray());
        Assert.Equal(id, pet.GetProperty("id").GetInt64());
        Assert.Equal("test-sprite", pet.GetProperty("monsterId").GetString());
        Assert.Equal("測試雪精", pet.GetProperty("species").GetString());
        Assert.Equal("小雪", pet.GetProperty("name").GetString());
        Assert.Equal("Aggressive", pet.GetProperty("personality").GetString());
        Assert.True(pet.GetProperty("carried").GetBoolean());
        Assert.Equal(2, pet.GetProperty("age").GetInt32());
        Assert.Equal(now.AddSeconds(6), pet.GetProperty("nextFeedAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Pets_FeedReturnsCharacter()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var id = await GivePetAsync(factory, client, TestPets.Create() with { HungerSince = factory.Clock.UtcNow.AddMinutes(-1) });

        var body = await ReadOkAsync(await client.PostAsJsonAsync(
            "/api/pets/feed", new { petId = id, foodId = "fruit-wild-small" }, CancellationToken));

        Assert.Equal(1980, body.GetProperty("character").GetProperty("money").GetInt64());
        var pet = Assert.Single(body.GetProperty("pets").GetProperty("pets").EnumerateArray());
        Assert.Equal(3, pet.GetProperty("spirit").GetInt32());
        Assert.Equal(factory.Clock.UtcNow.AddSeconds(10), pet.GetProperty("nextFeedAt").GetDateTimeOffset());

        var again = await client.PostAsJsonAsync("/api/pets/feed", new { petId = id, foodId = "fruit-wild-small" }, CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        var problem = await again.Content.ReadFromJsonAsync<JsonElement>(CancellationToken);
        Assert.Equal(["tooSoon"], problem.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString()));
    }

    [Fact]
    public async Task Pets_CarryRenameStoreAndRelease()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = await CreateClientWithCharacterAsync(factory);
        var first = await GivePetAsync(factory, client, TestPets.Create(name: "甲", carried: true));
        var second = await GivePetAsync(factory, client, TestPets.Create(name: "乙", carried: false));

        var carried = await ReadOkAsync(await client.PostAsJsonAsync("/api/pets/carry", new { petId = second }, CancellationToken));
        Assert.Equal([false, true], carried.GetProperty("pets").EnumerateArray().Select(pet => pet.GetProperty("carried").GetBoolean()));

        var renamed = await ReadOkAsync(await client.PostAsJsonAsync("/api/pets/rename", new { petId = second, name = "阿乙" }, CancellationToken));
        Assert.Equal("阿乙", renamed.GetProperty("pets")[1].GetProperty("name").GetString());

        var stored = await ReadOkAsync(await client.PostAsJsonAsync("/api/pets/store", new { petId = second }, CancellationToken));
        Assert.All(stored.GetProperty("pets").EnumerateArray(), pet => Assert.False(pet.GetProperty("carried").GetBoolean()));

        var released = await ReadOkAsync(await client.PostAsJsonAsync("/api/pets/release", new { petId = first }, CancellationToken));
        Assert.Equal([second], released.GetProperty("pets").EnumerateArray().Select(pet => pet.GetProperty("id").GetInt64()));
    }

    [Fact]
    public async Task Pets_RequireLogin()
    {
        await using var factory = new EndlessBattleApiFactory(database);
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/pets", CancellationToken)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/pets/feed", new { petId = 1, foodId = "fruit-wild-small" }, CancellationToken)).StatusCode);
    }
}
