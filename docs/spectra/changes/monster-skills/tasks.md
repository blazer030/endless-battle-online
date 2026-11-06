## 1. 結構整理

- [x] 1.1 把 `SkillCaster` 內的 `player` 改名 `caster`，`BattleModifiers.ApplyToPlayer`／`ApplyToMonster` 改名 `ApplyToCaster`／`ApplyToOpponent`，行為不變。驗證：改名前後後端測試都綠，單獨以結構性變更 commit（`♻️ refactor`）。

## 2. 戰鬥：怪物施放

- [x] 2.1 怪物在回合開頭、玩家之後施放，付 SP 與 HP 消耗、不付金錢，回復與削減寫回雙方，事件的 `Caster` 是 `Monster`；玩家的施放擊倒怪物時怪物不施放，怪物的施放擊倒玩家時本場結束。`WogBattle.Run` 多接 `monsterSkills`（預設空）。先寫失敗測試 `Run_MonsterCastsAfterPlayer_RecordsMonsterSkillEvent`、`Run_MonsterHeals_RaisesMonsterHp`、`Run_MonsterWithoutEnoughSp_DoesNotRoll`、`Run_MonsterDefeatedByPlayerCast_DoesNotCast`、`Run_MonsterMoneyCost_IsNotPaid`。驗證：這些測試通過，現有依固定亂數的戰鬥測試不改動也通過。（規格：Monster casts happen after the player's、Skill effects work in both directions） [after: 1.1]
- [ ] 2.2 雙方修正值依 `design.md` 的「戰鬥雙向合成」表作用於能力值、先攻、命中、暴擊、追加傷害、減傷（含寵物攻擊）與屬性。先寫失敗測試 `Run_MonsterBuff_RaisesItsDamage`、`Run_MonsterDebuff_LowersPlayerDamage`、`Run_BothSidesAttackChanges_Combine`、`Run_MonsterDamageReduction_AppliesToPlayerAndPet`、`Run_MonsterSpeedChange_AffectsFirstAttackerThatRound`。驗證：這些測試通過。（規格：Skill effects work in both directions；設計：怪物用第二個 SkillCaster，修正值雙向合成、怪物在玩家之後、同樣在回合開頭施放） [after: 2.1]
- [ ] 2.3 怪物的連擊、物理命中後、回合結束與持續傷害都會作用；代擋成功不觸發命中後技能；技能傷害不經過寵物代擋；回合結束任一方倒下都以正確的敗方結束。先寫失敗測試 `Run_MonsterMultiHit_StopsWhenPlayerFalls`、`Run_MonsterAfterHit_UsesItsDamage`、`Run_GuardedHit_TriggersNoAfterHitSkill`、`Run_MonsterDamageOverTime_CanDefeatPlayer`、`Run_MonsterSkillDamage_SkipsPetGuard`。驗證：這些測試通過。（規格：Skill effects work in both directions） [after: 2.2]
- [ ] 2.4 暈眩與封鎖雙向作用：玩家被暈眩時不出手並記錄 `Attacker: Player, Stunned: true`；`Silence` 在發動時、效果機率之後擲骰，擲中就封鎖對手 `max(1, rounds)` 回合；移除沒人用的 `BattleModifiers.SilenceChance`。先寫失敗測試 `Run_StunnedPlayer_DoesNotAttack`、`Run_PlayerSilencesMonster_BlocksForRounds`、`Run_MonsterSilencesPlayer_BlocksRoundEndSkill`、`Run_FailedSilenceRoll_BlocksNothing`，並更新 `Silence_BlocksOpponentSkills` 裡讀 `SilenceChance` 的斷言。驗證：這些測試通過。（規格：Silence blocks the opponent during a battle；設計：封鎖在施放時擲骰，由 WogBattle 轉給對手） [after: 2.3]

## 3. 內容：怪物技能檔與假死的機率調整

- [ ] 3.1 新增 `SkillDefinition.WithActivationRate`：每一級移除 `Chance` 條件、加上發動率，其他條件不動，沒有條件的技能變成要擲發動率。先寫失敗測試 `WithActivationRate_ReplacesChance`、`WithActivationRate_KeepsOtherConditions`、`WithActivationRate_AlwaysOnSkillRollsTheRate`、`WithActivationRate_EffectChanceStillRolls`。驗證：這些測試通過。（規格：The activation rate replaces the Chance condition；設計：發動率由 SkillDefinition 轉換，不改施放規則）
- [ ] 3.2 `JsonGameContent` 載入必要檔案 `monster-skills.json`，解析到 `MonsterDefinition.Skills` 並拒絕無效列；`AdventureSimulator` 與 `MercenarySimulator` 把怪物技能傳進戰鬥。`TestContent/monster-skills.json` 只放一隻不在起始子地區的怪（沒有合適的就新增），不影響現有測試。先寫失敗測試 `Load_MonsterSkillRow_IsOnItsMonster`、`Load_InvalidMonsterSkill_Throws`（`design.md` 拒絕清單的每一種）、`Load_WithoutMonsterSkillsFile_Throws`、`Run_MonsterSkillsApply_InAdventure`、`Run_MonsterSkillsApply_InMercenaryFight`。驗證：這些測試通過。（規格：Monster skills are listed in a content file；設計：怪物技能在內容載入時解析） [after: 2.1, 3.1]
- [ ] 3.3 `StunEffect` 支援 `chanceByStat`：比基礎能力，高就加、其餘含相等就減，不大於 0 不會暈眩；無效的 percent 載入時拒絕；效果文字顯示調整。先寫失敗測試 `Stun_ChanceByStat_FollowsComparison`（`skill-effects` 規格的兩張表）、`Load_InvalidChanceByStat_Throws`、`Summary_StunWithChanceByStat_ShowsAdjustment`。驗證：這些測試通過。（規格：A stun chance can depend on comparing a stat；設計：假死用 Stun 效果的機率調整，不改 Chance 條件） [after: 2.4]

## 4. 內容：武器附帶技能

- [ ] 4.1 `ItemDefinition.WeaponSkill` 從 `items.json` 載入並拒絕無效宣告。先寫失敗測試 `Load_WeaponSkill_IsOnItem`、`Load_InvalidWeaponSkill_Throws`（規格拒絕清單的每一種）。驗證：這些測試通過。（規格：Weapons can carry a skill） [after: 3.1]
- [ ] 4.2 裝備中的武器以 `SkillLoadout.WeaponSlot` 加入戰鬥技能：用武器的發動率、不扣 SP、不用學過；同一技能已在欄位 1～5 時不生效；SP 住宿門檻不把它算成主動技能。`AdventureService` 把裝備中的武器傳給 `EquippedSkills.LoadAsync`。先寫失敗測試 `Load_EquippedWeaponSkill_IsAddedInWeaponSlot`、`Load_UnequippedWeapon_AddsNoSkill`、`Load_SameSkillInSlot_TakesPriority`、`Run_WeaponSkill_ActivatesWithoutSp`、`Run_WeaponSkillOnly_IgnoresSpRestThreshold`。驗證：這些測試通過。（規格：An equipped weapon's skill works without SP；設計：武器附帶技能是第 6 欄，不耗 SP） [after: 4.1]
- [ ] 4.3 `InventoryItemResponse` 回傳 `weaponSkill`（`name`、`level`、`rate`，沒有時為 null），`pnpm generate:api` 重新產生 `frontend/src/api/schema.ts`；背包武器明細顯示 `附帶技能 爆氣 Lv2（發動率 12%）`。先寫失敗測試：API 測試 `Inventory_WeaponWithSkill_ReturnsWeaponSkill`，前端測試 `ItemDetail_WeaponWithSkill_ShowsSkillLine`、`ItemDetail_WeaponWithoutSkill_ShowsNoSkillLine`。驗證：這些測試通過。（規格：The bag shows a weapon's skill） [after: 4.2]

## 5. 匯入

- [ ] 5.1 `scripts/import-wog45.py` 匯入假死（第 1 級 `Chance` 40、SP 9、`Stun` 機率 2、`chanceByStat` intelligence 5、1 回合），從排除清單移出，刺客的 `job-skills` 加上它。先在 `WogImportTests` 寫失敗測試 `Import_ApparentDeath_MatchesOriginal`，並確認會讓前置條件恢復的 `gaps.md` 列跟著消失。驗證：匯入後測試通過。（規格：Monster and weapon skills are imported） [after: 3.3]
- [ ] 5.2 匯入腳本產出 `monster-skills.json` 與武器的 `weaponSkill`：等級依技能名稱的列數算、重複技能留高等級、`Chance` 不是預設值就換算發動率、沒有發動率的不寫、戰意驅使繼續列在 `gaps.md`。先在 `WogImportTests` 寫失敗測試 `Import_MonsterSkills_MatchOriginal`（史萊姆、魔盾-水 Lv1、盜墓者、列數 568）、`Import_WeaponSkills_MatchOriginal`（爆氣火斧、神經系毒氣的發動率 2、翼龍獸矛沒有附帶技能）、`Gaps_ListsOnlySkillsNotMoved`。跑兩次匯入。驗證：測試通過，第二次 `git diff --exit-code backend/content docs/research/wog45/gaps.md` 回 0。（規格：Monster and weapon skills are imported、Data the rules cannot use is listed；設計：鋒針陷阱的武器發動率在匯入時換算、同一隻怪重複的技能只留等級較高的） [after: 3.2, 4.1, 5.1]

## 6. 前端呈現

- [ ] 6.1 `frontend/src/game/stageModel.ts`：`skill` 事件依施放者更新雙方 HP；怪物施放是名牌 `over: "monster"`、不閃光；玩家被暈眩是淡色名牌 `over: "player"`。先寫失敗測試 `applyToStage_MonsterSkill_UpdatesBothSides`（規格的 HP 表）、`stageEffectsFor_MonsterSkill_ShowsPlateOverMonster`、`stageEffectsFor_StunnedPlayer_ShowsPlateOverPlayer`。驗證：這些測試通過，玩家施放的現有測試不改動也通過。（規格：The stage and log show monster skills） [after: 2.4]
- [ ] 6.2 `frontend/src/game/battleLog.ts`：怪物施放與玩家被暈眩的戰報文字。先寫失敗測試 `describeSkill_MonsterCaster_WritesExpectedLines`（規格的戰報表，含不寫消耗）、`describeAttack_StunnedPlayer_WritesMutedLine`。驗證：這些測試通過。（規格：The stage and log show monster skills） [after: 6.1]

## 7. 量測與收尾

- [ ] 7.1 用正式內容量測 6 小時補算的時間，比較這次變更前後（基準 commit 是提案開始前的 `develop`），也跑 `Advance_SixHours_ReturnsSummaryQuickly`。數字寫進 `tasks.md` 的變更紀錄；增加超過 20% 就先回報使用者。驗證：變更紀錄有前後數字，測試的 2000 ms 上限沒有被放寬。 [after: 5.2]
- [ ] 7.2 跑 `BalanceTests` 與平衡報告，把怪物技能造成的變化記進變更紀錄，不改內容數值。測試失敗時只調整測試並寫明原因。`README.md` 的戰鬥列補上怪物技能與武器附帶技能。驗證：`BalanceTests` 通過，`git diff` 裡內容數值只有這次新增的欄位。 [after: 7.1]
- [ ] 7.3 手動驗證：在 375×667 用真實內容打帶技能的怪，確認戰報寫怪物施展技能、怪物上方有名牌、血量與回復正確、被暈眩時有提示；裝備有附帶技能的武器，確認背包顯示與戰鬥觸發不扣 SP。確認沒有水平捲動與主控台錯誤。驗證：截圖與結果寫進變更紀錄；若用攔截或測試資料強制觸發，要明白寫出。 [after: 4.3, 6.2, 7.2]
- [ ] 7.4 `./scripts/check.sh` exit code 0。 [after: 7.3]
