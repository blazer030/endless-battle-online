## Why

設計稿（畫布 HomeJ＝EBO DEMO）第一版完成，接下來要把畫面套進遊戲前端，會用到大量圖片：道具、怪物、技能、職業立繪與圖示、戰鬥背景。現在只有「舞台的玩家、怪物、寵物圖放 `frontend/src/assets/art/`」一套舊規格，圖放進 git 會讓 repo 越換越大，立繪的尺寸與人物位置也各不相同（設計稿每張都要手調位移）。開始做畫面前，先定好圖放哪裡、怎麼匯入、遊戲怎麼找圖、缺圖怎麼顯示。

## What Changes

- 新增「內容圖」管線：道具、怪物（含寵物）、技能、職業立繪、職業圖示、戰鬥背景的圖放 Cloudflare R2 bucket `ebo-content`（公開讀取，網址 `https://ebo-assets.blazer030.dev`），原檔放私人 bucket `ebo-originals`。檔名帶內容雜湊、永不覆寫（bucket 已設永久鎖定）；git 只存清單 `frontend/src/assets/content-manifest.json`（鍵 → 檔名），跟著 release／hotfix 走。
- 新增匯入工具（獨立的 pnpm 專案 `tools/assets`）：把來源圖依類別轉成固定規格的 WebP（立繪在匯入時就對齊到固定畫布），先上傳再更新清單，並產生加了輔助線的預覽圖；另有「列出缺圖」與「匯入設計素材」兩個指令。上傳金鑰只放在 git 忽略的 `.env.r2`。
- 遊戲前端改用清單找內容圖；清單沒有的鍵一律顯示該類的通用佔位圖，不會出現破圖或 404。
- 設計素材（UI 元件、場景、NPC、佔位圖）放 `frontend/src/assets/<分類>/`，進 git、由 Vite 打包。
- **BREAKING（開發流程）**：移除舊的 `frontend/src/assets/art/`、`frontend/src/render/artIndex.ts`、`frontend/src/test/artRules.ts`、`frontend/src/render/artAssets.test.ts`、`scripts/import-art.sh`；`docs/art/README.md` 改寫成新流程。舊管線目前沒有任何圖，移除不影響玩家。
- 調整舊規則：推翻裁定彙整（docs/superpowers/specs/2026-10-22-wog-remake-rulings.md）「（四）明確不做」的「地區背景圖、道具圖示、依職業換玩家圖：程式沒有使用處，美術規格只涵蓋舞台現有的玩家、怪物、寵物圖」，改為涵蓋上述所有內容圖類別。
- 改專案 CLAUDE.md：專案硬規則 6 補上「內容圖放 R2、清單在 git」；前端硬規則 8 改成新的圖片規則；結構與指令段落加上 `tools/assets` 與新指令。
- 刪除延後清單（docs/superpowers/specs/2026-10-23-deferred-backlog.md）中只跟舊管線有關的四條（`import-art.sh` 寫到一半的 webp、ImageMagick 匯入流程沒有實跑過、`artAssets.test.ts` 沒有真正輸入、VP8 位元遮罩沒有釘住）。

## Non-Goals (optional)

見 design.md 的 Goals / Non-Goals。

## Capabilities

### New Capabilities

- `content-assets`: 內容圖的類別與規格、立繪對齊、雜湊檔名與不可覆寫的上傳、清單格式、遊戲前端找圖與佔位圖、缺圖清單、設計素材匯入。

### Modified Capabilities

（無）

## Impact

- frontend：`src/assets/`（新增清單、佔位圖、設計素材分類資料夾；移除 `art/`）、`src/render/`（舞台改用清單找圖；移除 `artIndex.ts`、`artAssets.test.ts`）、`src/test/artRules.ts`（移除）、新增內容圖查找模組與測試。
- 新增 `tools/assets`（TypeScript、sharp、S3 相容的上傳套件、vitest）與 `.env.r2.example`；`.gitignore` 加 `.env.r2` 與預覽輸出資料夾。
- scripts：`check.sh` 加跑 `tools/assets` 的 `pnpm check`；移除 `import-art.sh`。
- 文件：專案 `CLAUDE.md`、`docs/art/README.md`、`docs/art/prompts.md`、裁定彙整、延後清單。
- 外部服務：Cloudflare R2（`ebo-content`、`ebo-originals`）與網域 `blazer030.dev`，已由使用者設定好（CORS、永久鎖定、API token）。
- 不碰 backend、admin、db。
