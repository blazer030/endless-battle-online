# 計畫 5：職業與技能 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 玩家能在 12 個職業之間依 WOG 規則轉職（等級、前置職業熟練度、能力值門檻），用技能熟練度與金錢向職業學 16 個 WOG 技能、裝到 3 主動＋2 被動欄，戰鬥中依發動率施放並看到效果；並清掉計畫 4 延後的幾個小缺口。

**Architecture:** 技能與職業成長都是內容（`backend/content/skills.json`、`jobs.json`），啟動時載入驗證。Domain 新增 `Jobs/`（轉職判定）與 `Skills/`（技能定義、學習、裝備欄、戰鬥修正值）；`WogBattle` 在每回合玩家出手前後依欄位順序擲骰施放技能，效果以「回合開頭快照＋重套」實作，持續到期自動還原。玩家已學技能與裝備欄存 MySQL（V5）。API 新增 `/api/jobs`、`/api/skills`；前端開放「技能」分頁（職業、技能、欄位三區），戰報與舞台顯示技能施放。

**Tech Stack:** 沿用（.NET 10、EF Core database-first、Flyway、React 19、Pixi 8）。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §5（職業）、§6（技能）、§3.3、§7.1；研究依據 `docs/research/wog-research-battle.md` §3、`wog-research-stats-jobs.md` §4。

## Global Constraints

- 計畫 1～4 的 Global Constraints 全部適用。每個任務完成後都要經過 **test-runner**（獨立跑 `./scripts/check.sh`）與 **reviewer**。〔使用者要求〕
- 轉職條件一字不差照 spec §5.2：`Level ≥ UnlockLevel`；二階職業要求「父職業累積熟練度 `ch` ≥ ProficiencyRequired（3500）」；每個進階職業的 `statRequirements` 以**裸點 `Base`** 比較；無金錢成本；可換回基礎職業、同階互換；轉職後等級／經驗／裸點不變，**被動欄（4、5）清空，主動欄（1～3）保留**；裝備卸下留到計畫 7（目前沒有裝備）。同職業轉同職業 → 400。
- 技能規則照 spec §6 與研究 §3.2：學習條件「未擁有或可升下一級、前置技能等級達標、`sk_<job> ≥ proficiency`、金錢 ≥ money」，成功**同時扣** `sk_` 與金錢（`ch_` 不動）；欄位 1～3 只能放主動技能（發動率由玩家設定 10～100、步進 10、預設 50），欄位 4～5 只能放**當前職業**的被動技能（用技能自己的 `chance`）；同一技能不可重複裝備；只能裝已學技能。
- 戰鬥施放照研究 §3.3，簡化為：每回合玩家出手前，依欄位 1→5 順序對每個技能擲 `rand(1,100) ≤ chance`，且 `Sp ≥ sp`、且該技能未在持續中 → 扣 SP、施放、產生 `SkillEvent`；`timing = afterAttack` 的技能（嗜血）在玩家物理命中後才判定。持續 `rounds` 回合（含施放回合），到期還原。怪物在本計畫**沒有技能**（研究指出 WOG 怪物被動技能實際失效）。〔Claude 的判斷：欄位順序固定而非隨機，讓固定種子可重現〕
- 技能數值照研究 §3.4 的 WOG 公式，`lv` 取技能等級（1～5）。16 個技能與所屬職業固定如下（每個技能 5 級）：

| id | 名稱 | 職業 | 型別 | timing | 效果（value = lv） |
|---|---|---|---|---|---|
| burst | 爆氣 | warrior | active | beforeAttack | `AtMultiplier`：at ×(1+0.01·lv) |
| iron-skin | 硬甲 | warrior | passive | beforeAttack | `DfMultiplier`：df ×(1+0.01·lv) |
| mana-surge | 魔力增幅 | sorcerer | active | beforeAttack | `MatMultiplier`：mat ×(1+0.01·lv) |
| mana-skin | 魔甲 | sorcerer | passive | beforeAttack | `MdfMultiplier`：mdf ×(1+0.01·lv) |
| swift-shadow | 急影 | thief | passive | beforeAttack | `AgiMultiplier`：agi ×(1+0.01·lv) |
| combo | 連擊 | thief | active | beforeAttack | `MultiHit`：物理連打 lv+1 下，每下 at ×(1−0.01·lv) |
| lullaby | 靡靡之音 | bard | active | beforeAttack | `EnemyAgiDown`：敵 agi ×(1−0.01·lv)，持續 |
| heal | 治癒 | bard | active | beforeAttack | `Heal`：回復 round(smart × (1 + 0.375·(lv−1)))（lv1 1.0 … lv5 2.5） |
| war-god-wrath | 戰神之怒 | berserker | active | beforeAttack | `WarGodWrath`：at += round(str×0.45)，rag −75+5·lv |
| counter | 反擊 | knight | passive | beforeAttack | `Counter`：受物傷 ×(0.92−0.02·lv)，at ×(1+0.01·lv) |
| magic-combo | 魔連擊 | archmage | active | beforeAttack | `MagicMultiHit`：魔法連打 lv+1 下 |
| weaken | 減益 | geomancer | active | beforeAttack | `EnemyAttackDown`：敵 at、mat ×(1−0.02·lv)，持續 |
| keen-edge | 物暴擊率 | assassin | passive | beforeAttack | `CritChanceUp`：物理暴擊率 +2·lv |
| bloodthirst | 嗜血 | martialist | passive | afterAttack | `Bloodthirst`：物理命中後回復 round(實際傷害 × 0.02·lv) |
| bonus-strike | 附加傷 | dancer | active | beforeAttack | `BonusDamage`：每次命中附加 round(au × 0.08·lv)（物理與魔法皆加） |
| stun | 暈眩 | beastmaster | active | beforeAttack | `Stun`：持續期間每回合 `rand(1,100) ≤ 3·lv` 使怪物該回合不出手 |

- 每級數值表（`levels[lv-1]`）：`sp = 3·lv + 2`、`rounds`（持續，`Heal`／`Bloodthirst`／`BonusDamage` 為 1，其餘 3）、`chance`（被動技能的發動率：`20 + 10·lv`；主動技能此欄為 0，發動率由玩家欄位決定）、`money = 500·lv²`、`proficiency = 300·lv`。前置：同技能的上一級（lv1 無前置）。〔Claude 的設計：WOG 的 DB 值已遺失〕
- 熟練度顯示 `floor(ch/3500×100)%`，上限 100%。
- 新事件 `SkillEvent`（加入 `BattleEvents.cs` 的多型清單，discriminator `"skill"`）：

```csharp
public sealed record SkillEvent(double AtSeconds, CombatSide Caster, string SkillId, string SkillName, int Level, int SpAfter, int Heal) : BattleEvent(AtSeconds);
```

- `AttackEvent` 連打時每一下各一個事件，各下平均分配在攻方的半回合內（間隔 = 0.4 秒 ÷ 下數，最後一下早於對手出手與下一回合）〔執行時裁定，原訂每下 +0.1 秒會超出半回合〕；暈眩使怪物跳過出手時產生 `AttackEvent(Hit=false, Damage=0, Kind=Physical, Stunned=true)` — 為此 `AttackEvent` 新增 `bool Stunned` 欄位（預設 false），前端文案「{怪物}暈眩中，無法行動。」。
- 計畫 4 延後項目在本計畫處理（Task 1）：`GET /api/characters/me` 的 `actNum` 回復到現在（唯讀，不寫入）；模擬器在開打前 `hp < hpmax × AutoRestHpRatio(0.15)` 時先住宿（spec §7.1 的 HP 門檻，門檻先固定）；`jobs.json` 騎士體質門檻改 60（spec §5.2 例子）；成長表依 §5.3 檢查（基礎每級期望總和 7～9、進階 9～12）並附 Lv1→Lv100 期望值表。
- 程式碼註解中文、非必要不寫；commit 規則同前；生成檔 `Persistence/Models/*`、`EndlessBattleDbContext.cs` 只能由 `scripts/scaffold-db.sh` 產生。

## Review Focus

- **轉職後欄位**：轉職後被動欄清空、主動欄保留；主動欄裡非當前職業的技能仍可施放（WOG）。→ Task 5 `JobChange_ClearsPassiveSlotsKeepsActive`。
- **SP 不足**：SP 低於技能消耗時不發動也不扣 SP，整場都沒 SP 就一個技能都不出。→ Task 6 `Battle_SkipsSkillWhenSpInsufficient`。
- **持續到期**：持續 3 回合的倍率在第 4 回合還原，重複施放不會乘兩次。→ Task 6 `Battle_BuffExpiresAndDoesNotStack`。
- **學習原子性**：條件不符（錢夠但熟練度不夠、或反之）時金錢與熟練度都不扣。→ Task 4 `Learn_DeductsNothing_WhenProficiencyInsufficient`。
- **欄位驗證**：被動欄放主動技能、重複技能、未學技能、發動率超範圍 → 400 且原欄位不變。→ Task 4 `Slots_RejectInvalidLoadout_AndKeepExisting`。

---

## File Structure

```
backend/
  content/skills.json（新）、content/jobs.json（騎士 vit 60、成長表微調）
  db/ddl/V000000005__add_character_skills.sql（character_skills、character_skill_slots）
  EndlessBattle.Domain/Skills/SkillDefinition.cs（SkillDefinition、SkillLevel、SkillType、SkillTiming、SkillEffectKind）
  EndlessBattle.Domain/Skills/SkillBook.cs（已學技能 record）、SkillSlot.cs（欄位 record）
  EndlessBattle.Domain/Skills/SkillLearning.cs（學習判定與扣除）
  EndlessBattle.Domain/Skills/SkillLoadout.cs（欄位驗證）
  EndlessBattle.Domain/Jobs/JobChange.cs（轉職判定與套用）、JobProficiencyDisplay.cs
  EndlessBattle.Domain/Battle/BattleModifiers.cs（技能修正值）、SkillCaster.cs（施放與持續）、WogBattle.cs（修改）、RoundResolver.cs（修改：rag、暴擊加成、附加傷、受傷倍率、連打）、BattleEvents.cs（SkillEvent、Stunned）
  EndlessBattle.Domain/Adventure/AdventureSimulator.cs（修改：傳入 loadout、自動住宿門檻、SP 寫回）
  EndlessBattle.Application/Skills/{ISkillBookRepository, ISkillLoadoutRepository, SkillService}.cs
  EndlessBattle.Application/Jobs/JobService.cs
  EndlessBattle.Application/Characters/CharacterService.cs（修改：actNum 回復）
  EndlessBattle.Application/Content/IGameContent.cs（AllSkills、GetSkill、SkillsOf(jobId)）
  EndlessBattle.Infrastructure/Content/JsonGameContent.cs（載入 skills.json）
  EndlessBattle.Infrastructure/Persistence/{SkillBookRepository, SkillLoadoutRepository}.cs
  EndlessBattle.Api/Endpoints/{JobEndpoints, SkillEndpoints}.cs、Contracts.cs（擴充）、ContentEndpoints.cs（jobs 加 growth）
  EndlessBattle.Tests/TestContent/skills.json、jobs.json（補 thief、martialist）
  docs/research/job-growth-expectation.md
frontend/src/
  game/types.ts、battleLog.ts、stageModel.ts、skills.ts（新：純函式，欄位編輯模型）
  ui/SkillsScreen.tsx（新：職業／技能／欄位三區）、SkillsScreen.css、AdventureScreen.tsx（開放技能分頁）、jobBlurbs.ts（刪除，改讀 content growth）、CreateCharacterScreen.tsx
  api/client.ts、api/schema.ts（重新產生）
```

---

### Task 1: 計畫 4 延後項目與成長表

**Files:** `content/jobs.json`、`EndlessBattle.Application/Characters/CharacterService.cs`、`Domain/Adventure/AdventureSimulator.cs`、`docs/research/job-growth-expectation.md`、`ContentEndpoints.cs`／`Contracts.cs`（`JobContent` 加 `growth`）；測試 `Infrastructure/JsonGameContentTests.cs`、`Application/CharacterServiceTests.cs`、`Domain/Adventure/AdventureSimulatorTests.cs`、`Api/ContentEndpointsTests.cs`

**Interfaces:**
- Produces: `AdventureSimulator.AutoRestHpRatio = 0.15`；`JobContent(..., JobGrowthContent Growth)` 其中 `JobGrowthContent(int[] Str, int[] Smart, ... )` 每項 `[min, max]`；`CharacterService.GetAsync` 回傳的 `Character.ActNum` 已經過 `StaminaRules.Regenerate(…, clock.UtcNow)`（不寫入）。

- [ ] 測試 `JobGrowth_TotalsWithinSpecBands`（Theory 跑真正的 `backend/content/jobs.json`：每職業七項 `(min+max)/2` 總和，tier 1 在 [7, 9]、tier 2 在 [9, 12]）；`Knight_RequiresVitSixty`；`GetAsync_RegeneratesActNumWithoutWriting`（存 20、ActNumTime 10 分鐘前 → 回傳 32，repository 內仍是 20）；`Adventure_RestsBeforeFight_WhenHpBelowThreshold`（hp 14/100 → 先 RestEvent 再 Encounter；hp 15 不住宿）；`Content_ExposesJobGrowth`。
- [ ] 調整 `jobs.json`：騎士 `vit ≥ 60`；成長表落在區間（目前獸師 12.5、樂手／術士 9.5 不合）。寫 `docs/research/job-growth-expectation.md`：每職業 Lv100 期望值 = 10（裸點底）+ 99 × 平均，列七項與總和。
- [ ] Commit `✨ feat: regenerate stamina on read, auto-rest below 15% hp and expose job growth`（成長表與文件可另 commit `📄 docs:`）

---

### Task 2: 技能內容與載入

**Files:** `content/skills.json`、`Tests/TestContent/skills.json`、`Domain/Skills/SkillDefinition.cs`、`IGameContent.cs`、`JsonGameContent.cs`；測試 `Infrastructure/JsonGameContentTests.cs`

**Interfaces:**

```csharp
public enum SkillType { Active, Passive }
public enum SkillTiming { BeforeAttack, AfterAttack }
public enum SkillEffectKind { AtMultiplier, MatMultiplier, DfMultiplier, MdfMultiplier, AgiMultiplier, MultiHit, MagicMultiHit, EnemyAgiDown, EnemyAttackDown, Heal, WarGodWrath, Counter, CritChanceUp, Bloodthirst, BonusDamage, Stun }
public sealed record SkillLevel(int Level, int Sp, int Rounds, int Chance, long Money, long Proficiency);
public sealed record SkillDefinition(string Id, string Name, string JobId, SkillType Type, SkillTiming Timing, SkillEffectKind Effect, string Description, IReadOnlyList<SkillLevel> Levels)
{ public int MaxLevel => Levels.Count; public SkillLevel LevelAt(int level); }
// IGameContent 新增
IReadOnlyList<SkillDefinition> AllSkills { get; }
SkillDefinition GetSkill(string skillId);
IReadOnlyList<SkillDefinition> SkillsOf(string jobId);
```

- [ ] 測試：`Skills_LoadSixteenWithFiveLevels`（真實內容：16 筆、每筆 5 級、數值表符合公式 sp/money/proficiency）；`Skills_RejectUnknownJob`、`Skills_RejectDuplicateId`、`Skills_RejectNonIncreasingLevels`、`Skills_RejectPassiveWithZeroChance`、`Skills_RejectActiveWithNonZeroChance`。
- [ ] 寫 `skills.json`（Global Constraints 的表與數值公式）；測試內容 `TestContent/skills.json` 只放 burst、iron-skin、heal、combo、bloodthirst、stun（對應 TestContent 的 warrior／bard／thief／martialist／beastmaster — 補齊 `TestContent/jobs.json`）。
- [ ] Commit `✨ feat: load the WOG skill catalog`

---

### Task 3: 資料表與 repository

**Files:** `db/ddl/V000000005__add_character_skills.sql`、`scripts/scaffold-db.sh` 重新產生 `Persistence/Models/*`、`Domain/Skills/SkillBook.cs`、`SkillSlot.cs`、`Application/Skills/ISkillBookRepository.cs`、`ISkillLoadoutRepository.cs`、`Infrastructure/Persistence/SkillBookRepository.cs`、`SkillLoadoutRepository.cs`、`DependencyInjection.cs`；測試 `Infrastructure/SkillRepositoriesTests.cs`、`DatabaseSchemaTests.cs`

**Interfaces:**

```sql
CREATE TABLE character_skills (character_id BIGINT NOT NULL, skill_id VARCHAR(32) NOT NULL, level INT NOT NULL, PRIMARY KEY (character_id, skill_id), FOREIGN KEY → characters(id));
CREATE TABLE character_skill_slots (character_id BIGINT NOT NULL, slot TINYINT NOT NULL, skill_id VARCHAR(32) NOT NULL, chance INT NOT NULL, PRIMARY KEY (character_id, slot), FOREIGN KEY → characters(id));
```

```csharp
public sealed record LearnedSkill(string SkillId, int Level);
public sealed record SkillSlot(int Slot, string SkillId, int Chance);   // Slot 1..5；被動欄 Chance 存技能自己的值
public interface ISkillBookRepository { Task<IReadOnlyList<LearnedSkill>> ListAsync(long characterId, CancellationToken ct); Task UpsertAsync(long characterId, LearnedSkill skill, CancellationToken ct); }
public interface ISkillLoadoutRepository { Task<IReadOnlyList<SkillSlot>> ListAsync(long characterId, CancellationToken ct); Task ReplaceAsync(long characterId, IReadOnlyList<SkillSlot> slots, CancellationToken ct); }
```

- [ ] 測試：`SkillBook_UpsertRaisesLevel`、`SkillBook_ListsEmptyForNewCharacter`、`Loadout_ReplaceClearsMissingSlots`（先存 1、2、4，再存 1、3 → 只剩 1、3）、`Schema_HasSkillTables`。
- [ ] `ReplaceAsync` 在單一交易內 delete + insert。
- [ ] Commit `🚧 chore: add character skill tables and repositories`

---

### Task 4: 學習與裝備（Domain＋API）

**Files:** `Domain/Skills/SkillLearning.cs`、`SkillLoadout.cs`、`Application/Skills/SkillService.cs`、`Api/Endpoints/SkillEndpoints.cs`、`Contracts.cs`；測試 `Domain/Skills/SkillLearningTests.cs`、`SkillLoadoutTests.cs`、`Application/SkillServiceTests.cs`、`Api/SkillEndpointsTests.cs`、`Support/InMemorySkillBookRepository.cs`、`InMemorySkillLoadoutRepository.cs`

**Interfaces:**

```csharp
public sealed record LearnCheck(bool Allowed, IReadOnlyList<string> Reasons, int TargetLevel);   // Reasons 代碼：wrongJob、maxLevel、notEnoughProficiency、notEnoughMoney
public static class SkillLearning
{
    public static LearnCheck Evaluate(Character c, SkillDefinition skill, int currentLevel, long skillPoints);
    public static (Character Character, long ProficiencyCost, LearnedSkill Learned) Apply(Character c, SkillDefinition skill, int currentLevel);   // 先 Evaluate，不允許則丟 InputValidationException
}
public static class SkillLoadout
{
    public const int ActiveSlots = 3, PassiveSlots = 2, DefaultChance = 50, MinChance = 10, MaxChance = 100, ChanceStep = 10;
    // 代碼：slotOutOfRange、notLearned、activeInPassiveSlot、passiveInActiveSlot、passiveNotCurrentJob、duplicateSkill、chanceOutOfRange
    public static IReadOnlyList<string> Validate(IReadOnlyList<SkillSlot> slots, string currentJobId, IReadOnlyList<LearnedSkill> learned, Func<string, SkillDefinition> skills);
}
// SkillService：GetAsync(accountId) → SkillOverview(JobId, SkillPoints, Skills[], Slots[])；LearnAsync(accountId, skillId)；SetSlotsAsync(accountId, slots)
// API：GET /api/skills；POST /api/skills/learn { skillId }；PUT /api/skills/slots { slots: [{ slot, skillId, chance }] }（省略的欄位＝清空）
// SkillOverview.Skills[]：{ id, name, type, effect, description, jobId, level, maxLevel, next: { level, sp, rounds, chance, money, proficiency, canLearn, reasons[] } | null }
```

- [ ] 測試：`Learn_FirstLevel_DeductsMoneyAndSkillPoints`（錢 2000、sk 300、burst lv1 money 500 → 錢 1500、sk 0、level 1）；`Learn_NextLevel_RequiresPreviousLevel`（lv0 直接學 lv2 不可能：Apply 永遠學 currentLevel+1）；`Learn_DeductsNothing_WhenProficiencyInsufficient`；`Learn_RejectsOtherJobSkill`；`Learn_RejectsMaxLevel`；`Loadout_AcceptsThreeActiveTwoPassive`；`Slots_RejectInvalidLoadout_AndKeepExisting`（API：PUT 無效 → 400，GET 仍是原欄位）；`Slots_ClampChanceToStep`（55 → 400 chanceOutOfRange）；`Overview_ListsCurrentJobSkillsWithNextLevel`（API）。
- [ ] `SkillService.LearnAsync` 順序：讀角色與熟練度 → `Evaluate` → `characters.UpdateAsync`（扣錢，版本鎖）→ `jobProficiency.AddAsync(…, 0, −proficiencyCost)` → `skillBook.UpsertAsync`。〔與計畫 4 相同：角色更新成功後才寫其他表〕
- [ ] Commit `✨ feat: learn and equip job skills`

---

### Task 5: 轉職（Domain＋API）

**Files:** `Domain/Jobs/JobChange.cs`、`JobProficiencyDisplay.cs`、`Application/Jobs/JobService.cs`、`Api/Endpoints/JobEndpoints.cs`、`Contracts.cs`；測試 `Domain/Jobs/JobChangeTests.cs`、`Application/JobServiceTests.cs`、`Api/JobEndpointsTests.cs`

**Interfaces:**

```csharp
public sealed record JobChangeCheck(bool Eligible, IReadOnlyList<string> Reasons);   // 代碼：sameJob、levelTooLow、parentProficiencyTooLow、statTooLow:<stat>
public static class JobChange
{
    public static JobChangeCheck Evaluate(Character c, JobDefinition target, IReadOnlyList<JobProficiency> proficiency);
    public static Character Apply(Character c, JobDefinition target);   // JobId 換掉，其餘不變
}
public static class JobProficiencyDisplay { public const long Full = 3500; public static int Percent(long proficiency) => (int)Math.Min(100, Math.Floor(proficiency / 3500.0 * 100)); }
// JobService：ListAsync(accountId) → JobOption[]{ id, name, tier, parentId, parentName, unlockLevel, proficiencyRequired, statRequirements[], growth, current, proficiencyPercent, eligible, reasons[] }；ChangeAsync(accountId, jobId) → CharacterProfile
// API：GET /api/jobs；POST /api/jobs/change { jobId } → CharacterResponse
```

- [ ] 測試：`Evaluate_TierOne_AlwaysEligible`、`Evaluate_RequiresLevelParentProficiencyAndStats`（Lv 29／ch 3499／str 59 各自一個 reason）、`Evaluate_SameJob_Rejected`、`JobChange_ClearsPassiveSlotsKeepsActive`（Service 層：欄位 1、4 有技能 → 轉職後只剩 1）、`Percent_CapsAtHundred`（3499 → 99、3500 → 100、7000 → 100）、`Jobs_ListMarksCurrentAndEligibility`（API）、`Change_Returns400_WhenIneligible`（API，reasons 在 ProblemDetails `extensions.reasons`）。
- [ ] `JobService.ChangeAsync` 順序：`Evaluate` → `characters.UpdateAsync` → `skillLoadout.ReplaceAsync` 只保留 1～3。
- [ ] Commit `✨ feat: change jobs under the WOG requirements`

---

### Task 6: 戰鬥中施放技能（Domain）

**Files:** `Domain/Battle/BattleModifiers.cs`、`SkillCaster.cs`、`WogBattle.cs`、`RoundResolver.cs`、`BattleEvents.cs`、`CombatantState.cs`（加 `Str`、`Smart` 供戰神之怒／治癒）；測試 `Domain/Battle/SkillCasterTests.cs`、`WogBattleSkillTests.cs`

**Interfaces:**

```csharp
public sealed record EquippedSkill(SkillDefinition Skill, int Level, int Chance, int Slot);
public sealed record BattleModifiers(double At = 1, double Mat = 1, double Df = 1, double Mdf = 1, double Agi = 1, int FlatAt = 0, int Rag = 0, double CritChanceBonus = 0, int BonusDamage = 0, double PhysicalTaken = 1, int PhysicalHits = 1, int MagicHits = 1, double EnemyAgi = 1, double EnemyAttack = 1, int StunChance = 0);
public static class WogBattle { public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IReadOnlyList<EquippedSkill> skills, IRandomSource random); }   // 原簽章保留為 skills: [] 的多載
```

規則：每回合開頭先讓到期技能失效並重算修正值（快照＝裸 `CombatantState`，修正值由仍生效的技能依欄位順序重套，所以同技能不會乘兩次）；玩家出手前依欄位 1→5：`Sp ≥ sp && rand(1,100) ≤ chance && 未生效` → `Sp −= sp`、`SkillEvent`；`Heal` 立即回復（不超過 HpMax，`SkillEvent.Heal` 記實際值）；`Stun` 生效期間怪物出手前擲 `rand(1,100) ≤ 3·lv` → 跳過並產生 `AttackEvent(Stunned: true)`；`Bloodthirst` 在玩家物理命中後：`rand ≤ chance` → 回復 round(dmg×0.02·lv) 並產生 `SkillEvent`；連打每下各一個 `AttackEvent`，各自擲命中／暴擊。命中公式改為 `agi/(agi+敵agi)×100 + rag + 20 > rand`；暴擊率加 `CritChanceBonus`；傷害 `WogDamage.Fight(attack, defense, element, random, defenseMultiplier: 1, bonusDamage: BonusDamage)`；玩家受物傷再乘 `PhysicalTaken`（四捨五入，最少 1 若原傷害 > 0）。

- [ ] 測試（ScriptedRandom，手算寫註解）：`Caster_CastsInSlotOrder_AndDeductsSp`；`Battle_SkipsSkillWhenSpInsufficient`；`Battle_BuffExpiresAndDoesNotStack`（爆氣 lv5 持續 3 回合：第 1～3 回合 at 105%，第 4 回合 100%；第 2 回合再擲中也不再加成）；`Battle_ComboHitsLevelPlusOneTimes`；`Battle_HealCapsAtHpMax`；`Battle_StunSkipsMonsterTurn`；`Battle_BloodthirstHealsAfterPhysicalHit`；`Battle_CounterReducesPhysicalDamageTaken`；`Battle_WarGodWrathLowersHitChance`；`Battle_WeakenLowersMonsterAttack`；`Battle_WithoutSkills_IsUnchanged`（固定種子下與舊簽章結果完全相同）。
- [ ] Commit `✨ feat: cast equipped skills during WOG battles`

---

### Task 7: 模擬與推進串接

**Files:** `Domain/Adventure/AdventureSimulator.cs`、`AdventureRun.cs`、`Application/Adventure/AdventureService.cs`、`Api/Endpoints/Contracts.cs`；測試 `Domain/Adventure/AdventureSimulatorTests.cs`、`Application/AdventureServiceTests.cs`、`Api/AdventureEndpointsTests.cs`

**Interfaces:**
- `AdventureSimulator.Run(..., IReadOnlyList<EquippedSkill> skills, ...)`（在 `stage` 之後加參數）；`AdventureService` 讀 `ISkillLoadoutRepository`＋`ISkillBookRepository` 組 `EquippedSkill`（欄位指向的已學等級）；SP 每場延續並寫回；住宿回滿 SP。`AdvanceSummary` 加 `int SkillsCast`。

- [ ] 測試：`Adventure_CarriesSpBetweenFights`（第一場扣 SP，第二場從剩餘開始）；`Advance_UsesEquippedSkills`（API：學 burst、裝到欄 1 發動率 100 → 推進後事件含 `skill`，`summary.skillsCast > 0`）；`Advance_WithoutSkills_StillWorks`。
- [ ] Commit `✨ feat: run adventures with the equipped skill loadout`

---

### Task 8: 前端：技能分頁、戰報與舞台

**Files:** `api/schema.ts`（重新產生）、`api/client.ts`（jobs、skills、learn、setSlots、changeJob）、`game/types.ts`、`game/skills.ts`（欄位編輯純函式：`setSlot`、`clearSlot`、`stepChance`、`validateLoadout` 鏡射後端代碼）、`game/battleLog.ts`、`game/stageModel.ts`、`render/effects.ts`／`StageRenderer.ts`、`ui/SkillsScreen.tsx`（+css）、`ui/AdventureScreen.tsx`（開放「技能」分頁，背包仍關閉）、`ui/CharacterScreen.tsx`（職業熟練度顯示 %）、`ui/CreateCharacterScreen.tsx`（改讀 content growth，刪 `jobBlurbs.ts`）；測試對應 `*.test.ts(x)`

**文案：**

| 事件 | 文字 |
|---|---|
| skill（玩家） | `你施展了{技能}！`（trigger）；`Heal > 0` 另一行 `回復了 {n} 點 HP。` |
| attack（Stunned） | `{怪物}暈眩中，無法行動。`（muted） |
| 連打第 n 下 | 與一般攻擊相同文案 |
| 技能分頁 | 職業卡：`{名稱}（{階}）`、需求列 `Lv {unlockLevel}`、`{父職業}熟練度 {percent}%／100%`、`{能力} ≥ {n}`，達標綠、未達標 crimson-text；目前職業標「目前」；按鈕「轉職」（不合格 disabled，title 列原因）。技能卡：`{名稱} Lv {level}/5`、效果說明、下一級 `SP {sp}・{rounds} 回合・{money} 金錢・{proficiency} 熟練度`、按鈕「學習」／「升級」。欄位：`主動 1～3`（發動率 stepper 10～100）、`被動 4～5`、按鈕「儲存欄位」。 |
| 轉職完成 | 頂欄職業名立即更新；戰報 muted 一行 `你轉職為{職業}。`（前端本地訊息） |

舞台：技能施放時角色閃青光（`palette.cyan`）並顯示技能名牌；暈眩時怪物上方顯示「暈眩」名牌（muted）。

- [ ] 測試：`battleLog` 技能／暈眩各一案例；`stageModel` skill→plate、stunned→plate；`skills.test.ts`（stepChance 邊界、validateLoadout 代碼）；`SkillsScreen.test.tsx`（列出職業與原因、學習按鈕呼叫 API、欄位儲存送出 PUT）；`CreateCharacterScreen` 顯示 content growth（`成長：力量 2～3`）。
- [ ] 手動驗收（控制者）：建角 → 技能分頁學爆氣、裝欄 1 發動率 100 → 冒險看到「你施展了爆氣！」與名牌；職業卡顯示未達標原因；截圖 `plan-5-skills.png`。
- [ ] Commit `✨ feat: add the skills tab and narrate skill casts`

---

## 自我檢查

- Spec §5.1 職業樹（content 已有）、§5.2 轉職條件（Task 5）、§5.3 成長表＋期望表（Task 1）、§5.4 熟練度（計畫 4＋Task 4 扣 `sk`）、§6 技能學習／裝備／施放／目錄（Task 2、4、6、7）、§7.1 HP 門檻自動住宿（Task 1）。裝備卸下留到計畫 7；武器第 6 欄留到計畫 7；其餘技能後續。
- 型別一致：`EquippedSkill` 在 Task 6 定義、Task 7 消費；`SkillSlot`／`LearnedSkill` 在 Task 3 定義、Task 4／5／7 消費；`SkillEvent` 與 `AttackEvent.Stunned` 在 Global Constraints 定義、Task 6 產生、Task 8 顯示。
