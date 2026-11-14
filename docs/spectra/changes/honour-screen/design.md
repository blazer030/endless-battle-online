## Context

- 城鎮公會前的「勳章所」紙籤目前打開舊外殼背包分頁的勳章所子頁（`GameScreen` 的 `facilityViews.honour` → `{ tab: "bag", bagPage: "honour" }` → `BagScreen` 的 `HonourPage`）。
- 舊勳章所頁：持有勳章一行（`大陸勳章 ×14`…）、「傭兵」「道具」兩組卡片（獎品名 ×數量、花費，不夠的標紅、「兌換」鈕）、已僱用時傭兵卡停用（另呼叫 `getMercenaries`）、錯誤文字、載入失敗「重試」。
- API：`GET /api/honour`、`POST /api/honour/exchange { exchangeId }`，都回 `HonourShopResponse { medals[{ itemId, name, owned }], exchanges[{ id, kind, rewardId, rewardName, quantity, cost[{ itemId, name, quantity, owned }], affordable }] }`。沒有道具能力、需求、孔數、效果，也沒有傭兵契約內容與僱用狀態。
- 規則（Domain `HonourShop`）：一次兌換一筆；勳章從背包扣，取用順序同任務交付（`Delivery.Plan`）；原因依序 `honourUnknown`（單獨回傳）、`medalsMissing`、`mercenaryHired`、`bagFull`（付掉勳章後同分類的背包放不下）；傭兵兌換簽新契約、狀態休息中。`Affordable` 要每種勳章都夠。`HonourService` 先寫角色版本鎖再扣勳章、加道具或契約。
- 裝備需求（Domain `EquipmentRules.CanEquip`）比對角色裸點的力、智、敏、魅，不含裝備加成；勳章所的道具都沒有職業限定（實測內容檔 `jobId` 0 筆）。
- 內容（實測 `backend/content/honour-shop.json`）：144 筆；分類 身體 30、手部 30、腳部 28、武器 21、頭部 15、道具 13、鑰匙 4、傭兵 2、材料 1；每種勳章出現在 大陸 47、王國 39、雨林 28、王城 36、白銀 12 筆；用兩種以上勳章付的 15 筆；數量大於 1 的 6 筆（honour-133～137、142）。道具類消耗品種類：補給 4、密藥 4、魔石寶箱 2、捕捉 1、精鍊寶箱 1、寵物蛋 1。
- 勳章所沒有規格；既有測試 `HonourShopTests`、`HonourServiceTests`、`HonourEndpointsTests`、`CompanionTableTests` 的 `HonourShop_*`。
- 內容圖：R2 的道具圖只有少數幾張（142 個兌換道具裡 1 張），勳章 `item-1304`、`item-1305` 已在 R2（同伴頁匯入），`item-1306`、`item-1307`、`item-2222` 還沒有。
- 同伴（`companions-screen`，2026-11-14 封存）已做好 `RevealLayer`（可指定圓心的展開／收回）、`ContentImage` 的 `placeholder` 參數、管理人傳話 `KeeperSay`、`useKeeperPage`、羊皮紙、紅印章 `Seal`、圖片預載；旅館的 `inn-sheet` 羊皮紙、`title/clerk`（莉瑟立繪）、`title/bubble` 對話框都已在前端。

## Design Source

- 來源：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 `HonourFit`（2026-11-14 定案：v17 初版、v18～v20 雨林勳章重產、v19 分類佔位圖／背包滿說分類／傭兵頭盔、v21～v22 吊牌版位、v24 定 C 案＋名字 13px 並拿掉切換鈕）。產生器 `.design-work/work/honourfit/build.py`，點擊測試 `.design-work/work/honourfit/test-flow.js`。
- 快取：`.spectra/design-cache/honour-screen/HonourFit.dc.html`（git 忽略；素材 `/_blob/<id>`，原檔在 `.design-work/assets/<id>.*`；12 張分類佔位圖在 `.design-work/out/item-*.svg`，產生器 `.design-work/work/placeholders/build.py`；傭兵頭盔 `.design-work/out/hn-merc-a.png`；雨林勳章 `.design-work/work/honour/hn-medal-rainforest.png`，199×420）。
- 預覽：`cd .design-work && zsh work/honourfit/shots.sh <狀態>…`（輸出 `shots/hf2-<狀態>.png`，624／683／780 三框並排）。狀態：櫃檯 `desk`／`deskHired`／`deskNone`／`deskThanks`／`deskBack`／`deskWait`／`deskFail`；陳列板 `rack`／`rackKingdom`／`rackRain`／`rackRoyal`／`rackSilver`／`rackHired`／`rackNone`；詳情 `sheetOk`／`sheetReq`／`sheetShort`／`sheetMerc`／`sheetMercHired`／`sheetPotion`／`sheetKey`／`sheetStone`／`sheetBusy`／`sheetLong`；莉瑟說 `okItem`／`okMerc`／`ngBag`／`ngHired`／`ngMedals`／`ngNet`。
- 下表是主要版位；其餘每條規則（顏色、陰影、字級、轉角）以快取檔的 `.hnp`／`.hn-*` 規則為準。舞台寬 390、高 H＝624～780，k＝H／844。

| 區塊 | 元素 | 數值 |
| --- | --- | --- |
| 進出 | 展開 | 圓心＝公會前「勳章所」紙籤中心：x＝max(390 − 139k, 195 + 78k)、y＝256k + 19（624 → (287, 208)、683 → (278, 226)、720 → (271, 237)、780 → (267, 256)，四捨五入）；`circle(0)` → `circle(140%)`，0.55s，同旅館與同伴 |
| 進出 | 返回 | 櫃檯左上「公會前」收回同一點、回公會前段；陳列板左上「櫃檯」：陳列板升回、莉瑟回來（返回鍵 left 14、top 14，同旅館） |
| 櫃檯 | 勳章盒 | 寬 cw＝330 + (H − 624) × 0.282（330～374）、高＝cw × 0.47、置中；離舞台下緣 20 + (H − 624) × 0.051（20～28）；底圖 `fed58e66`（900×423）；五格各放勳章圖＋持有數小籤 13px |
| 櫃檯 | 木條／櫃檯 | 木條 `d137517f` top＝盒上緣 − 44、高 52；櫃檯 `6884370a` top＝盒上緣 − 28 到舞台底 |
| 櫃檯 | 莉瑟 | 立繪 `a32a504a`（＝前端 `title/clerk`）left −6、top 40、高 568，不縮放，矮時被櫃檯切到腰 |
| 櫃檯 | 對話框 | left 176、top 78、208×112，padding 12 14 12 46，13px 行高 1.5，底圖 `b97d9abd`（＝`title/bubble`） |
| 櫃檯 | 僱用中紙條 | `dd89ce59` 底、left 12、top＝盒上緣 − 35、轉 −2°；「僱用中」11px ＋ 傭兵名 Noto Serif TC 13px |
| 陳列板 | 外框 | left／right 8、top 50、高＝盒上緣 − 112（624 → 337、780 → 464）；木框 `3677319e` border-image 30px；絨布 `c85cc481` 另鋪一整塊（避免九宮格接縫）；頂端黃銅銘牌寫勳章名 |
| 陳列板 | 清單 | 板內捲動（`inset −8 −10 −8`、padding 36 4 14）；分區小銘牌 12px（`c368fe3d`）；吊牌 4 欄、gap 12 6 |
| 陳列板 | 吊牌 | 底圖 `13deafb3`，高 138、上內距 48、下內距 8、左右 5；頂上圖釘 `59eb4a17` 16×16；圖 28×28（道具：分類佔位圖；傭兵：頭盔高 28）；名字 Noto Serif TC 900 13px 行高 1.25、最多兩行、上下 auto 邊距置中；×N 角標 Cinzel 11px 在圖右下；花費列 勳章圖高 17＋Cinzel 12px（不夠 `#A0301E`），五種勳章時只畫圖；換不起 `brightness(.8) saturate(.55)` |
| 詳情 | 羊皮紙 | left／right 18、top 64、最高到舞台底 − 28；底圖 `75aeed6c`（＝`inn/parchment`）padding 30 24 22；名稱到孔數在紙內捲動；兌換列（每種勳章 圖＋「持有 N ／需要 M」12px，不夠的持有數紅）、莉瑟的話、印章那排固定在下方 |
| 詳情 | 內容 | 道具圖 40×40（傭兵頭盔高 46）＋×N；名稱；「分類・綁定」12px；效果；能力加成 13px；裝備需求（夠 綠、不夠 紅，只標色）；「孔 n・精鍊上限 +n」；傭兵：可戰鬥次數、金錢／寶物／經驗 %、戰敗消耗 |
| 詳情 | 印章 | 「兌換」紅印章 66×66（`759746ed`＝`title/seal`），停用時旁邊 12px `#A0301E` 寫「勳章不足」或「僱用中：名字」；送出中印章壓下、字一明一暗、關閉鈕停用 |
| 傳話 | 羊皮紙裡 | 頭像 36 圓（莉瑟立繪裁臉 background-size 78.7×205.4、position −24 −9.9，金框）＋框 13px 行高 1.38、padding 7 9 5、1px 圓角 5，間距 14；一般 `#BCA786`／`#FFF8E8`、失敗 `#CF7D6E`／`#F8DDD2`；尖角 8×8 轉 45° |
| 載入失敗 | 印章 | 92×92「再試／一次」（同同伴），放在對話框下方 |

素材（blob id 前 8 碼）：榮譽廳背景 `12b6be86`、陳列板木框 `3677319e`、絨布 `c85cc481`、吊牌 `13deafb3`、吊牌圖釘 `59eb4a17`、勳章盒 `fed58e66`、僱用中紙條 `dd89ce59`、返回／關閉 `f1386931`、木條 `d137517f`、櫃檯 `6884370a`、分區銘牌 `c368fe3d`（＝`companions/name-plate`）、傭兵頭盔 `9ae2440a`（`.design-work/out/hn-merc-a.png`）。勳章：大陸 `1e8f0f15`、王國 `88ffe03e`（R2 已有）、雨林 `9a9269b1`、王城 `644c74e7`、白銀 `f2285d8c`。分類佔位圖：武器 `9467d3dd`、頭部 `8f6baed1`、身體 `39e15647`、手部 `147d0702`、腳部 `f17855a3`、飾品 `c481022f`、道具 `cbe0a505`、魔石 `398cd9b6`、精鍊石 `a422cac0`、材料 `3e97cff9`、勳章 `c7a61228`、鑰匙 `4b4f5767`。已在前端的同一張圖沿用，不重複轉檔。

## 功能對照

| 舊畫面或規則 | 新畫面 | 決定 |
| --- | --- | --- |
| 持有勳章一行 | 勳章盒五格的持有數小籤 | 照稿 |
| 「傭兵」「道具」兩組卡片 | 點勳章 → 陳列板，依分區排 | 照稿；兩種以上勳章付的在每種都列（使用者 2026-11-14） |
| 獎品名 ×數量 | 吊牌名字＋×N 角標 | 照稿 |
| 花費（不夠標紅） | 吊牌花費列、詳情兌換列 | 照稿 |
| 「兌換」鈕 | 詳情紅印章 | 照稿；成功後羊皮紙不關（使用者 2026-11-14） |
| 已僱用時傭兵停用 | 吊牌變暗、印章停用＋「僱用中：名字」、櫃檯紙條 | 照稿；僱用狀態改由勳章所回應提供 |
| 錯誤文字 | 莉瑟說 | 使用者 2026-11-14；背包滿說出分類 |
| 載入失敗「重試」 | 莉瑟一句＋「再試一次」印章 | 使用者 2026-11-14 |
| （舊畫面沒有）道具能力、需求、孔數、效果、傭兵契約 | 羊皮紙詳情 | 使用者 2026-11-14：擴充 `/api/honour`，需求夠不夠由後端判斷 |
| （舊畫面沒有）道具圖 | 分類線條佔位圖、傭兵頭盔 | 使用者 2026-11-14 |
| 背包分頁的勳章所子頁 | 拿掉 | 使用者 2026-11-14 |

## Goals / Non-Goals

**Goals:**

- 照 HonourFit 做櫃檯、陳列板、羊皮紙詳情，涵蓋所有台詞、停用條件、送出中、載入中與載入失敗、進出。
- 勳章所回應補上詳情與僱用狀態；舊外殼不再有勳章所子頁。
- 補 `honour` 規格，既有行為逐條對照測試。

**Non-Goals:**

- 不改兌換規則與內容檔；不做道具內容圖（R2 道具圖仍是空的，用分類佔位圖）。
- 背包、鐵舖、商店的舊畫面不改用分類佔位圖（之後各自換新設計時再用）。
- 詳情不顯示武器屬性（舊背包與舊勳章所都沒有顯示，API 也沒有給）。

## Decisions

### 勳章所畫面是 GameScreen 的第五種狀態

`GameScreen` 的 view 加 `{ name: "honour"; phase }`，展開與收回用同伴做好的 `RevealLayer`，圓心由 `honourRevealOrigin(H)`（`src/game/honour.ts`）依舞台高算出。收回後城鎮以公會前段顯示並帶最新角色（兌換不扣錢，但背包、勳章有變，城鎮只顯示 Lv 不受影響，仍照同伴的做法帶回角色）。`TownFacility` 的 `honour` 從 `facilityViews` 移除。櫃檯與陳列板之間的切換由 `HonourScreen` 自己管。

替代方案：櫃檯與陳列板各自成為 GameScreen 的狀態；返回關係散在外層，不採用。

### 勳章所回應加詳情與僱用狀態，判斷在 Application

`HonourOverview` 加上角色裸點的需求判斷與目前契約：

- 每筆道具兌換附 `HonourItemDetail`：分類、消耗品種類、補給 HP／SP、數值（`Amount`）、密藥（種類、倍率、秒數）、鑰匙（地點名稱、秒數）、能力加成、裝備需求清單（`stat`、`minimum`、`met`，只列大於 0 的）、孔數、精鍊上限、綁定。`met` 用 Domain 的 `EquipmentRules.CanEquip` 回的 `statTooLow:<stat>` 判斷，不在 Api 重算。
- 每筆傭兵兌換附 `HonourMercenaryDetail`：次數、金錢／寶物／經驗 %、戰敗消耗（取自傭兵名冊內容）。
- 回應頂層加 `hiredMercenaryName`（目前契約的名字，沒有契約為 null）。

回應形狀：`HonourExchangeResponse` 加 `Item: HonourItemResponse?`、`Mercenary: HonourMercenaryResponse?`（依 `Kind` 恰好一個非 null）；`HonourShopResponse` 加 `HiredMercenaryName: string?`。`HonourItemResponse(ItemCategory Category, ConsumableKind Consumable, StatBonusResponse Bonus, IReadOnlyList<HonourRequirementResponse> Requirements, int Holes, int RefineMax, bool Bound, int SupplyHealthPoints, int SupplyManaPoints, int Amount, HonourElixirResponse? Elixir, HonourKeyResponse? Key)`；`HonourRequirementResponse(string Stat, int Minimum, bool Met)`（`Stat` 用既有的能力代碼 `strength`／`intelligence`／`agility`／`charisma`）；`HonourElixirResponse(BuffKind Kind, double Multiplier, int Seconds)`；`HonourKeyResponse(string PlaceName, int Seconds)`；`HonourMercenaryResponse(int Fights, int MoneyPercent, int DropPercent, int ExperiencePercent, int DeathCost)`。只加欄位，既有欄位不變。

替代方案：前端另呼叫 `/api/content` 或 `getMercenaries`、用前端既有的 `requirementRows` 自己比；新畫面不用 `/api/content`、需求判斷屬遊戲規則，不採用。

### 陳列板的分頁與排序在 `src/game/honour.ts`

`rackSections(exchanges, medalId)`：留下花費含這種勳章的兌換，依分區（傭兵、武器、頭部、身體、手部、腳部、飾品、鑰匙、道具、魔石、精鍊石、材料、勳章）分組、空的分區不顯示，組內依這種勳章的需要數由少到多、同數依內容順序。分區名沿用 `categoryLabel`（「道具」＝`Consumable`），傭兵分區名「傭兵」。

### 莉瑟傳話是一張純表

`clerkLine(event)` 回傳 `{ text, failed }`；`clerkGreeting(state)` 回傳櫃檯對話框的話。羊皮紙裡的傳話沿用同伴的傳話框樣式，但只有莉瑟一人（`KeeperSay` 換成可指定頭像，或新增 `ClerkSay`，實作時擇一，不影響行為）。傳話在下一個動作（換吊牌、關羊皮紙、再兌換）清掉。

| 情況 | 失敗 | 莉瑟說 |
| --- | --- | --- |
| 櫃檯：有勳章 | 否 | 歡迎來到勳章所！\n今天想兌換些什麼呢？ |
| 櫃檯：五種勳章都是 0 | 否 | 啊，您還沒有勳章呢……\n有了再來找我喔！ |
| 櫃檯：從陳列板回來、沒換過 | 否 | 慢慢挑沒關係，\n陳列板不會跑掉的～ |
| 櫃檯：從陳列板回來、換過 | 否 | 兌換好了！\n祝您冒險一路順風。 |
| 櫃檯：載入中 | 否 | 歡迎來到勳章所！\n我翻一下您的帳冊喔…… |
| 櫃檯：載入失敗 | 是 | 哎呀，帳冊一時\n翻不到了…… |
| 兌換道具成功 | 否 | 換好了！已經幫您放進背包囉。 |
| 兌換傭兵成功 | 否 | （傭兵名）的契約簽好囉，祝您順利！ |
| `bagFull` | 是 | 哎呀，（分類名）的格子已經滿了…… |
| `mercenaryHired` | 是 | 您已經有一位傭兵了，一次只能一位喔。 |
| `medalsMissing` | 是 | 勳章好像還差一點呢…… |
| 其他代碼、連線失敗 | 是 | 咦，剛剛沒送出去……再試一次吧？ |

（分類名）是換的那件道具的 `categoryLabel`；（傭兵名）是兌換後回應的 `hiredMercenaryName`。`\n` 是對話框裡的換行，羊皮紙裡的傳話是一行。

### 兌換成功後羊皮紙不關

兌換成功回傳新的勳章所資料，畫面直接換成新資料：持有數、亮暗、印章停用都依新資料重算，羊皮紙留著同一筆，可以再換。換過任何一筆後回到櫃檯，莉瑟說「兌換好了！」那句。兌換成功後重讀角色（同同伴，城鎮收回時帶回）。

### 能先擋的先擋，規則仍在伺服器

畫面停用只用回應已給的資料：`affordable` 為 false → 吊牌變暗、印章停用、旁邊寫「勳章不足」；傭兵兌換且 `hiredMercenaryName` 非 null → 傭兵吊牌變暗、印章停用、旁邊寫「僱用中：名字」。吊牌照樣能點開看。背包滿只有伺服器知道，被擋到時才說。

### 道具圖：分類線條佔位圖

吊牌與詳情的道具圖用 `ContentImage`（`items/<道具 id>`）並傳 `placeholder`＝該道具分類的線條佔位圖（`src/assets/placeholders/items/<分類>.svg`，12 張）；R2 有圖時自動換上。佔位圖在米色吊牌上偏淡，前端把透明度調高（使用者 2026-11-14 同意；實際數值在手動驗證時對照稿子決定）。傭兵用設計素材 `honour/mercenary-helmet`。勳章圖是道具內容圖 `items/item-1304`、`1305`、`1306`、`1307`、`2222`。

### 雨林、王城、白銀勳章匯入 R2

用 `tools/assets` 的 `import items item-1306 <雨林新圖>`、`item-1307`、`item-2222` 上傳 R2 並更新清單（同同伴匯入大陸、王國）。雨林用重產的 199×420 新圖，比例與其他四枚一致，不用額外縮放。

### 版位寫成純函式

`src/game/honour.ts`：`honourRevealOrigin(H)`、`medalCaseWidth(H)`（330 + (H − 624) × 0.282）、`medalCaseBottom(H)`（20 + (H − 624) × 0.051）、`rackHeight(H)`（盒上緣 − 112），四捨五入到整數。

### 硬規則

- 硬規則 1、4、5：需求判斷在 Domain（`EquipmentRules`）與 Application，Api 只轉換；不涉及 scaffold 檔與資料表。
- 硬規則 6：勳章圖放 R2、用 `import` 更新清單；內容檔不改。
- 前端規則 2、3、5、7、8、9：舞台 390×624～780、陳列板與羊皮紙在區域內捲動；色碼進 `theme.css`（同值沿用旅館、同伴的變數）；台詞、排序、版位在 `src/game/`；時長進 `timings.ts`；內容圖只經 `contentAssets.ts`；`schema.ts` 用 `pnpm generate:api` 產生。
- 不涉及 migration、admin。

## Implementation Contract

**行為**

- 公會前點「勳章所」→ 櫃檯；點一枚勳章 → 陳列板（那種勳章換得到的全部）；點吊牌 → 羊皮紙詳情；印章 → 兌換；陳列板「櫃檯」回櫃檯；櫃檯「公會前」回公會前。停在勳章所時不呼叫冒險推進 API。
- 所有回饋由莉瑟說，不跳提示框；勳章不夠、已有傭兵先停用。
- 舊外殼背包分頁沒有勳章所子頁。

**介面**

- `GET /api/honour`、`POST /api/honour/exchange` 回應加 `exchanges[].item`、`exchanges[].mercenary`、`hiredMercenaryName`（形狀見 Decisions）。不新增端點。
- 前端：`HonourShopVo` 加對應欄位；`getHonourShop`、`exchangeHonour` 不變；另用 `getMyCharacter`（兌換成功後重讀）。

**失敗模式**

- 載入失敗：櫃檯莉瑟說「帳冊翻不到」＋「再試一次」，勳章盒小籤留位置、勳章不能點。
- 兌換失敗：羊皮紙裡莉瑟說對應台詞，失敗偏紅；畫面不變。
- 道具沒有內容圖或載入失敗：分類佔位圖。

**驗收**

- 後端：`honour` 每個 Scenario 都有測試（新欄位：`HonourEndpointsTests`、`HonourServiceTests`）。
- 前端：`honour.test.ts`（純函式表）、`HonourScreen.test.tsx`、`GameScreen.test.tsx`、`BagScreen.test.tsx`。
- 手動：390×624、390×780、1440×720 逐格截圖，對照 HonourFit 的狀態。
- `./scripts/check.sh` exit code 0。

**範圍**

- 在範圍內：上述畫面、進出、回應加欄位、拿掉舊勳章所子頁、`honour` 補規格、設計素材、12 張分類佔位圖、三枚勳章圖、`ebo-canvas` 板子表加 HonourFit。
- 不在範圍：兌換規則與內容、道具內容圖、其他舊畫面改用分類佔位圖、武器屬性顯示。

## Risks / Trade-offs

- [回應變大：144 筆都帶詳情] → 量小（內容檔 144 筆，估計 JSON 幾十 KB，手動驗證時看 network 實際大小）；不另外做單筆詳情 API。
- [道具全是佔位圖，同分類的吊牌看起來一樣] → 名字與花費區分；R2 補圖後自動換掉。
- [資料過期時畫面以為換得起] → 伺服器仍會擋，莉瑟說對應台詞；兌換成功與失敗後都以伺服器回應為準（失敗時重讀勳章所資料）。
