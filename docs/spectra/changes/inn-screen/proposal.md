## Why

城鎮做好之後，點「旅館」仍是打開舊外殼的背包子頁。旅館是第一個換成新設計的內頁：它的功能少、後端幾乎不用動，做出來的木台、門牌分頁、羊皮紙詳情、紅印章之後同伴頁、勳章所、鐵舖都會沿用。同時使用者重新定了入住的意義：房卡只是花錢買 8 小時房型加成，回血交給自動住宿；住宿改存多筆紀錄，換房不必先結算離線時間。

## What Changes

- 新增旅館畫面（設計稿：新成品 https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 InnFit v6）：
  - 從城鎮市集的「旅館」名牌圓形展開進入（0.55 秒），左上返回鍵「市集」收回同一點、回到城鎮的市集段。
  - 上半部大廳＋老闆娘瑪莎立繪＋對話框；下半部木台：店名「暖爐亭」與金錢、目前入住橫幅（房型徽章、名稱、效果、剩餘時間、剩餘條）、兩塊門牌分頁「客房」「自動住宿」。
  - 客房：每間一張房卡（房間背景、徽章、名稱、效果、時長、價格、入住中角標），清單在木台剩下的高度內捲動；點房卡從下方滑出羊皮紙詳情（取代目前房型的說明、花費、紅印章「入住」，錢不夠時印章停用、花費變紅）。
  - 自動住宿：HP、SP 門檻各一條，−／＋ 每次 5、按了就存（沒有儲存鈕），0 再減是「略過」、略過再加回 0%，不是 5 倍數的舊值對齊到相鄰的 5 的倍數；住宿費一格（Lv ≤ 100「免費」，否則「N 起」）。
  - 回饋與錯誤都由瑪莎在對話框用角色口吻說：入住成功、入住失敗（錢不夠／其他）、門檻儲存失敗（數字退回）、載入失敗（木台上只放一個「再試一次」紅印章）。不跳說明式提示框。
  - 時長照內容的秒數：整點寫「8 小時」，否則「7 小時 30 分」。
- 入住規則改變（推翻裁定彙整〈（六）計畫 11：旅館〉的「入住時先結算尚未結算的冒險時間，再用結算後的角色重算價格、重新檢查錢夠不夠」，以及計畫 11 設計文件 `2026-10-29-inn-design.md` 的「入住回滿 HP、SP」）：
  - 入住只扣錢並開始 8 小時房型加成，不再回滿 HP、SP，也不再先結算離線時間。
  - 住宿改存多筆紀錄：每次入住新增一筆，目前還沒到期的那筆在入住時刻結束（不退錢）。冒險結算時每場戰鬥依開打時刻找所在那筆的加成。同一時間仍只有一間房生效（裁定「同時只能有一間房，入住會取代目前的房並重新計 8 小時」不變）。
  - 入住時刪掉結束時間早於 24 小時前的紀錄（離線補算上限 24 小時，再舊的用不到）。
- `GET /api/inn` 回應新增 `restFee`：目前等級自動住宿一次的最低費用（Lv ≤ 100 為 0，否則 10 × Lv），規則留在 Domain。
- 房型徽章與房間背景改成內容圖：R2 新增兩個類別 `room-badges`、`room-backgrounds`，鍵是房型 id，缺圖顯示該類佔位圖；匯入現有 12 張。城鎮加成列的住宿徽章改用同一個查找，刪掉 `frontend/src/assets/town/room-*.webp`。
- 拿掉舊外殼背包分頁的「旅館」子頁與舊 `InnPage`；冒險設定裡的自動住宿門檻保留。
- 補規格：旅館沒有規格，這次寫成 `inn`（房價、入住、多筆紀錄依時刻生效、清理、旅館回應、自動住宿的觸發與費用）。自動住宿與房價描述現況，逐條對照既有測試，缺的補上。

## Non-Goals

- 密藥仍是每種一筆、喝同種先結算再覆蓋，不改成多筆紀錄。
- 不做住宿紀錄與戰鬥歷程（裁定：留給之後的戰鬥歷程）。
- 舊外殼其他分頁、冒險設定面板的門檻輸入不改。
- 老闆娘不換立繪、不做表情變化；大廳背景不分時段。
- 不做「退房」：使用者考慮過先退房再開房，改用多筆紀錄後不需要。

## Capabilities

### New Capabilities

- `inn`: 旅館規則：房價、入住（只扣錢與開始加成、結束目前那筆）、住宿多筆紀錄依開打時刻生效、舊紀錄清理、旅館回應（含 `restFee`）、自動住宿的觸發條件與費用。
- `inn-screen`: 旅館畫面：從城鎮市集進出、大廳與瑪莎台詞、目前入住橫幅、客房與詳情、自動住宿門檻與住宿費、錯誤與載入失敗。

### Modified Capabilities

- `town-screen`: 「旅館」改成打開新的旅館畫面、返回市集段，不再打開舊外殼；加成列的住宿徽章改用內容圖查找。
- `content-assets`: 內容圖類別從六類變八類，新增 `room-badges`、`room-backgrounds`（輸出規格、預期鍵、清單檢查、佔位圖）。

## Impact

- db：新增 `backend/db/ddl/V000000021__lodging_stays.sql`（`character_lodgings` 改成每次入住一列：加自增主鍵、角色與結束時間索引），重新 scaffold `backend/EndlessBattle.Infrastructure/Persistence/Models/CharacterLodging.cs`。
- Domain：`backend/EndlessBattle.Domain/Rest/Inn.cs`（入住不回滿、結束目前那筆、依時刻找加成）、`backend/EndlessBattle.Domain/Rest/Hotel.cs`（最低住宿費）、`backend/EndlessBattle.Domain/Items/Buffs.cs` 與 `backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs`（住宿從一筆改成多筆）。
- Application：`backend/EndlessBattle.Application/Rest/InnService.cs`（入住不結算）、`ILodgingRepository.cs`、`backend/EndlessBattle.Application/Adventure/AdventureService.cs` 與 `backend/EndlessBattle.Application/Balance/BalanceReportService.cs`（傳多筆住宿）。
- Infrastructure：住宿 repository 讀寫多筆與清理。
- Api：`backend/EndlessBattle.Api/Endpoints/Contracts.cs` 的 `InnResponse` 加 `RestFee`、`InnEndpoints.cs`；`backend/openapi/` 重新產生。
- Tests：`backend/EndlessBattle.Tests` 的旅館、自動住宿、住宿紀錄與模擬器測試。
- tools：`tools/assets` 新增兩個類別。
- frontend：
  - `frontend/src/api/`：`schema.ts` 重新產生，`lodgingData.ts` 的旅館轉換加 `restFee`。
  - `frontend/src/game/`：`inn.ts` 時長格式、門檻步進、台詞選擇等純模型。
  - `frontend/src/render/contentAssets.ts` 與 `frontend/src/assets/placeholders/`：兩個新類別與佔位圖；`content-manifest.json` 加 12 張。
  - `frontend/src/ui/`：新的旅館畫面、`GameScreen.tsx` 開旅館與回市集、`town/TownScreen.tsx` 起始段落與住宿徽章、`BagScreen.tsx` 拿掉旅館子頁、刪 `InnPage.tsx`。
  - `frontend/src/assets/inn/`：大廳、瑪莎、木紋、櫃檯木條、門牌、羊皮紙、紅印章、圖釘、關閉鈕、對話框等設計素材；刪 `frontend/src/assets/town/room-*.webp`。
  - `frontend/src/motion/timings.ts`：展開與收回時長、門檻儲存延遲。
- 文件：`docs/art/README.md` 設計素材與內容圖類別（裁定彙整只當參考、不加註，被推翻的規則記在本變更的 design）。
