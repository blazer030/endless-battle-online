## 1. 多目標擊殺

- [x] 1.1 結構性變更：擊殺進度改存成清單，行為不變。新增 `backend/db/ddl/V000000019__mission_kill_progress.sql`（舊 `progress` 轉成 `[progress]` 後移除欄位），依 migration → `docker compose up -d` → `scripts/scaffold-db.sh` 的順序進行；`MissionRecord.Progress` 改成 `IReadOnlyList<int>`，`MissionRepository`、`InMemoryMissionRepository` 跟著改。用 Testcontainers 驗證：`MissionRepository_RoundTripsKillProgress`、`DatabaseSchemaTests` 有 `kill_progress` 且沒有 `progress`，以及 `Migration_KeepsExistingProgress`（讓 `MigrationRunner` 能只套到 V18，寫入舊資料後再套 V19，確認進度沒有遺失）。單獨以結構性變更 commit，前後測試都綠。
- [x] 1.2 [after: 1.1] 結構性變更：單一 `Kill` 改成 `Kills` 清單，全部內容仍只有一個目標，遊戲行為不變。範圍包含 `MissionDefinition`、`MissionObjective`、`ActiveMission.Targets`、內容 JSON 的 `kills`、匯入腳本、`TestContent/missions.json`、`MissionResponse.Kills`、`pnpm generate:api`、前端 `src/game/missions.ts`。驗證：既有任務測試只改欄位形狀後全綠；重新匯入後 `missions.json` 的差異只有 `kill` 變 `kills`。單獨以結構性變更 commit。
- [x] 1.3 [after: 1.2] 實作 Requirement "A mission can have several kill targets"：各目標分開計數、已滿的目標不再增加、全部滿才算達成；依 design 的「任務進度事件多一個 Completed」新增欄位；內容驗證拒絕重複目標、不在任務地區的目標、其他任務的任務怪。先寫失敗測試：`KillCounter_CountsTargetsSeparately`、`KillCounter_FullTargetDoesNotCount`、`Ready_NeedsEveryTarget`、`Adventure_MissionEventCompletedOnlyWhenAllTargetsFull`、`Mission_RejectsRepeatedKillTarget`、`Mission_RejectsKillTargetOutsidePlace`，以及 API 層級的 `Complete_MultiKillMission_RequiresEveryTarget`。
- [x] 1.4 [after: 1.3] 任務頁列出每個擊殺目標的進度，`progressRatio` 合計所有目標；舞台與戰報改用 `event.completed` 判斷達成（Requirement "The battle screen shows NPC encounters" 的達成名牌部分）。先寫失敗測試：`objectiveLines` 列出兩個目標、`progressRatio` 合計、`stageEffectsFor` 部分完成時沒有任務達成名牌、`MissionsScreen` 顯示兩行目標。

## 2. 交寵物與任務怪

- [x] 2.1 [after: 1.3] 實作 Requirement "A regular mission can ask for a pet"：一般任務的 `deliverPet` 走既有的交寵物流程；內容驗證要求至少有一項目標、拒絕 NPC 與其他任務的任務怪。先寫失敗測試：`Complete_RegularPetMission_RemovesCarriedPet`（API 層級）、`Complete_RegularPetMission_StabledPetFails`、`Mission_AcceptsDeliverPetOnly`、`Mission_RejectsPetOfAnotherMission`。
- [x] 2.2 [after: 2.1] 實作 Requirement "A mission's monsters appear while it is open"，依 design 的「任務怪以任務為單位出現」：`KillCounter.Track` 納入只交寵物的任務；任務還有未滿的目標，或沒有擊殺目標但要交寵物時都算開放；任務怪不必是擊殺目標。先寫失敗測試：`Track_IncludesPetOnlyMission`、`Spawnable_PetMissionOpenUntilFinished`、`Spawnable_FullTargetStillAppearsWhileOtherOpen`、`Spawnable_ClosedWhenAllTargetsFullWithItemsLeft`、`KillCounter_NonTargetMissionMonsterDoesNotCount`、`MissionMonster_MayNotBeKillTarget`。

## 3. 技能條件

- [x] 3.1 [after: 1.3] 實作 Requirement "A mission can require a skill level"：`requiredSkill` 不擋接取；看板的 Ready 與回報都要檢查已學等級，回報時未達回 `skillTooLow`；`MissionResponse.RequiredSkill` 帶 `Met`；任務頁列出條件，`missionErrorMessage` 有對應文字。先寫失敗測試：`Accept_WithoutRequiredSkill_Succeeds`、`Complete_SkillBelowRequired_ReturnsSkillTooLow`（API 層級，等級 0、1 失敗，2、3 成功）、`Board_NotReadyWithoutRequiredSkill`、`Mission_RejectsRequiredSkillAboveMaxLevel`，以及前端 `objectiveLines` 的技能條件。

## 4. 遭遇規則

- [x] 4.1 實作 Requirement "Monsters without sub-places appear everywhere in their place"：`MonsterPool` 把空的 `subPlaces` 視為該地區每個子地區都會出現；內容驗證允許有子地區的地區放空值，沒有子地區的地區仍拒絕非空值。先寫失敗測試：`Candidates_EmptySubPlacesEligibleInEverySubPlace`、`Candidates_ListedSubPlacesStillFilter`、`Monster_AcceptsEmptySubPlacesInPlaceWithSubPlaces`、`Monster_RejectsSubPlacesInPlaceWithout`。
- [x] 4.2 [after: 2.2] 實作 Requirement "NPC content is validated"：`MonsterDefinition.Npc`、`NpcMessage`，NPC 不受等級、HP、敏捷下限限制，但必須有 `missionId`、是該任務的擊殺目標、沒有掉落與技能。先寫失敗測試：`Npc_LoadsWithZeroStats`、`Npc_RejectsWithoutMission`、`Npc_RejectsNonTarget`、`Npc_RejectsDrops`、`Monster_RejectsZeroStatsWithoutNpc`。
- [x] 4.3 [after: 4.2] 實作 Requirement "An NPC encounter is not a fight"，依 design 的「NPC 遭遇不算戰鬥」：`AdventureSimulator` 遇到 NPC 不開打、不給報酬、HP／SP 不變、不計場次，目標未滿才 +1，推進 `BattleTiming.FightIntervalSeconds`，輸出 `NpcEncounterEvent`（`npc`）。先寫失敗測試：`Adventure_NpcAdvancesMissionWithoutFight`、`Adventure_NpcWithFullTargetAddsNothing`、`Adventure_NoNpcKeepsRollSequence`，以及 API 層級的 `Advance_NpcEncounter_SavesProgress`。
- [x] 4.4 [after: 4.3] 實作 Requirement "A mercenary can meet an NPC"：`MercenarySimulator` 遇到 NPC 時 +1，不扣次數、不記場次，等下一個間隔。先寫失敗測試：`Mercenary_NpcAdvancesMissionWithoutUsingCount`、`Mercenary_NpcNotInReport`。
- [x] 4.5 [after: 4.3] 完成 Requirement "The battle screen shows NPC encounters"：`pnpm generate:api`；舞台顯示 NPC 名牌、不顯示血條；戰報顯示名稱與訊息，沒有訊息時不留空引號。先寫失敗測試：`applyToStage` 遇到 `npc` 狀態不變、`stageEffectsFor` 回傳名牌、`battleLog` 有訊息與沒訊息兩種。

## 5. 配方前置

- [x] 5.1 實作 Requirement "A recipe can require a completed mission"：`Recipe.RequiredMissionId`；`Synthesis.Check` 依已完成任務判斷，未完成回 `missionIncomplete`，不扣材料與金錢；進行中不算完成；內容驗證拒絕不存在或每日的任務。先寫失敗測試：`Synthesize_RequiredMissionNotAccepted_Fails`、`Synthesize_RequiredMissionActive_Fails`（API 層級，確認材料與金錢不變）、`Synthesize_RequiredMissionCompleted_Runs`、`Recipe_RejectsDailyPrerequisite`、`Recipe_RejectsUnknownPrerequisite`。
- [x] 5.2 [after: 5.1] 實作 Requirement "The recipe list shows the prerequisite"：`GET /api/forge/recipes` 依角色回傳 `RequiredMission`（`Id`、`Title`、`Completed`），鎖住的配方也列出；`pnpm generate:api` 後，`ForgePage` 停用鎖住的配方並顯示任務標題。先寫失敗測試：`Recipes_ReportRequiredMissionState`（API 層級）、`ForgePage` 鎖住與解鎖兩種狀態。

## 6. 匯入與收尾

- [x] 6.1 [after: 2.2, 3.1, 4.2, 5.1] 實作 Requirement "Missions, monsters and recipes use the extended rules" 與 "Data the rules cannot use is listed"：匯入腳本寫入多目標、交寵物、技能條件、NPC（依 design 的「NPC 訊息在匯入時整理」，訊息的 `rn` 換成空格）、沒設子地區的怪、配方前置，並依 design 的「寵物獎勵等級上限調成 530」調整 `MaximumPetRewardLevel`；`gaps.md` 同步縮減。先寫失敗測試：`WogImportTests` 依 spec 的每個情境各一條（任務 29、16、318、325、131、240，怪物 265、399、434、324，配方 174，任務 312 的寵物獎勵），並以舊內容確認它們會失敗。驗證：一般任務 336 個、怪物 461 隻、第二次匯入 `git diff --exit-code` 為 0、`JsonGameContent.Load` 通過；原有內容拿掉新欄位後與舊版相同（用 `jq` 比對）。若實際數字與 `design.md` 不符，回來修正文件並說明原因。
- [x] 6.2 [after: 6.1] 量測新怪的影響：跑平衡報告比較變更前後的勝率，以及 `Advance_SixHours_ReturnsSummaryQuickly` 的耗時，寫進本文件的變更紀錄。平衡數值不調整；耗時增加超過 20% 就先查原因再繼續。
- [ ] 6.3 [after: 1.4, 3.1, 4.4, 4.5, 5.2, 6.1] 手動確認 375×667 的實際畫面，沒有水平捲動：任務頁的多目標、交寵物與技能條件，遇到 NPC 時的舞台與戰報，鐵舖鎖住與解鎖的配方。看不到的狀態可以用本機測試帳號直接改本機資料庫，做法寫進變更紀錄。
- [ ] 6.4 [after: 6.2, 6.3] `./scripts/check.sh` exit code 0。

## 變更紀錄

**量測（6.2，2026-11-07）**：用平衡報告比較匯入前後（第 3 組，每地區、每初階職業）。

- 全部地區合計的勝率：不帶技能 31.6% → 30.4%，帶技能 26.0% → 24.6%。
- 有變化的地區只有新一般怪加入的 6、7、10、12、13、15。除了地區 10，平均變化都在 2 個百分點以內。
- 地區 10（灼熱荒漠）平均下降約 12 個百分點，是唯一超過 10 點的地區：不帶技能的 job-6 71.3% → 58.4%、job-7 71.5% → 57.9%、job-8 88.3% → 69.3%。原因是 `monster-327` 結晶龍（Lv70、HP 4590）加入了遭遇池。依 `design.md`，平衡由其他人負責，這裡只記錄，不改數值。
- 平衡報告耗時：3457～3875 毫秒 → 3434～3896 毫秒，沒有變化。
- `Advance_SixHours_ReturnsSummaryQuickly` 單獨跑 3 次：變更前（`1fb7a68`）1122～1174 毫秒，變更後 1131～1200 毫秒，沒有超過 20%。

