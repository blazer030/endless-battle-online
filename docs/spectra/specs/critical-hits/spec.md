# critical-hits Specification

## Purpose

Critical hits are shown by the damage number on the battle stage and by the word 暴擊 in the battle log. No combatant has a named special attack, so players have nothing to fill in and battle data carries no special attack name.

## Requirements

### Requirement: Critical hits are shown by the damage number

The battle stage SHALL mark a critical hit only through its damage number, and SHALL show no plate for it. The damage number and hit shake of a critical hit SHALL be the same whichever side attacks, apart from the side that takes the damage. A pet's attack SHALL follow the same rule. Non-critical hits SHALL use the normal damage number.

#### Scenario: A player's critical hit shows no plate

- **WHEN** a player's physical attack hits critically for 500 damage
- **THEN** the stage effects are the slash, a critical damage number of 500 on the monster and the monster's hit shake, with no plate

#### Scenario: A monster's critical hit uses the same damage number

- **WHEN** a monster's attack hits critically for 45 damage
- **THEN** the stage effects are a critical damage number of 45 on the player and the player's hit shake, with no plate

#### Scenario: A normal hit uses the normal damage number

- **WHEN** a player's attack hits without a critical for 88 damage
- **THEN** the stage effects include a non-critical damage number of 88 on the monster

#### Scenario: Other plates are unaffected

- **WHEN** a monster is stunned and cannot act
- **THEN** the stage still shows the 暈眩 plate over the monster


<!-- @trace
source: remove-special-attack-name
updated: 2026-11-06
code:
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Application/Characters/CharacterService.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - docs/superpowers/screenshots/plan-10-settings.png
  - README.md
  - frontend/src/render/effects.ts
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - frontend/src/api/schema.ts
  - scripts/import-wog45.py
  - backend/db/ddl/V000000018__remove_character_special_attack_name.sql
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - frontend/src/game/names.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - backend/EndlessBattle.Infrastructure/Persistence/Models/Character.cs
  - frontend/src/ui/CharacterScreen.tsx
  - frontend/src/game/battleLog.ts
  - docs/research/wog45/gaps.md
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/ui/SettingsPanel.css
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - frontend/src/render/StageRenderer.ts
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - frontend/src/game/stageModel.ts
  - frontend/src/motion/timings.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - frontend/src/ui/CreateCharacterScreen.tsx
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
tests:
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/ui/BattleLog.test.tsx
  - frontend/src/ui/CreateCharacterScreen.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/client.test.ts
-->

---
### Requirement: The battle log calls every critical hit 暴擊

The battle log SHALL use the word 暴擊 for a critical hit of the player or of a monster, and SHALL NOT use any other name for it. The critical lines of the player SHALL keep the log kind `trigger`, and the critical lines of a monster SHALL keep the log kind `normal`. The pet's line SHALL stay 會心一擊.

#### Scenario: Critical lines name the attacker's action

##### Example: critical log lines

| Attacker | Kind | Damage | Log line | Log kind |
| -------- | ---- | ------ | -------- | -------- |
| player | physical | 120 | 暴擊！你攻擊史萊姆，造成 120 點傷害！ | trigger |
| player | magic | 88 | 暴擊！你施放魔法，史萊姆受到 88 點傷害！ | trigger |
| monster | physical | 45 | 史萊姆暴擊！你受到 45 點傷害！ | normal |
| monster | magic | 1,900 | 史萊姆暴擊！你受到 1,900 點傷害！ | normal |

#### Scenario: A pet's critical line is unchanged

- **WHEN** a pet's attack on 史萊姆 hits critically for 30 damage
- **THEN** the log line is 小白會心一擊！對史萊姆造成 30 點傷害。 for a pet named 小白


<!-- @trace
source: remove-special-attack-name
updated: 2026-11-06
code:
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Application/Characters/CharacterService.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - docs/superpowers/screenshots/plan-10-settings.png
  - README.md
  - frontend/src/render/effects.ts
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - frontend/src/api/schema.ts
  - scripts/import-wog45.py
  - backend/db/ddl/V000000018__remove_character_special_attack_name.sql
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - frontend/src/game/names.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - backend/EndlessBattle.Infrastructure/Persistence/Models/Character.cs
  - frontend/src/ui/CharacterScreen.tsx
  - frontend/src/game/battleLog.ts
  - docs/research/wog45/gaps.md
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/ui/SettingsPanel.css
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - frontend/src/render/StageRenderer.ts
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - frontend/src/game/stageModel.ts
  - frontend/src/motion/timings.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - frontend/src/ui/CreateCharacterScreen.tsx
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
tests:
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/ui/BattleLog.test.tsx
  - frontend/src/ui/CreateCharacterScreen.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/client.test.ts
-->

---
### Requirement: Nobody has a special attack name

Characters and monsters SHALL NOT have a special attack name. Creating a character and updating settings SHALL NOT take one, the character response SHALL NOT contain one, and battle events SHALL NOT carry one. The `characters` table SHALL NOT store one. The character creation screen, the settings panel and the character sheet SHALL NOT show one.

#### Scenario: A character is created without a name for its special attack

- **WHEN** a creation request lists the name, sex, element, birth, job and stat allocation and nothing else
- **THEN** the character is created, and the response has no `specialAttackName`

#### Scenario: Settings are saved without a name for the special attack

- **WHEN** a settings request lists only the attack choice and the five percent thresholds
- **THEN** the settings are saved, and an invalid threshold is still refused with reason `invalidPercent`

#### Scenario: Battle events carry no name

- **WHEN** a player with any character data lands a critical hit
- **THEN** the `attack` event has `critical` true and no `specialAttackName`

#### Scenario: The column is gone

- **WHEN** all migrations have been applied
- **THEN** the `characters` table has no `special_attack_name` column

<!-- @trace
source: remove-special-attack-name
updated: 2026-11-06
code:
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Application/Characters/CharacterService.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/EndlessBattleDbContext.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - docs/superpowers/screenshots/plan-10-settings.png
  - README.md
  - frontend/src/render/effects.ts
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - frontend/src/api/schema.ts
  - scripts/import-wog45.py
  - backend/db/ddl/V000000018__remove_character_special_attack_name.sql
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - frontend/src/game/names.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - backend/EndlessBattle.Infrastructure/Persistence/Models/Character.cs
  - frontend/src/ui/CharacterScreen.tsx
  - frontend/src/game/battleLog.ts
  - docs/research/wog45/gaps.md
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/ui/SettingsPanel.css
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - frontend/src/render/StageRenderer.ts
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/DatabaseSchemaTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - frontend/src/game/stageModel.ts
  - frontend/src/motion/timings.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - frontend/src/ui/CreateCharacterScreen.tsx
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
tests:
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/ui/BattleLog.test.tsx
  - frontend/src/ui/CreateCharacterScreen.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/client.test.ts
-->