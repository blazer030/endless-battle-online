<!-- SPECTRA:START v1.3.0 -->

# Spectra Instructions

This project uses Spectra for Spec-Driven Development(SDD). Specs live in `docs/spectra/specs/`, change proposals in `docs/spectra/changes/`.

## Skills

Each `/spectra-*` skill carries its own trigger description; these are the groups:

- Shape and plan → `/spectra-discuss`, `/spectra-propose`
- Continue tasks for an identified change → `/spectra-apply`
- Update requirements or plans for an identified change → `/spectra-ingest`
- Quality gate → `/spectra-verify`, `/spectra-review`, `/spectra-analyze`, `/spectra-audit`, `/spectra-drift`, `/spectra-debug`
- Finish → `/spectra-archive`, `/spectra-commit`

Explicit skill invocation takes precedence. Apply existing authorization within its unchanged scope.

## Workflow

discuss? → propose → apply ⇄ ingest → verify / review → archive

- `discuss` is optional — skip if requirements are clear
- Requirements change mid-work? Plan mode → `ingest` → resume `apply`

## Parked Changes

Changes can be parked（暫存）— temporarily moved out of `docs/spectra/changes/`. Parked changes won't appear in `spectra list` but can be found with `spectra list --parked`. To restore: `spectra unpark <name>`. The `/spectra-apply` and `/spectra-ingest` skills disclose parking and restore when the named operation is already explicitly requested; respect a known refusal, otherwise ask for missing authorization.

<!-- SPECTRA:END -->

# Endless Battle Online（EBO）

現代奇幻風的單人放置 RPG，WOG 4.5 的單機翻新版。後端是 .NET 10 Web API，前端是 React + PixiJS（計畫 2 開始）。

- 規格與變更：`docs/spectra/`（主規格在 `specs/`，進行中的變更在 `changes/`）；新功能、規則調整、重構都走 Spectra，見「開發流程」
- 導入 Spectra 前的文件（只當參考，不再新增）：
  - 設計文件：`docs/superpowers/specs/2026-10-21-wog-remake-design.md`（v2）；v1 設計文件見 v2 §0 所指的檔案（技術架構 §6 仍有效，其餘作廢）
  - 裁定彙整：`docs/superpowers/specs/2026-10-22-wog-remake-rulings.md`（各計畫的〔裁定〕與 Claude 的判斷／設計／簡化、明確不做）；要推翻或調整舊規則先查這裡，新的決策寫進變更的 `design.md`
  - 實作計畫：`docs/superpowers/plans/`（計畫 1～11）
- 延後項目：`docs/superpowers/specs/2026-10-23-deferred-backlog.md`（計畫 10 結束時仍未處理的項目，做完就刪掉那一條）
- 研究與內容總表：`docs/research/README.md`；美術規格：`docs/art/README.md`

## 結構

- `backend/EndlessBattle.Domain` — 遊戲規則：戰鬥、冒險、內容定義。純 C#，零依賴。
- `backend/EndlessBattle.Application` — 用例與介面（Repository、時鐘、亂數、遊戲內容）。
- `backend/EndlessBattle.Infrastructure` — EF Core（Database-First）、內容載入、介面實作。
- `backend/EndlessBattle.Api` — Minimal API、JWT、OpenAPI。
  - `Endpoints/Admin/` — admin API（`/admin/...`），見「admin」。
- `backend/EndlessBattle.Tests` — xUnit v3；需要資料庫的測試用 Testcontainers。
- `backend/db/ddl`、`backend/db/dml` — Flyway migration。
- `backend/content` — 遊戲內容 JSON。
- `backend/content-draft` — admin 的內容草稿（git 忽略；發布後刪除）。
- `backend/openapi` — `dotnet build` 產生的 OpenAPI 文件（進 git；`check.sh` 會擋下過期的文件與 `schema.ts`）。
- `backend/Dockerfile`、`frontend/Dockerfile`、`frontend/nginx.conf` — `docker compose --profile app` 用的映像。
- `frontend` — Vite + React 19 + TypeScript + PixiJS 8 + GSAP 3，pnpm 管理。
  - `src/api` — API client；`schema.ts` 由 `pnpm generate:api` 產生。
  - `src/game` — 遊戲狀態與播放模型，純 TypeScript。
  - `src/motion/timings.ts` — 動畫時長、輪詢間隔等常數。
  - `src/render` — Pixi 舞台與用色（`palette.ts`）。
  - `src/ui` — React 畫面與 `theme.css`。
  - `src/test/setup.ts` — Vitest＋Testing Library 設定。
- `admin` — 內容後台，Vite + React + TypeScript + Refine + Ant Design，pnpm 管理；獨立專案，不與 `frontend` 共用套件。
- `tools/assets` — 圖片匯入工具（TypeScript、sharp），pnpm 管理；獨立專案。內容圖轉檔、上傳 R2、維護清單，見 `docs/art/README.md`。

## 硬規則

1. 遊戲規則只寫在 Domain；Api 不做遊戲計算。
2. Domain 沒有任何 PackageReference 或 ProjectReference；Application 只引用 Domain 與 `Microsoft.Extensions.DependencyInjection.Abstractions`。
3. 資料表結構只由 Flyway migration 決定，不使用 EF Core Migrations。
   - 檔名 `V<9 位數>__<小寫英文與底線>.sql`，ddl 與 dml 共用一條版本號序列。
   - 已 commit 的 migration 不得修改，要改就新增版本。
   - migration 要用 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE` 時，每次都要先問使用者這次可不可以用，同意才能用；同意只算當次。
4. `EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs` 與 `Persistence/Models/*.cs` 由 `scripts/scaffold-db.sh` 產生，**不得手改**；擴充寫在 `*.Partial.cs`。
5. scaffold 產生的 model 只在 Infrastructure 使用，Application 與 Domain 只看見 Domain 型別。
6. 遊戲內容只放 `backend/content/*.json`，資料庫只存玩家狀態；測試用 `EndlessBattle.Tests/TestContent/`。內容圖（道具、怪物、技能、職業立繪與圖示、戰鬥背景）放 Cloudflare R2，清單在 `frontend/src/assets/content-manifest.json`，只用 `tools/assets` 的 `import` 指令更新。
7. 時間一律 UTC；資料庫時間欄位用 `DATETIME(6)`。

### 遊戲前端（`frontend`）

以下只適用於遊戲前端 `frontend`，不適用於 `admin`。

1. TypeScript `strict`，禁用 `any`。
2. 手機直式優先：以 375×667 為主要設計尺寸，360～430 寬度不得出現水平捲動；電腦上整個畫面置中、最大寬度 480px。
3. 顏色只定義在 `src/ui/theme.css` 的 CSS 變數，其他地方不得寫死色碼；Pixi 端從 `src/render/palette.ts` 取，數值與 CSS 變數一致（`theme.test.ts` 會比對）。
   - 色彩變數：`--bg-top`、`--bg-bottom`、`--panel`、`--border`、`--text`、`--text-muted`、`--gold`、`--cyan`、`--crimson`、`--crimson-text`（小字用，對比 ≥ 4.5:1）、`--green`、`--night`、`--white`，屬性色塊另有 `--element-earth`、`--element-water`、`--element-poison`（火、木、風沿用 `--crimson`、`--green`、`--cyan`）；字型變數：`--font-ui`、`--font-title`。
4. 字型（Google Fonts）：介面用 Noto Sans TC；標題用 Noto Serif TC 700（中文）與 Cinzel 700（拉丁），妖怪名牌用 Noto Serif TC 700。
5. 遊戲狀態只存在 `src/game/` 的純 TS 物件；React state 與 Pixi 物件都不持有權威狀態。`src/game/` 不得 import React、Pixi、GSAP 或 `src/api/` 以外的瀏覽器 API。
6. 整個 app 只有一個 Pixi `Application`；Pixi `Text` 一律明確指定 `resolution: window.devicePixelRatio`。
7. 動畫時長、輪詢間隔等常數集中在 `src/motion/timings.ts`。
8. 內容圖只透過 `src/render/contentAssets.ts` 的查找函式取得，缺圖或載入失敗時顯示該類佔位圖（`src/assets/placeholders/`）；設計素材（UI、場景、NPC）放 `src/assets/<分類>/`。規格、匯入流程與提示詞見 `docs/art/README.md`。
9. `src/api/schema.ts` 是產生檔，不得手改。

### admin

1. 遊戲 API 走 `/api/...`，admin API 一律走 `/admin/...`；admin endpoint 只放 `backend/EndlessBattle.Api/Endpoints/Admin/`，全部掛在同一個 `/admin` 群組下，遊戲 endpoint 不得放進這個群組。
2. admin 路由只在設定 `Admin:Enabled` 為 `true` 時註冊；`appsettings.json` 預設 `false`，只有 `appsettings.Development.json` 打開。
3. OpenAPI 分兩份：遊戲用 `v1`（不含 `/admin/` 路徑），admin 用 `admin`。
4. admin 的計算（例如平衡報告）寫在 Application 與 Domain，admin endpoint 只負責呼叫。
5. admin 改內容只寫草稿（`Admin:DraftDirectory`）；只有「發布」會寫入正式內容 `backend/content/`。
6. `admin/src/api/schema.ts` 是產生檔，不得手改。

## 指令

- `./scripts/check.sh` — SQL 檢查、compose 設定驗證（需要 `jq`）、API 文件與型別是否最新、後端 build（警告視為錯誤）與測試、前端、admin 與 `tools/assets` 的 `pnpm check`。提交前必須通過，以 exit code 判斷。
- `./scripts/check-api-types-test.sh` — 測試 API 文件與型別的檢查本身；會重複建置，`check.sh` 只在那兩支腳本有改動時才跑。
- `docker compose up -d` — 啟動 MySQL 8.4 並用 Flyway 套用 migration（開發用，只有這兩個服務）。第一次先 `cp .env.example .env`：compose 沒有 `EBO_JWT_SIGNING_KEY` 會拒絕啟動，不會退回公開的預設值。
- `docker compose --profile app up -d --build` — 再加上 `api` 與 `web`（nginx，`http://localhost:8080`），一鍵執行整個遊戲；設定見 `.env.example`。
- `./scripts/smoke-compose.sh` — 用獨立專案名與埠起完整 stack，經 nginx 打 `/api/health`、`/`、`/api/content`，結束一律拆掉；不在 `check.sh` 裡，改 Dockerfile、nginx 或 compose 時手動跑。
- `./scripts/deploy-nas.sh` — 把目前的 commit 部署到 NAS（`blazer@192.168.2.216:/volume1/docker/ebo`，`http://192.168.2.216:8080`）；第一次會在 NAS 上產生 `.env`，存檔不受重新部署影響；要在自己的終端機執行，中途會問 NAS 的 sudo 密碼。只上傳、改用 Container Manager 重新建置專案 `ebo`（路徑 `/docker/ebo/app`）時，執行時加 `< /dev/null`，上傳完會停下並以 exit 1 結束；改了 compose 要按「建置」，重新啟動不會套用。本機 `.env.nas.local`（範本 `.env.nas.local.example`，git 忽略）可設 `NAS_HOST`、`NAS_DIR`（留空用上面的預設），有 `NAS_TUNNEL_TOKEN` 時，腳本會把它寫進 NAS `.env` 的 `TUNNEL_TOKEN` 並一起啟動 Cloudflare Tunnel（`https://ebo.blazer030.dev`，Dashboard 管理、路由 `http://web:80`），nginx 對所有回應加 `X-Robots-Tag: noindex`。
- `./scripts/scaffold-db.sh` — 從資料庫重新產生 EF Core model。
- `./scripts/clean-test-containers.sh` — 刪除測試殘留的 MySQL 容器（只刪 label `ebo.tests=true` 的）。
- `cd tools/assets && pnpm cli import <類別> <鍵> <來源圖> [--head-y <n>] [--dry-run]` — 內容圖轉成規格 WebP、產生預覽圖（`.asset-preview/`）、上傳 R2 並更新清單；上傳需要 repo 根目錄的 `.env.r2`（範本 `.env.r2.example`），`--dry-run` 不需要。
- `cd tools/assets && pnpm cli missing [<類別>]` — 列出每類有幾張圖、缺哪些。
- `cd tools/assets && pnpm cli design <分類> <名稱> <來源圖> [--max-width <n>]` — 設計素材轉成 `frontend/src/assets/<分類>/<名稱>.webp`。
- `cd tools/assets && pnpm check` — 匯入工具的 eslint、prettier 檢查、`tsc -b`、vitest。
- `cd backend && dotnet run --project EndlessBattle.Api` — 啟動 API。
- `cd backend && dotnet test --filter-class "*SeededRandomTests"` — 只跑特定測試類別（xUnit v3 on Microsoft.Testing.Platform）。
- `cd frontend && pnpm dev` — 啟動前端（`/api`、`/openapi` proxy 到 `http://localhost:5062`；`host: true`，同一個 Wi-Fi 的手機可連）。
- `cd frontend && pnpm check` — eslint、prettier 檢查、`tsc -b`、vitest。
- `cd frontend && pnpm generate:api` — 從 `backend/openapi/EndlessBattle.Api.json`（`dotnet build` 時產生）重新產生 `src/api/schema.ts`，不用啟動 API。
- `cd admin && pnpm dev` — 啟動內容後台（`http://localhost:5174`；`/admin`、`/openapi` proxy 到 `http://localhost:5062`，API 要以 Development 執行才有 admin 路由）。
- `cd admin && pnpm check` — admin 的 eslint、prettier 檢查、`tsc -b`、vitest。
- `cd admin && pnpm generate:api` — 從 `backend/openapi/EndlessBattle.Api_admin.json` 重新產生 `admin/src/api/schema.ts`，不用啟動 API。

需要資料庫的測試依賴 Docker 執行中；卡在 MySQL 啟動時先跑 `scripts/clean-test-containers.sh`。

## 資料庫變更流程

1. 在 `backend/db/ddl`（或 `dml`）新增下一個版本的 migration。
2. `docker compose up -d`（flyway 服務會套用新 migration）。
3. `./scripts/scaffold-db.sh` 重新產生 model。
4. 寫用到它的測試與程式。

## 發版

用 git flow：平常在 `develop` 開發，發版時開 `release/<版本>`。

1. `git flow release start <版本>`（版本不加 `v`，例如 `0.2.0`）。
2. 在 release 分支把 `frontend/package.json` 與 `admin/package.json` 的 `version` 改成同一個版本，`check.sh` 通過後 commit。
3. `GIT_MERGE_AUTOEDIT=no git flow release finish -m "<版本>" <版本>`（併進 `main`、打 tag、併回 `develop`）。
4. `git push origin develop main <版本>`。

## 開發流程

- 照最上方的 Spectra 區塊；與其他流程 skill（superpowers 的 brainstorming、writing-plans、executing-plans、subagent-driven-development、requesting-code-review、systematic-debugging、finishing-a-development-branch）重疊時用 Spectra 的。
- 設計畫布的工作（`ebo-canvas` skill）不走 Spectra。
- `SPECTRA:START`～`SPECTRA:END` 之間由 Spectra 產生、會被覆寫，專案規則寫在區塊外。

## 慣例

- 程式碼註解用中文，非必要不寫。
- 命名用完整字詞，不縮寫。
