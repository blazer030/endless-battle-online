# skill-effects Specification

## Purpose

A skill is assembled from four parts — basic data, activation conditions, costs and effects — whose values are set level by level in the content files. Designers combine conditions, costs and effects freely, jobs choose which skills they teach, and battle computes results only from the kinds and values of those parts.

## Requirements

### Requirement: Skills are assembled from conditions, costs and effects

Every skill SHALL have basic data: name, optional icon, description, slot type (Active or Passive), timing (BeforeAttack, AfterHit or RoundEnd), `maxLevel` and an optional prerequisite. Every level SHALL list zero or more conditions, zero or more costs and one or more effects, and each of them SHALL have a kind and values. Skills SHALL NOT have a category; categories SHALL only group effect kinds. Every level of a skill SHALL list the same kinds of conditions, costs and effects in the same order.

#### Scenario: A skill combines several effects

- **GIVEN** a skill level with effects `BonusDamage` physical strength × 45% and `StatChange` caster hitRate −70
- **WHEN** the skill activates for a player with strength 100
- **THEN** that attack deals 45 extra damage and the player's hit rate is lowered by 70 for that round

#### Scenario: A buff raises attack by its level's percent

- **GIVEN** the skill `skill-1` 爆氣 whose level 3 effect is `StatChange` caster attack percent 3 for 3 rounds
- **WHEN** a player with attack 200 activates level 3 of it
- **THEN** the player's attack is 206 in that round and the next two rounds


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Same parts give the same battle result

Battle SHALL compute skill results only from the kinds and values of conditions, costs and effects. Two skills with equal levels SHALL produce identical battle results under the same random seed, whether each is active or passive. The slot type SHALL only decide the slot group: at most 3 active and 2 passive skills are equipped.

#### Scenario: Two skills with equal parts behave the same

- **GIVEN** 破滅一擊 level 1 and 爆氣 level 5, both with `StatChange` caster attack 5%
- **WHEN** the same player fights the same monster with the same seed, once with each skill
- **THEN** both battles produce the same events

#### Scenario: Active and passive skills behave the same

- **GIVEN** two skills with identical levels, one active and one passive
- **WHEN** the same player fights the same monster with the same seed, once with each skill equipped
- **THEN** both battles produce the same events, including the SP spent

#### Scenario: Changing a value in content changes the result without code changes

- **WHEN** the percent of a `StatChange` attack effect in the test content changes from 3 to 6
- **THEN** the player's attack for that round changes from 206 to 212 with no change to Domain code


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Activation conditions are a list

Condition kinds SHALL be `Chance`, `HitPointsBelow`, `HitPointsBelowOpponent` and `StatAbove`. A skill SHALL activate only when all of its conditions hold, and all of its effects SHALL then apply. An effect kind whose catalogue entry has a `chance` value (`StatChange` on the opponent, `BonusDamage`, `Stun` and `Silence`) SHALL take effect only when its own roll succeeds, and this roll SHALL NOT affect the skill's other effects. `StatChange` and `BonusDamage` SHALL roll once after the skill activates; `Stun` SHALL roll before each of the opponent's actions while it lasts; `Silence` SHALL keep its chance for when monsters use skills. A level with no conditions SHALL be always on: it applies without a roll, and SHALL have no costs, zero effect rounds and zero cooldown. While any effect of an activated skill is still lasting, the skill SHALL NOT activate again, and no condition SHALL be checked or rolled for it. Checks SHALL run in this order: lasting or cooling down, conditions that need no roll, affordability of costs, then `Chance`; a failing check SHALL stop the rest, so no random number is drawn.

#### Scenario: Two conditions must both hold

- **GIVEN** a skill level with conditions `HitPointsBelow` 30 and `Chance` 30
- **WHEN** the player is at 50% HP
- **THEN** the skill does not activate and no random number is drawn for it

##### Example: combined conditions

| Player HP | Roll (1–100) | Activates |
| --------- | ------------ | --------- |
| 50% | not drawn | no |
| 20% | 25 | yes |
| 20% | 31 | no |

#### Scenario: Chance covers all effects

- **GIVEN** 施毒 with condition `Chance` 25 and effects `StatChange` opponent attack −8% and `DamageOverTime` intelligence × 10%
- **WHEN** the roll is 20
- **THEN** both effects apply

#### Scenario: An effect's own chance is rolled after activation

- **GIVEN** 斯巴達 with conditions `StatAbove` strength and `Chance` 40, and effects `StatChange` caster physicalCriticalChance +20 and `Stun` chance 12
- **WHEN** the player's strength is above the monster's, the activation roll is 30 and the stun roll is 50
- **THEN** the critical chance rises by 20 and the monster is not stunned

#### Scenario: A silenced side does not use skills

- **GIVEN** a combatant hit by `Silence` lasting 2 rounds, whose own skill has condition `Chance` 100
- **WHEN** the next two rounds are resolved
- **THEN** that combatant's skill does not activate and no random number is drawn for it, and it activates again in the round after

#### Scenario: A chance on an effect kind without chance support is rejected

- **WHEN** a `MultiHit` effect has a `chance` value
- **THEN** loading fails with a `GameContentException` that names the skill id and level

#### Scenario: No conditions means always on

- **GIVEN** a passive skill whose levels have no conditions
- **WHEN** the player enters a battle with it equipped
- **THEN** its effects apply from the first round without any roll

#### Scenario: A lasting skill does not activate again

- **GIVEN** a skill level with conditions `HitPointsBelow` 50 and `Chance` 100, and an effect lasting 3 rounds
- **WHEN** it activates in round 1 and the player's HP stays below 50% through round 4
- **THEN** it does not activate in rounds 2 and 3, no random number is drawn for it in those rounds, and it activates again in round 4

#### Scenario: A stat condition blocks the skill

- **GIVEN** 劍雨 with conditions `StatAbove` magicAttack and `Chance` 40
- **WHEN** the player's magic attack is 50 and the monster's magic attack is 60
- **THEN** the skill does not activate, no SP is spent, and no random number is drawn for it

#### Scenario: An always-on level cannot have costs

- **WHEN** a skill level lists no conditions and a `SkillPoints` cost of 3
- **THEN** loading fails with a `GameContentException` that names the skill id and level


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Costs block activation when unaffordable

Cost kinds SHALL be `HitPoints` (amount or percent of maximum HP), `SkillPoints` (amount, percent of maximum SP, or all) and `Money` (amount). A skill SHALL NOT activate when any cost is unaffordable: HP SHALL stay at least 1 after paying, SP and money SHALL cover the amount, and `all` SHALL need at least 1 SP. Effect values SHALL be computed before the costs are paid. The battle log SHALL show the costs paid for each activation. A monster SHALL pay no money cost.

#### Scenario: A hit point cost is paid on activation

- **GIVEN** a skill level with cost `HitPoints` percent 45, used by a player with maximum HP 1000 and current HP 1000
- **WHEN** the skill activates
- **THEN** the player's HP becomes 550 before the attack, and the battle log shows HP 450 spent

#### Scenario: An unaffordable cost blocks the skill

- **GIVEN** a skill level with cost `HitPoints` percent 45, used by a player with maximum HP 1000 and current HP 400
- **WHEN** its conditions hold
- **THEN** the skill does not activate and no random number is drawn for it

#### Scenario: Effect values use resources before the cost

- **GIVEN** 魔力爆發 with cost `SkillPoints` all and effect `StatChange` caster magicAttack scaling skillPoints × 0.1
- **WHEN** a player with 300 SP and magic attack 100 activates it
- **THEN** the magic attack for that round is 130 and SP becomes 0


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Effect stacking

Within one skill, percentages on the same stat SHALL add up. Between different skills, percentages SHALL multiply. Flat amounts and scaled amounts SHALL always add up.

#### Scenario: Same skill adds, different skills multiply

- **GIVEN** a player with attack 200
- **WHEN** one skill gives attack +5% twice, and separately two skills each give attack +5%
- **THEN** the first case gives attack 220 and the second gives attack 221


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Every level is set separately up to the maximum level

Every skill SHALL declare `maxLevel`, and its levels SHALL be exactly levels 1 through `maxLevel`. Each level SHALL carry its own learning money, proficiency, cooldown, condition values, cost values and effect values. Domain SHALL NOT derive any skill value from the level number.

#### Scenario: Levels must match the maximum level

- **WHEN** a skill declares `maxLevel` 5 and lists levels 1 through 4
- **THEN** loading fails with a `GameContentException` that names the skill id

#### Scenario: A level's values are used as written

- **GIVEN** a skill whose level 2 has `Chance` 70 and `StatChange` attack percent 9, while level 1 has `Chance` 40 and percent 1
- **WHEN** a player with attack 200 has learned level 2
- **THEN** the activation roll uses 70 and the attack for that round is 218


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Jobs choose their skills

Skills SHALL NOT name a job, and jobs SHALL NOT list skills. Which job teaches which skill SHALL be a row in the job–skill relation table `job-skills.json`; one skill can appear in rows for several jobs, and a job–skill pair SHALL NOT repeat. Loading SHALL fail when a row names a missing job or skill, or repeats a pair. A character SHALL learn a skill only when a row pairs the character's current job with that skill and its prerequisite skill has reached the required level; learning SHALL spend the current job's skill points.

#### Scenario: A skill listed by the current job can be learned

- **GIVEN** a `job-skills.json` row pairs `job-6` 戰士 with `skill-1` 爆氣, and a 戰士 character with enough money and skill points
- **WHEN** the character learns 爆氣 level 1
- **THEN** learning succeeds and the 戰士 skill points drop by level 1's proficiency

#### Scenario: A skill not listed by the current job is refused

- **WHEN** a 戰士 character tries to learn a skill that no `job-skills.json` row pairs with 戰士
- **THEN** learning is refused with reason `wrongJob`

#### Scenario: A relation row naming a missing skill is rejected

- **WHEN** a `job-skills.json` row pairs `job-6` with a skill id that does not exist
- **THEN** loading fails with a `GameContentException` that names the row's job and skill

#### Scenario: A missing prerequisite is refused

- **GIVEN** a skill whose prerequisite is 爆氣 level 3, and a character who has 爆氣 level 2
- **WHEN** the character tries to learn it
- **THEN** learning is refused with reason `prerequisite`


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Effect text is generated from the parts

The game API SHALL return, for each skill, an effect summary generated from the conditions, costs and effects of the current level and of the next level, together with the skill's description text. The description SHALL be flavour text only and SHALL NOT affect battle.

#### Scenario: Summary reflects the level's values

- **GIVEN** 爆氣 level 1 with condition `Chance` 40, cost `SkillPoints` 2 and effect `StatChange` caster attack +1% for 3 rounds
- **WHEN** the skill list is requested
- **THEN** the level 1 summary reads 機率 40%｜消耗 SP 2｜自己物攻 +1%，3 回合


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Invalid skills are rejected at load

Content loading SHALL fail with a message that names the skill and level when a condition, cost or effect kind is unknown, a required value is missing or out of range, levels list different kinds, an always-on level has costs, effect rounds or cooldown, or a prerequisite names a missing skill or level.

#### Scenario: Missing value is rejected

- **WHEN** a skill level has a `StatChange` effect without any of `percent`, `amount` or `scaling`
- **THEN** loading fails with a `GameContentException` that names the skill id and the level

#### Scenario: Levels with different effect kinds are rejected

- **WHEN** level 1 of a skill lists `StatChange` and level 2 lists `Restore`
- **THEN** loading fails with a `GameContentException` that names the skill id and level 2


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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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
### Requirement: Original skills are imported as combinations

The import script SHALL import each original skill as conditions, costs and effects, with per-level values taken from that level's original description. Activation SHALL follow the current rules:

- A chance in the description that covers all effects SHALL become the `Chance` condition; otherwise active skills SHALL get `Chance` `min(100, 30 + 10·level)`.
- Original passive skills without a stated condition SHALL be always on.
- The original SP SHALL become a `SkillPoints` cost.
- `job-skills.json` rows SHALL come from the original job of each skill, and the original prerequisite SHALL become the prerequisite.

A skill the model cannot express SHALL be left out and listed in `docs/research/wog45/gaps.md` with its reason. A level whose description does not match the skill's template SHALL stop the import with a non-zero exit code.

#### Scenario: Burst levels follow their descriptions

- **GIVEN** the original 爆氣 levels whose descriptions read 物攻上升1% through 物攻上升10%
- **WHEN** the import runs
- **THEN** `skill-1` level n has condition `Chance` `min(100, 30 + 10·n)` and the single effect `StatChange` caster attack percent n

#### Scenario: Activation follows the current rules, not the original

- **GIVEN** the original passive skill 石人陣 with passive rate 28% and duration 4
- **WHEN** the import runs
- **THEN** 石人陣 is always on, its levels have no conditions and no costs, and `gaps.md` notes that its original passive rate 28% is not used

#### Scenario: A stun chance becomes the effect's own chance

- **GIVEN** the original 暈擊 level 1 whose description reads 3%機率使敵人無法動作
- **WHEN** the import runs
- **THEN** 暈擊 level 1 has condition `Chance` 40 and the effect `Stun` with chance 3

#### Scenario: A chance on bonus damage becomes the effect's own chance

- **GIVEN** the original 暗器 level 1 whose description reads 提升物攻必殺技機率40%，20%機率增加物攻固定傷害(敏捷*15%)，迴避力降低80%
- **WHEN** the import runs
- **THEN** 暗器 level 1 has the effects `StatChange` caster physicalCriticalChance +40, `BonusDamage` physical agility × 15% with chance 20, and `StatChange` caster evasion −80

#### Scenario: A pet skill is listed instead of imported

- **WHEN** the import meets the original skill 物攻命令
- **THEN** no skill named 物攻命令 is in `skills.json`, and `gaps.md` lists it with the reason that pet skills are not supported yet

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
  - backend/EndlessBattle.Tests/Domain/Champion/ChampionDuelTests.cs
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