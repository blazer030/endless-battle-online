## MODIFIED Requirements

### Requirement: Booking a room buys a timed bonus

Booking a room SHALL deduct its price from the character's money and SHALL start a stay from now until now + the inn's `seconds`. Booking SHALL NOT change the character's health or mana points and SHALL NOT settle adventure time. The price SHALL be computed from the stored character's level. When the money is less than the price the booking SHALL be rejected with reason `NOT_ENOUGH_MONEY`; an unknown room SHALL be rejected with reason `UNKNOWN_ROOM`; a rejected booking SHALL write nothing.

#### Scenario: Booking deducts money and starts the stay

- **WHEN** a level 6 character with 1,000 money and 20 of 53 HP books `apprentice-loft` at 08:00 with `seconds` 28800
- **THEN** the character has 400 money and 20 HP, and a stay in `apprentice-loft` runs from 08:00 to 16:00

#### Scenario: Booking does not settle adventure time

- **WHEN** a character last settled at 07:00 books a room at 08:00
- **THEN** the character's last settled time stays 07:00 and no fight is simulated

#### Scenario: Not enough money

- **WHEN** a level 6 character with 599 money books `apprentice-loft`
- **THEN** the request fails with HTTP 400 and reasons `["NOT_ENOUGH_MONEY"]`, and the money and stays are unchanged

#### Scenario: Unknown room

- **WHEN** a character books room `penthouse`, which the inn does not define
- **THEN** the request fails with HTTP 400 and reasons `["UNKNOWN_ROOM"]`, and nothing is written

---
