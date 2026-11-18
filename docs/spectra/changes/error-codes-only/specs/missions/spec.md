## MODIFIED Requirements

### Requirement: Mission board lists available, active and completed missions

`GET /api/missions` SHALL return the character's mission board with `available`, `active`, `completed`, `activeLimit` (3) and `nextDailyReset`. A mission SHALL be available when it has no record and every accept condition other than `TOO_MANY` holds, so a full hand still lists what could be accepted. Available missions SHALL be ordered daily first, then regular missions by level ascending, then content order. Active missions SHALL be ordered by acceptance time, oldest first, and completed missions by completion time, newest first. Records of a past daily period, records of missions missing from the content, and records whose kind (daily or regular) no longer matches the content SHALL NOT appear. Item counts used for delivery progress SHALL count the bag only, not equipped or stored items.

#### Scenario: Board order

- **WHEN** the character can accept a daily mission and two regular missions of levels 30 and 20
- **THEN** `available` lists the daily mission, then the level-20 mission, then the level-30 mission

#### Scenario: A full hand still lists available missions

- **GIVEN** the character has 3 active missions
- **WHEN** the board is read
- **THEN** missions whose only failing condition is `TOO_MANY` are still in `available`

#### Scenario: Yesterday's daily record is hidden

- **GIVEN** a daily mission completed in the previous daily period
- **WHEN** the board is read after the reset
- **THEN** the mission is not in `completed` and is in `available` again

---

### Requirement: Accepting a mission

`POST /api/missions/accept` SHALL add an active record for the mission and return the new board. Accepting SHALL fail with HTTP 400 and the reasons below, and nothing SHALL change. `MISSION_UNKNOWN`, `ALREADY_ACTIVE` and `ALREADY_COMPLETED` SHALL be reported alone; the other reasons SHALL all be reported together in this order: `TOO_MANY` (3 missions already active), `LEVEL_TOO_LOW` (character level below the mission level), `JOB_MISMATCH`, `SEX_MISMATCH`, `BIRTH_MISMATCH`, `PREREQUISITE_MISSING` (the prerequisite mission is not completed), `MISSION_CONFLICT` (the mission's rival route has a record in any state). A daily mission's record SHALL store the current daily period.

#### Scenario: Accept reasons in order

- **GIVEN** a level-5 warrior with 3 active missions
- **WHEN** the character accepts a level-10 mission limited to sorcerers whose prerequisite is not completed
- **THEN** the request fails with `TOO_MANY`, `LEVEL_TOO_LOW`, `JOB_MISMATCH`, `PREREQUISITE_MISSING` in that order

#### Scenario: Accepting a completed mission

- **WHEN** the character accepts a regular mission it has completed
- **THEN** the request fails with only `ALREADY_COMPLETED`

---

### Requirement: Abandoning a mission

`POST /api/missions/abandon` SHALL delete the active record and return the new board; the mission SHALL be acceptable again afterwards, and kill progress SHALL start over. Abandoning a mission that is not active SHALL fail with `MISSION_NOT_ACTIVE`.

#### Scenario: Abandon and accept again

- **GIVEN** an active mission with kill progress 2
- **WHEN** the player abandons it and accepts it again
- **THEN** the mission is active with progress 0

#### Scenario: Abandoning a completed mission

- **WHEN** the player abandons a completed mission
- **THEN** the request fails with `MISSION_NOT_ACTIVE`

---

### Requirement: Completing a mission

`POST /api/missions/complete` SHALL check in this order and fail with the first failing reason: `MISSION_NOT_ACTIVE`, `OBJECTIVE_INCOMPLETE` (a kill target is not full), `ITEMS_MISSING` (the bag lacks a delivery item), `PET_MISSING` (no carried pet of the asked species), `SKILL_TOO_LOW`, then the reward checks `BAG_FULL` (a reward item does not fit the bag after delivery) and `PETS_FULL` (a pet reward with 3 pets after delivery), which SHALL be reported together. On failure nothing SHALL change. On success the delivery items and the delivered pet SHALL be removed, the character SHALL gain experience (mission level × 190, levelling up at once) and money, the reward items SHALL go into the bag, a reward skill SHALL be learned at its level unless the character already knows that skill at any level, a reward pet SHALL be added to the stable, and the record SHALL become completed. The response SHALL return the new board, the updated character, the mission's completion message, the experience, money, items, skill, whether the skill was learned (`skillGranted`), the number of levels gained and the pet.

#### Scenario: Completing a delivery mission

- **GIVEN** the character has `mission-1` active and 1 `item-331` in the bag
- **WHEN** the player completes it
- **THEN** the bag loses the `item-331`, gains 1 `item-342`, the character gains 3,800 experience, and `completionMessage` is the content's message for `mission-1`

#### Scenario: A known skill is not learned again

- **GIVEN** the character already knows the reward skill
- **WHEN** the player completes the mission
- **THEN** the mission completes, `skillGranted` is false and the known skill's level is unchanged

#### Scenario: Reward does not fit

- **GIVEN** the reward item's category is full in the bag after delivery
- **WHEN** the player completes the mission
- **THEN** the request fails with `BAG_FULL`, and the bag, the character and the mission record are unchanged

---

### Requirement: Daily missions roll once per day and reset at 09:00 Taipei

A daily period SHALL start at 09:00 Asia/Taipei (UTC+8, no daylight saving) and `nextDailyReset` SHALL be the next such moment in UTC. A daily mission's kill target or pet species and its reward SHALL be rolled from the mission's pools with a seed from the character id, mission id and period, so they are the same all day and differ by character and day. A daily mission completed in a period SHALL be acceptable again only after the next reset, and an active daily record SHALL expire at the reset.

#### Scenario: Period switches at 09:00 Taipei

| Time (UTC) | Period |
| --- | --- |
| 2026-11-15 00:59 | 2026-11-14 |
| 2026-11-15 01:00 | 2026-11-15 |

#### Scenario: Re-accepting a completed daily

- **GIVEN** the character completed a daily mission today
- **WHEN** it accepts the mission again before the reset
- **THEN** the request fails with `ALREADY_COMPLETED`; after the reset the mission is available again

---
