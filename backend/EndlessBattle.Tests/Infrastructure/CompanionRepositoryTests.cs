using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Champion;
using EndlessBattle.Domain.Mercenaries;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class CompanionRepositoryTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static readonly DateTimeOffset Moment = new DateTimeOffset(2026, 10, 22, 1, 2, 3, TimeSpan.Zero).AddTicks(1230);

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"同伴{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    private static Pet NewPet(string name = "小狼") =>
        new(0, "wolf", name, PetPersonality.Ironwall, 11, 12, 13, 14, 15, 16, true, Moment, Moment.AddTicks(4560));

    private async Task<long> AddPetAsync(long characterId, Pet pet)
    {
        await using var context = database.CreateDbContext();
        return await new PetRepository(context).AddAsync(characterId, pet, CancellationToken);
    }

    private async Task<IReadOnlyList<Pet>> ListPetsAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new PetRepository(context).ListAsync(characterId, CancellationToken);
    }

    [Fact]
    public async Task Pets_RoundTrip()
    {
        var characterId = await CreateCharacterAsync();
        var first = await AddPetAsync(characterId, NewPet("甲"));
        var second = await AddPetAsync(characterId, NewPet("乙") with { Carried = false, Personality = PetPersonality.Desperate });

        var pets = await ListPetsAsync(characterId);

        Assert.Equal([first, second], pets.Select(pet => pet.Id));
        Assert.Equal(NewPet("甲") with { Id = first }, pets[0]);
        Assert.Equal(TimeSpan.Zero, pets[0].HungerSince.Offset);
        Assert.Equal((PetPersonality.Desperate, false), (pets[1].Personality, pets[1].Carried));
    }

    [Fact]
    public async Task Pets_UpdateAndRemoveScopedToCharacter()
    {
        var characterId = await CreateCharacterAsync();
        var otherId = await CreateCharacterAsync();
        var petId = await AddPetAsync(characterId, NewPet());
        var otherPetId = await AddPetAsync(otherId, NewPet("別人"));

        await using (var context = database.CreateDbContext())
        {
            var repository = new PetRepository(context);
            await repository.UpdateAsync(characterId, NewPet("改名") with { Id = petId, Intimacy = 99 }, CancellationToken);
            await repository.UpdateAsync(characterId, NewPet("越權") with { Id = otherPetId }, CancellationToken);
            await repository.RemoveAsync(characterId, otherPetId, CancellationToken);
            await repository.RemoveAsync(characterId, -1, CancellationToken);
        }

        var changed = Assert.Single(await ListPetsAsync(characterId));
        Assert.Equal(("改名", 99), (changed.Name, changed.Intimacy));
        Assert.Equal("別人", Assert.Single(await ListPetsAsync(otherId)).Name);

        await using (var context = database.CreateDbContext())
        {
            await new PetRepository(context).RemoveAsync(characterId, petId, CancellationToken);
        }

        Assert.Empty(await ListPetsAsync(characterId));
    }

    [Fact]
    public async Task Mercenary_RoundTripAndRejectSecond()
    {
        var characterId = await CreateCharacterAsync();
        var resting = new MercenaryContract("knight", "騎士", 10, null, null, Moment);
        await using (var context = database.CreateDbContext())
        {
            await new MercenaryRepository(context).AddAsync(characterId, resting, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            var repository = new MercenaryRepository(context);
            var stored = await repository.FindAsync(characterId, CancellationToken);
            Assert.Equal(resting, stored);
            Assert.False(stored!.Fighting);
            await Assert.ThrowsAsync<ConflictException>(() => repository.AddAsync(characterId, resting, CancellationToken));
        }

        var fighting = resting with { Remaining = 9, PlaceId = 3, NextFightAt = Moment.AddMinutes(1).AddTicks(70) };
        await using (var context = database.CreateDbContext())
        {
            await new MercenaryRepository(context).UpdateAsync(characterId, fighting, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            var repository = new MercenaryRepository(context);
            var stored = await repository.FindAsync(characterId, CancellationToken);
            Assert.Equal(fighting, stored);
            Assert.True(stored!.Fighting);
            Assert.Equal(TimeSpan.Zero, stored.NextFightAt!.Value.Offset);
            await repository.RemoveAsync(characterId, CancellationToken);
            await repository.RemoveAsync(characterId, CancellationToken);
            Assert.Null(await repository.FindAsync(characterId, CancellationToken));
        }
    }

    [Fact]
    public async Task Champion_SaveUpserts()
    {
        var characterId = await CreateCharacterAsync();
        var seat = new ChampionSeat("冠軍", 50, 2, "斬", 100, 200, 10, 20, 1, 2, 3, 4, 5, 6, true, 0, Moment, Moment.AddDays(1));
        var next = seat with { Hp = 7, Defenses = 3, CrownedAt = null, LockedUntil = null };

        await using (var context = database.CreateDbContext())
        {
            var repository = new ChampionRepository(context);
            Assert.Null(await repository.FindAsync(characterId, CancellationToken));
            await repository.SaveAsync(characterId, seat, CancellationToken);
        }

        await using (var context = database.CreateDbContext())
        {
            Assert.Equal(seat, await new ChampionRepository(context).FindAsync(characterId, CancellationToken));
            await new ChampionRepository(context).SaveAsync(characterId, next, CancellationToken);
        }

        await using var reader = database.CreateDbContext();
        Assert.Equal(next, await new ChampionRepository(reader).FindAsync(characterId, CancellationToken));
        Assert.Equal(1, await reader.CharacterChampions.CountAsync(row => row.CharacterId == characterId, CancellationToken));
    }

    // A 開交易寫入第一筆冠軍列但不提交；B 也看不到這一列，於是插入並等 A 的鎖；A 提交後 B 撞主鍵 → 409
    [Fact]
    public async Task ChampionRepository_FirstSaveRaceReturnsConflict()
    {
        var characterId = await CreateCharacterAsync();
        var name = $"競{Random.Shared.Next(100000, 999999)}";
        var seat = new ChampionSeat(name, 50, 2, "斬", 100, 200, 10, 20, 1, 2, 3, 4, 5, 6, true, 0, Moment, null);

        await using var first = database.CreateDbContext();
        await using var transaction = await first.Database.BeginTransactionAsync(CancellationToken);
        await new ChampionRepository(first).SaveAsync(characterId, seat, CancellationToken);

        var second = Task.Run(async () =>
        {
            await using var context = database.CreateDbContext();
            await new ChampionRepository(context).SaveAsync(characterId, seat with { Hp = 7 }, CancellationToken);
        }, CancellationToken);
        await WaitForBlockedInsertAsync(name);
        await transaction.CommitAsync(CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() => second);
        await using var reader = database.CreateDbContext();
        Assert.Equal(seat, await new ChampionRepository(reader).FindAsync(characterId, CancellationToken));
    }

    // 同一個資料庫使用者看得到自己的連線：等到帶有這個冠軍名稱的 INSERT 正在執行（被鎖住）
    private async Task WaitForBlockedInsertAsync(string name)
    {
        await using var context = database.CreateDbContext();
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            var waiting = await context.Database.SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value FROM information_schema.PROCESSLIST WHERE INFO LIKE 'INSERT%' AND INFO LIKE {0}", $"%{name}%")
                .SingleAsync(CancellationToken);
            if (waiting > 0)
            {
                return;
            }

            Assert.True(DateTime.UtcNow < deadline, "第二個寫入沒有在 20 秒內等鎖");
            await Task.Delay(50, CancellationToken);
        }
    }
}
