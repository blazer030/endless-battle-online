## 1. 技能規則與 API

- [x] 1.1 `skill-learning` 規格的 Skill overview lists the current job's skills and other learned skills、Learning raises a skill by one level、Learning rejections and their order 中描述現況的部分（不含 `jobId`、`NOT_LEARNED`），逐個 Scenario／Example 對照既有測試：`SkillLearningTests`、`SkillServiceTests`、`SkillEndpointsTests`。缺的照 Example 用正式內容補，例如：
  - `SkillServiceTests.Get_CurrentJobSkillsComeFirst`（戰士學過魔力增幅 → 爆氣、防禦心得、破滅一擊、嗜血、魔力增幅）；
  - `SkillServiceTests.Learn_FirstLevelSpendsMoneyAndSkillPoints`（1,000 金錢、100 技能點學爆氣 → 500、65，熟練度不動）；
  - `SkillLearningTests.Reasons_ExampleTable` 的前三列；
  - 被拒時什麼都不變。
  只描述現況：新測試若沒過，表示規格寫錯，先停下來回報，不改程式。驗證：完成紀錄寫下每個 Scenario 對應的測試；`dotnet test --filter-class "*Skill*"` 全綠。
  - 對照結果：
    - Current job skills come first → 新增 `SkillServiceTests.Get_CurrentJobSkillsComeFirst`（正式內容）；
    - Unlearned skill → `Get_ListsCurrentJobSkillsAndLearnedOthers`；
    - Learning the first level → 新增 `Learn_FirstLevelSpendsMoneyAndSkillPoints`（熟練度仍是 0）；
    - Learning the next level → 新增 `Learn_NextLevelSpendsMoneyAndSkillPoints`；
    - Every reason in order 前三列 → 新增 `Learn_ReasonsExampleTable`（放在 Application 層，用正式內容比 Domain 測試好準備；也驗被拒時金錢、技能點、技能書不變），另有 `SkillLearningTests.Evaluate_ReportsEachCondition`、`Learn_ReportsMoneyAndProficiencyTogether`；
    - `SKILL_UNKNOWN` → `Learn_RejectsUnknownSkill`；
    - `REQUEST_INVALID` → `SkillEndpointsTests.Learn_MissingSkillId_RequestInvalid`；
    - Rejection changes nothing → `Learn_DeductsNothing_WhenProficiencyInsufficient`、`Learn_ConflictLeavesProficiencyAndBook`、`Learn_ReasonsExampleTable`；
    - `JOB_UNKNOWN` 與以前職業的列留到 1.3。
  - 新測試第一次就通過，都是描述現況。`dotnet test --filter-class "*Skill*"` 243 項全綠。
- [x] 1.2 結構性變更（單獨以 ♻️ refactor commit）：依 design「學技能可以指定職業，條件仍在 Domain」，把 `SkillTeaching.TaughtByCurrentJob` 改名為 `Teaches`、`LearnCheck.TaughtByCurrentJob` 改名為 `Teaches`。`SkillService` 與測試跟著改名，對外的 `taughtByCurrentJob` 欄位不變。驗證：改名前後 `dotnet test --filter-class "*Skill*"` 都全綠，`backend/openapi/` 沒有差異。[after: 1.1]
- [x] 1.3 實作 Upgrading a learned skill with a previous job's skill points，以及 Learning rejections and their order 的新原因。
  - Domain：`SkillTeaching` 加 `ThroughPreviousJob`；`LearnCheck` 加 `Learned` 與 `NOT_LEARNED`（排在 `WRONG_JOB` 之後）。
  - Application：`SkillService.LearnAsync` 收 `jobId`，決定用哪個職業，順序是：不帶或等於目前職業 → 未知 `JOB_UNKNOWN` → 沒持有只回 `WRONG_JOB`；扣那個職業的技能點。
  - Api：`LearnSkillRequest` 加 `JobId`。
  - 先寫失敗測試：
    - `SkillLearningTests.PreviousJob_NotLearnedIsRejected`；
    - `SkillLearningTests.Reasons_ExampleTable` 的後四列；
    - `SkillServiceTests.Learn_PreviousJobSpendsThatJobsSkillPoints`（戰士 3,000 金錢、術士 300 技能點、魔力增幅 Lv1 → Lv2、1,000、65，戰士技能點不變、仍是戰士）；
    - `SkillServiceTests.Learn_PreviousJobNotEnoughSkillPoints`；
    - `SkillServiceTests.Learn_JobNeverHeldOnlyWrongJob`；
    - `SkillEndpointsTests.Learn_WithJobIdUsesThatJob`（Testcontainers，驗接線與 400 的 `reasons`）。
  驗證：測試全綠。[after: 1.2]
  - 實作紀錄：
    - Domain：`SkillTeaching` 加選填 `ThroughPreviousJob`，`LearnCheck` 加 `Learned`，原因 `NOT_LEARNED` 排在 `WRONG_JOB` 後；沿用 `ErrorCodes.NotLearned`。
    - Application：`SkillService.LearnAsync(accountId, skillId, jobId, …)`，原本三個參數的版本轉呼叫它；`TeachingOf` 改收職業 id。
    - Api：`LearnSkillRequest` 加 `JobId`；`backend/openapi/` 與 `schema.ts` 重新產生。
    - 例子表的後四列拆成：`SkillServiceTests.Learn_PreviousJobNotLearnedIsRejected`、`Learn_JobNeverHeldOnlyWrongJob`、`Learn_UnknownJob`，另有 `Learn_CurrentJobIdSameAsNone`；Domain 是 `SkillLearningTests.PreviousJob_NotLearnedIsRejected`、`PreviousJob_LearnedSkillCanBeUpgraded`。
    - 先跑新測試因 `Learned`、`ThroughPreviousJob`、四參數 `LearnAsync` 不存在而編譯失敗；實作後 `dotnet test --filter-class "*Skill*"` 252 項全綠（含 Testcontainers 的 `SkillEndpointsTests.Learn_WithJobIdUsesThatJob`）。
- [x] 1.4 依 design「數值列在 Application 拆，文字照 SkillSummary」實作 Skill level detail rows：新增 `SkillSummary.RowsOf(SkillLevel)` 回 `SkillDetailRow(Kind, Text)`，條件與效果文字與 `Of` 共用，`Of` 輸出不變。先寫失敗測試：
  - `SkillSummaryTests.Rows_ActiveSkill`（爆氣 Lv1 → `機率 40%`、`SP 2`、`自己物攻 +1%，3 回合`）；
  - `Rows_PassiveWithoutConditionsOrCosts`（華爾滋 → `常駐`＋效果，沒有消耗與冷卻）；
  - `Rows_CooldownIsLast`（測試內容冷卻 4 → `4 回合`）；
  - `Rows_CostTexts`（`SP 最大值×N%`、`全部 SP`、`HP N`、`金錢 N`）。
  驗證：`SkillSummaryTests` 全綠，既有 `Of` 測試不改也通過。[after: 1.3]
  - 實作紀錄：`SkillSummary.RowsOf` 與 `SkillDetailKind`、`SkillDetailRow`（放在 `SkillSummary.cs`）；消耗文字抽成 `CostText`，`Of` 的「消耗」前綴照原本的空白規則接回去。冷卻那條用測試自建的等級（正式內容沒有冷卻）。先跑因型別不存在而編譯失敗；實作後 `SkillSummaryTests` 8 項全綠（原本 4 項不改），`*Skill*` 256 項全綠。
- [ ] 1.5 依 design「技能回應加每個職業的清單，舊欄位不動」實作 Skill overview lists each held job's skills。
  - `SkillOverview` 加 `Jobs`（`JobSkills`、`JobSkillView`）：目前職業第一，其他持有的職業照 `jobs.json` 順序；以前的職業只列已學的；每個職業用自己的技能點判斷。
  - `SkillEndpoints` 與 `Contracts.cs` 加 `JobSkillsResponse`、`JobSkillResponse`、`JobSkillNextLevelResponse`、`SkillDetailRowResponse`；`GET` 與 `learn` 都帶，頂層欄位不變。
  - 先寫失敗測試：
    - `SkillServiceTests.Jobs_PreviousJobListsOnlyLearned`（Scenario 的術士 1,882 技能點只列魔力增幅）；
    - `Jobs_EachJobJudgesWithItsOwnSkillPoints`；
    - `Jobs_HeldJobWithoutLearnedSkillsIsEmpty`；
    - `Jobs_RowsOfCurrentAndNextLevel`；
    - `SkillEndpointsTests.Get_CarriesJobsWithoutReasons`。
  - 之後 `dotnet build` 產生 `backend/openapi/`，`cd frontend && pnpm generate:api` 更新 `schema.ts`（`git add` 產生檔）。
  驗證：測試全綠；`./scripts/check.sh` 的 API 文件與型別檢查通過。[after: 1.4]

## 2. 前端資料與純模型

- [ ] 2.1 前端轉換與文字：
  - `frontend/src/api/skillData.ts` 讀 `jobs`（`JobSkillsVo`、`JobSkill`、數值列 `kind` 要是四種之一），依 DTO→VO 慣例驗證欄位；
  - client 的 `learnSkill(skillId, jobId?)` 只在有 `jobId` 時送出這個欄位；
  - 新增 `getAccount()` 讀 `GET /api/accounts/me`（`AccountVo { accountId, username }`）；
  - `game/skills.ts`：`LearnConditionText.notEnoughProficiency` 改「技能點不足」，加 `NOT_LEARNED` 的文字；舊 `SkillsScreen` 的測試跟著改字。
  - 先寫失敗測試：`skillData.test.ts` 的 `SkillOverviewConverter_ReadsJobs`、`SkillOverviewConverter_RejectsUnknownRowKind`；`client.test.ts` 的 `LearnSkill_SendsJobIdOnlyWhenGiven`、`GetAccount_ReadsUsername`；`skills.test.ts` 的原因文字。
  驗證：測試與 `pnpm check` 全綠。[after: 1.5]
- [ ] 2.2 依 design「角色頁的資料由 `src/game/characterScreen.ts` 的純函式算出」新增 `characterCard`、`abilityRows`、`combatRows`、`jobSwitcher`、`skillCard`、`learnedPopup`、`learnFailureText`、`characterLayout`、`portraitOffset`、`characterRevealOrigin`。先寫失敗測試 `characterScreen.test.ts`，用規格的 Example 當測資：
  - Card values（收尾者 Lv 162、70%、`2,450 / 3,500`、EXP 42%）與熟練度 100% 上限；
  - 有正負號的加成；
  - Switcher of the current job（`Class I・見習・已學 3 / 4 招`）；
  - 停用原因 `前置技能：防禦心得 Lv 3・技能點不足`；
  - 花費行與不夠的標記、滿級；
  - 升級與習得彈窗、精通、變了的數值列；
  - 失敗文字對照表；
  - Layout values 兩列與 Feet offset。
  驗證：`characterScreen.test.ts` 全綠、`pnpm check` 全綠。[after: 2.1]

## 3. 共用回饋與素材

- [ ] 3.1 依 design「共用回饋元件放 `src/ui/feedback/`」實作 Waiting ink dots while an action is sent、Shared failure popup、Feedback timings live with the other motion constants：
  - `useActionFeedback`、`ActionWait`（沿用 `InkDots`）、`Popup`、`FailurePopup`；
  - 時長常數進 `motion/timings.ts`，`ActionWaitDelaySeconds` 取 `InkDotsDelaySeconds`；
  - 新色碼進 `theme.css`。
  - 先寫失敗測試（fake timers）：`ActionWait.test.tsx` 的 `QuickResponseShowsNoInkDots`（0.2 秒）、`SlowResponseShowsInkDots`（1.5 秒）、`InkDotsStayTheMinimumTime`（0.5 秒回應 → 停到 0.75 秒）、`BlocksPressesWhileWaiting`；`FailurePopup.test.tsx` 的 `ShowsTheText`、`ClosesOnOutsidePress`；`timings.test.ts` 的 `ActionWaitDelayMatchesInkDots`。
  驗證：測試與 `pnpm check` 全綠。
- [ ] 3.2 設計素材：
  - 用 `cd tools/assets && pnpm cli design character <名稱> <來源圖>` 轉出 Design Source 表的素材到 `frontend/src/assets/character/*.webp`：裝備間、登記證、索引籤兩張、技能圖示圓兩張、菱形兩張；
  - 前端已有同一張圖（紅印章、木條、金幣等）就比對像素後沿用；
  - `docs/art/README.md` 加 `character/` 一節，註明換立繪要重量雙腳偏移；
  - `.claude/skills/ebo-canvas` 的板子表加 CharacterFit。
  驗證：檔案存在；README 清單與檔案一致（內容檢查）；`cd tools/assets && pnpm check` 通過。

## 4. 角色頁畫面

- [ ] 4.1 角色頁外框：Loading and load failure、Registration card and portrait、Wallet、Character screen layout follows the stage height（`frontend/src/ui/character/CharacterScreen.tsx` 與子元件）：
  - 讀取依 design「打開時一次讀六個 API，任何一個失敗就整頁失敗」；
  - 登記證不含轉職鈕的接線；
  - 兩張索引籤（預設能力頁）、「城鎮」返回。
  - 先寫失敗測試 `CharacterScreen.test.tsx`：`LoadsSixRequestsOnOpen`、`LoadingShowsFaintBlocks`、`AnyFailureShowsRetry`、`RetryLoadsAgain`、`CardShowsTheCharacter`、`PortraitBySex`、`JobWithoutPortraitShowsPlaceholder`、`WalletShowsMoney`、`OpensOnTheAbilitiesPage`、`BackReturnsToTown`。
  驗證：測試與 `pnpm check` 全綠。[after: 2.2, 3.1, 3.2]
- [ ] 4.2 能力頁：Abilities page（七項能力與加成、戰鬥數值、密藥與住宿用 `bonusRows` 每秒倒數與 10 分鐘轉紅、空清單文字、到期消失不重讀）與 Sign out from the abilities page（確認框寫帳號、取消、點外面關、確定後呼叫 `onSignOut`、不送請求）。先寫失敗測試：`StatShowsEquipmentBonus`、`StatWithoutBonus`、`BonusListLodgingFirst`、`BonusCountsDownEverySecond`、`NothingActiveText`、`ExpiredBonusDisappearsWithoutRequest`、`SignOutAsksWithUsername`、`CancelSignOut`、`ConfirmSignOutCallsBack`。驗證：測試與 `pnpm check` 全綠。[after: 4.1]
- [ ] 4.3 技能頁：Skills page job switcher 與 Skill cards。
  - 職業切換：打開停目前職業、標頭、分組清單、選職業後只列那個職業的技能、不出現「熟練度」；
  - 技能卡：Lv 與未習得、菱形、主動／被動、「（說明待補）」、一次只展開一張、兩欄數值列與綠字、花費行與紅字、停用與原因、滿級精通。
  - 先寫失敗測試：`SkillsPageStartsOnCurrentJob`、`SwitcherShowsSkillPoints`、`ChoosingPreviousJobListsLearnedOnly`、`SkillsPageNeverSaysProficiency`、`OneCardExpandedAtATime`、`CardShowsBothLevelColumns`、`DisabledSealShowsReasons`、`MaxLevelShowsSeal`、`EmptyDescriptionShowsPending`。
  驗證：測試與 `pnpm check` 全綠。[after: 4.2]
- [ ] 4.4 Learning from the skills page：
  - 按下印章送 `learnSkill`（以前的職業帶 `jobId`）並套等待墨點；
  - 成功時換成回傳的技能與角色、留在同一職業同一張卡、更新錢袋與交出角色，跳升級成功或習得成功彈窗；
  - 失敗時跳共用失敗彈窗、畫面不變。
  - 先寫失敗測試：`UpgradeShowsUpgradedPopup`、`LearnShowsLearnedPopup`、`UpgradeToMaxShowsMasterSeal`、`PreviousJobSendsJobId`、`SuccessKeepsJobAndCard`、`SuccessUpdatesWalletAndCharacter`、`RejectionShowsReasonText`、`NetworkFailureShowsConnectionFailed`、`WaitingBlocksSecondPress`。
  驗證：測試與 `pnpm check` 全綠。[after: 4.3]
- [ ] 4.5 依 design「角色頁是 GameScreen 的第七種展開設施」接線，並實作 `town-screen` 的 Facilities open the old tabbed screen 與 Enter and leave the character screen：
  - `GameScreen`：`revealed` 加 `character`（圓心 `characterRevealOrigin`、收回停城門口、帶最新角色）、`facilityViews` 拿掉 `character`；
  - 轉職鈕開舊外殼技能分頁，城鎮預載角色頁素材；
  - `AdventureScreen` 拿掉「角色」分頁與 `onSignOut`，`App` 的登出改傳給 `GameScreen`→角色頁；
  - 刪舊 `frontend/src/ui/CharacterScreen.tsx`、`CharacterScreen.css`、`CharacterScreen.test.tsx`；
  - `docs/superpowers/specs/2026-10-23-deferred-backlog.md` 拿掉只跟舊 `CharacterScreen` 有關的條目（卸載時清倒數的測試缺口）。
  - 先寫失敗測試：`GameScreen.test.tsx` 的 `CharacterOpensTheCharacterScreen`、`BackFromCharacterShowsTheGate`、`BackFromCharacterCarriesTheLatestCharacter`、`JobChangeOpensTheSkillsTab`、`CharacterScreenMakesNoAdventureRequests`，`EachFacilityOpensItsTab` 拿掉角色；`AdventureScreen.test.tsx` 的分頁不含「角色」（改寫依賴舊角色分頁的既有測試）；`App.test.tsx` 的 `SignOutFromCharacterScreenShowsTitle`；`screenImages.test.ts` 包含角色頁素材。
  驗證：測試與 `pnpm check` 全綠。[after: 4.4]

## 5. 收尾

- [ ] 5.1 手動驗證。
  - 準備：`docker compose up -d`、啟動 API 與 `pnpm dev`，以本機測試角色登入；在資料庫準備：
    - Lv 162 戰士，當過術士與盜賊；
    - 戰士學了 3 招、術士學了魔力增幅；
    - 有住宿與兩種密藥（其中一個剩不到 10 分鐘）；
    - 另準備一個狂戰士（沒有立繪）。
  - 在 390×624、390×780、1440×720 逐格截圖，記錄每個畫面實際出現的元素，對照 CharacterFit：
    - 登記證、立繪站位（雙腳中心線）、錢袋；
    - 能力頁、密藥與住宿倒數、登出確認；
    - 技能頁：目前職業、切到術士、停用原因、滿級；
    - 升級成功、習得成功、以前職業升級；
    - 失敗彈窗：頁內攔截請求模擬連線失敗與金錢不足；
    - 等待墨點：攔截請求延遲 1.5 秒；
    - 讀取中、讀取失敗。
  - 另外確認：
    - 展開與收回的圓心落在「角色」紙籤、回城門口；
    - 轉職鈕開舊外殼技能分頁；
    - 停在角色頁時沒有 `POST /api/adventure/advance`；
    - 舊外殼沒有角色分頁；
    - 登出回標題；
    - 記錄 `/api/skills` 回應實際大小。
  [after: 4.5]
- [ ] 5.2 依 design「硬規則」複查（Api 只轉型、條件在 Domain；色碼只在 `theme.css`；內容圖只經 `ContentImage`；`schema.ts` 為產生檔），`./scripts/check.sh` exit code 0。[after: 1.1, 5.1]
