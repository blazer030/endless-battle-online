# accounts Specification

## Purpose

Accounts let a player register a name and password and sign in to receive an access token for the game API. This spec records the account rules that existed before Spectra was adopted, so screens and later changes can rely on them.

## Requirements

### Requirement: Account registration

`POST /api/accounts/register` SHALL create an account when the username is 3 to 32 characters of letters, digits or underscores and the password has at least 8 characters, and SHALL respond 201. Usernames SHALL be unique ignoring letter case. An invalid username SHALL be rejected with 400 and the detail 帳號必須是 3～32 個英數字或底線; a short password with 400 and 密碼至少要 8 個字元; a username already taken with 409 and 帳號已被使用. Registration SHALL NOT sign the player in.

#### Scenario: Valid registration

- **WHEN** a client registers username `ethan_01` with password `abcdefgh`
- **THEN** the API responds 201 and the account can sign in with those credentials

#### Scenario: Username taken in another letter case

- **WHEN** `Ethan` exists and a client registers `ethan`
- **THEN** the API responds 409 with the detail 帳號已被使用

##### Example: registration input

| Username | Password | Response |
| --- | --- | --- |
| `ab` | `abcdefgh` | 400 帳號必須是 3～32 個英數字或底線 |
| `abc` | `abcdefgh` | 201 |
| `has space` | `abcdefgh` | 400 帳號必須是 3～32 個英數字或底線 |
| 33 letters | `abcdefgh` | 400 帳號必須是 3～32 個英數字或底線 |
| `abc` | `abcdefg` | 400 密碼至少要 8 個字元 |


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
### Requirement: Sign in

`POST /api/accounts/login` SHALL respond 200 with an access token when the username and password match an account. A wrong password and an unknown username SHALL both respond 401 with the same detail 帳號或密碼錯誤, so the response does not reveal whether the account exists. The access token SHALL expire 30 days after it is issued. Endpoints other than registration, sign-in, health and `/api/public/` SHALL respond 401 without a valid token.

#### Scenario: Unknown user and wrong password look the same

- **WHEN** a client signs in once with an unknown username and once with a known username and a wrong password
- **THEN** both responses are 401 with the detail 帳號或密碼錯誤

#### Scenario: Protected endpoint without a token

- **WHEN** a client requests `GET /api/characters/me` without a token
- **THEN** the API responds 401

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