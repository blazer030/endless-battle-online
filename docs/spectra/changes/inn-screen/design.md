## Context

- 城鎮（`town-screen`，2026-11-12 封存）點「旅館」時，`GameScreen` 打開舊外殼停在背包分頁的旅館子頁（`BagScreen` 的 `inn` → `InnPage`）。`InnPage` 有：目前入住一行、客房清單（名稱・效果・時長・價格、入住鈕、取代提示）、金錢、自動住宿 HP／SP 門檻文字框＋儲存、載入失敗重試。
- 後端：`GET /api/inn` 回房型（含依等級算好的價格、`Seconds`）與目前住宿；`POST /api/inn/book` 在 `InnService.BookAsync` 先呼叫 `AdventureService.AdvanceAsync` 結算離線時間，再以結算後的角色檢查金錢，`Inn.Book` 扣錢、HP／SP 回滿、整筆取代 `character_lodgings`（每角色一列，`V000000015__add_inn.sql`）。結算後錢不夠時連結算一起回滾（`InnServiceTests.Book_RechecksMoneyAfterSettling`）。
- 冒險結算：`AdventureService` 讀那一列住宿（`Inn.ToBuff`），傳給 `AdventureSimulator.Run` 的單一 `CharacterBuff? lodging`；`Buffs.Multiplier` 依每場開打時刻判斷是否在 `[StartsAt, EndsAt)` 內。
- 自動住宿：`RestRules.ShouldRest`（死亡一定住；HP 低於門檻；有主動技能時 SP 低於門檻；門檻 −1 略過），`Hotel.Rest`（回滿到含裝備上限；Lv ≤ 100 免費；否則 max(round(10 × Lv × 係數), 10 × Lv)，付不起時經驗 ×0.8；等待 5 秒、死亡 15 秒）。門檻存在角色的冒險設定（`PUT /api/characters/me/settings`），冒險設定面板也能改。
- 房型徽章目前是城鎮的設計素材 `frontend/src/assets/town/room-<房型 id>.webp`，城鎮加成列依房型 id 取用（`BonusRow.roomId`）。房間背景還沒進 repo。
- 旅館、房價、自動住宿都沒有規格；`elixir-buffs` 有「同種密藥與住宿相乘」「住宿回應帶開始時間」兩條。
- 功能對照（2026-11-13）已和使用者確認，見下方「功能對照」。InnFit 回來後，使用者改了入住規則：房卡只買加成、不回滿；住宿改多筆紀錄、入住不結算。

## Design Source

- 來源：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 `InnFit`，v7（1794564296-dba6，2026-11-13；v6 依六項決定畫成 390×624～780，v7 改錢不夠的台詞、拿掉回滿與結算字樣）。產生器 `.design-work/work/innfit/build.py`，點擊測試 `work/innfit/test-inn.js`。
- 快取：`.spectra/design-cache/inn-screen/InnFit.dc.html`（git 忽略；素材網址 `/_blob/<blob id>`，原檔在 `.design-work/assets/<blob id>.*`）。
- 預覽：`python3 .claude/skills/ebo-canvas/scripts/canvas.py play InnFit -c fit/src -o inn -s "this.startView = 'rooms'=>this.startView = 'detSwap'"`，截圖 `shot inn --size 2690x970`。startView：`rooms`、`empty`、`auto`、`autoLv`、`detOk`、`detPoor`、`detSwap`、`booked`、`failMoney`、`failNet`、`thFail`、`loadFail`。

舞台寬 390、高 H＝624～780，k＝H/844；木台上緣 pt＝250 − (844 − H) × 0.4。單位都是舞台 px。

| 區塊 | 元素 | 數值 |
| --- | --- | --- |
| 進出 | 展開 | 整頁 `clip-path: circle(0 at ox oy)` → `circle(140% at ox oy)`，0.55s `cubic-bezier(.65,0,.25,1)`；圓心 ox＝195 − 81k、oy＝300k + 19（624 高 (135, 241)、780 高 (120, 296)）。收回同一點、同時長 |
| 進出 | 返回鍵 | left 14、top 14；左箭頭 18×18（stroke 2.4）＋「市集」14px、letter-spacing .1em、`#F3E3C0`、陰影 `0 1px 3px rgba(0,0,0,.9), 0 0 6px rgba(0,0,0,.6)` |
| 大廳 | 背景 | 高 pt + 40；`linear-gradient(rgba(251,246,234,.3) ×2), url(c95d61d5) 62% center / cover, #E8D8B8`；上緣 70 高 `rgba(42,27,14,.55)→0`，下緣 70 高 `0→rgba(42,27,14,.85)` |
| 大廳 | 瑪莎 `047c012e` | left 2、top 30、高 420、寬自動，不縮放；`drop-shadow(3px 0 10px rgba(30,18,8,.4))`；被木台蓋住下半 |
| 大廳 | 對話框 `b97d9abd` | left 172、top 40、208×104；padding 12 16 12 50；13px、行高 1.55、`#3A2410`；`drop-shadow(0 3px 5px rgba(30,18,8,.35))`；換台詞時 0.25s 從下 3px 淡入 |
| 木台 | 底 | top pt、到底；木紋 `6884370a` center top / cover，`sepia(.35) saturate(1.1) brightness(.5) contrast(1.08)`；上緣蓋櫃檯木條 `d137517f`（top −16、高 40、repeat-x、`drop-shadow(0 3px 2px rgba(0,0,0,.55))`）；padding-top 18 |
| 木台 | 店名列 | padding 12 12 6；「暖爐亭」Noto Serif TC 900 19px、letter-spacing .12em、漸層 `#FFFBEE→#F0CF8A`；副標「老闆娘・瑪莎」11px `#C9B58E` |
| 木台 | 金錢 | 高 30、圓角 15、padding 0 12 0 5；底 `linear-gradient(180deg,#1A1008,#2E1E10)`、框 `#C9A464`；錢袋 21×21（城鎮的 `purse.webp`）；數字 Cinzel 700 15px `#F6D98A`、千分位 |
| 木台 | 分隔線 | 高 1、margin 0 14 10，`transparent→#C9A464 18%→82%→transparent`，中間 7×7 菱形 |
| 目前入住 | 橫幅 | margin 0 10 10、padding 8 10、圓角 5；底 `linear-gradient(90deg, rgba(30,18,8,.45) 0, .78 40%, .9 100%)` 疊房間背景 30% center / cover；框 `#C9A464`；徽章 40×40；房名 Noto Serif TC 900 15px `#F6E8C8`；「剩 H:MM」Cinzel 700 13px `#F0CF8A` 靠右；效果 12px `#8FE0C8`；剩餘條高 4、`#C9A464→#F6D98A` |
| 目前入住 | 沒有入住 | 「目前沒有住宿加成」13px `#A8946E` 置中、padding 12、底 `rgba(0,0,0,.2)`、框 `rgba(201,164,100,.35)` |
| 分頁 | 門牌 | 兩塊各占一半、間距 6、padding 2 12 0、下方 8；高 44；門牌圖 `7d42c264` 100% 拉滿；Noto Serif TC 900 15px、letter-spacing .24em、`#4A2E14`。沒選：`translateY(2px)`、`brightness(.62) saturate(.7)`；選中：`translateY(-2px)`、亮。0.15s |
| 清單 | 區域 | 佔木台剩下的高（flex 1）、margin 0 10 10、padding 8、在區域內捲動、不顯示捲軸；底 `rgba(10,6,2,.42)`、框 `rgba(201,164,100,.28)` |
| 客房 | 房卡 | 高 78、間距 8、圓角 5；底 `linear-gradient(90deg, rgba(30,18,8,.05) 0, .1 30%, .8 56%, .92 100%)` 疊房間背景 30% center / cover；框 `#C9A464`；徽章 52×52 左 10 垂直置中；右側文字區寬 168、right 12：房名 Noto Serif TC 900 16px 漸層、效果 13px 700 `#8FE0C8`、底列時長 11px `#C9B58E`＋價格 13px 700 `#F0CF8A`（錢袋 14×14）；入住中：框 2px `#F6D98A`＋外光、「入住中」角標 left 68 top 7、11px 700 `#FFF2D6`、紅底 `#B3262A→#7A1418` |
| 詳情 | 羊皮紙 `75aeed6c` | left、right、bottom 14，padding 30 24 20；從下方滑出；背後遮罩 `rgba(30,18,8,.18)`、點了關閉；上緣中間圖釘 `59eb4a17` 26×26；關閉鈕 `f1386931` 34×34（right 16、top 14）；徽章 60×60；房名 20px `#3A2410`；效果 13px `#2F7A6A` |
| 詳情 | 取代說明 | 虛線上方 10；13px、`#6A5030`；換別間時 `#A0301E`：「會取代目前的（房名）（效果，剩 H:MM）」；同一間：「正在住這間，剩 H:MM」 |
| 詳情 | 花費與印章 | 「花費」12px＋錢袋 16×16＋Cinzel 700 19px `#3A2410`，錢不夠 `#B0301E`；紅印章 `759746ed` 66×66、rotate −3°、直排「入住」19px 900 `#F6EBD5`；停用 `grayscale(.85) opacity(.45)`、不縮放 |
| 自動住宿 | 門檻 | 盒子 padding 8 10 10、底 `rgba(0,0,0,.25)`、框 `rgba(201,164,100,.3)`；標題「自動住宿」12px 700、letter-spacing .2em、`#E8C888`。每列 grid `auto auto 26 1fr 26 44`、間距 7：標籤 HP（`#E8705A` 底）／SP（`#6EA8E0` 底）11px；「低於」12px；−／＋ 26×26 圓鈕（停用 opacity .35）；量條高 8（HP `#B84A3A→#E8705A`、SP `#3A6EA8→#6EA8E0`）；數值 Cinzel 14px `#F6E8C8`，「略過」12px `#8A7A60` |
| 自動住宿 | 住宿費 | 第二個盒子：標題「住宿費」＋右側「Lv N」11px `#A8946E`；一列「自動住宿一次」12px `#C9B58E`，值 700：免費 `#8FE0C8`、否則 `#F6E8C8`「1,200 起」 |
| 載入失敗 | 再試一次 | 木台上只放紅印章 92×92 置中（padding-bottom 20），字橫排兩行「再試／一次」19px 900、行高 1.2；按下 rotate −4° scale .9 |

素材（blob id 前 8 碼）：大廳 `c95d61d5`、瑪莎 `047c012e`、木紋 `6884370a`、櫃檯木條 `d137517f`、門牌 `7d42c264`、羊皮紙 `75aeed6c`、圖釘 `59eb4a17`、關閉鈕 `f1386931`（以上 8 張進 `frontend/src/assets/inn/`）；紅印章 `759746ed`、對話框 `b97d9abd` 沿用 `title/seal`、`title/bubble`，錢袋沿用 `town/purse`。房型（內容圖）：

| 房型 id | 徽章 | 房間背景 |
| --- | --- | --- |
| apprentice-loft | `87c454fc` | `0657d0f0` |
| training-chamber | `acfda66d` | `fbc71279` |
| fighters-bunk | `b0ea9422` | `8bb1000e` |
| stargazer-tower | `19e11db0` | `18b622f0` |
| bulwark-room | `56c3ace8` | `97fa24a9` |
| quiet-chapel | `1d5b6da7` | `b3f8428b` |

字型：店名、房名、門牌、印章用 Noto Serif TC 900；數字用 Cinzel；其他 Noto Sans TC。所有色碼改成 `theme.css` 變數，與既有變數同值的沿用（例如 `--brass` 是 `#C9A464`）。

## 功能對照

| 舊畫面或規則 | 新畫面 | 決定 |
| --- | --- | --- |
| 目前入住一行（房名、效果、剩餘） | 金框橫幅＋剩餘條 | 照稿 |
| 沒有入住 | 「目前沒有住宿加成」 | 照稿 |
| 客房清單、入住鈕、錢不夠停用 | 房卡＋羊皮紙詳情＋紅印章 | 照稿；時長照資料 |
| 取代提示 | 詳情的取代說明 | 照稿 |
| 金錢 | 錢袋 | 照稿 |
| 自動住宿文字框＋儲存 | −／＋ 每次 5，按了就存 | 使用者決定 4 |
| 入住失敗、載入失敗、重試 | 瑪莎台詞；「再試一次」紅印章 | 使用者決定 2 |
| 入住成功 | 瑪莎台詞，不跳提示框 | 使用者決定 3 |
| 房型徽章、房間背景 | 內容圖 R2 | 使用者決定 1 |
| 住宿費 | 後端給數字 | 使用者決定 5 |
| 舊外殼背包的旅館子頁 | 拿掉 | 使用者決定 6 |
| 入住回滿 HP、SP | 不回滿 | 使用者 2026-11-13 改規則 |
| 入住前結算離線時間 | 不結算；住宿多筆紀錄 | 使用者 2026-11-13 改規則 |

## Goals / Non-Goals

**Goals:**

- 照 InnFit v7 做旅館畫面，從城鎮市集進出，涵蓋客房、詳情、自動住宿、住宿費、所有台詞與載入失敗。
- 入住只扣錢與開始加成；住宿改成多筆紀錄，冒險結算依開打時刻找那筆的加成；入住不結算。
- `GET /api/inn` 帶自動住宿最低費用。
- 房型徽章與背景改成 R2 內容圖，城鎮加成列一起改。
- 補 `inn` 規格，既有行為逐條對照測試。

**Non-Goals:**

- 密藥不改成多筆紀錄（仍是每種一筆、喝前結算）。
- 不做退房、住宿紀錄畫面、戰鬥歷程。
- 冒險設定面板與舊外殼其他分頁不改。
- 大廳不分時段、瑪莎沒有表情變化。

## Decisions

### 住宿改成多筆紀錄，入住不結算

**Supersedes**: 裁定彙整〈（六）計畫 11：旅館〉「入住時先結算尚未結算的冒險時間，再用結算後的角色重算價格、重新檢查錢夠不夠」

`character_lodgings` 從「每角色一列」改成「每次入住一列」。入住時：

1. 以目前存檔的角色（不結算）算價格、檢查金錢；不夠回 400 `notEnoughMoney`。
2. 目前還沒到期的那一列（結束時間晚於現在）的結束時間改成現在。
3. 新增一列：開始＝現在、結束＝現在＋`seconds`。
4. 刪掉結束時間早於「現在 − 24 小時」的列（`AdventureSimulator.MaxSimulatedSeconds`，離線補算最多往前 24 小時，更舊的列用不到）。

冒險結算時讀「結束時間晚於這次模擬起點」的所有列，依開始時間排序；每場戰鬥找開打時刻落在 `[開始, 結束)` 的那一列。因為換房時舊列先截斷，任一時刻最多一列有效。

為什麼不結算：每列都保留自己的時間窗，之後才結算的場次仍套到正確的房型，不必在入住當下把舊房型的效果先算掉。這也消除了「結算中升級或自動住宿花錢，結算後錢不夠」的回滾問題。

替代方案：先「退房」（結算並停止）再開房，使用者考慮過後改選多筆紀錄；維持單列並把結算存檔，仍要處理結算後錢不夠，不採用。

### 入住不再回滿 HP、SP

使用者 2026-11-13：手動入住只是買加成，回血交給自動住宿。`Inn.Book` 只扣錢、產生住宿列；HP、SP 不變。設計文件 `2026-10-29-inn-design.md` 的回滿作廢；舊文件只當參考、不加註，以本決策為準。

### 住宿費由 Domain 算、放進旅館回應

`Hotel.MinimumCost(int level)`：Lv ≤ `FreeLevelMax`（100）為 0，否則 10 × Lv（就是 `Hotel.Rest` 的下限）。`InnResponse` 加 `long RestFee`。前端只顯示：0 →「免費」，否則千分位＋「起」。付不起扣經驗的規則不顯示。

### Domain 與資料介面

- `Lodging(string RoomId, DateTimeOffset StartsAt, DateTimeOffset EndsAt)` 不變。
- `Inn.Book(Character character, InnDefinition inn, InnRoom room, Lodging? active, DateTimeOffset now)` → `InnBooking(Character Character, Lodging Booked, Lodging? Ended, long Cost)`：`Character` 只扣錢；`Ended` 是 `active` 截斷到 `now`（`active` 為 null 或已到期時為 null）。
- `Inn.PruneBefore(DateTimeOffset now)` ＝ `now − MaxSimulatedSeconds`。
- `Inn.ToBuffs(IReadOnlyList<Lodging> stays, InnDefinition inn)` → 每列轉成 `CharacterBuff`，房型已不在內容的略過。
- `Buffs.Multiplier`／`ApplyTo` 的 `CharacterBuff? lodging` 改成 `IReadOnlyList<CharacterBuff> lodgings`，取開打時刻有效的那一筆。`AdventureSimulator.Run` 同步改參數；`BalanceReportService` 傳空清單。
- `ILodgingRepository`：
  - `Task<Lodging?> FindActiveAsync(long characterId, DateTimeOffset now, CancellationToken)`：開始 ≤ now ＜ 結束的那一列（只看結束時間會拿到之後才開始的列）。
  - `Task<IReadOnlyList<Lodging>> ListEndingAfterAsync(long characterId, DateTimeOffset after, CancellationToken)`：依開始時間排序。
  - `Task SaveBookingAsync(long characterId, Lodging booked, Lodging? ended, DateTimeOffset pruneBefore, CancellationToken)`：`ended` 以（角色、開始時間）找到那列更新結束時間；新增 `booked`；刪除結束時間早於 `pruneBefore` 的列。同一個 unit of work。

### 旅館畫面是 GameScreen 的第三種狀態

`GameScreen` 的 view 加 `{ name: "inn" }`。城鎮點「旅館」時切到旅館（城鎮先保持顯示，旅館在上層展開；展開結束才卸載城鎮），返回時旅館收回、城鎮以市集段（段落 1）顯示。`TownScreen` 加 `initialSegment` prop。旅館在 `StageFrame` 裡，跟城鎮一樣不呼叫冒險推進 API。

旅館畫面拿 `GameScreen` 的角色顯示金錢、等級與門檻；入住、存門檻、重讀角色成功時呼叫 `onCharacterChange`，回城鎮時城鎮顯示新的金錢。

`TownFacility` 的 `inn` 從 `facilityViews` 移除；`BagScreen` 的 `SubPage` 拿掉 `inn`，刪 `InnPage.tsx` 與測試。`AdventureScreen.test.tsx` 用 `inn` 當起始子頁的測試改用 `shop`。

### 瑪莎的台詞是純函式

`src/game/inn.ts` 的 `innkeeperLine(event, hasStay)`；`booked` 事件自己帶房名：

| 情況（event） | 台詞 |
| --- | --- |
| 沒事件、沒入住 | 歡迎光臨暖爐亭！今晚想住哪一間？ |
| 沒事件、有入住 | 房間還幫你留著呢。要換一間，還是續住？ |
| `booked`（房名） | （房名）收拾好了，好好睡一覺吧！ |
| `notEnoughMoney` | 哎呀，這間的錢好像還差一點呢。 |
| `bookFailed` | 咦，登記簿沒寫上去……等會兒再來吧。 |
| `thresholdFailed` | 剛剛那個沒記下來，再調一次好嗎？ |
| `loadFailed` | 哎呀，今天的房帳一時找不到…… |

事件在下一次動作（點房卡、調門檻、按再試一次、換分頁）時清掉，回到招呼。

### 入住失敗的處理

- `ApiError` 的 `reasons` 含 `notEnoughMoney`：台詞換成錢不夠，重新讀 `getMyCharacter` 更新金錢（可能被別的分頁花掉），詳情留著；花費變紅、印章停用由「新金錢 < 價格」自然得到。重讀失敗就保留原金錢。
- 其他錯誤（連線、逾時、5xx）：台詞換成 `bookFailed`，詳情留著可以再按。
- 送出中印章停用（避免連按）。

### 門檻步進與自動儲存

`stepThreshold(value, direction)`：

| 目前 | ＋ | − |
| --- | --- | --- |
| −1（略過） | 0 | 不能按 |
| 0 | 5 | −1 |
| 33 | 35 | 30 |
| 35 | 40 | 30 |
| 100 | 不能按 | 95 |

畫面數值立即改變；最後一次按下 `InnThresholdSaveDelayMilliseconds`（600，新增在 `timings.ts`）後送 `updateSettings`，內容是 `settingsForm(character)` 換掉兩個門檻。成功：`onCharacterChange`。失敗：兩個門檻退回上次存檔成功的值，台詞 `thresholdFailed`。離開旅館時有尚未送出的修改就立刻送出（不等延遲）。

### 時長格式

`stayLength(seconds)` 改成：時＝floor(seconds / 3600)、分＝round(餘數 / 60)；分為 0 寫「H 小時」，時為 0 寫「M 分」，否則「H 小時 M 分」。內容規定 60～86400 秒。

### 內容圖新增兩個類別

| 類別 | 鍵 | 畫布 | 構圖 | 透明 | 品質 | 上限 |
| --- | --- | --- | --- | --- | --- | --- |
| `room-badges` | 房型 id | 128×128 | 裁到不透明範圍、長邊 88%、置中（同 items） | 是 | 85 | 16384 |
| `room-backgrounds` | 房型 id | 800×450 | 等比放大到蓋滿、置中裁切（同 battlefields） | 否 | 75 | 61440 |

尺寸依畫面：徽章最大顯示 60（2 倍 120）；背景最寬 390（2 倍 780）。上限是估計值，匯入第一張時用 `--dry-run` 看實際大小，超出就回頭調整這個表與規格。`missing` 的預期鍵取 `backend/content/inn.json` 的 `rooms[].id`。前端 `ContentCategories` 加兩類、各一張佔位圖 `placeholders/room-badges.svg`、`room-backgrounds.svg`。

城鎮加成列的住宿徽章改用 `contentAsset("room-badges", roomId)`，刪 `frontend/src/assets/town/room-*.webp` 與 README 對應列。

### 硬規則

- 硬規則 3（Flyway）：新增 `backend/db/ddl/V000000021__lodging_stays.sql`，依 migration → `docker compose up -d` → `./scripts/scaffold-db.sh` 的順序；只用 `ALTER TABLE`（先加 `(character_id, ends_at)` 索引讓外鍵改用它，再拿掉以 `character_id` 為主鍵、加 `id BIGINT AUTO_INCREMENT` 主鍵；外鍵不動），不用 `DROP TABLE`／`TRUNCATE`。既有資料保留，每個角色原本那列變成第一筆紀錄。
- 硬規則 4、5：`Persistence/Models/CharacterLodging.cs` 由 scaffold 產生，不手改；Application 只看 Domain 的 `Lodging`。
- 硬規則 6：房型圖放 R2、只用 `tools/assets` 的 `import` 更新清單；內容檔不改。
- 前端規則 3、8、9：色碼進 `theme.css`；內容圖只經 `contentAssets.ts`；`schema.ts` 用 `pnpm generate:api` 產生。
- 前端規則 2：舞台 390×624～780，清單在區域內捲動、整頁不捲動；基準尺寸下內文 ≥ 13、小標籤 ≥ 11（稿子已照做）。
- 前端規則 5：步進、台詞、時長、展開圓心在 `src/game/`，不 import React。

## Implementation Contract

**行為**

- 玩家在城鎮市集點「旅館」，旅館從名牌圓形展開；按「市集」收回，城鎮停在市集段。旅館裡不呼叫冒險推進 API。
- 點房卡、詳情、紅印章「入住」：扣錢、得到 8 小時房型加成，HP、SP 不變；已有別間時舊的在此刻結束。橫幅換成新房、金錢扣掉、詳情收起、瑪莎說入住成功。
- 冒險結算時，入住前開打的場次用舊房型、之後用新房型，不論多久之後才結算（24 小時內）。
- 自動住宿門檻按了就存；住宿費照等級顯示。

**介面**

- `GET /api/inn` → `{ rooms: [{ id, name, kind, multiplier, seconds, price }], lodging: { roomId, startsAt, endsAt } | null, restFee: number }`。
- `POST /api/inn/book { roomId }` → `{ character, lodging, cost }`（形狀不變；`character` 的 HP、SP 與入住前相同）；錢不夠 400、`reasons: ["notEnoughMoney"]`；未知房型 400 `unknownRoom`。
- 前端 `InnVo` 加 `restFee: number`；`InnConverter` 轉換。
- `tools/assets`：`pnpm cli import room-badges <房型 id> <圖>`、`import room-backgrounds <房型 id> <圖>`。

**失敗模式**

- 載入旅館失敗：瑪莎說 `loadFailed`，木台只剩「再試一次」；按了重讀 `getInn`。
- 入住、存門檻失敗：見上方決策；都不跳系統提示框。
- 房型或背景缺圖：顯示該類佔位圖；圖片載入失敗同樣換佔位圖。
- 住宿列的房型已不在內容：不算加成（現況），也不出現在橫幅。

**驗收**

- 後端：`inn` 規格每個 Scenario 都有測試（`InnTests`、`InnServiceTests`、`InnEndpointsTests`、`LodgingRepositoryTests`、`AdventureSimulatorTests`、`HotelTests`、`RestRulesTests`）。
- 前端：`inn.test.ts`（純函式）、`InnScreen.test.tsx`、`GameScreen.test.tsx`、`TownScreen.test.tsx`、`contentAssets.test.ts`。
- `tools/assets`：`process.test.ts`、`missing.test.ts`。
- 手動：390×624、390×780、1440×720 逐個狀態截圖，對照稿子的 12 個 startView。
- `./scripts/check.sh` exit code 0。

**範圍**

- 在範圍內：上述後端規則、旅館畫面、城鎮進出、內容圖兩類與 12 張匯入、拿掉舊旅館子頁、`inn` 規格。
- 不在範圍：密藥紀錄方式、冒險設定面板、角色頁「密藥效果」卡片（照舊讀目前住宿）、其他設施。

## Risks / Trade-offs

- 住宿多筆後，模擬每場要在清單裡找有效那筆；24 小時內最多幾十筆（每次入住一筆），線性找就夠。〔估計：玩家一天入住次數；以實際資料表筆數確認〕
- 入住不結算，房價用存檔時的等級：離線很久的玩家可能用較低等級的價格入住，結算後才升級。使用者接受（價差只有等級差 × 50 × 倍數）。
- 截斷舊列依（角色、開始時間）比對：同一角色同一微秒入住兩次的機率可忽略；舊列在同一交易內更新。
- 房間背景與徽章的位元組上限是估計值，見「內容圖新增兩個類別」。

## Migration Plan

1. migration 套用後，既有那列保留成第一筆；舊程式的「每角色一列」假設由同一個變更內的 repository 取代，部署時前後端一起換。
2. 部署 NAS 時 Flyway 自動套用 V000000021。
3. 回滾：不提供自動回滾（新增欄位與索引不影響舊資料，但舊程式無法處理多列）。

## Open Questions

（無）
