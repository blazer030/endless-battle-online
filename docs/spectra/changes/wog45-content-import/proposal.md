## Why

目前的遊戲內容（68 隻怪物、166 個道具、16 種技能、24 個任務）是在 WOG 4.5 資料表遺失時自訂的。2026-10-23 找回原版 SQL 並整理成 `docs/research/wog45/data/*.json`，現在可以把內容換成原版：怪物 464、道具 3139 列、技能 597 列、任務 345。這要在 content-editor（後台編輯介面改版）之前完成，編輯器才能依實際內容的欄位與關聯來設計。

## What Changes

- 新增可重跑的匯入腳本，從 `docs/research/wog45/data/*.json` 產生 `backend/content/*.json`。輸出用內容檔的固定格式；同一份輸入跑兩次，結果逐字相同。
- 內容改用原版數值，取代自訂數值。id 改成原版編號（`monster-1`、`item-660`、`job-6`、`skill-1`、`mission-1`），同名的道具變體各自保留。
- **技能改成四個部分組成**：
  - 基本資料：名稱、icon、說明、主動或被動、時機、等級上限、前置技能。
  - 發動條件：機率、HP 低於、HP 比對手低、能力比對手高，可多選、全部成立才發動。沒有條件就是常駐。
  - 消耗：HP、SP、金錢，固定值或最大值的百分比，SP 另有「全部」，可多選；不夠就不能發動。
  - 效果：能力增減、追加傷害、連擊、削減 HP、持續傷害、減傷、回復、使對手無法行動、屬性轉換等，可多選搭配。條件成立就是所有效果都發動；部分效果本身可帶機率（追加傷害、降低對手能力、使對手無法行動、封鎖對手技能），發動後才擲。
- 技能本身沒有分類，分類只是效果種類的分組。每一級的數值分開設定，Domain 不再依等級套公式；現有 16 種 `SkillEffectKind` 與寫死在 `SkillCaster` 的數值移除。
- 主動與被動規則完全相同，只差欄位：主動最多 3 個、被動最多 2 個。
- 職業與技能改用獨立的關聯表 `job-skills.json` 連結，取代技能原本的職業欄位（之後怪物也用關聯表，帶等級）；學習要求關聯表有「目前職業＋技能」這一列，並達到前置技能的等級。
- 效果文字由條件、消耗、效果自動產生，顯示在技能頁與戰鬥紀錄；`description` 只是技能的文字說明。
- 原版 88 種技能匯入成條件、消耗、效果的組合，每級的數值取自原版各級的說明文字。
- 只匯入規則支援的欄位與紀錄。下列項目寫進 `docs/research/wog45/gaps.md`，附原因，這份清單就是後續補規則變更的範圍：
  - 不支援的欄位：怪物技能與必殺技、怪物的能力值欄位、多個擊殺目標、交寵物等。
  - 現有內容驗證會拒絕的紀錄，例如等級 0 的 NPC。
  - 這次不匯入的技能：寵物技能（等寵物系統調整）、組隊技能（本作沒有隊友，不做）、機率隨條件變動的假死。
- 原版沒有對應內容的自訂內容照舊保留，每一筆都在 `gaps.md` 寫明保留原因：旅館、冠軍種子、區域分組與推薦等級，以及現有功能需要、但原版沒有的道具。
- 取代以下裁定（`docs/superpowers/specs/2026-10-22-wog-remake-rulings.md`）中，因 WOG 資料遺失而自訂的內容：
  - 技能每級數值公式（第 46 行）：原版有的欄位改用原版。
  - 技能發動條件只有 `chance`、`hpBelow` 二擇一（第 48 行）：改成每級的條件清單，可以同時設多個；冷卻規則不變。
  - 裝備、補給品、精鍊石的數值公式（第 60 行）。
  - 一般任務內容（第 68 行）。
  - 勳章來源（第 76 行）：改用原版掉落。
  - 榮譽裝備不做（第 77 行）：原版榮譽商店有這些商品，可以照現有兌換格式匯入。
  - 新消耗品的名稱與數值（第 97 行）：原版有對應的改用原版。
- **BREAKING**：所有內容 id 都改變，現有的本機玩家存檔不再相容，要重建資料庫。新增一個 migration，把 `characters.job_id` 的預設值從 `warrior` 改成 `job-6`。
- 釘住自訂內容表的測試改寫成釘住匯入結果，包括 `MonsterRosterTests`、`ItemTableTests`、`MissionTableTests`、`CompanionTableTests`。`BalanceTests` 照舊執行；換成原版數值後若不通過，回報使用者決定，不自行放寬門檻。

## Non-Goals (optional)

- 其他補規則（怪物技能與必殺技、寵物技能、任務條件等）：依 `gaps.md` 另開後續變更。怪物技能沿用這次的效果分類。
- content-editor：等搬移與補規則都完成後再做，屆時用 `/spectra-ingest` 依新內容更新它的範例與欄位。

## Capabilities

### New Capabilities

- `wog-content-import`: 從 WOG 4.5 原版資料產生正式內容檔、列出規則還不支援的資料，以及保留自訂內容的規則
- `skill-effects`: 技能由基本資料、發動條件、消耗、效果四個部分組成，數值逐級寫在內容檔，職業與技能用關聯表連結，戰鬥只依條件、消耗、效果的種類與數值計算

### Modified Capabilities

(none)

## Impact

- Affected specs: `wog-content-import`（新增）、`skill-effects`（新增）
- Affected code:
  - New: backend/content/job-skills.json, backend/EndlessBattle.Tests/TestContent/job-skills.json, scripts/import-wog45.py, docs/research/wog45/gaps.md, backend/db/ddl/V000000016__default_job_id.sql
  - Modified: backend/content/items.json, backend/content/monsters.json, backend/content/jobs.json, backend/content/skills.json, backend/content/missions.json, backend/content/recipes.json, backend/content/honour-shop.json, backend/content/mercenaries.json, backend/content/world.json
  - Modified tests: backend/EndlessBattle.Tests/Content/MonsterRosterTests.cs, backend/EndlessBattle.Tests/Content/ItemTableTests.cs, backend/EndlessBattle.Tests/Content/MissionTableTests.cs, backend/EndlessBattle.Tests/Content/CompanionTableTests.cs, backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - New tests: backend/EndlessBattle.Tests/Content/WogImportTests.cs, backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - Domain: backend/EndlessBattle.Domain/Skills/SkillDefinition.cs, backend/EndlessBattle.Domain/Battle/SkillCaster.cs, backend/EndlessBattle.Domain/Battle/BattleModifiers.cs, backend/EndlessBattle.Domain/Battle/RoundResolver.cs, backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - Application / Api: backend/EndlessBattle.Application/Skills/SkillOverview.cs, backend/EndlessBattle.Application/Skills/SkillService.cs, backend/EndlessBattle.Api/Endpoints/Contracts.cs（技能回應改成 `slotType`、`summary`、`description`、`icon`）；Domain 另含 backend/EndlessBattle.Domain/Skills/SkillLearning.cs、backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - Infrastructure: backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs（技能的讀取與驗證）
  - Test content: backend/EndlessBattle.Tests/TestContent/skills.json 改成新格式，參數與舊效果等價；用到技能的戰鬥測試照舊要通過
  - frontend: frontend/src/api/schema.ts 重新產生；frontend/src/ui/SkillsScreen.tsx 改顯示 `summary`、frontend/src/ui/BattleLog.tsx 顯示技能消耗
  - Modified: docs/research/README.md, docs/research/wog45/README.md, docs/research/monster-roster.md, docs/research/item-table.md, docs/research/mission-table.md, docs/research/companion-table.md, docs/research/job-growth-expectation.md
- Not affected: admin（技能表單留給 content-editor）
