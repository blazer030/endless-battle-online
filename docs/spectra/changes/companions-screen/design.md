## Context

- 城鎮城門口的「同伴」紙籤目前打開舊外殼的同伴分頁（`AdventureScreen` 的 `companions` 分頁 → `CompanionsScreen`，子頁 `PetsPage`、`MercenaryPage`）。
- 舊寵物頁：寵物數 `n/3`、每隻一張卡（名字－種類 年齡、AT／MT／DEF／飽腹／個性／出擊值／親密度、出擊率說明、改名、餵食下拉＋按鈕、攜帶／放回、放生確認）、錯誤文字。API：`getPets`、`carryPet`、`storePet`、`releasePet`、`renamePet`、`feedPet`（回 `{ pets, character }`）。
- 舊傭兵頁：說明文字、契約卡（改名、狀態、冒險地、剩餘次數、冒險地下拉＋開始戰鬥／停止戰鬥、解除僱用確認）、名冊卡（價格或「勳章所兌換」、次數、金錢／寶物／經驗分成、死亡消耗、僱用）。API：`getMercenaries`、`hireMercenary`（回 `{ mercenaries, character }`）、`deployMercenary`、`restMercenary`、`renameMercenary`、`dismissMercenary`、`getWorld`（冒險地，排除 `restricted`）。
- 規則（Domain）：`PetRules.MaxPets` 3、`NameMaxLength` 12、`Carry` 同時一隻；`PetCare.Foods` 8 種（初級 +2／20 金、中級 +4／50 金）、`CanFeed` 依序 `tooSoon`（10 秒）、`full`（飽腹 ≥ 100）、`overOwner`（AT ≤ 物攻、MT ≤ 魔攻、DEF ≤ max(物防, 魔防)，含裝備；出擊值不限）、`notEnoughMoney`；`MercenaryRules.CanHire` 依序 `honourOnly`、`mercenaryHired`、`notEnoughMoney`，`CanDeploy` 擋禁區，`NameMaxLength` 8；次數用完契約消失（`MercenarySimulator`）。
- 寵物、傭兵都沒有規格；既有測試很齊（`PetCareTests`、`PetRulesTests`、`PetServiceTests`、`PetEndpointsTests`、`MercenaryRulesTests`、`MercenaryServiceTests`、`MercenaryEndpointsTests`、`MercenarySimulatorTests`）。
- 內容圖：R2 的 `monsters` 類別目前 0 張；`items` 只有 `item-2255`、`item-2257`。
- 旅館（`inn-screen`，2026-11-13 封存）已做好木台、羊皮紙詳情、紅印章 `Seal`、瑪莎台詞、圓形展開 `InnReveal`、`GameScreen` 的第三種狀態，同伴頁沿用。

## Design Source

- 來源：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 `CompanionsFit` v13（1794588353-9822，2026-11-14；v11 初版與回饋／載入失敗提案，v12 傳話框尖角加框、文字置中，v13 傳話框離頭像 14px）。產生器 `.design-work/work/companionsfit/build.py`，點擊測試 `work/companionsfit/test-flow.js`。
- 快取：`.spectra/design-cache/companions-screen/CompanionsFit.dc.html`（git 忽略；素材 `/_blob/<id>`，原檔 `.design-work/assets/<id>.*`；通用剪影的高解析原檔 `.design-work/out/pet-silhouette-a.png` 752×871）。
- 預覽：`S=.claude/skills/ebo-canvas/scripts; cd .design-work && python3 ../$S/canvas.py play CompanionsFit -c fit/src -o cf -s "this.startView = 'lobby'=>this.startView = 'pets'" && python3 ../$S/canvas.py shot cf --size 2690x970`。startView：入口 `lobby`／`lobbyIdle`／`lobbyEmpty`／`lobbyFail`；獸欄 `pets`／`pets2`／`petsNone`／`cool`／`full`／`poor`／`rename`／`renameEmpty`／`release`／`petsFail`；傭兵所 `merc0`／`hireOk`／`hirePoor`／`hireHonour`／`hireTaken`／`conRest`／`conFight`／`places`／`dismiss`／`renameMerc`／`mercFail`；米菈說 `okCarry`／`ngOwner`／`ngMoney`／`ngName`／`ngNet`；巴頓說 `okHire`／`okGo`／`ngOne`／`ngPlace`／`ngMoneyM`／`ngNetM`。
- 下表是主要版位；其餘每條規則（顏色、陰影、字級）以快取檔的 `.cmp`／`.cm-*` 規則為準。舞台寬 390、高 H＝624～780。

| 區塊 | 元素 | 數值 |
| --- | --- | --- |
| 進出 | 展開 | 圓心舞台座標 (307, 317)（城門口「同伴」紙籤中心，各高度相同），`circle(0)` → `circle(140%)`，0.55s `cubic-bezier(.65,0,.25,1)`；入口返回「城門」收回同一點、回城門口 |
| 進出 | 頁內返回 | 「同伴」回入口：入口 `opacity 0 → 1`、`scale(1.05) → 1`，0.35s |
| 入口 | 兩扇門 | 各高 H/2 + 8（上下疊 8）；上門獸欄背景 `3c3ccab8` center −150px、下門傭兵所 `9de43cfb` center −40px，寬 390；橫樑（旅館櫃檯木條）top H/2 − 18、高 40 |
| 入口 | 管理人 | 米菈 `b70e140f` left −6、top 60、高 570；巴頓 `cd486224` right −14、top 50、高 450、`scaleX(-1)`；不縮放，矮時裁掉下半身 |
| 入口 | 對話框 | 204×104、13px、行高 1.5；米菈 left 180 top 70（padding 12 16 12 50），巴頓 left 14 top 66（padding 12 50 12 16、底圖左右翻）；底圖沿用 `title/bubble` |
| 入口 | 門牌＋小籤 | 寬 152；門牌 52 高沿用 `inn/door-plate`、19px 900、letter-spacing .3em；小籤 11px、`rgba(36,23,12,.82)` 底；獸欄 right 22 bottom 58，傭兵所 left 22 bottom 44 |
| 頁面 | 木台上緣 | pt＝330 − (844 − H) × 0.45：624 → 231、683 → 258、720 → 274、780 → 301（四捨五入）；場景高 pt + 20；背景 y：獸欄 pt − 500、傭兵所 pt − 440 |
| 獸欄 | 柵欄 | 三格（left/right 6、gap 2）貼場景下緣、高 240；柵門 `49cac5a2` 寬 100%；寵物圖高 146、bottom 50、沒選中 `brightness(.72) saturate(.8)`、選中上移 6 並有暖光；名牌 `c368fe3d` 92×26、bottom 62、13px 900；「攜帶」小牌 11px 紅底 |
| 獸欄 | 寵物資料 | 名字 20px 900 ＋ 鉛筆鈕 28×28；個性標籤 11px；種類・年齡 12px；能力四格 Cinzel 16px；三條量條高 6（飽腹金、親密粉 `#A8485A→#F09AA6`、出擊率青 `#3E8A78→#8FE0C8`），右側數值 Cinzel 12px |
| 獸欄 | 果實槽 | 4 欄 2 列，每格高 68；初級圖高 32、中級高 38 加金光；效果 11px `#8FE0C8`、價格 11px（錢不夠 `#FF9C84`）；停用 `opacity .38 grayscale(.6)` 不縮放；冷卻：槽上緣 3px 金線 10 秒縮掉；果實圖 活力 `6660c17b`、香氣 `4c5d9e53`、甜味 `bbe61dc3`、野性 `da18363b` |
| 獸欄 | 動作列 | 攜帶（亮）／放回獸欄（暗）＋ 放生（暗鈕），高 40；放生確認換成「確定放生」「取消」 |
| 傭兵所 | 名冊 | 招牌「傭兵名冊」22px 900（left 96、top max(48, pt − 288)、下緣 pt − 140）；五張便條 top pt − 132、gap 6、高 112，羊皮紙＋圖釘、輪流傾斜 −3／2／−1.5／2.5／−2°；級 Cinzel 32px；價格 11px（萬以上寫「10萬」，錢不夠紅）；勳章兌換者畫兩枚勳章（各高 22）；僱用中蓋紅章 |
| 傭兵所 | 契約 | 羊皮紙 padding 26 18 16；狀態圓印 58（戰鬥中紅、休息中灰）；剩餘次數 Cinzel 20px＋條（總數＝名冊次數）；冒險地；分成四格（金錢、寶物、經驗、戰敗 −n）；派遣（亮）／召回休息（暗）＋解約（暗） |
| 詳情 | 羊皮紙 | 沿用旅館 `inn-sheet`（left/right/bottom 14）；冒險地清單 3 欄、每格高 42（名稱 13px、等級 11px），依大陸分組小標，最高 min(300, H − 380)、在清單內捲動 |
| 標頭 | 傳話 | 木台標頭高 48；名稱換成 36 圓框頭像（米菈、巴頓立繪裁臉，`background-size`／`position` 照快取檔 `.cm-face`）＋傳話框，間距 14；框 13px、行高 1.38、padding 7 9 3、1px 邊框圓角 5；一般 `#BCA786`／`#F3E6CA`、失敗 `#CF7D6E`／`#F6DCD0`；尖角 8×8 轉 45°（left −5）；羊皮紙裡的傳話底 `#FFF8E8`／`#F8DDD2` |
| 載入失敗 | 印章 | 92×92 紅印章（沿用 `title/seal`，字橫排兩行「再試／一次」）；入口在橫樑正中（top H/2 + 2）、頁內在木台上（padding 24 0 20） |
| 浮字 | 餵食 | 「AT +2　♥ +4」13px 700 白字黑影，從該格上方 1.4 秒浮起淡出；該寵物跳兩下（0.5s） |

素材（blob id 前 8 碼，新設計素材進 `frontend/src/assets/companions/`）：獸欄背景 `3c3ccab8`、傭兵所背景 `9de43cfb`（768×1376）、米菈 `b70e140f`、巴頓 `cd486224`、柵門 `49cac5a2`、名牌 `c368fe3d`、果實四張、通用寵物剪影（`.design-work/out/pet-silhouette-a.png`）。沿用：`inn/counter-wood`、`inn/counter-edge`、`inn/door-plate`、`inn/parchment`、`inn/pin`、`inn/close`、`title/seal`、`title/bubble`、`town/purse`。勳章是道具內容圖：`items/item-1304`（大陸勳章 `1e8f0f15`）、`items/item-1305`（王國勳章 `88ffe03e`），用 `import` 上傳 R2。

## 功能對照

| 舊畫面或規則 | 新畫面 | 決定 |
| --- | --- | --- |
| 寵物數 n/3 | 入口小籤、獸欄空柵欄 | 照稿 |
| 寵物卡的能力與狀態 | 木台寵物資料＋三條量條 | 照稿；親密條以 255 為滿（無上限，超過停在滿），出擊率以 35% 為滿 |
| 出擊率說明、餵食價格說明、傭兵所介紹 | 拿掉 | 不寫操作說明 |
| 餵食下拉＋按鈕 | 8 格果實槽 | 照稿；冷卻每隻各算 |
| 改名欄＋按鈕 | 鉛筆鈕 → 輸入框＋字數 | 寵物 12、傭兵 8 |
| 攜帶／放回、放生確認 | 動作列 | 照稿 |
| 名冊卡 | 名冊便條＋詳情羊皮紙 | 照稿 |
| 契約卡、冒險地下拉 | 契約羊皮紙、冒險地清單 | 照稿；分組與等級取 `getWorld` |
| 錯誤文字 | 管理人在標頭／羊皮紙裡說 | 使用者 2026-11-14 定案 |
| 載入失敗重試 | 入口與頁內「再試一次」 | 使用者 2026-11-14 定案 |
| 寵物圖 | 怪物內容圖，沒有時通用剪影 | 使用者 2026-11-14 |
| 舊外殼同伴分頁 | 拿掉 | 使用者 2026-11-14 |

## Goals / Non-Goals

**Goals:**

- 照 CompanionsFit v13 做入口、獸欄、傭兵所，涵蓋所有台詞、停用條件、載入失敗與進出。
- 舊外殼不再有同伴分頁，同伴只從城門口進。
- 補 `pets`、`mercenaries` 規格，既有行為逐條對照測試。

**Non-Goals:**

- 不改後端與 API；不做怪物圖；不做寵物捕捉、戰鬥模擬、競技場、勳章兌換的畫面。

## Decisions

### 同伴畫面是 GameScreen 的第四種狀態

`GameScreen` 的 view 加 `{ name: "companions"; phase }`，展開與收回沿用旅館的做法（`InnReveal` 抽成可指定圓心的 `RevealLayer`，旅館與同伴共用），圓心固定 (307, 317)。收回後城鎮以城門口（段落 0）顯示。`TownFacility` 的 `companions` 從 `facilityViews` 移除。同伴畫面內的三頁（入口、獸欄、傭兵所）由 `CompanionsScreen`（新）自己管，入口與頁之間的淡入是元件內部的狀態。

替代方案：入口與兩頁各自成為 GameScreen 的狀態；層數變多、返回關係散在外層，不採用。

### 入口同時讀寵物與傭兵

入口顯示兩人台詞與狀態小籤，需要寵物清單與契約。進入同伴畫面時同時讀 `getPets` 與 `getMercenaries`；任一失敗就是入口的載入失敗（兩人都說讀不到、橫樑放「再試一次」，門照樣能點）。進入獸欄、傭兵所時各自重讀一次對應的資料（舊頁也是每次打開都載入，寵物的飽腹、傭兵的次數才是最新的）；冒險地清單在打開派遣羊皮紙時讀 `getWorld`，失敗時由巴頓說連線失敗。

### 管理人傳話是一個共用元件與一張純表

`src/game/companions.ts` 的 `keeperLine(event)` 依事件回傳 `{ keeper: "mira" | "barton", text, failed }`；事件包含成功（攜帶、放回、改名、放生、僱用、派遣、召回、解約、傭兵改名）與失敗（伺服器原因代碼對到台詞；不認得的代碼與連線錯誤用連線失敗那句）。畫面上的 `KeeperSay` 元件顯示頭像＋框，`failed` 時偏紅。事件在下一個動作（點寵物、餵食、開始改名、開詳情、換頁）清掉，標頭換回名稱。羊皮紙打開時同一個事件顯示在羊皮紙裡。

| 原因代碼 | 誰 | 台詞 |
| --- | --- | --- |
| `overOwner` | 米菈 | 再吃下去，牠就要比你還強了呢。 |
| `notEnoughMoney`（餵食） | 米菈 | 哎呀，這顆果實的錢好像不太夠。 |
| `nameInvalid`（寵物） | 米菈 | 這名字牠好像記不住呢。 |
| `full` | 米菈 | 牠已經吃得飽飽的了。 |
| `tooSoon` | 米菈 | 慢慢來，讓牠先嚼完這顆。 |
| 其他、連線（寵物） | 米菈 | 咦，剛剛沒弄好……再試一次吧？ |
| `notEnoughMoney`（僱用） | 巴頓 | 錢不夠，這張契約簽不了。 |
| `mercenaryHired` | 巴頓 | 一次只接一份契約，先把（名字）的處理掉。 |
| `honourOnly` | 巴頓 | 那兩位得去勳章所換，我這兒不賣。 |
| `placeRestricted` | 巴頓 | 那裡是禁區，我的人不去送死。 |
| `nameInvalid`（傭兵） | 巴頓 | 這名字名冊上寫不了，換一個。 |
| 其他、連線（傭兵） | 巴頓 | 嘖，文書出了點差錯，再來一次。 |

成功台詞與入口招呼照 Design Source 快取檔的說明區（例：「（名字）會跟著你出門囉！」、「契約簽好了，（級）級傭兵歸你使喚。」、「（名字）往（冒險地）出發了。」；入口：沒寵物「獸欄空蕩蕩的……抓到寵物就帶來給我照顧吧！」、有攜帶「（名字）今天也很有精神喔！要來看看牠們嗎？」、有寵物沒攜帶「孩子們都在欄裡等你呢，今天要帶誰出門？」；巴頓沒契約「想找人手？名冊上的傢伙個個都能打。」、戰鬥中「（名字）還在（冒險地）幹活，契約我幫你顧著。」、休息中「（名字）在後頭磨刀，隨時可以再派出去。」；小籤「寵物 n / 3・（名字） 攜帶中」、「（名字）・戰鬥中／休息中」或「沒有契約」）。錢不夠被伺服器擋到時，重讀角色換成最新的金錢（同旅館）。

### 能先擋的先擋，規則仍在伺服器

畫面停用條件只用 API 已給的資料：果實（`nextFeedAt` 未到、飽腹 ≥ 100、金錢 < 價格）、僱用（價格為 null、已有契約、金錢 < 價格）、改名（去頭尾空白後 0 字或超過上限；字數以字碼計、與伺服器一致，超過上限時新輸入打不進去（只截掉這次新插入、超出的部分，原本的字都留著），注音拼音選字中不截、選完才截；不用輸入框的 `maxLength`，它以 UTF-16 計，emoji 會算兩個字）。「不超過主人」、禁區由伺服器判斷，被擋到時才說。冷卻每隻寵物各自倒數（`nextFeedAt`），到時重繪。

### 餵食浮字由前後兩份寵物算出

`feedPet` 回傳新的寵物清單；`feedEffect(before, after, food)` 算出「（能力名） +n　♥ +m」（能力名 AT／MT／DEF／出擊值，n＝果實加成，m＝親密差），由該格浮起。餵食成功不說話。

### 寵物圖：怪物內容圖，沒有時通用剪影

`ContentImage` 加可選的 `placeholder` 參數；獸欄用 `monsters/<怪物 id>`，清單沒有或載入失敗時顯示通用剪影（`assets/companions/pet-silhouette.webp`），而不是怪物類的灰階佔位圖。

### 量條與版位寫成純函式

`src/game/companions.ts`：`companionsCounterTop(H)`（330 − (844 − H) × 0.45，四捨五入）、`intimacyRatio`（÷255，上限 1）、`activationRatio`（÷35，上限 1）、`satietyRatio`（÷100，上限 1）、`shortPrice`（≥ 10,000 寫「n萬」，否則千分位）、`deployGroups(areas)`（依大陸分組、排除禁區、附 recommended 等級文字「Lv a–b」）。

### 勳章圖是道具內容圖

C、B 便條上的兩枚勳章用 `items/item-1304`、`items/item-1305`（勳章所兌換兩者都要）；名冊 API 只說價格為 null，所以兩枚勳章是固定畫法。圖用 `tools/assets` 的 `import items` 上傳 R2，之後勳章所也用得到。

### 硬規則

- 硬規則 6：勳章圖放 R2、用 `import` 更新清單；內容檔不改。
- 前端規則 2、3、5、7、8：舞台 390×624～780、區域內捲動；色碼進 `theme.css`（同值沿用旅館與城鎮的變數）；台詞、比例、分組在 `src/game/`；時長進 `timings.ts`；內容圖只經 `contentAssets.ts`。
- 不涉及資料表、scaffold、`schema.ts`、admin。

## Implementation Contract

**行為**

- 城門口點「同伴」→ 入口；入口點門 → 獸欄或傭兵所；頁內「同伴」回入口；入口「城門」回城門口。停在同伴畫面時不呼叫冒險推進 API。
- 獸欄：選寵物、看資料、餵食（成功只跳＋浮字）、改名、攜帶／放回、放生；傭兵所：看名冊與詳情、僱用、改名、派遣、召回、解約。所有回饋由米菈／巴頓說，不跳提示框。
- 舊外殼沒有同伴分頁。

**介面**

- 不新增 API。用 `getPets`、`carryPet`、`storePet`、`releasePet`、`renamePet`、`feedPet`、`getMercenaries`、`hireMercenary`、`deployMercenary`、`restMercenary`、`renameMercenary`、`dismissMercenary`、`getWorld`、`getMyCharacter`。
- `ContentImage` 新增選填 `placeholder`；`InnReveal` 改成 `RevealLayer`（`origin`、`closing`）。

**失敗模式**

- 載入失敗：入口兩人說讀不到＋「再試一次」；頁內空場景＋管理人說＋「再試一次」。
- 動作失敗：管理人說對應台詞，失敗偏紅；錢不夠時重讀角色。
- 寵物沒有內容圖或載入失敗：通用剪影。

**驗收**

- 後端：`pets`、`mercenaries` 每個 Scenario 都有測試。
- 前端：`companions.test.ts`（純函式表）、`CompanionsScreen.test.tsx`、`GameScreen.test.tsx`、`AdventureScreen.test.tsx`。
- 手動：390×624、390×780、1440×720 逐格截圖，對照 CompanionsFit 的 startView。
- `./scripts/check.sh` exit code 0。

**範圍**

- 在範圍內：上述畫面、進出、拿掉舊同伴分頁、`pets`／`mercenaries` 補規格、設計素材與兩張勳章圖。
- 不在範圍：後端規則與 API、怪物圖、勳章所畫面、冒險畫面。

## Risks / Trade-offs

- 寵物一律是剪影，三隻看起來一樣；名牌與種類文字區分。怪物圖補上後自動換掉。
- 入口要等兩個 API 都回來才顯示台詞；任一失敗就整個入口失敗，門仍可點進去重讀。
- 冒險地清單每次打開派遣都讀一次 `getWorld`；量小（21 個地點，實測內容檔），不另外快取。
