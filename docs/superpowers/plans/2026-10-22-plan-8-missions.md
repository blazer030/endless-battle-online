# 計畫 8：任務 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 WOG 的單人任務搬進來：任務表（擊殺、交付、兩者兼具）、接取條件（等級／職業／性別／出生地／前置／互斥）、同時 3 個、掛機戰鬥中的擊殺計數（含只在任務進行中出現的任務怪）、交付與獎勵（經驗 `level × 190`、金錢、道具、技能）、每日任務（台北時間 09:00 重置、每個角色每天擲一次目標與報酬）；前端新增「任務」分頁與戰報的任務進度。

**Architecture:** 任務是內容（`backend/content/missions.json`；`monsters.json` 加 `missionId` 標記任務怪）。Domain 新增 `Missions/`：定義、接取規則、每日週期與決定性擲骰、任務板、擊殺計數、交付、獎勵；`AdventureSimulator` 帶著進行中的擊殺任務模擬，勝場即時計數並輸出 `MissionProgressEvent`。玩家狀態只有一張表 `character_missions`（每角色每任務一列，每日任務記住接取時的週期，擲骰結果由「角色＋任務＋週期」重算，不另存）。`MissionService` 的每個寫入都在一筆交易裡、角色版本更新最先；`AdventureService` 在同一筆推進交易裡寫回擊殺進度。

**Tech Stack:** 沿用（.NET 10、EF Core Database-First、Flyway、xUnit v3、React 19、Vitest）。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §9（任務）、§12 第 8 列、§2／§7.1（放置與離線結算）；研究依據 `docs/research/wog-research-world.md` §2，並以 PHP 原始碼（`class/wog_act_mission.php`、`class/wog_mission_tool.php`、`class/wog_fight_select.php`、`wog_fight.php`、`mission/wog_mission_*.php`、`js/wog_mission.js`、`language/wog_act_traditional_chinese.php`）查證補充。

## Global Constraints

- 計畫 1～7 的 Global Constraints 全部適用。每個任務完成後都要經過 **test-runner**（獨立跑 `./scripts/check.sh`）與 **reviewer**。〔使用者要求〕
- **接取條件**（PHP `mission_list` 與 `mission_get` 的聯集；`mission_get` 本身漏查出生地與前置，這裡一律檢查）：角色等級 ≥ 任務 `level`（`m_lv` 同時是等級門檻與經驗基數）；`jobId` 為 null（WOG 的 99）或等於**目前職業**；`sex` 為 null（WOG 的 3）或等於角色性別（1 男 2 女）；`birth` 為 null 或等於出生地（1～4）；`prerequisiteId` 必須是**已完成**的任務；`conflictsWith`（WOG `m_not_id`）只要有任何狀態的紀錄就不能接。完成時不再重查這些條件（同 PHP）。
- **同時 3 個**：`MissionRules.MaxActive = 3`，計算 `Active` 狀態（含目標已達成未回報；WOG `m_status < 2`）。已完成的不計。
- **狀態**：只存 `Active`／`Completed`；WOG 的 1（條件達成待交）由進度推得（`progress ≥ count`）。一般任務完成後永遠不能再接（紀錄留著）；每日任務每個週期接一次。放棄＝刪除紀錄、進度歸零、無懲罰，之後可再接；已完成的不能放棄。內容檔已移除的任務 id，其紀錄在各處一律忽略（不顯示、不計入 3 個、不計數）。
- **擊殺計數**（PHP `wog_fight_select.php` 的勝場分支）：只有**勝利**才計入，敗北、40 回合超限都不算；每擊殺一隻目標怪，所有以牠為目標、尚未達成的進行中任務各 +1（PHP 以怪物 id 為 key 覆寫，同一隻怪只算到其中一個任務——當成實作缺陷，裁定全部都算〔Claude 的判斷〕）；進度不超過 `count`；每日任務在其週期結束（下一個 09:00）時或之後打完的場次不計。擊殺任務只有**一個**目標（WOG 支援 `id*n,id*n` 多目標，但現存 8 個模板都是單一目標；多目標留待需要時再加〔Claude 的簡化〕）。
- **任務怪**（WOG `wog_monster.m_mission`）：`missionId` 非 null 的怪物只在該任務進行中且尚未達成時進入遇敵候選；未接、已達成、已完成、已過期時都不出現（含「沒有候選時退回全地區」的分支）。內容中沒有任務怪時，遇敵擲骰序列與計畫 7 完全相同。
- **交付**（PHP `mission_check` + `mission_item`）：只看背包（`BagReader.Bag`，不含已裝備與補給欄）；數量不足 → `itemsMissing`；扣除時同一道具優先拿未強化的列，再依數量少→多、id 小→大；裝備交出時鑲嵌與精鍊一併消失（同 PHP `del_stone`/`del_plus`）。
- **獎勵**（PHP `mission_end`）：經驗 = `level × 190`（`wog_mission_tool.php:208`），加完立刻套 `LevelUp.Apply`（PHP 只加 `p_exp`、下一場戰鬥才升級；放置版直接升級〔Claude 的判斷〕）；金錢 `money`；道具用 `InventoryRules.TryAdd` 依序放入「交付後」的背包，任何一件放不下 → 400 `bagFull`，整筆不寫（PHP 的 `item_in` 無視容量；這裡不讓獎勵消失，也不超量）；技能獎勵只給 Lv1，已會該技能時略過技能、其餘照給、回應 `SkillGranted = false`（PHP 會報錯卡死任務〔Claude 的判斷〕）；寵物獎勵 `rewards.pet` 已在 schema 定義，但內容載入時拒絕，等計畫 9 才開放。
- **每日任務**：時區 **Asia/Taipei**（spec 未規定；WOG 用伺服器本地時間 `mktime(9,0,0, 明天)`，原服在台灣〔裁定〕），以固定位移 `+08:00` 計算（台灣無夏令時間，不依賴主機時區資料庫）＝每天 **01:00 UTC** 重置。週期日期 `PeriodOf(t) = DateOnly.FromDateTime((t.ToOffset(+8h) − 9h).Date)`。每個每日任務每天對每個角色擲一次：種子 = FNV-1a 32 位元（offset `0x811C9DC5`、prime `0x01000193`）對 UTF-8 字串 `"{characterId}|{missionId}|{yyyy-MM-dd}"` 的雜湊（`unchecked` 轉 `int`），`new SeededRandom(seed)` 先 `NextInt(dailyKills.Count)` 選目標、再 `NextInt(dailyRewards.Count)` 選報酬（WOG 也是目標與報酬分開擲）。過期（紀錄週期 ≠ 現在週期）的每日紀錄不論狀態都視為不存在，寫入型用例順手刪除（WOG 每次讀取都 `DELETE` 過期紀錄）。
- **不做**：公會任務 `group_mission/`、`class/wog_group_mission.php`（多人，spec §1 非目標）；NPC 對話計數 `m_npc`（遇到即計數、不戰鬥）；交寵物的每日任務 223／225（計畫 9）；交易所資源獎勵 `ex_id/ex_num`（多人，改發金錢）；榮譽道具獎勵（勳章是計畫 9，先改發道具與金錢）；禁區任務（禁區要鑰匙，計畫 10）；分頁（WOG 每頁 8 筆，任務數量少不需要）；完成時的技能需求 `n_skill_id`（內容遺失、無設計需要）。
- 新事件 `MissionProgressEvent(AtSeconds, string MissionId, string Title, string MonsterId, int Progress, int Required)` discriminator `"mission"`；`AdvanceSummary` 加 `int MissionKills = 0`（整段推進的計數增量總和，含窗口外）；`AdventureRun` 加 `IReadOnlyList<ActiveMission> Missions`。
- 前端文案（一字不差）：底部分頁「任務」（順序 冒險／角色／技能／背包／任務）；子頁「可接／進行中／已完成」；區塊「每日任務」「一般任務」；按鈕「接取」「放棄」（再按「確定放棄」／「取消」）「交付」（有交付需求的任務）「完成」（只有擊殺目標的任務）；戰報 `任務進度：{怪物名} {progress}/{required}`（WOG 原文 `任務進度：` 顯示剩餘數，這裡改顯示已完成數）。
- 程式碼註解中文、非必要不寫；commit 規則同前；生成檔只由 scaffold／`pnpm generate:api` 產生；migration 不得 DROP。

## Review Focus

- **跨 09:00 的離線結算**：前一天接的每日任務在離線期間跨過重置，重置前打完的場次照算、之後的不算，不會因為「上線時已過期」整段作廢或整段照算。→ Task 5 `Adventure_StopsCountingDailyAfterReset`。
- **交付清出的格子放獎勵**：材料分類 15 格全滿、交付正好清掉一格 → 獎勵材料放得進去，完成成功；交付後仍放不下 → 400 `bagFull`，道具、金錢、經驗、任務狀態、角色版本全部不變。→ Task 7 `Complete_DeliveryFreesSlotForReward`、`Complete_BagFullRollsBackEverything`。
- **推進與放棄同時發生**：推進讀到進行中的任務後玩家放棄了它 → 推進 409，不會把進度寫回已刪除的任務、也不會重建紀錄。→ Task 5 `Advance_WritesNoMissionProgress_WhenCharacterUpdateConflicts`。
- **兩個任務要同一種怪**：兩個進行中的任務都要擊敗測試雪精 → 一次擊殺兩邊各 +1，已達成的那邊不再增加。→ Task 5 `KillCounter_CountsEveryMatchingMission`。
- **每日任務同一天重接**：今天完成的每日任務今天不能再接（`alreadyCompleted`），過了 09:00 才能再接，且新週期的進度從 0 開始、目標重擲。→ Task 7 `Complete_DailyReacceptOnlyAfterReset`。

---

## File Structure

```
backend/
  content/missions.json（新）、monsters.json（加 3 隻任務怪與 missionId）
  db/ddl/V000000008__add_missions.sql
  EndlessBattle.Domain/Missions/{MissionDefinition, MissionRecord, MissionRules, DailyMissions, MissionBoard, ActiveMission, KillCounter, Delivery, MissionRewards}.cs
  EndlessBattle.Domain/Content/MonsterDefinition.cs（MissionId）、World/MonsterPool.cs（任務怪過濾）
  EndlessBattle.Domain/Adventure/{AdventureSimulator, AdventureRun}.cs、Battle/BattleEvents.cs（MissionProgressEvent）
  EndlessBattle.Application/Missions/{IMissionRepository, MissionService}.cs、Adventure/AdventureService.cs、Content/IGameContent.cs、DependencyInjection.cs
  EndlessBattle.Infrastructure/Persistence/MissionRepository.cs、Content/JsonGameContent.cs、DependencyInjection.cs
  EndlessBattle.Api/Endpoints/{MissionEndpoints, Contracts}.cs、Program.cs
  EndlessBattle.Tests/TestContent/{missions.json, monsters.json}、Support/InMemoryMissionRepository.cs
docs/research/mission-table.md
frontend/src/
  api/schema.ts、client.ts；game/{missions.ts, battleLog.ts, battlePlayback.ts, stageModel.ts, types.ts}
  ui/MissionsScreen.tsx（+css）、AdventureScreen.tsx（任務分頁）
```

---

### Task 1: 任務內容、任務怪與定義

**Files:**
- Create: `backend/content/missions.json`、`backend/EndlessBattle.Domain/Missions/MissionDefinition.cs`、`backend/EndlessBattle.Tests/TestContent/missions.json`、`backend/EndlessBattle.Tests/Content/MissionTableTests.cs`、`docs/research/mission-table.md`
- Modify: `backend/content/monsters.json`、`backend/EndlessBattle.Tests/TestContent/monsters.json`、`Domain/Content/MonsterDefinition.cs`、`Application/Content/IGameContent.cs`、`Infrastructure/Content/JsonGameContent.cs`、`docs/research/monster-roster.md`
- Test: `Tests/Infrastructure/JsonGameContentTests.cs`、`Tests/Content/MissionTableTests.cs`、`Tests/Content/MonsterRosterTests.cs`

**Interfaces:**
- Consumes: `MonsterDefinition`、`ItemDefinition`、`SkillDefinition`、`JobDefinition`、`PlaceDefinition`（既有）
- Produces:

```csharp
namespace EndlessBattle.Domain.Missions;
public sealed record KillTarget(string MonsterId, int Count);
public sealed record ItemQuantity(string ItemId, int Quantity);
public sealed record MissionRequirements(string? JobId = null, int? Sex = null, int? Birth = null, string? PrerequisiteId = null, string? ConflictsWith = null);
public sealed record SkillReward(string SkillId, int Level);
public sealed record PetReward(string MonsterId, int Level);            // 計畫 9 才有內容；本計畫載入時拒絕
public sealed record MissionReward(long Money, IReadOnlyList<ItemQuantity> Items, SkillReward? Skill = null, PetReward? Pet = null);
public sealed record MissionDefinition(
    string Id, string Giver, string Title, string Description, string CompletionMessage,
    int Level, int? PlaceId, bool Daily,
    KillTarget? Kill, IReadOnlyList<ItemQuantity> Deliver,               // 一般任務：至少一項
    MissionRequirements Requirements, MissionReward Rewards,             // 每日任務的 Rewards 為空（Money 0、Items []），用 DailyRewards
    IReadOnlyList<KillTarget> DailyKills, IReadOnlyList<MissionReward> DailyRewards)
{
    public const long ExpPerLevel = 190;
    public long Exp => Level * ExpPerLevel;
}
// MonsterDefinition 加最後一個選填參數 string? MissionId = null
// IGameContent：IReadOnlyList<MissionDefinition> AllMissions（內容檔順序）、MissionDefinition GetMission(string missionId)（找不到丟 GameContentException）
```

`missions.json` 每筆欄位：`id, giver, title, description, completionMessage, level, placeId, daily, kill {monsterId, count}, deliver [{itemId, quantity}], requirements {jobId, sex, birth, prerequisiteId, conflictsWith}, rewards {money, items [{itemId, quantity}], skill {skillId, level}, pet}, dailyKills [{monsterId, count}], dailyRewards [{money, items}]`（省略的清單＝空、省略的物件＝null）。

**載入驗證**（`GameContentException`，訊息含任務 id）：id 不重複、長度 ≤ 32；giver、title 非空；level ≥ 1。一般任務：`placeId` 存在且非禁區、`kill` 與 `deliver` 至少一項、不得有 `dailyKills`／`dailyRewards`；`kill.count ≥ 1`、怪物存在且 `PlaceId == placeId`；`deliver` 道具存在、數量 1～99、不重複。每日任務：`placeId` 為 null、不得有 `kill`／`deliver`／`prerequisiteId`／`conflictsWith`、`rewards` 須為空；`dailyKills`、`dailyRewards` 各 ≥ 1，怪物存在且不是任務怪。條件：`jobId` 存在、`sex ∈ {1,2}`、`birth ∈ 1..4`、`prerequisiteId`／`conflictsWith` 指向存在的非每日任務且不是自己、前置鏈無循環。獎勵：`money ≥ 0`；道具存在、數量 1～99、不重複；`skill` 存在、`level == 1`、且 `requirements.jobId == skill.JobId`；`pet` 非 null → 拒絕（「寵物獎勵要到計畫 9」）。任務怪：`missionId` 指向存在的一般任務，該任務的 `kill.monsterId` 就是這隻怪，且不是全域怪。

**內容（Claude 的設計，WOG 主線內容全部遺失；一般任務金錢 = `50 × level`、經驗 = `190 × level`）：**

| id | 區域／地區 | 委託者 | 標題 | Lv | 目標 | 條件 | 道具／技能獎勵 |
|---|---|---|---|---|---|---|---|
| `slime-cleanup` | 中央大陸／1 | 平原農夫 托姆 | 綠地上的黏液 | 1 | 擊敗 `meadow-slime` ×8 | — | `small-supply` ×5 |
| `bog-cult` | 中央大陸／3 | 沼澤巡守 葛倫 | 狂火祭壇的異動 | 15 | 擊敗 `pyre-high-priest` ×3（任務怪） | 前置 `slime-cleanup` | `iron-refine-stone-1` ×2 |
| `crown-road-patrol` | 中央大陸／7 | 王都衛兵隊長 雷蒙 | 王者之路巡邏 | 22 | 擊敗 `trial-knight` ×12 | 互斥 `wraith-bargain` | `steel-armor` ×1 |
| `wraith-bargain` | 中央大陸／7 | 棧橋的怨靈 | 怨靈的交易 | 22 | 擊敗 `bridge-wraith` ×12 | 互斥 `crown-road-patrol` | `steel-sword` ×1 |
| `academy-dust` | 魔法王國／2 | 王立學院研究員 菲歐 | 學院的研究材料 | 8 | 交付 `magic-dust` ×5 | 出生地 2 | `arcane-refine-stone-1` ×3 |
| `veteran-iron-skin` | 魔法王國／2 | 老兵 巴洛克 | 戰士的硬甲 | 10 | 擊敗 `tunnel-goblin` ×10 | 職業 `warrior` | 技能 `iron-skin` Lv1 |
| `dryad-blessing` | 魔法王國／8 | 光輝樹精 希爾芙 | 樹精的祝福 | 20 | 交付 `crystal-shard` ×3 | 性別 2 | `vitality-stone` ×1 |
| `sky-sentry` | 魔法王國／12 | 天空之城書記官 | 失控的金飾哨兵 | 33 | 擊敗 `gilded-sentry` ×12 | 職業 `sorcerer` | 技能 `mana-skin` Lv1 |
| `fog-fox` | 熱帶雨林／4 | 獵人 席德 | 濃霧中的狐影 | 12 | 擊敗 `breeze-fox` ×10 | 職業 `thief` | 技能 `swift-shadow` Lv1 |
| `ogre-king` | 熱帶雨林／11 | 探險家 諾拉 | 裂谷巨魔王 | 35 | 擊敗 `rift-ogre-king` ×3（任務怪） | — | `vigor-refine-stone-2` ×2 |
| `frost-hides` | 熱帶雨林／16 | 皮革匠 歐文 | 寒地的毛皮 | 40 | 交付 `beast-hide` ×10 | 前置 `ogre-king` | `large-supply` ×3 |
| `star-song` | 熱帶雨林／9 | 吟遊詩人 塞拉 | 星絲之歌 | 55 | 擊敗 `star-weaver` ×12 | 職業 `bard` | 技能 `heal` Lv1 |
| `desert-raiders` | 末日王城／10 | 商隊長 哈桑 | 絲路劫匪 | 52 | 擊敗 `caravan-raider` ×15 | — | `medium-supply` ×10 |
| `relic-core` | 末日王城／5 | 遺跡考古隊 | 甦醒的遺跡巨像 | 65 | 擊敗 `relic-core-colossus` ×3（任務怪） | 前置 `desert-raiders` | `iron-refine-stone-3` ×2 |
| `ghost-captain-treasure` | 末日王城／14 | 港口老水手 | 幽靈船長的寶藏 | 100 | 擊敗 `ghost-captain` ×10 ＋ 交付 `crystal-shard` ×10 | — | `chaos-stone` ×1 |

每日任務（WOG 模板 222／224／271～274 的規則移植；目標池與報酬池各自擲骰；目標池「×{5,6,7}」表示每隻怪各列 count 5、6、7 三筆，等同 WOG 的 `rand(5,7)`；`bounty` 類只取該地區 `meet 0` 的怪，同 WOG `m_meet=0` 篩選；報酬原為交易所資源，改發金錢 `round(level × 50 × f)`，`f ∈ {0.8, 1.0, 1.2}`）：

| id | 移植自 | 委託者 | 標題 | Lv | 目標池 | 報酬池 |
|---|---|---|---|---|---|---|
| `daily-hunt-novice` | 222 | 冒險者公會 | 每日討伐：新手 | 5 | `meadow-slime` 6、`shore-crab` 6、`lantern-bat` 6、`tunnel-goblin` 6、`bog-toad` 5、`breeze-fox` 5、`rot-crawler` 4、`jungle-panther` 4 | 金錢 300；金錢 150＋`small-supply`×5；150＋`iron-ore`×5；150＋`magic-dust`×5；150＋`iron-refine-stone-1`×1 |
| `daily-hunt-veteran` | 224 | 冒險者公會 | 每日討伐：老手 | 25 | `trial-knight` 6、`bridge-wraith` 6、`trail-boar` 6、`radiant-dryad` 6、`fiend-scout` 5、`miasma-slime` 5、`cloud-griffin` 5、`illusion-moth` 5 | 皆 金錢 1250＋`medium-supply`×3，另加其一：無；`crystal-shard`×2；`beast-hide`×3；`arcane-refine-stone-1`×1；`vigor-refine-stone-1`×1 |
| `daily-bounty-swamp` | 271（地區 3、4） | 沼澤懸賞板 | 懸賞：沼澤與迷霧 | 10 | `bog-toad`、`breeze-fox`、`ruin-sentinel` ×{5,6,7} | 400；500；600 |
| `daily-bounty-ruins` | 272（地區 5、6） | 遺跡懸賞板 | 懸賞：遺跡與戰場 | 60 | `vine-guardian`、`fallen-soldier` ×{5,6,7} | 2400；3000；3600 |
| `daily-bounty-crown` | 273（地區 7、13） | 王都懸賞板 | 懸賞：王者之路與水晶之間 | 25 | `trial-knight`、`bridge-wraith`、`mirror-phantom`、`bloom-turtle` ×{5,6,7} | 1000；1250；1500 |
| `daily-bounty-beasts` | 274（地區 8、16） | 森林懸賞板 | 懸賞：幻獸森林與冷峰寒地 | 20 | `jungle-panther`、`trail-boar`、`snow-sprite` ×{5,6,7} | 800；1000；1200 |

任務怪（依 `MonsterFormula` 與 monster-roster 的角色倍率；`meet 60`、`subPlaces [1,2,3,4]`、掉落 `crystal-shard` topr 10）：`pyre-high-priest` 狂火大祭司（地區 3、Lv20、法＋精英、火 3、`missionId: bog-cult`）、`rift-ogre-king` 裂谷巨魔王（地區 11、Lv35、坦＋精英、地 1、`ogre-king`）、`relic-core-colossus` 遺跡核心巨像（地區 5、Lv75、坦＋精英、地 1、`relic-core`）。

測試內容 `TestContent/missions.json`：`test-hunt`（地區 1、Lv1、擊敗 `test-sprite` ×3、金錢 50）、`test-sprite-bounty`（地區 1、Lv1、擊敗 `test-sprite` ×2）、`test-delivery`（地區 1、Lv1、交付 `test-ore` ×3、獎勵 `safe-refine-stone` ×1＋金錢 50）、`test-chief-hunt`（地區 1、Lv1、擊敗任務怪 `test-sprite-chief` ×1）、`test-skill-reward`（地區 1、Lv5、職業 `warrior`、前置 `test-hunt`、擊敗 `test-wolf` ×2、技能 `iron-skin` Lv1）、`test-daily`（每日、Lv1、目標池 `test-sprite` 2／`test-wolf` 2、報酬池 金錢 100／`test-supply`×2）。`TestContent/monsters.json` 加 `test-sprite-chief`（地區 1、`subPlaces [1]`、`meet 60`、數值同 `test-sprite`、`missionId: test-chief-hunt`）。

- [ ] **Step 1: 寫失敗測試**（`JsonGameContentTests`）：`Load_TestContent_ExposesMissions`（6 筆、順序同檔案；`test-delivery` 結構比對 `Assert.Equivalent(..., strict: true)`；`GetMission("test-hunt").Exp == 190`）、`Missions_RejectDuplicateId`、`Missions_RejectIdLongerThan32`、`Missions_RejectWithoutObjective`、`Missions_RejectKillTargetOutsidePlace`、`Missions_RejectUnknownDeliverItem`、`Missions_RejectDailyWithFixedObjective`、`Missions_RejectDailyPoolWithMissionMonster`、`Missions_RejectPetReward`、`Missions_RejectSkillRewardWithoutMatchingJob`、`Missions_RejectPrerequisiteCycle`、`Monsters_RejectMissionIdWithoutMatchingMission`；既有 `Load_TestContent_ExposesMonsters` 的清單加上 `test-sprite-chief`。
- [ ] **Step 2: 寫失敗測試**（`MissionTableTests`，真實內容）：`MissionTable_CoversOpenAreas`（15 筆一般任務；中央大陸、魔法王國、熱帶雨林各 4、末日王城 3；禁區 0）、`MissionTable_MoneyIsFiftyPerLevel`、`MissionTable_HasSixDailies`（6 筆、id 同上表、`daily-hunt-novice` 目標池逐筆比對）、`MissionTable_BountyPoolsAreMeetZeroTimesFiveToSeven`、`MissionTable_MissionMonstersBelongToOneMission`（3 隻，各被恰好一筆任務引用）；`MonsterRosterTests` 的每地區 3 隻、精英數、meet 0 覆蓋子地區等規則改為只看 `MissionId is null` 的怪，總數改為 65＋3，任務怪也要通過 `Roster_StatsWithinFormulaBands`。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*JsonGameContentTests" --filter-class "*MissionTableTests" --filter-class "*MonsterRosterTests"` → 編譯失敗或斷言失敗。
- [ ] **Step 4: 實作** `MissionDefinition` 等 record、`MonsterDefinition.MissionId`、`JsonGameContent` 的 `missions.json` 讀取與上列驗證（任務在怪物、道具、技能之後驗證，任務怪的 `missionId` 在任務載入後檢查）、`IGameContent.AllMissions`／`GetMission`；寫 `missions.json`、3 隻任務怪、測試內容；`docs/research/mission-table.md` 列兩張總表、任務怪數值，並加一節「與研究報告的差異」（擊殺只計勝場、PHP 同怪覆寫、任務怪、NPC 計數 `m_npc`、獎勵不檢查背包、技能已會即報錯、經驗不升級、過期每日紀錄連進行中一起刪、目標與報酬分開擲、WOG 進度顯示剩餘數）；`monster-roster.md` 補任務怪一節。
- [ ] **Step 5: 跑測試確認通過**：同 Step 3 → PASS；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit**

```bash
git add backend/content backend/EndlessBattle.Domain backend/EndlessBattle.Application backend/EndlessBattle.Infrastructure backend/EndlessBattle.Tests docs/research
git commit -m "✨ feat: load the mission table and mission-only monsters"
```

---

### Task 2: 任務資料表與 repository

**Files:**
- Create: `backend/db/ddl/V000000008__add_missions.sql`、`Domain/Missions/MissionRecord.cs`、`Application/Missions/IMissionRepository.cs`、`Infrastructure/Persistence/MissionRepository.cs`、`Tests/Support/InMemoryMissionRepository.cs`
- Modify: scaffold 產物（`./scripts/scaffold-db.sh`）、`Infrastructure/DependencyInjection.cs`
- Test: `Tests/Infrastructure/MissionRepositoryTests.cs`、`Tests/Infrastructure/DatabaseSchemaTests.cs`

```sql
CREATE TABLE character_missions (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    character_id BIGINT      NOT NULL,
    mission_id   VARCHAR(32) NOT NULL,
    state        VARCHAR(16) NOT NULL,             -- MissionState 名稱：Active、Completed
    progress     INT         NOT NULL DEFAULT 0,   -- 擊殺目標已計入的數量；只有交付的任務為 0
    daily_period DATE        NULL,                 -- 每日任務接取時的週期（台北 09:00 起算的日期）；一般任務 NULL
    accepted_at  DATETIME(6) NOT NULL,
    completed_at DATETIME(6) NULL,
    PRIMARY KEY (id),
    UNIQUE KEY ux_character_missions_character_mission (character_id, mission_id),
    CONSTRAINT fk_character_missions_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
```

**Interfaces:**
- Produces:

```csharp
namespace EndlessBattle.Domain.Missions;
public enum MissionState { Active, Completed }
public sealed record MissionRecord(string MissionId, MissionState State, int Progress, DateOnly? DailyPeriod, DateTimeOffset AcceptedAt, DateTimeOffset? CompletedAt);

namespace EndlessBattle.Application.Missions;
// 全部以 characterId 為範圍；時間以 UTC 存取
public interface IMissionRepository
{
    Task<IReadOnlyList<MissionRecord>> ListAsync(long characterId, CancellationToken cancellationToken);
    Task AddAsync(long characterId, MissionRecord record, CancellationToken cancellationToken);              // 同角色同任務已有一列 → ConflictException
    Task UpdateProgressAsync(long characterId, string missionId, int progress, CancellationToken cancellationToken);
    Task CompleteAsync(long characterId, string missionId, DateTimeOffset completedAt, CancellationToken cancellationToken);
    Task RemoveAsync(long characterId, string missionId, CancellationToken cancellationToken);
}
// Tests/Support/InMemoryMissionRepository(List<string>? log = null) : IMissionRepository, IRestorableRepository；寫入時記 "mission:add|progress|complete|remove:{missionId}"
```

- [ ] **Step 1: 寫失敗測試**：`Schema_HasMissionTable`（欄位與型別、`daily_period` 為 DATE、唯一鍵 `(character_id, mission_id)`）、`Missions_RoundTrip`（Active 每日紀錄的 `DailyPeriod`、`AcceptedAt` 微秒精度回讀一致）、`Missions_RejectDuplicate`（第二次 `AddAsync` 同任務 → `ConflictException`）、`Missions_UpdateAndCompleteScopedToCharacter`（另一角色同 id 的列不受影響）、`Missions_RemoveThenAddAgain`。
- [ ] **Step 2: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*MissionRepositoryTests" --filter-class "*DatabaseSchemaTests"` → FAIL。
- [ ] **Step 3: 實作**：新增 migration → `docker compose up -d` → `./scripts/scaffold-db.sh`；`MissionRepository`（重複鍵用 `MySqlErrors` 既有的判斷轉成 `ConflictException`）；DI 註冊；`InMemoryMissionRepository`。
- [ ] **Step 4: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 5: Commit** `🚧 chore: add the character missions table`

---

### Task 3: 接取規則、每日週期與任務板

**Files:**
- Create: `Domain/Missions/{MissionRules, DailyMissions, MissionBoard}.cs`
- Test: `Tests/Domain/Missions/{MissionRulesTests, DailyMissionsTests, MissionBoardTests}.cs`

**Interfaces:**
- Consumes: Task 1 的 `MissionDefinition` 系列、Task 2 的 `MissionRecord`、`Character`、`SeededRandom`
- Produces:

```csharp
// 擲骰後的實際目標與報酬；一般任務就是定義本身
public sealed record MissionObjective(string MissionId, KillTarget? Kill, IReadOnlyList<ItemQuantity> Deliver, MissionReward Reward, long Exp);

public static class MissionRules
{
    public const int MaxActive = 3;
    // records：已去掉過期每日紀錄與未知任務。代碼依序：missionUnknown（單獨回傳）、alreadyActive、alreadyCompleted、tooMany、
    // levelTooLow、jobMismatch、sexMismatch、birthMismatch、prerequisiteMissing、missionConflict
    public static IReadOnlyList<string> CanAccept(Character character, MissionDefinition? mission, IReadOnlyList<MissionRecord> records);
}

public static class DailyMissions
{
    public static readonly TimeSpan ResetZoneOffset = TimeSpan.FromHours(8);   // Asia/Taipei，無夏令時間
    public static readonly TimeSpan ResetTimeOfDay = TimeSpan.FromHours(9);
    public static DateOnly PeriodOf(DateTimeOffset now);
    public static DateTimeOffset PeriodStart(DateOnly period);                  // 該日台北 09:00 的 UTC 時刻
    public static DateTimeOffset NextReset(DateTimeOffset now);                 // PeriodStart(PeriodOf(now) + 1 天)
    public static bool IsExpired(MissionRecord record, DateTimeOffset now);     // 只有 DailyPeriod 非 null 且 ≠ PeriodOf(now)
    public static int Seed(long characterId, string missionId, DateOnly period); // FNV-1a，見 Global Constraints
    public static MissionObjective Resolve(MissionDefinition mission, long characterId, DateOnly? period); // 一般任務忽略 period
}

public sealed record MissionEntry(MissionDefinition Definition, MissionObjective Objective, MissionRecord? Record, bool Ready, DateTimeOffset? ExpiresAt);
public sealed record MissionBoardView(IReadOnlyList<MissionEntry> Available, IReadOnlyList<MissionEntry> Active, IReadOnlyList<MissionEntry> Completed, DateTimeOffset NextDailyReset);
public static class MissionBoard
{
    // owned：背包中各道具的總數（算 Ready 用）；records 為原始紀錄，這裡負責去掉過期與未知
    public static MissionBoardView Build(Character character, IReadOnlyList<MissionDefinition> missions, IReadOnlyList<MissionRecord> records,
        IReadOnlyDictionary<string, int> owned, DateTimeOffset now);
    public static IReadOnlyList<MissionRecord> Current(IReadOnlyList<MissionRecord> records, IReadOnlyList<MissionDefinition> missions, DateTimeOffset now);
}
```

規則：Available ＝ `CanAccept` 為空或只有 `tooMany` 的任務，每日任務在前（內容順序），一般任務依 `Level` 升冪、同級依內容順序；每日任務的 `Objective` 用現在週期擲、`ExpiresAt = NextReset(now)`。Active 依 `AcceptedAt`，每日任務用紀錄的週期擲、`ExpiresAt = PeriodStart(週期 + 1)`；`Ready = (Kill 為 null 或 Progress ≥ Kill.Count) 且 每項 Deliver 的 owned ≥ Quantity`。Completed 依 `CompletedAt` 新→舊（含本週期完成的每日任務）。

- [ ] **Step 1: 寫失敗測試**（定義在測試裡直接建構，角色用 `TestCharacters.Default`）：
  - `CanAccept_AllowsWhenEveryRequirementMet` → `[]`；`CanAccept_UnknownMission`（null）→ `["missionUnknown"]`。
  - `CanAccept_RejectsFourthActive`：3 筆 Active → `["tooMany"]`；3 筆 Completed、0 Active → `[]`。
  - `CanAccept_LevelIsTheMissionLevel`：Lv14 接 Lv15 → `["levelTooLow"]`；Lv15 → `[]`。
  - `CanAccept_ChecksJobSexBirth`（Theory）：`jobId "sorcerer"` → `jobMismatch`；`sex 2` → `sexMismatch`；`birth 2` → `birthMismatch`。
  - `CanAccept_PrerequisiteMustBeCompleted`：前置 Active → `["prerequisiteMissing"]`；Completed → `[]`。
  - `CanAccept_ConflictBlocksAnyState`：互斥任務 Active 或 Completed 都 → `["missionConflict"]`。
  - `CanAccept_ReportsActiveAndCompletedSeparately` → `alreadyActive`／`alreadyCompleted`；`CanAccept_ListsEveryReasonInOrder`（Lv1、法師限定、性別 2、前置未完成 → `["levelTooLow","jobMismatch","sexMismatch","prerequisiteMissing"]`）。
  - `Period_SwitchesAtNineTaipei`：`PeriodOf(2026-10-22T00:59:59Z) == 2026-10-21`、`PeriodOf(2026-10-22T01:00:00Z) == 2026-10-22`、`NextReset(2026-10-22T00:59:59Z) == 2026-10-22T01:00:00Z`、`PeriodStart(2026-10-22) == 2026-10-22T01:00:00Z`。
  - `IsExpired_OnlyForPastPeriods`：週期 2026-10-21 的紀錄在 `2026-10-22T00:30Z` 未過期、`01:00Z` 過期；`DailyPeriod` null 永不過期。
  - `Seed_IsFnv1a`：`Seed(1, "test-daily", 2026-10-22) == -1404635945`、`Seed(42, "daily-hunt-novice", 2026-10-22) == 1996040880`。
  - `Resolve_IsDeterministic`：同參數兩次結果 `Assert.Equivalent`；2026-10-01 起 30 個週期中目標至少 2 種；一般任務回傳定義的 `Kill`／`Deliver`／`Rewards`、`Exp == Level × 190`。
  - `Board_ListsTooManyAsAvailable`（3 筆 Active 時其他可接的任務仍在 Available）、`Board_HidesExpiredDailyAndUnknownMissions`、`Board_DailyFirstThenByLevel`、`Board_ReadyNeedsKillsAndItems`（擊殺 3/3 但 `owned` 不足交付 → `Ready false`）。
- [ ] **Step 2: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*MissionRulesTests" --filter-class "*DailyMissionsTests" --filter-class "*MissionBoardTests"` → 編譯失敗。
- [ ] **Step 3: 實作** 三個靜態類別（純 Domain，無 BCL 以外依賴；FNV 用 `Encoding.UTF8.GetBytes`）。
- [ ] **Step 4: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 5: Commit** `✨ feat: decide mission eligibility and the daily mission roll`

---

### Task 4: 任務服務：列表、接取、放棄與 API

**Files:**
- Create: `Application/Missions/MissionService.cs`、`Api/Endpoints/MissionEndpoints.cs`
- Modify: `Application/DependencyInjection.cs`、`Api/Endpoints/Contracts.cs`、`Api/Program.cs`
- Test: `Tests/Application/MissionServiceTests.cs`、`Tests/Api/MissionEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 2 `IMissionRepository`、Task 3 `MissionRules`／`DailyMissions`／`MissionBoard`、`BagReader`、`ICharacterRepository`、`IClock`、`IUnitOfWork`
- Produces:

```csharp
// Task 7 再加 ISkillBookRepository skillBook、IRandomSourceFactory randomFactory（未使用的主建構子參數會觸發警告，不要先加）
public sealed class MissionService(ICharacterRepository characters, IMissionRepository missions, IInventoryRepository inventory,
    IEquipmentRepository equipment, IGameContent content, IClock clock, IUnitOfWork unitOfWork)
{
    public Task<MissionBoardView> GetBoardAsync(long accountId, CancellationToken cancellationToken);      // 只讀，不刪過期
    public Task<MissionBoardView> AcceptAsync(long accountId, string missionId, CancellationToken cancellationToken);
    public Task<MissionBoardView> AbandonAsync(long accountId, string missionId, CancellationToken cancellationToken);
}
// API（需登入）：GET /api/missions → MissionBoardResponse；POST /api/missions/accept { missionId }、POST /api/missions/abandon { missionId } → MissionBoardResponse
public sealed record MissionRequest(string? MissionId);
public sealed record MissionBoardResponse(IReadOnlyList<MissionResponse> Available, IReadOnlyList<MissionResponse> Active, IReadOnlyList<MissionResponse> Completed, int ActiveLimit, DateTimeOffset NextDailyReset);
public sealed record MissionResponse(string Id, string Giver, string Title, string Description, int Level, int? PlaceId, string? PlaceName, bool Daily,
    MissionKillResponse? Kill, IReadOnlyList<MissionDeliverResponse> Deliver, MissionRequirementsResponse Requirements, MissionRewardResponse Rewards,
    bool Ready, DateTimeOffset? ExpiresAt);
public sealed record MissionKillResponse(string MonsterId, string MonsterName, int PlaceId, string PlaceName, int Count, int Progress);
public sealed record MissionDeliverResponse(string ItemId, string Name, int Quantity, int Owned);
public sealed record MissionRequirementsResponse(string? JobId, int? Sex, int? Birth, string? PrerequisiteId, string? PrerequisiteTitle);
public sealed record MissionRewardResponse(long Exp, long Money, IReadOnlyList<MissionRewardItemResponse> Items, MissionRewardSkillResponse? Skill);
public sealed record MissionRewardItemResponse(string ItemId, string Name, int Quantity);
public sealed record MissionRewardSkillResponse(string SkillId, string Name, int Level);
```

交易寫入順序（每個寫入型用例一筆 `unitOfWork.RunAsync`）：讀角色與紀錄 → `MissionBoard.Current` 去掉過期與未知 → 驗證 → **`characters.UpdateAsync(character)`（不改內容，只走版本鎖）** → 刪除過期每日紀錄 → 寫任務表。接取：`CanAccept` 非空 → `InputValidationException("無法接取任務", reasons)`；新紀錄 `Progress 0`、`DailyPeriod = Daily ? PeriodOf(now) : null`、`AcceptedAt = now`。放棄：不是 Active → reasons `["missionNotActive"]`。看板的 `owned` 是 `BagReader.Bag` 依 `ItemId` 加總的數量（Task 7 把它搬成 `Delivery.Owned`，屬結構性變更）。

- [ ] **Step 1: 寫失敗測試**（`MissionServiceTests`，TestContent、`FakeClock` 起點 `2026-10-22T02:00:00Z`、共用 log）：
  - `Accept_WritesCharacterVersionBeforeMission`：log 依序含 character update、`mission:add:test-hunt`；紀錄 Active、Progress 0、DailyPeriod null。
  - `Accept_RejectsWithReasons`：Lv1 接 `test-skill-reward` → reasons `["levelTooLow","prerequisiteMissing"]`，任務表與角色版本不變。
  - `Accept_DailyStoresTodaysPeriod` → `DailyPeriod == 2026-10-22`。
  - `Accept_ReplacesExpiredDailyRecord`：預置 `test-daily` Completed、週期 2026-10-21 → 接取成功，只剩一列、週期 2026-10-22。
  - `Abandon_RemovesActiveAndAllowsReaccept`：Progress 2 的 `test-hunt` 放棄後再接 → Progress 0。
  - `Abandon_RejectsCompleted` → `["missionNotActive"]`。
  - `Board_IgnoresMissionsMissingFromContent`：3 筆 Active 中 1 筆 id 不在內容 → 仍可再接 1 個。
  - `Accept_ConflictsWhenCharacterVersionIsStale`：角色版本先被改 → `ConflictException`，任務表不變。
- [ ] **Step 2: 寫失敗測試**（`MissionEndpointsTests`）：`Missions_GetBoard`（新角色看到 Available 含 `test-hunt`、`test-daily`，`ActiveLimit == 3`、`NextDailyReset` 為下一個 01:00Z）、`Missions_AcceptThenAbandon`（回傳的 board 狀態正確）、`Missions_FourthAcceptReturnsTooMany`（problem+json 400、`reasons == ["tooMany"]`）、`Missions_RequireLogin`（401）。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*MissionServiceTests" --filter-class "*MissionEndpointsTests"` → FAIL。
- [ ] **Step 4: 實作** `MissionService` 三個方法、端點與 DTO 映射（名稱由 `IGameContent` 解析；技能名稱用 `GetSkill`）、DI；`MissionService` 的 `CompleteAsync` 留到 Task 7。
- [ ] **Step 5: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit** `✨ feat: list, accept and abandon missions`

---

### Task 5: 掛機戰鬥中的擊殺計數

**Files:**
- Create: `Domain/Missions/{ActiveMission, KillCounter}.cs`
- Modify: `Domain/World/MonsterPool.cs`、`Domain/Adventure/{AdventureSimulator, AdventureRun}.cs`、`Domain/Battle/BattleEvents.cs`、`Application/Adventure/AdventureService.cs`、`Api/Endpoints/Contracts.cs`（`AdvanceResponse` 不變，事件自動序列化）
- Test: `Tests/Domain/Missions/KillCounterTests.cs`、`Tests/Domain/World/MonsterPoolTests.cs`、`Tests/Domain/Adventure/AdventureSimulatorTests.cs`、`Tests/Application/AdventureServiceTests.cs`

**Interfaces:**
- Consumes: Task 3 `DailyMissions.Resolve`／`PeriodStart`、Task 2 `IMissionRepository.ListAsync`／`UpdateProgressAsync`
- Produces:

```csharp
// 只放有擊殺目標、尚未達成的進行中任務；ExpiresAt 是每日任務週期結束的時刻
public sealed record ActiveMission(string MissionId, string Title, string MonsterId, int Required, int Progress, DateTimeOffset? ExpiresAt);
public sealed record KillResult(IReadOnlyList<ActiveMission> Missions, IReadOnlyList<ActiveMission> Advanced);
public static class KillCounter
{
    // killedAt 嚴格早於 ExpiresAt 才計入；Advanced 是這次 +1 的任務（新狀態）
    public static KillResult Apply(IReadOnlyList<ActiveMission> missions, string monsterId, DateTimeOffset killedAt);
    public static IReadOnlySet<string> SpawnableMissionIds(IReadOnlyList<ActiveMission> missions, DateTimeOffset at);   // 未達成且未過期
}
// MonsterPool.Candidates／Pick 加參數 IReadOnlySet<string>? activeMissionIds = null：MissionId 非 null 且不在集合內的怪一律排除（含退回分支）
// AdventureSimulator.Run 加最後一個參數 IReadOnlyList<ActiveMission>? missions = null
public sealed record MissionProgressEvent(double AtSeconds, string MissionId, string Title, string MonsterId, int Progress, int Required) : BattleEvent(AtSeconds);  // [JsonDerivedType(..., "mission")]
// AdvanceSummary(..., int PotionsUsed = 0, int MissionKills = 0)；AdventureRun(..., int? SupplyRemaining = null, IReadOnlyList<ActiveMission>? Missions = null)（null 視為空清單）
```

模擬器：每場遇敵前用 `SpawnableMissionIds(missions, from + cursor)` 過濾任務怪；勝利時在 `RewardEvent` 之後、升級之前，以 `killedAt = from + fightEnd` 呼叫 `KillCounter.Apply`，每個 Advanced 輸出 `MissionProgressEvent(fightEnd, …)`（只在窗口內），`MissionKills += Advanced.Count`（不論窗口）。`AdventureService`（建構子加 `IMissionRepository`；測試的 `InMemoryUnitOfWork` 一併登記 `InMemoryMissionRepository`）：模擬前 `missions.ListAsync`，只取內容中存在、`Active`、`Objective.Kill` 非 null 且 `Progress < Count` 者建成 `ActiveMission`（**不**以 now 過濾每日任務，交給 `ExpiresAt`）（每日：`ExpiresAt = PeriodStart(DailyPeriod + 1 天)`）；交易內在角色更新與熟練度之後、掉落之前，對 `run.Missions` 中進度有變的逐筆 `UpdateProgressAsync`。

- [ ] **Step 1: 寫失敗測試**：
  - `KillCounter_CountsEveryMatchingMission`：`test-hunt` 0/3 與 `test-sprite-bounty` 2/2（已達成）、`test-wolf` 任務 0/2 → 擊殺 `test-sprite` → `test-hunt` 1/3 在 Advanced，`test-sprite-bounty` 維持 2/2 不在 Advanced，狼任務不變。
  - `KillCounter_IgnoresKillsAtOrAfterExpiry`：`ExpiresAt == killedAt` → 不計。
  - `Pool_ExcludesMissionMonstersUnlessActive`：roll 100 時地區 1 子地區 1 的候選，集合為空 → 不含 `test-sprite-chief`；集合含 `test-chief-hunt` → 含；退回分支同樣排除。
  - `Adventure_CountsMissionKillsAndEmitsProgress`：`test-hunt` 0/3、20 秒（8 場全勝）→ `run.Missions` 中 Progress 3；事件 `MissionProgressEvent` 的 Progress 依序 `[1,2,3]`、Required 3，各自緊接在同一 `AtSeconds` 的 `RewardEvent` 之後；`Summary.MissionKills == 3`。
  - `Adventure_CountsMissionKillsOnlyOnWins`：沿用 `Adventure_DropsNothingOnLossesAndTimeouts` 的敗北與超限設定 → Progress 不變、沒有 mission 事件、`MissionKills == 0`。
  - `Adventure_StopsCountingDailyAfterReset`：`from = 2026-10-22T00:59:50Z`、20 秒、每日任務 0/10 且 `ExpiresAt = 01:00:00Z` → 場次結束於 50.8、53.6、56.4、59.2 秒計入，第 5 場（結束於 01:00:02）不計 → Progress 4、`MissionKills == 4`。
  - `Adventure_MissionMonsterOnlyWhileActive`：`SeededRandom(7)`、120 秒；沒帶任務時遭遇事件不含 `test-sprite-chief`；帶 `test-chief-hunt` 0/1 時，`test-sprite-chief` 只出現在第一個 `test-chief-hunt` 進度事件之前（含該場），之後的遭遇都不是牠。
  - `Advance_PersistsMissionProgress`：預置 `test-hunt` Active 0/3 → 推進 20 秒後紀錄 Progress 3；log 中 `mission:progress:test-hunt` 在 character update 與熟練度之後。
  - `Advance_WritesNoMissionProgress_WhenCharacterUpdateConflicts`：照 `Advance_WritesNoDrops_WhenCharacterUpdateConflicts` 的作法，在推進讀取之後刪掉 `test-hunt` 紀錄並改動角色版本（模擬同時放棄）→ `ConflictException`，任務表仍沒有 `test-hunt`、log 沒有任何 `mission:` 寫入。
  - `Advance_IgnoresCompletedDeliveryAndUnknownMissions`：Completed 的 `test-hunt`、Active 的 `test-delivery`、未知 id → 都沒有進度寫入。
- [ ] **Step 2: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*KillCounterTests" --filter-class "*MonsterPoolTests" --filter-class "*AdventureSimulatorTests" --filter-class "*AdventureServiceTests"` → FAIL。
- [ ] **Step 3: 實作** 上列型別與修改；既有的模擬器、遇敵測試不改期望值（沒有任務怪時擲骰序列不變）。
- [ ] **Step 4: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 5: Commit** `✨ feat: count mission kills while adventuring`

---

### Task 6: 前端：戰報與摘要的任務進度

**Files:**
- Modify: `frontend/src/api/schema.ts`（`pnpm generate:api` 產生）、`src/game/{types, battleLog, battlePlayback, stageModel}.ts`
- Test: `src/game/{battleLog, battlePlayback, stageModel}.test.ts`

**Interfaces:**
- Consumes: Task 5 的 `"mission"` 事件與 `summary.missionKills`
- Produces: `types.ts` 的 `MissionProgressEvent = Required<Schemas["BattleEventMissionProgressEvent"]>` 併入 `BattleEvent` 聯集；`PlaybackItem` 的 summary 加 `missionKills: number`。

**文案：** `mission` 事件 → `任務進度：{怪物名} {progress}/{required}`（kind `drop`）；`progress === required` 時再加一行 `任務「{title}」的目標已達成，回任務頁回報。`（kind `trigger`）。摘要在升級句之前加 `，任務進度 +{missionKills}`（只在 > 0 時）。舞台：達成時 `[{ type: "plate", text: "任務達成", tone: "gold" }]`，未達成的進度事件不加效果。

- [ ] **Step 1: 寫失敗測試**：`battleLog` — `describes mission progress with the monster name`（`任務進度：測試雪精 2/3`）、`adds a completion line when the target is met`（兩行、第二行 kind `trigger`）、`summary mentions mission progress`（`…金錢 0，任務進度 +3，升了 1 級。` 依現有句型）；`battlePlayback` — summary 帶 `missionKills`；`stageModel` — 達成事件產生金色「任務達成」名牌、`applyToStage` 對 mission 事件不改狀態。
- [ ] **Step 2: 跑測試確認失敗**：`cd frontend && pnpm vitest run src/game` → FAIL。
- [ ] **Step 3: 實作**：API 執行中 `pnpm generate:api`；補 `types.ts` 聯集與三個 switch 的 `case "mission"`。
- [ ] **Step 4: 跑測試確認通過**：`pnpm check` 通過；`./scripts/check.sh` exit 0。
- [ ] **Step 5: Commit** `✨ feat: narrate mission progress in the battle log`

---

### Task 7: 交付、獎勵與完成

**Files:**
- Create: `Domain/Missions/{Delivery, MissionRewards}.cs`
- Modify: `Application/Missions/MissionService.cs`（`CompleteAsync`）、`Api/Endpoints/{MissionEndpoints, Contracts}.cs`
- Test: `Tests/Domain/Missions/{DeliveryTests, MissionRewardsTests}.cs`、`Tests/Application/MissionServiceTests.cs`、`Tests/Api/MissionEndpointsTests.cs`

**Interfaces:**
- Consumes: `InventoryRules.TryAdd`、`LevelUp.Apply`、`BagReader`、`ISkillBookRepository.UpsertAsync`、Task 3 的 `MissionObjective`
- Produces:

```csharp
public sealed record DeliveryPlan(IReadOnlyList<InventoryEntry> Updated, IReadOnlyList<long> Removed, IReadOnlyList<string> Missing);
public static class Delivery
{
    public static IReadOnlyDictionary<string, int> Owned(IReadOnlyList<InventoryEntry> bag);
    public static DeliveryPlan Plan(IReadOnlyList<InventoryEntry> bag, IReadOnlyList<ItemQuantity> deliver);   // 取用順序見 Global Constraints
    public static IReadOnlyList<InventoryEntry> After(IReadOnlyList<InventoryEntry> bag, DeliveryPlan plan);
}
public sealed record MissionRewardOutcome(Character Character, int LevelsGained, IReadOnlyList<InventoryEntry> NewEntries,
    IReadOnlyList<InventoryEntry> UpdatedEntries, LearnedSkill? GrantedSkill, IReadOnlyList<string> Reasons);   // Reasons 非空（["bagFull"]）時其餘欄位不可用
public static class MissionRewards
{
    public static MissionRewardOutcome Apply(Character character, JobDefinition job, MissionObjective objective,
        IReadOnlyList<InventoryEntry> bagAfterDelivery, Func<string, ItemDefinition> items, IReadOnlyList<LearnedSkill> learned, IRandomSource random);
}
// MissionService.CompleteAsync(long accountId, string missionId, CancellationToken) → MissionCompletion
public sealed record MissionCompletion(MissionBoardView Board, string CompletionMessage, long Exp, long Money, IReadOnlyList<ItemQuantity> Items, SkillReward? Skill, bool SkillGranted, int LevelsGained);
// API：POST /api/missions/complete { missionId } → MissionCompleteResponse（角色照 BankEndpoints.ProfileAsync 的作法另外讀）
public sealed record MissionCompleteResponse(MissionBoardResponse Board, CharacterResponse Character, string CompletionMessage, long Exp, long Money,
    IReadOnlyList<MissionRewardItemResponse> Items, MissionRewardSkillResponse? Skill, bool SkillGranted, int LevelsGained);
```

`MissionService` 建構子加 `ISkillBookRepository skillBook`、`IRandomSourceFactory randomFactory`。`CompleteAsync` 一筆交易：讀角色、紀錄（`MissionBoard.Current`）→ 不是 Active → `missionNotActive` → `Objective`（每日用紀錄的週期）→ 擊殺未達 → `objectiveIncomplete` → `BagReader` → `Delivery.Plan` 有 Missing → `itemsMissing` → `MissionRewards.Apply`（背包用 `Delivery.After`）有 Reasons → `bagFull`（以上皆 `InputValidationException("無法完成任務", reasons)`，尚未寫入任何東西）→ **`characters.UpdateAsync(outcome.Character)`** → 刪過期每日紀錄 → 交付的 `UpdateQuantityAsync`／`RemoveAsync` → 獎勵的 `UpdateQuantityAsync`／`AddAsync` → `skillBook.UpsertAsync`（有 GrantedSkill 時）→ `missions.CompleteAsync(…, now)`。

- [ ] **Step 1: 寫失敗測試**（Domain）：
  - `Plan_TakesSmallerStacksFirst`：`test-ore` 兩列各 2（id 1、2）交 3 → Removed `[1]`、Updated 中 id 2 剩 1。
  - `Plan_PrefersPlainEquipment`：+3 精鍊的 `test-sword`（id 5）與未強化的（id 9）交 1 → Removed `[9]`。
  - `Plan_ReportsMissing`：只有 2 個 `test-ore` 交 3 → Missing `["test-ore"]`。
  - `Apply_GrantsLevelTimes190AndMoney`：Lv15 任務、金錢 750 → `Exp +2850`、`Money +750`。
  - `Apply_LevelsUpImmediately`：Exp 900 的 Lv1 角色完成 Lv1 任務 → 1090 > 1000 → Lv2、`LevelsGained 1`、剩 90。
  - `Apply_RejectsWhenRewardItemsDoNotFit` → `Reasons == ["bagFull"]`。
  - `Apply_SkipsKnownSkill`：已會 `iron-skin` Lv2 → `GrantedSkill null`、金錢經驗照給。
- [ ] **Step 2: 寫失敗測試**（Application／API）：
  - `Complete_KillMissionGrantsRewardsOnce`：`test-hunt` 3/3 → 紀錄 Completed、`CompletedAt == now`、金錢 +50、經驗 +190；再完成一次 → `["missionNotActive"]`。
  - `Complete_RejectsIncompleteKills` → `["objectiveIncomplete"]`；`Complete_RejectsMissingItems` → `["itemsMissing"]`。
  - `Complete_DeliveryFreesSlotForReward`：材料分類 15 格（14 列 `stone-remover` ×99＋`test-ore` ×3 一列；不放 `safe-refine-stone`，免得獎勵直接疊上去）→ 完成 `test-delivery` 成功，`test-ore` 列消失、`safe-refine-stone` ×1 入袋。
  - `Complete_BagFullRollsBackEverything`：同上但 `test-ore` ×5（交付後剩 2、格子沒空出）→ `["bagFull"]`；`test-ore` 仍 5、金錢與經驗與角色版本不變、紀錄仍 Active。
  - `Complete_SkillRewardLearnsLevelOne`：`test-skill-reward` → 技能書有 `iron-skin` Lv1、`SkillGranted true`。
  - `Complete_DailyReacceptOnlyAfterReset`：`2026-10-22T02:00Z` 接 `test-daily`、把進度設滿、完成 → 同一天再接 → `["alreadyCompleted"]`；時鐘調到 `2026-10-23T01:00Z` → 接取成功、Progress 0、`DailyPeriod 2026-10-23`、表中只剩一列。
  - `Complete_WritesCharacterVersionFirst`：log 中 character update 早於所有 inventory／skill／mission 寫入。
  - API：`Missions_CompleteReturnsRewardsAndCharacter`（`Character.Money` 已含獎勵、`Exp 190`、`CompletionMessage` 為內容文字）、`Missions_CompleteIncompleteReturnsReason`（400、`["objectiveIncomplete"]`）。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*DeliveryTests" --filter-class "*MissionRewardsTests" --filter-class "*MissionServiceTests" --filter-class "*MissionEndpointsTests"` → FAIL。
- [ ] **Step 4: 實作** `Delivery`、`MissionRewards`、`CompleteAsync`、端點。
- [ ] **Step 5: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit** `✨ feat: deliver items and claim mission rewards`

---

### Task 8: 前端：任務畫面與分頁

**Files:**
- Create: `frontend/src/game/missions.ts`（+test）、`src/ui/MissionsScreen.tsx`、`MissionsScreen.css`、`MissionsScreen.test.tsx`
- Modify: `src/api/schema.ts`（重新產生）、`src/api/client.ts`（+test）、`src/game/types.ts`、`src/ui/AdventureScreen.tsx`（+test）
- 截圖：`docs/superpowers/screenshots/plan-8-missions.png`

**Interfaces:**
- Consumes: Task 4、7 的 API
- Produces:

```ts
// client.ts
getMissions: () => Promise<MissionBoard>                        // GET /api/missions
acceptMission: (missionId: string) => Promise<MissionBoard>     // POST /api/missions/accept
abandonMission: (missionId: string) => Promise<MissionBoard>    // POST /api/missions/abandon
completeMission: (missionId: string) => Promise<MissionCompletion> // POST /api/missions/complete
// game/missions.ts（純函式）
export const MaxActiveMissions = 3
export const requirementLines: (mission: MissionView, content: ContentNames) => string[]
export const objectiveLines: (mission: MissionView) => string[]
export const rewardLines: (rewards: MissionRewardView) => string[]
export const progressRatio: (mission: MissionView) => number          // 擊殺 progress/count；只有交付時為 Σmin(owned,qty)/Σqty
export const actionLabel: (mission: MissionView) => "交付" | "完成"
export const resetHoursText: (nextReset: string, now: Date) => string
export const missionErrorMessage: (reasons: string[], fallback: string) => string
export const completionLines: (result: MissionCompletion) => string[]
```

**文案：**
- 條件：`Lv {level}`、`限 {職業名}`、`限 {性別名}性`、`限 {出生地名}出身`、`需先完成「{前置標題}」`。
- 目標：擊殺 `擊敗 {怪物名}（{地名}） {progress}/{count}`；交付 `交付 {道具名} {owned}/{quantity}`。
- 報酬：`經驗 {exp}`、`金錢 {money}`、`{道具名} ×{n}`、`技能 {技能名} Lv{level}`（數字用 `formatNumber`）。
- 卡片：標題、`委託者：{giver}`、說明、`委託內容`（目標）、`委託報酬`（報酬）、條件行；每日任務另顯示 `※ 每日任務將在 {n} 小時後重置`（n = 無條件進位小時，同 WOG 的「剩餘時間 N 小時」）。
- 可接頁：區塊「每日任務」在前、「一般任務」在後；按鈕「接取」，進行中已達 3 個時停用並顯示 `進行中的任務已達 3 個`；空清單 `目前無任務`（WOG 原文）。
- 進行中頁：進度條（`progressRatio`）、按鈕 `actionLabel`（`Ready` 為 false 時停用）與「放棄」→「確定放棄」／「取消」；空清單 `沒有進行中的任務`。
- 已完成頁：`{title}`、`委託者：{giver}`、`Lv {level}`；空清單 `還沒有完成的任務`。
- 完成後訊息框：委託者的 `completionMessage`，下一行 `獲得經驗 {exp}、金錢 {money}`，每件道具 `獲得 {名稱} ×{n}`，技能 `學會了{技能名} Lv{level}`（`SkillGranted false` 時 `已經會{技能名}，略過技能獎勵`），升級 `升了 {n} 級`。
- 錯誤代碼：`missionUnknown` 沒有這個任務、`alreadyActive` 已經接過此任務、`alreadyCompleted` 這個任務已經完成、`tooMany` 同時最多進行 3 個任務、`levelTooLow` 等級不足、`jobMismatch` 職業不符、`sexMismatch` 性別不符、`birthMismatch` 出生地不符、`prerequisiteMissing` 需先完成前置任務、`missionConflict` 已經選擇了另一條路線、`missionNotActive` 沒有進行中的這個任務、`objectiveIncomplete` 尚未完成任務、`itemsMissing` 交付的物品不足、`bagFull` 背包已滿；未知代碼用 `ApiError.detail`。

- [ ] **Step 1: 寫失敗測試**：`missions.test.ts`（各 `*Lines` 的文案、`progressRatio` 擊殺與交付、`actionLabel`、`resetHoursText("2026-10-23T01:00:00Z", 2026-10-22T22:30Z) === "※ 每日任務將在 3 小時後重置"`、錯誤對照與 fallback）；`client.test.ts`（四個方法的路徑、方法、body `{ missionId }`）；`MissionsScreen.test.tsx`（三個子頁；可接頁「每日任務」區塊在「一般任務」之前；按「接取」呼叫 `acceptMission("test-hunt")` 並以回傳 board 重繪；3 個進行中時「接取」停用；進行中顯示 `擊敗 草原史萊姆（中央平原） 3/8` 且「完成」停用；「放棄」→「確定放棄」才呼叫 `abandonMission`；交付任務按鈕為「交付」；完成後顯示完成訊息與獎勵行並呼叫 `onCharacterChange`；`tooMany` 顯示 `同時最多進行 3 個任務`）；`AdventureScreen.test.tsx`（底部第 5 個分頁「任務」，點下呼叫 `getMissions`）。
- [ ] **Step 2: 跑測試確認失敗**：`cd frontend && pnpm vitest run` → FAIL。
- [ ] **Step 3: 實作**：`pnpm generate:api`；`types.ts` 加 `MissionBoard`、`MissionView`、`MissionRewardView`、`MissionCompletion`；`missions.ts`；`MissionsScreen`（重用 `bagWidgets` 的 `Tabs`；每次打開分頁才載入，同背包分頁；顏色只用 `theme.css` 變數，進度條用 `--gold`）；`AdventureScreen` 加分頁並傳 `content`。
- [ ] **Step 4: 跑測試確認通過**：`pnpm check` 通過；`./scripts/check.sh` exit 0。
- [ ] **Step 5: 手動驗收（控制者）**：375×667 直式：新角色打開「任務」看到「綠地上的黏液」與「每日討伐：新手」；接取後回冒險，戰報出現 `任務進度：草原史萊姆 1/8`…`8/8` 與達成行、舞台出現「任務達成」；回「進行中」按「完成」看到完成訊息與 `獲得經驗 190、金錢 50`、`獲得 小補給藥劑 ×5`，頂欄金錢同步、背包有 5 瓶；連接 3 個後第 4 個「接取」停用；放棄一個每日任務再接，進度歸 0；360 與 430 寬無水平捲動；亮暗主題各看一次。截圖存 `docs/superpowers/screenshots/plan-8-missions.png`（進行中頁，含進度條）。
- [ ] **Step 6: Commit**

```bash
git add frontend docs/superpowers/screenshots/plan-8-missions.png
git commit -m "✨ feat: add the missions screen"
```

---

## 自我檢查

- Spec §9 任務：接取條件 職業／等級／性別／出生地／前置（T3，另加 WOG 的互斥）、殺怪計數（T5）、交付需求物（T7）、獎勵 物品／金錢／技能（T7；寵物 schema 在 T1、內容與發放在計畫 9）、經驗 `m_lv×190`（T1 `Exp`、T7）、同時 3 個（T3、T4）、每日隨機任務 09:00 重置（T3 週期與擲骰、T4 接取、T5 跨重置計數、T7 重接）。§12 第 8 列：任務引擎（T1～T4、T7）、殺怪計數（T5）、交付（T7）、每日任務（T3）、任務畫面（T8）。§2／§7.1 離線結算：計數在模擬器內依場次時間進行（T5），不是上線時才補算。
- 與研究報告不一致處依 PHP：`mission_get` 漏查出生地與前置（這裡都查）；擊殺只計勝場；同怪多任務（裁定全算）；每日模板是單一怪物的目標池＋獨立報酬池（研究稱「殺怪組合隨機」）；過期每日紀錄連進行中一起刪；任務怪與 `m_npc` 研究只帶過——前者做、後者不做。T1 的 `mission-table.md` 記下這些差異。
- 型別一致：`MissionDefinition`／`KillTarget`／`ItemQuantity`／`MissionReward`（T1）→ T3～T7；`MissionRecord`／`MissionState`（T2）→ T3～T7；`MissionObjective`／`MissionBoardView`／`MissionEntry`／`MissionBoard.Current`（T3）→ T4、T5、T7；`ActiveMission`／`MissionProgressEvent`／`AdvanceSummary.MissionKills`（T5）→ T6；`MissionCompletion`（T7）→ T8；前端 `MissionBoard`／`MissionView`（T8）對應 `MissionBoardResponse`／`MissionResponse`（T4）。
- Review Focus 五項都有對應測試：T5 `Adventure_StopsCountingDailyAfterReset`、`Advance_WritesNoMissionProgress_WhenCharacterUpdateConflicts`、`KillCounter_CountsEveryMatchingMission`；T7 `Complete_DeliveryFreesSlotForReward`／`Complete_BagFullRollsBackEverything`、`Complete_DailyReacceptOnlyAfterReset`。
- 順序理由：T6（戰報）緊接在 T5（後端開始送 `mission` 事件）之後，避免前端在中間狀態遇到未處理的事件型別。
