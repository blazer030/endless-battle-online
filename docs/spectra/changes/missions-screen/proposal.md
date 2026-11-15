## Why

勳章所之後，下一個換成新設計的內頁是任務板：城鎮公會前的「任務板」紙籤目前仍打開舊外殼的「任務」分頁（可接／進行中／已完成三頁文字卡片）。新設計把它變成公會牆上的軟木佈告欄（每日、一般兩頁）加上櫃檯上手邊接下的三張委託單，點一張拉近看整張、蓋紅印章接取或交付。設計要的完成台詞、報酬道具分類、技能目前等級與每日委託開放等級，目前的任務 API 都沒有給；任務板本身的接取、放棄、完成規則也還沒有規格，這次一併補上。

## What Changes

- 新增任務板畫面（設計稿：新成品 https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 MissionsFit，2026-11-15 v28 定案）：
  - 從城鎮公會前的「任務板」紙籤圓形展開（0.55 秒，圓心隨舞台高度變）；左上「公會前」收回同一點、回到公會前。
  - 佈告欄兩頁：上緣兩張標題紙並排（目前頁在前，點後面那張或左右滑換頁），另一頁有可接的單子時紙角標紅圈張數；每次打開時，每日頁有單子就停每日頁、否則停一般頁（使用者 2026-11-15 選換頁方案 A，MissionsFit v29）：
    - 每日頁 3×3 不捲動：照等級列出已開放的每日委託（接在手上的不列），今天完成的留在原位褪色＋「完」章；右上「N 小時後重置」紙條。
    - 一般頁列出全部可接的一般委託，在軟木板裡上下捲動；右上角一疊「完成的委託」（數量），點開是可捲動的清單，點一筆看完成台詞與報酬。
    - 單子寫名稱（拿掉「[每日]」前綴）、委託者、Lv、種類（討伐／交付／寵物）。
  - 櫃檯上手邊最多三張：名稱、第一項目標與進度、其餘「＋N 項」、進度條、全部達成時蓋「達成」或「可交」（只差技能時不蓋）；「進行中 N / 3」紙條。
  - 點一張拉近成委託單：委託者、說明、條件、全部目標（擊殺附出沒地點、交付持有數、交寵物要攜帶中、技能條件；不夠的標紅）、報酬（經驗、金錢、道具用分類佔位圖、技能、寵物），內文在紙內捲動，莉瑟的話與放棄／印章那排固定在下面。
  - 接取、放棄、完成後委託單不關、直接換成新的樣子；放棄要再確認；完成時右上蓋「完」章，紙上換成委託者的話（有台詞時）與這次拿到的東西（含升級）。手邊已滿 3 張時接取印章停用。
  - 回饋與錯誤都由莉瑟在委託單裡說（頭像＋一句，失敗偏紅），不跳提示框；失敗後重讀任務板。
  - 讀取中：軟木上 6 張空白的紙、手邊空位；讀取失敗：軟木正中間莉瑟一句＋「再試一次」紅印章；空板只剩圖釘，每日委託還沒開放時每日頁釘一張「Lv N」小紙。
- 任務 API `GET /api/missions`、`POST /api/missions/accept`、`/abandon`、`/complete` 的任務板回應加欄位（只加欄位，既有欄位不變）：
  - 已完成的委託帶 `completionMessage`（內容沒寫或還沒完成時為 null）。
  - 報酬道具帶道具分類（`category`），任務板與完成回應都是。
  - 技能條件帶目前已學等級（`learnedLevel`，沒學過為 null）。
  - 接取條件帶職業、性別、出生地的名稱（`jobName`、`sexName`、`birthName`，沒限制為 null），新畫面不用另外讀名稱表。
  - 任務板頂層帶 `firstDailyLevel`（內容裡每日委託的最低等級，沒有每日委託時為 null）。
- 拿掉舊外殼的「任務」分頁與舊的 `MissionsScreen`；任務板只從城鎮公會前的紙籤進。冒險戰報與舞台上的任務進度不變。
- 補規格 `missions`（描述現況：任務板清單與排序、接取條件與原因、放棄、完成的檢查順序與報酬、每日委託的擲骰與重置），逐條對照既有測試，缺的補上。
- 不推翻裁定彙整的規則；任務規則與內容檔不變。

## Capabilities

### New Capabilities

- `missions`: 任務規則與 API：任務板的可接／進行中／已完成清單與排序、接取的條件與拒絕原因、放棄、完成的檢查順序與拒絕原因、報酬（經驗、金錢、道具、技能、寵物、升級）、每日委託的目標與報酬擲骰與台北 09:00 重置、回應裡的完成台詞、道具分類、技能目前等級、條件名稱與每日委託開放等級。
- `missions-screen`: 任務板畫面：從公會前進出、佈告欄兩頁與換頁、每日頁與一般頁的單子、完成的一疊與清單、手邊三張、委託單詳情、接取／放棄／完成、莉瑟傳話、讀取中與讀取失敗、空板、版位隨舞台高度。

### Modified Capabilities

- `town-screen`: 「任務板」改成打開任務板畫面、返回公會前；舊外殼不再有任務分頁。

## Impact

- Domain：`backend/EndlessBattle.Domain/Missions/MissionBoard.cs` 的任務板加每日委託開放等級。
- Api：`backend/EndlessBattle.Api/Endpoints/MissionEndpoints.cs`、`backend/EndlessBattle.Api/Endpoints/Contracts.cs` 的任務回應加欄位；`backend/openapi/` 重新產生。
- Tests：`backend/EndlessBattle.Tests` 的任務規格對照與新欄位測試。
- frontend：
  - `frontend/src/api/`：`schema.ts` 重新產生、任務轉換器加新欄位。
  - `frontend/src/game/missions.ts`：任務板畫面的純模型（兩頁的單子、手邊單子、目標與報酬列、莉瑟台詞、版位）；拿掉舊畫面專用的函式。
  - `frontend/src/game/refreshSignals.ts`：拿掉只給舊任務分頁用的任務重載訊號。
  - `frontend/src/ui/missions/`：佈告欄、手邊、委託單、完成清單、莉瑟傳話。
  - `frontend/src/ui/GameScreen.tsx`：任務板改成新畫面、回公會前。
  - `frontend/src/ui/AdventureScreen.tsx`：拿掉任務分頁；刪 `frontend/src/ui/MissionsScreen.tsx`、`MissionsScreen.css` 與測試。
  - `frontend/src/assets/missions/`：佈告欄軟木與框、委託單紙、圖釘、標題紙、手邊單子等設計素材。
  - `frontend/src/ui/stage/screenImages.ts`：城鎮預載任務板素材。
  - `frontend/src/ui/theme.css`：新色碼。
- 文件：`docs/art/README.md` 設計素材清單；`.claude/skills/ebo-canvas` 板子表加 MissionsFit；`docs/superpowers/specs/2026-10-23-deferred-backlog.md` 拿掉只跟舊 `MissionsScreen` 有關的條目。
