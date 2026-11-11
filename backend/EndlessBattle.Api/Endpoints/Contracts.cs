using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
using EndlessBattle.Domain.Pets;
using EndlessBattle.Domain.Skills;
using EndlessBattle.Domain.World;

namespace EndlessBattle.Api.Endpoints;

// 欄位允許 null：缺欄位時由 Application 的驗證回 400，而不是在反序列化時失敗
public sealed record CredentialsRequest(string? Username, string? Password);

public sealed record RegisterResponse(long AccountId);

public sealed record LoginResponse(string AccessToken);

public sealed record AccountResponse(long AccountId, string Username);

public sealed record NamedContent(string Id, string Name);

public sealed record JobContent(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<StatRequirementContent> StatRequirements,
    JobGrowthContent Growth);

public sealed record StatRequirementContent(string Stat, int Minimum);

// 每項為 [min, max]
public sealed record JobGrowthContent(int[] Str, int[] Smart, int[] Life, int[] Vit, int[] Agi, int[] Au, int[] Be);

public sealed record ContentResponse(
    IReadOnlyList<NamedContent> Monsters,
    IReadOnlyList<JobContent> Jobs,
    IReadOnlyList<NamedContent> Elements,
    IReadOnlyList<NamedContent> Births,
    IReadOnlyList<NamedContent> Sexes);

public sealed record CreateCharacterRequest(
    string? Name,
    int? Sex,
    int? Element,
    int? Birth,
    string? JobId,
    StatAllocationRequest? Allocation);

public sealed record StatAllocationRequest(int? Str, int? Smart, int? Life, int? Agi);

public sealed record StatsResponse(
    int Str,
    int Smart,
    int Life,
    int Vit,
    int Agi,
    int Au,
    int Be,
    int At,
    int Mat,
    int Df,
    int Mdf,
    int HpMax,
    int SpMax);

// 七項裸點；前端顯示「裸點（+裝備）」
public sealed record BaseStatsResponse(int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be);

// Stats 為有效值（裸點 + 裝備）
public sealed record CharacterResponse(
    string Name,
    int Level,
    long Exp,
    long NextExp,
    long Money,
    long Bank,
    int Hp,
    int Sp,
    int Sex,
    int Element,
    int Birth,
    string JobId,
    string JobName,
    StatsResponse Stats,
    BaseStatsResponse BaseStats,
    int PlaceId,
    string PlaceName,
    int? SubPlace,
    string? SubPlaceName,
    string AreaName,
    IReadOnlyList<JobProficiencyResponse> JobProficiency,
    AdventureSettingsResponse Settings);

// 冒險設定：Attack null 表示自動；補給與住宿 -1 表示略過該條件；逃跑 0 表示不逃
public sealed record AdventureSettingsResponse(
    AttackKind? Attack, int PotionHpPercent, int PotionSpPercent, int RestHpPercent, int RestSpPercent, int EscapeHpPercent);

// 範圍由 Domain 驗證；數值欄位缺漏時回 400
public sealed record AdventureSettingsRequest(
    AttackKind? Attack,
    int? PotionHpPercent,
    int? PotionSpPercent,
    int? RestHpPercent,
    int? RestSpPercent,
    int? EscapeHpPercent);

// 道具與魔石的加成
public sealed record StatBonusResponse(int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be, int At, int Mat, int Df, int Mdf, int HpMax)
{
    internal static StatBonusResponse From(StatBonus bonus) =>
        new(bonus.Strength, bonus.Intelligence, bonus.Life, bonus.Vitality, bonus.Agility, bonus.Charisma, bonus.Faith, bonus.Attack, bonus.MagicAttack, bonus.Defense, bonus.MagicDefense, bonus.MaxHealthPoints);
}

// 裝備需求只比對這四項裸點
public sealed record EquipmentRequirementResponse(int Str, int Smart, int Agi, int Au)
{
    internal static EquipmentRequirementResponse From(EquipmentRequirement requirement) =>
        new(requirement.Strength, requirement.Intelligence, requirement.Agility, requirement.Charisma);
}

public sealed record AdvanceSummaryResponse(
    int Fights,
    int Wins,
    int Losses,
    int Timeouts,
    long Exp,
    long Money,
    long JobExp,
    int LevelsGained,
    int SkillsCast,
    int Drops,
    int PotionsUsed,
    int MissionKills,
    int PetsCaptured,
    int Escapes)
{
    internal static AdvanceSummaryResponse From(AdvanceSummary summary) => new(
        summary.Fights,
        summary.Wins,
        summary.Losses,
        summary.Timeouts,
        summary.Experience,
        summary.Money,
        summary.JobExperience,
        summary.LevelsGained,
        summary.SkillsCast,
        summary.Drops,
        summary.PotionsUsed,
        summary.MissionKills,
        summary.PetsCaptured,
        summary.Escapes);
}

public sealed record JobProficiencyResponse(string JobId, long Proficiency, long SkillPoints);

// Events 只含最後 30 秒開始的場次與住宿，其餘只計入 Summary；EventsCoverAll 為 false 表示有戰鬥被略去（只略去住宿仍為 true）
// Mercenary 只在這次推進有傭兵戰鬥時才有值；AreaPassExpiresAt 只在目前地區是禁區且有通行證時有值（可能已過期）
public sealed record AdvanceResponse(
    IReadOnlyList<BattleEventResponse> Events,
    AdvanceSummaryResponse Summary,
    bool EventsCoverAll,
    CharacterResponse Character,
    MercenaryReportResponse? Mercenary,
    DateTimeOffset? AreaPassExpiresAt);

// Fights 是這次推進的場數；Recent 是最後 5 場，依時間；Dismissed 表示次數用完、已解除僱用
public sealed record MercenaryReportResponse(
    string Name,
    int Fights,
    int Wins,
    long Exp,
    long Money,
    int LevelsGained,
    int Remaining,
    bool Dismissed,
    IReadOnlyList<MercenaryFightResponse> Recent);

public sealed record MercenaryFightResponse(DateTimeOffset At, string MonsterId, string MonsterName, bool Won, long Exp, long Money);

public sealed record LearnSkillRequest(string? SkillId);

// 省略的欄位＝清空；發動率由技能決定，請求不帶
public sealed record SetSkillSlotsRequest(IReadOnlyList<SkillSlotRequest?>? Slots);

public sealed record SkillSlotRequest(int? Slot, string? SkillId);

public sealed record SkillOverviewResponse(
    string JobId,
    long SkillPoints,
    IReadOnlyList<SkillResponse> Skills,
    IReadOnlyList<SkillSlotResponse> Slots);

// Level 0 表示尚未學習；Summary 是目前等級的效果文字（未學為 null），Description 是技能的文字說明；Prerequisite 在沒有前置技能時為 null；Next 在已滿級時為 null
public sealed record SkillResponse(
    string Id,
    string Name,
    SkillType SlotType,
    SkillTiming Timing,
    string Description,
    string? Icon,
    int Level,
    int MaxLevel,
    string? Summary,
    SkillPrerequisiteResponse? Prerequisite,
    SkillNextLevelResponse? Next);

public sealed record SkillPrerequisiteResponse(string SkillId, string Name, int Level);

public sealed record SkillNextLevelResponse(int Level, long Money, long Proficiency, string Summary, bool CanLearn, IReadOnlyList<string> Reasons);

public sealed record SkillSlotResponse(int Slot, string SkillId);

public sealed record JobOptionResponse(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    string? ParentName,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<StatRequirementContent> StatRequirements,
    JobGrowthContent Growth,
    bool Current,
    int ProficiencyPercent,
    bool Eligible,
    IReadOnlyList<string> Reasons);

public sealed record ChangeJobRequest(string? JobId);

public sealed record WorldAreaResponse(string Id, string Name, IReadOnlyList<WorldPlaceResponse> Places);

// Recommendation 只是提示，移動不看等級；禁區為 None。Keys 是背包裡這個地區的鑰匙數，
// PassExpiresAt 是通行證到期時刻（可能已過期），兩者只有禁區才有
public sealed record WorldPlaceResponse(
    int Id,
    string Name,
    IReadOnlyList<SubPlaceResponse> SubPlaces,
    LevelRangeResponse Recommended,
    bool Restricted,
    bool Current,
    Recommendation Recommendation,
    int Keys,
    DateTimeOffset? PassExpiresAt);

public sealed record SubPlaceResponse(int Index, string Name);

public sealed record LevelRangeResponse(int Min, int Max);

// 沒有子地區的地區 SubPlace 必須為 null
public sealed record MoveRequest(int? PlaceId, int? SubPlace);

// 背包只含沒有裝備上的道具；每個分類都會列出（含空分類）。Equipment 只列有裝備的欄位，補給欄的 Quantity 是補給數量
public sealed record InventoryResponse(
    IReadOnlyList<BagCategoryResponse> Bag,
    IReadOnlyList<EquippedItemResponse> Equipment,
    StatBonusResponse Bonus);

public sealed record BagCategoryResponse(ItemCategory Category, int Capacity, IReadOnlyList<InventoryItemResponse> Items);

public sealed record EquippedItemResponse(EquipmentSlot Slot, int Quantity, InventoryItemResponse Item);

public sealed record InventoryItemResponse(
    long InventoryId,
    string ItemId,
    string Name,
    ItemCategory Category,
    ConsumableKind Consumable,
    int Quantity,
    StatBonusResponse Bonus,
    EquipmentRequirementResponse Requires,
    string? JobId,
    int SupplyHp,
    int SupplySp,
    int Amount,
    int RefineLevel,
    int RefineMax,
    StatBonusResponse RefineBonus,
    int Holes,
    IReadOnlyList<SocketResponse> Sockets,
    long Price,
    bool Bound,
    int? RefineStoneLevel, // 只有精鍊石有值
    bool Enhanced, // 精鍊過、有鑲嵌或比原本多打了孔
    WeaponSkillResponse? WeaponSkill); // 只有附帶技能的武器有值

public sealed record WeaponSkillResponse(string Name, int Level, int Rate);

public sealed record SocketResponse(int Hole, string StoneId, string StoneName, StatBonusResponse Rolled);

// Quantity 只用於補給品，省略時整格裝上
public sealed record EquipRequest(long? InventoryId, int? Quantity);

public sealed record UnequipRequest(EquipmentSlot? Slot);

public sealed record UseItemRequest(long? InventoryId);

// Obtained：開出的道具名稱，依抽出順序；PetName：寵物蛋孵出的寵物；SkillPoints：加在目前職業的技能點；LevelsGained：經驗膠囊造成的升級數；
// Buff：喝下的密藥
public sealed record UseItemResponse(
    InventoryResponse Inventory, IReadOnlyList<string> Obtained, string? PetName, long SkillPoints, int LevelsGained, BuffResponse? Buff);

// 生效中的密藥：開打時刻早於 EndsAt 的場次乘上 Multiplier
public sealed record BuffResponse(BuffKind Kind, double Multiplier, DateTimeOffset EndsAt);

public sealed record ShopItemResponse(
    string Id, string Name, ItemCategory Category, long Price, StatBonusResponse Bonus, EquipmentRequirementResponse Requires, int Holes, int RefineMax);

public sealed record BuyRequest(string? ItemId, int? Quantity);

public sealed record SellRequest(long? InventoryId, int? Quantity);

public sealed record BankAmountRequest(long? Amount);

public sealed record DepotResponse(IReadOnlyList<InventoryItemResponse> Entries, int Capacity);

public sealed record DepotMoveResponse(InventoryResponse Inventory, DepotResponse Depot);

public sealed record StoreRequest(long? InventoryId, int? Quantity);

public sealed record TakeRequest(long? DepotId, int? Quantity);

// UseSafeStone 省略時為 false
public sealed record RefineRequest(long? InventoryId, long? StoneInventoryId, bool? UseSafeStone);

// Hole 從 1 起算
public sealed record SocketRequest(long? InventoryId, int? Hole, long? StoneInventoryId);

public sealed record UnsocketRequest(long? InventoryId, int? Hole);

public sealed record DrillRequest(long? InventoryId);

public sealed record MergeStonesRequest(long? StoneInventoryId, int? QuantityPairs);

public sealed record MergeCrossRequest(long? FirstInventoryId, long? SecondInventoryId, int? QuantityPairs);

// Success：精鍊、打洞為擲骰結果；鑲嵌、合成成功才會回 200，一律為 true
public sealed record ForgeResponse(bool Success, InventoryResponse Inventory);

public sealed record RecipeResponse(
    string Id,
    string ResultItemId,
    string ResultName,
    int ResultQuantity,
    IReadOnlyList<RecipeMaterialResponse> Materials,
    int SuccessRate,
    long CostPerAttempt,
    RecipeRequiredMissionResponse? RequiredMission);

// 要先完成的任務；Completed：這個角色已經完成
public sealed record RecipeRequiredMissionResponse(string Id, string Title, bool Completed);

public sealed record RecipeMaterialResponse(string ItemId, string Name, int Quantity);

public sealed record SynthesizeRequest(string? RecipeId, int? Attempts);

// Cost 是實際扣的金錢（1000 × Successes）；材料不論成敗都依 Attempts 扣
public sealed record SynthesisResponse(int Successes, int Attempts, long Cost, InventoryResponse Inventory);

// 拆除一律成功；Recovered 為 false 表示魔石沒有回收（沒擲中或背包已滿）
public sealed record UnsocketResponse(bool Success, bool Recovered, InventoryResponse Inventory);

public sealed record MissionRequest(string? MissionId);

// ActiveLimit 是同時進行中的上限；NextDailyReset 為 UTC
public sealed record MissionBoardResponse(
    IReadOnlyList<MissionResponse> Available,
    IReadOnlyList<MissionResponse> Active,
    IReadOnlyList<MissionResponse> Completed,
    int ActiveLimit,
    DateTimeOffset NextDailyReset);

// 每日任務的 Kills 與 Rewards 是該週期擲出的結果；ExpiresAt 只有每日任務有值
public sealed record MissionResponse(
    string Id,
    string Giver,
    string Title,
    string Description,
    int Level,
    int? PlaceId,
    string? PlaceName,
    bool Daily,
    IReadOnlyList<MissionKillResponse> Kills,
    IReadOnlyList<MissionDeliverResponse> Deliver,
    MissionRequirementsResponse Requirements,
    MissionRewardResponse Rewards,
    bool Ready,
    DateTimeOffset? ExpiresAt,
    MissionPetResponse? DeliverPet,
    MissionRequiredSkillResponse? RequiredSkill);

// 回報時要達到的技能等級；Met：已學等級達到要求
public sealed record MissionRequiredSkillResponse(string SkillId, string Name, int Level, bool Met);

// 要交出的寵物物種；Carried：攜帶中的寵物是這個物種
public sealed record MissionPetResponse(string MonsterId, string Name, bool Carried);

public sealed record MissionKillResponse(string MonsterId, string MonsterName, int PlaceId, string PlaceName, int Count, int Progress);

// Owned 是背包中的持有數（不含已裝備與補給欄）
public sealed record MissionDeliverResponse(string ItemId, string Name, int Quantity, int Owned);

public sealed record MissionRequirementsResponse(string? JobId, int? Sex, int? Birth, string? PrerequisiteId, string? PrerequisiteTitle);

public sealed record MissionRewardResponse(
    long Exp, long Money, IReadOnlyList<MissionRewardItemResponse> Items, MissionRewardSkillResponse? Skill, MissionRewardPetResponse? Pet);

public sealed record MissionRewardItemResponse(string ItemId, string Name, int Quantity);

public sealed record MissionRewardSkillResponse(string SkillId, string Name, int Level);

// Level 是能力倍率
public sealed record MissionRewardPetResponse(string MonsterId, string Name, int Level);

// Exp、Money、Items、Skill、Pet 是這次任務的報酬；SkillGranted 為 false 表示沒有技能獎勵或已會該技能；Character 已含獎勵
public sealed record MissionCompleteResponse(
    MissionBoardResponse Board,
    CharacterResponse Character,
    string CompletionMessage,
    long Exp,
    long Money,
    IReadOnlyList<MissionRewardItemResponse> Items,
    MissionRewardSkillResponse? Skill,
    bool SkillGranted,
    int LevelsGained,
    MissionRewardPetResponse? Pet);

// carry、store、release 只看 PetId；rename 另看 Name；feed 另看 FoodId
public sealed record PetRequest(long? PetId, string? Name, string? FoodId);

public sealed record PetsResponse(IReadOnlyList<PetResponse> Pets, int MaxPets, IReadOnlyList<PetFoodResponse> Foods);

// Species 是怪物名；Age 只顯示；NextFeedAt 之前餵食會被拒絕（tooSoon）；
// ActivationPercent 是對魅力與玩家相同的怪物的出擊率估計（%，一位小數）
public sealed record PetResponse(
    long Id,
    string MonsterId,
    string Species,
    string Name,
    PetPersonality Personality,
    int Attack,
    int Magic,
    int Defense,
    int Spirit,
    int Satiety,
    int Intimacy,
    bool Carried,
    int Age,
    DateTimeOffset NextFeedAt,
    double ActivationPercent);

public sealed record PetFoodResponse(string Id, string Name, PetStat Stat, int Amount, long Cost);

// Character 已扣餵食的金錢
public sealed record PetFeedResponse(PetsResponse Pets, CharacterResponse Character);

// hire 只看 MercenaryId；deploy 只看 PlaceId；rename 只看 Name；rest、dismiss 不需要 body
public sealed record MercenaryRequest(string? MercenaryId, int? PlaceId, string? Name);

public sealed record MercenaryOverviewResponse(IReadOnlyList<MercenaryCatalogueResponse> Catalogue, MercenaryContractResponse? Contract);

// Price 為 null：只能在勳章所兌換
public sealed record MercenaryCatalogueResponse(
    string Id, string Name, long? Price, int Fights, int MoneyPercent, int DropPercent, int ExpPercent, int DeathCost);

// PlaceId／PlaceName／NextFightAt 只在出擊中有值
public sealed record MercenaryContractResponse(
    string MercenaryId, string Name, int Remaining, int? PlaceId, string? PlaceName, bool Fighting, DateTimeOffset? NextFightAt);

// Character 已扣僱用的金錢
public sealed record MercenaryHireResponse(MercenaryOverviewResponse Mercenaries, CharacterResponse Character);

// exchange 只看 ExchangeId
public sealed record HonourExchangeRequest(string? ExchangeId);

public sealed record HonourShopResponse(IReadOnlyList<HonourMedalResponse> Medals, IReadOnlyList<HonourExchangeResponse> Exchanges);

// Owned 是背包中的持有數
public sealed record HonourMedalResponse(string ItemId, string Name, int Owned);

// Kind："item" | "mercenary"；RewardId 是道具或傭兵 id
public sealed record HonourExchangeResponse(
    string Id, string Kind, string RewardId, string RewardName, int Quantity, IReadOnlyList<HonourCostResponse> Cost, bool Affordable);

public sealed record HonourCostResponse(string ItemId, string Name, int Quantity, int Owned);

public sealed record HealthResponse(string Status);

// Price 依角色目前等級
public sealed record InnRoomResponse(string Id, string Name, BuffKind Kind, double Multiplier, int Seconds, long Price);

// 入住中的房型；開打時刻早於 EndsAt 的場次乘上房型倍率
public sealed record LodgingResponse(string RoomId, DateTimeOffset EndsAt);

public sealed record InnResponse(IReadOnlyList<InnRoomResponse> Rooms, LodgingResponse? Lodging);

public sealed record InnBookRequest(string? RoomId);

public sealed record InnBookResponse(CharacterResponse Character, LodgingResponse Lodging, long Cost);

public sealed record OptionResponse(int Id, string Name);

// StrongAgainst／WeakAgainst 是屬性 id：攻擊時剋制的對象、會被剋制的對象
public sealed record ElementOptionResponse(int Id, string Name, int StrongAgainst, int WeakAgainst);

public sealed record PublicSettingsResponse(
    IReadOnlyList<OptionResponse> Sexes,
    IReadOnlyList<ElementOptionResponse> Elements,
    IReadOnlyList<OptionResponse> Births);

public sealed record GrowthRangeResponse(int Minimum, int Maximum);

public sealed record JobGrowthResponse(
    GrowthRangeResponse Strength,
    GrowthRangeResponse Intelligence,
    GrowthRangeResponse Life,
    GrowthRangeResponse Vitality,
    GrowthRangeResponse Agility,
    GrowthRangeResponse Charisma,
    GrowthRangeResponse Faith);

// Stat 是 strength、intelligence、life、vitality、agility、charisma、faith 之一
public sealed record JobStatRequirementResponse(string Stat, int Minimum);

public sealed record SkillLevelSummaryResponse(int Level, string Summary);

// Prerequisite 在沒有前置技能時為 null；Levels 從 1 到最高級，Summary 與技能頁同一份文字
public sealed record JobSkillResponse(
    string Id,
    string Name,
    string Description,
    int MaxLevel,
    SkillPrerequisiteResponse? Prerequisite,
    IReadOnlyList<SkillLevelSummaryResponse> Levels);

// ParentId 在初階職業為 null
public sealed record PublicJobResponse(
    string Id,
    string Name,
    int Tier,
    string? ParentId,
    int UnlockLevel,
    int ProficiencyRequired,
    IReadOnlyList<JobStatRequirementResponse> StatRequirements,
    JobGrowthResponse Growth,
    IReadOnlyList<JobSkillResponse> Skills);
