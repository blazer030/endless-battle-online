# title-and-character-creation Specification

## Purpose

The title screen lets a visitor sign in or register on an adventurer's registry card, and the character creation screen lets a newly registered player fill in that card at the guild counter with the help of the receptionist 莉瑟. Together they take a player from opening the game to the first adventure, including the waits and failures in between.

## Requirements

### Requirement: Sign in on the registry card

The title screen SHALL show the player's own registry card with an account field, a password field and a 入城 seal. The seal SHALL be enabled only when both fields are non-empty and no request is in progress. Pressing the seal, or pressing Enter in either field, SHALL sign in. On success the screen SHALL load the character. On failure the card SHALL show the API's error detail; when the request fails without an API error the card SHALL show 發生錯誤，請稍後再試.

#### Scenario: Wrong password

- **WHEN** the player signs in with a password the API rejects
- **THEN** the own card shows 帳號或密碼錯誤 and the fields keep their values

#### Scenario: Seal disabled until both fields are filled

- **WHEN** the account field is filled and the password field is empty
- **THEN** the 入城 seal is disabled


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
### Requirement: Register with a blank card

The title screen SHALL show a stack of blank registry cards beside the own card. Tapping the stack SHALL bring a blank card to the front with its own account field, password field, password confirmation field and 登記 seal; tapping the own card SHALL bring it back. The 登記 seal SHALL stay disabled until all three fields are filled. When the two passwords differ, pressing 登記 SHALL show 兩次密碼不一樣 on the blank card and SHALL NOT call the API. Otherwise pressing 登記 SHALL register the account and then sign in with the same account and password. On success the blank card SHALL be sealed with wax and the screen SHALL load the character. When registration fails the blank card SHALL show the API's error detail. When registration succeeds but the sign-in fails, the blank card SHALL be sealed and, 0.9 seconds later, the own card SHALL come to the front with the new account name filled in, an empty password and the failure's error detail, because registering the same account again can only fail; 0.6 seconds after that the blank card SHALL be cleared and returned to the stack.

#### Scenario: Account already taken

- **WHEN** the player registers an account name that already exists
- **THEN** the blank card shows 帳號已被使用 and no sign-in is attempted

#### Scenario: Passwords do not match

- **WHEN** the player fills the blank card with the password `secret-one` and the confirmation `secret-two` and presses 登記
- **THEN** the blank card shows 兩次密碼不一樣 and no request is sent

#### Scenario: Sign-in fails after registering

- **WHEN** registration succeeds for the account `newcomer` and the following sign-in fails without an API response
- **THEN** the own card comes to the front with the account `newcomer`, an empty password and the connection error detail

#### Scenario: Successful registration leads to character creation

- **WHEN** registration and the following sign-in succeed for a new account
- **THEN** the blank card is sealed, the character is loaded, and because the account has no character the character creation screen opens


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
### Requirement: Load the character before entering

After signing in or registering, the screen SHALL enter a loading state that hides the registry cards and, after 0.45 seconds, shows three animated ink dots on the desk. When the game opens with a stored access token, the first screen SHALL be the passage background of the current time of day with three animated ink dots from the start, and SHALL NOT show the title sign, the desk or the registry cards. Loading SHALL request the player's character. When a character exists the screen SHALL show 歡迎回來 with the character's name over the passage background, and SHALL then open the game; this SHALL be the same whether loading started from signing in, registering or a stored access token. When the account has no character the character creation screen SHALL open. When the game opens with a stored access token and loading fails for any reason, including an unauthorized response, a network error or a server error, the stored token SHALL be cleared and the title screen SHALL show the registry cards with an empty account and no note. After signing in or registering, when the request is rejected as unauthorized the stored token SHALL be cleared and the title screen SHALL show the registry cards; any other failure SHALL show a note reading 公會那邊沒有回音…… with a 重試 seal that repeats the load.

#### Scenario: Opening the game while signed in

- **WHEN** the game opens with a stored access token whose account has a character named 青衣
- **THEN** the first screen shows the passage background with ink dots and neither the title sign nor the registry cards, then the ink dots give way to 歡迎回來 with 青衣, and the game opens after the transition

#### Scenario: Opening the game while signed in fails

- **WHEN** the game opens with a stored access token and loading the character fails with a network error
- **THEN** the stored token is cleared and the title screen shows the registry cards with an empty account and no 讀取失敗 note

#### Scenario: Signing in shows the character name

- **WHEN** the player signs in as ethan and the account has a character named 青衣
- **THEN** 歡迎回來 appears with 青衣 and not with the account name ethan, and the game opens after the transition

#### Scenario: Load fails and is retried

- **WHEN** after signing in, loading the character fails with a network error and the player presses 重試
- **THEN** the note disappears, the loading state returns, and a successful load continues as the first attempt would have

##### Example: result after loading

| Started from | Load result | Result |
| --- | --- | --- |
| sign in | character exists | 歡迎回來 + character name, then game |
| sign in | no character | character creation |
| sign in | unauthorized | title screen with cards, token cleared |
| sign in | network or server error | note with 重試, token kept |
| register | no character | character creation |
| stored token | character exists | passage background with ink dots, then 歡迎回來 + character name, then game |
| stored token | no character | character creation |
| stored token | unauthorized, network or server error | title screen with cards and empty account, token cleared, no note |


<!-- @trace
source: welcome-back-on-open
updated: 2026-11-11
code:
  - frontend/src/motion/timings.ts
  - frontend/src/ui/title/TitleScreen.tsx
  - frontend/src/ui/title/InkDots.css
  - frontend/src/ui/title/InkDots.tsx
  - frontend/src/ui/title/Passage.css
  - frontend/src/ui/title/TitleScreen.css
  - frontend/src/ui/App.tsx
  - frontend/src/ui/title/Passage.tsx
tests:
  - frontend/src/ui/title/TitleScreen.test.tsx
  - frontend/src/ui/App.test.tsx
-->

---
### Requirement: Title background follows the time of day

The title screen's town view SHALL use one of four images chosen by the device's local hour: dawn for 05:00–07:59, day for 08:00–16:59, dusk for 17:00–18:59, and night otherwise. The choice SHALL be re-evaluated at least once a minute while the title screen is shown.

#### Scenario: Evening

- **WHEN** the device's local time is 17:30
- **THEN** the title screen shows the dusk town image

##### Example: hour boundaries

| Local hour | Image |
| --- | --- |
| 04 | night |
| 05 | dawn |
| 07 | dawn |
| 08 | day |
| 16 | day |
| 17 | dusk |
| 18 | dusk |
| 19 | night |


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
### Requirement: Fill the registry card at the guild counter

The character creation screen SHALL show the receptionist 莉瑟 with a speech bubble and an empty registry card with the rows 名字, 性別, 出身, 屬性, 職業 and 能力, plus HP and SP previews. Tapping a row SHALL open that row's answer and change 莉瑟's line to the row's question; tapping the same row again or tapping the dimmed area SHALL close it without changing the row. The player SHALL be able to fill the rows in any order and change a filled row again. The 出發 seal SHALL be enabled only when all six rows are filled.

- 名字 SHALL accept 2 to 12 characters after trimming; otherwise 莉瑟 SHALL say 名字要 2～12 個字喔。 and the row SHALL stay unfilled.
- 性別, 出身 and 屬性 SHALL offer the options from `GET /api/public/settings`; choosing 屬性 SHALL make 莉瑟 name the elements it is strong and weak against.
- 能力 SHALL distribute 10 points over 力量, 智力, 生命 and 敏捷 with at least 1 each, showing each stat as its base value (points + 10). While points are left, the answer's confirm button SHALL be disabled and the row SHALL show how many points remain; the row SHALL be filled only when all 10 points are allocated and confirmed.
- HP and SP previews SHALL be computed from the allocation with the same formula the game uses for derived stats.

#### Scenario: Ready to depart

- **WHEN** 名字, 性別, 出身, 屬性, 職業 and 能力 have all been filled
- **THEN** the 出發 seal becomes enabled

#### Scenario: Points must all be allocated

- **WHEN** the 能力 answer is open with 4 points unspent
- **THEN** its confirm button is disabled and the 能力 row shows 剩 4 點; after the 4 points are allocated the button becomes enabled

##### Example: name length

| Input | Result |
| --- | --- |
| "艾" | rejected, 名字要 2～12 個字喔。 |
| "  艾倫  " | accepted as 艾倫 |
| 12 characters | accepted |
| 13 characters | rejected |


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
### Requirement: Job introduction book

Tapping the 職業 row SHALL open the job introduction book showing the first-tier jobs from `GET /api/public/jobs`, one page per job selected by bookmarks. A page SHALL show the job's icon, name, portrait, role, description, growth per stat with the fastest-growing stats highlighted, and every skill the job teaches with its prerequisite (or 入門 when it has none) and its one-line description from the skill content. The book SHALL NOT show a skill's maximum level or per-level effect summaries, because it introduces what each job is like rather than how to train its skills. The portrait SHALL be the content image `portraits/<job id>-female` when 性別 is 女 and `portraits/<job id>-male` otherwise, falling back to the portrait placeholder when missing. Pressing 就職 SHALL fill the 職業 row with the shown job and close the book.

#### Scenario: Female portrait

- **WHEN** 性別 is 女 and the book shows 術士 (`job-7`)
- **THEN** the page shows the content image `portraits/job-7-female`

#### Scenario: Skill entries

- **WHEN** the book shows 戰士 (`job-6`)
- **THEN** 爆氣 is marked 入門 with the description 爆發體內的鬥氣，接下來幾回合物攻提升。, 防禦心得 shows its prerequisite 爆氣 Lv3, and no 最高 Lv or level summary appears


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
### Requirement: Submit or leave character creation

Pressing 出發 SHALL create the character with the filled values. On success the screen SHALL show 踏上旅途 with the character name and then open the game. When the API responds 409 with the reason `accountHasCharacter`, the account already has a character (for example the first press of 出發 succeeded but its response timed out), so the screen SHALL load that character and continue as on success with the loaded character's name. Any other 409 SHALL make 莉瑟 say 哎呀，「<name>」已經有人登記了……換一個名字好嗎？ and the 名字 row SHALL open with the name to edit. When the API responds 400, 莉瑟 SHALL say the API's error detail. Any other failure SHALL make 莉瑟 say 咦，登記簿那邊沒有回應……, keep every filled row, and keep 出發 enabled; tapping a row or pressing 出發 again SHALL replace that line. The 離開 button SHALL ask 登記證還沒填完呢，確定現在就要離開嗎？ with 繼續填寫 and 先離開; 先離開 SHALL clear the stored access token and return to the title screen with the account name on the own card and an empty password.

#### Scenario: Name taken

- **WHEN** the player presses 出發 with the name 收尾者 and the API responds 409 with the reason `characterNameTaken`
- **THEN** 莉瑟 says 哎呀，「收尾者」已經有人登記了……換一個名字好嗎？ and the 名字 answer is open containing 收尾者

#### Scenario: Account already has a character

- **WHEN** the player presses 出發 again after an earlier press created the character 收尾者 but timed out, and the API responds 409 with the reason `accountHasCharacter`
- **THEN** the screen loads the character, shows 踏上旅途 with 收尾者, and opens the game

#### Scenario: Network failure keeps the card

- **WHEN** the player presses 出發 and the request fails without an API response
- **THEN** 莉瑟 says 咦，登記簿那邊沒有回應……, all six rows keep their values, and 出發 stays enabled

#### Scenario: Leaving signs out

- **WHEN** the player confirms 先離開
- **THEN** the stored access token is cleared and the title screen shows the own card with the registered account name and an empty password

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