using EndlessBattle.Infrastructure.Persistence.Models;
using EndlessBattle.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EndlessBattle.Tests.Infrastructure;

public class DatabaseSchemaTests(MySqlDatabaseFixture database)
{
    [Fact]
    public async Task Migrations_CreateAccountsAndCharacters()
    {
        var characterId = await CreateCharacterAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, TestContext.Current.CancellationToken);
        Assert.Equal(0, stored.Exp);
        Assert.Equal(0, stored.Version);
    }

    [Fact]
    public async Task Migration_GivesExistingRowsDefaults()
    {
        var characterId = await CreateCharacterAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, TestContext.Current.CancellationToken);
        Assert.Equal(1, stored.Level);
        Assert.Equal(1000, stored.NextExp);
        Assert.Equal("warrior", stored.JobId);
        Assert.Equal(10, stored.BaseStr);
        Assert.Equal(50, stored.Hp);
        Assert.Equal(50, stored.ActNum);
        Assert.Null(stored.ActNumTime);
        Assert.Equal(0, stored.Money);
        Assert.Equal(0, stored.Exp);
    }

    // 同伴相關的時間欄位都是 DATETIME(6)；冠軍一個角色一列
    [Fact]
    public async Task CompanionTables_HaveExpectedColumnTypes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = database.CreateDbContext();

        var columns = await context.Database.SqlQueryRaw<string>(
            """
            SELECT CONCAT(TABLE_NAME, '.', COLUMN_NAME, ' ', COLUMN_TYPE) AS Value FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND (TABLE_NAME, COLUMN_NAME) IN (
                ('character_pets', 'born_at'), ('character_pets', 'hunger_since'),
                ('character_mercenaries', 'next_fight_at'), ('character_champions', 'locked_until'))
            ORDER BY Value
            """).ToListAsync(cancellationToken);
        var championKey = await context.Database.SqlQueryRaw<string>(
            """
            SELECT COLUMN_NAME AS Value FROM information_schema.KEY_COLUMN_USAGE
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'character_champions' AND CONSTRAINT_NAME = 'PRIMARY'
            """).ToListAsync(cancellationToken);

        Assert.Equal(
            [
                "character_champions.locked_until datetime(6)", "character_mercenaries.next_fight_at datetime(6)",
                "character_pets.born_at datetime(6)", "character_pets.hunger_since datetime(6)",
            ],
            columns);
        Assert.Equal(["character_id"], championKey);
    }

    [Fact]
    public async Task Accounts_RejectDuplicateUsername_CaseInsensitively()
    {
        var username = $"dup_{Guid.NewGuid():N}"[..20];
        await using var context = database.CreateDbContext();
        context.Accounts.Add(new Account { Username = username, PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using var second = database.CreateDbContext();
        second.Accounts.Add(new Account { Username = username.ToUpperInvariant(), PasswordHash = "hash", CreatedAt = DateTime.UtcNow });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Characters_RejectStaleVersion()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var contextA = database.CreateDbContext();
        await using var contextB = database.CreateDbContext();
        var characterA = await contextA.Characters.SingleAsync(character => character.Id == characterId, cancellationToken);
        var characterB = await contextB.Characters.SingleAsync(character => character.Id == characterId, cancellationToken);

        characterA.Money = 10;
        characterA.Version += 1;
        await contextA.SaveChangesAsync(cancellationToken);

        characterB.Money = 20;
        characterB.Version += 1;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextB.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task Schema_HasSkillTables()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var context = database.CreateDbContext();
        context.CharacterSkills.Add(new CharacterSkill { CharacterId = characterId, SkillId = "slash", Level = 1 });
        context.CharacterSkillSlots.Add(new CharacterSkillSlot { CharacterId = characterId, Slot = 1, SkillId = "slash" });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        Assert.Equal(1, (await reader.CharacterSkills.SingleAsync(row => row.CharacterId == characterId, cancellationToken)).Level);
        Assert.Equal("slash", (await reader.CharacterSkillSlots.SingleAsync(row => row.CharacterId == characterId, cancellationToken)).SkillId);
    }

    [Fact]
    public async Task Schema_HasInventoryTables()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var context = database.CreateDbContext();
        var inventory = new CharacterInventory { CharacterId = characterId, ItemId = "sword", Quantity = 1 };
        context.CharacterInventories.Add(inventory);
        context.CharacterDepots.Add(new CharacterDepot { CharacterId = characterId, ItemId = "potion", Quantity = 2 });
        await context.SaveChangesAsync(cancellationToken);
        context.CharacterEquipments.Add(new CharacterEquipment { CharacterId = characterId, Slot = "Weapon", InventoryId = inventory.Id });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        Assert.Equal("Weapon", (await reader.CharacterEquipments.SingleAsync(row => row.CharacterId == characterId, cancellationToken)).Slot);
        Assert.Equal(2, (await reader.CharacterDepots.SingleAsync(row => row.CharacterId == characterId, cancellationToken)).Quantity);
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, cancellationToken);
        Assert.Equal((0L, 0, 0), (stored.Bank, stored.BagBonus, stored.DepotBonus));
    }

    [Fact]
    public async Task Schema_HasMissionTable()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var context = database.CreateDbContext();
        context.CharacterMissions.Add(new CharacterMission
        {
            CharacterId = characterId, MissionId = "m1", State = "Active", DailyPeriod = new DateTime(2026, 10, 22), AcceptedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        var stored = await reader.CharacterMissions.SingleAsync(row => row.CharacterId == characterId, cancellationToken);
        Assert.Equal((0, new DateTime(2026, 10, 22), (DateTime?)null), (stored.Progress, stored.DailyPeriod, stored.CompletedAt));

        reader.CharacterMissions.Add(new CharacterMission { CharacterId = characterId, MissionId = "m1", State = "Active", AcceptedAt = DateTime.UtcNow });
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => reader.SaveChangesAsync(cancellationToken));
        Assert.Contains("ux_character_missions_character_mission", exception.InnerException?.Message);
    }

    [Fact]
    public async Task Schema_HasCompanionTables()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var at = new DateTime(2026, 10, 22, 1, 2, 3, 456, DateTimeKind.Utc).AddTicks(7890);

        await using var context = database.CreateDbContext();
        context.CharacterPets.Add(new CharacterPet
        {
            CharacterId = characterId, MonsterId = "wolf", Name = "小狼", Personality = 3, Attack = 1, Magic = 2, Defense = 3,
            Spirit = 4, Satiety = 5, Intimacy = 6, Carried = true, BornAt = at, HungerSince = at,
        });
        context.CharacterMercenaries.Add(new CharacterMercenary
        {
            CharacterId = characterId, MercenaryId = "knight", Name = "騎士", Remaining = 9, HiredAt = at,
        });
        context.CharacterChampions.Add(new CharacterChampion
        {
            CharacterId = characterId, Name = "冠軍", Level = 50, Element = 2, SpecialAttackName = "斬", Hp = 1, HpMax = 2, Sp = 3, SpMax = 4,
            At = 5, Mat = 6, Df = 7, Mdf = 8, Agi = 9, Au = 10, IsPlayer = true, Defenses = 0,
        });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        var pet = await reader.CharacterPets.SingleAsync(row => row.CharacterId == characterId, cancellationToken);
        Assert.Equal((3, true, at, at), ((int)pet.Personality, pet.Carried, pet.BornAt, pet.HungerSince));
        var mercenary = await reader.CharacterMercenaries.SingleAsync(row => row.CharacterId == characterId, cancellationToken);
        Assert.Equal((9, (int?)null, (DateTime?)null), (mercenary.Remaining, mercenary.PlaceId, mercenary.NextFightAt));
        var champion = await reader.CharacterChampions.SingleAsync(row => row.CharacterId == characterId, cancellationToken);
        Assert.Equal((true, (DateTime?)null, (DateTime?)null), (champion.IsPlayer, champion.CrownedAt, champion.LockedUntil));

        await using var second = database.CreateDbContext();
        second.CharacterMercenaries.Add(new CharacterMercenary { CharacterId = characterId, MercenaryId = "x", Name = "x", Remaining = 1, HiredAt = at });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(cancellationToken));
    }

    // 每個角色每個地區一列；DATETIME(6) 保留到微秒
    [Fact]
    public async Task Schema_HasAreaPassTable()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var at = new DateTime(2026, 10, 22, 1, 2, 3, 456, DateTimeKind.Utc).AddTicks(7890);

        await using var context = database.CreateDbContext();
        context.CharacterAreaPasses.Add(new CharacterAreaPass { CharacterId = characterId, PlaceId = 17, ExpiresAt = at });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        var pass = await reader.CharacterAreaPasses.SingleAsync(row => row.CharacterId == characterId, cancellationToken);
        Assert.Equal((17, at), (pass.PlaceId, pass.ExpiresAt));

        await using var second = database.CreateDbContext();
        second.CharacterAreaPasses.Add(new CharacterAreaPass { CharacterId = characterId, PlaceId = 17, ExpiresAt = at });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(cancellationToken));
    }

    // 每個角色每種密藥一列；倍率 DECIMAL(4,2)、時刻 DATETIME(6) 保留到微秒
    [Fact]
    public async Task Schema_HasCharacterBuffTable()
    {
        var characterId = await CreateCharacterAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var startsAt = new DateTime(2026, 10, 22, 1, 2, 3, 456, DateTimeKind.Utc).AddTicks(7890);

        await using var context = database.CreateDbContext();
        context.CharacterBuffs.Add(new CharacterBuff
        {
            CharacterId = characterId, Kind = "MagicDefense", Multiplier = 1.25m, StartsAt = startsAt, EndsAt = startsAt.AddHours(1),
        });
        await context.SaveChangesAsync(cancellationToken);

        await using var reader = database.CreateDbContext();
        var buff = await reader.CharacterBuffs.SingleAsync(row => row.CharacterId == characterId, cancellationToken);
        Assert.Equal(("MagicDefense", 1.25m, startsAt, startsAt.AddHours(1)), (buff.Kind, buff.Multiplier, buff.StartsAt, buff.EndsAt));

        await using var second = database.CreateDbContext();
        second.CharacterBuffs.Add(new CharacterBuff
        {
            CharacterId = characterId, Kind = "MagicDefense", Multiplier = 1.5m, StartsAt = startsAt, EndsAt = startsAt,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync(cancellationToken));
    }

    // 新欄位不指定時取資料庫預設：地區 1、子地區 NULL；舊的 current_stage_id 可為 NULL
    [Fact]
    public async Task Schema_HasPlaceColumns()
    {
        var characterId = await CreateCharacterAsync();

        await using var reader = database.CreateDbContext();
        var stored = await reader.Characters.SingleAsync(character => character.Id == characterId, TestContext.Current.CancellationToken);
        Assert.Equal(1, stored.CurrentPlaceId);
        Assert.Null(stored.CurrentSubPlace);
        Assert.Null(stored.CurrentStageId);
    }

    private async Task<long> CreateCharacterAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = database.CreateDbContext();
        var account = new Account { Username = $"schema_{Guid.NewGuid():N}"[..32], PasswordHash = "hash", CreatedAt = DateTime.UtcNow };
        context.Accounts.Add(account);
        await context.SaveChangesAsync(cancellationToken);

        var character = new Character
        {
            AccountId = account.Id,
            Name = $"角{account.Id}",
            LastSettledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
        context.Characters.Add(character);
        await context.SaveChangesAsync(cancellationToken);
        return character.Id;
    }
}
