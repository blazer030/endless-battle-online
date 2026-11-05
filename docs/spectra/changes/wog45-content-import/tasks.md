## 1. 匯入腳本與擴充（原版內容加在自訂內容旁）

- [x] 1.1 建立 `scripts/import-wog45.py`（決策「匯入腳本放 scripts/、用 Python、輸出固定格式」、「id 用「種類-原版編號」」），先擴充道具：原版道具以 `item-<id>` 加在自訂道具之後。欄位依決策「欄位對照只搬現有規則用得到的」的道具列，包含魔石、精鍊石、即時使用物；對不到的寫進 `docs/research/wog45/gaps.md`。對應需求「Official content comes from the WOG 4.5 records」、「Importing is repeatable」、「Data the rules cannot use is listed」。
  - TDD：先寫 `WogImportTests.Items_KeepOriginalValues`（`item-1` 布衣：Body、df 3、mdf 1、price 6、holes 1、refineMax 3），再寫 `Items_KeepVariantsSeparate`（`item-660`、`item-928` 都存在）與 `OfficialContent_Loads`。
  - 驗證：
    - `ContentFileFormatterTests.OfficialEditableContentFiles_AreInCanonicalFormat` 通過。
    - 連跑兩次腳本後 `git diff --exit-code backend/content docs/research/wog45/gaps.md` 為 0。
    - 用暫存副本刪掉一筆道具的 `price`，腳本以非 0 結束、印出檔名與 id，且沒有寫任何檔案。
- [x] 1.2 擴充職業與一般怪物 [after: 1.1]：
  - 原版職業以 `job-<id>` 加入，`growth` 轉 `[min, max]`，有 `parentId` 的為 2 轉。
  - 沒有 `missionId` 的原版怪物以 `monster-<id>` 加入，掉落改引用 `item-<id>`，屬性轉 1～6。
  - 現有驗證會拒絕的怪（等級 0、hp 或 agi 為 0）不匯入；`skills`、`specialAttack`、`str`～`be`、`npc` 等寫進 `gaps.md`。
  - TDD：先寫 `WogImportTests.Jobs_KeepOriginalGrowth`（`job-6` 戰士：1 轉、力量 `[1, 3]`），再寫 `Monsters_KeepOriginalValues`（`monster-1` 史萊姆：level 2、hp 32、element 5、placeId 1、subPlaces `[1]`（香茅綠地是中央平原的第 1 個子地區）、掉落 `item-204` topr 90）與 `Monsters_LeaveOutInvalidRecords`（每一隻等級 0 的原版怪都不在內容裡，也都在 `gaps.md` 裡）。
  - 驗證：`gaps.md` 在 monster 1 底下列出「猛烈一擊」與「爆氣 Lv2 rate 10」；`OfficialContent_Loads` 通過。

## 2. 技能改成四個部分（Domain、內容載入、API、遊戲畫面）

- [x] 2.1 擴充：依決策「技能由四個部分組成」、「發動條件是一份清單，沒有條件就是常駐」、「消耗不夠就不能發動」、「效果目錄」，新增新格式的技能模型。對應需求「Skills are assembled from conditions, costs and effects」、「Activation conditions are a list」、「Effect stacking」、「Every level is set separately up to the maximum level」、「Invalid skills are rejected at load」。
  - 模型：基本資料、`maxLevel`、每級 `conditions`／`costs`／`effects`，每個效果有自己的 `rounds`。
  - 這一步先支援：
    - 條件：`Chance`、`HitPointsBelow`。
    - 消耗：`SkillPoints amount`。
    - 效果：`StatChange`（attack、magicAttack、defense、magicDefense、agility，`percent`、`amount`、`scaling`）、`BonusDamage`、`MultiHit`、物理 `DamageReduction`、`Restore`（`scaling`、`percentOfDamage`）、`Stun`（含效果自己的 `chance`）。
    - 時機：`BeforeAttack`、`AfterHit`。
  - 規則：判斷順序、持續中不重複發動、回復已滿不發動、疊加（同技能相加、不同技能相乘）。
  - 載入同時接受舊格式與新格式；`SkillCaster` 依種類計算新格式，舊 enum 照舊可用。
  - TDD：先寫 `SkillEffectTests.StatChange_RaisesAttackByLevelPercent`（物攻 200，第 3 級 +3%、3 回合 → 三個回合都是 206），再寫：
    - `SkillEffectTests` 的戰鬥規則：
      - `CombinedEffects_ApplyTogether`（追加傷害力量×45% ＋ 命中 −70）
      - `Conditions_AllMustHold`（HP 低於 30 且機率 30：HP 50% 不擲骰；HP 20% 擲 25 發動、擲 31 不發動）
      - `NoConditions_IsAlwaysOn`
      - `StunChance_RolledAfterActivation`（物攻 +20% 一定生效，Stun 擲 50 對 12 不生效）
      - `LastingSkill_DoesNotReactivate`（第 1 回合發動、第 2～3 回合不擲骰、第 4 回合再發動）
      - `Stacking_SameSkillAddsDifferentSkillsMultiply`（220 與 221）
      - `LevelValues_AreUsedAsWritten`（第 2 級 Chance 70、+9% → 218）
    - `JsonGameContentTests` 的載入檢查：
      - `Skill_MissingEffectValueIsRejected`
      - `Skill_LevelsWithDifferentKindsAreRejected`
      - `Skill_LevelsMustMatchMaxLevel`
      - `Skill_AlwaysOnLevelWithCostIsRejected`
  - 驗證：錯誤訊息含技能 id 與等級；全部測試通過。
- [x] 2.2 搬移 [after: 2.1]：依決策「舊效果先等價搬到新格式，再移除」，把 `backend/EndlessBattle.Tests/TestContent/skills.json` 與正式內容的 16 個自訂技能改寫成數值等價的新格式。職業與技能的關聯表 `job-skills.json` 移到 2.3 建立：那一步學習規則才開始讀它。對應需求「Same parts give the same battle result」。
  - TDD：先寫 `SkillEffectTests.SameParts_GiveSameBattle`（破滅一擊第 1 級與爆氣第 5 級，同種子同事件），再寫：
    - `ActiveAndPassive_BehaveTheSame`（同內容的主動與被動，同種子同事件、同 SP 消耗）
    - `ChangingValue_ChangesResult`（3 改 6 → 206 改 212）
  - 驗證：`SkillCasterTests`、`WogBattleSkillTests`、`WogBattlePetTests`、`AdventureSimulatorTests`、`ChampionDuelTests` 不改斷言全部通過。
- [x] 2.3 依決策「職業與技能用關聯表連結，技能不指定職業」改學習規則 [after: 2.2]：依原本技能的 `jobId` 建立 `job-skills.json`（正式與 TestContent 各一份），有「目前職業＋技能」這一列才能學，扣目前職業的技能熟練度；前置技能未達等級時以 `prerequisite` 拒絕。對應需求「Jobs choose their skills」。
  - TDD：先寫 `SkillLearningTests.ListedSkill_CanBeLearned`，再寫 `UnlistedSkill_IsRefusedAsWrongJob`、`MissingPrerequisite_IsRefused`、`JsonGameContentTests.JobSkill_MissingSkillIsRejected`、`JobSkill_DuplicatePairIsRejected`，以及 API 測試 `SkillEndpointsTests.Learn_RefusesMissingPrerequisite`（400，reasons 含 `prerequisite`）。
  - 驗證：全部測試通過。
- [x] 2.4 收尾與畫面 [after: 2.3]，對應需求「Effect text is generated from the parts」：
  - 移除 `SkillEffectKind`、`SkillTrigger`、技能的 `JobId`、`SkillLevel.Chance` 與舊格式。
  - 依決策「效果文字自動產生」在 Application 產生每級的效果文字。
  - 遊戲 API 的技能回應改成 `slotType`、`summary`（目前與下一級）、`description`、`icon`。
  - `pnpm generate:api` 重新產生 `frontend/src/api/schema.ts`，技能頁 `SkillsScreen` 改顯示 `summary` 與 `description`。
  - TDD：先寫 `SkillSummaryTests.Summary_ReflectsLevelValues`（爆氣第 1 級 →「機率 40%｜消耗 SP 2｜自己物攻 +1%，3 回合」），再寫 API 測試 `SkillEndpointsTests.Skills_ReturnSummaryAndDescription` 與前端測試 `SkillsScreen` 顯示 summary。
  - 驗證：`grep -rn "SkillEffectKind\|SkillTrigger" backend` 沒有結果；`SkillCaster` 沒有依等級計算數值的係數；`cd frontend && pnpm check` 通過。
- [x] 2.5 新增能力類的效果 [after: 2.4]：`StatChange` 支援 charisma、hitRate、evasion、physicalCriticalChance、magicCriticalChance、physicalCriticalCap、magicCriticalCap、escapeRate 與 `target: Opponent`，`scaling` 支援效果目錄列出的全部 `basis`。計算寫在 `BattleModifiers`、`RoundResolver`、`WogBattle`、`EscapeRules`。
  - TDD：每個 stat 一條 `SkillEffectTests`，例如：
    - `Evasion_LowersOpponentHitChance`
    - `MagicCriticalChance_RaisesMagicCriticalOdds`
    - `PhysicalCriticalCap_RaisesCriticalMultiplierLimit`
    - `EscapeRate_RaisesEscapeChance`
    - `OpponentStat_LowersMonsterAgility`
    - `Scaling_UsesCharisma`（力量之舞：物攻 + 魅力×5%）
    - `StunChance_DoesNotAffectOtherEffects`（斯巴達：必殺率 +20 一定生效，Stun 擲 50 對 12 不生效）
  - 驗證：全部測試通過。
- [x] 2.6 新增傷害、回復與屬性類的效果 [after: 2.5]：
  - `PercentDamage`、`DamageOverTime`。
  - 魔法 `DamageReduction`。
  - `ReduceSkillPoints`、`Silence`。
  - `BonusDamage` 與對對手的 `StatChange` 可帶效果自己的 `chance`；目錄沒開放的種類寫了 `chance` 就載入失敗。
  - 常駐技能的一次性效果每到時機都再生效。
  - `Restore` 的 `percentOfMaximum` 與 SP 回復。
  - `AttackElement`、`DefenseElement`、`ElementAdvantage`，接到 `ElementTable`。
  - TDD：`SkillEffectTests` 逐一各一條：
    - `PercentDamage_CutsOpponentCurrentHp`（星芒 10%）
    - `DamageOverTime_HitsEachRound`（施毒：智力×10%）
    - `ChanceCoversAllEffects`（施毒：擲 20 時兩個效果都生效）
    - `MagicDamageReduction_LowersMagicTaken`
    - `RestoreSkillPoints_UsesPercentOfMaximum`
    - `AttackElement_ChangesElementMultiplier`
    - `ElementAdvantage_RaisesAdvantageDamage`
    - `ReduceSkillPoints_LowersOpponentSp`（燃燒魔力 20 點）
    - `Silence_BlocksOpponentSkills`（封鎖 2 回合：被封鎖的一方技能不發動、不擲骰，第 3 回合恢復）
    - `BonusDamageChance_RollsAfterActivation`（暗器：擲中 20 才追加傷害）
    - `AlwaysOnBonusDamage_AppliesEveryAttack`（毒傷陷阱）
    - `JsonGameContentTests.Skill_ChanceOnUnsupportedEffectIsRejected`（MultiHit 帶 chance → 失敗）
  - 驗證：全部測試通過。
- [x] 2.7 新增條件、消耗與時機 [after: 2.6]，對應需求「Costs block activation when unaffordable」：
  - 條件 `StatAbove`、`HitPointsBelowOpponent`。
  - 消耗 `HitPoints`（amount、percent）、`SkillPoints` 的 percent 與 all、`Money`（怪物不扣）。
  - 時機 `RoundEnd`。
  - 戰鬥紀錄的技能事件（`SkillEvent`）帶這次的消耗，`BattleLog` 一起顯示。
  - TDD：
    - `SkillEffectTests.StatAbove_BlocksWhenLower`（劍雨：魔攻 50 對 60 不發動、不扣 SP、不擲骰）
    - `HitPointsCost_PaidOnActivation`（1000 → 550，事件記 HP 450）
    - `UnaffordableCost_BlocksSkill`（目前 HP 400、扣 45% → 不發動、不擲骰）
    - `EffectValues_UseResourcesBeforeCost`（魔力爆發：SP 300、魔攻 100 → 130，SP 變 0）
    - `MonsterMoneyCost_IsNotPaid`
    - `HitPointsBelowOpponent_TriggersOnlyWhenBehind`
    - `RoundEndTiming_ActivatesAtRoundEnd`
    - 前端 `BattleLog` 顯示消耗
  - 驗證：全部測試通過；`cd frontend && pnpm check` 通過。

## 3. 技能匯入

- [x] 3.1 依決策「原版技能的每級數值取自各級說明」、「原版技能的發動方式照現行規則」匯入原版技能 [after: 1.2, 2.7]。對應需求「Original skills are imported as combinations」。
  - 腳本為每種技能寫一列樣板（條件、消耗、效果的種類與擷取數值的正規表示式），擷取失敗以非 0 結束。
  - 有 `wog-research-battle.md` §3.4 公式的技能逐一比對，不一致寫進 `gaps.md`。
  - 寫在「使敵人無法行動」上的機率轉成 `Stun` 的 `chance`；涵蓋全部效果的機率轉成 `Chance` 條件。
  - 寫在追加傷害、對手能力降低、封鎖技能上的機率，轉成那個效果的 `chance`。
  - 表達不了的技能整個寫進 `gaps.md`：寵物（等寵物系統調整）、組隊（不做）、條件改變機率（假死）。
  - 原版技能以 `skill-<第 1 級 id>` 加入；`job-skills.json` 依原版技能的 `jobId` 產生；`need_sid` 轉成 `prerequisite`；`maxLevel` 取原版的最高等級；`description` 留空。
  - TDD：先寫 `WogImportTests.Skills_FollowDescriptions`（`skill-1` 爆氣第 n 級：條件 `Chance min(100, 30 + 10·n)`（機率上限 100）、效果 `StatChange Caster attack percent n`，n = 1～10），再寫：
    - `Skills_ActivationFollowsCurrentRules`（石人陣常駐、沒有條件與消耗，`gaps.md` 註明原版發動率 28% 未採用）
    - `Skills_StunChanceIsEffectChance`（暈擊第 1 級：條件 `Chance 40`、效果 `Stun chance 3`）
    - `Skills_BonusDamageChanceIsEffectChance`（暗器第 1 級：必殺率 +40、追加傷害敏捷×15% 機率 20、迴避 −80）
    - `Skills_PetSkillsAreListed`（物攻命令）
    - `JobSkills_FollowOriginalJobs`（`job-skills.json` 有 `job-6`＋`skill-1` 這一列）
  - 驗證：`OfficialContent_Loads` 通過；連跑兩次腳本 `git diff` 為 0。

## 4. 切換（改成原版、改引用原版 id）

- [x] 4.1 配方、榮譽商店、傭兵改成原版 [after: 3.1]：`recipe-<id>`、`honour-<id>`、`mercenary-<id>`；榮譽商店的 `h_1`～`h_5` 依 `docs/research/wog-research-world.md` 對到五種勳章道具。榮譽商店的裝備商品照現有兌換格式匯入（取代裁定第 77 行）。原版鑰匙（`item-1338`、`item-1339`、`item-1405`、`item-1522`）同時匯入、取代自訂鑰匙，`key.placeId` 依 `docs/research/wog45/shops-and-misc.md` 的地區鑰匙表、`seconds` 原版有就用原版。
  - TDD：先寫 `WogImportTests.Recipes_KeepOriginalMaterials`（`recipe-1` 產出鋼鐵圓桌盾 `item-308`，材料 `item-125` 與 `item-306` 各 1），再寫 `HonourShop_UsesOriginalExchanges`（`honour-1` 兌換 C 級傭兵，代價是 `h_1`、`h_2` 對到的勳章各 13）。
  - 改寫 `CompanionTableTests`、`ItemTableTests` 中釘住自訂配方、兌換、傭兵的測試，改成釘住原版值。
  - 驗證：`OfficialContent_Loads` 與改寫後的測試通過。
- [x] 4.2 任務與任務怪改成原版 [after: 4.1]：
  - 一般任務以 `mission-<id>` 匯入，欄位依決策「欄位對照只搬現有規則用得到的」的任務列；有 `missionId` 的原版怪同時加入。
  - 每日任務依決策「每日任務沿用現有結構、改引用原版」改名為 `mission-222` 等，池裡的怪物與道具改成原版 id。
  - 多個擊殺目標、交寵物、`needSkillId`，以及引用了被排除紀錄的獎勵，都寫進 `gaps.md`。
  - TDD：先寫 `WogImportTests.Missions_KeepOriginalGoals`（`mission-1` 製作漁網：交付 `item-331` 1 個，獎勵 `item-342` 1 個），再寫 `Missions_DropReferencesToExcludedRecords`：任務引用的道具若被排除，那筆獎勵就不存在，`gaps.md` 也記下這次移除。
  - 改寫 `MissionTableTests`，改成釘住原版任務與每日任務的結構。
  - 驗證：`OfficialContent_Loads` 通過。

## 5. 收尾（移除自訂內容、資料庫預設值）

- [x] 5.1 依決策「保留原版沒有的自訂內容，逐筆寫原因」移除不在保留清單上的自訂道具、怪物、職業、技能、傭兵，完成「先擴充、再切換、最後收尾」的最後一步 [after: 4.2]。保留清單寫在腳本裡，`gaps.md` 的「保留的自訂內容」一節逐筆列出原因，並列出 `inn.json`、`champion.json` 與 `world.json` 的區域、推薦等級、起始地點。對應需求「Custom content without an original counterpart is kept」。
  - TDD：先寫 `WogImportTests.CustomContent_WithoutCounterpartIsRemoved`（`wandering-puff` 不存在），再寫 `CustomContent_KeptRecordsAreListed`（每一筆 kebab-case id 的紀錄都出現在 `gaps.md` 的保留一節）。
  - 改寫 `MonsterRosterTests`、`ItemTableTests` 中釘住自訂內容表的測試（每地區 3 隻、kebab-case id、公式區間），改成釘住匯入結果。
  - 驗證：
    - `git diff --exit-code backend/content/inn.json backend/content/champion.json` 為 0。
    - 執行 `BalanceTests`，若有失敗就暫停、回報失敗的項目與數值，請使用者決定，不自行改門檻。
    - 擴充期間 `Balance_StartingSubPlaceNeverTakesHalfAFreshCharactersHp` 暫時只看自訂怪物；已知原版飛兔 `monster-9`（Lv6、物攻 33，香茅綠地）最大單擊 495，超過門檻 162，要在這一步請使用者決定。
- [ ] 5.2 依決策「新增 migration 改職業預設值」新增 `backend/db/ddl/V000000016__default_job_id.sql`，把 `characters.job_id` 的預設值改成 `job-6`；接著 `docker compose up -d`，再跑 `./scripts/scaffold-db.sh`，model 有變就一起提交 [after: 5.1]。
  - 驗證：`./scripts/check-sql.sh` 通過；Testcontainers 測試 `CharacterDefaultsTests.JobId_DefaultsToOriginalWarrior` 不指定 `job_id` 插入一列角色，讀回來是 `job-6`。

## 6. 文件與最終驗證

- [ ] 6.1 更新研究文件，並做最終驗證 [after: 5.2]：
  - 文件：
    - `docs/research/README.md` 與 `docs/research/wog45/README.md` 說明正式內容由 `scripts/import-wog45.py` 產生，並連到 `gaps.md`。
    - `monster-roster.md`、`item-table.md`、`mission-table.md`、`companion-table.md`、`job-growth-expectation.md` 開頭註明哪些總表已由原版取代。
    - 查 `docs/superpowers/specs/2026-10-23-deferred-backlog.md`，刪掉這次已處理的條目。
  - 核對：文件內的相對連結都指到存在的檔案；決策「硬規則」逐條核對無違反。
  - 驗證：
    - 連跑兩次匯入腳本後 `git diff --exit-code` 為 0。
    - 手動：`docker compose down -v` 後 `docker compose up -d` 重建資料庫，啟動 API 與前端，創一個戰士角色，學爆氣並裝上，在中央平原冒險一輪，確認遇到的是原版怪物、技能頁顯示爆氣的效果文字、戰鬥紀錄有爆氣發動與消耗的 SP。
    - `./scripts/check.sh` exit code 0。
