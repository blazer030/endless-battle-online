# 計畫 4：WOG 戰鬥與成長 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把計畫 1 的「秒數時間軸、攻擊力／防禦力」臨時戰鬥換成 WOG 的回合制戰鬥與成長：先攻、命中、暴擊、屬性相剋、40 回合、經驗金錢公式、職業熟練度、升級成長、行動力、死亡與住宿，並能離線結算。玩家會看到 Lv 上升、能力值隨職業成長、行動力耗盡時收益減半。

**Architecture:** Domain 新增 `Battle/`（回合制引擎，純函式，可指定種子）、`Progression/`（經驗曲線、升級成長、熟練度）、`Stamina/`（行動力）、`Rest/`（住宿）。`AdventureSimulator` 改為「依時間軸連續進行 WOG 戰鬥」：每場戰鬥瞬間算完，產生逐回合事件，事件時間以固定節奏（每回合 0.8 秒、場間 2 秒或 12 秒）排在時間軸上；伺服器一律完整模擬（WOG 一場最多 40 回合，計算極便宜），只對最後 30 秒輸出逐回合事件，其餘壓成摘要。前端播放模型不變，戰報與舞台改讀新事件。

**Tech Stack:** 沿用。專案已改名為 `EndlessBattle.*`。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §3.3、§3.4、§4、§7（旅館）、§7.1、§11；研究依據 `docs/research/wog-research-battle.md`、`wog-research-stats-jobs.md` §3。

## Global Constraints

- 計畫 1～3 的 Global Constraints 全部適用。每個任務完成後都要經過 **test-runner**（獨立跑 `./scripts/check.sh`）與 **reviewer**。〔使用者要求〕
- 公式一字不差照 spec §4（與研究報告一致）。整數截斷、`rand(a,b)` 兩端含、`a > b` 時對調，都照 PHP 行為；`round` 用 AwayFromZero。
- 亂數一律經 `IRandomSource`；戰鬥與結算可用固定種子重現。
- 事件型別（取代計畫 1 的四種）：

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(EncounterEvent), "encounter")]
[JsonDerivedType(typeof(RoundEvent), "round")]
[JsonDerivedType(typeof(AttackEvent), "attack")]
[JsonDerivedType(typeof(DefeatEvent), "defeat")]
[JsonDerivedType(typeof(TimeoutEvent), "timeout")]
[JsonDerivedType(typeof(RewardEvent), "reward")]
[JsonDerivedType(typeof(LevelUpEvent), "levelUp")]
[JsonDerivedType(typeof(RestEvent), "rest")]
public abstract record BattleEvent(double AtSeconds);
public sealed record EncounterEvent(double AtSeconds, string MonsterId, int MonsterHp, int MonsterHpMax, int PlayerHp, int PlayerHpMax, int PlayerSp, int PlayerSpMax, bool StaminaDepleted) : BattleEvent(AtSeconds);
public sealed record RoundEvent(double AtSeconds, int Round) : BattleEvent(AtSeconds);
public sealed record AttackEvent(double AtSeconds, CombatSide Attacker, AttackKind Kind, bool Hit, bool Critical, int Damage, double ElementMultiplier, int TargetHpAfter, string? SpecialAttackName) : BattleEvent(AtSeconds);   // Kind: Physical | Magic；未命中 Damage 0
public sealed record DefeatEvent(double AtSeconds, CombatSide Loser) : BattleEvent(AtSeconds);
public sealed record TimeoutEvent(double AtSeconds, int Rounds, int DamageDealt) : BattleEvent(AtSeconds);
public sealed record RewardEvent(double AtSeconds, long Exp, long Money, long JobExp) : BattleEvent(AtSeconds);
public sealed record LevelUpEvent(double AtSeconds, int NewLevel, BaseStats Growth) : BattleEvent(AtSeconds);
public sealed record RestEvent(double AtSeconds, long Cost, bool ExpPenalty, double WaitSeconds) : BattleEvent(AtSeconds);
```

- 時間節奏常數（Domain `Battle/BattleTiming.cs`）：`SecondsPerRound = 0.8`、`FightIntervalSeconds = 2`、`FightIntervalWhenDepletedSeconds = 12`、`RestWaitSeconds = 5`、`RestWaitWhenDeadSeconds = 15`。
- 行動力：上限 50，每 180 秒 +6；每場 −1；`act ≤ 0` 時 `f_down = 0.5`（經驗、金錢、熟練度 ×0.5，掉率分母 ×1.5）且場間 12 秒；`act == 0` 那一場仍全額（WOG 原規則）。
- 推進 API：模擬到「現在」為止的所有戰鬥；事件只輸出最後 `CatchUpWindowSeconds = 30` 秒的場次，其餘以 `AdvanceSummary(fights, wins, losses, exp, money, jobExp, levelsGained)` 回傳；單次推進最多模擬 24 小時。〔Claude 的判斷：取代 v1 的「統計估算」，因 WOG 戰鬥計算極便宜〕
- 補給品、掉落、技能在計畫 5、7 加入；本計畫的 `AttackEvent.SpecialAttackName` 只在玩家暴擊時填入。
- 從計畫 3 帶來：戰鬥從 `character.Hp`／`Sp` 開始並寫回；`RewardEvent`、`AdvanceResult`、`MonsterDefinition` 改用 exp／money 命名；戰報文案去仙俠味；暴擊顯示必殺技名稱；`BadHttpRequestException` 映射改用 `exception.StatusCode`。
- 程式碼註解中文、非必要不寫；commit 規則同前。

## Review Focus

- **40 回合超限**：雙方都很耐打時，第 40 回合後結束，給部分經驗、無金錢、不計敗場；不會無限迴圈。→ Task 2 `Battle_EndsAfterFortyRounds_WithPartialExp`。
- **死亡後必須住宿**：HP 0 的角色下一場開打前會先住宿（Lv ≤ 100 免費回滿、等待 15 秒），住宿事件出現在時間軸。→ Task 5 `Adventure_RestsBeforeNextFight_WhenDead`。
- **行動力耗盡**：連續掛機超過回復速度後，獎勵減半、場間 12 秒，`EncounterEvent.StaminaDepleted = true`；回復後恢復全額。→ Task 4 `Stamina_DepletedHalvesRewardsAndSlowsFights`。
- **長時間離線**：離線 6 小時後推進，回應在 1 秒內、事件只含最後 30 秒、摘要正確加總、Lv 與能力值都已成長、`LastSettledAt` 等於現在。→ Task 5 `Advance_SixHours_ReturnsSummaryQuickly`。
- **升級跨多級**：一場獲得的經驗足夠連升兩級時，`nextExp` 先扣再算，成長各擲一次，事件兩個。→ Task 3 `LevelUp_AppliesMultipleLevelsInOrder`。

---

## File Structure

```
backend/
  db/ddl/V000000004__add_stamina_and_job_proficiency.sql
  content/monsters.json（改為 WOG 欄位）、content/stages.json（不變）
  EndlessBattle.Domain/Battle/CombatantState.cs, AttackKind.cs, ElementTable.cs, WogDamage.cs, RoundResolver.cs, WogBattle.cs, BattleTiming.cs, BattleEvents.cs（取代 Combat/BattleEvents.cs）
  EndlessBattle.Domain/Progression/ExperienceCurve.cs, RewardFormulas.cs, LevelUp.cs, JobProficiency.cs
  EndlessBattle.Domain/Stamina/Stamina.cs
  EndlessBattle.Domain/Rest/Hotel.cs
  EndlessBattle.Domain/Adventure/AdventureSimulator.cs（重寫）, AdventureRun.cs（擴充）
  EndlessBattle.Domain/Content/MonsterDefinition.cs（WOG 欄位）
  EndlessBattle.Domain/Characters/Character.cs（ActNum, ActNumTime）、CombatBridge.cs 刪除
  EndlessBattle.Application/Characters/IJobProficiencyRepository.cs, Adventure/AdventureService.cs（重寫）, Adventure/AdvanceResult.cs
  EndlessBattle.Infrastructure/Persistence/JobProficiencyRepository.cs（+ scaffold）
  EndlessBattle.Api/Endpoints/Contracts.cs, AdventureEndpoints.cs, CharacterEndpoints.cs（ActNum）
frontend/src/
  game/types.ts, battlePlayback.ts（摘要欄位）, battleLog.ts（新文案）, stageModel.ts（新事件）
  render/StageRenderer.ts, effects.ts（miss、magic、levelUp 演出）
  ui/AdventureScreen.tsx（行動力、Lv 提升提示）, CharacterScreen.tsx（HP/SP 真實值）
```

---

### Task 1: 怪物 WOG 欄位、行動力與熟練度資料層

**Files:** migration V4、`monsters.json` 兩份、`MonsterDefinition.cs`、`JsonGameContent`（驗證）、`Character.cs`（`ActNum`、`ActNumTime`）、`IJobProficiencyRepository`／`JobProficiencyRepository`、scaffold、repository 測試

**Interfaces:**

```sql
-- V000000004__add_stamina_and_job_proficiency.sql
ALTER TABLE characters
    ADD COLUMN act_num INT NOT NULL DEFAULT 50,
    ADD COLUMN act_num_time DATETIME(6) NULL;
CREATE TABLE character_job_proficiency (
    character_id BIGINT      NOT NULL,
    job_id       VARCHAR(32) NOT NULL,
    proficiency  BIGINT      NOT NULL DEFAULT 0,   -- ch_<job>
    skill_points BIGINT      NOT NULL DEFAULT 0,   -- sk_<job>
    PRIMARY KEY (character_id, job_id),
    CONSTRAINT fk_job_proficiency_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
```

```csharp
public sealed record MonsterDefinition(string Id, string Name, int Level, int Hp, int Sp, int At, int Mat, int Df, int Mdf, int Agi, int Au, int Element, long JobExp, int Meet);
// monsters.json: { "id","name","level","hp","sp","at","mat","df","mdf","agi","au","element","jobExp","meet" }
// 正式內容（冷峰寒地・一）：snow-sprite Lv3 hp 60 sp 10 at 14 mat 8 df 6 mdf 4 agi 9 au 6 element 2 jobExp 12 meet 0；
//                           frost-wolf  Lv5 hp 95 sp 10 at 20 mat 6 df 9 mdf 5 agi 13 au 7 element 2 jobExp 18 meet 30
// 測試內容：test-sprite = snow-sprite 數值、test-wolf = frost-wolf 數值（名稱同前）
public sealed record JobProficiency(string JobId, long Proficiency, long SkillPoints);
public interface IJobProficiencyRepository
{
    Task<IReadOnlyList<JobProficiency>> ListAsync(long characterId, CancellationToken ct);
    Task AddAsync(long characterId, string jobId, long proficiencyDelta, long skillPointsDelta, CancellationToken ct);   // upsert 累加
}
```

- 驗證：`hp > 0`、`agi > 0`（命中公式分母）、`element` 0～6、`meet` 0～100、`level ≥ 1`。
- `Character` 新增 `ActNum`（預設 50）、`ActNumTime`（可空）。

- [ ] Steps: 測試（內容驗證、repository upsert 累加、角色欄位往返）→ migration + compose + scaffold → 實作 → `./scripts/check.sh` → 兩個 commit（`🚧 chore: add stamina columns and job proficiency table`、`✨ feat: describe monsters with WOG stats`）。

---

### Task 2: WOG 回合制戰鬥引擎（Domain）

**Files:** `Battle/*`（上列）、刪除 `Combat/DamageCalculator.cs`、`Combat/BattleSimulator.cs`、`Combat/BattleResult.cs`、`Combat/CombatStats.cs`（`CombatSide` 保留搬到 `Battle/`）；測試 `Domain/Battle/*Tests.cs`

**Interfaces:**

```csharp
public enum AttackKind { Physical, Magic }
public sealed record CombatantState(int Hp, int HpMax, int Sp, int SpMax, int At, int Mat, int Df, int Mdf, int Agi, int Au, int Element, string? SpecialAttackName);
public static class ElementTable { public static double Multiplier(int attackerElement, int defenderElement); }   // 0 → 1.0；diff −1 或 +5 → 1.2；+1 或 −5 → 0.8；其餘 1.0
public static class WogDamage
{
    public static int Fight(int attack, int defense, double elementMultiplier, IRandomSource random, double defenseMultiplier = 1, int bonusDamage = 0);
    // d = (A·(A/D) − D)·s；≤0 → 1；d += rand(⌊d/A/3⌋, ⌊d/A/2⌋)；round(d·def + bonus)；A、D 最小 1
}
public sealed record BattleOutcome(IReadOnlyList<BattleEvent> Events, CombatSide? Winner, int Rounds, int DamageDealtToMonster, CombatantState Player, CombatantState Monster);
public static class WogBattle
{
    public const int MaxRounds = 40;
    public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred, IRandomSource random);
}
```

規則（spec §4）：先攻 70/30；玩家攻擊方式：指定則用指定，否則 `at ≥ mat` 物理、否則 80% 魔法；怪物每回合 `m_type`；物理命中 `agi/(agi+敵agi)×100 + 20 > rand(1,100)`；魔法必中；暴擊：基礎 10%／×1.2，攻方 agi（魔法 au）較高時 `min(20, 比值×10)`、`min(比值×1.2, 2)`；暴擊判定 `rand(1,100) < cri`；傷害 `WogDamage.Fight`；HP 不低於 0；回合事件順序：`RoundEvent` → 先攻者 `AttackEvent` →（死亡則 `DefeatEvent` 結束）→ 後攻者 → …；第 40 回合結束仍無人倒下 → `TimeoutEvent`。事件 `AtSeconds` 由回合數 × `SecondsPerRound` 決定（同回合內兩次攻擊各 +0.4）。

- [ ] 測試（用 `ScriptedRandom` 控制每一次擲骰，預期值手算並寫在註解）：`ElementTable` 全 7×7 表（Theory）；`WogDamage.Fight` 四個案例（A=100 D=50、A<D、含 bonus、含 defenseMultiplier）；`Battle_PlayerStrikesFirst_WhenRollBelowSeventy`；`Battle_PhysicalMiss_DealsZero`；`Battle_MagicAlwaysHits`；`Battle_CritUsesAgiRatio_CappedAtTwenty`；`Battle_EndsAfterFortyRounds_WithPartialExp`（只驗 Timeout 事件與 DamageDealt）；`Battle_EventsAreOrderedByRound`。
- [ ] 實作 → `./scripts/check.sh`（計畫 1 的舊戰鬥測試在本任務刪除；`AdventureSimulator` 暫時編譯不過的話，先用 `#if false` 不行——改為在本任務把 `AdventureSimulator`／`AdventureService` 的呼叫改成最小橋接：每場 `WogBattle.Run`，獎勵暫時 0；Task 5 再完整重寫。在報告中說明。）
- [ ] Commit `✨ feat: resolve fights with the WOG round-based battle rules`

---

### Task 3: 經驗、金錢、升級、熟練度（Domain）

**Files:** `Progression/*`；測試

**Interfaces:**

```csharp
public static class ExperienceCurve { public static long NextExpFor(int level) => 6L*level*level + 1080L*level; }   // Lv1 的 nextExp 固定 1000（創角寫定）
public static class RewardFormulas
{
    public static long Exp(MonsterDefinition m, int monsterHpRemaining, int playerLevel, bool killed, double fDown, IRandomSource random);
    public static long Money(MonsterDefinition m, double fDown, IRandomSource random);
    public static long JobExp(MonsterDefinition m, double fDown) => (long)Math.Floor(m.JobExp * fDown);
}
public sealed record LevelUpResult(Character Character, IReadOnlyList<LevelUpEvent> Events);
public static class LevelUp { public static LevelUpResult Apply(Character character, JobDefinition job, IRandomSource random, double atSeconds); }   // while exp > nextExp
```

- 經驗公式照 spec §4（含 `plv ≤ 100 ×1.3`、`mlv ≥ 450 ×0.6`、`plv ≤ 15 ×2.5`、`×2`、`×fDown`、未擊殺再乘血量比）；超限時呼叫端傳 `damageDealt/1.5` 當 `monsterHpRemaining`、`m.Level×0.6` 當等級（由 Task 5 的呼叫端處理，`Exp` 多一個 `levelOverride` 參數）。
- 金錢公式照 spec §4；PHP `rand` 的浮點參數截斷、`a > b` 對調。
- 升級：`while exp > nextExp`（嚴格大於）：`level++`、`exp -= nextExp`、`nextExp = NextExpFor(level)`、七項各 `rand(min,max)` 加到 `Base`；HP／SP 不回滿；每級一個 `LevelUpEvent`。

- [ ] 測試：`NextExp_MatchesCurve`（Lv2 2184、Lv10 11400、Lv50 69000、Lv100 168000）；`Exp_KilledSnowSpriteAtLevelOne`（手算：base (14+8)/2=11；rand(11,14) 用 ScriptedRandom 取 11；×1.3=14.3；×3×14.3×(3/1)=128.7；×0.18=23.166；×2.5=57.9；×2=115.8 → round 116，fDown 1）；`Exp_HalvesWhenStaminaDepleted`；`Money_SwapsRangeWhenInverted`；`LevelUp_AppliesMultipleLevelsInOrder`；`LevelUp_UsesJobGrowthRanges`；`JobExp_FloorsAfterFDown`。
- [ ] Commit `✨ feat: add WOG experience, money and level-up rules`

---

### Task 4: 行動力與住宿（Domain）

**Files:** `Stamina/Stamina.cs`、`Rest/Hotel.cs`；測試

```csharp
public sealed record StaminaState(int ActNum, DateTimeOffset? LastRegenAt);
public static class Stamina
{
    public const int Max = 50, RegenAmount = 6, RegenSeconds = 180;
    public static StaminaState Regenerate(StaminaState s, DateTimeOffset now);          // 照 WOG：temp=(now-last)/180；≥1 +6；≥2 再 +6；上限 50；last=now（首次 null 視為 now）
    public static (StaminaState State, double FDown, double IntervalSeconds) Consume(StaminaState s);   // act−1；act<0 → fDown 0.5、12 秒；act==0 → fDown 1、12 秒；否則 1、2 秒
}
public sealed record HotelResult(Character Character, long Cost, bool ExpPenalty, double WaitSeconds);
public static class Hotel { public static HotelResult Rest(Character c); }   // spec §7：Lv ≤ 100 免費；否則 round(10×lv×係數) 下限 10×lv；付不起 exp×0.8；等待 5／死亡 15 秒；hp/sp 回滿
```

- [ ] 測試：`Stamina_RegeneratesSixPerThreeMinutes_CappedAtFifty`；`Stamina_DepletedHalvesRewardsAndSlowsFights`；`Hotel_IsFreeAtOrBelowLevel100`；`Hotel_ChargesByMissingHp`（Lv 120、hp 30/1000 → 係數 1.8 → 2160）；`Hotel_TakesExpPenalty_WhenBroke`；`Hotel_WaitsFifteenSecondsWhenDead`。
- [ ] Commit `✨ feat: model stamina regeneration and hotel rest`

---

### Task 5: 冒險模擬與推進 API（Application／Api）

**Files:** `AdventureSimulator.cs`（重寫）、`AdventureRun.cs`、`AdventureService.cs`、`AdvanceResult.cs`、`Contracts.cs`、`AdventureEndpoints.cs`、`CharacterEndpoints.cs`（回應加 `actNum`、`actNumMax`）；測試 `Application/AdventureServiceTests.cs`（重寫）、`Api/AdventureEndpointsTests.cs`（重寫）、`Domain/Adventure/AdventureSimulatorTests.cs`（重寫）

**Interfaces:**

```csharp
public sealed record AdventureRun(IReadOnlyList<BattleEvent> Events, AdvanceSummary Summary, Character Character, StaminaState Stamina, IReadOnlyList<(string JobId, long Proficiency, long SkillPoints)> JobGains, double ConsumedSeconds);
public sealed record AdvanceSummary(int Fights, int Wins, int Losses, int Timeouts, long Exp, long Money, long JobExp, int LevelsGained);
public static class AdventureSimulator
{
    public const double CatchUpWindowSeconds = 30;
    public const double MaxSimulatedSeconds = 86_400;
    public static AdventureRun Run(Character character, JobDefinition job, StageDefinition stage, Func<string, MonsterDefinition> monsters, StaminaState stamina, DateTimeOffset from, DateTimeOffset to, IRandomSource random);
}
```

時間軸演算法：
```
cursor = from；events 只保留 AtSeconds ≥ (to − from) − CatchUpWindowSeconds 的場次（整場為單位）
loop while cursor < to:
    stamina = Regenerate(stamina, cursor)
    if hp == 0: HotelResult r = Hotel.Rest(...)；RestEvent；cursor += r.WaitSeconds；continue
    (stamina, fDown, interval) = Consume(stamina)
    monster = 依 meet 篩選後均勻抽（rand(0,100) 一次 + 均勻）
    outcome = WogBattle.Run(player 狀態來自 character 的 hp/sp 與衍生值, monster, null, random)
    依結果：勝 → RewardEvent(exp, money, jobExp)；升級 → LevelUpEvent(s)；敗 → hp=0、losses++；超限 → 部分 exp、timeouts++
    寫回 character 的 hp/sp/exp/money/level/base；熟練度累加
    cursor += outcome.Rounds × SecondsPerRound + interval
Summary 累加全部；LastSettledAt = min(to, cursor)（最後一場打完即可超過 to，同計畫 1 的規則）
```
- `AdventureService.AdvanceAsync`：`elapsed = now − LastSettledAt`，上限 24 小時（超過的部分捨棄，`LastSettledAt` 設為 now − 24h 後再模擬）；寫回角色（含 ActNum/ActNumTime）與熟練度；回應 `AdvanceResponse(Events, Summary, Character)`。
- 新角色第一次推進前 `ActNumTime` 為 null → 視為現在。

- [ ] 測試：`Adventure_RestsBeforeNextFight_WhenDead`；`Advance_SixHours_ReturnsSummaryQuickly`（FakeClock 前進 6 小時；事件 AtSeconds 全部 ≥ 總長 − 30；Summary.Fights > 1000；回應時間 < 1 s 用 Stopwatch 斷言 < 2000 ms 以防慢機器）；`Advance_PersistsHpAndLevel`；`Advance_RecordsJobProficiency`（`GET /api/characters/me` 回傳 `jobProficiency[]`）；`Advance_CapsAtTwentyFourHours`；既有的 409／時鐘倒退／空推進測試保留並改寫。
- [ ] 手動驗證：curl 建角、等 30 秒推進，看到 round/attack/reward 事件與 Summary。
- [ ] Commit `✨ feat: simulate WOG adventures with stamina, rest and level-ups`

---

### Task 6: 前端：新事件的戰報、舞台與頂欄

**Files:** `game/types.ts`（重新產生 schema）、`battlePlayback.ts`（summary 改用伺服器的 `AdvanceSummary`：回應已含摘要，前端不再自行壓縮——`CatchUpThresholdSeconds` 邏輯移除，`summary` 項目由回應的 `summary.fights > 0 且事件不涵蓋全部` 判斷：伺服器另回傳 `eventsCoverAll: bool`）、`battleLog.ts`（新文案）、`stageModel.ts`、`effects.ts`／`StageRenderer.ts`、`AdventureScreen.tsx`、`CharacterScreen.tsx`、測試

**文案**（取代仙俠用語）：

| 事件 | 文字 |
|---|---|
| encounter | `遭遇 {妖怪}！`（StaminaDepleted 時另一行 `行動力耗盡，報酬減半。`，kind muted） |
| round | 不顯示 |
| 玩家物理命中 | `你攻擊 {妖怪}，造成 {傷害} 點傷害。` |
| 玩家魔法命中 | `你施放魔法，{妖怪}受到 {傷害} 點傷害。` |
| 玩家暴擊 | `{必殺技名}！{妖怪}受到 {傷害} 點傷害！`（trigger） |
| 玩家未命中 | `你的攻擊落空了。`（muted） |
| 怪物命中／暴擊／未命中 | `{妖怪}攻擊，你受到 {傷害} 點傷害。`／`{妖怪}重擊！你受到 {傷害} 點傷害！`／`{妖怪}的攻擊落空了。` |
| defeat（怪） | `{妖怪}倒下了。` |
| defeat（玩家） | `你倒下了……`（muted） |
| timeout | `戰鬥陷入膠著，雙方收手。`（muted） |
| reward | `獲得經驗 {exp}、金錢 {money}、熟練度 {jobExp}。`（drop） |
| levelUp | `升級！Lv {n}（力量+{a} 智力+{b} 生命+{c} 體質+{d} 敏捷+{e} 魅力+{f} 信仰+{g}）`（trigger，0 的省略） |
| rest | `你在旅館休息，恢復了體力。`／`付了 {cost} 金錢住宿。`／`付不起住宿費，經驗減少了。`（muted） |
| summary | `離開期間戰鬥 {fights} 場（勝 {wins}）、獲得經驗 {exp}、金錢 {money}，升了 {levels} 級。` |

舞台：未命中顯示「MISS」飄字（text-muted）；魔法攻擊用青色光束取代劍氣；升級時角色閃金光並顯示「LEVEL UP」牌；住宿時舞台淡出淡入。頂欄加「行動力 x/50」。角色畫面 HP/SP 顯示真實值。

- [ ] 測試：`battleLog` 每列一案例；`stageModel` 新事件對應；`AdventureScreen` 顯示行動力與升級提示；`battlePlayback` 摘要改為來自回應。
- [ ] 手動驗收（控制者）：掛機 1 分鐘看到回合節奏、MISS、暴擊必殺技名、升級；離開 3 分鐘回來看到摘要。截圖 `plan-4-battle.png`。
- [ ] Commit `✨ feat: narrate and render WOG round-based battles`
