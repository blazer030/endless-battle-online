using EndlessBattle.Application.Adventure;
using EndlessBattle.Application.Common;
using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Characters;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Missions;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Progression;
using EndlessBattle.Domain.Randomness;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.Stamina;
using EndlessBattle.Infrastructure.Content;
using EndlessBattle.Tests.Support;

namespace EndlessBattle.Tests.Application;

// 擲骰一律 0.0：遇敵擲骰 0 → 只有 Meet 0 的測試雪精（Lv3、60 血、at 14、mat 8、df 6、agi 9、水、jobExp 12）。
// 預設角色 At 12、agi 12、地屬性：agi 較高 → 1 ≤ 70 先攻；必中；比值 12/9 → 暴擊機率 13.3（1 < 13.3）、倍率 1.6 → A = 19.2；
// 地剋水 ×1.2：(19.2·3.2 − 6)·1.2 = 66.528，rand(1,1) = 1 → round(67.528) = 68 ≥ 60，第 1 回合擊倒，雪精不出手。
// 每場 0.8 秒 + 場間 2 秒 = 2.8 秒。獎勵（玩家 Lv1）：經驗 base 11 → rand(11, 14) = 11；×1.3 = 14.3；3 × 14.3 × 3 × 0.18 = 23.166；
// ×2.5 = 57.915；×2 = 115.83 → 116。金錢 rand(1.2, 3) = 1 × rand(22/3, 60/22×0.4) 對調為 rand(1, 7) = 1 → round(0.4) = 0。熟練度 12
public class AdventureServiceTests
{
    private const long AccountId = 7;
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly JsonGameContent Content = JsonGameContent.Load(Path.Combine(AppContext.BaseDirectory, "TestContent"));

    private readonly List<string> log = [];
    private readonly LoggingCharacterRepository repository;
    private readonly InMemoryJobProficiencyRepository proficiencies;
    private readonly InMemorySkillBookRepository skillBook = new();
    private readonly InMemorySkillLoadoutRepository loadout = new();
    private readonly InMemoryInventoryRepository inventory = new();
    private readonly InMemoryEquipmentRepository equipment;
    private readonly InMemoryMissionRepository missions;
    private readonly InMemoryPetRepository pets;
    private readonly FakeClock clock = new(Start);
    private readonly InMemoryUnitOfWork unitOfWork;
    private readonly AdventureService service;

    public AdventureServiceTests()
    {
        repository = new LoggingCharacterRepository(log);
        proficiencies = new InMemoryJobProficiencyRepository(log);
        equipment = new InMemoryEquipmentRepository(inventory);
        missions = new InMemoryMissionRepository(log);
        pets = new InMemoryPetRepository(log);
        unitOfWork = new InMemoryUnitOfWork(repository, proficiencies, inventory, missions, pets);
        service = new AdventureService(
            repository, proficiencies, skillBook, loadout, inventory, equipment, missions, pets, Content, clock, new ScriptedRandomFactory(), unitOfWork);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private Task<Character> AddCharacterAsync(Func<Character, Character>? customize = null)
    {
        var character = TestCharacters.Default(AccountId, "測試者", 1, 1, Start);
        return repository.AddAsync(customize is null ? character : customize(character), CancellationToken);
    }

    // 20 秒內在 0、2.8、…、19.6 開打 8 場，結算到 22.4 秒。100 + 8 × 116 = 1028 > 1000 → 第 8 場升 Lv2，剩 28
    [Fact]
    public async Task Advance_SettlesThroughLastStartedFight()
    {
        await AddCharacterAsync(character => character with { Exp = 100 });
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(8, result.Events.OfType<EncounterEvent>().Count());
        Assert.All(result.Events.OfType<EncounterEvent>(), encounter => Assert.Equal("test-sprite", encounter.MonsterId));
        Assert.Equal(new AdvanceSummary(Fights: 8, Wins: 8, Losses: 0, Timeouts: 0, Exp: 928, Money: 0, JobExp: 96, LevelsGained: 1, SkillsCast: 0, Drops: 8), result.Summary);
        Assert.True(result.EventsCoverAll);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(Start.AddSeconds(22.4), stored!.LastSettledAt, TimeSpan.FromMilliseconds(1));
        Assert.Equal(28, stored.Exp);
        Assert.Equal(result.Character, stored);
        Assert.Equal([new JobProficiency("warrior", 96, 96)], result.JobProficiency);
    }

    // 角色已寫入後掉落才寫入失敗：整筆推進回滾，角色、熟練度與背包都維持原狀，例外照常拋出
    [Fact]
    public async Task Advance_RollsBackCharacterWhenInventoryWriteFails()
    {
        var before = await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(20));
        inventory.FailWrite = "add";

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdvanceAsync(AccountId, CancellationToken));

        Assert.Equal(["begin", "rollback"], unitOfWork.Log);
        Assert.Equal(before, await repository.FindByAccountAsync(AccountId, CancellationToken));
        Assert.Empty(await proficiencies.ListAsync(before.Id, CancellationToken));
        Assert.Empty(await inventory.ListAsync(before.Id, CancellationToken));
    }

    // 讀取與模擬（純 CPU）都在開交易之前；交易裡只有寫入
    [Fact]
    public async Task Advance_SimulatesBeforeOpeningTheTransaction()
    {
        await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(20));
        var advance = new AdventureService(
            repository, proficiencies, skillBook, loadout, inventory, equipment, missions, pets, Content, clock, new LoggingRandomFactory(unitOfWork.Log), unitOfWork);

        await advance.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(["simulate", "begin", "commit"], unitOfWork.Log);
    }

    // HP 0 → 先住宿 15 秒回滿 350／91；15 秒那場擊倒雪精：999 + 116 > 1000 → Lv2，成長取下限 (2,0,1,1,1,0,0)。
    // 行動力 50 − 1 = 49，回復時間從第一次結算的 Start 起算
    [Fact]
    public async Task Advance_PersistsHpAndLevel()
    {
        await AddCharacterAsync(character => character with { Hp = 0, Sp = 0, Exp = 999 });
        clock.Advance(TimeSpan.FromSeconds(16));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.IsType<RestEvent>(result.Events[0]);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(350, stored!.Hp);
        Assert.Equal(91, stored.Sp);
        Assert.Equal(2, stored.Level);
        Assert.Equal(115, stored.Exp);
        Assert.Equal(2184, stored.NextExp);
        Assert.Equal(new BaseStats(14, 12, 13, 11, 13, 10, 10), stored.Base);
        Assert.Equal(49, stored.ActNum);
        Assert.Equal(Start, stored.ActNumTime);
        Assert.Equal(1, stored.Version);
    }

    // 陣亡的角色離線 40 秒：0 秒住宿（窗口 10 秒起，被略去）後 15 秒起連戰，戰鬥全部輸出 → EventsCoverAll 仍為 true
    [Fact]
    public async Task Advance_ReportsAllEvents_WhenOnlyTheOpeningRestIsSkipped()
    {
        await AddCharacterAsync(character => character with { Hp = 0 });
        clock.Advance(TimeSpan.FromSeconds(40));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Empty(result.Events.OfType<RestEvent>());
        Assert.Equal(result.Summary.Fights, result.Events.OfType<EncounterEvent>().Count());
        Assert.True(result.EventsCoverAll);
    }

    // 角色在地區 2 → 只遇到地區 2 的測試蝙蝠（meet 0），不會碰到地區 1 的怪
    [Fact]
    public async Task Advance_UsesCharacterPlace()
    {
        await AddCharacterAsync(character => character with { PlaceId = 2, SubPlace = 1 });
        clock.Advance(TimeSpan.FromSeconds(5));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.NotEmpty(result.Events.OfType<EncounterEvent>());
        Assert.All(result.Events.OfType<EncounterEvent>(), encounter => Assert.Equal("test-bat", encounter.MonsterId));
    }

    // 離線 30 小時：只模擬最後 24 小時，結果應與直接從 now − 24h 模擬到 now 相同
    [Fact]
    public async Task Advance_CapsAtTwentyFourHours()
    {
        var character = await AddCharacterAsync();
        clock.Advance(TimeSpan.FromHours(30));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        var from = clock.UtcNow.AddHours(-24);
        var expected = AdventureSimulator.Run(
            character, Content.GetJob("warrior"), Content.GetPlace(1), Content.MonstersOf(1), Content.GetItem, [],
            new StaminaState(character.ActNum, character.ActNumTime), from, clock.UtcNow, new ScriptedRandom(0.0));
        // 摘要的掉落是進背包的件數：8656 件補給品只放得下 15 格 × 99 = 1485
        Assert.Equal(expected.Summary, result.Summary with { Drops = 0 });
        Assert.Equal(1485, result.Summary.Drops);
        Assert.True(result.Summary.Fights > 1000);
        var stored = await repository.FindByAccountAsync(AccountId, CancellationToken);
        Assert.Equal(from.AddSeconds(expected.ConsumedSeconds), stored!.LastSettledAt);
        Assert.InRange(stored.LastSettledAt, clock.UtcNow, clock.UtcNow.AddSeconds(WogBattle.MaxRounds * BattleTiming.SecondsPerRound + BattleTiming.FightIntervalWhenDepletedSeconds));
        Assert.All(result.Events, battleEvent => Assert.True(battleEvent.AtSeconds >= AdventureSimulator.MaxSimulatedSeconds - AdventureSimulator.CatchUpWindowSeconds));
    }

    [Fact]
    public async Task Advance_WritesProficiencyAfterCharacterUpdate()
    {
        await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(5));

        await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(["update", "proficiency:warrior"], log);
    }

    // 版本衝突時角色沒有寫入，熟練度也不能寫
    [Fact]
    public async Task Advance_WritesNoProficiency_WhenCharacterUpdateConflicts()
    {
        await AddCharacterAsync();
        repository.FailNextUpdate = true;
        clock.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<ConflictException>(() => service.AdvanceAsync(AccountId, CancellationToken));

        Assert.DoesNotContain(log, entry => entry.StartsWith("proficiency", StringComparison.Ordinal));
        Assert.Empty(await proficiencies.ListAsync(1, CancellationToken));
    }

    [Fact]
    public async Task Advance_ReturnsNothingAndWritesNothing_WhenClockIsBehind()
    {
        var character = await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(-5));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Empty(result.Events);
        Assert.Equal(AdvanceSummary.Empty, result.Summary);
        Assert.Equal(character, result.Character);
        Assert.Empty(log);
    }

    private async Task EquipAsync(Character character, IReadOnlyList<LearnedSkill> learned, IReadOnlyList<SkillSlot> slots)
    {
        foreach (var skill in learned)
        {
            await skillBook.UpsertAsync(character.Id, skill, CancellationToken);
        }

        await loadout.ReplaceAsync(character.Id, slots, CancellationToken);
    }

    // 欄位依序 1→5 擲骰（全部擲 1），SP 91 滿；5 秒內開 2 場（0、2.8 秒；下一場 5.6 秒不開）：
    // 每場 burst lv2 耗 8、iron-skin lv1 耗 5：91 → 83 → 78 → 70 → 65。欄 2 的 combo 不在技能書裡 → 略過
    [Fact]
    public async Task Advance_BuildsLoadoutFromBookAndSlots()
    {
        var character = await AddCharacterAsync(character => character with { Sp = 91 });
        await EquipAsync(
            character,
            [new LearnedSkill("burst", 2), new LearnedSkill("iron-skin", 1)],
            [new SkillSlot(1, "burst", 100), new SkillSlot(2, "combo", 50), new SkillSlot(4, "iron-skin", 30)]);
        clock.Advance(TimeSpan.FromSeconds(5));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(
            [("burst", 2, 83), ("iron-skin", 1, 78), ("burst", 2, 70), ("iron-skin", 1, 65)],
            result.Events.OfType<SkillEvent>().Select(skill => (skill.SkillId, skill.Level, skill.SpAfter)));
        Assert.Equal(4, result.Summary.SkillsCast);
        Assert.Equal(65, result.Character.Sp);
    }

    // 轉職與推進競態時欄 4 可能還留著別職業的被動：不施放。主動欄照 WOG 不限職業，thief 仍可用 burst（6 秒開 3 場：25 → 20 → 15 → 10）
    [Fact]
    public async Task Advance_IgnoresPassiveSlotOfOtherJob()
    {
        var character = await AddCharacterAsync(character => character with { JobId = "thief" });
        await EquipAsync(
            character,
            [new LearnedSkill("burst", 1), new LearnedSkill("iron-skin", 1)],
            [new SkillSlot(1, "burst", 100), new SkillSlot(4, "iron-skin", 30)]);
        clock.Advance(TimeSpan.FromSeconds(6));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(Enumerable.Repeat("burst", 3), result.Events.OfType<SkillEvent>().Select(skill => skill.SkillId));
        Assert.Equal(3, result.Summary.SkillsCast);
    }

    // 裝上測試鐵劍（at 8）→ At 20：暴擊 A = 20×1.6 = 32；(32·32/6 − 6)·1.2 = 197.6，比值 6.175 → rand(2,3) = 2 → 200（沒裝備時 68）
    [Fact]
    public async Task Advance_UsesEquipmentStats()
    {
        var character = await AddCharacterAsync();
        var sword = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-sword"), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Weapon, sword, null, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(1));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        var first = result.Events.OfType<AttackEvent>().First(attack => attack.Attacker == CombatSide.Player);
        Assert.True(first.Critical);
        Assert.Equal(200, first.Damage);
        Assert.Equal(8, result.Equipment.Total.At);
    }

    [Fact]
    public async Task Advance_Throws_WhenNoCharacter()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => service.AdvanceAsync(AccountId, CancellationToken));
    }

    private async Task<IReadOnlyList<(string ItemId, int Quantity)>> BagAsync(long characterId) =>
        (await inventory.ListAsync(characterId, CancellationToken)).Select(entry => (entry.ItemId, entry.Quantity)).ToList();

    // 把消耗品分類填到只剩 1 格：14 格別的道具 ×99，第 15 格 test-supply ×supplyQuantity
    private async Task FillConsumablesAsync(Character character, int supplyQuantity)
    {
        for (var index = 0; index < 14; index++)
        {
            await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-str-potion", 99), CancellationToken);
        }

        await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-supply", supplyQuantity), CancellationToken);
    }

    // 8 場全勝，每場掉 test-supply（topr 1 → 必掉）→ 背包多 ×8；摘要計入背包的件數，事件列出掉落且沒有丟棄
    [Fact]
    public async Task Advance_PersistsDrops()
    {
        var character = await AddCharacterAsync();
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal([("test-supply", 8)], await BagAsync(character.Id));
        Assert.Equal(8, result.Summary.Drops);
        var rewards = result.Events.OfType<RewardEvent>().ToList();
        Assert.Equal(8, rewards.Count);
        Assert.All(rewards, reward => Assert.Equal([new DropResult("test-supply", "測試補給藥劑", 1, false)], reward.Drops));
    }

    // 消耗品第 15 格 ×97：前兩件補滿到 99，其餘 6 件沒有格子 → 丟棄。摘要只計 2，事件的前兩個掉落未丟棄、後面都標丟棄
    [Fact]
    public async Task Advance_DiscardsDropsWhenBagCategoryIsFull()
    {
        var character = await AddCharacterAsync();
        await FillConsumablesAsync(character, supplyQuantity: 97);
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(2, result.Summary.Drops);
        Assert.Equal(("test-supply", 99), (await BagAsync(character.Id)).Last());
        Assert.Equal(15, (await BagAsync(character.Id)).Count);
        var flags = result.Events.OfType<RewardEvent>().SelectMany(reward => reward.Drops).Select(drop => drop.Discarded);
        Assert.Equal([false, false, true, true, true, true, true, true], flags);
    }

    // 40 秒：前幾場在 30 秒窗口外，事件只留尾段。背包只剩 1 個空位的堆疊（98 → 99）→ 只有第一個掉落（窗口外）被收下，
    // 窗口內的掉落全部標丟棄：旗標必須從全部掉落的尾端對齊
    [Fact]
    public async Task Advance_AlignsDiscardFlagsWithTheEventWindow()
    {
        var character = await AddCharacterAsync();
        await FillConsumablesAsync(character, supplyQuantity: 98);
        clock.Advance(TimeSpan.FromSeconds(40));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        var eventDrops = result.Events.OfType<RewardEvent>().SelectMany(reward => reward.Drops).ToList();
        Assert.True(result.Summary.Wins > eventDrops.Count);
        Assert.Equal(1, result.Summary.Drops);
        Assert.NotEmpty(eventDrops);
        Assert.All(eventDrops, drop => Assert.True(drop.Discarded));
    }

    [Fact]
    public async Task Advance_WritesNoDrops_WhenCharacterUpdateConflicts()
    {
        var character = await AddCharacterAsync();
        repository.FailNextUpdate = true;
        clock.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<ConflictException>(() => service.AdvanceAsync(AccountId, CancellationToken));

        Assert.Empty(inventory.Writes);
        Assert.Empty(await BagAsync(character.Id));
    }

    // 力量、智力都 1 → At 1、Mat 1：每下只打雪精 1 點；雪精每下 15 點。HP 60／350、SP 25／58（智力 1 → (1+10)×3+25）。
    // 1 秒只開打一場：第 1 回合被打到 45 < 52.5 → 第 2 回合開頭喝（+50／+25 → 95／50）；第 4 回合後 50 → 第 5 回合再喝（+50／+8，SP 滿）。
    // 之後 SP 已滿，HP 再低也不喝，第 11 回合倒下。共喝 2 瓶
    private Task<Character> AddWeakCharacterAsync() =>
        AddCharacterAsync(character => character with { Base = character.Base with { Str = 1, Smart = 1 } });

    private async Task<long> EquipSupplyAsync(Character character, int quantity)
    {
        var id = await inventory.AddAsync(character.Id, TestItems.Entry(0, "test-supply", quantity), CancellationToken);
        await equipment.SetSlotAsync(character.Id, EquipmentSlot.Supply, id, quantity, CancellationToken);
        return id;
    }

    [Fact]
    public async Task Adventure_WritesBackRemainingQuantity()
    {
        var character = await AddWeakCharacterAsync();
        var supplyId = await EquipSupplyAsync(character, quantity: 20);
        clock.Advance(TimeSpan.FromSeconds(1));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(2, result.Summary.PotionsUsed);
        Assert.Equal([19, 18], result.Events.OfType<PotionEvent>().Select(potion => potion.Remaining));
        var loadout = await equipment.GetAsync(character.Id, CancellationToken);
        Assert.Equal(supplyId, loadout.Equipped[EquipmentSlot.Supply].Id);
        Assert.Equal(18, loadout.SupplyQuantity);
        Assert.Equal(18, loadout.Equipped[EquipmentSlot.Supply].Quantity);
    }

    // 只有 1 瓶：第 2 回合喝完 → 補給欄清空、那一列刪除，第 5 回合不再喝
    [Fact]
    public async Task Advance_ClearsSupplySlotWhenEmpty()
    {
        var character = await AddWeakCharacterAsync();
        await EquipSupplyAsync(character, quantity: 1);
        clock.Advance(TimeSpan.FromSeconds(1));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(1, result.Summary.PotionsUsed);
        var loadout = await equipment.GetAsync(character.Id, CancellationToken);
        Assert.False(loadout.Equipped.ContainsKey(EquipmentSlot.Supply));
        Assert.Equal(0, loadout.SupplyQuantity);
        Assert.Empty(await BagAsync(character.Id));
    }

    [Fact]
    public async Task Advance_WritesNoSupply_WhenCharacterUpdateConflicts()
    {
        var character = await AddWeakCharacterAsync();
        var supplyId = await EquipSupplyAsync(character, quantity: 1);
        inventory.Writes.Clear();
        repository.FailNextUpdate = true;
        clock.Advance(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<ConflictException>(() => service.AdvanceAsync(AccountId, CancellationToken));

        Assert.Empty(inventory.Writes);
        var loadout = await equipment.GetAsync(character.Id, CancellationToken);
        Assert.Equal(supplyId, loadout.Equipped[EquipmentSlot.Supply].Id);
        Assert.Equal(1, loadout.SupplyQuantity);
        Assert.Equal([("test-supply", 1)], await BagAsync(character.Id));
    }

    private static MissionRecord ActiveRecord(string missionId, int progress = 0) =>
        new(missionId, MissionState.Active, progress, DailyPeriod: null, AcceptedAt: Start, CompletedAt: null);

    private async Task AddMissionAsync(Character character, MissionRecord record)
    {
        await missions.AddAsync(character.Id, record, CancellationToken);
        log.Clear();
    }

    // 20 秒 8 場全勝雪精：test-hunt 0/3 → 3/3；進度在角色更新與熟練度之後寫入
    [Fact]
    public async Task Advance_PersistsMissionProgress()
    {
        var character = await AddCharacterAsync();
        await AddMissionAsync(character, ActiveRecord("test-hunt"));
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(3, (await missions.ListAsync(character.Id, CancellationToken)).Single().Progress);
        Assert.Equal(["update", "proficiency:warrior", "mission:progress:test-hunt"], log);
        Assert.Equal(3, result.Summary.MissionKills);
        Assert.Equal([1, 2, 3], result.Events.OfType<MissionProgressEvent>().Select(mission => mission.Progress));
    }

    // 推進讀到進行中的 test-hunt 之後，玩家放棄了它（刪除紀錄、角色版本 +1）：推進 409，不寫回也不重建任務。
    // 另一個沒被放棄的 test-sprite-bounty 也不能有任何進度寫入（不論先後，任務寫入都不得早於角色的版本檢查）
    [Fact]
    public async Task Advance_WritesNoMissionProgress_WhenCharacterUpdateConflicts()
    {
        var character = await AddCharacterAsync();
        await AddMissionAsync(character, ActiveRecord("test-hunt"));
        await AddMissionAsync(character, ActiveRecord("test-sprite-bounty"));
        clock.Advance(TimeSpan.FromSeconds(20));
        var advance = new AdventureService(
            repository, proficiencies, skillBook, loadout, inventory, equipment, missions, pets, Content, clock,
            new HookRandomFactory(() =>
            {
                missions.RemoveAsync(character.Id, "test-hunt", CancellationToken).GetAwaiter().GetResult();
                repository.UpdateAsync(character, CancellationToken).GetAwaiter().GetResult();
                log.Clear();
            }),
            unitOfWork);

        await Assert.ThrowsAsync<ConflictException>(() => advance.AdvanceAsync(AccountId, CancellationToken));

        var records = await missions.ListAsync(character.Id, CancellationToken);
        Assert.DoesNotContain(records, record => record.MissionId == "test-hunt");
        Assert.Equal(0, records.Single(record => record.MissionId == "test-sprite-bounty").Progress);
        Assert.DoesNotContain(log, entry => entry.StartsWith("mission:", StringComparison.Ordinal));
    }

    // 已完成的 test-hunt（進度刻意留在 0，確認看的是狀態而不是進度）、只有交付目標的 test-delivery、內容已移除的任務：
    // 8 場都打雪精，但沒有任何進度寫入
    [Fact]
    public async Task Advance_IgnoresCompletedDeliveryAndUnknownMissions()
    {
        var character = await AddCharacterAsync();
        await AddMissionAsync(character, ActiveRecord("test-hunt") with { State = MissionState.Completed, CompletedAt = Start });
        await AddMissionAsync(character, ActiveRecord("test-delivery"));
        await AddMissionAsync(character, ActiveRecord("removed-mission"));
        var before = await missions.ListAsync(character.Id, CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(8, result.Summary.Wins);
        Assert.Equal(0, result.Summary.MissionKills);
        Assert.Empty(result.Events.OfType<MissionProgressEvent>());
        Assert.DoesNotContain(log, entry => entry.StartsWith("mission:", StringComparison.Ordinal));
        Assert.Equal(before, await missions.ListAsync(character.Id, CancellationToken));
    }

    // 兩隻寵物、只有小雪攜帶：每場遭遇都帶小雪（測試雪精第 1 回合就被擊倒，寵物來不及出手）
    [Fact]
    public async Task Advance_UsesCarriedPetOnly()
    {
        var character = await AddCharacterAsync();
        await pets.AddAsync(character.Id, TestPets.Create("小狼", "test-wolf", carried: false), CancellationToken);
        await pets.AddAsync(character.Id, TestPets.Create("小雪", "test-sprite", carried: true), CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        var encounters = result.Events.OfType<EncounterEvent>().ToList();
        Assert.Equal(8, encounters.Count);
        Assert.All(encounters, encounter => Assert.Equal(("小雪", "test-sprite"), (encounter.PetName, encounter.PetMonsterId)));
    }

    // 寵物都在獸欄：遭遇不帶寵物，事件與完全沒有寵物的角色相同
    [Fact]
    public async Task Advance_WithoutCarriedPetMatchesNoPets()
    {
        var character = await AddCharacterAsync();
        await pets.AddAsync(character.Id, TestPets.Create("小狼", "test-wolf", carried: false), CancellationToken);
        clock.Advance(TimeSpan.FromSeconds(20));
        var baseline = new AdventureServiceTests();
        await baseline.AddCharacterAsync();
        baseline.clock.Advance(TimeSpan.FromSeconds(20));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);
        var expected = await baseline.service.AdvanceAsync(AccountId, CancellationToken);

        Assert.All(result.Events.OfType<EncounterEvent>(), encounter => Assert.True(encounter is { PetName: null, PetMonsterId: null }));
        Assert.Equivalent(expected.Events, result.Events, strict: true);
        Assert.Equal(expected.Summary, result.Summary);
    }

    private static async Task<long> EquipCaptureNetAsync(
        InMemoryInventoryRepository bag, InMemoryEquipmentRepository slots, Character character, int quantity)
    {
        var id = await bag.AddAsync(character.Id, TestItems.Entry(0, "test-capture-net", quantity), CancellationToken);
        await slots.SetSlotAsync(character.Id, EquipmentSlot.Supply, id, quantity, CancellationToken);
        return id;
    }

    // 補給欄裝測試捕獸網（倍率 2）×20，擲骰 0.0：每個勝場都損壞，前 3 場再各捕到一隻（之後寵物已滿）。20 秒 8 場 → 20 − 8 − 3 = 9。
    // 寵物在獸欄、能力 rand(1,10) = 1 × 2、個性 1 積極、名字＝怪物名，出生於擊殺時刻（0、2.8、5.6 秒）。
    // 背包共用 log 以確認寫入順序：寵物在補給欄寫回之後
    [Fact]
    public async Task Advance_PersistsCapturedPetsAndItemCount()
    {
        var loggedInventory = new InMemoryInventoryRepository(log);
        var loggedEquipment = new InMemoryEquipmentRepository(loggedInventory);
        var advance = new AdventureService(
            repository, proficiencies, skillBook, loadout, loggedInventory, loggedEquipment, missions, pets, Content, clock, new ScriptedRandomFactory(), unitOfWork);
        var character = await AddCharacterAsync();
        var netId = await EquipCaptureNetAsync(loggedInventory, loggedEquipment, character, quantity: 20);
        log.Clear();
        clock.Advance(TimeSpan.FromSeconds(20));

        var result = await advance.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(8, result.Summary.Wins);
        Assert.Equal(3, result.Summary.PetsCaptured);
        var stored = await pets.ListAsync(character.Id, CancellationToken);
        Assert.Equal(3, stored.Count);
        Assert.All(stored, pet =>
        {
            Assert.Equal(
                ("test-sprite", "測試雪精", PetPersonality.Aggressive, 2, 2, 2, 1, 80, 0, false),
                (pet.MonsterId, pet.Name, pet.Personality, pet.Attack, pet.Magic, pet.Defense, pet.Spirit, pet.Satiety, pet.Intimacy, pet.Carried));
            Assert.Equal(pet.BornAt.AddSeconds(-20), pet.HungerSince);
        });
        Assert.Equal([0.0, 2.8, 5.6], stored.Select(pet => Math.Round((pet.BornAt - Start).TotalSeconds, 3)));
        var slots = await loggedEquipment.GetAsync(character.Id, CancellationToken);
        Assert.Equal(9, slots.SupplyQuantity);
        Assert.Equal(9, slots.Equipped[EquipmentSlot.Supply].Quantity);
        Assert.Equal(["update", "proficiency:warrior", "add:test-supply", $"quantity:{netId}", "pet:add:1", "pet:add:2", "pet:add:3"], log);
    }

    // 同 Adventure_WritesBackRemainingQuantity 的弱角色（HP 低於 15% 而且輸掉）：補給欄是捕捉道具時不會喝
    [Fact]
    public async Task Advance_NeverDrinksCaptureItem()
    {
        var character = await AddWeakCharacterAsync();
        await EquipCaptureNetAsync(inventory, equipment, character, quantity: 5);
        clock.Advance(TimeSpan.FromSeconds(1));

        var result = await service.AdvanceAsync(AccountId, CancellationToken);

        Assert.Equal(1, result.Summary.Losses);
        Assert.Empty(result.Events.OfType<PotionEvent>());
        Assert.Equal(0, result.Summary.PotionsUsed);
        Assert.Equal(5, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);
    }

    // 版本衝突：捕到的寵物與道具數量都不寫
    [Fact]
    public async Task Advance_WritesNoPets_WhenCharacterUpdateConflicts()
    {
        var character = await AddCharacterAsync();
        await EquipCaptureNetAsync(inventory, equipment, character, quantity: 20);
        repository.FailNextUpdate = true;
        clock.Advance(TimeSpan.FromSeconds(20));

        await Assert.ThrowsAsync<ConflictException>(() => service.AdvanceAsync(AccountId, CancellationToken));

        Assert.DoesNotContain(log, entry => entry.StartsWith("pet:", StringComparison.Ordinal));
        Assert.Empty(await pets.ListAsync(character.Id, CancellationToken));
        Assert.Equal(20, (await equipment.GetAsync(character.Id, CancellationToken)).SupplyQuantity);
    }

    private sealed class ScriptedRandomFactory : IRandomSourceFactory
    {
        public IRandomSource Create() => new ScriptedRandom(0.0);
    }

    // 模擬開始時（讀取全部完成後）執行 hook，模擬推進讀取之後別的請求先寫入
    private sealed class HookRandomFactory(Action hook) : IRandomSourceFactory
    {
        public IRandomSource Create()
        {
            hook();
            return new ScriptedRandom(0.0);
        }
    }

    // 模擬開始時取亂數源，藉此在 log 裡標出模擬的時間點
    private sealed class LoggingRandomFactory(List<string> log) : IRandomSourceFactory
    {
        public IRandomSource Create()
        {
            log.Add("simulate");
            return new ScriptedRandom(0.0);
        }
    }
}
