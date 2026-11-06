## Why

目前戰鬥只有玩家會放技能，怪物只有普通攻擊：原版 391 隻已匯入的怪物裡有 390 隻帶技能（共 576 格），36 件武器也附帶技能，這些資料都還躺在 `docs/research/wog45/gaps.md`。怪物沒有技能時，後段地區的戰鬥跟原版差很多，封鎖、削減 SP 這類技能也完全沒有作用對象。這是補規則的第一步，任務條件（`mission-objectives`）接在這之後。

## What Changes

- 怪物會使用技能：新增內容檔 `backend/content/monster-skills.json`，記錄怪物、技能、等級與發動率。怪物與玩家用同一套施放規則，雙方的增減益、回復、削減、持續傷害、暈眩與封鎖都互相作用。
- 發動率取代 `Chance` 條件：怪物與武器的技能以自己的發動率取代技能的 `Chance` 條件，其他條件照舊。怪物要付 SP 與 HP 消耗、不付金錢；技能效果自己的機率（例如暈擊的 3%）發動後照擲，與原版一樣是兩段機率。
- 封鎖技能真的生效：`Silence` 效果目前沒有接進戰鬥，這次接上，玩家與怪物都會被封鎖。
- 匯入假死：`Stun` 效果可以依「自己的某項能力是否比對手高」調整機率，假死從排除清單移出，匯入成刺客的技能，玩家也能學。
- 武器附帶技能：`items.json` 的武器可以附帶技能與發動率，裝備時當成額外一欄生效，不耗 SP；33 件武器搬入，附帶寵物技能戰意驅使的 3 件翼龍獸矛繼續列在 `gaps.md`。背包的武器明細顯示附帶技能。
- 戰鬥畫面與戰報顯示怪物施放的技能、怪物的回復與玩家受到的技能傷害；玩家被暈眩時顯示無法行動。
- 匯入腳本產出上述資料，`gaps.md` 不再列這些欄位；同一隻怪重複的技能只留等級較高的一筆並列入 `gaps.md`。
- 推翻裁定彙整 `docs/superpowers/specs/2026-10-22-wog-remake-rulings.md` 第 80 行「怪物側沒有技能引擎」的簡化（「冠軍沒有技能」本身已隨冠軍移除）。

## Non-Goals

- 裝備回復 HP／SP（`gaps.md` 的「裝備回復」欄位）：原版規則還沒查，之後再做。
- 寵物技能與組隊技能：維持排除；所以翼龍獸矛的戰意驅使不搬。
- 不調整任何怪物或玩家的數值。怪物變強造成的平衡問題由負責平衡的人處理，這次只量測並記錄。
- 必殺技：已由 `remove-special-attack-name` 決定不搬。
- 平衡報告不加武器附帶技能；報告改版另開變更。
- admin 不新增 `monster-skills.json` 的編輯頁，留給 `content-editor`。

## Capabilities

### New Capabilities

- `monster-skills`: 怪物的技能資料、發動率、雙方技能互相作用的戰鬥規則，以及戰鬥畫面與戰報的呈現
- `weapon-skills`: 武器附帶技能的資料、裝備時的生效規則與背包顯示

### Modified Capabilities

- `skill-effects`: `Stun` 效果可以依能力比較調整機率（假死）；`Silence` 效果在戰鬥中實際封鎖對手
- `wog-content-import`: 匯入怪物技能、武器附帶技能與假死，`gaps.md` 改列仍然沒搬的部分

## Impact

- Affected specs: `docs/spectra/specs/skill-effects/spec.md`、`docs/spectra/specs/wog-content-import/spec.md`，新增 `monster-skills`、`weapon-skills`
- Domain: `backend/EndlessBattle.Domain/Battle/WogBattle.cs`、`SkillCaster.cs`、`BattleModifiers.cs`，`Skills/SkillParts.cs`、`Skills/SkillDefinition.cs`，`Content/MonsterDefinition.cs`，`Items/ItemDefinition.cs`，`Adventure/AdventureSimulator.cs`，`Mercenaries/MercenarySimulator.cs`
- Application: `backend/EndlessBattle.Application/Skills/EquippedSkills.cs`、`Skills/SkillSummary.cs`，`Adventure/AdventureService.cs`
- Infrastructure: `backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs`
- Api: `backend/EndlessBattle.Api/Endpoints/Contracts.cs`（`InventoryItemResponse` 多附帶技能）
- Content: 新增 `backend/content/monster-skills.json`，更新 `items.json`、`skills.json`、`job-skills.json`；測試內容新增 `backend/EndlessBattle.Tests/TestContent/monster-skills.json`
- Tests: `backend/EndlessBattle.Tests/Domain/Battle/`、`Domain/Skills/`、`Infrastructure/JsonGameContentTests.cs`、`Content/WogImportTests.cs`、`Content/BalanceTests.cs`
- Frontend: `frontend/src/game/stageModel.ts`、`frontend/src/game/battleLog.ts`、`frontend/src/ui/bagWidgets.tsx`、`frontend/src/api/schema.ts`（重新產生）
- 匯入與文件: `scripts/import-wog45.py`、`docs/research/wog45/gaps.md`、`README.md`
- 沒有資料表變更
