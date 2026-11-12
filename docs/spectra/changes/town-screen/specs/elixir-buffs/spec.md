## Purpose

Elixirs are consumable items that multiply one battle quantity (experience, job proficiency, attack, magic attack, defense or magic defense) for a limited time. This capability defines how drinking an elixir starts a bonus, how bonuses of the same kind replace each other and combine with lodging, and what the client receives about the active bonuses.

## ADDED Requirements

### Requirement: Drinking an elixir starts a bonus

Using an elixir item SHALL consume one of it and SHALL start a bonus of the elixir's kind and multiplier that applies to battles starting at or after the moment of use and before the moment of use plus the elixir's seconds. Before the bonus is saved, the elapsed adventure time SHALL be settled under the bonuses that were active until then. The bonus SHALL record the item that was drunk.

#### Scenario: Elixir applies only to later battles

- **WHEN** a player drinks an experience elixir with multiplier 1.5 and 7200 seconds while battles before that moment are still unsettled
- **THEN** those earlier battles are settled without the new multiplier, and battles starting in the next 7200 seconds gain 1.5 times experience

#### Scenario: Use records the item

- **WHEN** a player drinks item `item-2257` (神力秘藥LV1, Attack ×1.05, 7200 seconds) at 2026-11-12T08:00:00Z
- **THEN** the saved Attack bonus has multiplier 1.05, starts at 2026-11-12T08:00:00Z, ends at 2026-11-12T10:00:00Z and records item `item-2257`

### Requirement: One bonus per kind

A character SHALL have at most one elixir bonus per kind. Drinking an elixir of a kind that already has a bonus, active or expired, SHALL replace that bonus's multiplier, start, end and recorded item; remaining time SHALL NOT carry over. Elixirs of different kinds SHALL be active at the same time.

#### Scenario: Same kind replaces

- **WHEN** a character with an Attack bonus ending in 30 minutes drinks another Attack elixir of 7200 seconds
- **THEN** the character has one Attack bonus that ends 7200 seconds after the second drink and records the second item

#### Scenario: Different kinds coexist

- **WHEN** a character with an active Attack bonus drinks an Experience elixir
- **THEN** both the Attack and the Experience bonuses are active

### Requirement: Elixir and lodging of the same kind multiply

For a battle starting at time t, the multiplier of a kind SHALL be the active elixir multiplier of that kind (1 when none) multiplied by the active lodging multiplier when the lodging's room grants the same kind (1 otherwise). Attack, Magic, Defense and MagicDefense SHALL multiply attack, magic attack, defense and magic defense respectively, rounded like PHP `round`; Experience and JobExperience SHALL multiply gained experience and job proficiency.

#### Scenario: Elixir and lodging stack

- **WHEN** a battle starts while an Experience elixir ×1.05 and an Experience lodging ×1.10 are both active
- **THEN** the battle's experience multiplier is 1.155

### Requirement: Active bonus list

`GET /api/characters/me/buffs` SHALL return the character's elixir bonuses whose end time is after the current time, ordered by kind (Experience, JobExperience, Attack, Magic, Defense, MagicDefense). Each entry SHALL contain `kind`, `multiplier`, `startsAt`, `endsAt`, `itemId` and `itemName`. `itemId` and `itemName` SHALL be null when no item is recorded or the recorded item no longer exists in the content. The `buff` field of the `POST /api/inventory/use` response SHALL have the same shape. Without a character the endpoint SHALL return 404.

#### Scenario: Expired bonuses are not listed

- **WHEN** a character has an Attack bonus that ended a minute ago and an Experience bonus that ends in an hour
- **THEN** the list contains only the Experience bonus

#### Scenario: Entry carries the item

- **WHEN** a character drank the Experience elixir `test-exp-elixir` (測試經驗密藥 in the test content) and requests the list
- **THEN** the Experience entry has `itemId` `test-exp-elixir`, `itemName` `測試經驗密藥` and its `startsAt` and `endsAt`

#### Scenario: Bonus without a recorded item

- **WHEN** a stored Experience bonus has no recorded item
- **THEN** its entry has `itemId` null and `itemName` null

#### Scenario: Recorded item removed from the content

- **WHEN** a stored Experience bonus records `removed-item`, which the game content no longer has
- **THEN** its entry has `itemId` null and `itemName` null

### Requirement: Lodging response carries its start

The `lodging` object of `GET /api/inn` and of the `POST /api/inn/book` response SHALL contain `roomId`, `startsAt` and `endsAt`.

#### Scenario: Lodging start and end

- **WHEN** a character books a room at 2026-11-12T08:00:00Z and requests `GET /api/inn`
- **THEN** `lodging.startsAt` is 2026-11-12T08:00:00Z and `lodging.endsAt` is 2026-11-12T16:00:00Z
