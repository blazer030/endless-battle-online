# public-game-data Specification

## Purpose

Public game data is game content that any visitor can read without signing in, such as the job list with its skills and the options offered when creating a character. Each kind of data has its own endpoint under one public prefix, so a screen requests only what it shows and the data can later move to a database without one endpoint gathering unrelated tables.

## Requirements

### Requirement: Public data prefix

Endpoints that serve game data readable without signing in SHALL live under `/api/public/` and SHALL respond without requiring authentication. A request carrying an expired or invalid access token SHALL receive the same public response as a request without a token. Screens SHALL read sex, element, birthplace and job data from these endpoints.

#### Scenario: Reading public data without signing in

- **WHEN** a client requests `GET /api/public/jobs` or `GET /api/public/settings` without an `Authorization` header
- **THEN** the API responds 200 with the data

#### Scenario: Expired token does not block public data

- **WHEN** a client requests `GET /api/public/settings` with an expired access token
- **THEN** the API responds 200 with the same body as a request without a token


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
### Requirement: Public job list with skills

`GET /api/public/jobs` SHALL return every job in the game content in content order. Each job SHALL carry its id, name, tier, parent job id (null for first-tier jobs), unlock level, required proficiency, stat requirements, and growth as a minimum and maximum per stat using full stat names (`strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma`, `faith`). Each job SHALL list the skills its job teaches, in the same order as the skill page lists them (the order of `backend/content/skills.json`); each skill SHALL carry its id, name, description, maximum level, prerequisite (skill id, skill name and required level, or null), and one entry per level with the level number and the effect summary text that the skill page shows for that level.

#### Scenario: Job carries its skills in teaching order

- **WHEN** a client requests `GET /api/public/jobs`
- **THEN** each job lists the skills that `backend/content/job-skills.json` assigns to it, in the same order as the skill page, each with a summary for every level from 1 to its maximum level

##### Example: warrior skills

- **GIVEN** the official content, where `job-6` (戰士) teaches `skill-1` 爆氣 (max level 10, no prerequisite), `skill-61` 防禦心得 (max level 5, prerequisite 爆氣 level 3), `skill-71` 破滅一擊 and `skill-77` 嗜血
- **WHEN** a client requests `GET /api/public/jobs`
- **THEN** the `job-6` entry lists 爆氣, 防禦心得, 破滅一擊, 嗜血 in that order; 防禦心得 has `maxLevel` 5, prerequisite `{ "skillId": "skill-1", "name": "爆氣", "level": 3 }` and 5 level entries; 爆氣 has prerequisite null and 10 level entries

#### Scenario: Level summary matches the skill page

- **WHEN** the job list includes a skill level
- **THEN** that level's summary text equals the summary the skill page shows for the same skill and level


<!-- @trace
source: title-and-create-screens
updated: 2026-11-10
code:
  - frontend/src/ui/create/art.ts
  - frontend/src/game/jobBook.ts
  - frontend/src/assets/title/bookmark-thief.webp
  - frontend/src/ui/title/TitleBackdrop.tsx
  - frontend/src/ui/title/TitleScreen.tsx
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Api/Errors/ApplicationExceptionHandler.cs
  - backend/EndlessBattle.Infrastructure/Persistence/MySqlErrors.cs
  - frontend/src/assets/title/card-sealed.webp
  - frontend/src/ui/CharacterScreen.css
  - frontend/src/ui/screens.css
  - backend/EndlessBattle.Application/Characters/ICharacterRepository.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/assets/title/element-fire.webp
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/ui/CreateCharacterScreen.tsx
  - frontend/src/assets/title/element-water.webp
  - frontend/src/ui/theme.css
  - frontend/src/ui/title/TitleBackdrop.css
  - backend/content/skills.json
  - frontend/src/assets/title/sky-day.webp
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - frontend/src/assets/title/desk-edge.webp
  - frontend/src/ui/stage/StageFrame.css
  - frontend/src/assets/title/bookmark-bard.webp
  - backend/EndlessBattle.Tests/Api/AccountEndpointsTests.cs
  - docs/art/README.md
  - frontend/src/assets/title/bubble.webp
  - frontend/src/assets/title/sky-dawn.webp
  - frontend/src/ui/CharacterScreen.tsx
  - frontend/src/ui/create/CreateScreen.css
  - frontend/src/assets/title/card-blank.webp
  - frontend/src/ui/AdventureScreen.tsx
  - backend/EndlessBattle.Domain/Battle/ElementTable.cs
  - frontend/src/api/publicData.ts
  - frontend/src/assets/title/note.webp
  - frontend/src/game/creationOptions.ts
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Api/PublicEndpointsTests.cs
  - .env.nas.local.example
  - frontend/src/ui/stage/StageFrame.tsx
  - CLAUDE.md
  - frontend/index.html
  - frontend/src/ui/useContentArtUrl.ts
  - frontend/src/assets/title/sign.webp
  - frontend/src/game/creationDraft.ts
  - frontend/src/ui/CreateCharacterScreen.css
  - frontend/src/assets/title/hall.webp
  - frontend/src/assets/title/sky-dusk.webp
  - frontend/src/ui/title/Passage.tsx
  - frontend/src/ui/create/JobBook.css
  - frontend/src/ui/title/Seal.tsx
  - backend/EndlessBattle.Tests/Support/InMemoryCharacterRepository.cs
  - frontend/src/assets/title/bookmark-sorcerer.webp
  - frontend/src/assets/title/desk.webp
  - frontend/src/ui/App.tsx
  - frontend/src/api/schema.ts
  - frontend/src/assets/title/element-earth.webp
  - frontend/src/assets/title/bookmark-warrior.webp
  - frontend/src/api/client.ts
  - frontend/src/assets/title/element-wood.webp
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/assets/content-manifest.json
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - docs/superpowers/specs/2026-10-22-wog-remake-rulings.md
  - frontend/src/ui/create/JobBook.tsx
  - frontend/src/ui/stage/fitStage.ts
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - scripts/deploy-nas.sh
  - backend/EndlessBattle.Application/Common/ApplicationExceptions.cs
  - frontend/src/assets/title/book-page.webp
  - frontend/src/assets/title/element-poison.webp
  - frontend/src/game/timeOfDay.ts
  - frontend/src/assets/title/clerk.webp
  - backend/EndlessBattle.Api/Program.cs
  - frontend/src/assets/title/seal.webp
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - frontend/src/assets/title/sky-night.webp
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/ui/create/flavor.ts
  - frontend/src/ui/title/Passage.css
  - frontend/src/ui/title/useTimeOfDay.ts
  - frontend/src/ui/AuthScreen.tsx
  - frontend/src/ui/title/Seal.css
  - backend/openapi/EndlessBattle.Api.json
  - frontend/src/ui/ContentImage.tsx
  - frontend/src/api/parse.ts
  - frontend/src/assets/title/element-wind.webp
  - backend/EndlessBattle.Domain/Characters/CharacterOptions.cs
  - backend/EndlessBattle.Api/Endpoints/PublicEndpoints.cs
  - backend/EndlessBattle.Application/DependencyInjection.cs
  - frontend/src/motion/timings.ts
  - frontend/src/ui/title/TitleScreen.css
tests:
  - frontend/src/api/publicData.test.ts
  - frontend/src/ui/stage/StageFrame.test.tsx
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/game/timeOfDay.test.ts
  - frontend/src/ui/create/JobBook.test.tsx
  - frontend/src/ui/theme.test.ts
  - frontend/src/ui/create/CreateScreen.test.tsx
  - frontend/src/ui/AuthScreen.test.tsx
  - frontend/src/ui/CreateCharacterScreen.test.tsx
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/ui/stage/fitStage.test.ts
  - frontend/src/ui/title/TitleBackdrop.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/ui/title/TitleScreen.test.tsx
  - frontend/src/ui/create/CreateSubmit.test.tsx
-->

---
### Requirement: Public character options

`GET /api/public/settings` SHALL return the options used to create and describe a character: sexes, elements and birthplaces, each as a numeric id with a name, in id order. Each element SHALL also name the element it is strong against and the element it is weak against, taken from the battle rules, so a screen can describe element advantage without repeating the rule.

#### Scenario: Options are listed with numeric ids

- **WHEN** a client requests `GET /api/public/settings`
- **THEN** the response lists sexes 1 男 and 2 女, elements 1 地 to 6 毒, and birthplaces 1 中央大陸 to 4 末日王城

#### Scenario: Element advantage comes from the battle rules

- **WHEN** a client reads an element from `GET /api/public/settings`
- **THEN** its strong-against and weak-against elements follow the battle rule that each element beats the next one in the cycle 地 → 水 → 火 → 木 → 風 → 毒 → 地

##### Example: fire

- **GIVEN** the element 火 with id 3
- **WHEN** a client reads it from `GET /api/public/settings`
- **THEN** it is strong against 木 (id 4) and weak against 水 (id 2)

<!-- @trace
source: title-and-create-screens
updated: 2026-11-10
code:
  - frontend/src/ui/create/art.ts
  - frontend/src/game/jobBook.ts
  - frontend/src/assets/title/bookmark-thief.webp
  - frontend/src/ui/title/TitleBackdrop.tsx
  - frontend/src/ui/title/TitleScreen.tsx
  - backend/EndlessBattle.Domain/Characters/CharacterFactory.cs
  - backend/EndlessBattle.Api/Errors/ApplicationExceptionHandler.cs
  - backend/EndlessBattle.Infrastructure/Persistence/MySqlErrors.cs
  - frontend/src/assets/title/card-sealed.webp
  - frontend/src/ui/CharacterScreen.css
  - frontend/src/ui/screens.css
  - backend/EndlessBattle.Application/Characters/ICharacterRepository.cs
  - backend/EndlessBattle.Tests/Infrastructure/CharacterRepositoryTests.cs
  - backend/EndlessBattle.Tests/Domain/CharacterFactoryTests.cs
  - frontend/src/assets/title/element-fire.webp
  - backend/EndlessBattle.Tests/Api/InventoryEndpointsTests.cs
  - frontend/src/ui/CreateCharacterScreen.tsx
  - frontend/src/assets/title/element-water.webp
  - frontend/src/ui/theme.css
  - frontend/src/ui/title/TitleBackdrop.css
  - backend/content/skills.json
  - frontend/src/assets/title/sky-day.webp
  - backend/EndlessBattle.Tests/Content/BalanceTests.cs
  - frontend/src/assets/title/desk-edge.webp
  - frontend/src/ui/stage/StageFrame.css
  - frontend/src/assets/title/bookmark-bard.webp
  - backend/EndlessBattle.Tests/Api/AccountEndpointsTests.cs
  - docs/art/README.md
  - frontend/src/assets/title/bubble.webp
  - frontend/src/assets/title/sky-dawn.webp
  - frontend/src/ui/CharacterScreen.tsx
  - frontend/src/ui/create/CreateScreen.css
  - frontend/src/assets/title/card-blank.webp
  - frontend/src/ui/AdventureScreen.tsx
  - backend/EndlessBattle.Domain/Battle/ElementTable.cs
  - frontend/src/api/publicData.ts
  - frontend/src/assets/title/note.webp
  - frontend/src/game/creationOptions.ts
  - backend/EndlessBattle.Api/Endpoints/Contracts.cs
  - backend/EndlessBattle.Tests/Api/PublicEndpointsTests.cs
  - .env.nas.local.example
  - frontend/src/ui/stage/StageFrame.tsx
  - CLAUDE.md
  - frontend/index.html
  - frontend/src/ui/useContentArtUrl.ts
  - frontend/src/assets/title/sign.webp
  - frontend/src/game/creationDraft.ts
  - frontend/src/ui/CreateCharacterScreen.css
  - frontend/src/assets/title/hall.webp
  - frontend/src/assets/title/sky-dusk.webp
  - frontend/src/ui/title/Passage.tsx
  - frontend/src/ui/create/JobBook.css
  - frontend/src/ui/title/Seal.tsx
  - backend/EndlessBattle.Tests/Support/InMemoryCharacterRepository.cs
  - frontend/src/assets/title/bookmark-sorcerer.webp
  - frontend/src/assets/title/desk.webp
  - frontend/src/ui/App.tsx
  - frontend/src/api/schema.ts
  - frontend/src/assets/title/element-earth.webp
  - frontend/src/assets/title/bookmark-warrior.webp
  - frontend/src/api/client.ts
  - frontend/src/assets/title/element-wood.webp
  - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
  - frontend/src/assets/content-manifest.json
  - backend/EndlessBattle.Infrastructure/Persistence/CharacterRepository.cs
  - backend/EndlessBattle.Application/Jobs/PublicJobService.cs
  - docs/superpowers/specs/2026-10-22-wog-remake-rulings.md
  - frontend/src/ui/create/JobBook.tsx
  - frontend/src/ui/stage/fitStage.ts
  - backend/EndlessBattle.Tests/Support/ApiClientExtensions.cs
  - scripts/deploy-nas.sh
  - backend/EndlessBattle.Application/Common/ApplicationExceptions.cs
  - frontend/src/assets/title/book-page.webp
  - frontend/src/assets/title/element-poison.webp
  - frontend/src/game/timeOfDay.ts
  - frontend/src/assets/title/clerk.webp
  - backend/EndlessBattle.Api/Program.cs
  - frontend/src/assets/title/seal.webp
  - backend/EndlessBattle.Tests/Api/CharacterEndpointsTests.cs
  - frontend/src/assets/title/sky-night.webp
  - frontend/src/ui/create/CreateScreen.tsx
  - frontend/src/ui/create/flavor.ts
  - frontend/src/ui/title/Passage.css
  - frontend/src/ui/title/useTimeOfDay.ts
  - frontend/src/ui/AuthScreen.tsx
  - frontend/src/ui/title/Seal.css
  - backend/openapi/EndlessBattle.Api.json
  - frontend/src/ui/ContentImage.tsx
  - frontend/src/api/parse.ts
  - frontend/src/assets/title/element-wind.webp
  - backend/EndlessBattle.Domain/Characters/CharacterOptions.cs
  - backend/EndlessBattle.Api/Endpoints/PublicEndpoints.cs
  - backend/EndlessBattle.Application/DependencyInjection.cs
  - frontend/src/motion/timings.ts
  - frontend/src/ui/title/TitleScreen.css
tests:
  - frontend/src/api/publicData.test.ts
  - frontend/src/ui/stage/StageFrame.test.tsx
  - frontend/src/game/creationDraft.test.ts
  - frontend/src/test/fixtures.ts
  - frontend/src/game/timeOfDay.test.ts
  - frontend/src/ui/create/JobBook.test.tsx
  - frontend/src/ui/theme.test.ts
  - frontend/src/ui/create/CreateScreen.test.tsx
  - frontend/src/ui/AuthScreen.test.tsx
  - frontend/src/ui/CreateCharacterScreen.test.tsx
  - frontend/src/ui/CharacterScreen.test.tsx
  - frontend/src/ui/useAdventureLoop.test.tsx
  - frontend/src/ui/AdventureScreen.test.tsx
  - frontend/src/ui/stage/fitStage.test.ts
  - frontend/src/ui/title/TitleBackdrop.test.tsx
  - frontend/src/ui/App.test.tsx
  - frontend/src/ui/title/TitleScreen.test.tsx
  - frontend/src/ui/create/CreateSubmit.test.tsx
-->