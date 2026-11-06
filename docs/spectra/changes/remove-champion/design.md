## Context

冠軍在計畫 9 加入，是原版 PVP 冠軍（`wog_cp`）的單機版：每個角色一個席位，挑戰 NPC 或自己的舊快照。`wog45-content-import` 的審查發現 `ChampionSeat.ToCombatant` 沒帶力量、智力、體質、等級，`StatAbove` 條件打冠軍時一定成立；使用者決定不修，整個移除，因為它本來就是為 PVP 而設。

已確認沒有其他功能依賴冠軍：遊戲規則與 API 只有 `Domain/Champion`、`ChampionService`、`ChampionEndpoints` 使用；前端只有冒險頁的按鈕開啟 `ChampionPanel`；admin 與主規格（除了 `wog-content-import`）的內容都沒有引用；`skill-effects`、`wog-content-import` 主規格的 `@trace` 註解還指向 `ChampionDuelTests.cs`，這次一併刪除。

## Goals / Non-Goals

**Goals:**

- 程式、內容、資料表、測試與現行文件都沒有冠軍。
- 每一步單獨 commit，測試保持全綠。
- 其餘規則與戰鬥結果完全不變。

**Non-Goals:**

- 不動 `docs/research` 與 `docs/superpowers` 的舊文件，也不動已歸檔的變更；原版任務文字裡的「冠軍」也不動。
- 不新增任何替代玩法（英雄榜、對戰等）。
- 不動 `V000000009__add_companions.sql` 等既有 migration。
- 必殺技名稱由下一個變更 `remove-special-attack-name` 處理，這裡不動 `characters` 的欄位。

## Decisions

### 依相依順序由外往內移除

順序是前端 → API 與服務 → 規則與內容 → 資料表 → 匯入與文件。每一層拿掉時，已經沒有外層在用它，所以每步都能編譯、測試全綠。`IGameContent.SeedChampion` 回傳 `ChampionSeat`，所以內容載入與 `Domain/Champion` 要同一步拿掉。替代方案是一次刪完，但會變成一個難以檢視的大 commit。

### 危險語句每次使用前都要先問使用者

這條決定替換 `CLAUDE.md` 硬規則第 3 條的「不得含 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE`」。新規則是：「migration 要用 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE` 時，每次都要先問使用者這次可不可以用，同意才能用。」同意只算當次，之前同意過不代表之後可以用。`docs/spectra/config.yaml` 的同一條規則一起替換，避免兩邊矛盾。

`scripts/check-sql.sh` 拿掉語句檢查，只留檔名檢查：腳本無法判斷有沒有問過，留著會擋下使用者同意的 migration。這條規則不另外用清單或標記記錄同意，由使用者直接決定。

### 用 DROP TABLE 刪除 character_champions

新增 `backend/db/ddl/V000000017__drop_character_champions.sql`，內容是 `DROP TABLE character_champions;`。寫之前要依新規則問使用者這次可不可以用；不同意就停下來討論替代做法。替代方案是留下空表，但 `scripts/scaffold-db.sh` 會把資料庫裡每張表都產生成 model，`CharacterChampion` 會一直留在程式碼裡。資料不備份：專案還沒有正式玩家資料。

`scaffold` 的 `--force` 只覆寫不刪檔，所以重新產生後要刪掉過期的 `Persistence/Models/CharacterChampion.cs`。

### 不留防回歸測試

使用者決定不留檢查「冠軍已移除」的測試（路由回 404、沒有按鈕、沒有 `champion.json`、資料表不存在），讓 `backend`、`frontend/src` 等處完全找不到這些字。這些測試只在實作過程中用來確認修改有效，收尾時刪除。代價是之後冠軍被加回來時不會有測試發現。

### 舊文件保留原樣

`docs/superpowers`、`docs/research` 是當時的紀錄，專案規定不再更新，決策改記在這份 design。裁定彙整裡被推翻的項目已列在 proposal 的 What Changes。

## Implementation Contract

**硬規則對照：**

- 內容檔：刪除 `backend/content/champion.json` 與 `backend/EndlessBattle.Tests/TestContent/champion.json`；內容不再放別處。
- 資料表：只新增 `V000000017__drop_character_champions.sql`，流程是 migration → `docker compose up -d` → `./scripts/scaffold-db.sh`。
- scaffold 產生檔：內容只由腳本重新產生，唯一手動動作是刪除過期的 `CharacterChampion.cs`。
- `frontend/src/api/schema.ts`：API 執行中用 `pnpm generate:api` 重新產生。`admin/src/api/schema.ts` 沒有冠軍（已查），不用重新產生；admin 路由不涉及。

**行為：**

- `GET /api/champion`、`POST /api/champion/challenge` 回 404。
- 冒險頁沒有「冠軍」按鈕。
- `JsonGameContent.Load` 不讀 `champion.json`；資料夾沒有這個檔案也能載入。
- `character_champions` 不存在，其他資料表不變。
- `./scripts/check-sql.sh` 對含 `DROP TABLE` 的 migration 回 exit code 0，對不符命名規則的檔名仍回 exit code 1。

**驗收：** 全部測試全綠，`git grep` 在 `backend`、`frontend/src`、`admin/src`、`scripts`、`CLAUDE.md`、`README.md` 找不到 `champion` 與「冠軍」（`backend/db/ddl` 的 migration 除外），`./scripts/check.sh` exit code 0。

**範圍外：** Non-Goals 所列項目。

## Risks / Trade-offs

- [本機開發資料庫的冠軍資料會消失] → 使用者已同意完全移除，資料沒有保留價值。
- [腳本不再擋這些語句，漏問時不會有東西攔下] → 靠 `CLAUDE.md` 的規則與審查；這是使用者的決定。
- [沒有測試防止冠軍被加回來] → 使用者已決定接受，靠 `git grep` 在這次驗收時確認。
- [之後的 migration 可能誤刪資料] → 每次使用前都要問使用者；已 commit 的 migration 仍不得修改。
- [舊文件仍提到冠軍，讀者可能以為功能還在] → 裁定彙整是歷史紀錄，現行規則以主規格與 `CLAUDE.md` 為準。
