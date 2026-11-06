## Context

`WogBattle.Run` 只替玩家建一個 `SkillCaster`，怪物每次出手只有普通攻擊。`SkillCaster` 本身已經為雙方預留：`money` 為 null 表示怪物不扣金錢、`Silence(rounds)` 是給對手呼叫的入口；但整場戰鬥沒有人呼叫 `Silence`，`BattleModifiers.SilenceChance` 也沒有人讀，所以 `Silence` 效果目前沒有作用。

原版原始碼（`/Users/ethan/Project/WOG4.5_Releas_CT/class`）核對結果：

| 項目 | 原版 | 出處 |
|---|---|---|
| 怪物發動率 | `time_1～3` 直接當 `stime`，條件是 `SP 足夠` 且 `rand(1,100) <= stime` 且技能沒有持續中 | `wog_fight_select.php` `m_set_skill`、`wog_fight_m.php` `skill_use` |
| 武器附帶技能 | 第 6 欄：`stime = time_6`、`sp = 0` | `wog_fight_select.php` `p_set_skill` |
| 兩段機率 | 暈擊、假死的機率在技能函式裡另外擲，所以實際機率是「發動率 × 效果機率」 | `wog_fight_skill.php` `skill_19`、`skill_32` |
| 假死 | 自己的智力比對手高時機率 +5，否則 −5（相等也算 −5）；說明寫的「比敵人低」與程式不完全相同，以程式為準 | `skill_32` |
| 怪物第 4、5 格 | 引用未定義的 `$p`，沒有發動率，等於不會發動 | `m_set_skill` |
| 怪物的技能等級 | 寫成字串 `"3LV3"`，PHP 轉數字仍是 3，實際沒有錯 | `m_set_skill` |

資料統計（2026-11-06 用 `docs/research/wog45/data` 與 `backend/content` 量測）：已匯入的 391 隻怪有 390 隻帶技能，共 576 格。其中 9 格是假死、3 格是常駐的燃燒魔力，其餘 564 格的 `Chance` 都是預設的 `min(100, 30 + 10·lv)`。8 隻怪有同一技能重複兩格。沒有發動率的格子都在未匯入的怪上。附帶技能的武器 36 件都已匯入，12 種技能，其中 3 件是寵物技能戰意驅使。

硬規則：
- 遊戲內容只放 `backend/content/*.json`：怪物技能與武器附帶技能都寫在內容檔；測試內容放 `TestContent/`。
- 遊戲規則只寫在 Domain：雙方施放、發動率替換都在 Domain；Api 只把附帶技能轉成回應。
- `frontend/src/api/schema.ts` 是產生檔：要用 `pnpm generate:api` 重新產生，API 要在 5062 執行。
- 資料表、scaffold、admin 路由：不涉及。

## Goals / Non-Goals

**Goals:**

- 怪物與玩家用同一份 `SkillCaster` 規則施放，所有效果雙向作用
- 沒有技能的怪物擲骰序列完全不變，現有依固定亂數的測試不需要改
- 怪物與武器的發動率、等級都來自內容檔，可以重複匯入

**Non-Goals:**

- 裝備回復 HP／SP、寵物技能、組隊技能
- 調整數值與平衡，修改 `BalanceTests` 的門檻
- 平衡報告加武器附帶技能、admin 編輯 `monster-skills.json`

## Decisions

### 怪物用第二個 SkillCaster，修正值雙向合成

`WogBattle.Run` 多一個 `monsterSkills` 參數，建立第二個 `SkillCaster(monster, monsterSkills, player, money: null)`。把 `SkillCaster` 內的 `player` 改名 `caster`，`BattleModifiers.ApplyToPlayer`／`ApplyToMonster` 改名 `ApplyToCaster`／`ApplyToOpponent`，這是不改行為的結構性 commit。

每回合的能力值：`fighter = monsterMods.ApplyToOpponent(playerMods.ApplyToCaster(player))`，`opponent = playerMods.ApplyToOpponent(monsterMods.ApplyToCaster(monster))`。

替代方案是另寫一份怪物專用的簡化規則，但要維護兩份規則，效果也會少一半，不採用。

### 怪物在玩家之後、同樣在回合開頭施放

現有規則是玩家在回合開頭、決定先攻之前施放。怪物接在玩家之後施放，這樣雙方的敏捷增減都會影響同一回合的先攻。

原版是各自在自己出手前施放，且暈眩中不能施放。這裡不跟：玩家早就改成回合開頭，雙方用同一個時機比較單純；代價是被暈眩的怪物仍可能在回合開頭放技能。AfterHit 與 RoundEnd 時機則各自照現有規則，玩家先、怪物後。

### 發動率由 SkillDefinition 轉換，不改施放規則

新增 `SkillDefinition.WithActivationRate(int rate)`：回傳複本，每一級移除所有 `Chance` 條件、加上一個 `Chance rate`，其他條件不動。常駐技能因此多了 `Chance`，變成要擲骰的技能，例如 3 格燃燒魔力，符合原版用發動率的行為；它們在內容裡沒有消耗，所以怪物發動時也不扣 SP。

`SkillCaster` 完全不用知道發動率，也不用多一條規則。

### 怪物技能在內容載入時解析

`monster-skills.json` 是獨立的關聯檔，沿用 `job-skills.json` 的作法。`JsonGameContent.Load` 驗證後，把解析好的 `IReadOnlyList<EquippedSkill>` 放到 `MonsterDefinition.Skills`（`init` 屬性，預設空陣列）；欄位順序就是檔案順序，每場戰鬥不需要再查表。

替代方案是把技能寫進 `monsters.json` 每筆裡，但使用者要的是關聯表式的技能資料（見 `job-skills.json`），`content-editor` 也比較好處理，不採用。

### 假死用 Stun 效果的機率調整，不改 Chance 條件

先前討論時暫定「`Chance` 條件加上比較能力後調整機率」，當時還沒寫入任何變更；核對原始碼後改成這裡的作法。

假死與暈擊同一結構：預設 `Chance` 條件加上 `Stun` 效果。`StunEffect` 多一個 `ChanceByStat`（`SkillChanceAdjustment(SkillScalingBasis Stat, int Percent)`）：自己的基礎能力比對手高時機率 +Percent，否則 −Percent；結果不大於 0 時不會暈眩。

如果放在 `Chance` 條件上，怪物的發動率會把它整個取代，就變成沒有智力比較，也沒有效果機率，與原版的兩段機率不同。假死的 rounds 是 1，`Stun` 每次出手前擲一次，等同原版只擲一次。玩家學的假死也一樣是兩段機率：第 1 級發動率 40%，然後 2%±5%。

### 鋒針陷阱的武器發動率在匯入時換算

鋒針陷阱的 `Chance`（第 2 級 18%）來自說明文字，不是預設值，發動率取代後會少一段機率。只有 3 件武器神經系毒氣受影響，不值得多一個「不被取代的 Chance」欄位。匯入腳本換算：武器與怪物的技能 `Chance` 不是預設值時，寫入的發動率是 `max(1, round(原版發動率 × Chance / 100))`，所以神經系毒氣寫 2（12 × 18% = 2.16），並記在 `gaps.md`。

### 封鎖在施放時擲骰，由 WogBattle 轉給對手

`SkillCaster` 施放後依序擲 `SilenceEffect.Chance`，擲中時把 `max(1, rounds)` 放進 `SkillCast.SilenceRounds`；`WogBattle.Record` 呼叫對手的 `Silence(rounds)`。擲骰排在效果自己的機率之後。`BattleModifiers.SilenceChance` 沒有人用，一起移除。

### 武器附帶技能是第 6 欄，不耗 SP

`ItemDefinition.WeaponSkill`（`WeaponSkillSpec(SkillId, Level, Rate)`）只有武器可以有。`EquippedSkills.LoadAsync` 多接裝備中的武器，加上一筆 `EquippedSkill(skill.WithActivationRate(rate).WithoutSkillPointCosts(), level, SkillLoadout.WeaponSlot)`，`WeaponSlot` 是 6，排在欄位 1～5 之後施放。傭兵共用同一份 `skills`，所以也會用到。

同一技能已裝在欄位 1～5 時，武器附帶的不生效，避免同一技能疊加（原版以 `main_sid` 為鍵，兩筆會互相覆蓋）。

`RestRules` 的 SP 門檻看「有沒有主動技能」，這是因為主動技能耗 SP；武器欄不耗 SP，所以 `AdventureSimulator` 判斷時排除 `WeaponSlot`。

### 同一隻怪重複的技能只留等級較高的

8 隻怪有同一技能兩格（例如盜墓者 急影 Lv2 rate 16、Lv1 rate 19）。原版以 `main_sid` 為鍵，實際是隨機其中一筆。本作匯入時留等級較高的一筆，另一筆列在 `gaps.md`；`monster-skills.json` 載入時拒絕重複。

## Implementation Contract

**內容格式**

`backend/content/monster-skills.json`（必要檔案，跟 `job-skills.json` 一樣缺檔就載入失敗）：

```json
{ "monsterSkills": [ { "monsterId": "monster-1", "skillId": "skill-1", "level": 2, "rate": 10 } ] }
```

`items.json` 的武器：

```json
{ "id": "item-1823", "weaponSkill": { "skillId": "skill-1", "level": 2, "rate": 12 } }
```

`skills.json` 的 Stun 效果：

```json
{ "kind": "Stun", "chance": 2, "chanceByStat": { "stat": "intelligence", "percent": 5 }, "rounds": 1 }
```

載入時拒絕（丟 `GameContentException`，訊息指出是哪一筆）：引用不存在的怪物或技能、等級不在 1～`maxLevel`、`rate` 不在 1～100、同一隻怪同一技能重複、`weaponSkill` 放在非武器的道具上、`chanceByStat.percent` 不在 1～100。

**戰鬥雙向合成**（`P` 是玩家修正值，`M` 是怪物修正值）

| 項目 | 玩家出手 | 怪物出手 |
|---|---|---|
| 出手前暈眩 | `M.StunChance` | `P.StunChance` |
| 連擊次數 | `P` 的 Hits | `M` 的 Hits |
| 命中修正 | `P.Rag + M.EnemyRag` | `P.EnemyRag + M.Rag` |
| 暴擊率、暴擊上限、追加傷害、屬性加成 | `P` | `M` |
| 減傷 | `M.DamageTaken` | `P.DamageTaken` |
| 攻擊屬性／防禦屬性 | `P.AttackingAs`／`M.DefendingAs` | `M.AttackingAs`／`P.DefendingAs` |

寵物的攻擊也套用 `M.DamageTaken`。

**流程與事件**

- 回合開頭：雙方 `ExpireAtRoundStart` → 喝補給 → 逃跑判定 → 玩家施放 → 怪物 HP 歸零就結束 → 怪物施放 → 玩家 HP 歸零就結束 → 決定先攻。
- 物理命中後：攻擊方 `CastAfterPhysicalHit`。寵物代擋成功時玩家沒有受傷，怪物不觸發。
- 回合結束：寵物出手 → 玩家 RoundEnd 施放 → 怪物 RoundEnd 施放 → 玩家持續傷害 → 怪物持續傷害。任何一步有人倒下就以 `DefeatEvent` 結束，勝方是對方。
- 技能造成的傷害與持續傷害不經過寵物代擋。
- `SkillEvent` 的 `Caster` 寫 `Monster`；欄位意義不變，`SpAfter` 是施放者的 SP，`OpponentHpAfter` 是對手的 HP。`AttackEvent` 的 `Attacker: Player, Stunned: true` 表示玩家被暈眩。這些都已在現有 API 形狀內，只有 `InventoryItemResponse` 新增 `weaponSkill`（`{ name, level, rate }`，沒有時為 null）。
- `SkillEvent { Caster: Monster }` 不計入摘要的施放次數，現有的計算已經只算玩家。

**前端**

- `applyToStage`：`skill` 事件依施放者更新雙方 HP；施放者 HP 為目前值 − `hpCost` + `heal`（不超過上限），對手 HP 取 `opponentHpAfter`。
- `stageEffectsFor`：怪物施放顯示青色名牌 `over: "monster"`，不播全畫面閃光；持續傷害沒有效果。玩家被暈眩顯示淡色名牌「暈眩」`over: "player"`。
- 戰報：`史萊姆施展了爆氣！`（kind `trigger`）、`史萊姆回復了 N 點 HP。`、`史萊姆回復了 N 點 SP。`、`你受到 N 點傷害。`、`你失去 N 點 SP。`、持續傷害 `爆氣持續作用，你受到 N 點傷害。`、玩家被暈眩 `你暈眩中，無法行動。`。怪物的消耗不寫進戰報。
- 背包武器明細：`附帶技能 爆氣 Lv2（發動率 12%）`。

**驗收**

- `./scripts/check.sh` 通過。
- `python3 scripts/import-wog45.py` 跑兩次，第二次 `git diff --exit-code backend/content docs/research/wog45/gaps.md` 回 0。
- 手動在 375×667 用真實內容打帶技能的怪，確認戰報與名牌、沒有水平捲動與主控台錯誤。

**範圍邊界**

範圍內：`monster-skills.json`、武器附帶技能、假死、封鎖接上戰鬥、前端呈現、匯入與 `gaps.md`。範圍外：見 Non-Goals。

## Risks / Trade-offs

- [怪物變強，既有玩家在後段地區更容易輸] → 不調數值。照先前決定只量測並記錄，留給負責平衡的人；若 `BalanceTests` 因此失敗，調整測試並在 `tasks.md` 的變更紀錄寫明原因，不改內容數值。
- [效能：每場多一個 `SkillCaster`，6 小時離線補算要跑上千場] `Advance_SixHours_ReturnsSummaryQuickly` 的上限是 2000 ms，並行時已量到 2115 ms（估計多出的成本不大，尚未量測）→ 實作後用正式內容量測補算時間的前後差異，寫進變更紀錄；超過 20% 就先回報再決定，不放寬上限。
- [`EquippedSkill` 是 record，放進 Dictionary 要算雜湊] 同上，靠量測確認。
- [被暈眩的怪物仍可能施放] → 刻意與玩家同一時機，記在上面的決策裡。
- [燃燒魔力在怪物身上不扣 SP] → 內容裡常駐技能沒有消耗，影響只有 3 格，接受。
- [現有角色沒有資料遷移] → 內容檔變更不動資料庫；裝備中的武器下一場開始就生效。
