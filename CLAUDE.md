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
- `backend/Dockerfile`、`frontend/Dockerfile`、`frontend/nginx.conf` — `docker compose --profile app` 用的映像。
- `frontend` — Vite + React 19 + TypeScript + PixiJS 8 + GSAP 3，pnpm 管理。
  - `src/api` — API client；`schema.ts` 由 `pnpm generate:api` 產生。
  - `src/game` — 遊戲狀態與播放模型，純 TypeScript。
  - `src/motion/timings.ts` — 動畫時長、輪詢間隔等常數。
  - `src/render` — Pixi 舞台與用色（`palette.ts`）。
  - `src/ui` — React 畫面與 `theme.css`。
  - `src/test/setup.ts` — Vitest＋Testing Library 設定。
- `admin` — 內容後台，Vite + React + TypeScript + Refine + Ant Design，pnpm 管理；獨立專案，不與 `frontend` 共用套件。

## 硬規則

1. 遊戲規則只寫在 Domain；Api 不做遊戲計算。
2. Domain 沒有任何 PackageReference 或 ProjectReference；Application 只引用 Domain 與 `Microsoft.Extensions.DependencyInjection.Abstractions`。
3. 資料表結構只由 Flyway migration 決定，不使用 EF Core Migrations。
   - 檔名 `V<9 位數>__<小寫英文與底線>.sql`，ddl 與 dml 共用一條版本號序列。
   - 已 commit 的 migration 不得修改，要改就新增版本。
   - 不得含 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE`（`scripts/check-sql.sh` 會擋）。
4. `EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs` 與 `Persistence/Models/*.cs` 由 `scripts/scaffold-db.sh` 產生，**不得手改**；擴充寫在 `*.Partial.cs`。
5. scaffold 產生的 model 只在 Infrastructure 使用，Application 與 Domain 只看見 Domain 型別。
6. 遊戲內容只放 `backend/content/*.json`，資料庫只存玩家狀態；測試用 `EndlessBattle.Tests/TestContent/`。
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
8. 美術圖放 `src/assets/art/<內容 id>.webp`，以 `import.meta.glob` 建索引；沒有圖時顯示橫排名牌。規格（畫布、大小上限、構圖）、提示詞範本與匯入流程見 `docs/art/README.md`。
9. `src/api/schema.ts` 是產生檔，不得手改。

### admin

1. 遊戲 API 走 `/api/...`，admin API 一律走 `/admin/...`；admin endpoint 只放 `backend/EndlessBattle.Api/Endpoints/Admin/`，全部掛在同一個 `/admin` 群組下，遊戲 endpoint 不得放進這個群組。
2. admin 路由只在設定 `Admin:Enabled` 為 `true` 時註冊；`appsettings.json` 預設 `false`，只有 `appsettings.Development.json` 打開。
3. OpenAPI 分兩份：遊戲用 `v1`（不含 `/admin/` 路徑），admin 用 `admin`。
4. admin 的計算（例如平衡報告）寫在 Application 與 Domain，admin endpoint 只負責呼叫。
5. admin 改內容只寫草稿（`Admin:DraftDirectory`）；只有「發布」會寫入正式內容 `backend/content/`。
6. `admin/src/api/schema.ts` 是產生檔，不得手改。

## 指令

- `./scripts/check.sh` — SQL 檢查、compose 設定驗證（需要 `jq`）、後端 build（警告視為錯誤）與測試、前端與 admin 的 `pnpm check`。提交前必須通過，以 exit code 判斷。
- `docker compose up -d` — 啟動 MySQL 8.4 並用 Flyway 套用 migration（開發用，只有這兩個服務）。第一次先 `cp .env.example .env`：compose 沒有 `EBO_JWT_SIGNING_KEY` 會拒絕啟動，不會退回公開的預設值。
- `docker compose --profile app up -d --build` — 再加上 `api` 與 `web`（nginx，`http://localhost:8080`），一鍵執行整個遊戲；設定見 `.env.example`。
- `./scripts/smoke-compose.sh` — 用獨立專案名與埠起完整 stack，經 nginx 打 `/api/health`、`/`、`/api/content`，結束一律拆掉；不在 `check.sh` 裡，改 Dockerfile、nginx 或 compose 時手動跑。
- `./scripts/scaffold-db.sh` — 從資料庫重新產生 EF Core model。
- `./scripts/clean-test-containers.sh` — 刪除測試殘留的 MySQL 容器（只刪 label `ebo.tests=true` 的）。
- `./scripts/import-art.sh <來源圖> <id>` — 把生成的圖裁切、補成規格畫布並轉成 `frontend/src/assets/art/<id>.webp`（需要 ImageMagick 7 的 `magick`；`id` 是 `player` 或怪物 id），流程見 `docs/art/README.md`。
- `cd backend && dotnet run --project EndlessBattle.Api` — 啟動 API。
- `cd backend && dotnet test --filter-class "*SeededRandomTests"` — 只跑特定測試類別（xUnit v3 on Microsoft.Testing.Platform）。
- `cd frontend && pnpm dev` — 啟動前端（`/api`、`/openapi` proxy 到 `http://localhost:5062`；`host: true`，同一個 Wi-Fi 的手機可連）。
- `cd frontend && pnpm check` — eslint、prettier 檢查、`tsc -b`、vitest。
- `cd frontend && pnpm generate:api` — API 執行中時，從 `/openapi/v1.json` 重新產生 `src/api/schema.ts`。
- `cd admin && pnpm dev` — 啟動內容後台（`http://localhost:5174`；`/admin`、`/openapi` proxy 到 `http://localhost:5062`，API 要以 Development 執行才有 admin 路由）。
- `cd admin && pnpm check` — admin 的 eslint、prettier 檢查、`tsc -b`、vitest。
- `cd admin && pnpm generate:api` — API 執行中時，從 `/openapi/admin.json` 重新產生 `admin/src/api/schema.ts`。

需要資料庫的測試依賴 Docker 執行中；卡在 MySQL 啟動時先跑 `scripts/clean-test-containers.sh`。

## 資料庫變更流程

1. 在 `backend/db/ddl`（或 `dml`）新增下一個版本的 migration。
2. `docker compose up -d`（flyway 服務會套用新 migration）。
3. `./scripts/scaffold-db.sh` 重新產生 model。
4. 寫用到它的測試與程式。

## 開發流程

- 照最上方的 Spectra 區塊；與其他流程 skill（superpowers 的 brainstorming、writing-plans、executing-plans、subagent-driven-development、requesting-code-review、systematic-debugging、finishing-a-development-branch）重疊時用 Spectra 的。
- 設計畫布的工作（`ebo-canvas` skill）不走 Spectra。
- `SPECTRA:START`～`SPECTRA:END` 之間由 Spectra 產生、會被覆寫，專案規則寫在區塊外。

## 慣例

- 程式碼註解用中文，非必要不寫。
- 命名用完整字詞，不縮寫。
