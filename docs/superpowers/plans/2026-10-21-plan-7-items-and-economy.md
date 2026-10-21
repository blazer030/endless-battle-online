# 計畫 7：道具與經濟 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 WOG 的道具經濟搬進來：道具表（裝備、補給品、精鍊石、魔石、材料）、6 格裝備＋補給欄、背包（每分類 15 格、堆疊 99）、商店（依分類與等級 1～7）、賣給商店 50%、倉庫 20 格、銀行（Lv ≥ 15）、戰鬥掉落、戰鬥中自動補給、精鍊（`101−10n%`）、鑲嵌、合成；裝備加成進入戰鬥數值；前端新增背包分頁（背包／裝備／商店／倉庫／銀行／鍛造）。

**Architecture:** 道具與配方是內容（`backend/content/items.json`、`recipes.json`；`monsters.json` 加 `drops`）。Domain 新增 `Items/`（定義、背包規則）、`Equipment/`（欄位、需求、加成與有效數值）、`Economy/`（商店、銀行、倉庫）、`Forge/`（精鍊、鑲嵌、合成）；戰鬥引擎加入補給品自動使用與掉落。資料用正規列（V7：`character_inventory`、`character_equipment`、`character_depot`；`characters` 加 `bank`、`bag_bonus`、`depot_bonus`），不沿用 WOG 的逗號字串。前端背包分頁六個子頁。

**Tech Stack:** 沿用。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §8、§4（自動補給、掉落）、§7（銀行）、§5.2（轉職卸裝備）；研究依據 `docs/research/wog-research-items.md`、`wog-research-battle.md` §4。

## Global Constraints

- 計畫 1～6 的 Global Constraints 全部適用。每個任務完成後都要經過 **test-runner**（獨立跑 `./scripts/check.sh`）與 **reviewer**。〔使用者要求〕
- 規則一字不差照 spec §8 與研究：**沒有等級需求、沒有耐久**；裝備需求比對**裸點**（`Base`）與職業（`ch_id` 0 = 不限）；背包每分類 15 格（道具與魔石另加 `bag_bonus`），堆疊上限 99，裝備每件一格；倉庫 20 格（加 `depot_bonus`），堆疊合併上限 254；賣給商店 `floor(price × 0.5 × 數量)`（鑲嵌／精鍊一併消失，不補償）；商店只賣 `shopSellable == true` 的品項，依分類與 `shopLevel` 1～7 分頁；銀行 `Lv ≥ 15` 才可存，隨時可領。
- 能力計算照研究 §2.3：七項 = `Base` + 裝備 + 魔石 + 精鍊；`at = 裝備 at + str`、`mat = 裝備 mat + smart`、`df = 裝備 df + vit`、`mdf = 裝備 mdf + be`；`hpmax = round(life×25)+50 + 魔石 hpmax`、`spmax = round((smart+be)×3)+25`；補給欄（道具欄 2）的道具**不計**加成。現有 `DerivedStats.From(Base)` 改為 `DerivedStats.From(Base, EquipmentBonus)`，`CombatBridge` 與 `CharacterResponse.Stats` 都改用有效數值。
- 自動補給照研究 §5.1：補給品放補給欄（含數量 1～99）；每回合玩家出手前（技能判定之前）若 `hp < hpmax×15%` **且** `sp < spmax×100%`（即 SP 未滿）且本場已用 < 9 次 → `hp += supplyHp`、`sp += supplySp`（各自不超過上限）、數量 −1、產生 `PotionEvent`；門檻先固定（玩家可調留到計畫 10）。有補給品時，計畫 6 的 15% 自動住宿仍適用（補給先、住宿後）。
- 掉落照 spec §4：怪物 `drops: [{ itemId, topr }]`，每個配對獨立 `rand(1, floor(topr×0.9)) ≤ 1`（`topr×0.9 < 1` 視為必掉）；行動力不足時 `topr ×1.5`；背包該分類滿則丟棄並在 `RewardEvent.Drops` 標 `Discarded`。`RewardEvent` 加 `IReadOnlyList<DropResult(ItemId, Name, Quantity, bool Discarded)> Drops`。
- 精鍊照研究 §3.1：精鍊石 `RefineStone(Level 1～10, Kind 1～18, 能力範圍 min,max)`；目標等級 `n = 目前+1` 必須等於石頭等級；上限 `RefineMax`；`rand(1,100)`：`10n > r` 失敗；失敗只損石頭（或安全石），裝備不壞；成功把各能力 `rand(min,max)` 累加到裝備的 `RefineBonus`；無金錢成本。精鍊石合成（同類 +1 級必成功、異類 −1 級擲種類）本計畫做同類合成，異類留到計畫 10。
- 鑲嵌照研究 §3.2：孔數 ≤ 4；孔位 ≤ 孔數；同裝備不可重複同種魔石；固定魔石取定值、隨機魔石鑲嵌當下 `rand(min,max)` 固定；拆除需道具 `stone-remover`，`rand(1,100) < 25` 回收否則消失；打洞需道具 `hole-driller`，`rand(1,100) ≤ 8` 成功（改為同一 `ItemDefinition` 的 `Holes+1`，不用換 id——Claude 的簡化）。
- 合成照研究 §3.3：配方 `Recipe(Id, Result, ResultQuantity, Materials[{itemId, quantity}], SuccessRate%)`；每次 1000 金錢，**只有成功才扣**；材料不論成敗都消耗；每次獨立擲骰。
- 消耗品本計畫做：HP/SP 補給品（補給欄）、能力藥水（永久加裸點）、行動力藥水（+N，上限 50）、背包擴充（設定 `bag_bonus`，不累加）、倉庫擴充；密藥、寶箱、技能書、心得書留到計畫 10。
- 內容數值（WOG 資料遺失，Claude 的設計；測試釘住區間）：裝備依商店等級 `L`（1～7）：武器 `at = round(8·L^1.8)`、頭身手腳 `df` 分別 `round(1.5/4/2/2 × L^1.8)`、法系武器 `mat` 同武器公式、飾品類道具欄 1 給能力 +`2L`；需求 `str/smart/agi/au ≥ 10 + 8(L−1)`（依職業向）；價格 `200·L³`；精鍊上限 `3 + L`；孔數 0～2。每格裝備分類每等級至少 2 件（物理向、法系向），共 ≥ 70 件裝備；補給品 小／中／大 `hp 50/200/800`、`sp 25/100/400`、價 `20/80/300`；精鍊石 LV1～10 各 3 種；魔石 6 種（固定）＋2 種（隨機）；材料 4 種；配方 6 條。`docs/research/item-table.md` 列總表。
- `AdvanceSummary` 加 `Drops`（總件數）；新事件 `PotionEvent(AtSeconds, string ItemId, string Name, int Hp, int Sp, int Remaining)` discriminator `"potion"`；`RewardEvent` 新增 `Drops`。
- 轉職時裝備全部卸下（spec §5.2）：`JobService.ChangeAsync` 在換職後呼叫卸裝（背包滿則 400 `bagFull` 不轉職）。
- 程式碼註解中文、非必要不寫；commit 規則同前；生成檔只由 scaffold 產生；migration 不得 DROP。

## Review Focus

- **背包滿時的掉落**：該分類 15 格已滿且無可堆疊同項時，掉落被丟棄並標示，不會丟例外、不會覆寫。→ Task 5 `Drops_DiscardWhenBagFull`。
- **裝備需求用裸點**：穿上提高力量的裝備後，不能因此滿足另一件更高需求的裝備（需求比對 `Base`）。→ Task 3 `Equip_RequirementsUseBaseStats`。
- **自動補給每場最多 9 次**：HP 一直低於門檻時第 10 次不再使用，數量也不再減少。→ Task 6 `Battle_PotionStopsAfterNineUses`。
- **精鍊失敗不壞裝備**：+5 失敗後裝備仍在、精鍊等級不變、只少一顆石頭（或安全石）。→ Task 7 `Refine_FailureKeepsEquipment`。
- **賣出有鑲嵌的裝備**：賣價仍是基礎價 50%，鑲嵌與精鍊資料一併刪除。→ Task 4 `Sell_IgnoresEnhancements`。

---

## File Structure

```
backend/
  content/items.json、recipes.json（新）、monsters.json（加 drops）
  db/ddl/V000000007__add_inventory_tables.sql
  EndlessBattle.Domain/Items/{ItemDefinition, ItemCategory, ConsumableKind, InventoryRules, InventoryEntry}.cs
  EndlessBattle.Domain/Equipment/{EquipmentSlot, Loadout, EquipmentRules, EquipmentBonus}.cs、Characters/DerivedStats.cs（修改）、CombatBridge.cs（修改）
  EndlessBattle.Domain/Economy/{Shop, Bank, Depot}.cs
  EndlessBattle.Domain/Battle/{PotionRules, BattleEvents(PotionEvent, RewardEvent.Drops)}.cs、WogBattle.cs（補給）
  EndlessBattle.Domain/Adventure/{DropRoller, AdventureSimulator}.cs
  EndlessBattle.Domain/Forge/{Refine, Socket, Synthesis}.cs
  EndlessBattle.Application/Inventory/{IInventoryRepository, IEquipmentRepository, IDepotRepository, InventoryService}.cs
  EndlessBattle.Application/Economy/{ShopService, BankService, DepotService}.cs、Forge/ForgeService.cs、Jobs/JobService.cs（卸裝）
  EndlessBattle.Infrastructure/Persistence/{InventoryRepository, EquipmentRepository, DepotRepository}.cs、Content/JsonGameContent.cs
  EndlessBattle.Api/Endpoints/{InventoryEndpoints, ShopEndpoints, BankEndpoints, DepotEndpoints, ForgeEndpoints}.cs、Contracts.cs
  EndlessBattle.Tests/TestContent/{items.json, recipes.json}、monsters.json（drops）
  docs/research/item-table.md
frontend/src/
  api/schema.ts、client.ts；game/{inventory.ts, battleLog.ts, stageModel.ts, types.ts}
  ui/BagScreen.tsx（子頁：背包、裝備、商店、倉庫、銀行、鍛造）＋ css；AdventureScreen.tsx（開放背包分頁）；CharacterScreen.tsx（裝備加成顯示）
```

---

### Task 1: 道具內容與定義

**Files:** `content/items.json`、`recipes.json`、`Tests/TestContent/*`、`Domain/Items/*`、`IGameContent.cs`、`JsonGameContent.cs`、`docs/research/item-table.md`；測試 `Infrastructure/JsonGameContentTests.cs`、`Content/ItemTableTests.cs`

**Interfaces:**

```csharp
public enum ItemCategory { Weapon, Head, Body, Hand, Foot, Accessory, Consumable, Stone, RefineStone, Material }   // Accessory = 道具欄 1 的裝備
public enum ConsumableKind { None, Supply, StatPotion, StaminaPotion, BagExpansion, DepotExpansion }
public sealed record StatBonus(int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be, int At, int Mat, int Df, int Mdf, int HpMax = 0);
public sealed record StatRequirement7(int Str, int Smart, int Agi, int Au);
public sealed record RefineStoneSpec(int Level, int Kind, StatRange? At, StatRange? Mat, StatRange? Df, StatRange? Mdf, StatRange? Str, StatRange? Smart, StatRange? Life, StatRange? Vit, StatRange? Agi, StatRange? Au, StatRange? Be);
public sealed record StoneSpec(bool Random, StatBonus Fixed, StatBonus? Min, StatBonus? Max);
public sealed record ItemDefinition(string Id, string Name, ItemCategory Category, ConsumableKind Consumable, StatBonus Bonus, int SupplyHp, int SupplySp, int Amount /* 藥水加值、擴充格數、行動力 */, StatRequirement7 Requires, string? JobId, int Element, int Holes, int RefineMax, long Price, int ShopLevel, bool ShopSellable, bool Bound, RefineStoneSpec? Refine, StoneSpec? Stone)
{ public bool IsEquipment => Category is Weapon or Head or Body or Hand or Foot or Accessory; public bool Stackable => !IsEquipment; }
public sealed record Recipe(string Id, string ResultItemId, int ResultQuantity, IReadOnlyList<(string ItemId, int Quantity)> Materials, int SuccessRate);
// IGameContent：AllItems、GetItem、ItemsForShop(category, level)、AllRecipes、GetRecipe
```

- [ ] 測試：`Items_LoadTable`（真實內容：≥70 件裝備，每個裝備分類每商店等級 ≥2、價格與公式一致、需求單調遞增、補給品三檔、精鍊石 30、魔石 8、材料 4、配方 6）；`Items_RejectDuplicateId`、`Items_RejectEquipmentWithSupply`、`Items_RejectShopLevelOutsideRange`、`Items_RejectHolesAboveFour`、`Recipes_RejectUnknownItem`、`Recipes_RejectRateOutsideRange`；`ItemTable_MatchesFormula`（抽查 L1／L4／L7 武器 at 與價格）。
- [ ] Commit `✨ feat: load the item table and recipes`

---

### Task 2: 資料表與 repository

**Files:** `db/ddl/V000000007__add_inventory_tables.sql`、scaffold、`Domain/Items/InventoryEntry.cs`、`Equipment/Loadout.cs`、`Application/Inventory/I*Repository.cs`、`Infrastructure/Persistence/*Repository.cs`、`DependencyInjection.cs`；測試 `Infrastructure/InventoryRepositoriesTests.cs`、`DatabaseSchemaTests.cs`

```sql
CREATE TABLE character_inventory (id BIGINT AUTO_INCREMENT PRIMARY KEY, character_id BIGINT NOT NULL, item_id VARCHAR(32) NOT NULL, quantity INT NOT NULL DEFAULT 1, refine_level INT NOT NULL DEFAULT 0, refine_bonus JSON NULL, sockets JSON NULL, holes INT NOT NULL DEFAULT 0, FOREIGN KEY → characters);
CREATE TABLE character_equipment (character_id BIGINT NOT NULL, slot VARCHAR(16) NOT NULL, inventory_id BIGINT NOT NULL, supply_quantity INT NULL, PRIMARY KEY (character_id, slot), FOREIGN KEY → characters, FOREIGN KEY → character_inventory(id));
CREATE TABLE character_depot (id BIGINT AUTO_INCREMENT PRIMARY KEY, character_id BIGINT NOT NULL, item_id VARCHAR(32) NOT NULL, quantity INT NOT NULL, refine_level INT NOT NULL DEFAULT 0, refine_bonus JSON NULL, sockets JSON NULL, holes INT NOT NULL DEFAULT 0, FOREIGN KEY → characters);
ALTER TABLE characters ADD COLUMN bank BIGINT NOT NULL DEFAULT 0, ADD COLUMN bag_bonus INT NOT NULL DEFAULT 0, ADD COLUMN depot_bonus INT NOT NULL DEFAULT 0;
```

```csharp
public sealed record InventoryEntry(long Id, string ItemId, int Quantity, int RefineLevel, StatBonus RefineBonus, IReadOnlyList<SocketedStone> Sockets, int Holes);
public sealed record SocketedStone(int Hole, string StoneId, StatBonus Rolled);
public enum EquipmentSlot { Weapon, Head, Body, Hand, Foot, Accessory, Supply }
public sealed record Loadout(IReadOnlyDictionary<EquipmentSlot, InventoryEntry> Equipped, int SupplyQuantity);
// IInventoryRepository：ListAsync、AddAsync(entry)→id、UpdateQuantityAsync、UpdateEnhancementAsync、RemoveAsync；全部以 characterId 為範圍
// IEquipmentRepository：GetAsync(characterId)→Loadout、SetSlotAsync(characterId, slot, inventoryId?, supplyQuantity?)
// IDepotRepository：ListAsync、AddAsync、RemoveAsync、UpdateQuantityAsync
```

- [ ] 測試：`Inventory_RoundTripsEnhancements`（JSON 欄位）、`Equipment_SetAndClearSlot`、`Depot_RoundTrips`、`Schema_HasInventoryTables`、`Equipment_RejectsInventoryOfAnotherCharacter`（FK 加服務層檢查）。
- [ ] Commit `🚧 chore: add inventory, equipment and depot tables`

---

### Task 3: 背包規則、裝備與有效數值

**Files:** `Domain/Items/InventoryRules.cs`、`Equipment/{EquipmentRules, EquipmentBonus}.cs`、`Characters/DerivedStats.cs`、`CombatBridge.cs`、`Application/Inventory/InventoryService.cs`、`Characters/CharacterService.cs`、`Api/Endpoints/{InventoryEndpoints, CharacterEndpoints, Contracts}.cs`；測試對應

**Interfaces:**

```csharp
public static class InventoryRules
{
    public const int SlotsPerCategory = 15, StackLimit = 99;
    public static int Capacity(ItemCategory c, int bagBonus);            // Consumable、Stone 加 bagBonus
    public static AddResult TryAdd(IReadOnlyList<InventoryEntry> bag, ItemDefinition item, int quantity, int bagBonus);   // 先堆疊再開新格；AddResult(Accepted, Discarded, NewEntries, UpdatedEntries)
}
public static class EquipmentRules
{
    public static IReadOnlyList<string> CanEquip(Character c, ItemDefinition item);   // 代碼：notEquipment、wrongSlot、jobMismatch、statTooLow:<stat>
    public static EquipmentSlot SlotOf(ItemCategory c);
}
public sealed record EquipmentBonus(StatBonus Total);   // 裝備 + 魔石 + 精鍊（不含補給欄）
public static class EquipmentBonusCalculator { public static EquipmentBonus Sum(Loadout loadout, Func<string, ItemDefinition> items); }
// DerivedStats.From(BaseStats, EquipmentBonus) → 七項有效值與 At/Mat/Df/Mdf/HpMax/SpMax（舊的單參數版本 = 零加成）
// InventoryService：GetAsync→InventoryView(Bag by category, Loadout, Capacity)、EquipAsync(inventoryId, supplyQuantity?)、UnequipAsync(slot)、UseAsync(inventoryId)（消耗品：能力藥水、行動力、擴充）
// API：GET /api/inventory；POST /api/inventory/equip { inventoryId, quantity? }；POST /api/inventory/unequip { slot }；POST /api/inventory/use { inventoryId }
```

- [ ] 測試：`TryAdd_StacksThenOpensSlots`、`TryAdd_DiscardsOverflow`、`Equip_RequirementsUseBaseStats`、`Equip_SwapsBackToBag`（原裝備回背包；背包滿 → 400 `bagFull`）、`Equip_SupplySlotKeepsQuantity`（1～99）、`Stats_IncludeEquipmentAndStones`（手算：武器 at 28、str 14 → at 42）、`Me_ShowsEffectiveStats`、`Use_StatPotionRaisesBase`、`Use_StaminaPotionCapsAtFifty`、`Use_BagExpansionSetsNotAdds`。
- [ ] Commit `✨ feat: carry a bag, equip six slots and fight with equipment stats`

---

### Task 4: 商店、銀行、倉庫

**Files:** `Domain/Economy/{Shop, Bank, Depot}.cs`、`Application/Economy/*Service.cs`、`Api/Endpoints/{ShopEndpoints, BankEndpoints, DepotEndpoints}.cs`；測試對應

```csharp
public static class Shop { public static long BuyCost(ItemDefinition, int qty); public static long SellPrice(ItemDefinition, int qty) => (long)Math.Floor(item.Price * 0.5 * qty); }
public static class Bank { public const int MinimumLevel = 15; public static IReadOnlyList<string> CanDeposit(Character, long amount); }   // levelTooLow、invalidAmount、notEnoughMoney
public static class DepotRules { public const int Slots = 20, StackLimit = 254; … }
// API：GET /api/shop?category=&level=；POST /api/shop/buy { itemId, quantity }；POST /api/shop/sell { inventoryId, quantity }
//      POST /api/bank/deposit { amount }；POST /api/bank/withdraw { amount }；GET /api/depot；POST /api/depot/store { inventoryId, quantity }；POST /api/depot/take { depotId, quantity }
```

- [ ] 測試：`Buy_DeductsMoneyAndAddsToBag`、`Buy_RejectsWhenBagFull`、`Buy_OnlyShopSellableByLevel`、`Sell_IgnoresEnhancements`（有鑲嵌／精鍊的裝備賣價仍 50%，列刪除）、`Bank_RequiresLevel15`、`Bank_WithdrawAnytime`、`Depot_StoresAndTakes`、`Depot_StackLimit254`、`Depot_RejectsBound`（綁定物可存可取——WOG 綁定只擋贈送；裁定：倉庫允許）。
- [ ] Commit `✨ feat: trade with the shop, bank money and use the depot`

---

### Task 5: 掉落

**Files:** `content/monsters.json`（`drops`）、`Domain/Content/MonsterDefinition.cs`、`Domain/Adventure/{DropRoller, AdventureSimulator}.cs`、`Battle/BattleEvents.cs`（`RewardEvent.Drops`、`AdvanceSummary.Drops`）、`Application/Adventure/AdventureService.cs`（寫入背包）；測試對應

```csharp
public sealed record DropEntry(string ItemId, int Topr);
public sealed record DropResult(string ItemId, string Name, int Quantity, bool Discarded);
public static class DropRoller { public static IReadOnlyList<string> Roll(IReadOnlyList<DropEntry> drops, double fDown, IRandomSource random); }   // 每項 rand(1, floor(topr×0.9×(fDown<1?1.5:1))) ≤ 1
```

- [ ] 測試：`Roll_EachEntryIndependent`（ScriptedRandom）、`Roll_DepletedStaminaHalvesChance`、`Roll_ToprBelowOneAlwaysDrops`、`Drops_DiscardWhenBagFull`、`Advance_PersistsDrops`（背包多了掉落物；摘要 `Drops` 計數含被丟棄？——裁定：只計入背包的）、`Roster_DropsReferenceKnownItems`（內容驗證）。
- [ ] 每隻怪 1～3 個掉落（補給品、材料、低機率裝備／精鍊石），`topr` 3～200。
- [ ] Commit `✨ feat: drop items after victories`

---

### Task 6: 自動補給

**Files:** `Domain/Battle/{PotionRules, CombatantState, WogBattle, BattleEvents}.cs`、`Adventure/AdventureSimulator.cs`（剩餘數量寫回）、`Application/Adventure/AdventureService.cs`；測試 `Domain/Battle/WogBattlePotionTests.cs`、`Adventure` 測試

```csharp
public sealed record SupplyState(string ItemId, string Name, int Hp, int Sp, int Quantity);
public static class PotionRules { public const double HpThreshold = 0.15, SpThreshold = 1.0; public const int MaxUsesPerFight = 9; public static bool ShouldUse(CombatantState p, SupplyState? s, int usedThisFight); }
// WogBattle.Run(..., SupplyState? supply, ...)：每回合玩家出手前先判定補給；PotionEvent(AtSeconds, ItemId, Name, Hp, Sp, Remaining)
```

- [ ] 測試：`Battle_UsesPotionBelowThreshold`（hp 14/100、sp 50/100 → 補 50/25，事件）、`Battle_NoPotionWhenSpFull`、`Battle_PotionStopsAfterNineUses`、`Battle_PotionCapsAtMax`、`Adventure_WritesBackRemainingQuantity`（用完 → 補給欄清空）。
- [ ] Commit `✨ feat: drink supplies automatically in battle`

---

### Task 7: 精鍊與鑲嵌

**Files:** `Domain/Forge/{Refine, Socket}.cs`、`Application/Forge/ForgeService.cs`、`Api/Endpoints/ForgeEndpoints.cs`；測試對應

```csharp
public static class Refine { public static RefineOutcome Attempt(InventoryEntry equipment, ItemDefinition equipmentDef, ItemDefinition stone, bool useSafeStone, IRandomSource random); }   // RefineOutcome(Success, Entry, ConsumedStone, ConsumedSafeStone)；代碼 wrongStoneLevel、atRefineMax、notEquipment
public static class Socket { public static SocketOutcome Insert(InventoryEntry eq, int hole, ItemDefinition stone, IRandomSource random); public static RemoveOutcome Remove(InventoryEntry eq, int hole, IRandomSource random); public static DrillOutcome Drill(InventoryEntry eq, ItemDefinition def, IRandomSource random); }
public static class StoneMerge { public static ItemDefinition? SameKindNext(ItemDefinition a, ItemDefinition b, Func<int,int,ItemDefinition?> find); }   // 同類 +1
// API：POST /api/forge/refine { inventoryId, stoneInventoryId, useSafeStone }；POST /api/forge/socket { inventoryId, hole, stoneInventoryId }；POST /api/forge/unsocket { inventoryId, hole }；POST /api/forge/drill { inventoryId }；POST /api/forge/merge-stones { stoneInventoryId, quantityPairs }
```

- [ ] 測試：`Refine_SuccessRateIs101Minus10n`（Theory n=1..10 用擲骰邊界）、`Refine_FailureKeepsEquipment`、`Refine_SafeStoneKeepsRefineStone`、`Refine_RequiresNextLevelStone`、`Refine_StopsAtMax`、`Socket_RejectsDuplicateStoneKind`、`Socket_RandomStoneRollsOnce`、`Unsocket_RecoversAt24Percent`、`Drill_SucceedsAt8Percent`、`Merge_SameKindRaisesLevel`。
- [ ] Commit `✨ feat: refine and socket equipment`

---

### Task 8: 合成與轉職卸裝

**Files:** `Domain/Forge/Synthesis.cs`、`Application/Forge/ForgeService.cs`、`Jobs/JobService.cs`、`Api/Endpoints/ForgeEndpoints.cs`；測試對應

```csharp
public static class Synthesis { public const long CostPerAttempt = 1000; public static SynthesisOutcome Attempt(Recipe r, int attempts, IReadOnlyList<InventoryEntry> bag, IRandomSource random); }   // Outcome(Successes, MaterialsConsumed, Cost = 1000×Successes)
// API：GET /api/forge/recipes；POST /api/forge/synthesize { recipeId, attempts }
```

- [ ] 測試：`Synthesis_ConsumesMaterialsEvenOnFailure`、`Synthesis_ChargesOnlySuccesses`、`Synthesis_RejectsMissingMaterials`、`Synthesis_RespectsBagCapacity`、`JobChange_UnequipsEverything`（含補給欄；背包滿 → 400 `bagFull`，職業不變）。
- [ ] Commit `✨ feat: synthesize items and unequip on job change`

---

### Task 9: 前端：背包、裝備、商店、倉庫、銀行

**Files:** `api/schema.ts`、`client.ts`、`game/{types, inventory}.ts`、`ui/BagScreen.tsx`（+css，子頁 tabs）、`AdventureScreen.tsx`（開放背包）、`CharacterScreen.tsx`（能力顯示「裸點（+裝備）」）；測試對應

**文案：** 背包子頁「背包／裝備／商店／倉庫／銀行／鍛造」；背包依分類 tabs 顯示 `{名稱} ×{數量}`、裝備顯示 `+{精鍊}`、孔 `◆/◇`；裝備區六格＋補給欄，點格子顯示可換裝清單與需求（未達標 crimson-text）；商店依分類與等級 1～7 分頁，列 `{名稱} {價格} 金錢`，按鈕「購買」（數量 stepper 1～99）；賣出「賣出（{價格}）」；倉庫「存入／取出」；銀行「存款 {bank}」與「存入／提領」表單（Lv < 15 顯示「Lv 15 開放」）。錯誤代碼對照：`bagFull` → 「背包已滿」、`notEnoughMoney` → 「金錢不足」、`statTooLow:str` → 「力量不足」…

- [ ] 測試：`inventory.test.ts`（分類排序、代碼對照）、`BagScreen.test.tsx`（六子頁、購買送出正確 body、裝備需求顯示、銀行等級鎖）。
- [ ] Commit `✨ feat: add the bag screen with shop, depot and bank`

---

### Task 10: 前端：鍛造與戰報

**Files:** `ui/BagScreen.tsx`（鍛造子頁）、`game/{battleLog, stageModel}.ts`、`render/*`；測試對應

**文案：** 鍛造子頁三區「精鍊／鑲嵌／合成」；精鍊顯示 `+{n} → +{n+1}（成功率 {101−10(n+1)}%）`，勾選「使用安全石」；結果 `精鍊成功！{裝備} +{n}` ／ `精鍊失敗，{石頭}消失了。`；鑲嵌選孔位與魔石；合成列配方材料與成功率。戰報：`potion` → `使用了{補給品}，恢復 {hp} HP／{sp} SP。`（normal）；reward 的 drops → `獲得 {名稱} ×{n}`（drop）／`背包已滿，{名稱}被丟棄了。`（muted）；舞台：補給時角色綠色閃光，掉落時物品飄字。

- [ ] 測試：`battleLog` potion／drop／discard 三列；`stageModel` potion flash、drop float；`BagScreen` 鍛造子頁送出 refine body、結果文案。
- [ ] 手動驗收（控制者）：買一把武器穿上看物攻上升、買補給品放補給欄看戰鬥中使用、精鍊一次、截圖 `plan-7-bag.png`。
- [ ] Commit `✨ feat: forge equipment and narrate drops and supplies`

---

## 自我檢查

- Spec §8：分類與欄位（T1）、無等級需求無耐久（T3）、6 格＋補給欄（T3／T6）、背包 15／99、倉庫 20（T3／T4）、賣 50%（T4）、商店分頁與 `shopSellable`（T4）、精鍊公式（T7）、鑲嵌規則（T7）、合成 1000／次成功才扣、材料必耗（T8）、消耗品（T3；密藥等留計畫 10）。§4 自動補給（T6）、掉落（T5）。§7 銀行（T4）。§5.2 轉職卸裝（T8）。
- 型別一致：`ItemDefinition`／`StatBonus`（T1）→ T3～T10；`InventoryEntry`／`Loadout`（T2）→ T3～T8；`EquipmentBonus`→`DerivedStats`（T3）→ T6 戰鬥；`DropResult`／`PotionEvent`（T5／T6）→ T10。
- 計畫規模大（10 個任務）：若執行中發現單一任務超過一天的工作量，可在 T5 與 T6 之間切成 7a／7b 分支分別合併。
