# game-api-contract Specification

## Purpose

The game API contract defines how the game client and the API exchange player data: field names are complete words rather than abbreviations, stored content and player data keep their existing formats behind the server, and the client converts every response into its own value objects before screens or game logic use it. This keeps a change to the API's shape confined to the conversion layer on each side.

## Requirements

### Requirement: Complete field names in the game API

Responses and requests of the character, adventure, skill, job and world endpoints, and every stat or stat-bonus object returned by any game endpoint, SHALL use complete words for field names. The following names SHALL replace the abbreviated names, and the abbreviated names SHALL NOT appear in those responses:

| Abbreviated | Complete |
| --- | --- |
| `str` | `strength` |
| `smart` | `intelligence` |
| `vit` | `vitality` |
| `agi` | `agility` |
| `au` | `charisma` |
| `be` | `faith` |
| `at` | `attack` |
| `mat` | `magicAttack` |
| `df` | `defense` |
| `mdf` | `magicDefense` |
| `hp` | `healthPoints` |
| `hpMax` | `maxHealthPoints` |
| `sp` | `manaPoints` |
| `spMax` | `maxManaPoints` |
| `exp` | `experience` |
| `jobExp` | `jobExperience` |
| `nextExp` | `nextLevelExperience` |

The same substitution SHALL apply where an abbreviation is part of a longer name (for example `playerHp` becomes `playerHealthPoints`, `potionHpPercent` becomes `potionHealthPointsPercent`, `spCost` becomes `manaPointsCost`). Level ranges SHALL use `minimum` and `maximum` instead of `min` and `max`. `life` SHALL stay `life`.

#### Scenario: Character stats use complete names

- **WHEN** a signed-in client requests `GET /api/characters/me`
- **THEN** the character object has `healthPoints`, `manaPoints`, `experience` and `nextLevelExperience`, its `stats` object has `strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma`, `faith`, `attack`, `magicAttack`, `defense`, `magicDefense`, `maxHealthPoints` and `maxManaPoints`, and none of the abbreviated names appear

##### Example: new character

- **GIVEN** a character created with strength 4, intelligence 2, life 3 and agility 1
- **WHEN** the client requests `GET /api/characters/me`
- **THEN** `baseStats` is `{"strength":14,"intelligence":12,"life":13,"vitality":10,"agility":11,"charisma":10,"faith":10}`, `healthPoints` is 50 and `manaPoints` is 25

#### Scenario: Character creation takes complete names

- **WHEN** a client creates a character with `allocation` `{"strength":1,"intelligence":1,"life":1,"agility":7}`
- **THEN** the API responds 201 and the created character's base strength is 11 and base agility is 17

#### Scenario: Battle events use complete names

- **WHEN** a signed-in client advances an adventure that produced at least one fight
- **THEN** each encounter event has `monsterHealthPoints`, `monsterMaxHealthPoints`, `playerHealthPoints`, `playerMaxHealthPoints`, `playerManaPoints` and `playerMaxManaPoints`, each reward event has `experience` and `jobExperience`, and the summary has `experience` and `jobExperience`

#### Scenario: Item bonuses use complete names

- **WHEN** a signed-in client requests `GET /api/inventory` holding a weapon whose content bonus is 8 attack
- **THEN** that item's `bonus` object has `attack` 8 and has no `at` field

#### Scenario: World level ranges use complete names

- **WHEN** a signed-in client requests `GET /api/world`
- **THEN** each place's `recommended` object has `minimum` and `maximum`


<!-- @trace
source: game-api-dto-vo
updated: 2026-11-12
code:
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Api/JobEndpointsTests.cs
  - frontend/src/game/creationDraft.ts
  - backend/EndlessBattle.Api/Endpoints/AdventureEndpoints.cs
  - frontend/src/game/skills.ts
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Domain/World/MonsterFormulaTests.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Tests/Domain/Rest/HotelTests.cs
  - frontend/src/ui/App.tsx
  - frontend/src/ui/CharacterScreen.tsx
  - backend/EndlessBattle.Tests/Api/ForgeEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/InnEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/SocketTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCaptureTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/RefineTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - frontend/src/ui/CompanionsScreen.tsx
  - backend/EndlessBattle.Domain/Characters/BaseStats.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Tests/Application/EconomyServiceTests.cs
  - frontend/src/game/missions.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentBonusTests.cs
  - backend/openapi/EndlessBattle.Api.json
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/JobEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - frontend/src/api/publicData.ts
  - frontend/src/api/worldData.ts
  - frontend/src/game/character.ts
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - frontend/src/game/world.ts
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - backend/EndlessBattle.Tests/Domain/Progression/ExperienceCurveTests.cs
  - backend/EndlessBattle.Domain/Forge/Socket.cs
  - backend/EndlessBattle.Tests/Domain/Progression/RewardFormulasTests.cs
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Domain/Rest/Inn.cs
  - backend/EndlessBattle.Tests/Api/MissionEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/WorldEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/World/MonsterPoolTests.cs
  - backend/EndlessBattle.Domain/World/LevelRange.cs
  - backend/EndlessBattle.Tests/Domain/AdventureSettingsTests.cs
  - frontend/src/ui/ForgePage.tsx
  - backend/EndlessBattle.Domain/Forge/Refine.cs
  - frontend/src/ui/MissionsScreen.tsx
  - frontend/src/ui/WorldPanel.tsx
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Domain/Pets/PetCare.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Domain/Missions/MissionRewards.cs
  - backend/EndlessBattle.Domain/World/MonsterFormula.cs
  - scripts/smoke-compose.sh
  - backend/EndlessBattle.Domain/Jobs/JobChange.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/DeliveryTests.cs
  - frontend/src/ui/useAdventureLoop.ts
  - backend/EndlessBattle.Domain/Characters/DerivedStats.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Tests/Api/BattleEventResponsesTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Domain/Items/Buffs.cs
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/api/parse.ts
  - frontend/src/ui/MercenaryPage.tsx
  - backend/EndlessBattle.Domain/Pets/PetBattle.cs
  - backend/openapi/EndlessBattle.Api_admin.json
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - backend/EndlessBattle.Domain/Content/JobDefinition.cs
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Tests/Domain/Items/ConsumableUseTests.cs
  - frontend/src/api/characterData.ts
  - frontend/src/ui/PetsPage.tsx
  - backend/EndlessBattle.Domain/Equipment/EquipmentRules.cs
  - frontend/src/game/stats.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - frontend/src/api/adventureData.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs
  - backend/EndlessBattle.Domain/World/Recommendation.cs
  - backend/EndlessBattle.Api/Endpoints/BattleEventResponses.cs
  - backend/EndlessBattle.Api/Endpoints/ShopEndpoints.cs
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - frontend/src/game/battleEvents.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Economy/DepotRulesTests.cs
  - backend/EndlessBattle.Tests/Api/ContentEndpointsTests.cs
  - frontend/src/game/skillOverview.ts
  - backend/EndlessBattle.Domain/Battle/PotionRules.cs
  - backend/EndlessBattle.Tests/Application/WorldServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/WorldEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/InventoryRepositoriesTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Support/TestItems.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - admin/src/report/ReportPage.tsx
  - admin/src/api/schema.ts
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Domain/Progression/RewardFormulas.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/game/battlePlayback.ts
  - frontend/src/game/publicNames.ts
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Tests/Application/InventoryServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Rest/Hotel.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Jobs/JobChangeTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Api/EconomyEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Progression/LevelUpTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/RestRulesTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/game/battleLog.ts
  - frontend/src/ui/InnPage.tsx
  - frontend/src/ui/SkillsScreen.tsx
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Domain/DerivedStatsTests.cs
  - backend/EndlessBattle.Tests/Application/InnServiceTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureRun.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - backend/EndlessBattle.Domain/Items/ConsumableUse.cs
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - scripts/deploy-nas.sh
  - CLAUDE.md
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/game/types.ts
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentRulesTests.cs
  - backend/EndlessBattle.Domain/Progression/LevelUp.cs
  - backend/EndlessBattle.Tests/Api/AdventureEndpointsTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Adventure/RestRules.cs
  - frontend/src/game/mercenaries.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - frontend/src/game/refreshSignals.ts
  - backend/EndlessBattle.Domain/Characters/AdventureSettings.cs
  - frontend/src/ui/AdventureScreen.tsx
  - frontend/src/api/skillData.ts
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Domain/Progression/ExperienceCurve.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
tests:
  - frontend/src/ui/WorldPanel.test.tsx
  - admin/src/report/ReportPage.test.tsx
  - frontend/src/game/stats.test.ts
  - frontend/src/game/refreshSignals.test.ts
  - frontend/src/ui/MissionsScreen.test.tsx
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/api/client.test.ts
  - frontend/src/api/parse.test.ts
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/characterData.test.ts
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/api/adventureData.test.ts
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battlePlayback.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/game/world.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/api/worldData.test.ts
  - frontend/src/game/missions.test.ts
  - admin/src/report/compare.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/create/CreateSubmit.test.tsx
  - frontend/src/api/skillData.test.ts
-->

---
### Requirement: Stat reason codes name the stat in full

A reason code that reports a stat below a requirement SHALL be `statTooLow:<stat>`, where `<stat>` is one of `strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma` and `faith`.

#### Scenario: Job change blocked by a stat

- **WHEN** a signed-in client requests `GET /api/jobs` and an advanced job requires 50 strength while the character's base strength is 20
- **THEN** that job's `reasons` contains `statTooLow:strength`

#### Scenario: Equipping blocked by a stat

- **WHEN** a signed-in client equips an item that requires more charisma than the character's base charisma
- **THEN** the API responds 400 and the `reasons` list contains `statTooLow:charisma`


<!-- @trace
source: game-api-dto-vo
updated: 2026-11-12
code:
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Api/JobEndpointsTests.cs
  - frontend/src/game/creationDraft.ts
  - backend/EndlessBattle.Api/Endpoints/AdventureEndpoints.cs
  - frontend/src/game/skills.ts
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Domain/World/MonsterFormulaTests.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Tests/Domain/Rest/HotelTests.cs
  - frontend/src/ui/App.tsx
  - frontend/src/ui/CharacterScreen.tsx
  - backend/EndlessBattle.Tests/Api/ForgeEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/InnEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/SocketTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCaptureTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/RefineTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - frontend/src/ui/CompanionsScreen.tsx
  - backend/EndlessBattle.Domain/Characters/BaseStats.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Tests/Application/EconomyServiceTests.cs
  - frontend/src/game/missions.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentBonusTests.cs
  - backend/openapi/EndlessBattle.Api.json
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/JobEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - frontend/src/api/publicData.ts
  - frontend/src/api/worldData.ts
  - frontend/src/game/character.ts
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - frontend/src/game/world.ts
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - backend/EndlessBattle.Tests/Domain/Progression/ExperienceCurveTests.cs
  - backend/EndlessBattle.Domain/Forge/Socket.cs
  - backend/EndlessBattle.Tests/Domain/Progression/RewardFormulasTests.cs
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Domain/Rest/Inn.cs
  - backend/EndlessBattle.Tests/Api/MissionEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/WorldEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/World/MonsterPoolTests.cs
  - backend/EndlessBattle.Domain/World/LevelRange.cs
  - backend/EndlessBattle.Tests/Domain/AdventureSettingsTests.cs
  - frontend/src/ui/ForgePage.tsx
  - backend/EndlessBattle.Domain/Forge/Refine.cs
  - frontend/src/ui/MissionsScreen.tsx
  - frontend/src/ui/WorldPanel.tsx
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Domain/Pets/PetCare.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Domain/Missions/MissionRewards.cs
  - backend/EndlessBattle.Domain/World/MonsterFormula.cs
  - scripts/smoke-compose.sh
  - backend/EndlessBattle.Domain/Jobs/JobChange.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/DeliveryTests.cs
  - frontend/src/ui/useAdventureLoop.ts
  - backend/EndlessBattle.Domain/Characters/DerivedStats.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Tests/Api/BattleEventResponsesTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Domain/Items/Buffs.cs
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/api/parse.ts
  - frontend/src/ui/MercenaryPage.tsx
  - backend/EndlessBattle.Domain/Pets/PetBattle.cs
  - backend/openapi/EndlessBattle.Api_admin.json
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - backend/EndlessBattle.Domain/Content/JobDefinition.cs
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Tests/Domain/Items/ConsumableUseTests.cs
  - frontend/src/api/characterData.ts
  - frontend/src/ui/PetsPage.tsx
  - backend/EndlessBattle.Domain/Equipment/EquipmentRules.cs
  - frontend/src/game/stats.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - frontend/src/api/adventureData.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs
  - backend/EndlessBattle.Domain/World/Recommendation.cs
  - backend/EndlessBattle.Api/Endpoints/BattleEventResponses.cs
  - backend/EndlessBattle.Api/Endpoints/ShopEndpoints.cs
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - frontend/src/game/battleEvents.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Economy/DepotRulesTests.cs
  - backend/EndlessBattle.Tests/Api/ContentEndpointsTests.cs
  - frontend/src/game/skillOverview.ts
  - backend/EndlessBattle.Domain/Battle/PotionRules.cs
  - backend/EndlessBattle.Tests/Application/WorldServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/WorldEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/InventoryRepositoriesTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Support/TestItems.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - admin/src/report/ReportPage.tsx
  - admin/src/api/schema.ts
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Domain/Progression/RewardFormulas.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/game/battlePlayback.ts
  - frontend/src/game/publicNames.ts
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Tests/Application/InventoryServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Rest/Hotel.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Jobs/JobChangeTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Api/EconomyEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Progression/LevelUpTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/RestRulesTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/game/battleLog.ts
  - frontend/src/ui/InnPage.tsx
  - frontend/src/ui/SkillsScreen.tsx
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Domain/DerivedStatsTests.cs
  - backend/EndlessBattle.Tests/Application/InnServiceTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureRun.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - backend/EndlessBattle.Domain/Items/ConsumableUse.cs
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - scripts/deploy-nas.sh
  - CLAUDE.md
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/game/types.ts
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentRulesTests.cs
  - backend/EndlessBattle.Domain/Progression/LevelUp.cs
  - backend/EndlessBattle.Tests/Api/AdventureEndpointsTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Adventure/RestRules.cs
  - frontend/src/game/mercenaries.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - frontend/src/game/refreshSignals.ts
  - backend/EndlessBattle.Domain/Characters/AdventureSettings.cs
  - frontend/src/ui/AdventureScreen.tsx
  - frontend/src/api/skillData.ts
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Domain/Progression/ExperienceCurve.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
tests:
  - frontend/src/ui/WorldPanel.test.tsx
  - admin/src/report/ReportPage.test.tsx
  - frontend/src/game/stats.test.ts
  - frontend/src/game/refreshSignals.test.ts
  - frontend/src/ui/MissionsScreen.test.tsx
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/api/client.test.ts
  - frontend/src/api/parse.test.ts
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/characterData.test.ts
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/api/adventureData.test.ts
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battlePlayback.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/game/world.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/api/worldData.test.ts
  - frontend/src/game/missions.test.ts
  - admin/src/report/compare.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/create/CreateSubmit.test.tsx
  - frontend/src/api/skillData.test.ts
-->

---
### Requirement: Stored formats stay readable

Renaming fields SHALL NOT change the format of stored data. Content files under `backend/content/` SHALL keep their existing keys and SHALL load with the same values as before. Enhancement data already stored for a player's items SHALL read back with the same values, and newly written enhancement data SHALL use the same keys as data written before this change.

#### Scenario: Existing content loads unchanged

- **WHEN** the API starts with the current `backend/content/` files
- **THEN** content loading succeeds and each monster's attack, magic attack, defense, magic defense, health points and mana points equal the `at`, `mat`, `df`, `mdf`, `hp` and `sp` values in `monsters.json`

#### Scenario: Previously stored refine bonus reads back

- **WHEN** an inventory row's stored refine bonus is `{"str":2,"smart":0,"life":0,"vit":0,"agi":0,"au":0,"be":0,"at":3,"mat":0,"df":0,"mdf":0,"hpMax":0}`
- **THEN** that item's refine bonus reads back as strength 2 and attack 3, and writing it back stores the same keys


<!-- @trace
source: game-api-dto-vo
updated: 2026-11-12
code:
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Api/JobEndpointsTests.cs
  - frontend/src/game/creationDraft.ts
  - backend/EndlessBattle.Api/Endpoints/AdventureEndpoints.cs
  - frontend/src/game/skills.ts
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Domain/World/MonsterFormulaTests.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Tests/Domain/Rest/HotelTests.cs
  - frontend/src/ui/App.tsx
  - frontend/src/ui/CharacterScreen.tsx
  - backend/EndlessBattle.Tests/Api/ForgeEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/InnEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/SocketTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCaptureTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/RefineTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - frontend/src/ui/CompanionsScreen.tsx
  - backend/EndlessBattle.Domain/Characters/BaseStats.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Tests/Application/EconomyServiceTests.cs
  - frontend/src/game/missions.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentBonusTests.cs
  - backend/openapi/EndlessBattle.Api.json
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/JobEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - frontend/src/api/publicData.ts
  - frontend/src/api/worldData.ts
  - frontend/src/game/character.ts
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - frontend/src/game/world.ts
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - backend/EndlessBattle.Tests/Domain/Progression/ExperienceCurveTests.cs
  - backend/EndlessBattle.Domain/Forge/Socket.cs
  - backend/EndlessBattle.Tests/Domain/Progression/RewardFormulasTests.cs
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Domain/Rest/Inn.cs
  - backend/EndlessBattle.Tests/Api/MissionEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/WorldEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/World/MonsterPoolTests.cs
  - backend/EndlessBattle.Domain/World/LevelRange.cs
  - backend/EndlessBattle.Tests/Domain/AdventureSettingsTests.cs
  - frontend/src/ui/ForgePage.tsx
  - backend/EndlessBattle.Domain/Forge/Refine.cs
  - frontend/src/ui/MissionsScreen.tsx
  - frontend/src/ui/WorldPanel.tsx
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Domain/Pets/PetCare.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Domain/Missions/MissionRewards.cs
  - backend/EndlessBattle.Domain/World/MonsterFormula.cs
  - scripts/smoke-compose.sh
  - backend/EndlessBattle.Domain/Jobs/JobChange.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/DeliveryTests.cs
  - frontend/src/ui/useAdventureLoop.ts
  - backend/EndlessBattle.Domain/Characters/DerivedStats.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Tests/Api/BattleEventResponsesTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Domain/Items/Buffs.cs
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/api/parse.ts
  - frontend/src/ui/MercenaryPage.tsx
  - backend/EndlessBattle.Domain/Pets/PetBattle.cs
  - backend/openapi/EndlessBattle.Api_admin.json
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - backend/EndlessBattle.Domain/Content/JobDefinition.cs
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Tests/Domain/Items/ConsumableUseTests.cs
  - frontend/src/api/characterData.ts
  - frontend/src/ui/PetsPage.tsx
  - backend/EndlessBattle.Domain/Equipment/EquipmentRules.cs
  - frontend/src/game/stats.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - frontend/src/api/adventureData.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs
  - backend/EndlessBattle.Domain/World/Recommendation.cs
  - backend/EndlessBattle.Api/Endpoints/BattleEventResponses.cs
  - backend/EndlessBattle.Api/Endpoints/ShopEndpoints.cs
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - frontend/src/game/battleEvents.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Economy/DepotRulesTests.cs
  - backend/EndlessBattle.Tests/Api/ContentEndpointsTests.cs
  - frontend/src/game/skillOverview.ts
  - backend/EndlessBattle.Domain/Battle/PotionRules.cs
  - backend/EndlessBattle.Tests/Application/WorldServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/WorldEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/InventoryRepositoriesTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Support/TestItems.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - admin/src/report/ReportPage.tsx
  - admin/src/api/schema.ts
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Domain/Progression/RewardFormulas.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/game/battlePlayback.ts
  - frontend/src/game/publicNames.ts
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Tests/Application/InventoryServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Rest/Hotel.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Jobs/JobChangeTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Api/EconomyEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Progression/LevelUpTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/RestRulesTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/game/battleLog.ts
  - frontend/src/ui/InnPage.tsx
  - frontend/src/ui/SkillsScreen.tsx
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Domain/DerivedStatsTests.cs
  - backend/EndlessBattle.Tests/Application/InnServiceTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureRun.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - backend/EndlessBattle.Domain/Items/ConsumableUse.cs
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - scripts/deploy-nas.sh
  - CLAUDE.md
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/game/types.ts
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentRulesTests.cs
  - backend/EndlessBattle.Domain/Progression/LevelUp.cs
  - backend/EndlessBattle.Tests/Api/AdventureEndpointsTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Adventure/RestRules.cs
  - frontend/src/game/mercenaries.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - frontend/src/game/refreshSignals.ts
  - backend/EndlessBattle.Domain/Characters/AdventureSettings.cs
  - frontend/src/ui/AdventureScreen.tsx
  - frontend/src/api/skillData.ts
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Domain/Progression/ExperienceCurve.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
tests:
  - frontend/src/ui/WorldPanel.test.tsx
  - admin/src/report/ReportPage.test.tsx
  - frontend/src/game/stats.test.ts
  - frontend/src/game/refreshSignals.test.ts
  - frontend/src/ui/MissionsScreen.test.tsx
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/api/client.test.ts
  - frontend/src/api/parse.test.ts
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/characterData.test.ts
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/api/adventureData.test.ts
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battlePlayback.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/game/world.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/api/worldData.test.ts
  - frontend/src/game/missions.test.ts
  - admin/src/report/compare.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/create/CreateSubmit.test.tsx
  - frontend/src/api/skillData.test.ts
-->

---
### Requirement: Battle events carry monster names

Battle events that refer to a monster SHALL carry the monster's display name alongside its id: the encounter event SHALL have `monsterName`, the capture event SHALL have `capturedMonsterName` (null when nothing was captured), and the mission progress event SHALL have `monsterName`. The client SHALL show these names in the battle log without another request.

#### Scenario: Encounter names the monster

- **WHEN** a signed-in client advances an adventure and the character met the monster `monster-1` named 史萊姆
- **THEN** the encounter event has `monsterId` `monster-1` and `monsterName` 史萊姆, and the battle log shows 遭遇 史萊姆！


<!-- @trace
source: game-api-dto-vo
updated: 2026-11-12
code:
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Api/JobEndpointsTests.cs
  - frontend/src/game/creationDraft.ts
  - backend/EndlessBattle.Api/Endpoints/AdventureEndpoints.cs
  - frontend/src/game/skills.ts
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Domain/World/MonsterFormulaTests.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Tests/Domain/Rest/HotelTests.cs
  - frontend/src/ui/App.tsx
  - frontend/src/ui/CharacterScreen.tsx
  - backend/EndlessBattle.Tests/Api/ForgeEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/InnEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/SocketTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCaptureTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/RefineTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - frontend/src/ui/CompanionsScreen.tsx
  - backend/EndlessBattle.Domain/Characters/BaseStats.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Tests/Application/EconomyServiceTests.cs
  - frontend/src/game/missions.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentBonusTests.cs
  - backend/openapi/EndlessBattle.Api.json
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/JobEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - frontend/src/api/publicData.ts
  - frontend/src/api/worldData.ts
  - frontend/src/game/character.ts
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - frontend/src/game/world.ts
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - backend/EndlessBattle.Tests/Domain/Progression/ExperienceCurveTests.cs
  - backend/EndlessBattle.Domain/Forge/Socket.cs
  - backend/EndlessBattle.Tests/Domain/Progression/RewardFormulasTests.cs
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Domain/Rest/Inn.cs
  - backend/EndlessBattle.Tests/Api/MissionEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/WorldEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/World/MonsterPoolTests.cs
  - backend/EndlessBattle.Domain/World/LevelRange.cs
  - backend/EndlessBattle.Tests/Domain/AdventureSettingsTests.cs
  - frontend/src/ui/ForgePage.tsx
  - backend/EndlessBattle.Domain/Forge/Refine.cs
  - frontend/src/ui/MissionsScreen.tsx
  - frontend/src/ui/WorldPanel.tsx
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Domain/Pets/PetCare.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Domain/Missions/MissionRewards.cs
  - backend/EndlessBattle.Domain/World/MonsterFormula.cs
  - scripts/smoke-compose.sh
  - backend/EndlessBattle.Domain/Jobs/JobChange.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/DeliveryTests.cs
  - frontend/src/ui/useAdventureLoop.ts
  - backend/EndlessBattle.Domain/Characters/DerivedStats.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Tests/Api/BattleEventResponsesTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Domain/Items/Buffs.cs
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/api/parse.ts
  - frontend/src/ui/MercenaryPage.tsx
  - backend/EndlessBattle.Domain/Pets/PetBattle.cs
  - backend/openapi/EndlessBattle.Api_admin.json
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - backend/EndlessBattle.Domain/Content/JobDefinition.cs
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Tests/Domain/Items/ConsumableUseTests.cs
  - frontend/src/api/characterData.ts
  - frontend/src/ui/PetsPage.tsx
  - backend/EndlessBattle.Domain/Equipment/EquipmentRules.cs
  - frontend/src/game/stats.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - frontend/src/api/adventureData.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs
  - backend/EndlessBattle.Domain/World/Recommendation.cs
  - backend/EndlessBattle.Api/Endpoints/BattleEventResponses.cs
  - backend/EndlessBattle.Api/Endpoints/ShopEndpoints.cs
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - frontend/src/game/battleEvents.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Economy/DepotRulesTests.cs
  - backend/EndlessBattle.Tests/Api/ContentEndpointsTests.cs
  - frontend/src/game/skillOverview.ts
  - backend/EndlessBattle.Domain/Battle/PotionRules.cs
  - backend/EndlessBattle.Tests/Application/WorldServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/WorldEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/InventoryRepositoriesTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Support/TestItems.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - admin/src/report/ReportPage.tsx
  - admin/src/api/schema.ts
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Domain/Progression/RewardFormulas.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/game/battlePlayback.ts
  - frontend/src/game/publicNames.ts
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Tests/Application/InventoryServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Rest/Hotel.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Jobs/JobChangeTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Api/EconomyEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Progression/LevelUpTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/RestRulesTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/game/battleLog.ts
  - frontend/src/ui/InnPage.tsx
  - frontend/src/ui/SkillsScreen.tsx
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Domain/DerivedStatsTests.cs
  - backend/EndlessBattle.Tests/Application/InnServiceTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureRun.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - backend/EndlessBattle.Domain/Items/ConsumableUse.cs
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - scripts/deploy-nas.sh
  - CLAUDE.md
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/game/types.ts
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentRulesTests.cs
  - backend/EndlessBattle.Domain/Progression/LevelUp.cs
  - backend/EndlessBattle.Tests/Api/AdventureEndpointsTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Adventure/RestRules.cs
  - frontend/src/game/mercenaries.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - frontend/src/game/refreshSignals.ts
  - backend/EndlessBattle.Domain/Characters/AdventureSettings.cs
  - frontend/src/ui/AdventureScreen.tsx
  - frontend/src/api/skillData.ts
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Domain/Progression/ExperienceCurve.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
tests:
  - frontend/src/ui/WorldPanel.test.tsx
  - admin/src/report/ReportPage.test.tsx
  - frontend/src/game/stats.test.ts
  - frontend/src/game/refreshSignals.test.ts
  - frontend/src/ui/MissionsScreen.test.tsx
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/api/client.test.ts
  - frontend/src/api/parse.test.ts
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/characterData.test.ts
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/api/adventureData.test.ts
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battlePlayback.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/game/world.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/api/worldData.test.ts
  - frontend/src/game/missions.test.ts
  - admin/src/report/compare.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/create/CreateSubmit.test.tsx
  - frontend/src/api/skillData.test.ts
-->

---
### Requirement: Content catalogue endpoint removed

`GET /api/content` SHALL NOT exist. Screens SHALL take monster names from battle events, and sex, element, birthplace and job names from `/api/public/settings` and `/api/public/jobs`.

#### Scenario: Catalogue request

- **WHEN** a client requests `GET /api/content`
- **THEN** the API responds 404

#### Scenario: Character page names come from public data

- **WHEN** the character page is shown for a character with sex 2, element 3 and birth 4
- **THEN** it shows the sex, element and birthplace names that `/api/public/settings` lists for ids 2, 3 and 4

#### Scenario: Mission requirement names the job

- **WHEN** the mission board shows a mission limited to the job `job-6`
- **THEN** its requirement line shows the name that `/api/public/jobs` lists for `job-6`


<!-- @trace
source: game-api-dto-vo
updated: 2026-11-12
code:
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Api/JobEndpointsTests.cs
  - frontend/src/game/creationDraft.ts
  - backend/EndlessBattle.Api/Endpoints/AdventureEndpoints.cs
  - frontend/src/game/skills.ts
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Domain/World/MonsterFormulaTests.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Tests/Domain/Rest/HotelTests.cs
  - frontend/src/ui/App.tsx
  - frontend/src/ui/CharacterScreen.tsx
  - backend/EndlessBattle.Tests/Api/ForgeEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/InnEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/SocketTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCaptureTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/RefineTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - frontend/src/ui/CompanionsScreen.tsx
  - backend/EndlessBattle.Domain/Characters/BaseStats.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Tests/Application/EconomyServiceTests.cs
  - frontend/src/game/missions.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentBonusTests.cs
  - backend/openapi/EndlessBattle.Api.json
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/JobEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - frontend/src/api/publicData.ts
  - frontend/src/api/worldData.ts
  - frontend/src/game/character.ts
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - frontend/src/game/world.ts
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - backend/EndlessBattle.Tests/Domain/Progression/ExperienceCurveTests.cs
  - backend/EndlessBattle.Domain/Forge/Socket.cs
  - backend/EndlessBattle.Tests/Domain/Progression/RewardFormulasTests.cs
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Domain/Rest/Inn.cs
  - backend/EndlessBattle.Tests/Api/MissionEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/WorldEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/World/MonsterPoolTests.cs
  - backend/EndlessBattle.Domain/World/LevelRange.cs
  - backend/EndlessBattle.Tests/Domain/AdventureSettingsTests.cs
  - frontend/src/ui/ForgePage.tsx
  - backend/EndlessBattle.Domain/Forge/Refine.cs
  - frontend/src/ui/MissionsScreen.tsx
  - frontend/src/ui/WorldPanel.tsx
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Domain/Pets/PetCare.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Domain/Missions/MissionRewards.cs
  - backend/EndlessBattle.Domain/World/MonsterFormula.cs
  - scripts/smoke-compose.sh
  - backend/EndlessBattle.Domain/Jobs/JobChange.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/DeliveryTests.cs
  - frontend/src/ui/useAdventureLoop.ts
  - backend/EndlessBattle.Domain/Characters/DerivedStats.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Tests/Api/BattleEventResponsesTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Domain/Items/Buffs.cs
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/api/parse.ts
  - frontend/src/ui/MercenaryPage.tsx
  - backend/EndlessBattle.Domain/Pets/PetBattle.cs
  - backend/openapi/EndlessBattle.Api_admin.json
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - backend/EndlessBattle.Domain/Content/JobDefinition.cs
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Tests/Domain/Items/ConsumableUseTests.cs
  - frontend/src/api/characterData.ts
  - frontend/src/ui/PetsPage.tsx
  - backend/EndlessBattle.Domain/Equipment/EquipmentRules.cs
  - frontend/src/game/stats.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - frontend/src/api/adventureData.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs
  - backend/EndlessBattle.Domain/World/Recommendation.cs
  - backend/EndlessBattle.Api/Endpoints/BattleEventResponses.cs
  - backend/EndlessBattle.Api/Endpoints/ShopEndpoints.cs
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - frontend/src/game/battleEvents.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Economy/DepotRulesTests.cs
  - backend/EndlessBattle.Tests/Api/ContentEndpointsTests.cs
  - frontend/src/game/skillOverview.ts
  - backend/EndlessBattle.Domain/Battle/PotionRules.cs
  - backend/EndlessBattle.Tests/Application/WorldServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/WorldEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/InventoryRepositoriesTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Support/TestItems.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - admin/src/report/ReportPage.tsx
  - admin/src/api/schema.ts
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Domain/Progression/RewardFormulas.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/game/battlePlayback.ts
  - frontend/src/game/publicNames.ts
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Tests/Application/InventoryServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Rest/Hotel.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Jobs/JobChangeTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Api/EconomyEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Progression/LevelUpTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/RestRulesTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/game/battleLog.ts
  - frontend/src/ui/InnPage.tsx
  - frontend/src/ui/SkillsScreen.tsx
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Domain/DerivedStatsTests.cs
  - backend/EndlessBattle.Tests/Application/InnServiceTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureRun.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - backend/EndlessBattle.Domain/Items/ConsumableUse.cs
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - scripts/deploy-nas.sh
  - CLAUDE.md
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/game/types.ts
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentRulesTests.cs
  - backend/EndlessBattle.Domain/Progression/LevelUp.cs
  - backend/EndlessBattle.Tests/Api/AdventureEndpointsTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Adventure/RestRules.cs
  - frontend/src/game/mercenaries.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - frontend/src/game/refreshSignals.ts
  - backend/EndlessBattle.Domain/Characters/AdventureSettings.cs
  - frontend/src/ui/AdventureScreen.tsx
  - frontend/src/api/skillData.ts
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Domain/Progression/ExperienceCurve.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
tests:
  - frontend/src/ui/WorldPanel.test.tsx
  - admin/src/report/ReportPage.test.tsx
  - frontend/src/game/stats.test.ts
  - frontend/src/game/refreshSignals.test.ts
  - frontend/src/ui/MissionsScreen.test.tsx
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/api/client.test.ts
  - frontend/src/api/parse.test.ts
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/characterData.test.ts
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/api/adventureData.test.ts
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battlePlayback.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/game/world.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/api/worldData.test.ts
  - frontend/src/game/missions.test.ts
  - admin/src/report/compare.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/create/CreateSubmit.test.tsx
  - frontend/src/api/skillData.test.ts
-->

---
### Requirement: Client converts responses before use

The game client SHALL convert each response of the character, adventure, skill, job and world endpoints into its own value objects before any screen or game-state code reads it. A response missing a required field, or holding a value of the wrong type, SHALL reject the request with a parse error that names the field's path; the screen SHALL treat it like any other failed request. Fields the client does not know SHALL be ignored.

#### Scenario: Missing field is reported with its path

- **WHEN** `GET /api/characters/me` responds 200 with a body whose `stats` object lacks `strength`
- **THEN** the client rejects the request with a parse error whose path is `character.stats.strength`

#### Scenario: Unknown field is ignored

- **WHEN** `GET /api/world` responds 200 with an extra field on a place that the client does not know
- **THEN** the client returns the world map and the extra field does not appear on the converted place

<!-- @trace
source: game-api-dto-vo
updated: 2026-11-12
code:
  - backend/EndlessBattle.Api/Program.cs
  - backend/EndlessBattle.Tests/Api/JobEndpointsTests.cs
  - frontend/src/game/creationDraft.ts
  - backend/EndlessBattle.Api/Endpoints/AdventureEndpoints.cs
  - frontend/src/game/skills.ts
  - backend/EndlessBattle.Domain/Characters/Character.cs
  - backend/EndlessBattle.Tests/Domain/Items/BuffsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/CombatantStateTests.cs
  - backend/EndlessBattle.Domain/Battle/CombatantState.cs
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/game/inventory.ts
  - backend/EndlessBattle.Tests/Domain/World/MonsterFormulaTests.cs
  - backend/EndlessBattle.Application/Adventure/AdventureService.cs
  - backend/EndlessBattle.Tests/Domain/Rest/HotelTests.cs
  - frontend/src/ui/App.tsx
  - frontend/src/ui/CharacterScreen.tsx
  - backend/EndlessBattle.Tests/Api/ForgeEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/InnEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/SocketTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCaptureTests.cs
  - backend/EndlessBattle.Tests/Domain/Forge/RefineTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillEffectTests.cs
  - frontend/src/ui/CompanionsScreen.tsx
  - backend/EndlessBattle.Domain/Characters/BaseStats.cs
  - backend/EndlessBattle.Tests/Api/ApiHostStartupTests.cs
  - backend/EndlessBattle.Tests/Application/EconomyServiceTests.cs
  - frontend/src/game/missions.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentBonusTests.cs
  - backend/openapi/EndlessBattle.Api.json
  - backend/EndlessBattle.Tests/Application/MissionServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/JobEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/SkillCasterTests.cs
  - frontend/src/api/publicData.ts
  - frontend/src/api/worldData.ts
  - frontend/src/game/character.ts
  - backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs
  - frontend/src/game/world.ts
  - backend/EndlessBattle.Domain/Battle/SkillCaster.cs
  - backend/EndlessBattle.Tests/Domain/Progression/ExperienceCurveTests.cs
  - backend/EndlessBattle.Domain/Forge/Socket.cs
  - backend/EndlessBattle.Tests/Domain/Progression/RewardFormulasTests.cs
  - frontend/src/api/client.ts
  - backend/EndlessBattle.Domain/Rest/Inn.cs
  - backend/EndlessBattle.Tests/Api/MissionEndpointsTests.cs
  - backend/EndlessBattle.Tests/Api/WorldEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/World/MonsterPoolTests.cs
  - backend/EndlessBattle.Domain/World/LevelRange.cs
  - backend/EndlessBattle.Tests/Domain/AdventureSettingsTests.cs
  - frontend/src/ui/ForgePage.tsx
  - backend/EndlessBattle.Domain/Forge/Refine.cs
  - frontend/src/ui/MissionsScreen.tsx
  - frontend/src/ui/WorldPanel.tsx
  - frontend/src/ui/SettingsPanel.tsx
  - backend/EndlessBattle.Domain/Pets/PetCare.cs
  - backend/EndlessBattle.Tests/TestContent/items.json
  - backend/EndlessBattle.Domain/Battle/EscapeRules.cs
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - backend/EndlessBattle.Domain/Missions/MissionRewards.cs
  - backend/EndlessBattle.Domain/World/MonsterFormula.cs
  - scripts/smoke-compose.sh
  - backend/EndlessBattle.Domain/Jobs/JobChange.cs
  - backend/EndlessBattle.Tests/Domain/Rest/InnTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/DeliveryTests.cs
  - frontend/src/ui/useAdventureLoop.ts
  - backend/EndlessBattle.Domain/Characters/DerivedStats.cs
  - backend/EndlessBattle.Tests/Domain/Battle/BattleEventsTests.cs
  - backend/EndlessBattle.Tests/Api/BattleEventResponsesTests.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleMonsterSkillTests.cs
  - backend/EndlessBattle.Domain/Items/Buffs.cs
  - backend/EndlessBattle.Tests/Application/ForgeServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/api/parse.ts
  - frontend/src/ui/MercenaryPage.tsx
  - backend/EndlessBattle.Domain/Pets/PetBattle.cs
  - backend/openapi/EndlessBattle.Api_admin.json
  - backend/EndlessBattle.Domain/Mercenaries/MercenarySimulator.cs
  - backend/EndlessBattle.Domain/Content/JobDefinition.cs
  - backend/EndlessBattle.Domain/Battle/RoundResolver.cs
  - backend/EndlessBattle.Tests/Domain/Items/ConsumableUseTests.cs
  - frontend/src/api/characterData.ts
  - frontend/src/ui/PetsPage.tsx
  - backend/EndlessBattle.Domain/Equipment/EquipmentRules.cs
  - frontend/src/game/stats.ts
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattleSkillTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - backend/EndlessBattle.Tests/Domain/Skills/SkillActivationRateTests.cs
  - frontend/src/api/adventureData.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/BagScreen.tsx
  - backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs
  - backend/EndlessBattle.Domain/World/Recommendation.cs
  - backend/EndlessBattle.Api/Endpoints/BattleEventResponses.cs
  - backend/EndlessBattle.Api/Endpoints/ShopEndpoints.cs
  - frontend/src/api/schema.ts
  - backend/EndlessBattle.Domain/Items/ItemDefinition.cs
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - backend/EndlessBattle.Tests/Support/TestCharacters.cs
  - backend/EndlessBattle.Api/Endpoints/InventoryEndpoints.cs
  - frontend/src/game/battleEvents.ts
  - backend/EndlessBattle.Tests/Content/WogImportTests.cs
  - backend/EndlessBattle.Tests/Domain/Economy/DepotRulesTests.cs
  - backend/EndlessBattle.Tests/Api/ContentEndpointsTests.cs
  - frontend/src/game/skillOverview.ts
  - backend/EndlessBattle.Domain/Battle/PotionRules.cs
  - backend/EndlessBattle.Tests/Application/WorldServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/WorldEndpoints.cs
  - backend/EndlessBattle.Tests/Domain/Battle/RoundResolverTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/InventoryRepositoriesTests.cs
  - backend/EndlessBattle.Domain/Characters/CombatBridge.cs
  - backend/EndlessBattle.Tests/Application/BalanceReportServiceTests.cs
  - backend/EndlessBattle.Tests/Support/TestItems.cs
  - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs
  - admin/src/report/ReportPage.tsx
  - admin/src/api/schema.ts
  - backend/EndlessBattle.Domain/Battle/WogBattle.cs
  - backend/EndlessBattle.Domain/Progression/RewardFormulas.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/AdventureSimulatorTests.cs
  - frontend/src/game/battlePlayback.ts
  - frontend/src/game/publicNames.ts
  - frontend/src/game/stageModel.ts
  - backend/EndlessBattle.Tests/Application/InventoryServiceTests.cs
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Domain/Rest/Hotel.cs
  - backend/EndlessBattle.Tests/Application/AdventureServiceTests.cs
  - backend/EndlessBattle.Tests/Domain/Jobs/JobChangeTests.cs
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Tests/Api/EconomyEndpointsTests.cs
  - backend/EndlessBattle.Tests/Domain/Pets/PetBattleTests.cs
  - backend/EndlessBattle.Tests/Domain/Progression/LevelUpTests.cs
  - backend/EndlessBattle.Tests/Domain/Adventure/RestRulesTests.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Domain/Characters/CreateCharacterCommand.cs
  - backend/EndlessBattle.Tests/Infrastructure/JsonGameContentTests.cs
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/game/battleLog.ts
  - frontend/src/ui/InnPage.tsx
  - frontend/src/ui/SkillsScreen.tsx
  - backend/EndlessBattle.Application/Balance/BalanceReportService.cs
  - backend/EndlessBattle.Tests/Domain/DerivedStatsTests.cs
  - backend/EndlessBattle.Tests/Application/InnServiceTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureRun.cs
  - backend/EndlessBattle.Tests/Application/CharacterServiceTests.cs
  - backend/EndlessBattle.Domain/Items/ConsumableUse.cs
  - frontend/src/ui/bagWidgets.tsx
  - backend/EndlessBattle.Tests/Api/Admin/AdminContentReadTests.cs
  - scripts/deploy-nas.sh
  - CLAUDE.md
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePotionTests.cs
  - frontend/src/game/types.ts
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/game/settings.ts
  - backend/EndlessBattle.Tests/Domain/Equipment/EquipmentRulesTests.cs
  - backend/EndlessBattle.Domain/Progression/LevelUp.cs
  - backend/EndlessBattle.Tests/Api/AdventureEndpointsTests.cs
  - backend/EndlessBattle.Domain/Adventure/AdventureSimulator.cs
  - backend/EndlessBattle.Domain/Adventure/RestRules.cs
  - frontend/src/game/mercenaries.ts
  - backend/EndlessBattle.Domain/Battle/BattleEvents.cs
  - frontend/src/game/refreshSignals.ts
  - backend/EndlessBattle.Domain/Characters/AdventureSettings.cs
  - frontend/src/ui/AdventureScreen.tsx
  - frontend/src/api/skillData.ts
  - backend/EndlessBattle.Domain/Battle/BattleModifiers.cs
  - backend/EndlessBattle.Domain/Content/MonsterDefinition.cs
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Domain/Progression/ExperienceCurve.cs
  - backend/EndlessBattle.Tests/Domain/Battle/WogBattlePetTests.cs
  - backend/EndlessBattle.Tests/Domain/Missions/MissionRewardsTests.cs
tests:
  - frontend/src/ui/WorldPanel.test.tsx
  - admin/src/report/ReportPage.test.tsx
  - frontend/src/game/stats.test.ts
  - frontend/src/game/refreshSignals.test.ts
  - frontend/src/ui/MissionsScreen.test.tsx
  - frontend/src/ui/SkillsScreen.test.tsx
  - frontend/src/ui/BagScreen.test.tsx
  - frontend/src/ui/InnPage.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/api/client.test.ts
  - frontend/src/api/parse.test.ts
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/api/characterData.test.ts
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/api/adventureData.test.ts
  - frontend/src/game/inventory.test.ts
  - frontend/src/game/battlePlayback.test.ts
  - frontend/src/game/skills.test.ts
  - frontend/src/game/world.test.ts
  - frontend/src/game/stageModel.test.ts
  - frontend/src/ui/SettingsPanel.test.tsx
  - frontend/src/game/settings.test.ts
  - frontend/src/api/worldData.test.ts
  - frontend/src/game/missions.test.ts
  - admin/src/report/compare.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/game/battleLog.test.ts
  - frontend/src/ui/create/CreateSubmit.test.tsx
  - frontend/src/api/skillData.test.ts
-->