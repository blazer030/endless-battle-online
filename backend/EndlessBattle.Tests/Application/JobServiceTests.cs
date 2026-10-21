using EndlessBattle.Application.Common;
using EndlessBattle.Application.Jobs;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

public class JobServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository characters;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly InMemorySkillLoadoutRepository loadout;
    private readonly JobService service;

    public JobServiceTests()
    {
        characters = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        loadout = new InMemorySkillLoadoutRepository(log);
        service = new JobService(characters, proficiencies, loadout, Content);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private async Task<Character> ArrangeAsync(params SkillSlot[] slots)
    {
        var character = await characters.AddAsync(TestCharacters.Default(AccountId, "測試者", "test-stage-1", Now), CancellationToken);
        await loadout.ReplaceAsync(character.Id, slots, CancellationToken);
        log.Clear();
        return character;
    }

    [Fact]
    public async Task JobChange_ClearsPassiveSlotsKeepsActive()
    {
        var character = await ArrangeAsync(new SkillSlot(1, "burst", 0), new SkillSlot(4, "iron-skin", 40));

        var profile = await service.ChangeAsync(AccountId, "thief", CancellationToken);

        Assert.Equal("thief", profile.Character.JobId);
        Assert.Equal(["update", "loadout"], log);
        Assert.Equal([new SkillSlot(1, "burst", 0)], await loadout.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task Change_DoesNotTouchSlots_WhenIneligible()
    {
        await ArrangeAsync(new SkillSlot(4, "iron-skin", 40));

        var exception = await Assert.ThrowsAsync<InputValidationException>(() => service.ChangeAsync(AccountId, "berserker", CancellationToken));

        Assert.Contains("levelTooLow", exception.Reasons);
        Assert.Empty(log);
    }

    [Fact]
    public async Task Change_RejectsUnknownJob()
    {
        await ArrangeAsync();

        await Assert.ThrowsAsync<InputValidationException>(() => service.ChangeAsync(AccountId, "nope", CancellationToken));
    }

    [Fact]
    public async Task Change_ConflictLeavesSlots()
    {
        var character = await ArrangeAsync(new SkillSlot(4, "iron-skin", 40));
        characters.FailNextUpdate = true;

        await Assert.ThrowsAsync<ConflictException>(() => service.ChangeAsync(AccountId, "thief", CancellationToken));

        Assert.Empty(log);
        Assert.Single(await loadout.ListAsync(character.Id, CancellationToken));
    }

    [Fact]
    public async Task List_MarksCurrentAndPercent()
    {
        var character = await ArrangeAsync();
        await proficiencies.AddAsync(character.Id, "warrior", 1750, 0, CancellationToken);

        var options = await service.ListAsync(AccountId, CancellationToken);

        Assert.Equal(Content.AllJobs.Select(job => job.Id), options.Select(option => option.Id));
        var warrior = options.Single(option => option.Id == "warrior");
        Assert.True(warrior.Current);
        Assert.Equal(50, warrior.ProficiencyPercent);
        Assert.Equal(["sameJob"], warrior.Reasons);
        var thief = options.Single(option => option.Id == "thief");
        Assert.True(thief.Eligible);
        Assert.False(thief.Current);
        var berserker = options.Single(option => option.Id == "berserker");
        Assert.Equal("測試戰士", berserker.ParentName);
        Assert.False(berserker.Eligible);
    }
}
