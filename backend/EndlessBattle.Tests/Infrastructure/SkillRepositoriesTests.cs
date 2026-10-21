using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Persistence;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Infrastructure;

public class SkillRepositoriesTests(MySqlDatabaseFixture database)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<long> CreateCharacterAsync()
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateDbContext();
        var accountId = await new AccountRepository(context).AddAsync(ApiClientExtensions.NewUsername(), "hash", now, CancellationToken);
        var character = await new CharacterRepository(context).AddAsync(
            TestCharacters.Default(accountId, $"技能{Random.Shared.Next(100000, 999999)}", 1, 1, now), CancellationToken);
        return character.Id;
    }

    private async Task ReplaceSlotsAsync(long characterId, params SkillSlot[] slots)
    {
        await using var context = database.CreateDbContext();
        await new SkillLoadoutRepository(context).ReplaceAsync(characterId, slots, CancellationToken);
    }

    private async Task<IReadOnlyList<SkillSlot>> ListSlotsAsync(long characterId)
    {
        await using var context = database.CreateDbContext();
        return await new SkillLoadoutRepository(context).ListAsync(characterId, CancellationToken);
    }

    [Fact]
    public async Task SkillBook_UpsertRaisesLevel()
    {
        var characterId = await CreateCharacterAsync();

        foreach (var level in new[] { 1, 2 })
        {
            await using var context = database.CreateDbContext();
            await new SkillBookRepository(context).UpsertAsync(characterId, new LearnedSkill("slash", level), CancellationToken);
        }

        await using var readContext = database.CreateDbContext();
        var stored = await new SkillBookRepository(readContext).ListAsync(characterId, CancellationToken);
        Assert.Equal([new LearnedSkill("slash", 2)], stored);
    }

    [Fact]
    public async Task SkillBook_ListsEmptyForNewCharacter()
    {
        var characterId = await CreateCharacterAsync();

        await using var context = database.CreateDbContext();
        var stored = await new SkillBookRepository(context).ListAsync(characterId, CancellationToken);

        Assert.Empty(stored);
    }

    [Fact]
    public async Task Loadout_ReplaceClearsMissingSlots()
    {
        var characterId = await CreateCharacterAsync();
        await ReplaceSlotsAsync(characterId, new SkillSlot(1, "slash", 30), new SkillSlot(2, "guard", 20), new SkillSlot(4, "heal", 10));

        await ReplaceSlotsAsync(characterId, new SkillSlot(1, "slash", 40), new SkillSlot(3, "bash", 25));

        Assert.Equal([new SkillSlot(1, "slash", 40), new SkillSlot(3, "bash", 25)], await ListSlotsAsync(characterId));
    }

    [Fact]
    public async Task Loadout_ReplaceWithEmptyClearsAll()
    {
        var characterId = await CreateCharacterAsync();
        await ReplaceSlotsAsync(characterId, new SkillSlot(1, "slash", 30), new SkillSlot(2, "guard", 20));

        await ReplaceSlotsAsync(characterId);

        Assert.Empty(await ListSlotsAsync(characterId));
    }

    // 重複主鍵讓 insert 失敗時，前面的 delete 要一起回復
    [Fact]
    public async Task Loadout_ReplaceRollsBack_WhenInsertFails()
    {
        var characterId = await CreateCharacterAsync();
        var original = new[] { new SkillSlot(1, "slash", 30), new SkillSlot(2, "guard", 20) };
        await ReplaceSlotsAsync(characterId, original);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            ReplaceSlotsAsync(characterId, new SkillSlot(3, "bash", 10), new SkillSlot(3, "heal", 15)));

        Assert.Equal(original, await ListSlotsAsync(characterId));
    }

    [Fact]
    public async Task Loadout_ListsOrderedBySlot()
    {
        var characterId = await CreateCharacterAsync();

        await ReplaceSlotsAsync(characterId, new SkillSlot(4, "heal", 10), new SkillSlot(1, "slash", 30));

        Assert.Equal([new SkillSlot(1, "slash", 30), new SkillSlot(4, "heal", 10)], await ListSlotsAsync(characterId));
    }

    [Fact]
    public async Task SkillBook_ListsOrderedBySkillId()
    {
        var characterId = await CreateCharacterAsync();

        foreach (var skillId in new[] { "zeal", "aim" })
        {
            await using var context = database.CreateDbContext();
            await new SkillBookRepository(context).UpsertAsync(characterId, new LearnedSkill(skillId, 1), CancellationToken);
        }

        await using var readContext = database.CreateDbContext();
        var stored = await new SkillBookRepository(readContext).ListAsync(characterId, CancellationToken);
        Assert.Equal([new LearnedSkill("aim", 1), new LearnedSkill("zeal", 1)], stored);
    }
}
