## Why

同伴之後，下一個換成新設計的內頁是勳章所：城鎮公會前的「勳章所」紙籤目前仍打開舊外殼背包分頁裡的勳章所子頁（一張張文字卡片）。新設計把它變成公會櫃檯：接待員莉瑟、五格絲絨勳章盒、榮譽廳牆上的綠絨陳列板與麻繩吊牌，點吊牌看羊皮紙詳情再蓋印兌換；詳情要的道具能力、裝備需求與傭兵契約，目前的勳章所 API 都沒有給。勳章所規則也還沒有規格，這次一併補上。

## What Changes

- 新增勳章所畫面（設計稿：新成品 https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 HonourFit，2026-11-14 定案）：
  - 從城鎮公會前的「勳章所」紙籤圓形展開到櫃檯（0.55 秒，圓心隨舞台高度變）；櫃檯返回鍵「公會前」收回同一點、回到公會前；陳列板返回鍵「櫃檯」收起陳列板、莉瑟回來。
  - 櫃檯：莉瑟＋對話框（歡迎、沒有勳章、沒換就回來、換過再回來）、五格勳章盒（每格勳章圖＋持有數小籤）、已僱用傭兵時木條上的「僱用中」紙條。
  - 點一枚勳章：莉瑟退開、陳列板降下，掛著那種勳章換得到的全部吊牌（用兩種以上勳章付的兌換在每種相關勳章都列），依分區（傭兵、武器、頭部…材料）排；換得起的亮、不夠的暗；數量大於 1 的標「×N」；陳列板內捲動。
  - 點吊牌：羊皮紙詳情（名稱、分類・綁定、消耗品與鑰匙的效果、能力加成、裝備需求夠的綠不夠的紅、孔數・精鍊上限、傭兵契約內容），下方固定的兌換列（每種勳章持有／需要）與紅印章「兌換」。
  - 兌換成功後羊皮紙不關、持有數立刻更新，可以接著再換；回饋與錯誤由莉瑟說（櫃檯在對話框、陳列板時在羊皮紙印章上方，頭像＋一句，失敗偏紅）；背包滿時說出是哪一類的格子滿了。勳章不夠、已有傭兵先在畫面上停用。不跳提示框。
  - 載入中：數字小籤留位置、勳章不能點；載入失敗：莉瑟一句＋「再試一次」紅印章。
  - 道具圖用各道具分類的線條佔位圖（R2 的道具圖幾乎都還沒有）；傭兵用紅羽頭盔圖；勳章用道具內容圖。
- 勳章所 API `GET /api/honour`、`POST /api/honour/exchange` 的回應加上詳情：每筆兌換的道具資料（分類、消耗品種類與效果數值、能力加成、裝備需求與角色是否達到、孔數、精鍊上限、綁定）或傭兵契約（次數、金錢／寶物／經驗分成、戰敗消耗），以及目前僱用中的傭兵名字。只加欄位，既有欄位不變。
- 拿掉舊外殼背包分頁的「勳章所」子頁與舊的 `HonourPage`；勳章所只從城鎮公會前的紙籤進。
- 補規格 `honour`（描述現況：清單、能否兌換、兌換的扣勳章與原因、傭兵兌換），逐條對照既有測試，缺的補上。
- 不推翻裁定彙整的規則；兌換規則不變。

## Capabilities

### New Capabilities

- `honour`: 勳章所規則與 API：兌換清單與持有數、能否兌換、兌換（扣勳章順序、道具進背包、傭兵簽約）、拒絕原因與順序、回應裡的道具與傭兵詳情、裝備需求是否達到、僱用中的傭兵。
- `honour-screen`: 勳章所畫面：從公會前進出、櫃檯與莉瑟、勳章盒、陳列板與吊牌、羊皮紙詳情與兌換、莉瑟傳話、載入中與載入失敗、版位隨舞台高度。

### Modified Capabilities

- `town-screen`: 「勳章所」改成打開勳章所畫面、返回公會前；舊外殼背包分頁不再有勳章所子頁。

## Impact

- Application：`backend/EndlessBattle.Application/Honour/HonourService.cs` 的總覽加上角色裸點、僱用中的契約，供回應判斷裝備需求與僱用狀態（判斷用 Domain 既有的 `EquipmentRules`）。
- Api：`backend/EndlessBattle.Api/Endpoints/HonourEndpoints.cs`、`backend/EndlessBattle.Api/Endpoints/Contracts.cs` 的勳章所回應加欄位；`backend/openapi/` 重新產生。
- Tests：`backend/EndlessBattle.Tests` 的勳章所規格對照與新欄位測試。
- frontend：
  - `frontend/src/api/`：`schema.ts` 重新產生、勳章所轉換器加新欄位。
  - `frontend/src/game/honour.ts`：勳章所畫面的純模型（台詞、分頁與排序、效果文字、版位）。
  - `frontend/src/ui/honour/`：櫃檯、陳列板、羊皮紙詳情、莉瑟傳話。
  - `frontend/src/ui/GameScreen.tsx`：勳章所改成新畫面、回公會前。
  - `frontend/src/ui/BagScreen.tsx`：拿掉勳章所子頁；刪 `frontend/src/ui/HonourPage.tsx`、`HonourPage.css` 與測試。
  - `frontend/src/assets/honour/`、`frontend/src/assets/placeholders/`：榮譽廳、陳列板、吊牌、勳章盒、傭兵頭盔等設計素材與 12 張道具分類佔位圖。
  - `frontend/src/ui/stage/screenImages.ts`：城鎮預載勳章所素材。
  - `frontend/src/ui/theme.css`：新色碼（展開時長沿用 `timings.ts` 既有的 `RevealMilliseconds`）。
- 內容圖：雨林、王城、白銀勳章用 `tools/assets` 匯入 R2（`frontend/src/assets/content-manifest.json`）。
- 文件：`docs/art/README.md` 設計素材清單；`.claude/skills/ebo-canvas` 板子表加 HonourFit。
