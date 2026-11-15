## Context

- 城鎮公會前的「任務板」紙籤目前打開舊外殼的「任務」分頁（`GameScreen` 的 `facilityViews.missions` → `{ tab: "missions" }` → `AdventureScreen` 的 `MissionsScreen`）。
- 舊任務頁（`frontend/src/ui/MissionsScreen.tsx`）：「可接」「進行中」「已完成」三個子頁的文字卡片（標題、委託者、說明、條件、委託內容、進度條、報酬、重置時間）、「接取」「交付／完成」「放棄 → 確定放棄／取消」按鈕、錯誤文字、完成結果（委託者的話＋獲得列）、載入失敗「重試」；戰報有任務進度時（`refreshSignals.missions`）重新載入。條件的職業、性別、出生地名稱來自 `AdventureScreen` 讀的名稱表（`getPublicJobs`、`getCreationOptions`）。
- API：`GET /api/missions`、`POST /api/missions/accept|abandon { missionId }` 回 `MissionBoardResponse { available, active, completed, activeLimit, nextDailyReset }`；`POST /api/missions/complete` 回 `MissionCompleteResponse { board, character, completionMessage, experience, money, items, skill, skillGranted, levelsGained, pet }`。每筆 `MissionResponse` 有 id、giver、title、description、level、placeId／placeName、daily、kills（怪物、地點、count、progress）、deliver（道具、quantity、owned）、requirements（jobId、sex、birth、prerequisiteId／Title）、rewards（經驗、金錢、道具、技能、寵物）、ready、expiresAt、deliverPet（species、carried）、requiredSkill（skill、level、met）。沒有完成台詞、道具分類、技能目前等級、條件名稱、每日委託開放等級。
- 規則（Domain `MissionRules`、`MissionBoard`、`DailyMissions`、`MissionRewards`，Application `MissionService`）：最多 3 個進行中；接取原因與順序、完成的檢查順序、報酬、每日擲骰與台北 09:00 重置都已有實作與測試（`MissionRulesTests`、`MissionBoardTests`、`DailyMissionsTests`、`MissionRewardsTests`、`MissionServiceTests`、`MissionEndpointsTests`、`MissionTableTests`），但除了 `mission-objectives` 外沒有規格。
- 內容（實測 `backend/content/missions.json`）：344 筆；每日 8 筆（Lv 50、50、150、150、350、500、500、700，標題都有「[每日]」前綴）、一般 336 筆（Lv 1～5500，126 筆沒有前置；沒前置的 Lv ≤ 50 有 27 筆、≤ 100 有 34 筆、≤ 300 有 50 筆）；擊殺目標最多 4 個、交付最多 6 種、一般委託交寵物 30 筆、技能條件 7 筆（全是 Lv 5500）；報酬是技能的 6 筆都是 Lv1；完成台詞只有 26 筆有寫（其餘是空字串）；條件只用到前置 210、職業 46、互斥 8、性別 4（出生地 0）。
- 勳章所（`honour-screen`，2026-11-14 封存）已做好 `RevealLayer`、`ContentImage` 的 `placeholder`、12 張分類佔位圖（`src/assets/placeholders/items/`）、`KeeperSay` 的莉瑟頭像、`BackButton` 的 `disabled`、紅印章 `Seal`、圖片預載 `screenImages.ts`。

## Design Source

- 來源：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 `MissionsFit`（2026-11-15 v28；v29 加換頁方案，使用者選 A；比較板 `MissionsPages`；EBO-design 依本變更的 8 項決定畫；底稿是舊畫布 HomeJ 的任務板）。產生器 `.design-work/work/missionsfit/build.py`（資料 `build-data.py` → `missions-data.json`，取正式內容），點擊測試 `.design-work/work/missionsfit/test-flow.js`。
- 快取：`.spectra/design-cache/missions-screen/MissionsFit.dc.html`（git 忽略；素材 `/_blob/<id>`，原檔在 `.design-work/assets/<id>.*`）。
- 預覽：`cd .design-work && zsh work/missionsfit/shots.sh <狀態>…`（624／683／780 三框並排）；狀態名以 `shots.sh` 與板子的切換 chip 為準（佈告欄、莉瑟說、完成三組）。
- 下表是主要版位；其餘每條規則（顏色、陰影、字級、轉角、傾斜）以快取檔的 `.msp`／`.ms-*` 規則為準。舞台寬 390、高 H＝624～780，k＝H／844。

| 區塊 | 元素 | 數值 |
| --- | --- | --- |
| 進出 | 展開 | 圓心＝公會前「任務板」紙籤中心：x＝max(195 + 104k, 390 − 113k)、y＝540k + 19（624 → (306, 418)、683 → (299, 456)、720 → (294, 480)、780 → (291, 518)）；0.55s，同旅館、同伴、勳章所 |
| 進出 | 返回 | 左上「公會前」（同旅館的返回鍵位置）；委託單右上關閉（×） |
| 背景 | 公會內牆 | `82726833`（720×1280）鋪滿 |
| 佈告欄 | 外框 | top 46、高 bh＝手邊上緣 − 94（394～502）；木框 `eb177ca7` 九宮格（四角取原圖 150px、顯示 30px）；軟木 `cc3ffb99` 另鋪一整塊蓋到木框內緣 |
| 佈告欄 | 標題紙（換頁方案 A，v29） | 軟木上緣兩張並排（`.ms-tabs` left −4、top −4），每日左、一般右，後一張 margin-left −10；底圖 `e30f6058`＋雙線框；Noto Serif TC 900 16px、字距 .2em、padding 8 15 6；兩角圖釘（每日紅 `ad435803`、一般黃銅 `6ee68da0`）15×15；目前頁在前（轉 −1.5°），另一張在後（`translateY(5px) rotate(2.5deg) scale(.93)`、`brightness(.8) saturate(.7)`、字色較淡）；張數紅圈 24 高、Cinzel 700 13px、在紙右上（−9, −10），每日紙在左上（−10, −10） |
| 佈告欄 | 單子 | 3 欄、排高 rh＝min(112, (bh − 140) / 3)、間距 10；底圖 `e30f6058`；傾斜輪流 −2.5／1.8／−1.2／2.6／−1.8／1.2°；名稱 13px、委託者 11px、Lv 與種類 11px；每日紅圖釘、一般黃銅 |
| 佈告欄 | 每日頁 | 3×3 不捲動（top 52、bottom 28，垂直置中）；右上重置紙條 `c2c9385f`，12px |
| 佈告欄 | 一般頁 | 軟木裡上下捲動（top 58、bottom 24、上緣淡出 9、下緣 16）；完成的一疊在右上標題紙旁（72×48），數字 18 |
| 櫃檯 | 手邊 | 三格；單子高 hh＝128 + (H − 624) × 0.256（128～168），離下緣 8 + (H − 624) × 0.051（8～16）；名稱 Noto Serif TC 900 13px 最多兩行、目標 11px 一行、進度 Cinzel 13、「＋N 項」11px；「達成／可交」章 `6ee9aa59` |
| 櫃檯 | 櫃檯／木條／紙條 | 櫃檯 `702d44ef` top＝手邊上緣 − 28；木條 `cf92f721` top＝手邊上緣 − 44；「進行中 N / 3」紙條 top＝手邊上緣 − 38 |
| 委託單 | 羊皮紙 | left／right 18、top 64、最低到舞台底 − 28；名稱到報酬在紙內捲動（下緣淡出 14）；莉瑟的話＋放棄／印章那排固定在下面；關閉 `f1b14431` |
| 委託單 | 印章 | 紅印章（同勳章所 `Seal`）；停用時旁邊寫「手邊已有 3 張」；完成時右上「完」章 76×76（`6ee9aa59`，壓下去的動畫） |
| 傳話 | 莉瑟 | 頭像 36 圓（`db299a57`＝莉瑟立繪）＋一句，同勳章所的傳話框；失敗偏紅 |
| 讀取 | 讀取中／失敗 | 讀取中 6 張空白單子淡淡閃；失敗：軟木正中間莉瑟一句＋「再試一次」紅印章 92×92（同同伴、勳章所） |
| 空板 | 每日未開放 | 只剩圖釘；等級低於 `firstDailyLevel` 時每日頁正中間釘「Lv N」小紙 |

素材（blob id 前 8 碼）見上表；道具圖用 12 張分類佔位圖（同勳章所），金錢用金幣圖示（快取檔 `COIN`）。轉檔前先比對前端已有的同一張圖（例如莉瑟立繪 `title/clerk`、紅印章 `title/seal`、羊皮紙 `inn/parchment`、圖釘 `inn/pin`、關閉 `inn/close`），相同就沿用不重轉。

## 功能對照

| 舊畫面或規則 | 新畫面 | 決定 |
| --- | --- | --- |
| 「可接」子頁（每日、一般兩組） | 佈告欄每日頁、一般頁 | 照稿；一般頁在軟木裡捲動（使用者 2026-11-15：正式內容一般委託遠超過 9 張） |
| 「進行中」子頁 | 櫃檯手邊三張 | 照稿；手邊只寫第一項目標＋「＋N 項」（使用者 2026-11-15） |
| 「已完成」子頁 | 一般頁右上的一疊＋完成清單；今天完成的每日留在每日頁原位蓋「完」 | 照稿 |
| 卡片的條件、委託內容、報酬 | 委託單 | 照稿；技能條件一行（使用者 2026-11-15）；道具用分類佔位圖（使用者 2026-11-15） |
| 進度條 | 手邊單子、委託單的進度條 | 照稿；只差技能時不蓋「達成／可交」 |
| 重置時間 | 每日頁右上紙條、每日委託單「每日委託・N 小時後重置」 | 照稿 |
| 接取／交付／完成按鈕 | 紅印章 | 照稿；送出後委託單不關 |
| 放棄 → 確定放棄／取消 | 委託單左下「放棄」→ 再確認 | 照稿 |
| 錯誤文字、「進行中已達 3 個」提示 | 莉瑟說；接取印章停用＋「手邊已有 3 張」 | 使用者 2026-11-15：EBO-design 提案（v28），照提案 |
| 完成結果（委託者的話＋獲得列） | 「完」章＋委託者的話＋「這次拿到」 | 照稿；沒有台詞時不放委託者那塊 |
| （舊畫面沒有）完成過的委託台詞 | 點「完」章或清單一筆 | 使用者 2026-11-15：API 只對已完成的補 `completionMessage` |
| 載入失敗「重試」、「載入中…」 | 莉瑟一句＋「再試一次」；6 張空白單子 | 照提案 |
| 「目前無任務」等空白文字 | 只剩圖釘；每日未開放釘「Lv N」 | 照提案（不寫說明文字） |
| 戰報有任務進度時重新載入 | 不需要（城鎮與任務板不打冒險 API，進入時讀一次） | 拿掉 `refreshSignals.missions` |
| 舊外殼「任務」分頁 | 拿掉 | 使用者 2026-11-15 |
| （舊畫面三個子頁一眼可見） | 上緣兩張標題紙＋另一頁張數、打開時依狀況選頁 | 使用者 2026-11-15：一般頁不夠明顯，選換頁方案 A（v29） |

## Goals / Non-Goals

**Goals:**

- 照 MissionsFit 做佈告欄兩頁、手邊三張、委託單、完成清單，涵蓋所有台詞、停用條件、送出中、讀取中、讀取失敗、空板、進出。
- 任務板回應補上完成台詞、道具分類、技能目前等級、條件名稱、每日委託開放等級；舊外殼不再有任務分頁。
- 補 `missions` 規格，既有行為逐條對照測試。

**Non-Goals:**

- 不改任務規則與內容檔（包括「學過的技能不再給」，報酬技能都是 Lv1，不需要升級規則）；不補完成台詞內容。
- 不做道具內容圖（R2 道具圖仍幾乎是空的，用分類佔位圖）；不做怪物圖。
- 冒險戰報、舞台上的任務進度與「任務達成」名牌不變。
- 錯誤只回代碼的後端改動另做（使用者 2026-11-15 排在本變更之後）；本變更的畫面只依 `reasons` 代碼說話，不顯示 `detail`。

## Decisions

### 任務板畫面是 GameScreen 的第六種狀態

`GameScreen` 的 `revealed` 表加 `missions: { origin: missionsRevealOrigin, segment: 2 }`，沿用勳章所的展開／收回與收回後停在公會前段、帶最新角色（完成委託會改角色）。`facilityViews` 拿掉 `missions`。佈告欄、委託單、完成清單之間的切換由 `MissionsScreen`（新，`src/ui/missions/`）自己管。

替代方案：委託單另成 GameScreen 的狀態；返回關係散在外層，不採用（同勳章所）。

### 任務板回應只加欄位，計算留在 Domain

- `MissionBoardView` 加 `FirstDailyLevel`（`MissionBoard.Build` 從內容的每日委託取最低等級，沒有為 null）。
- `MissionResponse` 加 `CompletionMessage: string?`：只有 `completed` 的委託帶內容台詞，空字串轉 null。
- `MissionRewardItemResponse`、`MissionDeliverResponse` 加 `Category: ItemCategory`（內容查表）。
- `MissionRequiredSkillResponse` 加 `LearnedLevel: int?`（`board.Learned` 查同技能，沒學過 null；`Met` 仍用 `MissionRules.HasRequiredSkill`）。
- `MissionRequirementsResponse` 加 `JobName`、`SexName`、`BirthName`（職業查內容、性別與出生地查 Domain `CharacterOptions`，沒限制為 null）。
- `MissionBoardResponse` 加 `FirstDailyLevel: int?`。

都是查表，Api 不做規則判斷。既有欄位不變，舊前端照常運作（任務分頁在本變更最後才拿掉）。

替代方案：前端讀 `getPublicJobs`／`getCreationOptions` 自己對名稱、讀 `/api/content` 查分類；新畫面不用通用內容 API（記憶：新畫面用專屬 API），不採用。

### 兩頁的單子與手邊的資料由 `src/game/missions.ts` 從任務板算出

- `displayTitle(mission)`：拿掉開頭的「[每日]」。
- `kindLabel(mission)`：有擊殺「討伐」、否則有寵物「寵物」、否則「交付」。
- `dailyNotes(board)`：`available` 與 `completed` 裡的每日委託合併，依等級、再依內容順序（API 的 `available` 已是內容順序；完成的照同一順序插回），帶 `done`。
- `regularNotes(board)`：`available` 裡的一般委託，照 API 順序。
- `completedRegular(board)`：`completed` 裡的一般委託（API 已是新到舊）。
- `handSlip(mission)`：第一項目標文字與進度、其餘項數、進度比例（沿用 `progressRatio`）、章（`ready` 時「可交」或「達成」，沿用 `actionLabel` 判斷交付）。
- `objectiveRows(mission, accepted)`、`rewardRows(rewards)`、`completionRows(result)`、`requirementText(mission)`：委託單各列，資料型別帶 `met`／`short` 與分類，畫面只負責排版。
- `resetHours(nextReset, now)`：剩餘時間無條件進位到小時、最少 1。
- 版位：`missionsRevealOrigin(H)`、`missionsLayout(H)`（佈告欄高、排高、手邊上緣、手邊高、離底），四捨五入到整數。

舊畫面專用的 `requirementLines`、`objectiveLines`、`rewardLines`、`resetHoursText`、`missionErrorMessage`、`completionLines` 隨舊畫面刪掉；`progressRatio`、`actionLabel`、VO 型別保留。

### 換頁用上緣兩張標題紙，打開時依狀況選頁

使用者 2026-11-15 覺得一般委託那頁不夠明顯（前期每日未開放，打開只看到空的每日頁），從 v29 的三個方案選 A：軟木上緣兩張標題紙並排，目前頁在前、另一張在後變暗，點後面那張換頁，左右滑照舊；底部兩根頁數圖釘拿掉。另一頁有可接的單子時在它的紙角用紅圈標張數（每日＝可接且今天沒完成、沒接在手上；一般＝全部可接）。每次從公會前打開，在第一次讀到任務板時選頁：每日頁有單子（可接或今天完成）停每日頁，否則停一般頁；讀取中每日紙在前、不標張數。選頁與張數寫成 `src/game/missions.ts` 的純函式 `openingPage(board)`、`pageCounts(board)`。

替代方案：B 露出另一頁的一疊（要多想一步、每日加到 9 張就要換位置）、C 下緣吊牌（靠近手邊、容易被忽略），不採用。

### 送出後委託單不關，失敗後重讀任務板

接取、放棄、完成成功時畫面換成回傳的任務板，委託單留著同一筆、顯示新狀態（接取後變進行中、放棄後變可接、完成後變完成檢視＋「這次拿到」）。完成時用回傳的角色更新（`onCharacterChange`）。失敗時莉瑟說原因、委託單不關，並重讀任務板（資料可能過期，例如另一個分頁已接滿）。同一時間只送一個請求；送出中印章、放棄、關閉、返回鍵都停用，點單子也不換委託單（同勳章所的修正）。

### 莉瑟傳話是一張純表

`liseLine(event)` 回傳 `{ text, failed }`，表在 `missions-screen` 規格的「Lise speaks feedback and errors」。多個原因時取表中最前面的那個。背包滿的分類：報酬道具都同一分類時用該分類的 `categoryLabel`，不同分類（或沒有道具）時用「背包」（後端只回 `bagFull`，不知道是哪一件放不下）。技能已學過的完成列寫「已經學會（技能），這次沒有學到」（MissionsFit 示範寫了等級，回應沒有已學等級，報酬都是 Lv1，拿掉等級）。

### 拿掉舊任務分頁

`AdventureScreen` 的 `Tab` 拿掉 `missions`、分頁列拿掉「任務」；刪 `MissionsScreen.tsx`、`MissionsScreen.css`、`MissionsScreen.test.tsx`；`refreshSignals` 拿掉 `missions`（只給舊任務分頁用）。`api/client.ts` 的任務方法與轉換器保留給新畫面。

### 設計素材

用 `tools/assets` 的 `design missions <名稱> <來源圖>` 轉出 `frontend/src/assets/missions/*.webp`；前端已有同一張圖就沿用。木框九宮格保留原圖切片（不縮，同勳章所陳列板木框）。

### 硬規則

- 硬規則 1、4、5：開放等級在 Domain，Api 只查表轉換；不涉及 scaffold 檔與資料表。
- 硬規則 6：內容檔不改；不涉及 R2。
- 前端規則 2、3、5、7、8、9：舞台 390×624～780、一般頁、完成清單、委託單在區域內捲動；色碼進 `theme.css`（同值沿用勳章所、同伴的變數）；單子、台詞、版位在 `src/game/`；程式用到的時長沿用 `timings.ts`（展開 `RevealMilliseconds`），CSS 動畫秒數寫在 CSS；道具圖只經 `ContentImage`＋分類佔位圖；`schema.ts` 用 `pnpm generate:api` 產生。
- 不涉及 migration、admin。

## Implementation Contract

**行為**

- 公會前點「任務板」→ 佈告欄（依狀況停在每日頁或一般頁）；左右滑或點後面那張標題紙換頁、另一頁標張數；點單子 → 委託單；印章接取／交付／完成；「放棄」再確認；一疊 → 完成清單 → 一筆；「公會前」回公會前。停在任務板時不呼叫冒險推進 API。
- 所有回饋由莉瑟在委託單裡說，不跳提示框；手邊滿 3 張接取印章停用；送出後委託單不關。
- 舊外殼沒有任務分頁。

**介面**

- 任務 API 回應加 `completionMessage`、道具 `category`、`requiredSkill.learnedLevel`、`requirements.jobName／sexName／birthName`、`firstDailyLevel`（形狀見 Decisions）。不新增端點。
- 前端：`Mission`、`MissionBoardVo`、`MissionCompletionVo` 加對應欄位；`getMissions`、`acceptMission`、`abandonMission`、`completeMission` 不變。

**失敗模式**

- 讀取失敗：軟木正中間莉瑟一句＋「再試一次」。
- 送出失敗：委託單裡莉瑟說對應台詞、失敗偏紅；重讀任務板；委託單不關。
- 道具沒有內容圖或載入失敗：分類佔位圖。

**驗收**

- 後端：`missions` 每個 Scenario 都有測試（新欄位：`MissionEndpointsTests`、`MissionBoardTests` 或對照正式內容的回應測試）。
- 前端：`missions.test.ts`（純函式表）、`MissionsScreen.test.tsx`（新，`src/ui/missions/`）、`GameScreen.test.tsx`、`AdventureScreen.test.tsx`、`refreshSignals.test.ts`。
- 手動：390×624、390×780、1440×720 逐格截圖，對照 MissionsFit 的狀態。
- `./scripts/check.sh` exit code 0。

**範圍**

- 在範圍內：上述畫面、進出、回應加欄位、拿掉舊任務分頁、`missions` 補規格、設計素材、`ebo-canvas` 板子表加 MissionsFit、延後清單拿掉舊 `MissionsScreen` 的條目。
- 不在範圍：任務規則與內容、錯誤只回代碼、道具與怪物內容圖、戰報與舞台。

## Risks / Trade-offs

- [任務板回應變大：一般委託可接的都帶詳情] → 量小（實測前看 network；高等級可接約 50～90 筆），不另做單筆詳情 API；手動驗證時記下實際大小。
- [每日頁合併 available 與 completed 的順序] → 依等級、再依內容順序；內容順序取自 API 的 `available` 排序，完成的每日用 id 的內容序號（`mission-N` 的 N）排，測試釘住。
- [資料過期時畫面以為可以接或交] → 伺服器仍會擋，莉瑟說對應台詞並重讀任務板。
- [背包滿說的分類不精確] → 報酬道具同分類時精確，不同時說「背包」；之後錯誤代碼改版時可再帶分類。
