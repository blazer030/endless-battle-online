## 1. 規則與內容檔整理

- [x] 1.1 CLAUDE.md 加上 admin 規則（依「admin 路由與開關」「admin 前端：Refine + Ant Design」）：「結構」加入 `admin/` 與 `backend/EndlessBattle.Api/Endpoints/Admin/`；新增「admin」段落，寫明 `/admin/...` 與 `/api/...` 以網址區分、admin endpoint 只放 `Endpoints/Admin/` 並掛在 `/admin` 群組、`Admin:Enabled` 預設關閉、OpenAPI 分 `v1` 與 `admin`、admin 只寫草稿且只有發布會寫正式內容；前端硬規則標題改為「遊戲前端（`frontend`）」並註明不適用 admin。驗證：人工檢閱 CLAUDE.md diff，確認以上每一點都有對應文字。
- [x] 1.2 依「內容檔固定格式」實作內容檔格式化程式（Infrastructure），並把 items、monsters、jobs、skills、world 五個檔案轉成固定格式（Stable file format）；只改格式、不改數值，單獨以結構性變更 commit。驗證：新測試證明格式化前後解析出的 JSON 完全相同、格式化已是固定格式的檔案輸出位元組不變；`cd backend && dotnet test` 全綠。

## 2. admin 路由

- [x] 2.1 依「admin 路由與開關」新增 `Admin:Enabled` 設定與 `/admin` 群組（Admin routes live under the admin namespace；Admin is enabled only by configuration）：關閉時任何 `/admin/` 路徑 404，開啟時 `GET /admin/content/items` 回 200（此時可先回空清單）。驗證：API 測試 `Admin_DisabledReturnsNotFound`、`Admin_EnabledServesAdminRoutes`。[after: 1.1]
- [ ] 2.2 新增名為 `admin` 的 OpenAPI 文件（Admin has its own OpenAPI document）：`/openapi/admin.json` 只含 `/admin/` 路徑，`/openapi/v1.json` 不含任何 `/admin/` 路徑。驗證：API 測試比對兩份文件的路徑集合。[after: 2.1]

## 3. 內容草稿

- [ ] 3.1 依「草稿是整份內容資料夾的副本」與「以 JSON 物件逐筆讀寫」實作讀取（Editable content kinds）：`GET /admin/content/{kind}` 與 `GET /admin/content/{kind}/{id}` 支援 items、monsters、jobs、skills、areas、places，有草稿讀草稿、否則讀正式內容；其他 kind 404。驗證：API 測試涵蓋六種 kind 的列表筆數與 `recipes` 回 404。[after: 1.2, 2.1]
- [ ] 3.2 實作修改（Edits only change the draft）：`PUT /admin/content/{kind}/{id}` 第一次寫入時複製整份正式內容到 `Admin:DraftDirectory` 並寫入 `.base.json` 指紋，只取代該筆紀錄；正式內容檔與 API 已載入的內容不變；本體 `id` 與路徑不同 400、找不到 404。驗證：API 測試 `Draft_FirstEditCopiesContentAndLeavesOfficialUntouched`（含 `iron-sword` 價格 200 → 250 的範例）與草稿 `items.json` 只有該行改變的 diff 斷言。[after: 3.1]
- [ ] 3.3 實作新增：`POST /admin/content/{kind}` 把新紀錄加在陣列最後並回 201；`id` 已存在回 409 且草稿不變。驗證：API 測試新增怪物與重複 id 兩個案例。[after: 3.2]
- [ ] 3.4 每次寫入後用 `JsonGameContent.Load` 檢查草稿（Every draft write is checked），寫入照樣保存；`PUT`、`POST` 回應與 `GET /admin/draft` 都回傳是否存在、是否通過與錯誤訊息。驗證：API 測試把怪物 `placeId` 改成不存在的地區，斷言寫入保存且回報不通過與錯誤訊息。[after: 3.3]
- [ ] 3.5 依「發布前比對正式內容指紋」實作發布與捨棄（Publish and discard）：`POST /admin/draft/publish` 在通過檢查且指紋相同時寫回正式內容並刪除草稿、回應標明重啟後生效；不通過或指紋不同 409（附錯誤訊息或變動檔名）且正式內容不變；`DELETE /admin/draft` 刪除草稿。驗證：API 測試涵蓋發布成功、草稿不通過、正式內容被改過、捨棄四個案例。[after: 3.4]

## 4. 平衡報告

- [ ] 4.1 依「平衡報告搬到 Application」把 `backend/EndlessBattle.Tests/Content/BalanceReport.cs` 的計算搬成 Application 服務，輸入 `IGameContent` 與選項、回傳穿戴等級（Wear levels）與各地區冒險結果（Adventure results per place）的結構化資料，然後刪除測試專案裡的雛型。驗證：Application 測試用 `TestContent` 斷言穿戴等級表的一格數值、超過 Lv100 回報無法達到、同一輸入兩次結果相同。[after: 1.2]
- [ ] 4.2 依「報告帶技能」讓每筆冒險結果同時算出不帶技能與帶技能（Skills in the report），技能等級選項 `first`／`max`、預設 `max`，欄位上限照 `SkillLoadout`。驗證：Application 測試斷言提高某技能發動率後，只有帶技能的結果改變。[after: 4.1]
- [ ] 4.3 新增 `POST /admin/reports`（Report source）：`source` 為 `official` 或 `draft`；草稿不存在 404、草稿不通過 409。驗證：API 測試涵蓋兩種來源與兩種錯誤。[after: 3.4, 4.2]

## 5. admin 前端

- [ ] 5.1 依「admin 前端：Refine + Ant Design」建立 `admin/` 專案：Vite + React + TypeScript strict、`@refinedev/core`、`@refinedev/antd`、pnpm；`/admin`、`/openapi` proxy 到 `http://localhost:5062`；`pnpm generate:api` 從 `/openapi/admin.json` 產生型別；`pnpm check` 跑 lint、格式、型別檢查與測試；`scripts/check.sh` 加入 admin 的 `pnpm check`，CLAUDE.md「指令」加入 admin 的啟動與檢查指令。驗證：`./scripts/check.sh` exit code 0。[after: 2.2]
- [ ] 5.2 五類內容的列表、編輯、新增頁（Content pages）：大陸與地區分兩頁；編輯以取得的整筆紀錄為底，表單沒有的欄位原樣送回。驗證：元件測試斷言編輯怪物等級後送出的 `PUT` 本體只改了等級、其他欄位保留。[after: 3.3, 5.1]
- [ ] 5.3 每頁顯示草稿狀態與發布（Draft status and publishing）：顯示是否有草稿、是否通過與錯誤訊息；不通過時發布按鈕停用；發布被 409 拒絕時顯示原因或變動檔名；提供捨棄草稿。驗證：元件測試涵蓋不通過停用發布、409 顯示檔名兩個案例。[after: 3.5, 5.2]
- [ ] 5.4 報告對照頁（Report comparison）：同一個技能等級選項下並排顯示草稿與正式內容的報告，數值不同的格子加上標記。驗證：元件測試以兩份只差一格的報告斷言只有那一格被標記；人工以 `pnpm dev` 打開頁面確認可讀。[after: 4.3, 5.1]
