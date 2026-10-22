using EndlessBattle.Domain.Adventure;
using EndlessBattle.Domain.Battle;
using EndlessBattle.Domain.Equipment;
using EndlessBattle.Domain.Items;
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
    StatAllocationRequest? Allocation,
    string? SpecialAttackName);

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
    string SpecialAttackName,
    StatsResponse Stats,
    BaseStatsResponse BaseStats,
    int PlaceId,
    string PlaceName,
    int? SubPlace,
    string? SubPlaceName,
    string AreaName,
    int ActNum,
    int ActNumMax,
    IReadOnlyList<JobProficiencyResponse> JobProficiency);

public sealed record JobProficiencyResponse(string JobId, long Proficiency, long SkillPoints);

// Events 只含最後 30 秒開始的場次與住宿，其餘只計入 Summary；EventsCoverAll 為 false 表示有戰鬥被略去（只略去住宿仍為 true）
public sealed record AdvanceResponse(
    IReadOnlyList<BattleEvent> Events,
    AdvanceSummary Summary,
    bool EventsCoverAll,
    CharacterResponse Character);

public sealed record LearnSkillRequest(string? SkillId);

// 省略的欄位＝清空；主動欄省略 Chance 時用預設發動率，被動欄的 Chance 一律由伺服器填入
public sealed record SetSkillSlotsRequest(IReadOnlyList<SkillSlotRequest?>? Slots);

public sealed record SkillSlotRequest(int? Slot, string? SkillId, int? Chance);

public sealed record SkillOverviewResponse(
    string JobId,
    long SkillPoints,
    IReadOnlyList<SkillResponse> Skills,
    IReadOnlyList<SkillSlotResponse> Slots);

// Level 0 表示尚未學習；Next 在已滿級時為 null
public sealed record SkillResponse(
    string Id,
    string Name,
    SkillType Type,
    SkillEffectKind Effect,
    string Description,
    string JobId,
    int Level,
    int MaxLevel,
    SkillNextLevelResponse? Next);

public sealed record SkillNextLevelResponse(
    int Level,
    int Sp,
    int Rounds,
    int Chance,
    long Money,
    long Proficiency,
    bool CanLearn,
    IReadOnlyList<string> Reasons);

public sealed record SkillSlotResponse(int Slot, string SkillId, int Chance);

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

// Recommendation 只是提示，移動不看等級；禁區為 None
public sealed record WorldPlaceResponse(
    int Id,
    string Name,
    IReadOnlyList<SubPlaceResponse> SubPlaces,
    LevelRangeResponse Recommended,
    bool Restricted,
    bool Current,
    Recommendation Recommendation);

public sealed record SubPlaceResponse(int Index, string Name);

public sealed record LevelRangeResponse(int Min, int Max);

// 沒有子地區的地區 SubPlace 必須為 null
public sealed record MoveRequest(int? PlaceId, int? SubPlace);

// 背包只含沒有裝備上的道具；每個分類都會列出（含空分類）。Equipment 只列有裝備的欄位，補給欄的 Quantity 是補給數量
public sealed record InventoryResponse(
    IReadOnlyList<BagCategoryResponse> Bag,
    IReadOnlyList<EquippedItemResponse> Equipment,
    StatBonus Bonus);

public sealed record BagCategoryResponse(ItemCategory Category, int Capacity, IReadOnlyList<InventoryItemResponse> Items);

public sealed record EquippedItemResponse(EquipmentSlot Slot, int Quantity, InventoryItemResponse Item);

public sealed record InventoryItemResponse(
    long InventoryId,
    string ItemId,
    string Name,
    ItemCategory Category,
    ConsumableKind Consumable,
    int Quantity,
    StatBonus Bonus,
    StatRequirement7 Requires,
    string? JobId,
    int SupplyHp,
    int SupplySp,
    int Amount,
    int RefineLevel,
    int RefineMax,
    StatBonus RefineBonus,
    int Holes,
    IReadOnlyList<SocketResponse> Sockets,
    long Price,
    bool Bound,
    int? RefineStoneLevel); // 只有精鍊石有值

public sealed record SocketResponse(int Hole, string StoneId, StatBonus Rolled);

// Quantity 只用於補給品，省略時整格裝上
public sealed record EquipRequest(long? InventoryId, int? Quantity);

public sealed record UnequipRequest(EquipmentSlot? Slot);

public sealed record UseItemRequest(long? InventoryId);

public sealed record ShopItemResponse(
    string Id, string Name, ItemCategory Category, long Price, StatBonus Bonus, StatRequirement7 Requires, int Holes, int RefineMax);

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

// Success：精鍊、打洞為擲骰結果；鑲嵌、合成成功才會回 200，一律為 true
public sealed record ForgeResponse(bool Success, InventoryResponse Inventory);

public sealed record RecipeResponse(
    string Id,
    string ResultItemId,
    string ResultName,
    int ResultQuantity,
    IReadOnlyList<RecipeMaterialResponse> Materials,
    int SuccessRate,
    long CostPerAttempt);

public sealed record RecipeMaterialResponse(string ItemId, string Name, int Quantity);

public sealed record SynthesizeRequest(string? RecipeId, int? Attempts);

// Cost 是實際扣的金錢（1000 × Successes）；材料不論成敗都依 Attempts 扣
public sealed record SynthesisResponse(int Successes, int Attempts, long Cost, InventoryResponse Inventory);

// 拆除一律成功；Recovered 為 false 表示魔石沒有回收（沒擲中或背包已滿）
public sealed record UnsocketResponse(bool Success, bool Recovered, InventoryResponse Inventory);
