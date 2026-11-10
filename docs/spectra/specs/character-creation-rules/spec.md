# character-creation-rules Specification

## Purpose

Character creation rules decide what a new account can choose for its single character and what the character starts with. This spec records the rules that existed before Spectra was adopted, which follow the original WOG 4.5 creation rules, except that all 10 points must now be allocated.

## Requirements

### Requirement: One named character per account

`POST /api/characters` SHALL create the signed-in account's only character. The name SHALL be trimmed of surrounding whitespace and SHALL then be 2 to 12 characters, counting each user-perceived character (including a combined emoji) as one; a name that fits that count but exceeds 12 code points SHALL also be rejected. Invalid names SHALL respond 400. Character names SHALL be unique ignoring letter case, and an account SHALL have at most one character; either conflict SHALL respond 409, and the response's `reasons` SHALL name which one happened: `accountHasCharacter` with the detail 此帳號已有角色 when the account already has a character, or `characterNameTaken` with the detail 角色名稱已被使用 when another character has the name. `GET /api/characters/me` SHALL respond 404 when the account has no character.

#### Scenario: Name is trimmed

- **WHEN** a character is created with the name `  艾倫  `
- **THEN** the character's name is `艾倫`

#### Scenario: Second character for the same account

- **WHEN** an account that already has a character creates another
- **THEN** the API responds 409 with the reason `accountHasCharacter` and the first character is unchanged

#### Scenario: Name used by another account

- **WHEN** a new account creates a character with the name of another account's character in a different letter case
- **THEN** the API responds 409 with the reason `characterNameTaken` and no character is created

##### Example: name length

| Name | Response |
| --- | --- |
| `艾` | 400 |
| `艾倫` | 201 |
| 12 Chinese characters | 201 |
| 13 Chinese characters | 400 |


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
### Requirement: Creation choices and allocation

A new character SHALL choose a sex (1 男, 2 女), an element (1 地 to 6 毒), a birthplace (1 中央大陸 to 4 末日王城) and a first-tier job; any other value SHALL respond 400. The player SHALL distribute exactly 10 points over strength, intelligence, life and agility with at least 1 point each. Any stat below 1 SHALL respond 400 with the detail 每項能力至少要分配 1 點; a total other than 10 SHALL respond 400 with the detail 能力點數必須剛好分完 10 點.

#### Scenario: Unspent points are rejected

- **WHEN** a character is created with strength 1, intelligence 1, life 1 and agility 3
- **THEN** the API responds 400 with the detail 能力點數必須剛好分完 10 點 and no character is created

##### Example: allocation totals

| Strength / Intelligence / Life / Agility | Response |
| --- | --- |
| 1 / 1 / 1 / 3 (6) | 400 能力點數必須剛好分完 10 點 |
| 4 / 2 / 3 / 1 (10) | 201 |
| 4 / 2 / 3 / 2 (11) | 400 能力點數必須剛好分完 10 點 |
| 0 / 4 / 3 / 3 (10) | 400 每項能力至少要分配 1 點 |

#### Scenario: Advanced job at creation

- **WHEN** a character is created with a job whose tier is not 1
- **THEN** the API responds 400 with the detail 只能選擇初階職業


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
### Requirement: Starting state

A new character SHALL start at level 1 with 0 experience, 1000 experience to the next level, 2000 money, 50 HP and 25 SP, at the content's starting place. Its base strength, intelligence, life and agility SHALL each be its allocated points plus 10, and its vitality, charisma and faith SHALL each be 10.

#### Scenario: Base stats from allocation

- **WHEN** a character is created with strength 4, intelligence 2, life 3 and agility 1
- **THEN** its base stats are strength 14, intelligence 12, life 13, agility 11, vitality 10, charisma 10 and faith 10, and it starts at level 1 with 2000 money, 50 HP and 25 SP at the starting place

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