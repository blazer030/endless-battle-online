# companions-screen Specification

## Purpose

The companions screen is the redesigned page for pets and the mercenary, opened from the town gate: an entrance with the stable keeper Mira and the hall master Barton, a stable where pets are raised, and a mercenary hall where the contract is managed. Feedback and errors are spoken by the keepers rather than shown as system messages.

## Requirements

### Requirement: Enter and leave the companions screen

Clicking "同伴" at the town gate SHALL open the companions entrance with a circular reveal of 0.55 seconds centred on the stage point (307, 317). The entrance's back button "城門" SHALL close it toward the same point and show the town at the gate segment with the latest character. The stable and the hall SHALL each have a back button "同伴" returning to the entrance. The companions screen SHALL make no adventure requests.

#### Scenario: Back from the entrance

- **WHEN** the player opens "同伴" and presses "城門"
- **THEN** the town is shown at the gate segment

#### Scenario: Back from the stable

- **WHEN** the player opens the stable from the entrance and presses "同伴"
- **THEN** the entrance is shown


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
### Requirement: Companions entrance

The entrance SHALL show two doors, the stable with Mira above and the mercenary hall with Barton below, each with a line and a status chip, and clicking a door SHALL open its page. Mira SHALL say `獸欄空蕩蕩的……抓到寵物就帶來給我照顧吧！` without pets, `<name>今天也很有精神喔！要來看看牠們嗎？` with a carried pet, and `孩子們都在欄裡等你呢，今天要帶誰出門？` otherwise; Barton SHALL say `想找人手？名冊上的傢伙個個都能打。` without a contract, `<name>還在<place>幹活，契約我幫你顧著。` while fighting and `<name>在後頭磨刀，隨時可以再派出去。` while resting. The stable chip SHALL read `寵物 n / 3`, followed by `・<name> 攜帶中` when a pet is carried; the hall chip SHALL read `<name>・戰鬥中`, `<name>・休息中` or `沒有契約`.

#### Scenario: Entrance with a carried pet and a fighting mercenary

- **WHEN** the character has two pets with 小風 carried and a contract named 葛雷 fighting at 迷霧森林
- **THEN** Mira says `小風今天也很有精神喔！要來看看牠們嗎？` with the chip `寵物 2 / 3・小風 攜帶中`, and Barton says `葛雷還在迷霧森林幹活，契約我幫你顧著。` with the chip `葛雷・戰鬥中`


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
### Requirement: Companions layout follows the stage height

On the stable and the hall the counter SHALL start at pt = 330 − (844 − H) × 0.45 (rounded) for stage height H, the scene above it SHALL be pt + 20 tall, and the pet details, fruits and contract SHALL scroll inside the counter's remaining height; the page itself SHALL NOT scroll.

#### Scenario: Counter top for stage heights

- **WHEN** the stable is shown on stages of different heights
- **THEN** the counter starts at pt

##### Example: counter top

| Stage height | pt |
| --- | --- |
| 624 | 231 |
| 683 | 258 |
| 720 | 274 |
| 780 | 301 |


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
### Requirement: Stable

The stable SHALL show three stalls; each pet SHALL appear in a stall with its name plate and a `攜帶` tag when carried, empty stalls SHALL show an empty plate, and the selected pet's stall SHALL be lit. The pet's image SHALL be the content image `monsters/<monster id>`, or the shared pet silhouette when that image is missing or fails to load. The counter SHALL show the selected pet's name, species and age as `<species>・<age> 歲`, personality, attack, magic, defense and spirit, and bars for satiety (out of 100), intimacy (out of 255, full when above) and activation (out of 35%), each capped at full.

#### Scenario: Bars for a pet

- **WHEN** a pet has satiety 72, intimacy 312 and activation 35
- **THEN** the satiety bar is 72% full and the intimacy and activation bars are full


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
### Requirement: Feeding from the fruit trough

The trough SHALL show the eight fruits with their effect (for example `AT+2`) and price. A fruit SHALL be disabled while the selected pet's next feed time has not come (with a 10-second countdown line), when its satiety is 100 or more, or when the money is below the price, which SHALL then be marked insufficient. A successful feeding SHALL update the pet and the money, make the pet hop and float `<stat> +<amount>　♥ +<intimacy gained>` above its stall, and say nothing.

#### Scenario: Feeding a fruit

- **WHEN** a pet with intimacy 88 is fed a small vigor fruit and comes back with intimacy 92
- **THEN** `AT +2　♥ +4` floats above its stall and the money drops by 20

#### Scenario: Fruits disabled

- **WHEN** the selected pet has satiety 100
- **THEN** all eight fruits are disabled


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
### Requirement: Pet actions

The stable SHALL let the player carry or store the selected pet, rename it with a character count `n / 12` (confirm disabled at 0 and input limited to 12), and release it after a second confirmation `確定放生`.

#### Scenario: Releasing needs confirmation

- **WHEN** the player presses `放生`
- **THEN** `確定放生` and `取消` replace it, and only `確定放生` releases the pet


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
### Requirement: Mercenary hall

The hall SHALL show the catalogue as five notes by grade with the price (amounts of 10,000 or more written as `n萬`, insufficient marked), or the continent and kingdom medals for honour-only grades, and a hired stamp on the contracted grade. Clicking a note SHALL open a detail sheet with fights, money, drop and experience percents, death cost and a `僱用` seal; the seal SHALL be disabled when the money is below the price or a contract exists, and absent for honour-only grades, which SHALL show `只能在勳章所兌換`. The contract SHALL be shown as a sheet with its name and rename (`n / 8`), a status stamp `戰鬥中` or `休息中`, remaining fights with a bar out of the catalogue's fights, the place, the shares, `派遣` or `召回休息`, and `解約` with a second confirmation. `派遣` SHALL open a sheet listing deployable places grouped by continent with their recommended levels `Lv a–b`, excluding restricted places.

#### Scenario: Price text

- **WHEN** catalogue prices are shown
- **THEN** they follow the short format

##### Example: prices

| Price | Text |
| --- | --- |
| 500 | 500 |
| 100,000 | 10萬 |
| 1,000,000 | 100萬 |

#### Scenario: Deployable places

- **WHEN** the deploy sheet opens
- **THEN** it lists 17 places under their continents and no restricted place


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
### Requirement: Keepers speak feedback and errors

Results SHALL be spoken in the counter header, replacing its title with the keeper's face and one line, the line marked failed for errors; when a detail sheet is open the line SHALL appear inside the sheet above its seal. The line SHALL return to the title at the next action. No system message box SHALL be shown. When an action is rejected with `notEnoughMoney` the character SHALL be reloaded so that the shown money is current.

#### Scenario: Lines for each result

- **WHEN** an action ends
- **THEN** the keeper says the line for it

##### Example: lines

| Result | Keeper | Line |
| --- | --- | --- |
| carried 小風 | Mira | 小風會跟著你出門囉！ |
| stored 小風 | Mira | 小風回欄裡休息囉。 |
| renamed to 小風 | Mira | 「小風」，牠很喜歡喔！ |
| released 小風 | Mira | 小風回到野外去了，要保重喔。 |
| feeding rejected `overOwner` | Mira | 再吃下去，牠就要比你還強了呢。 |
| feeding rejected `notEnoughMoney` | Mira | 哎呀，這顆果實的錢好像不太夠。 |
| pet rename rejected `nameInvalid` | Mira | 這名字牠好像記不住呢。 |
| feeding rejected `full` | Mira | 牠已經吃得飽飽的了。 |
| feeding rejected `tooSoon` | Mira | 慢慢來，讓牠先嚼完這顆。 |
| pet action failed otherwise | Mira | 咦，剛剛沒弄好……再試一次吧？ |
| hired F grade | Barton | 契約簽好了，F級傭兵歸你使喚。 |
| deployed 葛雷 to 迷霧森林 | Barton | 葛雷往迷霧森林出發了。 |
| rested 葛雷 | Barton | 葛雷回來歇口氣了。 |
| dismissed 葛雷 | Barton | 葛雷的契約，我收回來了。 |
| renamed mercenary to 葛雷 | Barton | 葛雷？行，名冊上改好了。 |
| hire rejected `notEnoughMoney` | Barton | 錢不夠，這張契約簽不了。 |
| hire rejected `mercenaryHired` with 葛雷 | Barton | 一次只接一份契約，先把葛雷的處理掉。 |
| hire rejected `honourOnly` | Barton | 那兩位得去勳章所換，我這兒不賣。 |
| deploy rejected `placeRestricted` | Barton | 那裡是禁區，我的人不去送死。 |
| mercenary rename rejected `nameInvalid` | Barton | 這名字名冊上寫不了，換一個。 |
| mercenary action failed otherwise | Barton | 嘖，文書出了點差錯，再來一次。 |


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
### Requirement: Companions load failure

When the pets or the mercenaries cannot be loaded at the entrance, Mira SHALL say `咦，獸欄的名簿一下子找不到了……`, Barton SHALL say `嘖，名冊一時翻不到……`, the chips SHALL be hidden and a `再試一次` seal SHALL sit on the beam; the doors SHALL stay usable and each page SHALL load again. When the stable or the hall cannot load, its scene SHALL show only empty stalls or the empty roster board, the keeper SHALL say `咦，欄裡的名簿一時找不到……` or `嘖，名冊一時翻不到……` in the header, and the counter SHALL show only a `再試一次` seal; pressing it SHALL load again.

#### Scenario: Retry at the entrance

- **WHEN** loading the mercenaries fails once and the player presses `再試一次`
- **THEN** both lines return to the greetings and the chips are shown

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