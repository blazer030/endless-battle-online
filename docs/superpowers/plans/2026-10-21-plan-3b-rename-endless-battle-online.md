# 計畫 3b：改名為 Endless Battle Online Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 遊戲改名為 **Endless Battle Online（EBO）**，不再使用「墨渡／Inkbound」。這是純結構性變更：行為不變，所有測試在改名前後都綠。

**Architecture:** 程式碼的根名稱統一為 `EndlessBattle`（.NET 命名空間與專案）；資料庫與設定用 `endless_battle`／`ebo`；前端顯示名稱 `Endless Battle Online`，縮寫 `EBO`。使用者決定：全部改、只用英文。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §0（名稱待定）→ 本計畫定案。

## Global Constraints

- 結構性變更：**不得改任何行為或文案以外的程式邏輯**；測試只改名稱與字串斷言。
- 歷史文件（`docs/superpowers/specs/*`、`docs/superpowers/plans/*` 既有檔、`docs/research/*`）**不改**，它們記錄當時的名稱；只在 spec v2 §0 加一行「2026-10-21 定名 Endless Battle Online」。
- `CLAUDE.md`、`README`（若有）、`scripts/*`、`docker-compose.yml`、設定檔、程式碼、測試全部改。
- 名稱對照表（一律照此）：
  | 舊 | 新 |
  |---|---|
  | `Inkbound.slnx` | `EndlessBattle.slnx` |
  | `Inkbound.Domain/Application/Infrastructure/Api/Tests`（資料夾、csproj、命名空間） | `EndlessBattle.*` |
  | `InkboundDbContext` | `EndlessBattleDbContext` |
  | `InkboundApiFactory` | `EndlessBattleApiFactory` |
  | `ConnectionStrings:Inkbound` | `ConnectionStrings:EndlessBattle` |
  | MySQL database `inkbound`、user `inkbound`、password `inkbound_dev`、root `root_dev` | database `endless_battle`、user `ebo`、password `ebo_dev`、root 不變 |
  | Testcontainers database/user/password | `endless_battle`／`ebo`／`ebo_test` |
  | `Jwt:Issuer`／`Audience` `inkbound` | `ebo` |
  | docker compose project／service 名稱 | 專案名由資料夾決定；service `mysql`、`flyway` 不變；volume `mysql-data` 不變 |
  | 前端 `package.json` name `frontend`/`inkbound-*` | `endless-battle-online` |
  | localStorage key `inkbound.accessToken` | `ebo.accessToken` |
  | `<title>墨渡</title>`、`<h1>墨渡</h1>`、頁面標題文案 | `Endless Battle Online`；登入頁大標題顯示 `EBO`（Cinzel），下方小字 `Endless Battle Online`；副標「奇幻放置 RPG」保留 |
  | `BackendPaths` 尋找 `Inkbound.slnx` | `EndlessBattle.slnx` |
  | `scripts/scaffold-db.sh` 的 `--context InkboundDbContext`、namespace、`-uinkbound -pinkbound_dev`、schema `inkbound` | 對應新名 |
  | `docs/superpowers/screenshots/`、`.superpowers/` | 不改 |
- Repo 資料夾 `~/Project/Inkbound` → `~/Project/endless-battle-online`：**由控制者在合併後執行**（`mv`），不在任務內做（會讓進行中的 agent 失去工作目錄）。
- 本機 MySQL 資料庫會因為 database 名稱改變而重建：`docker compose down -v && docker compose up -d`。開發資料（測試帳號）會消失，可接受。
- Commit：兩個結構性 commit（後端、前端＋文件），訊息 `♻️ refactor: rename the backend to EndlessBattle`、`♻️ refactor: rename the game to Endless Battle Online`；不加 Co-Authored-By。
- 完成條件：`./scripts/check.sh` 全綠；`git grep -i inkbound` 與 `git grep 墨渡` 只剩歷史文件（`docs/superpowers/specs|plans|research`、`docs/superpowers/screenshots`）的命中。

## Review Focus

- **scaffold 可重跑**：改名後 `./scripts/scaffold-db.sh` 必須能重新產生出與 commit 中一致的 `EndlessBattleDbContext.cs`／`Models/*.cs`（`git diff` 為空）。→ Task 1 Step 4。
- **連線字串與 Flyway 一致**：compose 的 flyway url／user／password、API 的 appsettings、Testcontainers 三者都指向新名，任何一處漏改都會讓 `check.sh` 紅。→ `./scripts/check.sh`。
- **舊 token 不殘留**：前端改 key 後，舊瀏覽器裡的 `inkbound.accessToken` 不再被讀取（使用者需重新登入，屬預期）。→ Task 2 `authStorage` 測試 key 名稱。

---

### Task 1: 後端改名

**Files:** `backend/**`（資料夾、`.slnx`、`.csproj`、`global.json` 不變、`dotnet-tools.json` 不變、所有 `.cs` 的 namespace/using、`appsettings*.json`、`Properties/launchSettings.json` 若含名稱）、`docker-compose.yml`、`scripts/scaffold-db.sh`、`backend/Inkbound.Tests/Support/BackendPaths.cs`、`MySqlDatabaseFixture.cs`、`InkboundApiFactory.cs`

- [ ] **Step 1:** `git mv` 五個專案資料夾與 `.slnx`、`.csproj`；用 `sed`／IDE 全域替換 `Inkbound` → `EndlessBattle`（區分大小寫；`inkbound` 小寫的只出現在設定與 compose，依對照表個別改）。`ProjectReference` 路徑一併更新。
- [ ] **Step 2:** `docker compose down -v && docker compose up -d`，確認 flyway 以新資料庫名套用 3 個 migration。
- [ ] **Step 3:** `cd backend && dotnet build -warnaserror`，再 `dotnet test`，預期 114/114。
- [ ] **Step 4:** `./scripts/scaffold-db.sh`，然後 `git status` 必須乾淨（產生檔與 commit 一致；若 scaffold 產生差異，代表腳本或 Partial 檔沒改對，修到為止）。
- [ ] **Step 5:** `./scripts/check.sh` 全綠；`git grep -n -i inkbound -- backend scripts docker-compose.yml` 為空。
- [ ] **Step 6: Commit** `♻️ refactor: rename the backend to EndlessBattle`

### Task 2: 前端、文件改名

**Files:** `frontend/package.json`、`pnpm-lock.yaml`（name 欄位）、`index.html`、`src/api/authStorage.ts`（+ test）、`src/ui/AuthScreen.tsx`／`CreateCharacterScreen.tsx`／`App.tsx` 及其測試中的「墨渡」、`CLAUDE.md`、`frontend/src/api/schema.ts`（若含 `Inkbound` 字樣——它是產生檔：改後端後重新 `pnpm generate:api` 產生，不手改）、`docs/superpowers/specs/2026-10-21-wog-remake-design.md` §0 加一行定名

- [ ] **Step 1:** 先改測試中的字串斷言（標題、key），看它們變紅。
- [ ] **Step 2:** 改實作；登入頁標題改為 `EBO`（`--font-title`，大字、字距加寬），其下一行 `Endless Battle Online`，再下方既有副標。
- [ ] **Step 3:** 啟動 API（新設定）後 `pnpm generate:api`，確認 `schema.ts` 不含 `Inkbound`。
- [ ] **Step 4:** `pnpm check`、`pnpm build`、`./scripts/check.sh` 全綠；`git grep -n -i inkbound -- frontend CLAUDE.md` 與 `git grep -n 墨渡 -- frontend CLAUDE.md` 為空。
- [ ] **Step 5: Commit** `♻️ refactor: rename the game to Endless Battle Online`

### Task 3（控制者）：資料夾改名與收尾

- [ ] 合併後：`mv ~/Project/Inkbound ~/Project/endless-battle-online`；重新啟動 docker compose（專案名隨資料夾改變，volume 會是新的——Task 1 已重建過資料，再做一次 `up -d` 即可）；更新記憶檔中的路徑。
