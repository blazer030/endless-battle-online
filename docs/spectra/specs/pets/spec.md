# pets Specification

## Purpose

Pets are monsters the character has captured. The character keeps at most three, carries at most one into adventure, and raises them by feeding fruits. These requirements describe the stable that the companions screen shows and acts on; capture and the pets' part in battle are described elsewhere.

## Requirements

### Requirement: Pet listing

`GET /api/pets` SHALL return the character's pets with id, monster id, species name, name, personality, attack, magic, defense, spirit, satiety, intimacy, whether carried, age, the time from which the pet can be fed again, and the estimated activation percent against a monster with the owner's charisma (at most 35); it SHALL also return the maximum of 3 pets and the fruit menu. A pet's age SHALL be the time since it was born divided by 864,000 seconds (ten days per year), rounded like PHP.

#### Scenario: Listing a pet

- **WHEN** a character with one pet born 20 days ago requests its pets
- **THEN** the response lists that pet with age 2, the maximum 3 and eight fruits


<!-- @trace
source: companions-screen
updated: 2026-11-14
code:
  - frontend/src/ui/companions/useKeeperPage.ts
  - frontend/src/game/names.ts
  - frontend/src/ui/companions/RenameField.tsx
  - frontend/src/game/companions.ts
  - frontend/src/ui/stage/screenImages.ts
  - frontend/src/ui/companions/HallPage.tsx
  - backend/EndlessBattle.Tests/Application/PetServiceTests.cs
  - frontend/src/render/contentAssets.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/companions/CompanionsScreen.css
  - frontend/src/ui/companions/StablePage.tsx
  - frontend/src/ui/companions/CompanionsScreen.tsx
  - backend/EndlessBattle.Tests/Application/MercenaryServiceTests.cs
  - frontend/src/ui/GameScreen.tsx
  - frontend/src/ui/stage/preloadImages.ts
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - frontend/src/ui/App.tsx
tests:
  - frontend/src/ui/companions/RenameField.test.tsx
  - frontend/src/ui/companions/HallPage.test.tsx
  - frontend/src/ui/GameScreen.test.tsx
  - frontend/src/ui/companions/CompanionsScreen.test.tsx
  - frontend/src/ui/companions/StablePage.test.tsx
  - frontend/src/ui/stage/screenImages.test.ts
-->

---
### Requirement: Fruit menu

The fruit menu SHALL be eight fruits: small vigor, aroma, sweet and wild fruits raising attack, magic, defense and spirit by 2 for 20 money, and medium ones raising the same stats by 4 for 50 money.

#### Scenario: Menu entries

- **WHEN** the fruit menu is listed
- **THEN** it holds the eight fruits with their stat, amount and cost

##### Example: fruits

| Fruit | Stat | Amount | Cost |
| --- | --- | --- | --- |
| 初級活力果實 | attack | 2 | 20 |
| 初級香氣果實 | magic | 2 | 20 |
| 初級甜味果實 | defense | 2 | 20 |
| 初級野性果實 | spirit | 2 | 20 |
| 中級活力果實 | attack | 4 | 50 |
| 中級野性果實 | spirit | 4 | 50 |


<!-- @trace
source: companions-screen
updated: 2026-11-14
code:
  - frontend/src/ui/companions/useKeeperPage.ts
  - frontend/src/game/names.ts
  - frontend/src/ui/companions/RenameField.tsx
  - frontend/src/game/companions.ts
  - frontend/src/ui/stage/screenImages.ts
  - frontend/src/ui/companions/HallPage.tsx
  - backend/EndlessBattle.Tests/Application/PetServiceTests.cs
  - frontend/src/render/contentAssets.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/companions/CompanionsScreen.css
  - frontend/src/ui/companions/StablePage.tsx
  - frontend/src/ui/companions/CompanionsScreen.tsx
  - backend/EndlessBattle.Tests/Application/MercenaryServiceTests.cs
  - frontend/src/ui/GameScreen.tsx
  - frontend/src/ui/stage/preloadImages.ts
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - frontend/src/ui/App.tsx
tests:
  - frontend/src/ui/companions/RenameField.test.tsx
  - frontend/src/ui/companions/HallPage.test.tsx
  - frontend/src/ui/GameScreen.test.tsx
  - frontend/src/ui/companions/CompanionsScreen.test.tsx
  - frontend/src/ui/companions/StablePage.test.tsx
  - frontend/src/ui/stage/screenImages.test.ts
-->

---
### Requirement: Feeding a pet

Feeding SHALL be rejected, checking in this order and reporting only the first reason: `tooSoon` before the pet's next feed time (10 seconds after it was last fed), `full` when satiety is 100 or more, `overOwner` when the raised stat would exceed the owner's effective stat including equipment (attack against physical attack, magic against magic attack, defense against the higher of physical and magic defense; spirit has no cap), and `notEnoughMoney` when the money is below the fruit's cost. An unknown fruit SHALL be rejected with `foodUnknown`. Otherwise feeding SHALL deduct the cost, raise the stat by the fruit's amount, add a random satiety in the fruit's range (small 10–20, medium 18–28; satiety above 100 is kept), add a random intimacy of 2–6 (no upper limit), restart the 10-second wait, and return the updated pets and character.

#### Scenario: Feeding raises the pet

- **WHEN** a pet with attack 30 and satiety 40 is fed a small vigor fruit by a character with 100 money
- **THEN** the character has 80 money, the pet's attack is 32, its satiety rises by 10–20 and its intimacy by 2–6

#### Scenario: Feeding is refused

- **WHEN** feeding is attempted in each refused state
- **THEN** the first applicable reason is reported and nothing changes

##### Example: refusals

| State | Reason |
| --- | --- |
| fed 5 seconds ago | tooSoon |
| satiety 100 | full |
| pet attack 30, owner attack 31, medium vigor fruit | overOwner |
| 10 money, small fruit | notEnoughMoney |


<!-- @trace
source: companions-screen
updated: 2026-11-14
code:
  - frontend/src/ui/companions/useKeeperPage.ts
  - frontend/src/game/names.ts
  - frontend/src/ui/companions/RenameField.tsx
  - frontend/src/game/companions.ts
  - frontend/src/ui/stage/screenImages.ts
  - frontend/src/ui/companions/HallPage.tsx
  - backend/EndlessBattle.Tests/Application/PetServiceTests.cs
  - frontend/src/render/contentAssets.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/companions/CompanionsScreen.css
  - frontend/src/ui/companions/StablePage.tsx
  - frontend/src/ui/companions/CompanionsScreen.tsx
  - backend/EndlessBattle.Tests/Application/MercenaryServiceTests.cs
  - frontend/src/ui/GameScreen.tsx
  - frontend/src/ui/stage/preloadImages.ts
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - frontend/src/ui/App.tsx
tests:
  - frontend/src/ui/companions/RenameField.test.tsx
  - frontend/src/ui/companions/HallPage.test.tsx
  - frontend/src/ui/GameScreen.test.tsx
  - frontend/src/ui/companions/CompanionsScreen.test.tsx
  - frontend/src/ui/companions/StablePage.test.tsx
  - frontend/src/ui/stage/screenImages.test.ts
-->

---
### Requirement: Carrying and storing pets

Carrying a pet SHALL make it the only carried pet and put every other pet in the stable; storing a pet SHALL put it in the stable. An unknown pet SHALL be rejected with `petUnknown`.

#### Scenario: Switching the carried pet

- **WHEN** pet A is carried and the character carries pet B
- **THEN** B is carried and A is in the stable


<!-- @trace
source: companions-screen
updated: 2026-11-14
code:
  - frontend/src/ui/companions/useKeeperPage.ts
  - frontend/src/game/names.ts
  - frontend/src/ui/companions/RenameField.tsx
  - frontend/src/game/companions.ts
  - frontend/src/ui/stage/screenImages.ts
  - frontend/src/ui/companions/HallPage.tsx
  - backend/EndlessBattle.Tests/Application/PetServiceTests.cs
  - frontend/src/render/contentAssets.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/companions/CompanionsScreen.css
  - frontend/src/ui/companions/StablePage.tsx
  - frontend/src/ui/companions/CompanionsScreen.tsx
  - backend/EndlessBattle.Tests/Application/MercenaryServiceTests.cs
  - frontend/src/ui/GameScreen.tsx
  - frontend/src/ui/stage/preloadImages.ts
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - frontend/src/ui/App.tsx
tests:
  - frontend/src/ui/companions/RenameField.test.tsx
  - frontend/src/ui/companions/HallPage.test.tsx
  - frontend/src/ui/GameScreen.test.tsx
  - frontend/src/ui/companions/CompanionsScreen.test.tsx
  - frontend/src/ui/companions/StablePage.test.tsx
  - frontend/src/ui/stage/screenImages.test.ts
-->

---
### Requirement: Renaming and releasing pets

Renaming a pet SHALL trim surrounding spaces and accept 1 to 12 characters counted as Unicode code points, otherwise reject with `nameInvalid`. Releasing a pet SHALL remove it.

#### Scenario: Name length

- **WHEN** a pet is renamed
- **THEN** the trimmed name is accepted only when it has 1 to 12 characters

##### Example: names

| Name | Result |
| --- | --- |
| `  小風  ` | 小風 |
| (empty) | nameInvalid |
| 13 characters | nameInvalid |

#### Scenario: Releasing

- **WHEN** a pet is released
- **THEN** it is no longer listed

<!-- @trace
source: companions-screen
updated: 2026-11-14
code:
  - frontend/src/ui/companions/useKeeperPage.ts
  - frontend/src/game/names.ts
  - frontend/src/ui/companions/RenameField.tsx
  - frontend/src/game/companions.ts
  - frontend/src/ui/stage/screenImages.ts
  - frontend/src/ui/companions/HallPage.tsx
  - backend/EndlessBattle.Tests/Application/PetServiceTests.cs
  - frontend/src/render/contentAssets.ts
  - backend/EndlessBattle.Tests/Domain/Mercenaries/MercenarySimulatorTests.cs
  - frontend/src/ui/companions/CompanionsScreen.css
  - frontend/src/ui/companions/StablePage.tsx
  - frontend/src/ui/companions/CompanionsScreen.tsx
  - backend/EndlessBattle.Tests/Application/MercenaryServiceTests.cs
  - frontend/src/ui/GameScreen.tsx
  - frontend/src/ui/stage/preloadImages.ts
  - backend/EndlessBattle.Tests/Domain/Pets/PetCareTests.cs
  - frontend/src/ui/App.tsx
tests:
  - frontend/src/ui/companions/RenameField.test.tsx
  - frontend/src/ui/companions/HallPage.test.tsx
  - frontend/src/ui/GameScreen.test.tsx
  - frontend/src/ui/companions/CompanionsScreen.test.tsx
  - frontend/src/ui/companions/StablePage.test.tsx
  - frontend/src/ui/stage/screenImages.test.ts
-->