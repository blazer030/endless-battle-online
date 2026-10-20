# 墨渡（Inkbound）

水墨修仙風格的單人放置 RPG。後端是 .NET 10 Web API，前端是 React + PixiJS（計畫 2 開始）。

- 設計文件：`docs/superpowers/specs/2026-10-21-inkbound-design.md`
- 實作計畫：`docs/superpowers/plans/`

## 結構

- `backend/Inkbound.Domain` — 遊戲規則：戰鬥、歷練、內容定義。純 C#，零依賴。
- `backend/Inkbound.Application` — 用例與介面（Repository、時鐘、亂數、遊戲內容）。
- `backend/Inkbound.Infrastructure` — EF Core（Database-First）、內容載入、介面實作。
- `backend/Inkbound.Api` — Minimal API、JWT、OpenAPI。
- `backend/Inkbound.Tests` — xUnit v3；需要資料庫的測試用 Testcontainers。
- `backend/db/ddl`、`backend/db/dml` — Flyway migration。
- `backend/content` — 遊戲內容 JSON。

## 硬規則

1. 遊戲規則只寫在 Domain；Api 不做遊戲計算。
2. Domain 沒有任何 PackageReference 或 ProjectReference；Application 只引用 Domain 與 `Microsoft.Extensions.DependencyInjection.Abstractions`。
3. 資料表結構只由 Flyway migration 決定，不使用 EF Core Migrations。
   - 檔名 `V<9 位數>__<小寫英文與底線>.sql`，ddl 與 dml 共用一條版本號序列。
   - 已 commit 的 migration 不得修改，要改就新增版本。
   - 不得含 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE`（`scripts/check-sql.sh` 會擋）。
4. `Inkbound.Infrastructure/Persistence/InkboundDbContext.cs` 與 `Persistence/Models/*.cs` 由 `scripts/scaffold-db.sh` 產生，**不得手改**；擴充寫在 `*.Partial.cs`。
5. scaffold 產生的 model 只在 Infrastructure 使用，Application 與 Domain 只看見 Domain 型別。
6. 遊戲內容只放 `backend/content/*.json`，資料庫只存玩家狀態；測試用 `Inkbound.Tests/TestContent/`。
7. 時間一律 UTC；資料庫時間欄位用 `DATETIME(6)`。

## 指令

- `./scripts/check.sh` — SQL 檢查、build（警告視為錯誤）、全部測試。提交前必須通過，以 exit code 判斷。
- `docker compose up -d` — 啟動 MySQL 8.4 並用 Flyway 套用 migration。
- `./scripts/scaffold-db.sh` — 從資料庫重新產生 EF Core model。
- `cd backend && dotnet run --project Inkbound.Api` — 啟動 API。
- `cd backend && dotnet test -- --filter-class "*SeededRandomTests"` — 只跑特定測試類別（xUnit v3 on Microsoft.Testing.Platform）。

需要資料庫的測試依賴 Docker 執行中。

## 資料庫變更流程

1. 在 `backend/db/ddl`（或 `dml`）新增下一個版本的 migration。
2. `docker compose up -d`（flyway 服務會套用新 migration）。
3. `./scripts/scaffold-db.sh` 重新產生 model。
4. 寫用到它的測試與程式。

## 慣例

- 開發方式：TDD，一次一個測試；結構性與行為性變更分開 commit。
- Commit 訊息：英文 Conventional Commits，type 前加 emoji（✨ feat、🐛 fix、♻️ refactor、🧪 test、🚧 chore、📄 docs）。
- 程式碼註解用中文，非必要不寫。
- 命名用完整字詞，不縮寫。
