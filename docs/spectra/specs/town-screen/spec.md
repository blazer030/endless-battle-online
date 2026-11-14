# town-screen Specification

## Purpose

The town is the hub a player lands in after the character is loaded: a three-part panorama of the town whose signs lead to each facility, with the character's status and active bonuses pinned on top. Staying in the town counts as being offline, so the client makes no adventure requests until the player opens the adventure or another facility.

## Requirements

### Requirement: Enter the town after loading the character

The client SHALL show the town at its gate segment whenever a signed-in player's character becomes available: after the opening load finds a character, after the "歡迎回來" passage, and after character creation succeeds. The client SHALL NOT show the old tabbed screen directly at these moments.

#### Scenario: Opening the game with a character

- **WHEN** a player with a stored token opens the game and the character loads
- **THEN** the town is shown at the gate segment and no adventure request has been sent

#### Scenario: Character created

- **WHEN** character creation succeeds
- **THEN** the town is shown at the gate segment with the new character's name, job and level

---
### Requirement: Town panorama segments

The town SHALL consist of three segments in order: the gate (segment 0), the market (segment 1) and the guild square (segment 2). The panorama SHALL be scaled by k = stage height / 844 and SHALL be shifted left by 0 at the gate, by 875k − 195 at the market, and by min(1295k − 195, 1512k − 390) at the guild square, rounded to whole stage pixels. Changing segment SHALL animate the shift over 750 ms. Each segment's signs SHALL be visible and clickable only while that segment is shown.

#### Scenario: Shift amounts follow the stage height

- **WHEN** the panorama offsets are computed for a stage height
- **THEN** they match the formula

##### Example: offsets at the stage bounds

| Stage height | Market offset | Guild square offset |
| --- | --- | --- |
| 624 | 452 | 728 |
| 780 | 614 | 1002 |

#### Scenario: Other segments' signs are inactive

- **WHEN** the market segment is shown
- **THEN** the gate's facility signs and the guild square's signs are hidden and do not respond to clicks

---
### Requirement: Change segment by sign or swipe

Clicking a segment sign SHALL move to the segment it names. A pointer press and release on the stage SHALL count as a swipe when the horizontal distance, measured in stage pixels, is at least 40 and its absolute value is greater than the vertical distance. A leftward swipe SHALL move to the next segment and a rightward swipe to the previous segment, one segment per swipe; a swipe past either end SHALL do nothing. A sign click within 300 ms after a swipe SHALL be ignored. Changing segment SHALL close the bonus list.

#### Scenario: Swipe moves one segment

- **WHEN** the gate is shown and the player swipes left
- **THEN** the market is shown

##### Example: swipe outcomes

| Current segment | Horizontal distance | Vertical distance | Result |
| --- | --- | --- | --- |
| 0 | −60 | 10 | 1 |
| 1 | −60 | 10 | 2 |
| 2 | −60 | 10 | 2 |
| 1 | 60 | 10 | 0 |
| 0 | 60 | 10 | 0 |
| 0 | −39 | 0 | 0 |
| 0 | −60 | 70 | 0 |

#### Scenario: Sign click after a swipe is ignored

- **WHEN** a swipe that starts on the "市集" sign ends and the sign receives a click within 300 ms
- **THEN** the segment changes only once, by the swipe

---
### Requirement: Town status board

The town SHALL show, pinned to the top of the stage and not moving with the panorama, the character's name, job name, level, health points and mana points as bars of current over maximum, and the money with thousands separators. These values SHALL be those of the character at the moment the town was entered and SHALL NOT change while the town is shown. The gate's character sign SHALL show `Lv` followed by the level; the bag and companion signs SHALL show no numbers.

#### Scenario: Status shows the entered character

- **WHEN** the town is entered with a character at level 6 with 317 of 600 health points, 88 of 106 mana points and 12345 money
- **THEN** the status board shows level 6, a health bar at 53%, a mana bar at 83%, the money as `12,345`, and the character sign shows `Lv 6`

---
### Requirement: Town makes no adventure requests

While the town is shown the client SHALL NOT send adventure advance requests. Opening any facility from the town SHALL open the old tabbed screen, which polls the adventure as before; returning to the town SHALL stop polling and SHALL discard battle playback that has not been shown, so the town shows the latest character returned by the server.

#### Scenario: Idling in the town

- **WHEN** the town stays shown for longer than one polling interval
- **THEN** no request to `POST /api/adventure/advance` is sent

#### Scenario: Returning to the town stops polling

- **WHEN** the player opens the adventure tab from the town, then presses "回城鎮"
- **THEN** no further adventure requests are sent and the town shows the character from the last adventure response

#### Scenario: A response arriving after returning to the town

- **WHEN** an adventure advance or a facility action was sent before the player pressed "回城鎮" and its response arrives while the town is shown
- **THEN** the town shows the character from that response, unless the player has opened the old tabbed screen again since; a response from an earlier opening never replaces the character from a later one

---
### Requirement: Facilities open the old tabbed screen

Each facility in the town except the inn and the companions SHALL open the old tabbed screen at the following tab and bag sub-page, and the old tabbed screen SHALL show a "回城鎮" button on every tab that returns to the town at the gate segment. The inn SHALL open the inn screen instead (see the `inn-screen` capability), and the old tabbed screen's bag tab SHALL no longer have an inn sub-page. The companions sign SHALL open the companions screen (see the `companions-screen` capability), and the old tabbed screen SHALL no longer have a companions tab.

| Facility | Tab | Bag sub-page |
| --- | --- | --- |
| 角色 | character | — |
| 背包 | bag | bag |
| 出發 | adventure | — |
| 商店街 | bag | shop |
| 鐵舖 | bag | forge |
| 勳章所 | bag | honour |
| 任務板 | missions | — |

#### Scenario: Inn opens the inn screen

- **WHEN** the player clicks "旅館" in the market
- **THEN** the inn screen opens and the old tabbed screen is not shown

#### Scenario: Bag tab has no inn sub-page

- **WHEN** the player opens "背包" from the gate
- **THEN** the bag tab's sub-page buttons do not include "旅館"

#### Scenario: Companions opens the companions screen

- **WHEN** the player clicks "同伴" at the gate
- **THEN** the companions entrance opens and the old tabbed screen is not shown

#### Scenario: Old tabbed screen has no companions tab

- **WHEN** the player opens "角色" from the gate
- **THEN** the old tabbed screen's tabs do not include "同伴"

#### Scenario: Return to the town

- **WHEN** the player opens "商店街" from the market, then presses "回城鎮"
- **THEN** the town is shown at the gate segment with the character handed back by the old tabbed screen


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
### Requirement: Bonus row and list

The town SHALL show the active lodging and elixirs as a row of badges, the lodging first and then the elixirs in the order of buff kinds (Experience, JobExperience, Attack, Magic, Defense, MagicDefense). The row SHALL be hidden when nothing is active or when loading the bonuses failed. Each badge SHALL show the remaining time as `HH:MM` (minutes rounded down, hours zero-padded) when at least one hour remains and as `MM:SS` (seconds rounded down) otherwise, and SHALL be marked as ending soon when less than 10 minutes remain. Clicking a badge SHALL open a list showing, per bonus, its name, its effect, a remaining bar of (end − now) / (end − start) and the remaining time; clicking anywhere outside the list SHALL close it. An elixir's row SHALL be named by the item last drunk, exactly as the content names it, with the effect line showing the kind name and multiplier (for example `物攻 ×1.05`); when that item is unknown the row SHALL be named by the kind name and multiplier and SHALL have no effect line. The lodging's row SHALL be named by the room name with the effect line showing the room's kind name and multiplier.

#### Scenario: Remaining time text

- **WHEN** remaining times are formatted
- **THEN** they follow the format and the ending-soon mark

##### Example: remaining time

| Remaining | Text | Ending soon |
| --- | --- | --- |
| 6130 s | `01:42` | no |
| 3600 s | `01:00` | no |
| 3599 s | `59:59` | no |
| 600 s | `10:00` | no |
| 599 s | `09:59` | yes |
| 30 s | `00:30` | yes |

#### Scenario: Elixir row names the item

- **WHEN** an active Attack elixir ×1.05 records item `神力秘藥LV1`
- **THEN** its list row is named `神力秘藥LV1` with the effect line `物攻 ×1.05`

#### Scenario: Unknown elixir item

- **WHEN** an active Defense elixir ×1.05 has no item name
- **THEN** its list row is named `物防 ×1.05` and has no effect line

---
### Requirement: Bonus countdown and refresh

The town SHALL recompute the remaining times from the local clock every second without contacting the server. When any shown bonus's end time has passed, the town SHALL request the elixirs and the lodging again and show only those still active.

#### Scenario: A bonus runs out in the town

- **WHEN** an elixir's end time passes while the town is shown
- **THEN** the town requests `GET /api/characters/me/buffs` and `GET /api/inn` once and the expired elixir is no longer shown

---
### Requirement: Town background follows the time of day

The panorama SHALL use the dawn image from 05:00 to before 08:00, day from 08:00 to before 17:00, dusk from 17:00 to before 19:00 and night otherwise, by the device's local time, re-evaluated every minute.

#### Scenario: Time of day boundaries

- **WHEN** the time of day is determined
- **THEN** it follows the hour boundaries

##### Example: hours

| Local time | Time of day |
| --- | --- |
| 04:59 | night |
| 05:00 | dawn |
| 08:00 | day |
| 16:59 | day |
| 17:00 | dusk |
| 19:00 | night |

---
### Requirement: Town portrait

The gate SHALL show the portrait for the character's job and sex from the content image `portraits/<job id>-male` (sex 1) or `portraits/<job id>-female` (sex 2), and SHALL show the portraits placeholder when that image is missing or fails to load.

#### Scenario: Portrait key

- **WHEN** a female character of job `job-7` is in the town
- **THEN** the portrait is looked up as `portraits/job-7-female`

---
### Requirement: Lodging badge uses the room badge image

The lodging's badge in the bonus row and list SHALL be the content image `room-badges/<room id>` of the active stay's room, obtained through the content image lookup; when the image is missing or fails to load, the room-badges placeholder SHALL be shown.

#### Scenario: Lodging badge for a booked room

- **WHEN** the active stay is in room `fighters-bunk`
- **THEN** the lodging badge shows the image looked up for (`room-badges`, `fighters-bunk`)

#### Scenario: Lodging badge without an image

- **WHEN** the manifest has no `room-badges/quiet-chapel` and the active stay is in `quiet-chapel`
- **THEN** the lodging badge shows the room-badges placeholder