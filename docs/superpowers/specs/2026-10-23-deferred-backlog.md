# 延後項目清單

- 日期：2026-10-23（計畫 10 收尾時整理）
- 用途：計畫 10 結束時仍未處理的延後項目。SDD 帳本與記憶檔 `ebo-plan-10-backlog.md` 在合併後刪除，這份是唯一清單；之後的計畫從這裡挑，做完就刪掉那一條。
- 來源：計畫 10 的 SDD 帳本（各任務審查標為 backlog／deferred 的項目）、各任務報告的「未處理」段落、記憶檔裡計畫 10「延後項目分派」排了任務但實際沒有完成的項目。
- 已決定不做的項目見 `2026-10-22-wog-remake-rulings.md`（四）；計畫 10 怎麼分派記憶檔，見同檔附錄。

## 需要先做產品決定

- **內容移除了角色所在的地區**：有經過時間時推進仍然 500（`AdventureService.cs:67`、移動時的 `WorldService.cs:80`），`GET /api/characters/me` 也會 500（`CharacterEndpoints.cs:83` 的 `ToResponse` 呼叫 `GetPlace`）。計畫 10 Task 9 只修了經過時間為 0 時能移出去。要先決定被移除的地區怎麼處理（跳過那段時間？送回起始地區？）。來源：Task 9 報告、審查。
- **內容移除了玩家持有的道具（孤兒列）**：列會被隱藏而不是 500，但被隱藏的魔石在那件裝備被精鍊、鑲嵌、存倉時會從資料庫消失；隱藏的列不計入背包容量；對著「看起來是空的孔」鑲嵌、`Delivery` 也可能選到它。完整修正橫跨 Domain／Application／Infrastructure。來源：Task 8 裁定。

## 前端

- **移動前送出的輪詢在移動後才回來**：`BattlePlayback.enqueue` 會把已確認的角色換回舊地點、把舊地區的戰鬥排在「你前往」之後，地點標題最多 10 秒顯示舊地點。建議 `useAdventureLoop`（`useAdventureLoop.ts:39`）維護一個 generation，移動時遞增，回應回來時 generation 已變就整包丟棄。來源：Task 10 審查。
- **背景重載與使用者操作沒有先後保護**：`BagScreen.tsx`、`MissionsScreen.tsx`、`SkillsScreen.tsx` 的背景重載若晚於賣出、接任務、學技能的回應，舊資料會蓋掉新結果；初次載入失敗後背景重載成功也不會清掉錯誤狀態。修法：`busy` 時略過背景重載，或只接受最新一次請求的回應。來源：Task 10 審查。
- **通行倒數的判斷寫在畫面裡**：`AdventureScreen.tsx` 裡 `areaPassText` 的三行判斷建議抽到 `game/world.ts` 並寫單元測試，釘住「同 placeId、不同 subPlace 不顯示」。來源：Task 10 審查。
- `WorldPanel.tsx:166` 在 render 裡呼叫 `new Date()`。來源：Task 4 實作者疑慮。
- 移動後佇列清空了，但舞台上舊怪物的血條會留到下一場遭遇（≤10 秒）。來源：Task 10 審查。
- 無障礙：賣出確認的文字沒有 `role="status"`（「放生」也沒有）；有合約時停用的「僱用」按鈕沒有 `aria-describedby` 指向「只能僱用一位傭兵」。來源：Task 10 審查。
- `nameInvalid` 的文案只描述長度規則。來源：Task 3 審查。

## 後端

- **EF sentinel**：冒險設定欄位有資料庫預設值，以非預設設定**新增**角色時，值為 0 的欄位會被 EF 略過、改用資料庫預設。目前新角色一律用預設設定，沒有觸發路徑。來源：Task 3 實作者疑慮。

## 部署與工具

- `api` 沒有 healthcheck，`web` 只 `depends_on: api`（started），剛啟動時 `/api/` 可能短暫回 502。可加 api healthcheck，讓 web 等 `service_healthy`。來源：Task 1 審查（README 已註明）。
- `frontend/nginx.conf` 送出 `X-Forwarded-Proto`，但 API 沒有啟用 `UseForwardedHeaders`，目前沒有用途。來源：Task 1 審查。
- `scripts/smoke-compose.sh` 會讀 repo 根目錄 `.env` 的 `EBO_DB_*`（埠已覆寫），沒有完全與 `.env` 隔離。來源：Task 1 審查。
- `scripts/import-art.sh` 在 `magick` 失敗時可能留下寫到一半的 webp；改成先寫暫存檔再 `mv`。來源：Task 11 審查。
- ImageMagick 的匯入流程沒有實跑過（這台機器沒裝 ImageMagick 7）；使用者第一次匯入 PNG 時順便確認參數。來源：Task 11 裁定。
- `artAssets.test.ts` 的「每張美術圖都合規」在第一張圖進來前沒有真正的輸入。來源：Task 11 審查。

## 測試缺口

- 計畫 8 的前端測試：`MissionsScreen` 的重複送出、確認後重置、卡片位置沒有測試（計畫 10 分派給 Task 9，但 Task 9 只做後端）。來源：記憶檔、`MissionsScreen.test.tsx` 現況。
- 容器啟動（Task 2）：fixture 把所有 `TimeoutException` 都改寫成「3 分鐘內沒有就緒」，Testcontainers 自己的逾時也會被改寫；「start 完全不理 token」只靠 `WaitAsync` 逃生的路徑、「第一次丟例外、第二次成功」、「dispose 本身丟例外」都沒有測試；`attempts <= 0` 沒有參數檢查。來源：Task 2 審查。
- 逃跑戰報的角色名稱（`AdventureScreen` 傳 `playerName`）沒有接線測試。來源：Task 3 審查。
- `WorldService.MoveAsync` 的交易包覆：通行證寫入失敗時鑰匙是否回滾沒有測試（拿掉交易包覆預測會存活）。來源：Task 4 審查。
- 密藥的經驗倍率以「開打時刻」判斷；改成以打完時刻判斷的變異可能存活（要一場勝場剛好跨過到期時刻才抓得到）。來源：Task 6 審查。
- 技能頁「背景重載不覆蓋未儲存的草稿」沒有測試；`game/format.ts` 沒有 `format.test.ts`。來源：Task 10 審查。
- `CharacterScreen` 卸載時清掉密藥倒數的 interval，沒有測試釘住（實作正確）。來源：Task 10 審查。
- `artAssets.test.ts` 沒有釘住 VP8 寬高的 `& 0x3fff`（scale 位元）與 VP8L 的 alpha／version 位元：測試資料沒有設定這些位元，拿掉遮罩測試仍然通過。來源：Task 11 審查。

## 計畫 10 最終審查延後項目

來源：`.superpowers/sdd/2026-10-22-plan-10-wrap-up/final-review.md` 的 Minor 1～9（兩個 Important 已在收尾修正）。

- **魔石寶箱可以靠「滿包重試」挑結果**：`InventoryService.cs:115-124` 先擲骰、放不下才 400 且什麼都不扣；把魔石分類放滿想要的種類，其他結果一律 400、寶箱不扣，重按到開出想要的為止。修法：放不下時寶箱照扣、結果丟棄並提示，或先檢查最壞情況的格數。測試：`Use_StoneBoxRejectionDoesNotLetARetryPickTheResult`。
- **改設定、喝密藥時先結算的那段戰鬥不會播放**：`CharacterService.cs:97`、`InventoryService.cs:109` 只取 `AdvanceAsync` 回傳的 `.Character`，事件與摘要丟掉，最多約 10 秒的戰報（含掉落、升級）不出現。建議 `ItemUseResult`／設定回應帶 `Summary`，前端寫一行摘要。測試：`SettingsPanel_ShowsTheSettledSummary`。
- **經驗膠囊（與既有的能力藥水）不先結算就套用**：`ConsumableUse.cs:53`、`InventoryService.cs:105-118` 只有密藥先推進；升級後未結算的那段戰鬥以新等級結算，獎勵回溯變少，與「新設定、新密藥不回溯」不一致。測試：`Use_ExperienceCapsuleDoesNotChangeUnsettledFights`。
- **背包「使用」不更新頂欄角色**：`BagScreen.tsx:344` 只回 `inventory`、沒有呼叫 `onCharacterChanged`；膠囊、行動力藥水、能力藥水在下一次輪詢前頂欄都是舊值。修法：`UseItemResponse` 帶 `CharacterResponse`。測試：`BagScreen_UsingACapsuleUpdatesTheTopBarLevel`。
- **密藥的生效時刻比結算時刻晚一次資料庫往返**：`InventoryService.cs:115` 再讀一次 `clock.UtcNow`；結算停在 `[推進的 now, 喝下的 now)` 之間時，下一場早於 `StartsAt` 幾毫秒而不吃加成。修法：`AdvanceResult` 帶結算時刻，`StartsAt = max(結算時刻, now)` 或共用同一個 `now`。測試：`Use_ElixirAppliesToTheFirstFightAfterSettling`（會前進的假時鐘）。
- **離線超過 24 小時時，捨棄的是最舊、已付費的通行時間**：`AdventureService.cs:70-74` 的 `from = now − 24h` 晚於通行證到期時，剩下的通行時間一場都不打。既有上限語意碰上禁區，可接受。
- **`UpdateSettingsAsync` 的寫入在 UoW 之外**：`CharacterService.cs:97-99` 推進自己提交後，`characters.UpdateAsync` 自動提交，`EfUnitOfWork` 的 1213／1205 → 409 對映不涵蓋這一筆（同 `MoveAsync` 舊寫法）。包進 `unitOfWork.RunAsync` 即可一致。
- **🐛 commit 的「兩條測試」規則有缺口**（流程問題，行為都已釘住）：`a5b1973` 只有 repository 層一條；`96b405f` 只有載入器一條；`144ec3a` 只有元件層 `CharacterScreen.test.tsx`、沒有 `game/buffs` 的最小重現；`68b8645` 的外層測試在修正回合 `4e370b5` 才補上。
- **設定面板沒有提示補給 −1／0 的差別**：補給 HP 填 0 是「永遠不喝」，填 −1 是「略過 HP 條件」，畫面只說明 −1（`frontend/src/ui/SettingsPanel.tsx`）。建議說明加一句「HP 填 0 不使用恢復道具」。
