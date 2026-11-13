# inn Specification

## Purpose

The inn sells timed room bonuses and defines automatic rest during adventure. Each booking is kept as its own stay so that adventure time settled later still gives every fight the bonus of the stay it started in, without settling at booking time.

## Requirements

### Requirement: Room price

The inn SHALL price a room as the character's level × `pricePerLevel` × the room's `priceRate`, where `pricePerLevel` and each room's `priceRate` come from `backend/content/inn.json`.

#### Scenario: Prices for the character's level

- **WHEN** the inn prices rooms with `pricePerLevel` 50
- **THEN** each price is level × 50 × the room's rate

##### Example: room prices

| Level | Room | Rate | Price |
| --- | --- | --- | --- |
| 6 | apprentice-loft | 2 | 600 |
| 6 | fighters-bunk | 1 | 300 |
| 120 | apprentice-loft | 2 | 12000 |

---
### Requirement: Booking a room buys a timed bonus

Booking a room SHALL deduct its price from the character's money and SHALL start a stay from now until now + the inn's `seconds`. Booking SHALL NOT change the character's health or mana points and SHALL NOT settle adventure time. The price SHALL be computed from the stored character's level. When the money is less than the price the booking SHALL be rejected with reason `notEnoughMoney`; an unknown room SHALL be rejected with reason `unknownRoom`; a rejected booking SHALL write nothing.

#### Scenario: Booking deducts money and starts the stay

- **WHEN** a level 6 character with 1,000 money and 20 of 53 HP books `apprentice-loft` at 08:00 with `seconds` 28800
- **THEN** the character has 400 money and 20 HP, and a stay in `apprentice-loft` runs from 08:00 to 16:00

#### Scenario: Booking does not settle adventure time

- **WHEN** a character last settled at 07:00 books a room at 08:00
- **THEN** the character's last settled time stays 07:00 and no fight is simulated

#### Scenario: Not enough money

- **WHEN** a level 6 character with 599 money books `apprentice-loft`
- **THEN** the request fails with HTTP 400 and reasons `["notEnoughMoney"]`, and the money and stays are unchanged

#### Scenario: Unknown room

- **WHEN** a character books room `penthouse`, which the inn does not define
- **THEN** the request fails with HTTP 400 and reasons `["unknownRoom"]`, and nothing is written

---
### Requirement: Booking ends the active stay

A character SHALL keep one record per booking. When a booking is made while a stay has not yet ended, that stay's end SHALL be set to the booking time and no money SHALL be refunded; the new stay SHALL start at the booking time. At any moment at most one stay SHALL be in effect.

#### Scenario: Switching rooms

- **WHEN** a stay in `fighters-bunk` runs from 06:00 to 14:00 and the character books `apprentice-loft` at 10:00
- **THEN** the records are `fighters-bunk` 06:00–10:00 and `apprentice-loft` 10:00–18:00

#### Scenario: Booking the same room again restarts it

- **WHEN** a stay in `fighters-bunk` runs from 06:00 to 14:00 and the character books `fighters-bunk` at 10:00
- **THEN** the records are `fighters-bunk` 06:00–10:00 and `fighters-bunk` 10:00–18:00

---
### Requirement: Stays apply by fight start time

When adventure time is settled, each fight SHALL use the multiplier of the stay whose start is at or before the fight's start and whose end is after it; a fight inside no stay SHALL get no lodging bonus. A stay whose room no longer exists in the content SHALL give no bonus. The lodging multiplier SHALL multiply with an elixir of the same kind as described in `elixir-buffs`.

#### Scenario: Settling after switching rooms

- **WHEN** the records are `fighters-bunk` (Attack ×1.05) 06:00–10:00 and `apprentice-loft` (Experience ×1.10) 10:00–18:00, and adventure time from 05:00 to 12:00 is settled at 12:00
- **THEN** fights are boosted by the stay containing their start

##### Example: fight start times

| Fight starts | Bonus |
| --- | --- |
| 05:30 | none |
| 09:00 | Attack ×1.05 |
| 10:00 | Experience ×1.10 |
| 11:00 | Experience ×1.10 |

#### Scenario: Room removed from the content

- **WHEN** a record's room id is not in `backend/content/inn.json`
- **THEN** fights inside that record get no lodging bonus

---
### Requirement: Old stays are removed when booking

When a booking is made, records whose end is earlier than the booking time minus 86,400 seconds (the adventure's offline catch-up limit) SHALL be deleted.

#### Scenario: Pruning at booking time

- **WHEN** a character books a room on day 2 at 10:00 and has records ending on day 1 at 09:59 and on day 1 at 10:30
- **THEN** the record ending on day 1 at 09:59 is deleted and the one ending on day 1 at 10:30 is kept

---
### Requirement: Inn listing

`GET /api/inn` SHALL return every room in content order with `id`, `name`, `kind`, `multiplier`, `seconds` and `price` for the character's level; `lodging` with `roomId`, `startsAt` and `endsAt` of the stay in effect now, or null when no stay ends after now or its room is no longer in the content; and `restFee`, the minimum automatic rest cost for the character's level: 0 at level 100 or below, otherwise 10 × level.

#### Scenario: Listing with an active stay

- **WHEN** a level 6 character has a stay in `apprentice-loft` from 08:00 to 16:00 and requests the inn at 09:00
- **THEN** the response lists 6 rooms, `lodging` is `apprentice-loft` 08:00–16:00 and `restFee` is 0

#### Scenario: Ended stay is not listed

- **WHEN** the only record ended at 08:00 and the inn is requested at 09:00
- **THEN** `lodging` is null

##### Example: rest fee

| Level | restFee |
| --- | --- |
| 1 | 0 |
| 100 | 0 |
| 101 | 1010 |
| 120 | 1200 |

---
### Requirement: Automatic rest

Before each fight during adventure settlement the character SHALL rest when dead, when HP × 100 is below the HP threshold × max HP, or, only when an active skill is equipped, when SP × 100 is below the SP threshold × max SP; a threshold of −1 SHALL skip that condition. Resting SHALL restore HP and SP to their maximum including equipment and SHALL wait 5 seconds, or 15 seconds when dead. Resting SHALL be free at level 100 or below. Above level 100 it SHALL cost max(round(10 × level × coefficient), 10 × level), where the coefficient is 2.1 when dead, 1.8 when HP is below a tenth of max HP, and otherwise 1.4 × (max HP − HP) / max HP; when the character cannot pay, no money SHALL be taken and the experience SHALL become round(experience × 0.8) (midpoint away from zero).

#### Scenario: Rest triggers

- **WHEN** the rest conditions are evaluated before a fight
- **THEN** the character rests as listed

##### Example: rest triggers (max HP 100, max SP 100)

| HP | SP | HP threshold | SP threshold | Active skill | Rests |
| --- | --- | --- | --- | --- | --- |
| 0 | 100 | −1 | −1 | no | yes |
| 14 | 100 | 15 | 15 | no | yes |
| 15 | 100 | 15 | 15 | no | no |
| 100 | 10 | 15 | 15 | no | no |
| 100 | 10 | 15 | 15 | yes | yes |
| 10 | 100 | −1 | 15 | yes | no |

#### Scenario: Rest cost

- **WHEN** a character rests
- **THEN** the cost follows the level, HP and money

##### Example: rest cost (max HP 100)

| Level | HP | Money | Cost | Experience after (from 1000) |
| --- | --- | --- | --- | --- |
| 100 | 0 | 0 | 0 | 1000 |
| 101 | 0 | 5000 | 2121 | 1000 |
| 120 | 5 | 5000 | 2160 | 1000 |
| 120 | 50 | 5000 | 1200 | 1000 |
| 120 | 50 | 100 | 0 | 800 |
