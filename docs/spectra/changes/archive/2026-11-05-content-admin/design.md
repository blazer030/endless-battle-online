## Context

- 遊戲內容放在 `backend/content/*.json`（11 個檔案），API 啟動時由 `JsonGameContent.Load(資料夾)` 一次讀入並檢查，有錯就啟動失敗；載入結果以 `IGameContent` 單例注入（`backend/EndlessBattle.Infrastructure/DependencyInjection.cs` 的 `AddInfrastructure`，資料夾由設定 `Content:Directory` 決定）。
- 遊戲 API 全部以 `app.MapGroup("/api/...")` 註冊在 `backend/EndlessBattle.Api/Endpoints/`，OpenAPI 只有預設的 `v1` 一份，遊戲前端用它產生 `frontend/src/api/schema.ts`。
- `backend/EndlessBattle.Tests/Content/BalanceReport.cs` 是平常不跑的報告雛型，直接呼叫 Domain 的 `AdventureSimulator`、`EquipmentRules`、`CharacterFactory`，輸出 Markdown 檔。
- 五個要編輯的內容檔格式不一：items、skills 一筆一行或多行、jobs 手動對齊、monsters 大括號內有空白、world 完全展開。
- 參考 fantasy-system 的做法：同一個 server，以網址前綴區分 admin 與遊戲，用設定決定開放哪些 endpoint。
- 使用者只有開發者本人，第一版只在本機使用。

## Goals / Non-Goals

**Goals:**

- 道具、怪物、職業、技能、世界（大陸、地區）五類內容可在 admin 逐筆修改與新增。
- 所有改動先進草稿；正式內容與執行中的遊戲在發布前不變。
- 草稿每次寫入都經過遊戲同一套內容檢查，錯誤顯示在 admin。
- 可對草稿與正式內容產生平衡報告並對照（單一表格，與正式內容不同的格子標成「正式 → 草稿」），報告可帶技能。
- admin 版面有固定的 header 與不隨頁面捲動的 sidebar，sidebar 每種內容有各自的 icon。
- 發布寫回正式內容並刪除草稿；正式內容在草稿建立後被改過時拒絕發布。
- 發布產生的 git diff 只出現真正改動的那幾筆。

**Non-Goals:**

- 刪除內容紀錄（刪掉的 id 可能還被其他內容引用；第一版不做）。
- 編輯其他六個內容檔（missions、recipes、mercenaries、inn、honour-shop、champion）。
- 發布後即時生效、遊戲執行中替換內容：發布後要重啟 API。
- 內容改存資料庫、硬規則第 3、6 條的調整：之後另開變更。
- admin 帳號角色與登入：第一版只靠 `Admin:Enabled` 開關、只在本機開。
- 同時存在多份草稿、多人同時編輯。
- 用公式產生內容：只做逐筆編輯。

## Decisions

### 草稿是整份內容資料夾的副本

第一次寫入時把正式內容資料夾整份複製到 `Admin:DraftDirectory`（預設 `../content-draft`，與 `Content:Directory` 一樣相對於 API 的 content root，即 `backend/content-draft/`，加入 `.gitignore`）。檢查草稿就是對這個資料夾呼叫現有的 `JsonGameContent.Load`，不用改動載入程式。

- 替代方案：把 `JsonGameContent` 的讀檔與檢查拆開，讓記憶體裡的草稿也能檢查。討論時曾規劃，但草稿既然是檔案就不需要，省下一次大型重構。
- 取捨：`Load` 遇到第一個錯誤就丟例外，所以一次只回報一個錯誤。

### 以 JSON 物件逐筆讀寫

Infrastructure 以 `System.Text.Json.Nodes.JsonObject` 讀寫每一筆紀錄，依 `id` 找到要改的那筆；admin API 的請求與回應本體就是那筆紀錄的 JSON 物件。欄位的意義與表單由 admin 前端定義，欄位值是否合法由內容檢查判斷。

- 替代方案：為五類內容各寫型別化 DTO。`JsonGameContent` 的檔案 DTO 都是 private，且部分欄位用 `JsonElement` 分辨漏寫與 0；另寫一套等於重複定義且容易漏欄位。
- 取捨：admin OpenAPI 文件裡紀錄本體是一般物件，前端型別要自己定義。

可編輯種類與檔案的對應：`items` → `items.json` 的 `items`、`monsters` → `monsters.json` 的 `monsters`、`jobs` → `jobs.json` 的 `jobs`、`skills` → `skills.json` 的 `skills`、`areas` → `world.json` 的 `areas`、`places` → `world.json` 的 `places`。

### 內容檔固定格式

寫出內容檔時一律用同一個格式化程式：兩格縮排、頂層物件的每個陣列一筆一行、其餘欄位照原順序。先做一次「只改格式」的整理，把五個檔案轉成這個格式並單獨 commit（數值不變，結構性變更），之後發布的 diff 只出現改動的那幾行。

### 發布前比對正式內容指紋

建立草稿時，在草稿資料夾的 `.base.json` 記下每個正式內容檔的 SHA-256。發布時重算正式內容的指紋，任何一個不同就 409 並列出檔名（例如草稿建立後又 `git pull` 了新內容），不覆蓋。發布成功後刪除整個草稿資料夾。

### admin 路由與開關

`Admin:Enabled` 為 `true` 時，Program.cs 才呼叫 `MapAdminEndpoints`；它建立 `app.MapGroup("/admin")`，所有 admin endpoint 掛在這個群組下，檔案放 `backend/EndlessBattle.Api/Endpoints/Admin/`。`appsettings.json` 預設 `false`，`appsettings.Development.json` 設 `true`。OpenAPI 另加一份名為 `admin` 的文件，只收 `/admin/` 路徑；`v1` 排除 `/admin/` 路徑。

### 平衡報告搬到 Application

報告邏輯搬到 `backend/EndlessBattle.Application/Balance/`，對外是一個以 `IGameContent` 與報告選項為輸入、回傳結構化結果（穿戴等級表、各地區冒險結果）的服務；計算全部呼叫 Domain，符合「Api 不做遊戲計算」。參考角色的假設照雛型：初階職業、創角配點 力4／智4／生1／敏1、每級成長取期望值並捨去、推薦等級下限、各欄穿得上的最高商店等級裝備、亂數種子固定、模擬 8 小時、從 0 元開始。搬完後刪除 `backend/EndlessBattle.Tests/Content/BalanceReport.cs`，不再輸出 Markdown 檔。

### 報告帶技能

每個地區與職業的冒險結果算兩次：不帶技能、帶技能。帶技能時取 `IGameContent.SkillsOf(職業)`，主動技能依內容順序放主動欄、被動放被動欄，數量上限照 `SkillLoadout.ActiveSlots`、`SkillLoadout.PassiveSlots`。技能等級由請求選項決定：`First`（Lv1）或 `Max`（各技能最高級），預設 `Max`。目前每個職業最多 2 個技能（`backend/content/skills.json`），不會超過欄位上限。

### admin 前端：Refine + Ant Design

`admin/` 是獨立的 pnpm 專案：Vite + React + TypeScript strict + `@refinedev/core` + `@refinedev/antd`（皆 MIT 授權）。開發伺服器把 `/admin`、`/openapi` proxy 到 `http://localhost:5062`；API 型別用 `openapi-typescript` 從 `/openapi/admin.json` 產生。遊戲前端的硬規則（手機直式、`theme.css` 色彩變數、字型、Pixi）不適用於 admin。`scripts/check.sh` 加入 admin 的 lint、型別檢查與測試。

### 報告對照：單一表格

報告頁只放一張表，顯示草稿的數值（沒有草稿時顯示正式內容）；和正式內容不同的格子寫成「正式 → 草稿」（例如 `8 → 10`）並加上標記。

- Supersedes：原本的「並排顯示草稿與正式內容的報告」（本文件 Goals 與 tasks 5.4 的舊寫法）。實作時改成單一表格，因為兩張大表左右擺要來回對位置，單一表格直接在格子上看出變化；實作已是單一表格，不需補做。
- 報告內容本身的改版（重點清單、職業與地區的圖表、先選職業／地區再看）不在本變更，另開變更處理。

### admin 版面

用 Refine `ThemedLayout` 的 `Header` 與 `Sider`：

- header 固定在頁面頂端（`sticky`），左邊是目前頁面的名稱，右邊是草稿狀態與「發布」「捨棄」按鈕（原本放在內容上方的草稿列移進 header）；草稿不通過時的錯誤訊息仍顯示在內容區最上方。
- sidebar 用 `fixed`，頁面捲動時 sidebar 留在原位。
- sidebar 每個項目用 `@ant-design/icons` 的不同 icon：道具 `ShoppingOutlined`、怪物 `BugOutlined`、職業 `IdcardOutlined`、技能 `ThunderboltOutlined`、大陸 `GlobalOutlined`、地區 `EnvironmentOutlined`、平衡報告 `BarChartOutlined`。

## Implementation Contract

**admin API（全部在 `Admin:Enabled` 為 `true` 時才存在）**

| 方法與路徑 | 行為 | 錯誤 |
|---|---|---|
| `GET /admin/content/{kind}` | 列出該種類全部紀錄（有草稿讀草稿，否則讀正式內容） | 不支援的 kind → 404 |
| `GET /admin/content/{kind}/{id}` | 取得一筆 | 找不到 → 404 |
| `PUT /admin/content/{kind}/{id}` | 以本體取代該筆並存入草稿（沒有草稿先建立）；回傳草稿狀態 | 本體的 `id` 與路徑不同 → 400；找不到 → 404 |
| `POST /admin/content/{kind}` | 新增一筆到該陣列最後並存入草稿；回傳 201 與草稿狀態 | `id` 已存在 → 409 |
| `GET /admin/draft` | 草稿狀態：是否存在、是否通過檢查、錯誤訊息 | — |
| `DELETE /admin/draft` | 刪除草稿 | 沒有草稿 → 404 |
| `POST /admin/draft/publish` | 草稿寫回正式內容並刪除草稿 | 沒有草稿 → 404；檢查不通過 → 409 附錯誤訊息；正式內容指紋不同 → 409 附變動的檔名 |
| `POST /admin/reports` | 本體 `{ source: "Official" \| "Draft", skillLevel: "First" \| "Max" }`（讀入時不分大小寫），回傳報告 | 草稿不存在 → 404；草稿檢查不通過 → 409 |

**報告結果**：穿戴等級（職業 × 需求能力 × 商店等級 → 等級或「無法達到」）、冒險結果（地區 × 職業 × 有無技能 → 場數、勝率、敗場、超時、經驗／時、毛收入／時、淨收入／時、升級數、下一階整套裝備所需小時），以及產生時使用的假設（配點、種子、模擬時長、技能等級選項）。

**範圍內**：上表 API、草稿資料夾、內容檔格式化、報告服務與兩種技能選項、admin 前端的列表／編輯／新增／草稿狀態／發布／報告對照、admin 版面（header、固定 sidebar、各項目 icon）、CLAUDE.md 與 check.sh 更新。

**範圍外**：Non-Goals 所列各項；遊戲 API、遊戲前端與遊戲規則的行為；平衡報告內容的改版（重點清單、圖表、依職業／地區篩選）。

**測試**：admin API 測試用 `EndlessBattleApiFactory`，正式內容與草稿資料夾都指向測試暫存資料夾裡 `TestContent` 的副本，不得改到 `EndlessBattle.Tests/TestContent/` 本身。

## Risks / Trade-offs

- [發布後要重啟才生效，容易以為沒套用] → 發布成功的回應與 admin 畫面都標明「重啟 API 後生效」。
- [一次只看到一個檢查錯誤] → 每次寫入都重新檢查，逐一修正；之後需要再把檢查改成收集全部錯誤。
- [紀錄本體是無型別的 JSON，前端表單可能漏欄位] → 編輯時以取得的整筆紀錄為底再改，未在表單上的欄位原樣保留；最後由內容檢查把關。
- [地區 id 上限] → 現有檢查規定地區 id 1～21、子地區 0 或 4 個（`JsonGameContent` 的 `MaximumPlaceId`、`SubPlacesPerPlace`），新增第 22 個地區會被檢查擋下；這是現有規則，本變更不調整。
- [一次性格式整理的 diff 很大] → 單獨 commit、標明結構性變更，並以測試證明整理前後解析出的內容完全相同。
- [admin 路由意外在正式環境開啟] → 預設關閉，只有開發設定打開；上線前要另加 admin 權限。

## Migration Plan

沒有資料庫變更。部署方式不變：正式環境的 `Admin:Enabled` 維持預設 `false`。要回復時刪除 admin 相關程式與 `admin/` 專案即可，內容檔的格式整理不影響數值。

## Open Questions

無。
