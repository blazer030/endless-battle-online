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

## 產圖（grok，EBO 畫風）

```bash
zsh $S/gen.sh 名稱 1:1 item "A single small leather coin purse tied with a cord…"
zsh $S/gen.sh 名稱 16:9 scene "Interior of a cozy blacksmith forge…"
zsh $S/gen.sh 名稱 16:9 item "Four leather bookmark tabs in a row…" --pieces 4 red green blue gold
zsh $S/gen.sh 名稱 1:1 item "An empty red seal stamp impression…" --all
```

- 畫風固定在 `gen.sh`（墨線＋水彩、紙紋、暖色；item 加洋紅底、no text、no drop shadow）。換專案時改這段即可。
- item 會自動跑 `keyclean.py`（preparing-ai-generated-ui-assets skill），並產生 `out/名稱.check.png`（疊在淺、深兩種底上）。**一定要看 check 圖**，看邊緣有沒有紫紅色。
- `--all`：主體把洋紅包在裡面時用（印章框、戒指圈），而且主體本身沒有粉紫色。
- 立繪不用 grok：交給使用者用 Gemini 產。
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

- `-s 舊=>新` 改初始狀態，直接打開要看的畫面；每組要剛好比對到 1 處。
- `test` 的腳本裡可用 `log()`、`q()`、`qa()`、`tap(el)`（在元素中心找實際點到的東西再點）、`swipe(el, dx)`，步驟寫成 `window.STEPS = [[等待毫秒, () => {...}], …]`。
  **要用 `tap`，不要直接 `el.click()`**：3D、疊層會擋住點擊，只有 `tap` 測得出來（帳簿翻開後點不到「闔上」就是這樣漏掉的）。
- 無頭 Chrome 的虛擬時間會跳過動畫中間。要看動畫某一刻：用 `-s` 在 `</style>` 前加一段 CSS，讓動畫 `animation-play-state: paused` 並把 `animation-delay` 設成負的那個時間點；transition 則直接把屬性寫成那一刻的值。PixiJS 的 ticker 也不會自己跑，要手動呼叫一次 tick 再 `app.render()`。

## 發布

1. 改 `.design-work/canvas/project/` 裡的檔案，先 `play` ＋ `shot` 確認。
2. `Artifact` 發布：`url`＝畫布、`root`＝`.design-work/canvas`、`file_path`＝那個板子，加一個簡短的 `label`；新增或刪除板子時一併送 `project/canvas.json`。
3. 發布被拒（畫布上有更新的版本）時，先 `Artifact read` 讀回來比對差異再合併，不要硬蓋。

## 畫布上的坑

- 畫布會把 `{{文字}}` 包成 `<span class="sc-interp">`：寫 `.x span{…}` 會套到內層，要寫 `.x > span{…}`。
- 板子只能從 cdnjs、jsdelivr、unpkg 載外部腳本（PixiJS 用 `https://cdn.jsdelivr.net/npm/pixi.js@8.22.0/dist/pixi.min.js`）。
- `/_blob/` 網址沒有副檔名：PixiJS 載圖要先用 `Image` 載完再 `Texture.from(img)`，不能用 `Assets.load`。
- 介面不寫操作說明文字；台詞照角色口吻，畫面被蓋住時就不放台詞。
