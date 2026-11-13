---
name: ebo-canvas
description: Use when working on the EBO design canvas (claude.ai artifact 5cCvccEmyZvkiAeSffazXZ, boards like HomeJ) — generating art with grok in the EBO ink-and-watercolor style, keying it, uploading it, previewing a board locally, screenshotting states, comparing frames, or click-testing a board before publishing
---

# EBO 設計畫布工作台

畫布：https://claude.ai/artifact/5cCvccEmyZvkiAeSffazXZ（Design 類型；板子在 `project/*.dc.html`）。
工作資料夾：`.design-work/`（git 忽略、不在 session scratchpad，不會被清掉）。

```
.design-work/canvas/project/   畫布板子的本機副本（改這裡，再發布）
.design-work/runtime/          dc-runtime.js（畫布的執行環境）
.design-work/assets/<blob id>.<副檔名>   上傳過的素材，檔名就是 blob id
.design-work/raw/ out/         產圖原圖、去背成品
.design-work/play/ shots/      預覽頁、截圖
.design-work/work/             各頁的修改腳本與素材（例：work/forge/）
```

`S=.claude/skills/ebo-canvas/scripts`

設計規則（文字、版面、字型、用哪個工具產圖）見 `ebo-design-rules`；接到別的 session 或使用者的改稿需求時照 `ebo-design-request` 走。

## 板子

| 板子 | 內容 |
|---|---|
| `HomeJ` | EBO DEMO：標題、建角、城鎮與各設施，一開就是標題層 |
| `TitleFit` | 標題與建角的新比例稿，由 `work/titlefit/build.py` 產生（見下）；只在新成品「EBO 新比例畫面」（舊畫布那份 2026-11-12 已刪） |
| `TownFit` | 城鎮主畫面的新比例稿（全景三段＋上方狀態），由 `work/townfit/build.py` 產生；在新成品 |
| `InnFit` | 旅館（暖爐亭）的新比例稿，由 `work/innfit/build.py` 產生（房型讀 `backend/content/inn.json`）；在新成品 |
| `CompanionsFit` | 同伴（入口、獸欄、傭兵所）的新比例稿，由 `work/companionsfit/build.py` 產生（傭兵讀 `mercenaries.json`、冒險地讀 `world.json`）；在新成品 |
| `AssetsJ*` | 素材庫（介面、背景、精靈圖、職業、道具、立繪、備用）；`Fonts` 字型比較 |
| `StyleE`、`Bag2`、`Character2`、`Town`、`Assets` | 棄用，在畫布最下面 |

- 有互動的板子要在 `canvas.json` 標 `"is_interactive": true`，否則畫布上點了只會選取。
- 互動不能寫 `<script>`＋`addEventListener`（Play 模式不執行）：寫 `class Component extends DCLogic`，constructor 設 `this.state`，`renderVals()` 回傳值與 handler，markup 用 `onClick="{{x}}"`、`hidden="{{y}}"`、`<sc-for>`、`<sc-if>`。
- 一塊板子有很多狀態時，留一個 `this.startView = '…'` 加一排切換 chip，別人才能用 `-s` 直接打開某個狀態。

**TitleFit**：
- 五個外框：390×624、390×683、390×780；「細長」是 390×848，舞台停在 780；「扁」是 470×720，示意 1440×720 的視窗。整塊板子 2690×970，截圖用 `shot --size 2690x970`。
- 狀態（`startView`）：`title` `reg` `regpw` `relogin` `wait` `fail` `create` `name` `stats` `book`（職業介紹冊）`leave` `done` `crfail` `go`。
- 新增狀態要改 `build.py` 兩處：`viewState` 裡那張狀態表加一項，`VIEWS` 加 `[名稱, 中文標籤]`。切換 chip 由 `VIEWS` 產生，不用另外加。
- 點擊測試操作第一個外框（624）：`q('.tf-fr')` 取外框，再往裡面找元素（見 `work/titlefit/test-register.js`）。量數值用 `offsetTop` 這類 DOM 屬性，不受無頭視窗大小影響。
- 內容從 HomeJ 抽出來再改：要改 TitleFit 就改 `build.py` 裡的替換，HomeJ 不動。要兩邊一起改，需求會明講。

## 新比例成品「EBO 新比例畫面」

https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts（Design 類型，跟舊畫布分開）：放照新比例（寬 390、高 624～780）實作的畫面稿，目前有 TitleFit、TownFit、InnFit、CompanionsFit。

- 本機：`.design-work/fit/src/`（產生器的輸出，用舊畫布的 blob id，可直接預覽）→ `work/fit/remap.py` 換成新成品的 id → `.design-work/fit/project/`（發布用，含 `canvas.json`）。
- 兩邊的素材 id 不同：舊畫布的素材要先複製進新成品（`Artifact` publish、`url`＝新成品、`asset: true`、`from_url`＝舊畫布、`asset_ids` 一次最多 10 個），結果的「新 id ← 舊 id」寫進 `work/fit/blobmap.json`。`remap.py --missing` 列出還沒複製的。新產的素材直接上傳到新成品，也記進 blobmap（舊 id 欄填新 id 本身）。
- 預覽：`canvas.py play TownFit -c fit/src -o 名稱`（`-c` 指板子資料夾）。
- 發布：`root`＝`.design-work/fit` 的絕對路徑、`file_path`＝改過的板子（例 `<repo>/.design-work/fit/project/TownFit.dc.html`）；新增板子或改位置時連 `project/canvas.json` 一起送。

## 改板子：一律用腳本

不要手改 `canvas/project/` 裡的板子，改法寫成腳本放 `work/<頁>/`，重跑得出同樣結果：

- **補丁**（改 HomeJ 這類大板子）：`work/<頁>/patch-<名>.py`，開頭先把板子備份成 `<板子>-before-<名>.html`，再用 `rep(old, new, count=1)` 逐段替換；`rep` 要斷言比對次數，對不上就整個不寫。
- **產生器**（從別的板子抽出來重排，例如 TitleFit）：`build.py` 讀 HomeJ 產出整塊板子，改的是 `build.py` 不是板子。改之前把 `build.py` 複製成 `build-v<目前畫布版本>.py`。目前版本是這個 session 上一次發布結果裡的版本號；不知道的話，改用日期命名 `build-<YYYYMMDD>.py`。
- 替換字串要挑全檔唯一的那段；HomeJ 的 markup 縮排是 8 格，複製時連空白一起確認。

## 產圖（grok，EBO 畫風）

```bash
zsh $S/gen.sh 名稱 1:1 item "A single small leather coin purse tied with a cord…"
zsh $S/gen.sh 名稱 16:9 scene "Interior of a cozy blacksmith forge…"
zsh $S/gen.sh 名稱 16:9 item "Four leather bookmark tabs in a row…" --pieces 4 red green blue gold
zsh $S/gen.sh 名稱 1:1 item "An empty red seal stamp impression…" --all
zsh $S/gen.sh bg-place-03 1:1 stage "a gloomy swamp with murky green water pools…"
```

- 畫風固定在 `gen.sh`（墨線＋水彩、紙紋、暖色；item 加洋紅底、no text、no drop shadow）。換專案時改這段即可。
- `stage`＝戰鬥舞台背景：繪本水彩、明亮可愛、限定細節（不寫細節限制，grok 會畫得太寫實）；主體只寫場景。21 個地點的場景清單與重產腳本在 `.design-work/work/battle-bg/`（`gen-all.sh 03 07` 只重產指定地點）。
- 每張圖實際送出的提示詞存在 `raw/名稱.prompt.txt`。素材庫每張素材的提示詞整理在 `docs/art/prompts/demo/`；新增素材後依序跑 `.design-work/work/prompts/` 的 `collect.py`（從 grok／Gemini 本機紀錄抓提示詞）→ `thumbs.py 原圖… grok 原圖…`（比對用縮圖，要帶檔名）→ `match.py` → 在 `names.json` 補新素材的「名稱、現用／備用、板子」→ `build-docs.py` 更新，對不到的要人工比對（見那裡的 keyed.py、cropped.py、byname.py）。
- item 會自動跑 `keyclean.py`（preparing-ai-generated-ui-assets skill），並產生 `out/名稱.check.png`（疊在淺、深兩種底上）。**一定要看 check 圖**，看邊緣有沒有紫紅色。
- `--all`：主體把洋紅包在裡面時用（印章框、戒指圈），而且主體本身沒有粉紫色。
- 立繪不用 grok，用 Gemini（見 `ebo-design-rules` 的產圖表）。
- grok 不太聽「很細很長」：長條素材取一段左右鏡射後橫向重複鋪，不要壓扁。

## 上傳與記錄

1. 上傳：`Artifact` 工具，`url`＝畫布、`asset: true`、`file_path(s)`。
2. 記下本機副本：`python3 $S/canvas.py asset <blob id> out/名稱.png`。沒記下的話，`play` 會列出缺哪些。
3. 缺本機副本時（例如舊素材）：`Artifact read`，`url`＝畫布、`path`＝blob id（一次一個；`paths` 不收 blob id），下載到 artifact-files 資料夾後 `python3 $S/canvas.py import <那個資料夾>`。只補要截圖的畫面用到的就好。
4. 預覽伺服器在 8770（`canvas.py serve`，`shot`／`test` 會自動開），根目錄是 `.design-work/`。

## 預覽、截圖、測試

```bash
python3 $S/canvas.py play HomeJ -o forge-open \
  -s "this.state = { forgeOn: false,=>this.state = { forgeOn: true," \
  -s "phase: 'closed', book: 0=>phase: 'open', book: 0"
python3 $S/canvas.py shot forge-open
python3 $S/canvas.py strip .design-work/shots/cmp.png a.png b.png --crop 0,140,390,360 --zoom 2
python3 $S/canvas.py test forge-open steps.js
```

- `-s 舊=>新` 改初始狀態，直接打開要看的畫面；每組要剛好比對到 1 處（比對到多處就改挑更長、唯一的字串）。
- `shot --size 寬x高` 截不同視窗大小；`strip` 的 `--zoom` 只收整數。
- `test` 的無頭視窗實際只有 500×757：板子底部 87px 在視窗外，`tap` 在那裡會點空。量底部元素改比對位置。
- transition／animation 在無頭瀏覽器（Edge） 裡停在起點，會留下看不見卻擋點擊的層。要測整段流程或量最終位置，加 `-s "</style>=>*{transition:none !important;animation:none !important}</style>"`。
- 有 `transform`（旋轉、縮放）的元素，量法看要量什麼：
  - 回報 CSS 數值（欄位 top、高度）：用 `offsetTop`／`offsetHeight` 一路加上去，量的是變形前的版面，跟 CSS 寫的值對得上。`getBoundingClientRect` 量到的是旋轉後的外接框，會變大。
  - 檢查有沒有重疊、超出：看截圖，或比對兩個元素的 `getBoundingClientRect`。
- `test` 的腳本裡可用 `log()`、`q()`、`qa()`、`tap(el)`（在元素中心找實際點到的東西再點）、`swipe(el, dx)`，步驟寫成 `window.STEPS = [[等待毫秒, () => {...}], …]`。
  **要用 `tap`，不要直接 `el.click()`**：3D、疊層會擋住點擊，只有 `tap` 測得出來（帳簿翻開後點不到「闔上」就是這樣漏掉的）。
- 無頭瀏覽器 的虛擬時間會跳過動畫中間。要看動畫某一刻：用 `-s` 在 `</style>` 前加一段 CSS，讓動畫 `animation-play-state: paused` 並把 `animation-delay` 設成負的那個時間點；transition 則直接把屬性寫成那一刻的值。PixiJS 的 ticker 也不會自己跑，要手動呼叫一次 tick 再 `app.render()`。

## 發布

1. 改 `.design-work/canvas/project/` 裡的檔案，先 `play` ＋ `shot` 確認。
2. `Artifact` 發布：`url`＝畫布、`root`＝`.design-work/canvas` 的絕對路徑、`file_path`＝那個板子的絕對路徑（例 `<repo>/.design-work/canvas/project/TitleFit.dc.html`），加一個簡短的 `label`（寫這次改了什麼）；新增、刪除板子，或板子的大小、位置變了，要用 `files: {"project/canvas.json": "project/canvas.json"}` 一起送。發布結果裡的版本號（v463 這種）就是回報用的畫布版本。
3. 發布被拒（畫布上有更新的版本）時，先 `Artifact read` 讀回來比對差異再合併，不要硬蓋。

## 畫布上的坑

- 畫布會把 `{{文字}}` 包成 `<span class="sc-interp">`：寫 `.x span{…}` 會套到內層，要寫 `.x > span{…}`。
- 板子只能從 cdnjs、jsdelivr、unpkg 載外部腳本（PixiJS 用 `https://cdn.jsdelivr.net/npm/pixi.js@8.22.0/dist/pixi.min.js`）。
- `/_blob/` 網址沒有副檔名：PixiJS 載圖要先用 `Image` 載完再 `Texture.from(img)`，不能用 `Assets.load`。
- 子元素自己寫 `visibility:visible` 會蓋過父層的 `hidden`；父層只靠 `opacity:0` 藏起來時，那個子元素看不見卻會擋點擊。這類 `.on` 要連同所在畫面的條件一起判斷。
- CSS 的 `calc()` 不能除以長度（`calc(var(--h) / 368px)` 無效）。要依高度算倍率時，在外框或產生器先算好，寫成不帶單位的變數（例 `--cs`）。
- 捲動清單想讓出角落給固定的按鈕：兩層 mask 用 `mask-composite:intersect`（WebKit 寫 `-webkit-mask-composite:source-in`），一層做下緣淡出，一層用 `radial-gradient` 挖出按鈕那一圈；清單底部再補 padding，捲到底時最後一項才不會被蓋。
