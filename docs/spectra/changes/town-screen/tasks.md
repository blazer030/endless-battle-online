## 1. 補規格：既有行為對照測試

- [x] 1.1 `adventure-catch-up` 三條需求（Advance simulates the elapsed time、Offline time is capped at 24 hours、Only the last 30 seconds are reported as events），逐個 Scenario 對照 `AdventureServiceTests`、`AdventureSimulatorTests` 的既有測試，例如 `Advance_SettlesThroughLastStartedFight`、`Advance_CapsAtTwentyFourHours`、`Advance_ReportsAllEvents_WhenOnlyTheOpeningRestIsSkipped`、`Adventure_EventsCoverAll_WhenOnlyARestIsSkipped`。沒有對應測試的情境就補上，例如 `Advance_NoElapsedTimeWritesNothing`、`Advance_LongAbsenceSummarizesAllFights`（3600 秒：summary 計入全部場次、`EventsCoverAll` 為 false）。只描述現況：新測試若沒過，表示規格寫錯，要先停下來回報，不能改程式。驗證：在 tasks 的完成紀錄寫下每個 Scenario 對應的測試名稱；`dotnet test --filter-class "*Adventure*"` 全綠。
  - 對照結果：Advance after a pause → `Advance_SettlesThroughLastStartedFight`；No time elapsed → `Advance_ZeroElapsedInRemovedPlaceReturnsEmpty`、`Advance_ReturnsNothingAndWritesNothing_WhenClockIsBehind`；Two days offline → `Advance_CapsAtTwentyFourHours`（30 小時）；Long absence reports a summary → 新增 `Advance_LongAbsenceSummarizesAllFights`（現有行為，一跑就過）；Only an opening rest is skipped → `Advance_ReportsAllEvents_WhenOnlyTheOpeningRestIsSkipped`、`Adventure_EventsCoverAll_WhenOnlyARestIsSkipped`。
- [x] 1.2 `elixir-buffs` 的既有部分（Drinking an elixir starts a bonus 的「只對之後的場次生效」、One bonus per kind、Elixir and lodging of the same kind multiply），對照 `InventoryServiceTests`（`Use_ElixirDoesNotApplyToUnsettledFightsBeforeIt` 等）、`ConsumableUseTests.Elixir_StartsNowAndReplacesSameKind`、`BuffsTests`、`AdventureSimulatorTests.Adventure_LodgingBoostsWinExpInsideWindow`。缺的補上，例如 `Multiplier_ElixirAndLodgingOfSameKindMultiply`（經驗 1.05 × 1.10 ＝ 1.155）、`Use_DifferentKindsCoexist`。驗證：完成紀錄寫下每個 Scenario 對應的測試；後端相關測試全綠。
  - 對照結果：Elixir applies only to later battles → `InventoryServiceTests.Use_ElixirDoesNotApplyToUnsettledFightsBeforeIt`、`Use_ElixirSettlesElapsedTimeUnderTheBuffItReplaces`；Use records the item → 任務 2.2；Same kind replaces、Different kinds coexist → `BuffRepositoryTests.Save_OverwritesSameKind`（同種覆蓋倍率與窗口、不同種另存）、`ConsumableUseTests.Elixir_StartsNowAndReplacesSameKind`；Elixir and lodging stack → 新增 `InnTests.Multiplier_ElixirAndLodgingOfSameKindMultiply`（1.155，現有行為），另有 `Combined_MultipliesElixirAndLodging`、`AdventureSimulatorTests.Adventure_LodgingAndElixirMultiply`；Expired bonuses are not listed → `CharacterEndpointsTests.Buffs_ListsOnlyActive`。

## 2. 密藥記下最後喝的道具（後端）

- [x] 2.1 依「密藥記下最後喝的道具」與「硬規則」，新增 `backend/db/ddl/V000000020__add_character_buff_item.sql`：`character_buffs` 加 `item_id VARCHAR(64) NULL`。照 migration → `docker compose up -d` → `./scripts/scaffold-db.sh` 的順序執行，不手改產生檔。先寫失敗測試 `DatabaseSchemaTests.Schema_CharacterBuffHasNullableItemId`。驗證：該測試與 `./scripts/check-sql.sh` 通過，`Persistence/Models/CharacterBuff.cs` 多出 `ItemId`。
- [x] 2.2 Domain 的 `CharacterBuff` 加 `ItemId`，`Buffs.Start` 收道具 id，`ConsumableUse` 喝密藥時寫入道具 id，`Inn.ToBuff` 的 `ItemId` 為 null；`BuffRepository` 讀寫 `item_id`。這是 Drinking an elixir starts a bonus 的「記下道具」，以及 One bonus per kind 的「覆蓋時換成新道具」。先寫失敗測試：`ConsumableUseTests.Elixir_RecordsTheItem`（範例 `item-2257`、08:00 到 10:00）、`BuffRepositoryTests.Buffs_SaveReplacesTheItem`（同種再存一次，`item_id` 換成新的）、`BuffRepositoryTests.Buffs_ReadLegacyRowWithoutItem`（`item_id` 為 NULL 時讀回 null）。驗證：這些測試與既有密藥測試全綠。[after: 2.1]
- [x] 2.3 Active bonus list 與 Lodging response carries its start：`CharacterService.ListBuffsAsync` 從遊戲內容查出道具名稱（`FindItem`，查不到時 `ItemId`、`ItemName` 都是 null）；`BuffResponse` 加 `StartsAt`、`ItemId`、`ItemName`，`POST /api/inventory/use` 的 `buff` 形狀相同；`LodgingResponse` 加 `StartsAt`。先寫失敗的 API 測試：`CharacterEndpointsTests.Buffs_EntryCarriesTheItem`（`item-2255`、`戰鬥熟練手冊LV1`，用 TestContent 對應的道具）、`Buffs_EntryWithoutRecordedItemIsNull`、`InventoryEndpointsTests.Use_ElixirResponseCarriesTheItem`、`InnEndpointsTests.Inn_LodgingCarriesStartAndEnd`（範例 08:00 到 16:00）。重新產生 OpenAPI 與前端 `schema.ts`（`pnpm generate:api`）。前端 `BuffConverter` 加 `startsAt`（轉成 `Date`）、`itemId`、`itemName`，`InnConverter`、`InnBookConverter` 的 `lodging` 加 `startsAt`；先在 `lodgingData.test.ts` 寫失敗測試 `BuffConverter_ConvertsTheStartAndItem`、`BuffConverter_KeepsAMissingItemAsNull`、`InnConverter_ConvertsTheLodgingStart`。驗證：後端測試與前端 `pnpm check` 全綠。[after: 2.2]

## 3. 舊外殼從城鎮打開

- [x] 3.1 依「點設施對應的舊分頁」與「城鎮不連線，靠卸載舊外殼停止輪詢」，`AdventureScreen` 加 `initialTab`、`initialBagPage`、`onReturnToTown`，`BagScreen` 加 `initialPage`。舊上方欄最左邊加「回城鎮」按鈕：按下時先 `discardQueued`，再把顯示中的角色交給 `onReturnToTown`。這是 Facilities open the old tabbed screen 的舊外殼部分，以及 Town makes no adventure requests 的「回城鎮停止輪詢」。先寫失敗測試：`AdventureScreen.test.tsx` 的 `OpensAtTheGivenTabAndBagPage`（`bag`＋`inn` 顯示旅館子頁）、`ReturnToTownHandsBackTheLatestCharacter`、`ReturnToTownStopsPolling`（卸載後推進 mock 不再被呼叫）。驗證：前端 `pnpm check` 全綠；360 寬時上方欄不換行（瀏覽器確認，排除：樣式不寫自動測試）。
  - 實作紀錄：`onReturnToTown` 是選填，沒傳時不顯示「回城鎮」（6.3 之前 App 還直接開舊外殼，不放沒作用的按鈕）；GameScreen 一律會傳。`ReturnToTownStopsPolling` 一跑就過（既有的卸載清理已會停止輪詢）。360 寬的確認併入 7.1。

## 4. 城鎮的純模型

- [ ] 4.1 依「換段的狀態放在 `src/game/`」，新增 `frontend/src/game/town.ts`：`panoramaOffsets(stageHeight)`、`swipeTarget(segment, deltaX, deltaY)`、`timeOfDay(date)`、`remainingText(endsAt, now)`、`isEndingSoon(endsAt, now)`，以及加成列的 `bonusRows(buffs, lodging, rooms, now)`。`bonusRows` 的規則：住宿排第一、密藥依種類排序；名稱用道具名或「種類 ×倍率」，有道具時才有第二行；剩餘比例；已到期的不列出；`hasExpired` 表示要不要重新讀取。先寫失敗測試，用規格的 Example 表當測資：Town panorama segments 的偏移（624 → 452／728、780 → 614／1002）、Change segment by sign or swipe 的滑動結果表、Bonus row and list 的剩餘時間表、Town background follows the time of day 的時刻表、`BonusRows_NamesTheItemOrTheKind`（`神力秘藥LV1`＋`物攻 ×1.05`；舊資料 `物防 ×1.05` 沒有第二行）、`BonusRows_PutsLodgingFirstThenKindOrder`。驗證：`town.test.ts` 全綠、`pnpm check` 全綠。

## 5. 立繪與設計素材

- [ ] 5.1 依「立繪與設計素材」，用 `cd tools/assets && pnpm cli import items item-2257 <來源圖>`、`import items item-2255 <來源圖>` 匯入神力秘藥（`.design-work/assets/8a7c67f613bdcf739873435319f65458.*`）與戰鬥熟練手冊（`e233c86386e9d40d3c0218ed34953c76.*`）的圖；立繪 `portraits/job-6～9-male|female` 已在 R2，不重新匯入。上傳需要 repo 根目錄的 `.env.r2`。驗證：`pnpm cli missing items` 不再列出 item-2255、item-2257；`content-manifest.json` 多出這兩個 items；`frontend` 的 `validateManifest` 測試通過。
- [ ] 5.2 用 `pnpm cli design town <名稱> <來源圖>` 把城鎮素材轉成 `frontend/src/assets/town/*.webp`：四時段全景（`b1b11a9c`、`cbcc9c3b`、`75b36d0b`、`44221259`）、紙條 `35548540`、羅盤 `d55573c6`、錢袋 `c85813b2`、角色／背包／同伴圖示（`1b66a6085`、`4ca152c1`、`e0b38181`）、六個房型徽章。`docs/art/README.md` 的設計素材清單加上這些。驗證：檔案存在且被畫面 import（tsc 不報錯）；README 清單與檔案一致（內容檢查）。

## 6. 城鎮畫面

- [ ] 6.1 新增城鎮畫面 `frontend/src/ui/town/`，放在 `StageFrame` 裡，照 Design Source 的數值實作：三段全景與換段（點紙籤、滑動；`StageFrame` 提供縮放倍率，讓位移換算成舞台 px）、只顯示自己那段的紙籤與名牌、Town status board、Town portrait、Town background follows the time of day。新色碼加進 `theme.css`（硬規則 3），同值的沿用既有變數。先寫失敗測試 `TownScreen.test.tsx`：`ShowsTheGateWithTheEnteredCharacter`（Lv 6、HP 53%、SP 83%、`12,345`、角色紙籤 `Lv 6`、背包與同伴紙籤沒有數字）、`SignMovesToTheMarket`、`SwipeLeftMovesOneSegment`、`ClickRightAfterSwipeIsIgnored`、`OtherSegmentsSignsAreInactive`、`PortraitUsesJobAndSex`（`portraits/job-7-female`）、`MakesNoAdventureRequests`（推進 mock 在數個輪詢間隔後仍是 0 次）。驗證：測試與 `pnpm check` 全綠。[after: 4.1, 5.1, 5.2]
- [ ] 6.2 依「加成列在本機倒數，到期才重新讀取」，城鎮進入時讀 `getBuffs` 與 `getInn`，顯示加成列與清單（Bonus row and list），每 `BuffTickMilliseconds` 重算；任一項到期就重新讀一次兩個 API（Bonus countdown and refresh）。讀取失敗時隱藏加成列，不顯示錯誤。先寫失敗測試：`BonusRowShowsLodgingThenElixirs`、`BonusListOpensAndClosesOnOutsideClick`、`EndingSoonBadgeIsMarked`、`ExpiredBonusTriggersOneRefresh`（假時鐘越過結束時間後，兩個 API 各被呼叫一次，到期項目消失）、`BonusRowHiddenWhenLoadingFails`；換段時清單收起。驗證：測試與 `pnpm check` 全綠。[after: 2.3, 6.1]
- [ ] 6.3 依「讀到角色先進城鎮，舊外殼改成從城鎮打開」，新增 `GameScreen`，管理城鎮與舊外殼兩種狀態並保管角色；`App.tsx` 的 `game` 階段改用它（Enter the town after loading the character），點設施依對照表打開舊外殼（Facilities open the old tabbed screen），回城鎮時回到城門口。先寫失敗測試：`App.test.tsx` 的 `OpeningWithACharacterShowsTheTown`（沒有推進請求）、`CreatedCharacterEntersTheTown`；`GameScreen.test.tsx` 的 `InnOpensTheBagTabAtTheInnPage`、`EachFacilityOpensItsTab`（依對照表）、`ReturnToTownShowsTheGateWithTheLatestCharacter`。驗證：測試與 `pnpm check` 全綠。[after: 3.1, 6.1]

## 7. 收尾

- [ ] 7.1 手動驗證：`docker compose up -d`、啟動 API 與 `pnpm dev`，以既有角色登入；先在資料庫補住宿與兩種密藥，其中一種剩不到 10 分鐘，另一列 `item_id` 為 NULL。在 390×624、390×780、1440×720 確認以下項目並截圖記錄每個畫面實際出現的元素：
  - 三段換段（點紙籤、滑鼠拖曳滑動）。
  - 加成列與清單（道具名、舊資料那一列、警示）。
  - 時段背景、立繪。
  - 每個設施打開的分頁和子頁，以及回城鎮。
  - 網路紀錄：停在城鎮時沒有 `POST /api/adventure/advance`，進舊外殼後才開始。
  [after: 6.2, 6.3]
- [ ] 7.2 `./scripts/check.sh` exit code 0。[after: 7.1]
