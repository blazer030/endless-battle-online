## Context

- 設計稿 EBO DEMO（HomeJ）定案後，畫面要逐頁換新。使用者決定先做標題（登入、註冊）與建角，因為它們跟遊戲頁關聯最小。功能對照（2026-11-09）確認現有功能都放得進設計稿；設計稿缺的三個狀態（讀角色、讀取失敗、建角送出失敗）由 EBO-design 補在 v452。
- 現況：`frontend/src/ui/AuthScreen.tsx`（帳號密碼表單、登入／註冊兩顆按鈕）、`CreateCharacterScreen.tsx`（一頁表單，資料來自 `GET /api/content`）、`App.tsx`（auth → loading → create／adventure 的 phase，401 回 auth）。`/api/content` 混了怪物名、全部職業、屬性／出生地／性別，沒有技能；`/api/jobs` 要有角色、回的是轉職狀態。
- 設計稿原本 390×844 太細長，前端硬規則 2 寫 375×667。EBO-design 研究了瀏覽器實際可用的視窗（iOS 26 Safari Compact 展開時高寬比 1.75～1.78、Android Chrome 估 1.9～2.0、iPhone SE 約 1.5，其中 Android 與 SE 為推算值），使用者定案：寬 390、高寬比 1.75～1.85 之間伸縮不補背景，超出時等比縮放補背景。EBO-design 依此開新板子 TitleFit（v453），HomeJ 不動。
- 後端錯誤：註冊重複帳號 409「帳號已被使用」、登入失敗 401「帳號或密碼錯誤」、建角重名或帳號已有角色 409「此帳號已有角色，或角色名稱已被使用」、輸入錯誤 400；`ApplicationExceptionHandler` 把訊息放在 ProblemDetails 的 `detail`。

## Design Source

- 來源：EBO 設計畫布 https://claude.ai/artifact/5cCvccEmyZvkiAeSffazXZ 的板子 `project/TitleFit.dc.html`（畫布 v454，2026-11-09 由 EBO-design 依新比例從 HomeJ 抽出重做，v454 改成能力要分完 10 點才能確定；產生腳本 `.design-work/work/titlefit/build.py`）。流程與台詞同 HomeJ v452。
- 快取：`.spectra/design-cache/title-and-create-screens/TitleFit.dc.html`（git 忽略）。素材原檔在 `.design-work/assets/<blob id>.*`。
- 補背景（使用者 2026-11-09 改決定）：TitleFit 各畫面往舞台外延伸的天空、木桌、大廳（上下 80、左右 40）**不實作**；舞台外一律是外框的共用背景（先用深色，之後可換一張大的靜態圖）。表中負的 top／left／right／bottom 一律在舞台邊緣被裁掉。
- 預覽：`python3 .claude/skills/ebo-canvas/scripts/canvas.py play TitleFit -o tf -s "this.startView = 'title'=>this.startView = '<狀態>'"`，狀態有 title、reg、wait、fail、create、name、stats、book、leave、done、crfail、go；加 `-s "{ marks: false }=>{ marks: true }"` 顯示錨點。截圖 `shot tf --size 2252x940`（四框：683、722、細長補上下、矮補左右）。

舞台：寬 390、高 H＝683～722；以下單位都是舞台 px，`bottom` 從舞台下緣算。

| 畫面 | 元素 | 數值 |
| --- | --- | --- |
| 標題 | 招牌 `a02aafe6` | 貼上緣 top −34、寬 300、高 223、置中；字「無盡戰記」Noto Serif TC 900 46px `#4A2E14` letter-spacing .12em；副標 Great Vibes 23px `#5A3A1C`；擺動 ±1.1° 7s |
| 標題 | 天空（四時段）| dawn `e6eb4eb9`、day `815cd4a2`、dusk `67e2bf51`、night `6a33dc7e`；圖層 left/right −48、top −80、下緣在 bottom 330；寬 486、`background-position: center bottom -140px`；露出高度＝H−356（327～366）；漂移 26s |
| 標題 | 木桌 `6884370a`＋前緣木條 `d137517f` | 貼下緣 left/right −40、bottom −80、高 436（舞台內 356）；底色 `#6A4A2C`；木條高 36、top −18 |
| 標題 | 登記證（自己 `e5a31933` 有蠟封／空白 `3a7feeba`）| 貼下緣 bottom 12、寬 236、高 336；自己那張 rotate(−2deg)；空白證 translate(222px,50px) rotate(7deg)；註冊時交換；欄位名 Noto Serif TC 700 11px `#8A6A40` letter-spacing .3em；輸入 Noto Sans TC 18px `#2A1A0C` 下緣 1.5px dotted |
| 標題 | 印章 `75974 6ed`（`759746eda4a85bfa24de7eba49435578`）| 64×64、字 Noto Serif TC 900 18px 直書 `#F6EBD5`；停用 `grayscale(.85) opacity(.45)`、不縮放 |
| 標題 | 錯誤字 | 13px `#A8321E` |
| 標題 | 讀角色 | 登記證與那疊 translate(0,470px) 0.5s；墨點三個 13px `#F3E3C0`、間距 14、bottom 190、0.45s 後淡入、跳動 1.2s |
| 標題 | 讀取失敗字條 `75aeed6c` | bottom 108、寬 274、高 176、rotate(−2deg)；字 Noto Serif TC 900 17px `#4A2E14`；重試印章右下、發光 2.4s |
| 標題 | 過場 | 鋪滿舞台；「歡迎回來／踏上旅途」Noto Serif TC 900 26px `#4A2E14`、副字 Great Vibes 26px `#6A4626`；夜晚改 `#F6EBD5` |
| 建角 | 大廳 `12b6be86` | top −80、寬 470；露出高度＝H−502（181～220）；元素高 calc(H − 406px) |
| 建角 | 莉瑟 `a32a504a` | 貼上緣 top 28、高 486、left −4 |
| 建角 | 對話框 `b97d9abd` | top 44、left 166、寬 216、高 104；13px 行高 1.6 `#3A2410`、最多三行 |
| 建角 | 離開 | top 12、left 12、14px `#F3E3C0` |
| 建角 | 櫃檯木桌 | 同標題木桌，高 582（舞台內 502）、bottom −80 |
| 建角 | 登記證 `3a7feeba`（成功後蠟封 `e5a31933` 淡入）| 照 250×368 排版、bottom 12、`transform-origin: 50% 100%`、scale(1.3)、rotate(−1deg)；列高 ≥25；欄位名 11px（放大後 14.3）；「剩 N 點」9px `#F6EBD5` 底 `#8A2A1A`；出發印章 bottom −4、right −6、可按時發光 |
| 建角 | 回答彈窗（紙 `75aeed6c` border-image `140 210 70 60 fill / 28px 42px 14px 12px`）| left/right 14、中心在 H−250（bottom 250＋translateY(50%)）；壓暗層 top＝H−520 到底 `rgba(20,10,4,.55)`；選項四欄格、間距 8；選項 min-height 38、`#F6EBD5`、選中 `#5A3A1C` 字 `#F6EBD5`、確定 `#8A2A1A` |
| 建角 | 屬性徽章 | 地 `374bc9f4`、水 `9bc2ed53`、火 `df05ace4`、木 `d85235a7`、風 `e8f93fd9`、毒 `9f653c0d`；26×26；三欄兩排 |
| 介紹冊 | 紙 `dd89ce59`、書籤 戰士 `d702e92f`／術士 `d371eec9`／盜賊 `88b2eedf`／樂手 `2ccfd7be` | 介紹冊 top 206 到底；書籤 44×110 翻轉、往上露到 148，選中多露 20 |
| 介紹冊 | 頁首 | 職業圖示 30×30、名稱 Noto Serif TC 900 24px、英文 Great Vibes 24px `#8A6A40`；「就職」印章 top 4、right 16 |
| 介紹冊 | 立繪區 | top 58、高 190；右欄 left 160、top 62；定位 13px 700、說明 13px、成長兩欄 Cinzel 12px（最快的 `#8A2A1A`） |
| 介紹冊 | 技能清單 | top 234 到介紹冊底（約 205～244 高）、內部捲動、下緣淡出 26px、不顯示捲軸；每招一行標題（名稱、入門或「前置 xx LvN」、右側「最高 LvN」）＋ Lv1 與最高級摘要各一行 13px |

職業圖示（`job-icons`，匯入 R2）：戰士 `62fa0017`、術士 `707f3b3a`、盜賊 `eb722b44`、樂手 `36314e2f`。職業立繪已在 R2 `portraits/job-6～9-male|female`（不照設計稿的 fx 位移，立繪在匯入時已對齊）。

主要色（設計稿寫死，實作改成 CSS 變數）：底色 `#2A1A0C`、紙 `#F6EBD5`、紙淺 `#F3E3C0`、墨 `#2A1A0C`、深褐 `#3A2410`、褐 `#4A2E14`、褐中 `#5A3A1C`、褐淡 `#8A6A40`、木 `#6A4A2C`、木深 `#6A4626`、印章紅 `#8A2A1A`、錯誤紅 `#A8321E`、選中框 `#C9A464`、標示列 `#B07A2A`；陰影與壓暗用 `rgb(20 10 4 / α)`、`rgb(90 60 30 / α)`、`rgb(255 244 222 / α)`。字型：Noto Sans TC 400／500／700／900、Noto Serif TC 700／900、Cinzel 500／700、Great Vibes。

## Goals / Non-Goals

**Goals:**

- 標題與建角照 TitleFit 實作，含讀角色、讀取失敗、送出失敗、離開確認與兩段過場。
- 新畫面的版面基準可重用：之後其他頁套同一個舞台元件。
- 職業與技能、建角選項改由 `/api/public/` 的專屬 API 提供，前端經轉換層取得。
- 前端硬規則 2、3、4 改成新畫面的規則。

**Non-Goals:**

- 遊戲頁（`AdventureScreen`、城鎮）不改；過場結束後進的仍是現有遊戲頁。
- `/api/content` 不移除、不改（舊畫面照用）；拆除放在之後的 `game-api-dto-vo`。`/api/jobs`（轉職狀態）不動。
- 不做角色頁的登出（設計稿在角色頁，之後做角色頁時再做）；本變更只有建角的「離開」會登出。
- 不記住上次登入的帳號（使用者決定，重新整理後登記證是空白）。

## Decisions

### Public data endpoints under /api/public

新增 `backend/EndlessBattle.Api/Endpoints/PublicEndpoints.cs`：`app.MapGroup("/api/public").AllowAnonymous()`，掛 `GET /jobs`、`GET /settings`。不用驗證中介層的結果，所以帶過期 token 也照樣 200。考慮過：讓 `/api/jobs` 有登入時多帶個人欄位——回應格式不固定、過期 token 時語意模糊、不好快取，使用者同意不做；另取 `/api/jobs/catalog`——為了避開既有網址取怪名，使用者否決。

### Job list assembly reuses SkillSummary

在 Application 新增公開資料的組裝（例如 `PublicDataService`）：讀 `IGameContent` 的職業、技能與職業技能關聯，技能每級的摘要呼叫 `SkillSummary.Of(level)`，跟技能頁同一份文字。回應欄位用完整名稱（`strength`、`intelligence`、`life`、`vitality`、`agility`、`charisma`、`faith`），成長每項 `{ minimum, maximum }`。不在後端另做「1～10%」這種範圍文字：前端顯示 Lv1 與最高級兩行即可，之後轉職頁要逐級表也能沿用。

### Element advantage exposed from ElementTable

`GET /api/public/settings` 的屬性帶 `strongAgainst`、`weakAgainst`（屬性 id），由 Domain 的 `ElementTable` 算出，前端不重寫相剋規則（硬規則 1）。性別、屬性、出生地的名稱目前寫在 `ContentEndpoints.cs`，搬到共用的地方讓兩支 API 都用同一份。

### Frontend reads public data through converters

`frontend/src/api/` 新增公開資料的請求與轉換：OpenAPI 產生的 DTO → `toVo` 轉成 `frontend/src/game/` 的型別（職業介紹冊需要的職業、技能、成長；建角選項）。缺必要欄位就丟錯、不認得的欄位忽略（使用者的分層要求，同 `game-web` 的 Converter 做法）。新畫面不 import `/api/content` 的型別。

### Stage frame fits a flexible-height stage

新增純函式 `fitStage(width, height)`：`scale = min(width / 390, height / 683, 1.25)`、`stageHeight = clamp(height / scale, 683, 722)`、置中位移。新增元件（例如 `StageFrame`）用 `window.innerWidth/innerHeight`（版面視窗）量視窗、監聽 `resize`，把舞台設成 `width: 390px; height: var(--stage-h)`、`transform: scale(...)`、`overflow: hidden`。舞台外的空間由外框統一鋪一個背景（先用深色 `#2A1A0C`，之後要換成一張大的靜態背景圖只改外框一處），各畫面不畫延伸背景。版面全部照 TitleFit 以 top／bottom 錨點絕對定位，伸縮的區塊用 `calc(var(--stage-h) - Npx)`。`#root` 的 max-width 480 只套舊畫面；新畫面的外框鋪滿視窗、底色 `#2A1A0C`。考慮過：用 CSS `aspect-ratio` 與 container query——縮放字與圖要逐一處理，跟設計稿逐像素對不上。也考慮過用 `visualViewport`（review 時改掉）：它在雙指放大與鍵盤彈出時會變小，舞台會跟著縮，抵銷放大、鍵盤一出來登記證縮到一半。

### Theme tokens for the new screens

`theme.css` 保留舊變數（舊畫面還在用），新增新畫面用的色彩變數（紙、墨、褐、木、印章紅、錯誤紅等，見 Design Source）與陰影用的 RGB 三元組變數（例如 `--shade-rgb: 20 10 4`，用 `rgb(var(--shade-rgb) / .55)`），新畫面的 CSS 只用變數。`theme.test.ts` 的「palette 對應每個 CSS 色彩變數」改成「`palette.ts` 有的鍵要跟 CSS 一致」，因為新變數不給 Pixi 用。字型 `index.html` 改成 Noto Sans TC 400;500;700;900、Noto Serif TC 700;900、Cinzel 500;700、Great Vibes。推翻舊設計文件 `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §10 的色票與字型（裁定彙整計畫 3「主題與創角規則依 spec §3、§10」中 §10 的部分）；§3.2 創角規則不變。

### Title flow and creation draft as plain TypeScript

- 時段：純函式 `timeOfDay(hour)`（5～7 dawn、8～16 day、17～18 dusk、其餘 night），畫面每分鐘重算一次。
- 建角草稿：`frontend/src/game/` 的純 TS 模型，管欄位值、能力配點（剛好 10 點、每項至少 1、剩餘點數，有剩就不能確定）、名字驗證（trim 後 2～12 字，沿用 `game/names.ts`）、六欄是否填完；HP／SP 用既有的 `deriveStats(baseStatsFromAllocation(...))`。莉瑟的台詞依狀態決定，文字放在畫面層。
- 流程：`App.tsx` 的 phase 改成 title（含 mode login／reg、load null／wait／fail、loadTo login／reg／open）→ create → game；有 token 打開網頁直接 load（loadTo open）。過場時長（歡迎回來與踏上旅途 2.4 秒、墨點延遲 0.45 秒等）放 `frontend/src/motion/timings.ts`（硬規則 7）。
- 離開：`clearAccessToken()`，回 title、自己那張帶剛註冊的帳號（只在記憶體，不存 localStorage）。

### Design assets and job icons

- 設計素材用 `tools/assets` 的 `pnpm cli design` 轉成 `frontend/src/assets/title/`（招牌、四時段天空、木桌、木條、登記證兩張、印章、字條紙、介紹冊紙、對話框、大廳、莉瑟、書籤四張、屬性徽章六張），每張依顯示大小的 2 倍設 `--max-width`，單張上限 300 KB。
- 四個職業圖示用 `pnpm cli import job-icons job-6～9` 上傳 R2 並 commit 清單；職業圖示與立繪都經 `contentAsset()` 取得，缺圖用佔位圖（硬規則 8）。

### Allocation must use all 10 points

`CharacterFactory` 的配點檢查從「每項至少 1、總和不超過 10」改成「每項至少 1、總和剛好 10」，不是 10 時丟 `CharacterCreationException("能力點數必須剛好分完 10 點")`，Api 回 400。原本照 WOG 原版允許沒分完（作廢），使用者 2026-11-09 決定改掉：沒分完沒有好處，多半是誤點。舊設計文件 §3.2 沒寫這點，程式註解「沒分完的點數作廢（WOG 如此）」一併改掉。建角畫面：能力彈窗還有點數時「就這樣」停用（停用時不給按下回饋），能力列掛「剩 N 點」；不再有「作廢」的提醒台詞。設計板 TitleFit 的提醒流程由 EBO-design 改成停用。舊的 `CreateCharacterScreen` 在被取代前送出未分完的配點會收到 400 並顯示錯誤，可接受。

### Replace old screens

新畫面完成後刪除 `AuthScreen.tsx`、`CreateCharacterScreen.tsx`（含 CSS 與測試）；`screens.css` 裡只剩舊畫面用的部分保留。

## Implementation Contract

**Behavior**：見 specs `public-game-data`、`screen-layout`、`title-and-character-creation`。玩家看到的：打開網頁 → 沒登入是登記證、有登入是墨點後直接進遊戲；登入 → 墨點 → 歡迎回來 → 遊戲；註冊 → 蠟封 → 墨點 → 公會櫃檯建角 → 出發 → 踏上旅途 → 遊戲。

**Interface / data shape**：

- `GET /api/public/jobs` → `[{ id, name, tier, parentId, unlockLevel, proficiencyRequired, statRequirements: [{ stat, minimum }], growth: { strength: { minimum, maximum }, intelligence, life, vitality, agility, charisma, faith }, skills: [{ id, name, description, maxLevel, prerequisite: { skillId, name, level } | null, levels: [{ level, summary }] }] }]`。
- `GET /api/public/settings` → `{ sexes: [{ id, name }], elements: [{ id, name, strongAgainst, weakAgainst }], births: [{ id, name }] }`，id 為數字。
- 兩支都在 OpenAPI `v1` 文件裡；`frontend/src/api/schema.ts` 由 `pnpm generate:api` 產生（硬規則：不得手改）。
- 前端：`fitStage(width, height) → { scale, stageHeight, offsetX, offsetY }`；`timeOfDay(hour) → 'dawn' | 'day' | 'dusk' | 'night'`；建角草稿模型的 API 由實作決定，但驗證規則以 spec 為準。

**Failure modes**：登入 401、註冊 409／400 → 證上紅字照 `detail`；請求沒有 API 回應 → 證上「發生錯誤，請稍後再試」；讀角色 401 → 清 token 回登記證；讀角色其他失敗 → 字條＋重試；建角 409 → 重名台詞＋打開名字欄；配點沒分完在前端就不能確定，後端也回 400；400 → 莉瑟說 `detail`；其他 → 「咦，登記簿那邊沒有回應……」、內容保留。公開 API 讀內容失敗屬伺服器錯誤（500），建角頁取不到職業或選項時走「讀取失敗＋重試」。

**Acceptance criteria**：

- 後端測試：兩支公開 API 不帶 token、帶過期 token 都 200；`job-6` 技能順序與前置、`防禦心得` 5 級；摘要等於 `SkillSummary.Of`；火剋木、怕水。
- 前端測試：`fitStage` 照 spec 範例表；`timeOfDay` 照時段表；建角草稿（名字長度、配點、六欄填完才可出發）；標題與建角元件測試（登入錯誤、註冊重複、讀角色各目的地、重試、建角 409／400／斷線、離開清 token）。
- 手動：`pnpm dev` 在 390×683、390×722、412×880、375×555、1440×780 開標題、建角、介紹冊各截一張，對照 TitleFit 同狀態；確認 360×640 沒有整頁捲動。
- `./scripts/check.sh` exit 0。

**Scope**：本變更內——兩支公開 API、舞台元件、標題與建角畫面與流程、設計素材、職業圖示、theme 與字型、硬規則 2／3／4 改寫、刪除舊的兩個畫面。本變更外——遊戲頁、城鎮、角色頁登出、`/api/content` 拆除、其他頁套新比例。

**硬規則**：不涉及內容檔、資料表、migration、scaffold 產生檔、admin 路由；會重新產生 `frontend/src/api/schema.ts` 與 `backend/openapi/`（用 `dotnet build` 與 `pnpm generate:api`，不手改）；遊戲規則（相剋）留在 Domain。

## Risks / Trade-offs

- [Android 與 iPhone SE 的視窗高度是推算值] → 縮放是依實際視窗即時計算，估錯只影響補背景多寡；上線後可做探測頁實測。
- [絕對定位、逐像素照設計稿，之後改版面要改很多數值] → 錨點與伸縮規則集中在每個畫面的 CSS 開頭，並跟 TitleFit 的錨點說明一致。
- [字型變重（Noto Serif TC 900、Great Vibes）拖慢首次載入] → `display=swap`；只載用到的字重。
- [設計素材進 git 讓 repo 變大] → 每張依顯示大小 2 倍轉 WebP、單張上限 300 KB；本變更約 25 張。
- [`/api/content` 與新 API 並存一段時間，資料有兩份出口] → 新畫面只用新 API；拆除排在 `game-api-dto-vo`。
- [行動瀏覽器在網址列收起或旋轉時視窗大小會變] → 監聽 `window` 的 resize 重新計算（版面視窗會跟著網址列與旋轉變，不受雙指放大與鍵盤影響）；遊戲頁面本身不捲動，Safari 工具列維持展開。

## Migration Plan

純前端與新增 API，不動資料庫。部署順序不拘（新前端需要新 API，兩者同一個版本一起上）。回滾：回到前一個 commit 重新部署即可。

## Open Questions

（無）
