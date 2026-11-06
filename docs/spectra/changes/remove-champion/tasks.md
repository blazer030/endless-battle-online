## 1. 硬規則

- [x] 1.1 依「危險語句每次使用前都要先問使用者」，把 `CLAUDE.md` 硬規則與 `docs/spectra/config.yaml` 的「不得含」替換成「每次都要先問使用者這次可不可以用，同意才能用」，並讓 `scripts/check-sql.sh` 只檢查檔名。先在 scratchpad 建立含 `DROP TABLE` 且檔名合法的 migration，執行 `./scripts/check-sql.sh <目錄>` 確認回 exit code 1（改之前），改完後確認回 exit code 0；另一個不合法檔名的 migration 仍回 exit code 1。

## 2. 前端

- [x] 2.1 依「依相依順序由外往內移除」，讓冒險頁沒有冠軍。先把 `AdventureScreen_ChampionButtonOpensTheChampionDialog` 換成失敗測試 `AdventureScreen_HasNoChampionButton`，再移除按鈕與 `ChampionPanel`（含 CSS、測試）、`game/champion.ts`（含測試）、`api/client.ts` 的兩個呼叫、`game/types.ts` 的型別、`frontend/src/test/fixtures.ts` 的 `makeChampion`，以及 `App.test.tsx`、`useAdventureLoop.test.tsx`、`AdventureScreen.test.tsx` 的替身。以 `cd frontend && pnpm check` 驗證。

## 3. API 與服務

- [x] 3.1 讓冠軍路由不存在。先寫失敗測試 `Champion_RoutesAreGone`（帶登入的 `GET /api/champion`、`POST /api/champion/challenge` 都回 404），再移除 `ChampionEndpoints`、`Program.cs` 的註冊、`Contracts.cs` 的回應型別、`ChampionService`、`IChampionRepository`、`ChampionRepository`、兩個 `DependencyInjection.cs` 的註冊，並刪除 `ChampionEndpointsTests`、`ChampionServiceTests`、`InMemoryChampionRepository`、`CompanionRepositoryTests` 的冠軍案例。API 執行中跑 `pnpm generate:api` 更新 `frontend/src/api/schema.ts`。以 `cd backend && dotnet test` 與 `cd frontend && pnpm check` 驗證。 [after: 2.1]

## 4. 規則與內容

- [x] 4.1 依 Requirement「Custom content without an original counterpart is kept」，讓遊戲不需要 `champion.json`。先寫失敗測試 `Load_WithoutChampionFile_Succeeds`，再移除 `IGameContent.SeedChampion`、`JsonGameContent` 的讀取與驗證、`Domain/Champion` 全部，刪除 `backend/content/champion.json`、`TestContent/champion.json`、`Domain/Champion` 的測試、`CompanionTableTests.Champion_SeedMatchesTable` 與 `JsonGameContentTests` 的冠軍案例與輔助內容。以 `cd backend && dotnet test` 驗證，其他測試的數值斷言一律不動。 [after: 3.1]

## 5. 資料表

- [x] 5.1 依「用 DROP TABLE 刪除 character_champions」與「不留防回歸測試」，使用者已同意這次使用 `DROP TABLE`。把 `DatabaseSchemaTests` 的冠軍斷言拿掉，先用暫時的 Testcontainers 測試 `CharacterChampions_TableIsGone` 確認資料表還在時會失敗，再新增 `backend/db/ddl/V000000017__drop_character_champions.sql`，`docker compose up -d` 後跑 `./scripts/scaffold-db.sh`，並刪除過期的 `Persistence/Models/CharacterChampion.cs`。測試轉綠後刪除這條暫時測試，不 commit 它。以 `cd backend && dotnet test` 驗證，並確認 `EndlessBattleDbContext.cs` 與 `Models/Character.cs` 沒有 `CharacterChampion`。 [after: 1.1, 4.1]

## 6. 匯入與文件

- [x] 6.1 依 Requirement「Custom content without an original counterpart is kept」，先改 `WogImportTests`：拿掉 `champion.json` 必須在保留清單的斷言，改成失敗斷言 `gaps.md` 不提到 `champion.json`、`backend/content` 沒有這個檔案。再改 `scripts/import-wog45.py` 的 `KEPT_CONTENT_FILES`，執行 `python3 -I scripts/import-wog45.py` 重新產生 `gaps.md`，測試轉綠後 commit；再跑一次腳本，以 `git diff --exit-code backend/content docs/research/wog45/gaps.md` exit code 0 確認沒有變動。 [after: 4.1]
- [ ] 6.2 依「不留防回歸測試」與「舊文件保留原樣」，刪除 `backend/EndlessBattle.Tests/Api/RemovedRoutesTests.cs`、`AdventureScreen_HasNoChampionButton`、`Load_WithoutChampionFile_Succeeds` 與 `WogImportTests` 裡提到 `champion.json` 的兩條斷言，並拿掉 `README.md` 的冠軍列，`docs/superpowers`、`docs/research` 不動。以 `git grep -i -E 'champion|冠軍' -- backend frontend/src admin/src scripts CLAUDE.md README.md ':!backend/db/ddl'` 沒有輸出確認；有輸出就回頭處理。 [after: 2.1, 5.1, 6.1]

## 7. 收尾

- [ ] 7.1 實際打開遊戲確認：冒險頁沒有冠軍按鈕、版面沒有留下空位、一般戰鬥正常。以 `pnpm dev` 搭配 Development API，在 375×667 手動確認。 [after: 2.1, 5.1]
- [ ] 7.2 執行 `./scripts/check.sh`，確認 exit code 0。 [after: 6.2, 7.1]

## 變更紀錄

使用者決定不留防回歸測試。任務 2.1、3.1、4.1、6.1 寫下的測試仍依原文完成，由任務 6.2 刪除；任務 5.1 的測試只在實作時暫時使用。
