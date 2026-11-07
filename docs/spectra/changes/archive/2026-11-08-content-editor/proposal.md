## Why

admin 現在只是 JSON 編輯器：表單直接顯示原始欄位（`at`、`mdf`、`be`）、關聯要手填 id、成長和加成以 JSON 字串呈現，跟直接改內容檔沒差多少。內容匯入與補規則之後問題更明顯：道具從 166 筆變成 3127 筆、怪物 461 隻，技能改成由條件、消耗、效果組成（76 個技能、509 級），職業與怪物的技能放在獨立的關聯表，用 JSON 字串幾乎沒辦法編輯。根本原因是分層不清：內容檔的儲存格式一路穿過 API 直接進到畫面，任何一層的調整都會牽動其他層。這次把邊界資料限制在轉換層——JSON 檔只是儲存載體、API 用有型別且完整名稱的合約、前端先把 API 回應轉成畫面用的物件——之後改 API 介面或換成資料庫時，只要改轉換的地方，畫面與遊戲邏輯不受影響。

## What Changes

- **BREAKING（只影響 admin）**：admin 內容 API 從「原樣轉送 JSON 物件」改成每種內容一個有型別的紀錄，欄位一律用完整名稱（例如 `str` → `strength`、`mdf` → `magicDefense`、`be` → `faith`、`topr` → `dropRarity`、`meet` → `encounterRate`）；Infrastructure 負責內容檔與紀錄之間的雙向轉換，內容檔格式維持不變。
- 正式內容檔讀成紀錄再寫回，必須與原檔逐字相同；內容檔若有紀錄沒涵蓋的欄位，測試會失敗，避免存檔時默默丟資料。兩張關聯表也一樣：`monster-skills.json` 併進怪物紀錄、`job-skills.json` 併進技能紀錄，存檔時一起寫回。
- admin API 新增唯讀的任務列表（id、標題、是否每日），供怪物的「限定任務」下拉選單使用；任務本身仍不可編輯。
- OpenAPI 文件改在 `dotnet build` 時產生成檔案；`frontend` 與 `admin` 的 `pnpm generate:api` 改從檔案產生 `schema.ts`，不必先啟動 API；`scripts/check.sh` 檢查兩份 `schema.ts` 是否與目前的 API 一致。
- admin 前端改成三層：OpenAPI 產生的 DTO 型別 → 手寫的 Converter（`toVo`／`toDto`）→ 畫面用的 VO class，寫法參考 `~/Project/game-web`；回應缺少必要欄位時顯示錯誤，不認識的欄位忽略；不保留原始 JSON 編輯。
- 編輯與新增改成在列表上開彈窗，不換頁。
- 關聯欄位改成下拉選單（地區與連動的子地區、掉落道具、限定任務、怪物的技能、上級職業、能力需求的能力、技能能學的職業、前置技能、武器附帶技能、大陸的地區、鑰匙的地區），選單顯示名稱、存回 id；項目多的選單可以搜尋。
- 編輯怪物時可以選它能用的技能（技能、等級、發動率）；寫入時後端檢查引用的技能與職業還存在，不存在就回 400，草稿不變。
- 結構化欄位改成逐項輸入：職業成長（七項能力各最小／最大）、能力加成與需求；技能先設等級上限再逐級填學習金錢、熟練度、冷卻、發動條件、消耗、效果，各欄位依種類顯示；道具依分類只顯示相關欄位；怪物勾選 NPC 時只留 NPC 用得到的欄位。
- 列表分頁、可搜尋（id、名稱）與依欄位篩選，在前端處理；關聯欄位在列表上顯示名稱。

## Non-Goals (optional)

（見 design.md 的 Goals / Non-Goals）

## Capabilities

### New Capabilities

- `api-documents`: OpenAPI 文件在建置時產生成檔案，兩個前端從檔案產生 API 型別，檢查會擋下過期的型別檔

### Modified Capabilities

- `content-draft`: 內容紀錄改成有型別、完整欄位名稱的合約；新增唯讀任務列表；新增內容檔與紀錄雙向轉換不失真的要求
- `admin-console`: 內容頁改成彈窗表單、關聯欄位用選的、結構化欄位逐項輸入、列表搜尋與篩選；新增 API 回應轉換成畫面物件的要求
- `admin-access`: 遊戲 API 型別改從建置產生的 `v1` 文件檔產生，不再從執行中的 API 抓

## Impact

- Application：`backend/EndlessBattle.Application/Admin/`（各種內容的紀錄型別、`ContentAdminService` 改用紀錄、新增任務列表、寫入時檢查關聯表的引用）。
- Infrastructure：`backend/EndlessBattle.Infrastructure/Content/`（內容檔與紀錄的轉換、`FileContentDraftStore` 改用紀錄、一次寫入含兩個檔案）。
- Api：`backend/EndlessBattle.Api/Endpoints/Admin/AdminContentEndpoints.cs`（有型別的請求與回應、任務列表）、`backend/EndlessBattle.Api/EndlessBattle.Api.csproj`（建置時產生 OpenAPI 文件）、新增產生的文件資料夾 `backend/openapi/`。
- Tests：`backend/EndlessBattle.Tests/`（admin API 測試改用新欄位名稱、內容檔雙向轉換測試）。
- admin：`admin/src/`（DTO／Converter／VO、彈窗表單、關聯選單、結構化欄位元件、列表搜尋篩選；`package.json` 的 `generate:api`）。
- frontend：只有 `frontend/package.json` 的 `generate:api` 改成從檔案產生；遊戲 API 與畫面不變。
- 工具與文件：`scripts/check.sh`、`CLAUDE.md`（指令段落的 generate:api 說明）。
- 不涉及：Domain、content 內容檔的數值與格式（包含 `job-skills.json`、`monster-skills.json`）、db。
