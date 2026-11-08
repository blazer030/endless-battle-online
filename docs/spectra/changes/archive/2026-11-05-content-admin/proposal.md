## Why

遊戲數值要重新設計，但現在只能手改 `backend/content/*.json`，改完也看不出對平衡的影響。需要一個本機後台：逐筆調整、新增內容，先存成草稿，用平衡報告確認沒問題後才寫回正式內容，避免「改一個欄位就直接改掉正式資料」。長期方向是內容改存資料庫、線上即時調整，這一版先把後台與草稿流程建起來。

## What Changes

- 新增獨立的 admin 前端專案 `admin/`（Vite + React + TypeScript + Refine + Ant Design），不受遊戲前端硬規則約束。
- `EndlessBattle.Api` 新增 `/admin/...` 路由群組，與遊戲的 `/api/...` 以網址區分；以設定 `Admin:Enabled` 開關，預設關閉、開發環境開啟；admin 有自己的 OpenAPI 文件，遊戲的 `v1` 文件不含 admin 路由。
- 內容草稿：道具、怪物、職業、技能、世界五類可逐筆修改與新增；改動只寫入草稿資料夾，正式內容與執行中的遊戲不受影響；每次寫入後用遊戲的內容載入檢查草稿並回報錯誤。
- 發布：草稿通過檢查、且正式內容在草稿建立後沒被改過時，寫回 `backend/content/` 並刪除草稿；重啟後遊戲才套用。也可以捨棄草稿。
- 平衡報告：從測試專案的 `BalanceReport` 搬到 Application，改成回傳結構化資料，可對草稿或正式內容產生；參考角色可帶職業技能；admin 的報告頁以單一表格對照草稿與正式內容，不同的格子標成「正式 → 草稿」。
- 五個可編輯的內容檔先統一成固定格式（一筆一行），之後發布的 diff 只出現真正改動的那幾筆。
- admin 版面：固定在頂端的 header（頁面名稱、草稿狀態與發布）、不隨頁面捲動的 sidebar、sidebar 每種內容有各自的 icon。
- CLAUDE.md 加上 admin 的結構與規則，前端硬規則限定為遊戲前端 `frontend`。

## Non-Goals (optional)

（見 design.md 的 Goals / Non-Goals）

## Capabilities

### New Capabilities

- `admin-access`: `/admin/...` 路由命名空間、`Admin:Enabled` 開關、admin 專用的 OpenAPI 文件
- `content-draft`: 五類內容的讀取、逐筆修改與新增、草稿檢查、捨棄與發布
- `balance-report`: 對草稿或正式內容產生平衡報告（穿戴等級、各地區冒險效率，可帶技能）
- `admin-console`: admin 前端的內容列表、編輯與新增表單、草稿狀態與發布、報告對照頁、版面（header、sidebar、icon）

### Modified Capabilities

（無：目前還沒有任何主規格）

## Impact

- 後端：`backend/EndlessBattle.Api`（Program.cs、新增 `Endpoints/Admin/`、appsettings）、`backend/EndlessBattle.Application`（草稿與報告的用例與介面）、`backend/EndlessBattle.Infrastructure`（草稿資料夾存取、內容檔格式化寫出）、`backend/EndlessBattle.Tests`（移除 `Content/BalanceReport.cs`，新增 admin 測試）。
- 內容：`backend/content/` 的 items、monsters、jobs、skills、world 五個檔案一次性改成固定格式，數值不變。
- 新專案：`admin/`（pnpm），`scripts/check.sh` 加入 admin 的檢查。
- 文件：`CLAUDE.md`（結構、admin 規則、指令、前端硬規則的適用範圍）、`.gitignore`（草稿資料夾）。
- 遊戲前端 `frontend` 與遊戲 API 行為不變；`pnpm generate:api` 產生的 `schema.ts` 不會出現 admin 路由。
