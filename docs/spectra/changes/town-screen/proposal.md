## Why

標題與建角換成新畫面後，玩家進遊戲仍直接落在舊的分頁外殼。設計稿的下一頁是城鎮：一張可以左右換段的全景，把各設施當成城裡的地點，取代舊外殼的「一進遊戲就在冒險分頁」。各設施頁還沒重做，所以這次先做城鎮本身，點設施時接回舊畫面。同時定下「城鎮＝離線掛機」：在城鎮不呼叫冒險 API，玩家要掛機就進冒險或其他舊頁面。

## What Changes

- 新增城鎮畫面（設計稿：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 TownFit v3）：
  - 讀到角色（打開網頁、登入的「歡迎回來」之後、建角完成之後）先進城鎮，不再直接進舊外殼。
  - 全景三段：城門口（立繪、角色／背包／同伴紙籤、出發羅盤與所在地）、市集（旅館、商店街）、公會前（鐵舖、勳章所、任務板）。點兩側紙籤或在舞台上左右滑動換段，一次一段，平移 0.75 秒；每段的紙籤、名牌只在那一段可見、可點。
  - 固定在上方：名字、職業、Lv、HP／SP 條、金錢、加成列（住宿與密藥的圓章與剩餘時間，點開看清單）。
  - 背景依裝置時間換成清晨、白天、黃昏、夜晚四張。
  - 立繪依職業與性別，從內容圖 `portraits/<職業 id>-male|female` 取得，缺圖時用佔位圖。
- 城鎮＝離線掛機：在城鎮不呼叫冒險推進 API，城鎮上的 HP、SP、金錢、等級是進城鎮那一刻的值。進冒險分頁或其他舊頁面才開始輪詢，伺服器照既有規則補算離線期間（最多 24 小時）。
- 點設施接舊畫面：打開舊外殼（上方欄＋下方分頁），停在對應分頁或背包子頁，並加「回城鎮」按鈕；技能、轉職、倉庫、銀行、移動、設定都從舊外殼進入。回到城鎮時舊外殼關閉，輪詢停止。
- 加成列的剩餘時間用本機時鐘每分鐘倒數；任一項倒數結束時，重新讀取密藥與住宿（這兩個不是冒險 API）。
- 密藥記下最後喝的是哪一瓶：資料庫 `character_buffs` 新增 `item_id`，喝密藥時跟著覆蓋。密藥回應（`GET /api/characters/me/buffs` 與 `POST /api/inventory/use` 回應裡的 `buff`）新增 `startsAt`、`itemId`、`itemName`。舊資料或內容已移除的道具，`itemId`／`itemName` 為 null，畫面改顯示種類名稱。
- 城鎮的紙籤只顯示角色 Lv；背包數量、同伴人數、任務板紅點這次不做。
- 神力秘藥、戰鬥熟練手冊的道具圖匯入 R2 `items`（四個初階職業的男女立繪已在 R2）。城鎮四時段全景、紙條、羅盤、錢袋、紙籤圖示、住宿房型徽章與密藥種類圓章轉成設計素材，放進 `frontend/src/assets/town/`。
- 補規格：城鎮依賴的密藥規則（喝下、同種覆蓋、到期不列出、與住宿同種相乘），以及冒險的離線補算（上限 24 小時、只回傳最後 30 秒的戰鬥），都還沒有規格。這次寫成 `elixir-buffs` 與 `adventure-catch-up`，除了上面新增的欄位都描述現況，逐條對照既有測試，缺的補上。
- 不推翻裁定彙整的規則。

## Capabilities

### New Capabilities

- `town-screen`: 城鎮畫面：進入時機、三段全景與換段（點紙籤、滑動）、上方狀態與加成列、四時段背景、立繪、點設施接舊外殼與回城鎮、在城鎮不呼叫冒險 API。
- `elixir-buffs`: 密藥加成：喝下、同種覆蓋、到期、與住宿同種相乘、記下最後喝的道具、回應欄位（新增 `startsAt`、`itemId`、`itemName`）。
- `adventure-catch-up`: 冒險推進的離線補算：上限 24 小時、事件只回傳最後 30 秒、略過的戰鬥併成摘要。

### Modified Capabilities

(none)

## Impact

- db：新增 `backend/db/ddl/V000000020__add_character_buff_item.sql`（`character_buffs` 加 `item_id`），重新 scaffold `backend/EndlessBattle.Infrastructure/Persistence/Models/CharacterBuff.cs`。
- Domain：`backend/EndlessBattle.Domain/Items/Buffs.cs` 的 `CharacterBuff` 與 `ElixirSpec` 帶道具 id；`ConsumableUse` 喝密藥時寫入。
- Application：密藥清單回傳道具名稱（從遊戲內容查）。
- Infrastructure：`BuffRepository` 讀寫 `item_id`；`JsonGameContent` 的密藥規格帶道具 id。
- Api：`backend/EndlessBattle.Api/Endpoints/Contracts.cs` 的 `BuffResponse` 加欄位；`backend/openapi/` 重新產生。
- Tests：`backend/EndlessBattle.Tests` 密藥與離線補算的規格測試、補缺的測試。
- frontend：
  - `frontend/src/api/`：`schema.ts` 重新產生，`lodgingData.ts` 的 `BuffConverter` 加欄位。
  - `frontend/src/game/`：城鎮換段、時段、加成列等純 TS 模型，`buffs.ts` 的 `BuffVo`。
  - `frontend/src/ui/`：新的城鎮畫面、`App.tsx` 進城鎮、`AdventureScreen.tsx` 可指定起始分頁並回城鎮、`BagScreen.tsx` 可指定起始子頁。
  - `frontend/src/assets/`：城鎮設計素材，`content-manifest.json` 加入兩個道具圖。
- 文件：`docs/art/README.md` 設計素材清單。
