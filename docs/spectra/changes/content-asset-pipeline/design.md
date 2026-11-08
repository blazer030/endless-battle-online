## Context

- 設計稿 HomeJ 用到約 278 張圖（原檔約 70 MB，存在畫布與 `.design-work/assets/`，不進 git）。正式前端的 `frontend/src/assets/art/` 目前是空的；舊管線（`artIndex.ts` 用 `import.meta.glob` 依檔名找圖、`artRules.ts` 規定玩家 600×1020／怪物 720×1000／150 KiB、`scripts/import-art.sh` 需要 ImageMagick）只涵蓋舞台的玩家、怪物、寵物。
- 內容量（2026-11-08 量測 `backend/content`）：道具 3127 個（同名合併後 1449、再去掉 LVn 1176）、怪物 461、技能 76、職業 12、地點 21。
- 2026-11-08 試轉（scratchpad，Pillow，WebP method 6）的實測結果決定了下面的規格表；四職男女立繪原圖尺寸各不相同（398～662 寬、987～1004 高），設計稿每張都手調 `fx` 位移。
- 使用者已建好 Cloudflare R2：`ebo-content`（自訂網域 `ebo-assets.blazer030.dev`、CORS 允許 `http://localhost:5173`、`http://localhost:5175`、`http://192.168.2.216:8080`、`https://ebo.blazer030.dev` 的 GET／HEAD、無前綴永久鎖定）、`ebo-originals`（私人）、一組只限這兩個 bucket 的 Object Read & Write token。實測自訂網域與 CORS 預檢都正常；Cloudflare 會把 404 快取 4 小時。
- 硬規則：不涉及資料表、scaffold 產生檔、schema.ts、admin 路由。涉及專案硬規則 6（遊戲內容的位置）與前端硬規則 8（美術圖），兩條都在本變更改寫，見 Decisions。

## Goals / Non-Goals

**Goals:**

- 內容圖不進 git、可被協作者與所有環境讀到，且已發布的版本永遠指得到當時的圖。
- 匯入一張圖只需一個指令：輸出就是規格內、已對齊的 WebP，程式不需要逐張調位置。
- 遊戲任何地方找內容圖都走同一個查找函式，缺圖時顯示該類佔位圖。
- 設計素材有固定的放置位置與匯入指令，供之後的畫面變更使用。

**Non-Goals:**

- `items.json` 的 `icon` 欄位與同名道具共用圖：另開後續變更 `item-icons`（要動 Domain、內容載入、遊戲 API、admin 表單與資料），排在背包頁之前。本變更的道具鍵先用道具 id。
- 配色與字型（前端硬規則 3、4）：隨第一個畫面變更（外殼與城鎮）一起改。
- 把設計稿的素材搬進 `frontend/src/assets/`：各畫面變更需要時才匯入。
- admin 上傳圖、內容進資料庫、清理沒用到的檔案、R2 備份、Cloudflare Tunnel。
- 改舞台的演出或版面（冒險頁變更才做）。

## Decisions

### 內容圖放 R2、設計素材放 git

**Supersedes**: 裁定彙整（docs/superpowers/specs/2026-10-22-wog-remake-rulings.md）「（四）明確不做」的「地區背景圖、道具圖示、依職業換玩家圖：程式沒有使用處，美術規格只涵蓋舞台現有的玩家、怪物、寵物圖」。

內容圖（數量跟著內容增加、會反覆換）放 R2；設計素材（約 10～15 MB，跟版面一起改、CSS 直接引用）放 `frontend/src/assets/<分類>/` 進 git。考慮過：全部進 git（repo 隨換圖膨脹，使用者不要）、git LFS（GitHub 額度、協作者要裝 LFS）、NAS 資料夾（協作者連不到區網）。R2 免費 10 GB、下載不收流量費，自訂網域走 Cloudflare CDN。

專案硬規則 6 改為：「遊戲內容只放 `backend/content/*.json`；內容圖放 R2，清單在 `frontend/src/assets/content-manifest.json`；資料庫只存玩家狀態」。前端硬規則 8 改為：「內容圖只透過內容圖查找函式取得，缺圖顯示該類佔位圖；設計素材放 `src/assets/<分類>/`；規格與流程見 `docs/art/README.md`」。

### 檔名帶雜湊、永不覆寫，清單在 git

R2 物件鍵是 `<類別>/<鍵>.<雜湊>.webp`，雜湊是輸出 WebP 位元組 SHA-256 的前 12 個十六進位字元。換圖＝上傳新檔＋清單改指向新檔；舊檔留著，舊版本仍指得到。bucket 的永久鎖定讓「不覆寫、不刪除」由 Cloudflare 強制。考慮過固定檔名＋覆寫（舊版本壞掉、快取難失效）。

### 先上傳、再寫清單

清單只在上傳成功後才更新；上傳失敗時清單不變。因為 Cloudflare 會快取 404，網址不能在檔案存在之前出現在任何地方。

### 清單內含網址前綴

清單的 `baseUrl` 是 `https://ebo-assets.blazer030.dev`，所有環境共用同一個 bucket，所以不用環境變數，前端 Dockerfile 與 compose 不用改。考慮過 `VITE_` 環境變數（多一個要在每個環境設定的值，沒有對應的需求）。

### 匯入工具是獨立的 pnpm 專案 `tools/assets`

TypeScript＋sharp（轉檔與像素運算）＋`@aws-sdk/client-s3`（R2 是 S3 相容）＋vitest，跟 admin 一樣獨立專案、不共用套件；`check.sh` 加跑它的 `pnpm check`。考慮過：Python（repo 沒有 Python 環境）、.NET 主控台（影像套件要另外引入，跟前端資料格式分兩種語言）、放進 frontend（sharp 等重套件會拖慢前端 Docker 建置）、rclone（要另外安裝，也要另外寫轉檔）。上傳端用介面注入，測試用假的儲存體。

### 規格依試轉結果

| 類別 | 鍵 | 畫布 | 構圖 | 透明 | 品質 | 上限 |
|---|---|---|---|---|---|---|
| `portraits` 職業立繪 | `<職業 id>-male`、`<職業 id>-female`（對應角色的 `Sex`：1＝男 → male、2＝女 → female，建角時必選） | 800×1200 | 人物高 1100、腳底 y=1180、身體中線 x=400 | 是 | 80 | 160 KB |
| `job-icons` 職業圖示 | `<職業 id>` | 96×96 | 圖案長邊 88% 置中 | 是 | 85 | 12 KB |
| `skills` 技能圖示 | `<技能 id>` | 96×96 | 圖案長邊 88% 置中 | 是 | 85 | 12 KB |
| `items` 道具圖示 | `<道具 id>`（之後改圖示鍵） | 160×160 | 圖案長邊 88% 置中 | 是 | 85 | 20 KB |
| `monsters` 怪物與寵物 | `<怪物 id>` | 540×540 | 長邊縮到高 486／寬 513 以內、水平置中、腳底 y=529 | 是 | 80 | 60 KB |
| `battlefields` 戰鬥背景 | `<地點 id>`（world.json places 的數字 id） | 1024×1024 | 等比縮放後置中裁切填滿 | 否 | 75 | 150 KB |

KB＝1024 bytes。試轉實測（q 為上表品質）：立繪 68～123 KB、道具 2～7 KB、技能與職業圖示 2～5 KB、怪物 360 寬時 11～27 KB（540 的實測 19～46 KB 都是原圖放大）、戰鬥背景 42～109 KB。全部內容估約 25 MB（估計值：張數 × 試轉平均；第一批正式匯入後以 `missing` 指令的統計核對）。

超過上限時品質每次降 5 重試，降到 60 仍超過就拒絕。原圖比畫布需要的小（需要放大）時照樣輸出，但印出警告；怪物原圖建議至少 600 高。「不透明像素」指 alpha > 24。

### 立繪在匯入時對齊

身體中線＝人物高度 35%～95% 區間內，每一列不透明像素中點的中位數（武器、披風不易拉歪）。人物高度預設是不透明外框的頂到底；法杖、帽子把頂端拉高時，用 `--head-y <原圖 y>` 指定頭頂，指定值記在清單，重新匯入結果一致。縮放後有任何不透明像素落在畫布外就拒絕並說明超出哪一邊。試轉時八張都在畫布內。

### 佔位圖是前端內建的設計素材

每個類別一張 `frontend/src/assets/placeholders/<類別>.svg`（灰階剪影），由 Vite 打包，離線也能顯示。SVG 是美術檔，不是程式碼，不受前端硬規則 3（色碼只在 theme.css）約束；這點寫進 `docs/art/README.md`。

## Implementation Contract

**匯入工具指令**（在 `tools/assets` 以 `pnpm cli <指令>` 執行——`pnpm import` 是 pnpm 內建指令，不能直接當腳本名；路徑參數相對於下指令的位置）：

- `import <類別> <鍵> <來源圖> [--head-y <n>] [--dry-run]`
  - 依規格表輸出 WebP、寫預覽圖 `.asset-preview/<類別>/<鍵>.png`（棋盤底＋輔助線：立繪畫頂線 y=80、腳底線 y=1180、中線 x=400；怪物畫腳底線；圖示畫 88% 框），印出尺寸、品質、位元組數與警告。
  - 非 `--dry-run`：上傳原檔到 `ebo-originals` 的 `<類別>/<鍵>.<原檔雜湊>.<原副檔名>`、上傳 WebP 到 `ebo-content` 的 `<類別>/<鍵>.<雜湊>.webp`（`Content-Type: image/webp`、`Cache-Control: public, max-age=31536000, immutable`）；物件已存在就略過上傳；兩者都成功後才更新清單。
  - 錯誤：未知類別、鍵不符合 `^[a-z0-9-]+$`、鍵不在 `backend/content` 推出的應有鍵裡（R2 鎖定後刪不掉，所以上傳前擋下；`--dry-run` 只印提醒、照常結束）、來源讀不到、規格不合（超出畫布、降到 q60 仍超過上限）、缺 `.env.r2`（`--dry-run` 不需要）、上傳失敗 → 非零 exit code 與一行中文說明；清單不變。
- `missing [<類別>]`：讀 `backend/content/*.json` 列出每類應有的鍵，跟清單比較，印出每類「有圖 n／應有 m」、缺圖的鍵，以及清單有、內容沒有的鍵（「多：」）；`portraits` 以 jobs × {male, female} 計算、`monsters` 以 monsters.json 計算。exit code 0。
- `design <分類> <名稱> <來源圖> [--max-width <n>]`：輸出 `frontend/src/assets/<分類>/<名稱>.webp`（品質 85、保留透明、不裁切），超過 300 KB 拒絕；分類與名稱都要符合 `^[a-z0-9-]+$`；`--max-width` 要是 1 以上的整數。

**金鑰**：repo 根目錄 `.env.r2`（git 忽略），欄位 `R2_ENDPOINT`、`R2_ACCESS_KEY_ID`、`R2_SECRET_ACCESS_KEY`；範本 `.env.r2.example` 只有欄位名。工具不印出金鑰。

**清單格式** `frontend/src/assets/content-manifest.json`（鍵依字母排序，方便看 diff）：

```json
{
  "baseUrl": "https://ebo-assets.blazer030.dev",
  "assets": {
    "monsters/monster-1": { "file": "monsters/monster-1.3fa2c1d04e5b.webp", "original": "monsters/monster-1.9a8b7c6d5e4f.png" },
    "portraits/job-6-male": { "file": "portraits/job-6-male.0c1d2e3f4a5b.webp", "original": "portraits/job-6-male.1a2b3c4d5e6f.png", "headY": 40 }
  }
}
```

**前端查找**（`frontend/src/render/contentAssets.ts`）：`contentAsset(category, key)` 回傳 `{ url: string; placeholder: boolean }`；清單有就回 `baseUrl + "/" + file`、`placeholder: false`，沒有就回該類佔位圖、`placeholder: true`。類別型別是六個類別的字串聯集。舞台：怪物與寵物用 `monsters/<怪物 id>`，缺圖時顯示佔位圖（怪物的名字照舊在血條上；寵物沒有血條，名牌保留）；玩家不再找 `player` 圖，維持現有的剪影繪製。圖載入失敗時退回佔位圖。

**驗收**：

- `tools/assets` 的 vitest：各類輸出尺寸、透明、腳底與中線位置（用合成測試圖）、超出畫布拒絕、品質遞減與拒絕、已存在略過上傳、上傳失敗時清單不變、清單排序、`missing` 統計。
- `frontend` 的 vitest：查找函式的有圖／缺圖；清單格式檢查（每個鍵的類別合法、`file` 符合 `<類別>/<鍵>.<12 位十六進位>.webp`、`baseUrl` 是 https）；舞台在缺圖時使用佔位圖。
- `./scripts/check.sh` 通過（含 `tools/assets` 的 `pnpm check`）。
- 手動（不寫自動測試）：舞台上怪物名字留在血條、寵物名牌一直顯示，屬於 Pixi 畫面呈現，依「樣式不寫測試」只做畫面確認；載入哪些網址與失敗時退回佔位圖由 `artCandidates`、`loadFirst` 的測試涵蓋。2026-11-09 用無頭 Chrome 確認過（寵物以攔截 API 回應的方式放入）。
- 手動：使用者填好 `.env.r2` 後，匯入四職男女立繪與 21 張戰鬥背景，確認預覽圖、`missing` 統計，並用瀏覽器打開清單裡任一網址看得到圖。

**範圍**：在內——上述工具、清單、查找、佔位圖、舞台改用查找、移除舊管線、文件與規則。在外——見 Non-Goals。

## Risks / Trade-offs

- [身體中線的自動判斷對非人形或大披風的圖不準] → 預覽圖必看；需要時之後加 `--center-x` 覆寫（本變更不做，試轉八張都正常）。
- [`.env.r2` 外洩] → token 只有兩個 bucket 的物件讀寫，`ebo-content` 有永久鎖定不能被覆寫或刪除；外洩時在 Cloudflare 撤銷重建。
- [離線開發看不到內容圖] → 顯示佔位圖，功能與測試不受影響。
- [協作者沒有金鑰] → 只有要上傳的人需要；讀圖不需要。
- [404 被快取 4 小時] → 先上傳再寫清單，網址不會在檔案存在前出現。
- [sharp 與 Pillow 編碼結果略有差異，規格表的實測數字可能偏移] → 上限留了餘裕；第一批正式匯入時核對。

## Migration Plan

1. 實作完成、check 通過後 commit。
2. 使用者照 `.env.r2.example` 建 `.env.r2`。
3. 匯入第一批（四職男女立繪、21 張戰鬥背景），commit 清單。
4. 回退：清單是普通檔案，`git revert` 即可；R2 上的檔案不需要清。
