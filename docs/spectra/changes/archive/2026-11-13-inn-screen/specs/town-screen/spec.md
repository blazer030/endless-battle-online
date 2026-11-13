## MODIFIED Requirements

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

### Requirement: Bonus countdown and refresh

The town SHALL recompute the remaining times from the local clock every second without contacting the server. When any shown bonus's end time has passed, the town SHALL request the elixirs and the lodging again and show only those still active.

#### Scenario: A bonus runs out in the town

- **WHEN** an elixir's end time passes while the town is shown
- **THEN** the town requests `GET /api/characters/me/buffs` and `GET /api/inn` once and the expired elixir is no longer shown


### Requirement: Facilities open the old tabbed screen

Each facility in the town except the inn SHALL open the old tabbed screen at the following tab and bag sub-page, and the old tabbed screen SHALL show a "回城鎮" button on every tab that returns to the town at the gate segment. The inn SHALL open the inn screen instead (see the `inn-screen` capability), and the old tabbed screen's bag tab SHALL no longer have an inn sub-page.

| Facility | Tab | Bag sub-page |
| --- | --- | --- |
| 角色 | character | — |
| 背包 | bag | bag |
| 同伴 | companions | — |
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

#### Scenario: Return to the town

- **WHEN** the player opens "商店街" from the market, then presses "回城鎮"
- **THEN** the town is shown at the gate segment with the character handed back by the old tabbed screen

## ADDED Requirements

### Requirement: Lodging badge uses the room badge image

The lodging's badge in the bonus row and list SHALL be the content image `room-badges/<room id>` of the active stay's room, obtained through the content image lookup; when the image is missing or fails to load, the room-badges placeholder SHALL be shown.

#### Scenario: Lodging badge for a booked room

- **WHEN** the active stay is in room `fighters-bunk`
- **THEN** the lodging badge shows the image looked up for (`room-badges`, `fighters-bunk`)

#### Scenario: Lodging badge without an image

- **WHEN** the manifest has no `room-badges/quiet-chapel` and the active stay is in `quiet-chapel`
- **THEN** the lodging badge shows the room-badges placeholder
