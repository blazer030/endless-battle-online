# weapon-skills Specification

## Purpose

A weapon can carry a skill with its own activation rate. While the weapon is equipped, the skill works like an extra skill slot that costs no SP, and the bag shows it on the weapon.

## Requirements

### Requirement: Weapons can carry a skill

An item SHALL be able to declare a weapon skill with a skill id, level and activation rate. Loading SHALL be rejected with a `GameContentException` when the item is not a weapon, when the skill is unknown, when the level is outside 1 to the skill's maximum level, or when the rate is outside 1 to 100.

#### Scenario: A weapon's skill is loaded

- **GIVEN** `item-1823` 爆氣火斧 with weapon skill `skill-1`, level 2, rate 12
- **WHEN** the content loads
- **THEN** the item has that weapon skill

#### Scenario: Invalid weapon skills are rejected

##### Example: rejected declarations

| Declaration | Reason |
| --- | --- |
| weapon skill on a head item | not a weapon |
| skill `skill-x` | unknown skill |
| level 0 | level below 1 |
| level above the skill's maximum level | level too high |
| rate 0 | rate below 1 |
| rate 101 | rate above 100 |

- **WHEN** the content loads with one of these declarations
- **THEN** loading throws a `GameContentException` that names the item


<!-- @trace
source: monster-skills
updated: 2026-11-07
code:
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Application/Skills/SkillSummary.cs
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - docs/research/wog45/gaps.md
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Tests/TestContent/monster-skills.json
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Domain/Skills/SkillLoadout.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Application/Skills/EquippedSkills.cs
  - backend/content/job-skills.json
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillParts.cs
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Infrastructure/Content/SkillPartsReader.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Skills/SkillDefinition.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - backend/content/items.json
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Tests/Application/SkillSummaryTests.cs
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/content/monster-skills.json
  - backend/content/skills.json
  - frontend/src/api/schema.ts
  - frontend/src/game/battleLog.ts
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - README.md
tests:
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/bagWidgets.test.tsx
  - frontend/src/game/stageModel.test.ts
-->

---
### Requirement: An equipped weapon's skill works without SP

While a weapon with a skill is equipped, the skill SHALL be tried after the character's five skill slots, in adventure and mercenary fights. The weapon's rate SHALL replace the skill's `Chance` conditions, its other conditions SHALL still apply, and its `SkillPoints` costs SHALL NOT be paid. The character SHALL NOT need to have learned the skill. When the same skill is in one of the five slots, the weapon's skill SHALL NOT take effect. The weapon's skill SHALL NOT count as an active skill for the rest threshold on SP.

#### Scenario: The weapon's skill activates at its rate with no SP

- **GIVEN** an equipped weapon with 爆氣 level 2 at rate 12, a character with 0 SP who has not learned 爆氣
- **WHEN** the activation roll is 12
- **THEN** 爆氣 level 2 activates and the skill event has SP cost 0

#### Scenario: An unequipped weapon's skill does not work

- **GIVEN** the same weapon in the bag, not equipped
- **WHEN** a fight runs
- **THEN** no skill event for 爆氣 is recorded

#### Scenario: The same skill in a slot takes priority

- **GIVEN** an equipped weapon with 爆氣 level 2 at rate 12 and 爆氣 level 5 in slot 1
- **WHEN** the battle skills are loaded
- **THEN** 爆氣 appears once, at level 5, in slot 1

#### Scenario: The weapon's skill does not trigger the SP rest threshold

- **GIVEN** a character whose only skill is an equipped weapon's active skill, and whose SP is below the rest threshold
- **WHEN** the adventure decides whether to rest
- **THEN** the character does not rest because of SP


<!-- @trace
source: monster-skills
updated: 2026-11-07
code:
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Application/Skills/SkillSummary.cs
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - docs/research/wog45/gaps.md
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Tests/TestContent/monster-skills.json
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Domain/Skills/SkillLoadout.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Application/Skills/EquippedSkills.cs
  - backend/content/job-skills.json
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillParts.cs
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Infrastructure/Content/SkillPartsReader.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Skills/SkillDefinition.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - backend/content/items.json
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Tests/Application/SkillSummaryTests.cs
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/content/monster-skills.json
  - backend/content/skills.json
  - frontend/src/api/schema.ts
  - frontend/src/game/battleLog.ts
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - README.md
tests:
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/bagWidgets.test.tsx
  - frontend/src/game/stageModel.test.ts
-->

---
### Requirement: The bag shows a weapon's skill

The inventory API SHALL return a weapon's skill name, level and rate, and null when it has none. The bag's item detail SHALL show them, and the panel of an equipment slot SHALL show the detail of the item equipped there.

#### Scenario: The bag shows the skill

- **GIVEN** a weapon with 爆氣 level 2 at rate 12
- **WHEN** its detail is shown in the bag
- **THEN** the text `附帶技能 爆氣 Lv2（發動率 12%）` is shown

#### Scenario: The panel of the weapon slot shows the equipped weapon's skill

- **GIVEN** an equipped weapon with 爆氣 level 2 at rate 12
- **WHEN** the weapon slot is opened on the equipment page
- **THEN** the text `附帶技能 爆氣 Lv2（發動率 12%）` is shown

#### Scenario: A weapon without a skill shows no line

- **GIVEN** a weapon with no skill
- **WHEN** its detail is shown in the bag
- **THEN** no 附帶技能 text is shown

<!-- @trace
source: monster-skills
updated: 2026-11-07
code:
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Application/Skills/SkillSummary.cs
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - docs/research/wog45/gaps.md
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - backend/EndlessBattle.Tests/TestContent/monster-skills.json
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Domain/Skills/SkillLoadout.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Application/Skills/EquippedSkills.cs
  - backend/content/job-skills.json
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - backend/EndlessBattle.Domain/Skills/SkillParts.cs
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Infrastructure/Content/SkillPartsReader.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Skills/SkillDefinition.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - backend/content/items.json
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Tests/Application/SkillSummaryTests.cs
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - scripts/import-wog45.py
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/content/monster-skills.json
  - backend/content/skills.json
  - frontend/src/api/schema.ts
  - frontend/src/game/battleLog.ts
  - backend/EndlessBattle.Tests/Content/ImportScriptTests.cs
  - README.md
tests:
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/bagWidgets.test.tsx
  - frontend/src/game/stageModel.test.ts
-->