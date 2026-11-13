## MODIFIED Requirements

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
