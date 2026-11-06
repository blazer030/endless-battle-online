# wog-content-import Specification

## Purpose

Generate the official game content from the recovered WOG 4.5 data, list the original data the current rules cannot use, and keep custom content only where the original has nothing comparable.

## Requirements

### Requirement: Official content comes from the WOG 4.5 records

The official content files in `backend/content/` SHALL be generated from `docs/research/wog45/data/*.json` by `scripts/import-wog45.py`. Imported records SHALL keep the original values and SHALL use ids of the form `<kind>-<original id>` (`monster-1`, `item-660`, `job-6`, `skill-<id of level 1 row>`, `mission-1`, `recipe-1`, `honour-1`, `mercenary-1`). Item variants that share a name SHALL remain separate records.

#### Scenario: A regular monster keeps its original values

- **WHEN** the official content is loaded after the import
- **THEN** each imported monster has the level, hit points, attack, defence, element, place, sub-places and drops of its original record

##### Example: slime

- **GIVEN** the original monster 1 史萊姆 with level 2, hp 32, element 風, place 1, sub-place 香茅綠地, and a drop of item 204 錢包 with topr 90
- **WHEN** the official content is loaded
- **THEN** monster `monster-1` is named 史萊姆 with level 2, hp 32, element 5, placeId 1, subPlaces `[1]` (香茅綠地 is sub-place 1 of 中央平原), and a drop of `item-204` with topr 90

#### Scenario: Item variants stay separate

- **WHEN** two original items share the name 木刀 with ids 660 and 928
- **THEN** the official content holds both `item-660` and `item-928`, each with its own original values

#### Scenario: A job keeps its original growth

- **GIVEN** the original job 6 戰士 with strength growth `"1,3"` and no parent job
- **WHEN** the official content is loaded
- **THEN** job `job-6` is a tier 1 job whose strength growth is `[1, 3]`

#### Scenario: A delivery mission keeps its original goal and reward

- **GIVEN** the original mission 1 製作漁網 that needs one 蜘蛛絲 (item 331) and rewards one 白色布料 (item 342)
- **WHEN** the official content is loaded
- **THEN** mission `mission-1` is titled 製作漁網, delivers one `item-331`, and rewards one `item-342`


<!-- @trace
source: wog45-content-import
updated: 2026-11-06
code:
  - docs/research/README.md
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Api/Endpoints/SkillEndpoints.cs
  - backend/content/monsters.json
  - backend/EndlessBattle.Tests/Domain/Skills/SkillLearningTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/ContentFileFormatterTests.cs
  - docs/research/mission-table.md
  - backend/EndlessBattle.Tests/Domain/Honour/HonourShopTests.cs
  - backend/EndlessBattle.Domain/Forge/ForgeTools.cs
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - docs/research/companion-table.md
  - backend/EndlessBattle.Domain/Battle/BattleOutcome.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - frontend/src/game/stageModel.ts
  - frontend/src/ui/CreateCharacterScreen.tsx
  - backend/EndlessBattle.Application/Content/IGameContent.cs
  - docs/research/wog45/export.py
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - docs/research/wog45/gaps.md
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Domain/Skills/SkillParts.cs
  - docs/research/monster-roster.md
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillLearning.cs
  - backend/EndlessBattle.Application/Skills/SkillSummary.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - docs/research/item-table.md
  - backend/content/items.json
  - backend/EndlessBattle.Tests/Application/SkillServiceTests.cs
  - backend/EndlessBattle.Tests/Support/SkillPreset.cs
  - frontend/src/game/battleLog.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - backend/EndlessBattle.Tests/Content/MonsterRosterTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillDefinition.cs
  - frontend/src/game/skills.ts
  - frontend/src/ui/SkillsScreen.tsx
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - backend/EndlessBattle.Tests/TestContent/job-skills.json
  - docs/research/wog45/README.md
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Support/CustomContent.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/content/honour-shop.json
  - backend/content/job-skills.json
  - backend/EndlessBattle.Infrastructure/Content/SkillPartsReader.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillDefinitionTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Support/TestSkills.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - backend/EndlessBattle.Application/Skills/SkillOverview.cs
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Content/MissionTableTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Application/SkillSummaryTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/content/jobs.json
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/content/missions.json
  - backend/content/skills.json
  - backend/EndlessBattle.Application/Skills/SkillService.cs
  - backend/db/ddl/V000000016__default_job_id.sql
  - backend/EndlessBattle.Tests/Api/SkillEndpointsTests.cs
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - docs/research/job-growth-expectation.md
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/content/mercenaries.json
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Content/CompanionTableTests.cs
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentUpdateTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillLoadoutTests.cs
  - backend/content/recipes.json
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Tests/Content/ItemTableTests.cs
  - backend/EndlessBattle.Tests/TestContent/skills.json
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
tests:
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battleLog.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/CreateCharacterScreen.test.tsx
-->

---
### Requirement: Importing is repeatable

Running the import script twice on the same input SHALL produce byte-identical content files and gap list. The content files SHALL be written in the canonical content file format.

#### Scenario: Second run changes nothing

- **WHEN** the import script runs, and then runs again without any change to its input
- **THEN** `git diff --exit-code backend/content docs/research/wog45/gaps.md` exits 0 after the second run

#### Scenario: Output is in canonical format

- **WHEN** the import script writes `backend/content/monsters.json`
- **THEN** formatting the file with `ContentFileFormatter` leaves it byte-identical

#### Scenario: Missing source field stops the import

- **WHEN** an original record lacks a field the script needs, such as a monster without `hp`
- **THEN** the script exits with a non-zero code, prints the file name and record id, and writes no files


<!-- @trace
source: wog45-content-import
updated: 2026-11-06
code:
  - docs/research/README.md
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Api/Endpoints/SkillEndpoints.cs
  - backend/content/monsters.json
  - backend/EndlessBattle.Tests/Domain/Skills/SkillLearningTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/ContentFileFormatterTests.cs
  - docs/research/mission-table.md
  - backend/EndlessBattle.Tests/Domain/Honour/HonourShopTests.cs
  - backend/EndlessBattle.Domain/Forge/ForgeTools.cs
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - docs/research/companion-table.md
  - backend/EndlessBattle.Domain/Battle/BattleOutcome.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - frontend/src/game/stageModel.ts
  - frontend/src/ui/CreateCharacterScreen.tsx
  - backend/EndlessBattle.Application/Content/IGameContent.cs
  - docs/research/wog45/export.py
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - docs/research/wog45/gaps.md
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Domain/Skills/SkillParts.cs
  - docs/research/monster-roster.md
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillLearning.cs
  - backend/EndlessBattle.Application/Skills/SkillSummary.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - docs/research/item-table.md
  - backend/content/items.json
  - backend/EndlessBattle.Tests/Application/SkillServiceTests.cs
  - backend/EndlessBattle.Tests/Support/SkillPreset.cs
  - frontend/src/game/battleLog.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - backend/EndlessBattle.Tests/Content/MonsterRosterTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillDefinition.cs
  - frontend/src/game/skills.ts
  - frontend/src/ui/SkillsScreen.tsx
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - backend/EndlessBattle.Tests/TestContent/job-skills.json
  - docs/research/wog45/README.md
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Support/CustomContent.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/content/honour-shop.json
  - backend/content/job-skills.json
  - backend/EndlessBattle.Infrastructure/Content/SkillPartsReader.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillDefinitionTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Support/TestSkills.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - backend/EndlessBattle.Application/Skills/SkillOverview.cs
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Content/MissionTableTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Application/SkillSummaryTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/content/jobs.json
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/content/missions.json
  - backend/content/skills.json
  - backend/EndlessBattle.Application/Skills/SkillService.cs
  - backend/db/ddl/V000000016__default_job_id.sql
  - backend/EndlessBattle.Tests/Api/SkillEndpointsTests.cs
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - docs/research/job-growth-expectation.md
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/content/mercenaries.json
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Content/CompanionTableTests.cs
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentUpdateTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillLoadoutTests.cs
  - backend/content/recipes.json
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Tests/Content/ItemTableTests.cs
  - backend/EndlessBattle.Tests/TestContent/skills.json
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
tests:
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battleLog.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/CreateCharacterScreen.test.tsx
-->

---
### Requirement: Data the rules cannot use is listed

Original data that the current rules cannot use SHALL be left out of the content files and listed in `docs/research/wog45/gaps.md` with its original id, name and reason. This SHALL cover:

- unsupported fields;
- records the current content validation would reject;
- skills the effect catalogue cannot express;
- references removed because their target was left out.

The official content SHALL load without errors after every import.

#### Scenario: An NPC with level 0 is listed, not imported

- **GIVEN** an original monster whose level is 0
- **WHEN** the import runs
- **THEN** that monster is absent from `monsters.json`, and `gaps.md` lists its id and name with the reason that level must be at least 1

#### Scenario: Unsupported monster fields are listed

- **GIVEN** the original monster 1 史萊姆 with the special attack 猛烈一擊 and the skill 爆氣 Lv2 at rate 10
- **WHEN** the import runs
- **THEN** `monster-1` is imported without those fields, and `gaps.md` lists 猛烈一擊 and 爆氣 Lv2 rate 10 under monster 1

#### Scenario: A reference to an excluded record is removed and listed

- **WHEN** a mission rewards an item that was excluded from the import
- **THEN** that reward is absent from the mission, and `gaps.md` records the removal and names the excluded item

#### Scenario: Official content loads

- **WHEN** `JsonGameContent.Load` reads `backend/content` after the import
- **THEN** loading succeeds without a `GameContentException`


<!-- @trace
source: wog45-content-import
updated: 2026-11-06
code:
  - docs/research/README.md
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Api/Endpoints/SkillEndpoints.cs
  - backend/content/monsters.json
  - backend/EndlessBattle.Tests/Domain/Skills/SkillLearningTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/ContentFileFormatterTests.cs
  - docs/research/mission-table.md
  - backend/EndlessBattle.Tests/Domain/Honour/HonourShopTests.cs
  - backend/EndlessBattle.Domain/Forge/ForgeTools.cs
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - docs/research/companion-table.md
  - backend/EndlessBattle.Domain/Battle/BattleOutcome.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - frontend/src/game/stageModel.ts
  - frontend/src/ui/CreateCharacterScreen.tsx
  - backend/EndlessBattle.Application/Content/IGameContent.cs
  - docs/research/wog45/export.py
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - docs/research/wog45/gaps.md
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Domain/Skills/SkillParts.cs
  - docs/research/monster-roster.md
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillLearning.cs
  - backend/EndlessBattle.Application/Skills/SkillSummary.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - docs/research/item-table.md
  - backend/content/items.json
  - backend/EndlessBattle.Tests/Application/SkillServiceTests.cs
  - backend/EndlessBattle.Tests/Support/SkillPreset.cs
  - frontend/src/game/battleLog.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - backend/EndlessBattle.Tests/Content/MonsterRosterTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillDefinition.cs
  - frontend/src/game/skills.ts
  - frontend/src/ui/SkillsScreen.tsx
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - backend/EndlessBattle.Tests/TestContent/job-skills.json
  - docs/research/wog45/README.md
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Support/CustomContent.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/content/honour-shop.json
  - backend/content/job-skills.json
  - backend/EndlessBattle.Infrastructure/Content/SkillPartsReader.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillDefinitionTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Support/TestSkills.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - backend/EndlessBattle.Application/Skills/SkillOverview.cs
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Content/MissionTableTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Application/SkillSummaryTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/content/jobs.json
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/content/missions.json
  - backend/content/skills.json
  - backend/EndlessBattle.Application/Skills/SkillService.cs
  - backend/db/ddl/V000000016__default_job_id.sql
  - backend/EndlessBattle.Tests/Api/SkillEndpointsTests.cs
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - docs/research/job-growth-expectation.md
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/content/mercenaries.json
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Content/CompanionTableTests.cs
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentUpdateTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillLoadoutTests.cs
  - backend/content/recipes.json
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Tests/Content/ItemTableTests.cs
  - backend/EndlessBattle.Tests/TestContent/skills.json
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
tests:
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battleLog.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/CreateCharacterScreen.test.tsx
-->

---
### Requirement: Custom content without an original counterpart is kept

Custom content for which the original data has nothing comparable SHALL be kept with its existing id, and every kept record SHALL appear in the "kept custom content" section of `gaps.md` with its reason. This SHALL cover `inn.json`, and the areas, recommended levels and starting place in `world.json`. Custom items, monsters and mercenaries SHALL be kept only when a current feature needs them and no original record fills the same role. All other custom content SHALL be removed. The official content SHALL NOT include a `champion.json`, and the game SHALL load without one.

#### Scenario: Inn stays unchanged

- **WHEN** the import runs
- **THEN** `inn.json` is byte-identical to before the import, and `gaps.md` lists it as kept custom content

#### Scenario: Champion content is gone

- **WHEN** the import runs and `JsonGameContent.Load` reads `backend/content`
- **THEN** no `champion.json` exists in `backend/content`, `gaps.md` does not mention `champion.json`, and loading succeeds without a `GameContentException`

#### Scenario: Custom content with an original counterpart is removed

- **GIVEN** the custom monster `wandering-puff` that is not on the kept list
- **WHEN** the import runs
- **THEN** `wandering-puff` is absent from `monsters.json`

<!-- @trace
source: remove-champion
updated: 2026-11-06
code:
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Infrastructure/CompanionRepositoryTests.cs
  - backend/EndlessBattle.Application/Content/IGameContent.cs
  - backend/EndlessBattle.Tests/Content/CompanionTableTests.cs
  - docs/research/wog45/gaps.md
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - frontend/src/game/types.ts
  - frontend/src/ui/AdventureScreen.css
  - frontend/src/ui/AdventureScreen.tsx
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Infrastructure/DependencyInjection.cs
  - backend/EndlessBattle.Application/DependencyInjection.cs
  - README.md
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/db/ddl/V000000017__drop_character_champions.sql
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/Models/Character.cs
tests:
  - frontend/src/test/fixtures.ts
  - frontend/src/api/client.test.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/ui/App.test.tsx
-->