## 1. 匯入工具專案

- [x] 1.1 依「匯入工具是獨立的 pnpm 專案 `tools/assets`」建立 `tools/assets`（TypeScript strict、eslint、prettier、vitest，相依 sharp 與 `@aws-sdk/client-s3`），`pnpm check` 依序跑 eslint、prettier 檢查、`tsc -b`、vitest；`scripts/check.sh` 在 admin 之後加跑它；`.gitignore` 加 `.env.r2` 與 `.asset-preview/`；新增只有欄位名的 `.env.r2.example`。驗證：先寫一條會失敗的 `cli.test.ts`「未知指令以非零 exit code 結束並印出可用指令」再實作讓它通過；`cd tools/assets && pnpm check` exit 0；`git check-ignore .env.r2` 有輸出。

## 2. 轉檔規格

- [x] 2.1 [after: 1.1] Content image categories and output specifications（圖示類與共通檢查，依「規格依試轉結果」）：`items` 輸出 160×160、`skills` 與 `job-icons` 輸出 96×96，裁到不透明外框、長邊 88%、置中、保留透明；alpha > 24 算不透明；超過上限時品質每次降 5、降到 60 仍超過就拒絕；需要放大時照樣輸出並回傳警告；未知類別與不符合 `^[a-z0-9-]+$` 的鍵拒絕。驗證：先寫失敗測試 `process.test.ts`「152×546 的道具圖輸出 160×160 且圖案長邊 141 並置中」「技能圖輸出 96×96」「上限設很小時依序嘗試 85、80…60 後拒絕」「放大時有警告」「鍵含大寫時拒絕」，再實作。
- [x] 2.2 [after: 2.1] Content image categories and output specifications（怪物與戰鬥背景）：`monsters` 輸出 540×540、縮到寬 513／高 486 以內、水平置中、最下面的不透明列在 y=529；`battlefields` 輸出 1024×1024 不透明、等比填滿置中裁切。驗證：先寫失敗測試「怪物腳底在 y=529 且水平置中」「1600×900 的背景輸出 1024×1024 且沒有透明像素」，再實作。
- [x] 2.3 [after: 2.1] Portrait alignment（依「立繪在匯入時對齊」）：立繪輸出 800×1200，人物高 1100、最下面的不透明列在 y=1180、身體中線（人物高度 35%～95% 每列不透明中點的中位數）在 x=400；`headY` 指定時以原圖該列為頂；任何不透明像素超出畫布就拒絕並說明哪一邊。驗證：先寫失敗測試「兩張不同尺寸的合成立繪腳底都在 1180、中線都在 400」「高 1000 的原圖頂端落在 y=80」「headY 40 把原圖第 40 列放到 y=80」「左邊超出時拒絕並寫『左側超出畫布』」，再實作。

## 3. 上傳與清單

- [x] 3.1 [after: 1.1] Content manifest 與 Immutable content-hashed upload（依「檔名帶雜湊、永不覆寫，清單在 git」「先上傳、再寫清單」）：清單讀寫（`baseUrl`、`assets` 鍵依字母排序、`file`／`original`／`headY`）；物件鍵 `<類別>/<鍵>.<SHA-256 前 12 碼>.webp` 與原檔鍵；儲存體介面（存在檢查、上傳）；已存在就略過；兩個上傳都成功才寫清單。驗證：先用假儲存體寫失敗測試 `publish.test.ts`「換圖時上傳新檔名、清單指向新檔、舊檔沒被刪」「內容圖上傳失敗時清單位元組不變」「同一張圖再匯入不上傳、清單不變」「寫出的清單鍵是排序過的」，再實作。
- [ ] 3.2 [after: 3.1] Upload credentials：從 repo 根目錄 `.env.r2` 讀 `R2_ENDPOINT`、`R2_ACCESS_KEY_ID`、`R2_SECRET_ACCESS_KEY`，缺欄位時錯誤訊息寫出欄位名、不印出任何值；實作 R2 版的儲存體（S3 相容，`ebo-content` 上傳帶 `Content-Type: image/webp` 與 `Cache-Control: public, max-age=31536000, immutable`，`ebo-originals` 存原檔）。驗證：先寫失敗測試 `credentials.test.ts`「缺 R2_SECRET_ACCESS_KEY 時錯誤訊息含欄位名且不含其他欄位的值」，再實作；R2 儲存體的上傳參數用假的 S3 client 測「帶了正確的 Content-Type 與 Cache-Control」。
- [ ] 3.3 [after: 2.2, 2.3, 3.2] `import <類別> <鍵> <來源圖> [--head-y <n>] [--dry-run]` 指令：串起轉檔、預覽圖（`.asset-preview/<類別>/<鍵>.png`，棋盤底＋輔助線：立繪頂線 y=80、腳底線 y=1180、中線 x=400；怪物腳底線；圖示 88% 框）、上傳與清單；`--dry-run` 不需要金鑰、不上傳、不改清單；任何錯誤非零 exit code 且清單不變。驗證：先寫失敗測試 `cli.test.ts`「沒有 .env.r2 時 --dry-run 產生預覽並以 0 結束、清單不變」「非 dry-run 缺金鑰時非零結束」，再實作。

## 4. 其他指令

- [ ] 4.1 [after: 3.1] Missing image report：`missing [<類別>]` 從 `backend/content` 推出各類應有的鍵（立繪＝職業 × male／female、職業圖示、技能、道具、怪物、地點），跟清單比較，印出每類「有圖 n／應有 m」與缺圖的鍵，exit 0。驗證：先寫失敗測試 `missing.test.ts`「清單有 8 張立繪、內容有 12 個職業時顯示 8／24 並列出 16 個鍵」（用測試用的內容資料夾），再實作；之後對真實內容跑 `pnpm missing` 看得到六類統計。
- [ ] 4.2 [after: 1.1] Design asset import：`design <分類> <名稱> <來源圖> [--max-width <n>]` 寫出 `frontend/src/assets/<分類>/<名稱>.webp`（品質 85、保留透明、不裁切、比來源窄時等比縮小），超過 307200 bytes 就拒絕且不寫檔。驗證：先寫失敗測試 `design.test.ts`「1200 寬的來源加 --max-width 600 輸出 600 寬」「超過上限時不寫檔並非零結束」，再實作。

## 5. 遊戲前端

- [ ] 5.1 [after: 3.1] Frontend content image lookup（依「清單內含網址前綴」「佔位圖是前端內建的設計素材」）：新增 `frontend/src/assets/content-manifest.json`（`baseUrl` 為 `https://ebo-assets.blazer030.dev`、`assets` 為空物件）、六張灰階剪影佔位圖 `frontend/src/assets/placeholders/<類別>.svg`、`frontend/src/render/contentAssets.ts` 的 `contentAsset(category, key)`。驗證：先寫失敗測試 `contentAssets.test.ts`「清單有 monsters/monster-1 時回傳完整網址且不是佔位圖」「沒有 items/item-9999 時回傳道具佔位圖並標記為佔位圖」，以及清單格式檢查「類別不合法、file 跟鍵對不上、baseUrl 不是 https 時失敗」（用測試資料驗證檢查本身，再對正式清單跑一次），再實作。
- [ ] 5.2 [after: 5.1] 舞台改用 Frontend content image lookup：怪物與寵物用 `monsters/<怪物 id>`，缺圖或載入失敗時顯示 monsters 佔位圖，名字照舊顯示在血條；玩家不再找 `player` 圖、維持剪影。驗證：先寫失敗測試（把「依怪物 id 決定要載入的網址、失敗時改用佔位圖」抽成可測的純函式並測兩種情況），再接到 `StageRenderer`；手動在 `pnpm dev`（5175）開冒險頁，確認怪物位置顯示佔位圖、血條有名字、console 沒有錯誤。
- [ ] 5.3 [after: 5.2] 移除舊管線（依「內容圖放 R2、設計素材放 git」）：刪除 `frontend/src/render/artIndex.ts`、`artIndex.test.ts`、`artAssets.test.ts`、`frontend/src/test/artRules.ts`、`frontend/src/assets/art/`、`scripts/import-art.sh`；行為不變（舊管線沒有圖），單獨以結構性變更 commit。驗證：刪除前後 `cd frontend && pnpm check` 都 exit 0；`git grep -n "resolveArt\|artRules\|import-art"` 只剩 docs/superpowers 與封存資料夾。

## 6. 文件與規則

- [ ] 6.1 [after: 5.3] 改寫規則與文件：專案 `CLAUDE.md` 硬規則 6（內容圖放 R2、清單在 git）、前端硬規則 8（只透過查找函式取內容圖、缺圖用佔位圖、設計素材放 `src/assets/<分類>/`）、「結構」加 `tools/assets`、「指令」換掉 `import-art.sh` 改列 `import`／`missing`／`design`；`docs/art/README.md` 改寫成新規格表、匯入流程、`.env.r2` 設定、佔位圖 SVG 不受色碼規則約束；`docs/art/prompts.md` 改指向新流程；裁定彙整「（四）明確不做」那一條註明已由本變更推翻；延後清單刪除 `import-art.sh` 寫到一半、`artAssets.test.ts` 沒有真正輸入、VP8 位元遮罩三條。驗證：逐檔內容檢查；`git grep -n "import-art\|assets/art"` 在 CLAUDE.md 與 docs/art 沒有結果。

## 7. 第一批匯入與總檢查

- [ ] 7.1 [after: 3.3, 4.1, 5.2] 第一批正式匯入（使用者先依 `.env.r2.example` 建好 `.env.r2`）：從 `.design-work/assets` 匯入四職男女立繪（`job-6`～`job-9` 的 `-male`／`-female`）與 21 張戰鬥背景，逐張看預覽圖、必要時用 `--head-y`；`pnpm missing` 顯示立繪 8／24、戰鬥背景 21／21；用瀏覽器打開清單裡任一網址看得到圖；核對實際檔案大小與 design 規格表的實測範圍，差太多就回報。驗證：上述手動檢查，清單 commit。
- [ ] 7.2 [after: 6.1, 7.1] `./scripts/check.sh` exit code 0。
