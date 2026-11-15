## Purpose

Defines the mission board and its API: which missions a character can see and accept, how accepting, abandoning and completing work, what a completed mission gives, and how daily missions roll and reset. The objectives a mission can ask for (several kill targets, a pet, a skill level) are defined by the `mission-objectives` capability.

## ADDED Requirements

### Requirement: Mission board lists available, active and completed missions

`GET /api/missions` SHALL return the character's mission board with `available`, `active`, `completed`, `activeLimit` (3) and `nextDailyReset`. A mission SHALL be available when it has no record and every accept condition other than `tooMany` holds, so a full hand still lists what could be accepted. Available missions SHALL be ordered daily first, then regular missions by level ascending, then content order. Active missions SHALL be ordered by acceptance time, oldest first, and completed missions by completion time, newest first. Records of a past daily period, records of missions missing from the content, and records whose kind (daily or regular) no longer matches the content SHALL NOT appear. Item counts used for delivery progress SHALL count the bag only, not equipped or stored items.

#### Scenario: Board order

- **WHEN** the character can accept a daily mission and two regular missions of levels 30 and 20
- **THEN** `available` lists the daily mission, then the level-20 mission, then the level-30 mission

#### Scenario: A full hand still lists available missions

- **GIVEN** the character has 3 active missions
- **WHEN** the board is read
- **THEN** missions whose only failing condition is `tooMany` are still in `available`

#### Scenario: Yesterday's daily record is hidden

- **GIVEN** a daily mission completed in the previous daily period
- **WHEN** the board is read after the reset
- **THEN** the mission is not in `completed` and is in `available` again

---
### Requirement: Accepting a mission

`POST /api/missions/accept` SHALL add an active record for the mission and return the new board. Accepting SHALL fail with HTTP 400 and the reasons below, and nothing SHALL change. `missionUnknown`, `alreadyActive` and `alreadyCompleted` SHALL be reported alone; the other reasons SHALL all be reported together in this order: `tooMany` (3 missions already active), `levelTooLow` (character level below the mission level), `jobMismatch`, `sexMismatch`, `birthMismatch`, `prerequisiteMissing` (the prerequisite mission is not completed), `missionConflict` (the mission's rival route has a record in any state). A daily mission's record SHALL store the current daily period.

#### Scenario: Accept reasons in order

- **GIVEN** a level-5 warrior with 3 active missions
- **WHEN** the character accepts a level-10 mission limited to sorcerers whose prerequisite is not completed
- **THEN** the request fails with `tooMany`, `levelTooLow`, `jobMismatch`, `prerequisiteMissing` in that order

#### Scenario: Accepting a completed mission

- **WHEN** the character accepts a regular mission it has completed
- **THEN** the request fails with only `alreadyCompleted`

---
### Requirement: Abandoning a mission

`POST /api/missions/abandon` SHALL delete the active record and return the new board; the mission SHALL be acceptable again afterwards, and kill progress SHALL start over. Abandoning a mission that is not active SHALL fail with `missionNotActive`.

#### Scenario: Abandon and accept again

- **GIVEN** an active mission with kill progress 2
- **WHEN** the player abandons it and accepts it again
- **THEN** the mission is active with progress 0

#### Scenario: Abandoning a completed mission

- **WHEN** the player abandons a completed mission
- **THEN** the request fails with `missionNotActive`

---
### Requirement: Completing a mission

`POST /api/missions/complete` SHALL check in this order and fail with the first failing reason: `missionNotActive`, `objectiveIncomplete` (a kill target is not full), `itemsMissing` (the bag lacks a delivery item), `petMissing` (no carried pet of the asked species), `skillTooLow`, then the reward checks `bagFull` (a reward item does not fit the bag after delivery) and `petsFull` (a pet reward with 3 pets after delivery), which SHALL be reported together. On failure nothing SHALL change. On success the delivery items and the delivered pet SHALL be removed, the character SHALL gain experience (mission level × 190, levelling up at once) and money, the reward items SHALL go into the bag, a reward skill SHALL be learned at its level unless the character already knows that skill at any level, a reward pet SHALL be added to the stable, and the record SHALL become completed. The response SHALL return the new board, the updated character, the mission's completion message, the experience, money, items, skill, whether the skill was learned (`skillGranted`), the number of levels gained and the pet.

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
- **THEN** the request fails with `bagFull`, and the bag, the character and the mission record are unchanged

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
- **THEN** the request fails with `alreadyCompleted`; after the reset the mission is available again

---
### Requirement: Board details for the mission board screen

Each mission in the board SHALL also carry:

- `completionMessage`: for a completed mission, the mission's completion message from the content, or null when the content has none (an empty string); null for available and active missions.
- `category` on each reward item and each delivery item: the item's category from the content; reward items carry it in the board and in the completion response.
- `learnedLevel` on `requiredSkill`: the character's learned level of that skill, or null when not learned.
- `jobName`, `sexName` and `birthName` on `requirements`: the display names of the required job (from the job content), sex (男／女) and birthplace (from the character options), each null when that condition is not set.

The board SHALL also carry `firstDailyLevel`: the lowest level among the daily missions in the content, or null when the content has no daily mission. These fields SHALL be added without changing existing fields.

#### Scenario: Completion message only for completed missions

| Mission | State | completionMessage |
| --- | --- | --- |
| `mission-1` | completed | the content's message (starts with 感謝您的努力) |
| `mission-3` | completed | null (the content's message is empty) |
| `mission-1` | available | null |

#### Scenario: Item categories

- **WHEN** the board lists `mission-1`
- **THEN** its reward item `item-342` (白色布料) has `category` `Material`, and its delivery item `item-331` (蜘蛛絲) has `category` `Material`

#### Scenario: Learned level of a required skill

| Learned `skill-414` | `requiredSkill` of `mission-318` |
| --- | --- |
| not learned | level 2, learnedLevel null, met false |
| Lv1 | level 2, learnedLevel 1, met false |
| Lv2 | level 2, learnedLevel 2, met true |

#### Scenario: Requirement names

- **WHEN** the board lists a mission limited to `job-6` and to women (sex 2)
- **THEN** its requirements have `jobName` 戰士, `sexName` 女 and `birthName` null

#### Scenario: First daily level

- **WHEN** the board is read with the real content (daily missions at levels 50, 50, 150, 150, 350, 500, 500, 700)
- **THEN** `firstDailyLevel` is 50
