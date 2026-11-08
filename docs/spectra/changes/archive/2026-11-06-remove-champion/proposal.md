## Why

冠軍是原版 PVP 系統（`wog_cp`）的單機替代品：對手只有 NPC 或自己的舊快照，本來就沒有對戰的意義，能力資料還有缺漏（力量、智力、體質、等級都是 0）。接下來要做怪物技能與任務規則，不值得繼續維護它，所以整個移除。

## What Changes

- **BREAKING** 移除 `GET /api/champion` 與 `POST /api/champion/challenge`，以及 `ChampionResponse`、`ChampionChallengeResponse`。
- 移除遊戲規則與服務：`Domain/Champion`、`ChampionService`、`IChampionRepository`、`ChampionRepository`。
- 移除內容：`backend/content/champion.json`、`IGameContent.SeedChampion`，以及內容載入與驗證。
- 移除前端：冒險頁的「冠軍」按鈕、`ChampionPanel`、`game/champion.ts`、`api/client.ts` 的呼叫。
- 新增 migration 用 `DROP TABLE` 刪除 `character_champions`，既有的冠軍資料不保留。實作到這一步要先問使用者這次可不可以用，同意才寫。
- **替換專案硬規則「migration 不得含 `DROP TABLE`、`DROP VIEW`、`DROP DATABASE`、`TRUNCATE`」**：這四種語句改成每次使用前都要先問使用者，這次同意才能用。`CLAUDE.md`、`docs/spectra/config.yaml` 的規則同步替換；`scripts/check-sql.sh` 拿掉語句檢查，只留檔名檢查。檔名規則與「已 commit 的 migration 不得修改」不變。
- 匯入腳本的保留清單拿掉 `champion.json`，重新產生 `docs/research/wog45/gaps.md`。
- `README.md` 拿掉冠軍那一列。
- 推翻裁定彙整（`docs/superpowers/specs/2026-10-22-wog-remake-rulings.md`）計畫 9 的冠軍相關裁定：冠軍被推翻後鎖定 7 天、每個角色一個冠軍席位、冠軍沒有技能、冠軍是冒險頁地點標題旁的按鈕；「傭兵與冠軍挑戰不吃密藥」只剩傭兵的部分有效。

## Capabilities

### New Capabilities

（無）

### Modified Capabilities

- `wog-content-import`: 「Custom content without an original counterpart is kept」不再保留 `champion.json`。

## Impact

- Domain：`backend/EndlessBattle.Domain/Champion`
- Application：`backend/EndlessBattle.Application/Champion`、`backend/EndlessBattle.Application/Content/IGameContent.cs`、`backend/EndlessBattle.Application/DependencyInjection.cs`
- Infrastructure：`backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs`、`backend/EndlessBattle.Infrastructure/Persistence`（重新 scaffold）、`backend/EndlessBattle.Infrastructure/DependencyInjection.cs`
- Api：`backend/EndlessBattle.Api/Endpoints/ChampionEndpoints.cs`、`backend/EndlessBattle.Api/Endpoints/Contracts.cs`、`backend/EndlessBattle.Api/Program.cs`
- Tests：`backend/EndlessBattle.Tests` 的冠軍測試與 `TestContent/champion.json`
- content：`backend/content/champion.json`
- db：`backend/db/ddl` 新增一個 migration
- frontend：`frontend/src/ui`、`frontend/src/game`、`frontend/src/api`（`schema.ts` 重新產生）、`frontend/src/test/fixtures.ts`
- 工具與文件：`scripts/check-sql.sh`、`scripts/import-wog45.py`、`docs/research/wog45/gaps.md`、`CLAUDE.md`、`docs/spectra/config.yaml`、`README.md`、`docs/spectra/specs/skill-effects/spec.md`（只刪失效的 `@trace`）
