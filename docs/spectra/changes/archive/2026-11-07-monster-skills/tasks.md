## 1. 結構整理

- [x] 1.1 把 `SkillCaster` 內的 `player` 改名 `caster`，`BattleModifiers.ApplyToPlayer`／`ApplyToMonster` 改名 `ApplyToCaster`／`ApplyToOpponent`，行為不變。驗證：改名前後後端測試都綠，單獨以結構性變更 commit（`♻️ refactor`）。

## 2. 戰鬥：怪物施放

- [x] 2.1 怪物在回合開頭、玩家之後施放，付 SP 與 HP 消耗、不付金錢，回復與削減寫回雙方，事件的 `Caster` 是 `Monster`；玩家的施放擊倒怪物時怪物不施放，怪物的施放擊倒玩家時本場結束。`WogBattle.Run` 多接 `monsterSkills`（預設空）。先寫失敗測試 `Run_MonsterCastsAfterPlayer_RecordsMonsterSkillEvent`、`Run_MonsterHeals_RaisesMonsterHp`、`Run_MonsterWithoutEnoughSp_DoesNotRoll`、`Run_MonsterDefeatedByPlayerCast_DoesNotCast`、`Run_MonsterMoneyCost_IsNotPaid`。驗證：這些測試通過，現有依固定亂數的戰鬥測試不改動也通過。（規格：Monster casts happen after the player's、Skill effects work in both directions） [after: 1.1]
- [x] 2.2 雙方修正值依 `design.md` 的「戰鬥雙向合成」表作用於能力值、先攻、命中、暴擊、追加傷害、減傷（含寵物攻擊）與屬性。先寫失敗測試 `Run_MonsterBuff_RaisesItsDamage`、`Run_MonsterDebuff_LowersPlayerDamage`、`Run_BothSidesAttackChanges_Combine`、`Run_MonsterDamageReduction_AppliesToPlayerAndPet`、`Run_MonsterSpeedChange_AffectsFirstAttackerThatRound`。驗證：這些測試通過。（規格：Skill effects work in both directions；設計：怪物用第二個 SkillCaster，修正值雙向合成、怪物在玩家之後、同樣在回合開頭施放） [after: 2.1]
- [x] 2.3 怪物的連擊、物理命中後、回合結束與持續傷害都會作用；代擋成功不觸發命中後技能；技能傷害不經過寵物代擋；回合結束任一方倒下都以正確的敗方結束。先寫失敗測試 `Run_MonsterMultiHit_StopsWhenPlayerFalls`、`Run_MonsterAfterHit_UsesItsDamage`、`Run_GuardedHit_TriggersNoAfterHitSkill`、`Run_MonsterDamageOverTime_CanDefeatPlayer`、`Run_MonsterSkillDamage_SkipsPetGuard`。驗證：這些測試通過。（規格：Skill effects work in both directions） [after: 2.2]
- [x] 2.4 暈眩與封鎖雙向作用：玩家被暈眩時不出手並記錄 `Attacker: Player, Stunned: true`；`Silence` 在發動時、效果機率之後擲骰，擲中就封鎖對手 `max(1, rounds)` 回合；移除沒人用的 `BattleModifiers.SilenceChance`。先寫失敗測試 `Run_StunnedPlayer_DoesNotAttack`、`Run_PlayerSilencesMonster_BlocksForRounds`、`Run_MonsterSilencesPlayer_BlocksRoundEndSkill`、`Run_FailedSilenceRoll_BlocksNothing`，並更新 `Silence_BlocksOpponentSkills` 裡讀 `SilenceChance` 的斷言。驗證：這些測試通過。（規格：Silence blocks the opponent during a battle；設計：封鎖在施放時擲骰，由 WogBattle 轉給對手） [after: 2.3]

## 3. 內容：怪物技能檔與假死的機率調整

- [x] 3.1 新增 `SkillDefinition.WithActivationRate`：每一級移除 `Chance` 條件、加上發動率，其他條件不動，沒有條件的技能變成要擲發動率。先寫失敗測試 `WithActivationRate_ReplacesChance`、`WithActivationRate_KeepsOtherConditions`、`WithActivationRate_AlwaysOnSkillRollsTheRate`、`WithActivationRate_EffectChanceStillRolls`。驗證：這些測試通過。（規格：The activation rate replaces the Chance condition；設計：發動率由 SkillDefinition 轉換，不改施放規則）
- [x] 3.2 `JsonGameContent` 載入必要檔案 `monster-skills.json`，解析到 `MonsterDefinition.Skills` 並拒絕無效列；`AdventureSimulator` 與 `MercenarySimulator` 把怪物技能傳進戰鬥。`TestContent/monster-skills.json` 只放一隻不在起始子地區的怪（沒有合適的就新增），不影響現有測試。先寫失敗測試 `Load_MonsterSkillRow_IsOnItsMonster`、`Load_InvalidMonsterSkill_Throws`（`design.md` 拒絕清單的每一種）、`Load_WithoutMonsterSkillsFile_Throws`、`Run_MonsterSkillsApply_InAdventure`、`Run_MonsterSkillsApply_InMercenaryFight`。驗證：這些測試通過。（規格：Monster skills are listed in a content file；設計：怪物技能在內容載入時解析） [after: 2.1, 3.1]
- [x] 3.3 `StunEffect` 支援 `chanceByStat`：比基礎能力，高就加、其餘含相等就減，不大於 0 不會暈眩；無效的 percent 載入時拒絕；效果文字顯示調整。先寫失敗測試 `Stun_ChanceByStat_FollowsComparison`（`skill-effects` 規格的兩張表）、`Load_InvalidChanceByStat_Throws`、`Summary_StunWithChanceByStat_ShowsAdjustment`。驗證：這些測試通過。（規格：A stun chance can depend on comparing a stat；設計：假死用 Stun 效果的機率調整，不改 Chance 條件） [after: 2.4]

## 4. 內容：武器附帶技能

- [x] 4.1 `ItemDefinition.WeaponSkill` 從 `items.json` 載入並拒絕無效宣告。先寫失敗測試 `Load_WeaponSkill_IsOnItem`、`Load_InvalidWeaponSkill_Throws`（規格拒絕清單的每一種）。驗證：這些測試通過。（規格：Weapons can carry a skill） [after: 3.1]
- [x] 4.2 裝備中的武器以 `SkillLoadout.WeaponSlot` 加入戰鬥技能：用武器的發動率、不扣 SP、不用學過；同一技能已在欄位 1～5 時不生效；SP 住宿門檻不把它算成主動技能。`AdventureService` 把裝備中的武器傳給 `EquippedSkills.LoadAsync`。先寫失敗測試 `Load_EquippedWeaponSkill_IsAddedInWeaponSlot`、`Load_UnequippedWeapon_AddsNoSkill`、`Load_SameSkillInSlot_TakesPriority`、`Run_WeaponSkill_ActivatesWithoutSp`、`Run_WeaponSkillOnly_IgnoresSpRestThreshold`。驗證：這些測試通過。（規格：An equipped weapon's skill works without SP；設計：武器附帶技能是第 6 欄，不耗 SP） [after: 4.1]
- [x] 4.3 `InventoryItemResponse` 回傳 `weaponSkill`（`name`、`level`、`rate`，沒有時為 null），`pnpm generate:api` 重新產生 `frontend/src/api/schema.ts`；背包武器明細顯示 `附帶技能 爆氣 Lv2（發動率 12%）`。先寫失敗測試：API 測試 `Inventory_WeaponWithSkill_ReturnsWeaponSkill`，前端測試 `ItemDetail_WeaponWithSkill_ShowsSkillLine`、`ItemDetail_WeaponWithoutSkill_ShowsNoSkillLine`。驗證：這些測試通過。（規格：The bag shows a weapon's skill） [after: 4.2]

## 5. 匯入

- [x] 5.1 `scripts/import-wog45.py` 匯入假死（第 1 級 `Chance` 40、SP 9、`Stun` 機率 2、`chanceByStat` intelligence 5、1 回合），從排除清單移出，刺客的 `job-skills` 加上它。先在 `WogImportTests` 寫失敗測試 `Import_ApparentDeath_MatchesOriginal`，並確認會讓前置條件恢復的 `gaps.md` 列跟著消失。驗證：匯入後測試通過。（規格：Monster and weapon skills are imported） [after: 3.3]
- [x] 5.2 匯入腳本產出 `monster-skills.json` 與武器的 `weaponSkill`：等級依技能名稱的列數算、重複技能留高等級、`Chance` 不是預設值就換算發動率、沒有發動率的不寫、戰意驅使繼續列在 `gaps.md`。先在 `WogImportTests` 寫失敗測試 `Import_MonsterSkills_MatchOriginal`（史萊姆、魔盾-水 Lv1、盜墓者、列數 568）、`Import_WeaponSkills_MatchOriginal`（爆氣火斧、神經系毒氣的發動率 2、翼龍獸矛沒有附帶技能）、`Gaps_ListsOnlySkillsNotMoved`。跑兩次匯入。驗證：測試通過，第二次 `git diff --exit-code backend/content docs/research/wog45/gaps.md` 回 0。（規格：Monster and weapon skills are imported、Data the rules cannot use is listed；設計：鋒針陷阱的武器發動率在匯入時換算、同一隻怪重複的技能只留等級較高的） [after: 3.2, 4.1, 5.1]

## 6. 前端呈現

- [x] 6.1 `frontend/src/game/stageModel.ts`：`skill` 事件依施放者更新雙方 HP；怪物施放是名牌 `over: "monster"`、不閃光；玩家被暈眩是淡色名牌 `over: "player"`。先寫失敗測試 `applyToStage_MonsterSkill_UpdatesBothSides`（規格的 HP 表）、`stageEffectsFor_MonsterSkill_ShowsPlateOverMonster`、`stageEffectsFor_StunnedPlayer_ShowsPlateOverPlayer`。驗證：這些測試通過，玩家施放的現有測試不改動也通過。（規格：The stage and log show monster skills） [after: 2.4]
- [x] 6.2 `frontend/src/game/battleLog.ts`：怪物施放與玩家被暈眩的戰報文字。先寫失敗測試 `describeSkill_MonsterCaster_WritesExpectedLines`（規格的戰報表，含不寫消耗）、`describeAttack_StunnedPlayer_WritesMutedLine`。驗證：這些測試通過。（規格：The stage and log show monster skills） [after: 6.1]

## 7. 量測與收尾

- [x] 7.1 用正式內容量測 6 小時補算的時間，比較這次變更前後（基準 commit 是提案開始前的 `develop`），也跑 `Advance_SixHours_ReturnsSummaryQuickly`。數字寫進 `tasks.md` 的變更紀錄；增加超過 20% 就先回報使用者。驗證：變更紀錄有前後數字，測試的 2000 ms 上限沒有被放寬。 [after: 5.2]
- [x] 7.2 跑 `BalanceTests` 與平衡報告，把怪物技能造成的變化記進變更紀錄，不改內容數值。測試失敗時只調整測試並寫明原因。`README.md` 的戰鬥列補上怪物技能與武器附帶技能。驗證：`BalanceTests` 通過，`git diff` 裡內容數值只有這次新增的欄位。 [after: 7.1]
- [x] 7.3 手動驗證：在 375×667 用真實內容打帶技能的怪，確認戰報寫怪物施展技能、怪物上方有名牌、血量與回復正確、被暈眩時有提示；裝備有附帶技能的武器，確認背包顯示與戰鬥觸發不扣 SP。確認沒有水平捲動與主控台錯誤。驗證：結果寫進變更紀錄（截圖只是當下的驗證畫面，不進 repo）；若用攔截或測試資料強制觸發，要明白寫出。 [after: 4.3, 6.2, 7.2]
- [x] 7.4 `./scripts/check.sh` exit code 0。 [after: 7.3]

## 8. 驗證後的修正

- [x] 8.1 裝備頁點開欄位的面板顯示裝備中道具的明細，武器裝上後也看得到附帶技能（規格：The bag shows a weapon's skill）。先寫失敗測試 `BagScreen_EquippedWeaponPanelShowsItsSkill`。驗證：測試通過，375×667 畫面確認。 [after: 7.4]
- [x] 8.2 匯入時沒有發動率的怪物技能不寫入並列在 `gaps.md`，文字不再出現 `rate None`（規格：Monster and weapon skills are imported）。先寫失敗測試 `Import_MonsterSkillWithoutRate_IsListedNotWritten`。驗證：測試通過，第二次匯入 `git diff --exit-code backend/content docs/research/wog45/gaps.md` 回 0。 [after: 7.4]
- [x] 8.3 補強既有行為的測試：`Run_MonsterSkillDamage_SkipsPetGuard` 改用會讓代擋成立的擲骰，新增 `Run_WeaponSkillApplies_InMercenaryFight`，`Load_InvalidMonsterSkill_Throws` 的重複列檢查名稱。驗證：測試通過，前兩條在故意改壞程式時會失敗。 [after: 7.4]
- [x] 8.4 `./scripts/check.sh` exit code 0。 [after: 8.1, 8.2, 8.3]

## 變更紀錄

### 效能量測（7.1，2026-11-06，本機量測，基準 `c446350`）

| 項目 | 變更前 | 第一版 | `SkillCaster` 字典改比參考後 |
|---|---|---|---|
| 正式內容跑一次平衡報告（84 列 × 2 組，各模擬 8 小時，約 33 萬場） | 約 3010 ms | 約 4350 ms（+44%） | 約 3440 ms（+14%） |
| 換算成平均一次 6 小時補算 | 約 13 ms | 約 19 ms | 約 15 ms |
| `Advance_SixHours_ReturnsSummaryQuickly` 單獨跑 5 次（測試內容，含資料庫） | 177～218 ms | | 197～213 ms |

- 第一版超過設計定的 20% 門檻，原因是 `EquippedSkill` 是 record，放進字典時每次都要算整個技能定義的雜湊；改比參考後回到門檻內。
- 測試的 2000 ms 上限沒有放寬。
- `Advance_SixHours_ReturnsSummaryQuickly` 在 `./scripts/check.sh` 的完整並行測試裡偶爾超時（量到 2028、2115、2136 ms），其中 2115 ms 發生在這次變更開始之前，單獨跑與多次完整跑都在 500 ms 內，所以判斷是並行時搶 CPU 的既有不穩定，不是這次變更造成的；確切觸發條件沒有查證。

### 平衡變化（7.2，同一次量測；每場擲骰序列不同，個別列有隨機差異）

| 平衡報告總計 | 變更前 | 變更後 |
|---|---|---|
| 不帶技能：勝率 | 32.0% | 31.6% |
| 不帶技能：每小時經驗合計 | 680,893 | 655,876 |
| 帶技能：勝率 | 26.5% | 26.0% |
| 帶技能：每小時經驗合計 | 676,966 | 673,732 |

- 84 列裡沒有勝率掉超過 10 個百分點的列，也沒有從有勝場變成零勝的列。掉最多的是試鍊洞窟的術士（Lv6，72.3% → 65.0%）。
- 平衡報告不含武器附帶技能（設計範圍外），也只看各地區推薦等級，不代表所有情況。
- `BalanceTests` 不改動就通過；它算的是普通一擊，沒有把怪物技能算進去。
- 內容數值沒有改：與 `c446350` 比對，`items.json` 只多 `weaponSkill`，`skills.json` 只多假死，`monsters.json`、`jobs.json`、`world.json` 沒有變。

### 手動驗證（7.3，2026-11-06，375×667、2 倍解析度，開發用 API＋vite）

驗證前重啟了 vite 與開發用 API：vite 原本還在送舊檔，用 `curl` 確認模組有新程式才開始。

| 項目 | 怎麼驗 | 結果 |
|---|---|---|
| 怪物施放的戰報與名牌 | 真實戰鬥，沒有改任何回應 | `毒蜂施展了急影！`、`該死小白施展了破滅一擊！` 等；怪物上方有青色名牌，沒有全畫面閃光 |
| 怪物回復 | **攔截回應強制**把一次施放的回復改成 50（起始地區等了 200 秒沒遇到） | `飛兔回復了 50 點 HP。` |
| 玩家被暈眩 | **攔截回應強制**改一次攻擊事件（起始地區的怪沒有暈眩技能） | `你暈眩中，無法行動。`；玩家上方有淡色「暈眩」名牌 |
| 武器附帶技能 | **直接改本機開發資料庫**：一次性測試角色力量 2000、放入爆氣火斧，再用 API 裝備 | `你施展了爆氣！`，事件等級 2、SP 消耗 0 |
| 背包顯示 | 同上，未裝備時看背包清單 | `附帶技能 爆氣 Lv2（發動率 12%）` |
| 版面與錯誤 | 全程 | 沒有水平捲動；只有 `/favicon.ico` 404，沒有主控台錯誤 |

- 「SP 0 也能發動」沒有在瀏覽器驗到：測試角色先去住宿回滿了 SP，這條由 `Run_WeaponSkill_ActivatesWithoutSp` 涵蓋；瀏覽器只驗到 SP 消耗是 0。
- 怪物回復與被暈眩是強制的，只驗呈現；實際觸發由後端測試涵蓋。
- 「裝備」頁的欄位列表只顯示道具名稱，是現有版面，這次沒有改；裝備中道具的明細改放在點開欄位的面板（8.1）。
- 第 ① 段看到起始地區的飛兔（monster-9）對 Lv1 角色一下 100、暴擊 344，是已知的原版數值問題，不是這次變更造成的。
- 驗證後補了裝備頁的面板（8.1）：同樣改本機資料庫給測試角色武器，375×667 點開武器欄位，顯示能力加成與 `附帶技能 爆氣 Lv2（發動率 12%）`，沒有水平捲動。
- 測試用了 5 個一次性 `check...` 帳號，留在本機開發資料庫。Docker 的 `web`、`api` 容器（8080 埠）還是舊版，沒有重建。

