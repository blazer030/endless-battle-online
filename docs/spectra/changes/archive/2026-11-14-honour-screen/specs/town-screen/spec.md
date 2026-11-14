## MODIFIED Requirements

### Requirement: Facilities open the old tabbed screen

Each facility in the town except the inn, the companions and the honour hall SHALL open the old tabbed screen at the following tab and bag sub-page, and the old tabbed screen SHALL show a "回城鎮" button on every tab that returns to the town at the gate segment. The inn SHALL open the inn screen instead (see the `inn-screen` capability), and the old tabbed screen's bag tab SHALL no longer have an inn sub-page. The companions sign SHALL open the companions screen (see the `companions-screen` capability), and the old tabbed screen SHALL no longer have a companions tab. The honour hall sign SHALL open the honour screen (see the `honour-screen` capability), and the old tabbed screen's bag tab SHALL no longer have an honour sub-page.

| Facility | Tab | Bag sub-page |
| --- | --- | --- |
| 角色 | character | — |
| 背包 | bag | bag |
| 出發 | adventure | — |
| 商店街 | bag | shop |
| 鐵舖 | bag | forge |
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

#### Scenario: Honour hall opens the honour screen

- **WHEN** the player clicks "勳章所" at the guild square
- **THEN** the honour counter opens and the old tabbed screen is not shown

#### Scenario: Bag tab has no honour sub-page

- **WHEN** the player opens "背包" from the gate
- **THEN** the bag tab's sub-page buttons do not include "勳章所"

#### Scenario: Return to the town

- **WHEN** the player opens "商店街" from the market, then presses "回城鎮"
- **THEN** the town is shown at the gate segment with the character handed back by the old tabbed screen
