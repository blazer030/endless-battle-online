## Context

- 標題與建角已是新畫面（`title-and-create-screens`，2026-11-10 封存）。讀到角色後，`App.tsx` 的 `game` 階段直接顯示舊外殼 `AdventureScreen`：上方欄（名字、職業、Lv、金錢、EXP）、六個分頁（冒險、角色、技能、背包、任務、同伴）。背包分頁 `BagScreen` 另有八個子頁：背包、裝備、商店、倉庫、銀行、鐵舖、勳章所、旅館。
- 冒險輪詢 `useAdventureLoop` 掛在 `AdventureScreen` 裡，外殼一出現就開始呼叫 `POST` 推進 API，切到任何分頁都不停。伺服器端 `AdventureService` 每次推進會補算距上次結算的時間：最多 `AdventureSimulator.MaxSimulatedSeconds`＝86400 秒，事件只回傳最後 `CatchUpWindowSeconds`＝30 秒內開始的場次，其餘併成摘要。所以前端停止輪詢不會損失戰鬥，只是沒有即時播放。
- 密藥 `character_buffs` 的主鍵是（角色、種類）：每種一筆，喝同種會整筆覆蓋（倍率、開始、結束），不記喝了哪一瓶。`GET /api/characters/me/buffs` 只回 `kind`、`multiplier`、`endsAt`，不含 `starts_at`，雖然資料庫有存。住宿 `character_lodgings` 每個角色一筆、有 `room_id`；`GET /api/inn` 回傳房型清單與目前住宿。
- 內容的密藥目前 6 瓶，種類各不相同，每瓶 7200 秒（`backend/content/items.json` 的 item-2255～2260）；住宿 28800 秒，房型 6 種。
- 職業立繪：R2 清單 `frontend/src/assets/content-manifest.json` 已有四個初階職業的男女立繪（`portraits/job-6～9-male|female`，標題變更時匯入）；道具圖（`items`）還一張都沒有。
- 功能對照（2026-11-12）已和使用者確認：點設施接舊外殼、可滑動、紙籤只留 Lv、城鎮不連線、密藥記下最後喝的那瓶並顯示道具名、剩餘時間在本機倒數，倒數完再讀一次 API。

## Design Source

- 來源：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 `TownFit`，v4（1794470634-8f79，2026-11-12；v3 加滑動、拿掉紙籤數字，v4 補密藥六種底色與舊資料那一列）。由 EBO-design 依 HomeJ（EBO DEMO）城鎮改成 390×624～780 版，產生器是 `.design-work/work/townfit/build.py`，點擊測試是 `work/townfit/test-town.js`、`test-swipe.js`，以及加成列的測試。
- 快取：`.spectra/design-cache/town-screen/TownFit.dc.html`（git 忽略；素材網址是 `/_blob/<blob id>`，原檔在 `.design-work/assets/<blob id>.*`）。
- 預覽：`python3 .claude/skills/ebo-canvas/scripts/canvas.py play TownFit -c fit/src -o tf -s "this.startView = 'gate'=>this.startView = 'guild'"`，截圖用 `shot tf --size 2690x970`。另有 startView `'six'`：六種密藥加一列舊資料（物防那一列）。

舞台寬 390，高 H＝624～780。全景倍率 k＝H/844。以下單位都是舞台 px。

| 區塊 | 元素 | 數值 |
| --- | --- | --- |
| 全景 | 背景（四時段） | 原圖排成 1512×844；寬 1512k、高 H，上下不裁切。清晨 `b1b11a9c`、白天 `cbcc9c3b`、黃昏 `75b36d0b`、夜晚 `44221259`。時段以本機時間判斷：5–8 清晨、8–17 白天、17–19 黃昏、其餘夜晚，每分鐘重算 |
| 全景 | 淡化層 | 由上到下 `rgba(255,252,244,.6)` 0% → 透明 20% → 透明 70% → `rgba(250,246,236,.7)` 100%。夜晚改成 `rgba(28,36,64,.45)` 0% → 透明 22% → 透明 68% → `rgba(28,36,64,.5)` 100% |
| 全景 | 平移 | 城門口 0；市集 p1＝875k−195；公會前 p2＝min(1295k−195, 1512k−390)。`transform: translateX(-p)`，0.75s `cubic-bezier(.65,0,.25,1)`。624 高時 p1 452、p2 728；780 高時 p1 614、p2 1002 |
| 全景 | 只顯示自己那段 | 不在該段的紙籤與名牌：0.25s 淡出到 opacity 0、`pointer-events: none` |
| 換段 | 滑動 | 舞台 `touch-action: pan-y`。按下到放開的水平位移（舞台 px）≥40，且絕對值大於垂直位移，才算滑動。往左滑到下一段、往右滑回上一段，兩端不動作。滑動後 300ms 內的紙籤點擊忽略。換段時收起加成清單 |
| 城門口 | 立繪 | 框 left −40k、bottom 40k、高 560k、寬 368k，圖在框內水平置中、高 100%；陰影 `drop-shadow(0 10px 16px rgba(60,40,20,.25))`；上下浮動 ±8px、5s；z 25、不接收點擊 |
| 城門口 | 紙籤（紙 `35548540`） | 貼上緣。角色 left 222、top 168、rotate −2°；背包 left 230、top 230、rotate 1.5°；同伴 left 218、top 292、rotate −1°。寬 178、高 50、padding 0 24 0 18。圖示 46×46、margin-left −26（角色 `1b66a6085`、背包 `4ca152c1`、同伴 `e0b38181`）。名稱 19px、letter-spacing .2em。角色右側 `Lv N` 11px `#8A7356`。紙陰影 `drop-shadow(0 3px 4px rgba(70,50,20,.28))`；hover 時 translateX(−6px) |
| 城門口 | 往市集 | left 286、top 372、寬 118、高 44、padding 0 22 0 18、`#2F5F5A`；字 17px、letter-spacing .14em；右箭頭 18×18，每 1.6s 往右輕推 5px |
| 城門口 | 出發羅盤 `d55573c6` | 貼下緣 left 226、bottom 40、148×148；外圈每 60s 轉一圈；「出發」24px `#4A3416`、letter-spacing .2em；下方所在地 11px `#6A5434`，內容是地區名（`placeName`）；z 26 |
| 市集 | 名牌（紙條＋圖釘） | 高 38、padding 0 16 0 14、字 16px、letter-spacing .16em；副標 11px `#8A7356`。圖釘：名牌上緣中點，9×9 紅色漸層 `#F08A7A→#B0322A`。名牌以圖釘釘在全景：left＝圖釘x×k−半寬，top＝原top×k。旅館（副標「休息」）圖釘 794／300、rotate −2°；商店街（副標「買賣」）961.5／420 |
| 市集 | 換段紙籤 | 「‹ 城門口」left p1−16、top 446k、寬 140；「公會前 ›」left p1+286、top 560k、寬 118；字 16px、letter-spacing .12em、`#2F5F5A` |
| 公會前 | 名牌 | 鐵舖（副標「精鍊・鑲嵌」）圖釘 1209／452；勳章所 1373／256、rotate 2°；任務板 1399／540（寬 86） |
| 公會前 | 換段紙籤 | 「‹ 市集」left p2−12、top 700k、寬 106 |
| 上方 | 狀態牌 | left 0、top 14、寬 214、padding 8 30 9 16；右下斜切 16；底色 `linear-gradient(90deg, rgba(255,252,244,.94), rgba(255,252,244,.78))`、陰影 `0 2px 8px rgba(70,50,20,.18)`。第一行：名字 18px 700、職業 12px `#7A6648`、`Lv N` 13px 700 `#2F5F5A`。HP、SP 兩行：標籤 11px（HP `#A2463C`、SP `#2F6A8A`），條高 5、圓角 3、底 `rgba(80,60,30,.15)`；HP 填色 `#F2A08E→#C9473A`，SP 填色 `#9ED0E8→#3E86B0` |
| 上方 | 金錢 | right 0、top 18、高 30、padding 0 14 0 20；左上斜切 12；底色同狀態牌；錢袋 `c85813b2` 17×17；數字 15px 700 `#4A3412`、千分位 |
| 上方 | 加成列 | left 14、top 104、間距 8；沒有任何加成時整列隱藏。圓章 29×29、圓形；住宿是圓角 7 的方形、排第一個。底色 `radial-gradient(circle at 50% 55%, var(--c), transparent 72%), radial-gradient(circle at 50% 40%, #4A3420, #20150A 75%)`；框 `inset 0 0 0 1.5px #F0CF8A, 0 0 0 1px #5A3A14`。圖佔 74%（住宿 88%）。角落剩餘時間 11px 700、底 `#2A1C0E`、字 `#F6E8C8`，在圓章下方 −9px。不到 10 分鐘時改成底 `#8E1E1E`、字 `#FFE2D6`，並以 1.6s 週期閃 4px 紅光 `rgba(220,70,50,.45)` |
| 上方 | 加成清單（點圓章展開） | 寬 236、從列下 44 開始；底色 `linear-gradient(100deg, #3A2716, #24170C)`、金框 `#C9A464`。標題「加成效果」13px `#E8C888`，後接「住宿 1・密藥 N」11px `#B8A27A`。每列：圓章 30×30；名稱 13px 700；效果 11px `#8FE0C8`；剩餘條高 3、填色 `#C9A464→#F0CF8A`；右側剩餘 12px 700 `#F6E8C8`。不到 10 分鐘時剩餘字 `#FF9C84`、條改 `#A8322A→#E8705A`。點清單外任一處收起 |
| 上方 | 剩餘時間格式 | 滿 1 小時寫 `H:MM`（例 `1:42`），不到 1 小時寫 `N 分`（無條件進位） |
| 住宿 | 房型徽章 | 依房型的加成種類對應（HomeJ 旅館板）：經驗 apprentice-loft `87c454fc`、熟練度 training-chamber `acfda66d`、物攻 fighters-bunk `b0ea9422`、魔攻 stargazer-tower `19e11db0`、物防 bulwark-room `56c3ace8`、魔防 quiet-chapel `1d5b6da7`；住宿圓章底色 `rgba(232,176,96,.45)`；效果寫「經驗 ×1.10」 |
| 密藥 | 圓章底色（`--c`） | 經驗 `rgba(216,176,72,.55)`、熟練度 `rgba(104,176,96,.55)`、物攻 `rgba(232,120,74,.55)`、魔攻 `rgba(156,108,224,.55)`、物防 `rgba(128,150,172,.55)`、魔防 `rgba(72,168,208,.55)`；住宿 `rgba(232,176,96,.45)`、方角框。圓章最多七個（住宿＋六種），排成一排不換行，最右到 x 265 |
| 密藥 | 清單每一列 | 第一行是最後喝的道具名稱，照內容檔原名（例「神力秘藥LV1」）；第二行是「種類 ×倍率」（例「物攻 ×1.05」）。圖示是該道具的內容圖，缺圖時用道具佔位圖 `placeholders/items.svg`。舊資料（沒有道具）：第一行改成「種類 ×倍率」，沒有第二行，剩餘條直接接在名稱下面；圖用道具佔位圖，底色照種類 |

字型：名字、紙籤、名牌、金錢等介面字用 Noto Sans TC；剩餘時間、清單標題、羅盤「出發」用 Noto Serif TC 900（照設計稿）。所有色碼改成 `theme.css` 的變數，與既有變數同值的直接沿用（例如 `--brass` 是 `#C9A464`）。

## Goals / Non-Goals

**Goals:**

- 照 TownFit 做城鎮，涵蓋三段換段、滑動、上方狀態、加成列與清單、四時段、立繪。
- 讀到角色後先進城鎮；從城鎮可以到達舊外殼的所有分頁和子頁，再回到城鎮。
- 在城鎮不呼叫冒險推進 API。
- 密藥記下最後喝的那瓶，回應帶開始時間與道具名稱；加成列倒數，到期後重新讀取。
- 補 `elixir-buffs`、`adventure-catch-up` 規格，逐條確認有測試，缺的補上。

**Non-Goals:**

- 舊外殼與各設施頁的畫面不改，只加「回城鎮」按鈕，以及可指定起始分頁、起始子頁。
- 紙籤上的背包數量、同伴人數、任務板紅點這次不做（使用者 2026-11-12 決定之後再補，設計稿 v3 已拿掉）。
- 冒險畫面的新設計、角色頁的翻頁過場（HomeJ 的立繪縮成頭像）不在這次。
- 倉庫、銀行在城鎮沒有入口（設計稿 v3 寫明從背包舊畫面進），這次不另外處理。
- 密藥道具圖示只匯入設計稿上已有的兩張（神力秘藥、戰鬥熟練手冊）；其他四瓶與四個初階以外職業的立繪顯示佔位圖。

## Decisions

### 讀到角色先進城鎮，舊外殼改成從城鎮打開

`App.tsx` 的 `game` 階段改成新的 `GameScreen`，它有兩種狀態：城鎮，以及舊外殼（帶起始分頁、背包子頁）。打開網頁讀到角色、歡迎回來過場結束、建角成功，都進城鎮。

GameScreen 保管目前的角色。打開舊外殼時把角色傳進去。按「回城鎮」時，舊外殼先丟棄還沒播的戰鬥（`BattlePlayback.discardQueued`，顯示值就等於伺服器最新值），再把角色交回 GameScreen，然後卸載舊外殼。

替代方案：把輪詢和 `BattlePlayback` 搬到 GameScreen，舊外殼只負責顯示。好處是戰報在進出城鎮時保留，但要大改 `AdventureScreen`。舊外殼之後會被新畫面逐頁取代，不值得現在大改，所以不採用。代價是每次回城鎮戰報會清空，下次進舊外殼時，伺服器補算的那段以摘要顯示。

### 城鎮不連線，靠卸載舊外殼停止輪詢

輪詢只存在於舊外殼裡，城鎮完全不碰推進 API；舊外殼卸載時，`useAdventureLoop` 的清理函式會停掉計時器，並忽略還在路上的回應。不另外做「暫停輪詢」的旗標。

城鎮上的角色是進城鎮那一刻 GameScreen 手上的值：打開網頁時是 `GET /api/characters/me` 的結果，回城鎮時是舊外殼交回的值。

### 點設施對應的舊分頁

| 城鎮 | 舊外殼分頁 | 背包子頁 |
| --- | --- | --- |
| 角色 | `character` | — |
| 背包 | `bag` | `bag` |
| 同伴 | `companions` | — |
| 出發 | `adventure` | — |
| 旅館 | `bag` | `inn` |
| 商店街 | `bag` | `shop` |
| 鐵舖 | `bag` | `forge` |
| 勳章所 | `bag` | `honour` |
| 任務板 | `missions` | — |

`AdventureScreen` 新增 `initialTab`、`initialBagPage`、`onReturnToTown(character)`、`onConfirmedCharacter(character)` 四個 props。`onConfirmedCharacter` 在每次收到伺服器的角色（推進回應、轉職、移動、背包操作）時呼叫，卸載後才回來的回應也會呼叫；`GameScreen` 只收最後打開的那次舊外殼交出的角色，回城鎮後城鎮仍會更新成最新的角色。`BagScreen` 新增 `initialPage`。「回城鎮」放在舊上方欄最左邊，每個分頁都看得到。舊畫面沿用 375×667 版面，不放進 `StageFrame`。

### 密藥記下最後喝的道具

新增 migration `backend/db/ddl/V000000020__add_character_buff_item.sql`：`ALTER TABLE character_buffs ADD COLUMN item_id VARCHAR(64) NULL AFTER kind`。沿用「每種一筆、喝同種整筆覆蓋」，覆蓋時一起寫入 `item_id`。不改成流水帳，因為戰鬥只看每種目前的倍率。

- Domain：`CharacterBuff` 加 `string? ItemId`，`Buffs.Start(string itemId, ElixirSpec elixir, DateTimeOffset now)`。住宿轉成的 `CharacterBuff`（`Inn.ToBuff`）的 `ItemId` 是 null。
- 道具名稱在 Application 查：`CharacterService.ListBuffsAsync` 改成回傳帶 `ItemName` 的清單，名稱用 `IGameContent.FindItem` 查。查不到（舊資料 null，或內容已移除）時 `ItemId`、`ItemName` 都是 null。使用道具的回應同樣帶名稱。
- 回應 `BuffResponse(BuffKind Kind, double Multiplier, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? ItemId, string? ItemName)`。
- 前端 `BuffVo` 加 `startsAt: Date`、`itemId: string | null`、`itemName: string | null`。

替代方案：從種類反查道具。目前一種一瓶，但以後加 LV2 就猜不準，使用者選擇記錄，所以不採用。

### 加成列在本機倒數，到期才重新讀取

城鎮進入時讀 `GET /api/characters/me/buffs` 與 `GET /api/inn`，用回應裡的開始、結束時間，每分鐘（沿用 `BuffTickMilliseconds`）重算剩餘時間。只要有任一項的結束時間已過，就重新讀一次這兩個 API，結果中已到期的不列出。

剩餘條以（結束−現在）／（結束−開始）計算。住宿的開始時間由 `GET /api/inn` 的 `lodging` 提供：`LodgingResponse` 加 `StartsAt`，資料庫已有 `starts_at`。這兩個 API 都不是冒險推進，所以不違反「城鎮不連線」。

讀取失敗時加成列隱藏，不顯示錯誤（城鎮不顯示連線錯誤）。下一次倒數到期，或下次進城鎮時，會再讀一次。

### 換段的狀態放在 `src/game/`

段落（0、1、2）、滑動判斷（位移 → 下一段或不動）、時段、平移量、剩餘時間文字與是否警示，都寫成 `src/game/town.ts` 的純函式，給畫面和測試共用。

舞台被 `StageFrame` 縮放過，所以滑動判斷要把視窗 px 換成舞台 px，也就是位移除以縮放倍率。`StageFrame` 把倍率傳給子元件（例如 context 或 CSS 變數）。

### 立繪與設計素材

- 立繪沿用 R2 已有的 `portraits`，城鎮用 `contentArtCandidates("portraits", ...)`，缺圖時（初階以外的職業）用佔位圖。性別 1 是 male，2 是 female。
- 神力秘藥（item-2257）、戰鬥熟練手冊（item-2255）的圖用 `import items` 上傳 R2。
- 城鎮四時段全景、紙條、羅盤、錢袋、三個紙籤圖示、六個房型徽章用 `pnpm cli design town <名稱>` 轉成 `frontend/src/assets/town/*.webp`。
- 硬規則 6、8：內容圖（立繪、道具）只透過 `contentAssets.ts` 取得；設計素材放 `src/assets/town/`。

### 硬規則

- 硬規則 3（只用 migration）：新增 V000000020。步驟是 migration → `docker compose up -d` → `./scripts/scaffold-db.sh`，不手改 `Persistence/Models/CharacterBuff.cs`（硬規則 4）。
- 硬規則 6：內容 JSON 不改；`item_id` 是玩家狀態。
- `schema.ts` 用 `pnpm generate:api` 重新產生，不手改。
- 前端硬規則 3：新色碼加進 `theme.css`。不涉及 admin 路由。

## Implementation Contract

**Behavior：**

- 讀到角色後先看到城鎮的城門口。點「市集 ›」或往左滑，全景在 0.75 秒內平移到市集；在公會前往左滑、在城門口往右滑都不動。
- 城鎮上點任何設施，打開舊外殼並停在上表的分頁和子頁；舊外殼開始輪詢冒險。按「回城鎮」回到城鎮的城門口，輪詢停止，城鎮顯示舊外殼最後的角色值。
- 停在城鎮時，網路紀錄中沒有 `POST /api/adventure/advance`。
- 加成列：住宿排第一，其後是密藥，依種類順序。每一項顯示剩餘時間，不到 10 分鐘時變紅並閃爍。點開清單可看名稱、效果、剩餘條。密藥那一列第一行是最後喝的道具名、第二行是「種類 ×倍率」；沒有道具名時第一行改成「種類 ×倍率」，沒有第二行。
- 喝下密藥後，`GET /api/characters/me/buffs` 回傳該種的 `itemId`、`itemName`、`startsAt`、`endsAt`。再喝同種的另一瓶，`itemId` 換成新的那瓶。

**Interface / data shape：**

- `GET /api/characters/me/buffs` → `[{ kind, multiplier, startsAt, endsAt, itemId: string|null, itemName: string|null }]`；`POST /api/inventory/use` 的 `buff` 欄位形狀相同。
- `GET /api/inn` 的 `lodging` → `{ roomId, startsAt, endsAt }`。
- `AdventureScreen` props：`initialTab?: Tab`、`initialBagPage?: SubPage`、`onReturnToTown?: (character: CharacterVo) => void`、`onConfirmedCharacter?: (character: CharacterVo) => void`。兩個都是選填：既有的 `AdventureScreen` 測試不從城鎮打開，沒傳 `onReturnToTown` 時不顯示「回城鎮」。
- `src/game/town.ts`：`townSegments`、`swipeTarget(segment, deltaX, deltaY) → segment`、`panoramaOffsets(stageHeight)`、`remainingText(endsAt, now)`、`isEndingSoon(endsAt, now)`。時段沿用既有的 `src/game/timeOfDay.ts`（`timeOfDay(hour)`，與標題頁同一組界線），不在 `town.ts` 另寫。

**Failure modes：**

- 密藥或住宿讀取失敗：加成列隱藏，城鎮其餘照常；不重試，直到下一次倒數到期或下次進城鎮。
- 立繪缺圖或載入失敗：顯示 `portraits` 佔位圖。
- `character_buffs.kind` 是無法辨識的值時照舊丟例外（`elixir-buffs` 規格）。

**Acceptance criteria：**

- `town-screen`、`elixir-buffs`、`adventure-catch-up` 每個 Scenario 都有對應測試，或寫明不測的原因。樣式類（位置、顏色）只做畫面確認。
- `./scripts/check.sh` 通過。
- 在 390×624、390×780、1440×720 用瀏覽器確認：三段換段、滑動、點設施進舊外殼、回城鎮，以及加成清單（有住宿和兩種密藥）；過程中網路紀錄確認停在城鎮時沒有推進請求。

**Scope：**

- 範圍內：城鎮畫面、`App` 流程、舊外殼的起始分頁與回城鎮、密藥 `item_id` 與回應欄位、住宿回應加 `startsAt`、兩個道具圖與城鎮設計素材匯入、兩份補規格。
- 範圍外：舊畫面的版面與樣式、角色頁的翻頁過場、冒險畫面的新設計、紙籤數字與紅點、倉庫與銀行入口。

## Risks / Trade-offs

- [每次回城鎮，戰報會清空] → 下次進舊外殼時，伺服器補算的部分以摘要顯示，不會漏算。舊外殼之後逐頁取代，接受這個代價。
- [停留城鎮超過 24 小時，超出的時間不補算] → 這是既有規則，寫進 `adventure-catch-up`；要改上限另開變更。
- [本機時鐘不準時，倒數會有偏差] → 倒數只用來顯示。到期時重新讀取，以伺服器的結束時間為準。
- [舊外殼的上方欄寬度有限，加「回城鎮」可能擠壓 EXP 條] → 實作時在 360 寬確認不換行、不水平捲動。

## Migration Plan

1. 新增 `V000000020__add_character_buff_item.sql`，執行 `docker compose up -d`（Flyway 套用），再跑 `./scripts/scaffold-db.sh`。
2. 部署：新欄位允許 null，舊資料不用回填；舊版程式讀新表時不認得 `item_id`，不受影響。
3. 回滾：程式回到舊版即可，欄位保留不用刪。

## Open Questions

無。
