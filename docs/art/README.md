# 美術資源

遊戲用到的圖分兩種，放的地方不同：

| | 是什麼 | 放哪裡 | 怎麼更新 |
|---|---|---|---|
| 內容圖 | 道具、怪物（含寵物）、技能、職業立繪、職業圖示、戰鬥背景 | Cloudflare R2 bucket `ebo-content`（`https://ebo-assets.blazer030.dev`），git 只存清單 `frontend/src/assets/content-manifest.json` | `tools/assets` 的 `import` 指令，跟著 release／hotfix |
| 設計素材 | UI 元件、場景、NPC、佔位圖 | `frontend/src/assets/<分類>/`，進 git、由 Vite 打包 | `tools/assets` 的 `design` 指令，跟著畫面改版 |

原檔（生圖的 PNG）上傳到私人 bucket `ebo-originals`，不進 git。

## 內容圖的規格

| 類別 | 鍵 | 畫布 | 構圖 | 透明 | 品質 | 上限 |
|---|---|---|---|---|---|---|
| `portraits` 職業立繪 | `<職業 id>-male`、`<職業 id>-female`（角色 `Sex` 1 男、2 女） | 800×1200 | 人物高 1100、腳底 y=1180、身體中線 x=400 | 是 | 80 | 160 KB |
| `job-icons` 職業圖示 | `<職業 id>` | 96×96 | 圖案長邊 88% 置中 | 是 | 85 | 12 KB |
| `skills` 技能圖示 | `<技能 id>` | 96×96 | 圖案長邊 88% 置中 | 是 | 85 | 12 KB |
| `items` 道具圖示 | `<道具 id>` | 160×160 | 圖案長邊 88% 置中 | 是 | 85 | 20 KB |
| `monsters` 怪物與寵物 | `<怪物 id>` | 540×540 | 縮到寬 513／高 486 以內、水平置中、腳底 y=529 | 是 | 80 | 60 KB |
| `battlefields` 戰鬥背景 | `<地點 id>`（`world.json` places 的數字） | 1024×1024 | 等比縮放後置中裁切填滿 | 否 | 75 | 150 KB |

- 鍵只能用小寫英文、數字與連字號，而且要是 `backend/content` 裡有的 id（見「看缺哪些圖」）；新內容要先加進內容檔再匯入圖。R2 鎖定後刪不掉，所以打錯的鍵會在上傳前被擋下，`--dry-run` 只提醒。
- alpha > 24 的像素算不透明；「圖案」指不透明像素的外框。
- 超過上限時品質每次降 5 重試，降到 60 還超過就拒絕。
- 原圖比規格小時照樣輸出、會印出「已放大」的警告；怪物原圖建議至少 600 高。
- 程式版本在 `tools/assets/src/process.ts`。

### 立繪對齊

立繪在匯入時就對齊好，畫面上只要放在固定位置，不用逐張調位移：

- 人物從頭頂到腳底縮放成 1100 高，頭頂在 y=80、腳底（最下面的不透明列）在 y=1180。
- 身體中線＝人物高度 35%～95% 區間內，每一列不透明像素中點的中位數；武器、披風不容易把它拉歪。中線對齊 x=400。
- 頭頂預設是最上面的不透明列。法杖、帽子比頭高時，用 `--head-y <原圖的 y>` 指定頭頂；這個值會記在清單，重新匯入結果一樣。
- 任何不透明像素超出畫布就拒絕，並說明是哪一邊。

## 匯入內容圖

1. 第一次使用：把 `.env.r2.example` 複製成 repo 根目錄的 `.env.r2`，填入 R2 API token 的 `R2_ENDPOINT`、`R2_ACCESS_KEY_ID`、`R2_SECRET_ACCESS_KEY`（Cloudflare → R2 → Overview → API Tokens，權限 Object Read & Write，只限 `ebo-content`、`ebo-originals`）。`.env.r2` 不進 git，也不要貼到任何地方。
2. 先試轉看預覽：

   ```sh
   cd tools/assets
   pnpm cli import portraits job-6-male ../../.design-work/assets/<檔名>.png --dry-run
   ```

   預覽圖在 `.asset-preview/<類別>/<鍵>.png`：棋盤底看得出透明，立繪有頭頂、腳底（紅）與中線（藍）的輔助線，怪物有腳底線，圖示有 88% 框。
3. 沒問題就拿掉 `--dry-run` 再跑一次：工具會上傳原檔與內容圖，兩者都成功才更新清單。
4. commit 清單（訊息例：`📄 docs: add art for job-6-male`），跟著 release 或 hotfix 上線。

### 換圖

再匯入一次同一個鍵就好。內容圖的檔名是 `<類別>/<鍵>.<內容雜湊>.webp`，換圖＝上傳新檔＋清單改指向新檔；舊檔留在 R2（bucket 設了永久鎖定，不能覆寫或刪除），所以舊版本永遠指得到當時的圖，換錯了把清單改回去即可。

### 看缺哪些圖

```sh
cd tools/assets && pnpm cli missing          # 六類全部
cd tools/assets && pnpm cli missing monsters # 只看一類
```

應有的鍵從 `backend/content/*.json` 推出來。清單裡有、內容沒有的鍵會列在「多：」那一行。

## 遊戲怎麼找圖

- 一律透過 `frontend/src/render/contentAssets.ts` 的 `contentAsset(類別, 鍵)`：清單有就回 R2 網址，沒有就回該類的佔位圖。
- R2 的圖載入失敗時也退回佔位圖（`contentArtCandidates`）。
- 佔位圖在 `frontend/src/assets/placeholders/<類別>.svg`，是灰階剪影。SVG 是美術檔不是程式碼，裡面的色碼不受「顏色只寫在 `theme.css`」的規則約束。
- 冒險頁舞台：怪物與寵物用 `monsters/<怪物 id>`；怪物名字在血條上，寵物保留名牌。

## 設計素材

```sh
cd tools/assets
pnpm cli design ui paper-strip ../../.design-work/assets/<檔名>.png --max-width 600
```

輸出 `frontend/src/assets/ui/paper-strip.webp`：品質 85、保留透明、不裁切，比 `--max-width` 寬才等比縮小；超過 300 KB 就拒絕。

## 提示詞

日系手繪（設計畫布 EBO DEMO）的提示詞在 [`prompts/demo/`](prompts/demo/README.md)；道具描述檔在 `prompts/items/`。`prompts.md` 是舊的深藍夜色舞台範本，只留作參考。
