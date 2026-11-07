## Why

WOG 4.5 有 345 個任務，目前只匯入 248 個（一般任務 240、每日 8）。其餘 97 個因為現有規則做不到而留在 `docs/research/wog45/gaps.md`：只能有一個擊殺目標、一般任務不能交寵物、沒有 NPC 遭遇、沒設子地區的怪被排除，連帶讓後面的任務失去前置條件。補齊這些規則後，原版主線才能接得起來，之後的 `content-editor` 也才有完整內容可以編輯。

## What Changes

- 一般任務可以有多個擊殺目標，各目標分開計數；原本的 `kill` 改成 `kills` 清單（**BREAKING**：內容 JSON 與任務 API 的欄位都會變）。
- 一般任務可以把寵物當交付目標（`deliverPet`），規則沿用每日任務：交出攜帶中的那隻，完成後刪除。
- 新增 NPC 遭遇：NPC 不打架、沒有報酬，遇到時目標 +1 並顯示原版訊息，傭兵遇到也一樣。
- 新增任務的技能條件（`requiredSkill`）：照原版在回報時檢查，不擋接取。
- 有子地區的地區裡，沒設子地區的怪在每個子地區都會出現。
- 配方可以有前置任務（`requiredMissionId`），任務完成後才能合成；這點刻意與原版不同（原版接了就能做）。
- 匯入腳本依新規則重新產生內容：一般任務 240 → 336 個，怪物 391 → 461 隻；`gaps.md` 同步縮減。

推翻或調整裁定彙整（`docs/superpowers/specs/2026-10-22-wog-remake-rulings.md`）的條目：

- 計畫 8 的〔簡化〕「擊殺任務只有一個目標」。
- 明確不做清單的「NPC 對話任務（`m_npc`）：本作沒有 NPC 對話系統」與「多目標擊殺任務」。這裡只做遭遇時顯示訊息，不做對話系統。

## Capabilities

### New Capabilities

- `mission-objectives`：一般任務的多目標擊殺、交寵物、技能條件，以及任務怪在任務進行中的出現範圍。
- `monster-encounters`：沒設子地區的怪如何出現，以及 NPC 遭遇。
- `recipe-prerequisites`：配方的前置任務。

### Modified Capabilities

- `wog-content-import`：任務、怪物、配方的匯入範圍與 `gaps.md` 列出的項目改變。

## Impact

- Domain：`Missions/`、`World/MonsterPool.cs`、`Content/MonsterDefinition.cs`、`Items/Recipe.cs`、`Forge/Synthesis.cs`、`Adventure/AdventureSimulator.cs`、`Mercenaries/MercenarySimulator.cs`、`Battle/BattleEvents.cs`
- Application：`Missions/MissionService.cs`、`Missions/IMissionRepository.cs`、`Adventure/AdventureService.cs`、`Forge/ForgeService.cs`
- Infrastructure：`Content/JsonGameContent.cs`、`Persistence/MissionRepository.cs`、scaffold 產生的 model
- Api：`Endpoints/MissionEndpoints.cs`、`Endpoints/ForgeEndpoints.cs`、`Endpoints/Contracts.cs`
- Tests：任務、怪物池、合成、匯入與內容驗證的測試，以及 `TestContent/`
- content：`backend/content/missions.json`、`monsters.json`、`monster-skills.json`、`recipes.json`
- db：`backend/db/ddl/V000000019__mission_kill_progress.sql`
- frontend：`src/api/schema.ts`（產生檔）、`src/game/missions.ts`、`src/game/stageModel.ts`、`src/game/battleLog.ts`、`src/game/types.ts`、`src/game/inventory.ts`、`src/render/StageRenderer.ts`、`src/ui/ForgePage.tsx`、`src/ui/BagScreen.css`
- scripts 與文件：`scripts/import-wog45.py`、`docs/research/wog45/gaps.md`
