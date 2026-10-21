using EndlessBattle.Application.Common;
using EndlessBattle.Application.Skills;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// 預設角色是戰士、2000 錢；burst lv1 要 500 錢、300 技能點，iron-skin lv2 的 chance 是 40
public class SkillServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly InMemorySkillBookRepository skillBook;
    private readonly InMemorySkillLoadoutRepository loadout;
    private readonly SkillService service;

    public SkillServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        skillBook = new InMemorySkillBookRepository(log);
        loadout = new InMemorySkillLoadoutRepository(log);
        service = new SkillService(characters, proficiencies, skillBook, loadout, Content);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // 準備角色與技能點，並清掉準備過程的紀錄
    private async Task<Character> ArrangeAsync(long skillPoints, params LearnedSkill[] learned)
    {
        var character = await characters.AddAsync(TestCharacters.Default(AccountId, "測試者", "test-stage-1", Now), CancellationToken);
        if (skillPoints > 0)
        {
            await proficiencies.AddAsync(character.Id, "warrior", 0, skillPoints, CancellationToken);
        }

        foreach (var skill in learned)
        {
            await skillBook.UpsertAsync(character.Id, skill, CancellationToken);
        }

        log.Clear();
        return character;
    }

    [Fact]
    public async Task Learn_WritesInOrder_CharacterThenProficiencyThenBook()
    {
        var character = await ArrangeAsync(skillPoints: 500);

        var overview = await service.LearnAsync(AccountId, "burst", CancellationToken);

        Assert.Equal(["update", "spend:warrior:300", "book:burst:1"], log);
        Assert.Equal(1500, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
        Assert.Equal([new JobProficiency("warrior", 0, 200)], await proficiencies.ListAsync(character.Id, CancellationToken));
        Assert.Equal(200, overview.SkillPoints);
        var burst = Assert.Single(overview.Skills, skill => skill.Id == "burst");
        Assert.Equal(1, burst.Level);
        Assert.Equal(2, burst.Next!.Level);
    }

    [Fact]
    public async Task Learn_ConflictLeavesProficiencyAndBook()
    {
        var character = await ArrangeAsync(skillPoints: 300);
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.LearnAsync(AccountId, "burst", CancellationToken));

        Assert.Empty(log);
        Assert.Equal([new JobProficiency("warrior", 0, 300)], await proficiencies.ListAsync(character.Id, CancellationToken));
        Assert.Empty(await skillBook.ListAsync(character.Id, CancellationToken));
    }

    // 最小重現：檢查時夠 300，扣除前被別的請求花到 100 → 條件式扣除拒絕，技能書不寫
    [Fact]
    public async Task Learn_Conflicts_WhenSkillPointsSpentConcurrently()
    {
        var character = await ArrangeAsync(skillPoints: 300);
        proficiencies.BeforeNextSpend = () => proficiencies.AddAsync(character.Id, "warrior", 0, -200, CancellationToken);

        await Assert.ThrowsAsync<ConflictException>(() => service.LearnAsync(AccountId, "burst", CancellationToken));

        Assert.Equal([new JobProficiency("warrior", 0, 100)], await proficiencies.ListAsync(character.Id, CancellationToken));
        Assert.Empty(await skillBook.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Learn_DeductsNothing_WhenProficiencyInsufficient()
    {
        var character = await ArrangeAsync(skillPoints: 299);

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.LearnAsync(AccountId, "burst", CancellationToken));

        Assert.Equal(["notEnoughProficiency"], exception.Reasons);
        Assert.Empty(log);
        Assert.Equal(2000, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Money);
        Assert.Equal([new JobProficiency("warrior", 0, 299)], await proficiencies.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Learn_RejectsUnknownSkill()
    {
        await ArrangeAsync(skillPoints: 300);

        await Assert.ThrowsAsync<InputValidationException>(() => service.LearnAsync(AccountId, "no-such-skill", CancellationToken));

        Assert.Empty(log);
    }

    [Fact]
    public async Task Learn_Throws_WhenNoCharacter() =>
        await Assert.ThrowsAsync<NotFoundException>(() => service.LearnAsync(AccountId, "burst", CancellationToken));

    [Fact]
    public async Task Learn_Throws_WhenNoCharacter_EvenIfSkillUnknown() =>
        await Assert.ThrowsAsync<NotFoundException>(() => service.LearnAsync(AccountId, "no-such-skill", CancellationToken));

    // 已裝在欄 4 的 iron-skin 從 lv1 升到 lv2（600 技能點、2000 錢）：欄位存的 30 不重寫，回傳的 chance 依已學等級算成 40
    [Fact]
    public async Task Learn_RaisingAnEquippedPassive_UpdatesItsChance()
    {
        var character = await ArrangeAsync(600, new LearnedSkill("iron-skin", 1));
        await loadout.ReplaceAsync(character.Id, [new SkillSlot(4, "iron-skin", 30)], CancellationToken);

        var overview = await service.LearnAsync(AccountId, "iron-skin", CancellationToken);

        Assert.Equal([new SkillSlot(4, "iron-skin", 40)], overview.Slots);
        Assert.Equal([new SkillSlot(4, "iron-skin", 40)], (await service.GetAsync(AccountId, CancellationToken)).Slots);
    }

    [Fact]
    public async Task SetSlots_FillsPassiveChanceFromSkill()
    {
        var character = await ArrangeAsync(0, new LearnedSkill("burst", 1), new LearnedSkill("iron-skin", 2));

        var overview = await service.SetSlotsAsync(
            AccountId, [new SkillSlot(4, "iron-skin", 0), new SkillSlot(1, "burst", 70)], CancellationToken);

        IReadOnlyList<SkillSlot> expected = [new SkillSlot(1, "burst", 70), new SkillSlot(4, "iron-skin", 40)];
        Assert.Equal(expected, await loadout.ListAsync(character.Id, CancellationToken));
        Assert.Equal(expected, overview.Slots);
    }

    [Fact]
    public async Task SetSlots_RejectsInvalidLoadoutWithoutWriting()
    {
        var character = await ArrangeAsync(0, new LearnedSkill("burst", 1));
        await loadout.ReplaceAsync(character.Id, [new SkillSlot(1, "burst", 50)], CancellationToken);
        log.Clear();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() =>
            service.SetSlotsAsync(AccountId, [new SkillSlot(4, "burst", 50), new SkillSlot(2, "iron-skin", 50)], CancellationToken));

        Assert.Equal(["activeInPassiveSlot", "notLearned"], exception.Reasons);
        Assert.Empty(log);
        Assert.Equal([new SkillSlot(1, "burst", 50)], await loadout.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task SetSlots_ConflictLeavesSlots()
    {
        var character = await ArrangeAsync(0, new LearnedSkill("burst", 1));
        await loadout.ReplaceAsync(character.Id, [new SkillSlot(1, "burst", 50)], CancellationToken);
        log.Clear();
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.SetSlotsAsync(AccountId, [], CancellationToken));

        Assert.Empty(log);
        Assert.Equal([new SkillSlot(1, "burst", 50)], await loadout.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task SetSlots_BumpsCharacterVersion()
    {
        var character = await ArrangeAsync(0, new LearnedSkill("burst", 1));

        await service.SetSlotsAsync(AccountId, [new SkillSlot(1, "burst", 70)], CancellationToken);

        Assert.Equal(["update", "loadout"], log);
        Assert.Equal(character.Version + 1, (await characters.FindByAccountAsync(AccountId, CancellationToken))!.Version);
    }

    // 目前職業的技能全部列出（含未學）；其他職業的技能只列已學的
    [Fact]
    public async Task Get_ListsCurrentJobSkillsAndLearnedOthers()
    {
        await ArrangeAsync(300, new LearnedSkill("combo", 2));

        var overview = await service.GetAsync(AccountId, CancellationToken);

        Assert.Equal("warrior", overview.JobId);
        Assert.Equal(300, overview.SkillPoints);
        Assert.Equal(["burst", "iron-skin", "combo"], overview.Skills.Select(skill => skill.Id));
        var burst = overview.Skills[0];
        Assert.Equal(0, burst.Level);
        Assert.Equal(5, burst.MaxLevel);
        Assert.Equal(new NextLevelView(1, 5, 3, 0, 500, 300, true, []), burst.Next, NextLevelViewComparer.Instance);
        var combo = overview.Skills[2];
        Assert.Equal(2, combo.Level);
        Assert.Equal(["wrongJob", "notEnoughProficiency", "notEnoughMoney"], combo.Next!.Reasons);
        Assert.False(combo.Next.CanLearn);
    }

    [Fact]
    public async Task Get_ShowsNoNextLevel_AtMaxLevel()
    {
        await ArrangeAsync(0, new LearnedSkill("burst", 5));

        var overview = await service.GetAsync(AccountId, CancellationToken);

        var burst = Assert.Single(overview.Skills, skill => skill.Id == "burst");
        Assert.Equal(5, burst.Level);
        Assert.Null(burst.Next);
    }

    private sealed class NextLevelViewComparer : IEqualityComparer<NextLevelView?>
    {
        public static readonly NextLevelViewComparer Instance = new();

        public bool Equals(NextLevelView? x, NextLevelView? y) =>
            x is not null && y is not null && x with { Reasons = [] } == y with { Reasons = [] } && x.Reasons.SequenceEqual(y.Reasons);

        public int GetHashCode(NextLevelView? obj) => obj?.Level ?? 0;
    }
}
