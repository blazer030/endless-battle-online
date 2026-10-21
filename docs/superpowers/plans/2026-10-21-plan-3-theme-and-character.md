# 計畫 3：現代奇幻主題與角色面板 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 畫面換成現代奇幻風；角色有 WOG 的七項能力值與衍生值，創角時配點並選性別／屬性／出生地／職業，新的「角色」畫面能看到所有數字，而且現在的戰鬥就用這些數字打。

**Architecture:** 後端在 `characters` 表加上能力值欄位（Flyway migration → scaffold），Domain 新增 `BaseStats`／`DerivedStats`／`CharacterFactory`，職業先以內容 JSON 存在（轉職邏輯留給計畫 5）。推進 API 暫時把衍生值橋接到現有的 `CombatStats`（計畫 4 換成 WOG 戰鬥）。前端換主題色票與字型、重做創角與角色畫面、舞台改夜景。

**Tech Stack:** 沿用計畫 1、2。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §3、§5.1、§5.3、§10；研究依據 `docs/research/wog-research-stats-jobs.md`。

## Global Constraints

- 計畫 1、2 的 Global Constraints 全部適用（分層、Flyway、scaffold 不手改、`src/game/` 純 TS、顏色只在 `theme.css`／`palette.ts`、測試與 commit 規則、`./scripts/check.sh` 為提交門檻）。
- 每個任務完成後必須：(1) **test-runner** 獨立執行 `./scripts/check.sh`；(2) **reviewer** 審查。兩者都通過才算完成。〔使用者要求〕
- 數值公式一字不差照 spec §3.1–3.3：
  - `hpmax = round(life × 25) + 50`、`spmax = round((smart + be) × 3) + 25`、`at = str`、`mat = smart`、`df = vit`、`mdf = be`（本計畫裝備加成一律 0）。
  - 創角：10 點分配到 str／smart／life／agi，每項 ≥ 1，總和 ≤ 10；底值 +10；vit／au／be = 10；hp 50、sp 25、money 2000、level 1、nextExp 1000。
  - 戰鬥橋接：`CombatStats(MaxHealth: hpmax, Attack: at, Defense: df, CritChance: 0.10, CritMultiplier: 1.2, AttacksPerSecond: 1.0)`。
- 枚舉值：性別 1 男 2 女；屬性 1 地 2 水 3 火 4 木 5 風 6 毒；出生地 1 中央大陸 2 魔法王國 3 熱帶雨林 4 末日王城。
- 主題色票（spec §10）：背景 `#121826`→`#1b2232`、面板 `#222b3d`、邊框 `#2e3950`、文字 `#e8ecf4`、次要文字 `#9aa5bd`、金 `#e0a63a`、青 `#4cc3d9`、緋紅 `#e2554f`、翠綠 `#4fc47a`。字型：介面 Noto Sans TC；標題 Noto Serif TC 700（中文）／Cinzel（拉丁）。
- 現有的 56→58 後端測試與 51 前端測試必須持續通過（除非本計畫明確改寫其斷言）。
- 商業用詞：介面稱「角色」（原 WOG 名稱）；導覽改為 **冒險、角色、技能、背包**（技能、背包本計畫停用）。

## Review Focus

- **配點邊界**：總和 11、某項 0、負數、非整數、缺欄位 → 400；總和 ≤ 9 可以（剩餘點數作廢，WOG 如此）。→ Task 3 `Create_RejectsInvalidAllocation`、`Create_AllowsUnderspentPoints`。
- **枚舉越界**：性別 3、屬性 7、出生地 0、不存在的職業 id、進階職業當起始職業 → 400。→ Task 3 `Create_RejectsInvalidChoices`。
- **既有角色**：migration 後舊資料列要有合理預設（戰士、各 10 點、hp 50），`GET /me` 不會 500。→ Task 1 `Migration_GivesExistingRowsDefaults`。
- **衍生值四捨五入**：`round` 用 `MidpointRounding.AwayFromZero`，與 PHP `round` 一致。→ Task 2 `Derived_RoundsHalfAwayFromZero`。
- **現有戰鬥用新數字**：不同配點的角色打同一隻怪，傷害不同；推進後 `GET /me` 的 exp／money 增加，且回應含完整能力值。→ Task 3 `Advance_UsesCharacterStats`。

---

## File Structure

```
backend/
  db/ddl/V000000003__add_character_stats.sql
  content/jobs.json                                   12 個職業
  Inkbound.Domain/Characters/BaseStats.cs             七項裸點 record
  Inkbound.Domain/Characters/DerivedStats.cs          at/mat/df/mdf/hpmax/spmax 計算
  Inkbound.Domain/Characters/CharacterFactory.cs      創角規則
  Inkbound.Domain/Characters/Character.cs             擴充欄位（取代 Experience/SpiritStones 命名）
  Inkbound.Domain/Characters/PlayerStats.cs           刪除，改為 CombatBridge
  Inkbound.Domain/Characters/CombatBridge.cs          DerivedStats → CombatStats
  Inkbound.Domain/Content/JobDefinition.cs
  Inkbound.Application/Content/IGameContent.cs        AllJobs / GetJob
  Inkbound.Application/Characters/CharacterService.cs CreateAsync 新簽名
  Inkbound.Application/Characters/CreateCharacterCommand.cs
  Inkbound.Infrastructure/Content/JsonGameContent.cs  載入 jobs.json
  Inkbound.Infrastructure/Persistence/CharacterRepository.cs 對應新欄位
  Inkbound.Api/Endpoints/Contracts.cs, CharacterEndpoints.cs, ContentEndpoints.cs
  Inkbound.Tests/TestContent/jobs.json
frontend/src/
  ui/theme.css, index.html, render/palette.ts         主題
  ui/screens.css, AdventureScreen.css, BattleLog.css  改用新 token
  render/StageRenderer.ts, effects.ts                 夜景、橫排名牌、改色
  ui/CreateCharacterScreen.tsx (+ .css)               配點與選項
  ui/CharacterScreen.tsx (+ .css)                     角色面板
  ui/AdventureScreen.tsx                              導覽改名、角色分頁切換、頂欄 Lv/EXP/金錢
  game/types.ts, api/schema.ts                        重新產生
```

---

### Task 1: 能力值欄位與職業內容（後端資料層）

**Files:**
- Create: `backend/db/ddl/V000000003__add_character_stats.sql`, `backend/content/jobs.json`, `backend/Inkbound.Tests/TestContent/jobs.json`, `backend/Inkbound.Domain/Content/JobDefinition.cs`
- Generate: `Persistence/InkboundDbContext.cs`, `Persistence/Models/Character.cs`（`./scripts/scaffold-db.sh`）
- Modify: `IGameContent.cs`, `JsonGameContent.cs`, `JsonGameContentTests.cs`, `DatabaseSchemaTests.cs`

**Interfaces:**
- Produces:

```csharp
public sealed record StatRange(int Min, int Max);
public sealed record JobGrowth(StatRange Str, StatRange Smart, StatRange Life, StatRange Vit, StatRange Agi, StatRange Au, StatRange Be);
public sealed record StatRequirement(string Stat, int Minimum);   // Stat ∈ str|smart|life|vit|agi|au|be
public sealed record JobDefinition(string Id, string Name, int Tier, string? ParentId, int UnlockLevel, int ProficiencyRequired, IReadOnlyList<StatRequirement> StatRequirements, JobGrowth Growth);
// IGameContent
IReadOnlyList<JobDefinition> AllJobs { get; }
JobDefinition GetJob(string jobId);   // 找不到 → GameContentException
```

Migration（`characters` 表）：

```sql
ALTER TABLE characters
    CHANGE COLUMN experience exp BIGINT NOT NULL DEFAULT 0,
    CHANGE COLUMN spirit_stones money BIGINT NOT NULL DEFAULT 0,
    ADD COLUMN level INT NOT NULL DEFAULT 1,
    ADD COLUMN next_exp BIGINT NOT NULL DEFAULT 1000,
    ADD COLUMN sex TINYINT NOT NULL DEFAULT 1,
    ADD COLUMN element TINYINT NOT NULL DEFAULT 1,
    ADD COLUMN birth TINYINT NOT NULL DEFAULT 1,
    ADD COLUMN job_id VARCHAR(32) NOT NULL DEFAULT 'warrior',
    ADD COLUMN special_attack_name VARCHAR(16) NOT NULL DEFAULT '必殺',
    ADD COLUMN base_str INT NOT NULL DEFAULT 10,
    ADD COLUMN base_smart INT NOT NULL DEFAULT 10,
    ADD COLUMN base_life INT NOT NULL DEFAULT 10,
    ADD COLUMN base_vit INT NOT NULL DEFAULT 10,
    ADD COLUMN base_agi INT NOT NULL DEFAULT 10,
    ADD COLUMN base_au INT NOT NULL DEFAULT 10,
    ADD COLUMN base_be INT NOT NULL DEFAULT 10,
    ADD COLUMN hp INT NOT NULL DEFAULT 50,
    ADD COLUMN sp INT NOT NULL DEFAULT 25;
```

`jobs.json`（正式內容，`JsonSerializerDefaults.Web`；成長 `[min,max]`）：

```json
{ "jobs": [
  { "id":"warrior",     "name":"戰士",   "tier":1, "parentId":null,       "unlockLevel":1,  "proficiencyRequired":0,    "statRequirements":[], "growth":{"str":[2,3],"smart":[0,1],"life":[1,2],"vit":[1,2],"agi":[1,2],"au":[0,1],"be":[0,1]} },
  { "id":"sorcerer",    "name":"術士",   "tier":1, "parentId":null,       "unlockLevel":1,  "proficiencyRequired":0,    "statRequirements":[], "growth":{"str":[0,1],"smart":[2,3],"life":[1,2],"vit":[0,1],"agi":[1,2],"au":[1,2],"be":[1,2]} },
  { "id":"thief",       "name":"盜賊",   "tier":1, "parentId":null,       "unlockLevel":1,  "proficiencyRequired":0,    "statRequirements":[], "growth":{"str":[1,2],"smart":[0,1],"life":[1,2],"vit":[0,1],"agi":[2,3],"au":[1,2],"be":[0,1]} },
  { "id":"bard",        "name":"樂手",   "tier":1, "parentId":null,       "unlockLevel":1,  "proficiencyRequired":0,    "statRequirements":[], "growth":{"str":[0,1],"smart":[1,2],"life":[1,2],"vit":[0,1],"agi":[1,2],"au":[2,3],"be":[1,2]} },
  { "id":"berserker",   "name":"狂戰士", "tier":2, "parentId":"warrior",  "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"str","minimum":60}],                           "growth":{"str":[3,4],"smart":[0,1],"life":[2,3],"vit":[0,1],"agi":[1,2],"au":[0,1],"be":[0,1]} },
  { "id":"knight",      "name":"騎士",   "tier":2, "parentId":"warrior",  "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"vit","minimum":50}],                           "growth":{"str":[2,3],"smart":[0,1],"life":[2,3],"vit":[2,3],"agi":[0,1],"au":[0,1],"be":[1,2]} },
  { "id":"archmage",    "name":"魔導師", "tier":2, "parentId":"sorcerer", "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"smart","minimum":60}],                         "growth":{"str":[0,1],"smart":[3,4],"life":[1,2],"vit":[0,1],"agi":[1,2],"au":[1,2],"be":[1,2]} },
  { "id":"geomancer",   "name":"風水師", "tier":2, "parentId":"sorcerer", "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"be","minimum":40}],                            "growth":{"str":[0,1],"smart":[2,3],"life":[1,2],"vit":[1,2],"agi":[1,2],"au":[1,2],"be":[2,3]} },
  { "id":"assassin",    "name":"刺客",   "tier":2, "parentId":"thief",    "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"agi","minimum":60}],                           "growth":{"str":[1,2],"smart":[0,1],"life":[1,2],"vit":[0,1],"agi":[3,4],"au":[1,2],"be":[0,1]} },
  { "id":"martialist",  "name":"武術家", "tier":2, "parentId":"thief",    "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"str","minimum":40},{"stat":"agi","minimum":40}], "growth":{"str":[2,3],"smart":[0,1],"life":[2,3],"vit":[1,2],"agi":[2,3],"au":[0,1],"be":[0,1]} },
  { "id":"dancer",      "name":"舞者",   "tier":2, "parentId":"bard",     "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"au","minimum":50},{"stat":"agi","minimum":40}], "growth":{"str":[0,1],"smart":[1,2],"life":[1,2],"vit":[0,1],"agi":[2,3],"au":[2,3],"be":[1,2]} },
  { "id":"beastmaster", "name":"獸師",   "tier":2, "parentId":"bard",     "unlockLevel":30, "proficiencyRequired":3500, "statRequirements":[{"stat":"au","minimum":50}],                            "growth":{"str":[1,2],"smart":[1,2],"life":[2,3],"vit":[1,2],"agi":[1,2],"au":[2,3],"be":[1,2]} }
] }
```

測試內容 `TestContent/jobs.json`：同結構，只放 `warrior`（名稱「測試戰士」）、`sorcerer`（「測試術士」）、`berserker`（parent warrior，str ≥ 60）。

驗證規則（`JsonGameContent.Load`）：id 重複、`parentId` 不存在、tier 1 必須無 parent、tier 2 必須有 parent、成長 `min ≤ max` 且 ≥ 0、`statRequirements.stat` 必須是七項之一 → `GameContentException`（訊息含 id）。

- [ ] **Step 1: Write the failing tests**
  - `DatabaseSchemaTests.Migration_GivesExistingRowsDefaults`：用 Models 新增一個只填必填欄位（AccountId、Name、CurrentStageId、LastSettledAt、CreatedAt）的角色，讀回 `Level == 1`、`NextExp == 1000`、`JobId == "warrior"`、`BaseStr == 10`、`Hp == 50`、`Money == 0`、`Exp == 0`。
  - `JsonGameContentTests.Load_TestContent_ExposesJobs`：`AllJobs.Count == 3`、`GetJob("berserker").ParentId == "warrior"`、`.StatRequirements[0] == new StatRequirement("str", 60)`、`.Growth.Str == new StatRange(3, 4)`。
  - `Load_Rejects_InvalidJobs`（Theory）：parent 不存在、tier 1 有 parent、growth min > max、stat 名稱 `"luck"` → 例外訊息含 job id。
  - `Load_ShippedContent_IsValid` 擴充：`AllJobs.Count == 12`，每個 tier 2 的 parent 都是 tier 1。
- [ ] **Step 2:** 寫 migration → `docker compose up -d`（flyway 會套用 V3）→ `./scripts/scaffold-db.sh` → 確認 `Models/Character.cs` 有新屬性（`Exp`、`Money`、`Level`、`NextExp`、`Sex`、`Element`、`Birth`、`JobId`、`SpecialAttackName`、`BaseStr`…`BaseBe`、`Hp`、`Sp`）。
- [ ] **Step 3:** `cd backend && dotnet test`，預期新測試失敗（型別不存在／屬性不存在）。
- [ ] **Step 4:** 實作 `JobDefinition`、`IGameContent` 擴充、`JsonGameContent` 載入與驗證、兩份 `jobs.json`。
- [ ] **Step 5:** `./scripts/check.sh`，預期通過（既有的 Repository 編譯會因欄位改名而壞——在本任務把 `CharacterRepository` 對 `Experience`/`SpiritStones` 的引用改成 `Exp`/`Money`，Domain `Character` 暫不改，由 Task 2 擴充）。
- [ ] **Step 6: Commit**（兩個 commit）：`🚧 chore: add character stat columns and scaffold models`、`✨ feat: load job definitions from content`

---

### Task 2: 能力值與創角規則（Domain）

**Files:**
- Create: `BaseStats.cs`, `DerivedStats.cs`, `CharacterFactory.cs`, `CombatBridge.cs`, `Application/Characters/CreateCharacterCommand.cs`
- Modify: `Character.cs`（新欄位）、刪除 `PlayerStats.cs`，`CharacterRepository.cs` 對應全部欄位
- Test: `Inkbound.Tests/Domain/DerivedStatsTests.cs`, `CharacterFactoryTests.cs`

**Interfaces:**

```csharp
public sealed record BaseStats(int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be);
public sealed record DerivedStats(int At, int Mat, int Df, int Mdf, int HpMax, int SpMax)
{
    public static DerivedStats From(BaseStats total);   // 本計畫 total == base（裝備加成為 0）
}
public sealed record Character(long Id, long AccountId, string Name, int Level, long Exp, long NextExp, long Money,
    int Sex, int Element, int Birth, string JobId, string SpecialAttackName, BaseStats Base, int Hp, int Sp,
    string CurrentStageId, DateTimeOffset LastSettledAt, int Version);
public sealed record StatAllocation(int Str, int Smart, int Life, int Agi);
public sealed record CreateCharacterCommand(string Name, int Sex, int Element, int Birth, string JobId, StatAllocation Allocation, string SpecialAttackName);
public static class CharacterFactory
{
    public const int TotalPoints = 10;
    // 驗證失敗丟 InputValidationException（在 Application 層包裝）；Domain 這裡以回傳 Result 或丟 ArgumentException 二選一，實作者決定並在報告說明
    public static Character Create(long accountId, CreateCharacterCommand command, JobDefinition job, string startingStageId, DateTimeOffset now);
}
public static class CombatBridge { public static CombatStats ToCombatStats(DerivedStats derived); }
```

- [ ] **Step 1: Write the failing tests**
  - `Derived_MatchesWogFormulas`：`BaseStats(17,12,15,10,14,10,10)` → `At 17, Mat 12, Df 10, Mdf 10, HpMax 425, SpMax 91`。
  - `Derived_RoundsHalfAwayFromZero`：建一組會產生 `.5` 的值（`life` 乘 25 永遠整數，所以用 `spmax`：smart+be = 奇數時 ×3 仍整數——因此本測試改為驗證實作使用 `Math.Round(x, MidpointRounding.AwayFromZero)` 的輔助函式 `DerivedStats.RoundLikePhp(2.5) == 3`、`(-2.5) == -3`）。
  - `Create_AppliesAllocationAndDefaults`：allocation (4,2,3,1) → Base (14,12,13,10,11,10,10)、Hp 50、Sp 25、Money 2000、Level 1、NextExp 1000、JobId、Sex/Element/Birth、SpecialAttackName、Exp 0。
  - `Create_RejectsAllocationOverTen`、`Create_RejectsZeroInAnyStat`、`Create_AllowsUnderspent`（總和 8 可以）。
  - `Create_RejectsInvalidEnums`（sex 3、element 0、birth 5）、`Create_RejectsNonStartingJob`（tier 2）。
  - `Create_TrimsAndValidatesSpecialAttackName`（1～8 字，空白拒絕）。
  - `CombatBridge_MapsDerivedToCombatStats`：HpMax→MaxHealth、At→Attack、Df→Defense、0.10、1.2、1.0。
- [ ] **Step 2:** `dotnet test --filter-class "*DerivedStatsTests"` 等，預期編譯失敗。
- [ ] **Step 3:** 實作；`CharacterRepository` 的 `ToDomain`／`AddAsync`／`UpdateAsync` 對應所有新欄位；`InMemoryCharacterRepository` 與既有測試的 `Character` 建構式呼叫改用新簽名（可加測試用 builder `TestCharacters.Default()`）。
- [ ] **Step 4:** `./scripts/check.sh`，預期通過。
- [ ] **Step 5: Commit** `✨ feat: model WOG base stats, derived stats and character creation`

---

### Task 3: 創角與角色 API、戰鬥橋接

**Files:**
- Modify: `CharacterService.cs`, `AdventureService.cs`, `Contracts.cs`, `CharacterEndpoints.cs`, `ContentEndpoints.cs`
- Test: `Api/CharacterEndpointsTests.cs`（改寫）, `Api/AdventureEndpointsTests.cs`, `Api/ContentEndpointsTests.cs`, `Application/AdventureServiceTests.cs`

**Interfaces:**

```csharp
public sealed record CreateCharacterRequest(string? Name, int? Sex, int? Element, int? Birth, string? JobId, StatAllocationRequest? Allocation, string? SpecialAttackName);
public sealed record StatAllocationRequest(int? Str, int? Smart, int? Life, int? Agi);
public sealed record StatsResponse(int Str, int Smart, int Life, int Vit, int Agi, int Au, int Be, int At, int Mat, int Df, int Mdf, int HpMax, int SpMax);
public sealed record CharacterResponse(string Name, int Level, long Exp, long NextExp, long Money, int Hp, int Sp,
    int Sex, int Element, int Birth, string JobId, string JobName, string SpecialAttackName, StatsResponse Stats, string CurrentStageId);
public sealed record JobContent(string Id, string Name, int Tier, string? ParentId, int UnlockLevel, int ProficiencyRequired, IReadOnlyList<StatRequirementContent> StatRequirements);
public sealed record StatRequirementContent(string Stat, int Minimum);
public sealed record ContentResponse(IReadOnlyList<NamedContent> Monsters, IReadOnlyList<NamedContent> Stages, IReadOnlyList<JobContent> Jobs,
    IReadOnlyList<NamedContent> Elements, IReadOnlyList<NamedContent> Births, IReadOnlyList<NamedContent> Sexes);
// AdvanceResponse: ExperienceGained → ExpGained, SpiritStonesGained → MoneyGained
```

- 屬性／出生地／性別名稱表寫死在 `ContentEndpoints`（id 為字串 "1"…）。
- `AdventureService`：`CombatBridge.ToCombatStats(DerivedStats.From(character.Base))` 取代 `PlayerStats.Starting`；收益加到 `Exp`／`Money`。升級與 `NextExp` 由計畫 4 處理（本計畫 exp 只累加）。
- `GET /api/characters/me` 與 `POST /api/characters` 回傳完整 `CharacterResponse`。

- [ ] **Step 1: Write the failing tests**
  - `Create_ReturnsFullCharacter`：完整 body（戰士、allocation 4/2/3/1、男、火、中央大陸、「烈焰斬」）→ 201，`stats.hpMax == 375`（life 13）、`stats.at == 14`、`money == 2000`、`jobName == "測試戰士"`。
  - `Create_RejectsInvalidAllocation`（Theory：總和 11；某項 0；負數；缺 allocation）→ 400。
  - `Create_AllowsUnderspentPoints`（總和 8）→ 201。
  - `Create_RejectsInvalidChoices`（Theory：sex 3、element 7、birth 0、jobId `"ghost"`、jobId `"berserker"`）→ 400。
  - `GetContent_IncludesJobsAndEnums`：`jobs` 3 筆、`elements` 6 筆（"3" → 火）、`births` 4、`sexes` 2。
  - `Advance_UsesCharacterStats`：兩個帳號，一個 allocation str 7／其餘 1，一個 str 1／life 7；各推進 10 秒；斷言第一個的 `attack` 事件 `damage` 最大值大於第二個；兩者 `GET /me` 的 `exp` 等於回應的 `expGained`。
  - `AdventureServiceTests` 既有手算改為以 `DerivedStats` 推導的角色（Base 全 10＋allocation 2/2/2/2：At 12, Df 12, HpMax 350）重新手算，並記錄在測試註解。
- [ ] **Step 2:** 執行，預期失敗。
- [ ] **Step 3:** 實作。
- [ ] **Step 4:** `./scripts/check.sh`，預期通過。
- [ ] **Step 5: Commit** `✨ feat: create characters with WOG stats and expose them over the API`

---

### Task 4: 現代奇幻主題

**Files:**
- Modify: `frontend/index.html`（字型改 Noto Sans TC 400/700 + Noto Serif TC 700 + Cinzel 700）、`src/ui/theme.css`、`src/render/palette.ts`、`src/ui/screens.css`、`src/ui/AdventureScreen.css`、`src/ui/BattleLog.css`、`src/render/StageRenderer.ts`、`src/render/effects.ts`、`src/ui/AuthScreen.tsx`（副標改「奇幻放置 RPG」）
- Test: 既有測試維持；新增 `src/ui/theme.test.ts`（讀取 `theme.css` 文字，斷言舊色票 `#f3ead8`、`#b8322a` 不再出現、新變數名存在）

**Theme tokens（`theme.css`）**：`--bg-top #121826`、`--bg-bottom #1b2232`、`--panel #222b3d`、`--border #2e3950`、`--text #e8ecf4`、`--text-muted #9aa5bd`、`--gold #e0a63a`、`--cyan #4cc3d9`、`--crimson #e2554f`、`--green #4fc47a`；`--font-ui`、`--font-title`。`palette.ts` 同名鍵（camelCase）同值，並加 `night #0b1020`（舞台遠景）。

**舞台**：背景透明維持；遠山改 `#1b2232` 80% 疊 `night`；底部霧氣（白 6% 漸層）；劍氣 `gold`→白；傷害數字 `crimson`、暴擊更大並帶 `gold` 描邊；「會心」印章改為金邊黑底的橫排名牌樣式文字「CRITICAL」（拉丁，Cinzel）；玩家剪影改 `#e8ecf4` 30% 的淡色輪廓；沒有圖時妖怪顯示**橫排**名牌（黑底 `#0b1020` 90%、金邊 1px、Noto Serif TC 700 28px）。

**戰報**：面板底 `--panel`，muted 用 `--text-muted`，drop 數字 `--gold`，trigger 數字 `--crimson`。**導覽**：四個分頁「冒險、角色、技能、背包」，選中金底黑字，停用灰字。

- [ ] **Step 1:** 寫 `theme.test.ts`（RED：舊色票仍在）。
- [ ] **Step 2:** 實作主題替換；跑 `pnpm check`；`pnpm build` 無警告。
- [ ] **Step 3:** 用瀏覽器（375×667 與桌面）截圖存 `docs/superpowers/screenshots/plan-3-theme.png`，對照 spec §10 檢查配色與可讀性（深底淺字對比 ≥ 4.5:1，用 devtools 或手算）。
- [ ] **Step 4:** `./scripts/check.sh`；Commit `✨ feat: switch the UI to a modern fantasy theme`

---

### Task 5: 創角畫面（配點與選項）

**Files:**
- Regenerate: `src/api/schema.ts`（API 啟動後 `pnpm generate:api`）；Modify `src/game/types.ts`（`CharacterView` 新形狀、`ContentNames` 含 jobs/elements/births/sexes）、`src/api/client.ts`（`createCharacter(command)`）
- Rewrite: `src/ui/CreateCharacterScreen.tsx` (+ `CreateCharacterScreen.css`)，Modify `App.tsx`（建角前先取 content）
- Test: `CreateCharacterScreen.test.tsx`（改寫）、`client.test.ts`（createCharacter body）

**畫面**：
- 名稱、必殺技名稱（1～8 字）。
- 性別（兩個按鈕）、屬性（6 個色塊按鈕：地棕／水藍／火紅／木綠／風青／毒紫，顏色用 token 或新增 `--element-*` 變數）、出生地（4 選 1）、職業（4 張卡：名稱＋定位說明＋成長傾向文字，資料來自 content `jobs` 的 tier 1）。
- 配點：四個 stepper（力量／智力／生命／敏捷），各從 1 起，剩餘點數顯示「剩餘 N 點」，總和不得超過 10；旁邊即時顯示 HP 上限／SP 上限預覽（公式同後端，放 `src/game/stats.ts` 純函式 `deriveStats(base)` 並測試）。
- 送出按鈕「踏上旅途」；錯誤顯示 `role="alert"`。

- [ ] **Step 1:** 測試：`deriveStats` 與後端相同案例（17,12,15,10,14,10,10 → 425/91）；畫面：預設剩餘 6 點、加到 10 後再加被擋、送出的 body 與選擇一致、400 的 detail 顯示。
- [ ] **Step 2–4:** RED → 實作 → `pnpm check`、`./scripts/check.sh`。
- [ ] **Step 5: Commit** `✨ feat: allocate stats and choose a job on character creation`

---

### Task 6: 角色畫面與頂欄

**Files:**
- Create: `src/ui/CharacterScreen.tsx` (+ `.css`)；Modify: `AdventureScreen.tsx`（分頁切換：冒險／角色；頂欄顯示 `Lv`、`EXP x/next` 進度條、金錢；`displayedCharacter` 改用新欄位 `exp`／`money`）、`game/battlePlayback.ts`（`displayedCharacter` 的 exp/money 欄位名）、`game/battleLog.ts`（獎勵文案「獲得經驗 {exp}、金錢 {money}。」；摘要同理）
- Test: `CharacterScreen.test.tsx`、`AdventureScreen.test.tsx`（更新）、`battleLog.test.ts`、`battlePlayback.test.ts`（欄位名）

**角色畫面**：卡片 1：名稱、職業、Lv、性別／屬性／出生地、必殺技；HP／SP／EXP 三條；卡片 2：七項能力兩欄表格（裸點，括號留位給加成，本計畫顯示 `(+0)`）；卡片 3：物攻／魔攻／物防／魔防／HP 上限／SP 上限。

- [ ] **Step 1:** 測試：角色畫面顯示所有數字（用固定的 `CharacterView`）；點導覽「角色」切換、再點「冒險」切回且戰報仍在（不重新掛載 loop：用 CSS 隱藏而非卸載）；頂欄 EXP 條比例。
- [ ] **Step 2–4:** RED → 實作 → 檢查。
- [ ] **Step 5:** 瀏覽器實測：創角（配點）→ 冒險 → 角色畫面，截圖 `docs/superpowers/screenshots/plan-3-character.png`。
- [ ] **Step 6: Commit** `✨ feat: show the character sheet with WOG stats`
