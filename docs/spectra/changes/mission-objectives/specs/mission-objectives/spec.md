## Purpose

Defines what a regular mission can ask for beyond a single kill target: several kill targets, a pet to hand in, and a required skill level. It also defines when a mission's own monsters can be met, so these objectives can be completed.

## ADDED Requirements

### Requirement: A mission can have several kill targets

A regular mission SHALL list its kill targets in `kills`, and each target SHALL count separately. A win SHALL add 1 to the matching target of every open mission, and a target that is already full SHALL NOT count further. A mission's kill objective SHALL be met only when every target is full. A daily mission SHALL resolve to a single kill target as before.

Content validation SHALL reject a mission whose targets repeat a monster, whose target is outside the mission's place, or whose target is the mission monster of another mission.

#### Scenario: Targets count separately

- **WHEN** the player wins against one target of a mission with two targets
- **THEN** only that target's progress increases

##### Example: Mission 29

- **GIVEN** `mission-29` needs 3 `monster-111` and 3 `monster-112`, with progress 3 and 1
- **WHEN** the player beats `monster-111` and then `monster-112`
- **THEN** the progress is 3 and 2, and the mission cannot be completed yet

#### Scenario: The objective is met when every target is full

- **GIVEN** `mission-29` with progress 3 and 2
- **WHEN** the player beats `monster-112`
- **THEN** the progress is 3 and 3, and the mission progress event has `Completed` true

#### Scenario: Progress saved before this change is kept

- **GIVEN** a mission record with the single progress value 2 from before the migration
- **WHEN** the migration runs and the mission is read
- **THEN** the first target's progress is 2, and any target beyond the saved values has progress 0

#### Scenario: Invalid targets are rejected

- **WHEN** content lists the same monster twice in `kills`, or a target outside the mission's place
- **THEN** loading the content throws a `GameContentException`

### Requirement: A regular mission can ask for a pet

A regular mission SHALL be able to name a pet in `deliverPet`. Completing it SHALL require a carried pet of that species, and that pet SHALL be removed on completion, the same as a daily pet mission. Without such a carried pet, completion SHALL fail with `petMissing` and nothing SHALL change.

A regular mission SHALL have at least one of `kills`, `deliver` or `deliverPet`. A pet species SHALL NOT be an NPC, and a mission monster SHALL only be named by its own mission.

#### Scenario: Handing in the carried pet

- **GIVEN** `mission-16` asks for `monster-1`, and the player carries a pet of that species
- **WHEN** the player completes the mission
- **THEN** the carried pet is removed and the reward is given

#### Scenario: A pet in the stable does not count

- **GIVEN** `mission-16` asks for `monster-1`, and the player's only pet of that species is not carried
- **WHEN** the player completes the mission
- **THEN** the request fails with `petMissing`, and the pet and the mission record are unchanged

#### Scenario: Another mission's monster is rejected

- **WHEN** content names a pet whose `missionId` points to a different mission
- **THEN** loading the content throws a `GameContentException`

### Requirement: A mission can require a skill level

A mission SHALL be able to name a skill and level in `requiredSkill`. This SHALL NOT block accepting the mission. Completing it SHALL require the learned level of that skill to be at least the required level; otherwise completion SHALL fail with `skillTooLow` and nothing SHALL change. The mission board SHALL report whether the condition is met, and a mission that does not meet it SHALL NOT be ready.

#### Scenario: Accepting without the skill

- **GIVEN** `mission-318` requires `skill-414` Lv2, and the player meets its other conditions but has not learned the skill
- **WHEN** the player accepts the mission
- **THEN** the mission becomes active

#### Scenario: Completion checks the learned level

- **GIVEN** a mission that requires `skill-414` Lv2 and whose other objectives are met
- **WHEN** the player completes it

##### Example: Learned levels

| Learned level | Result |
| ------------- | ------ |
| not learned | fails with `skillTooLow` |
| 1 | fails with `skillTooLow` |
| 2 | completes |
| 3 | completes |

### Requirement: A mission's monsters appear while it is open

A mission monster SHALL appear only while its mission is accepted, not expired and open. A mission SHALL be open while it has a kill target that is not full, or while it has no kill targets and asks for a pet. All of the mission's monsters SHALL appear while it is open, including a monster that is not a kill target and a monster whose own target is already full. A mission monster SHALL NOT be required to be a kill target of its mission, but its `missionId` SHALL point to a regular mission.

#### Scenario: A monster for a pet mission can be met

- **GIVEN** `mission-268` asks for the pet `monster-435`, which is its mission monster
- **WHEN** the player adventures in that monster's place with the mission active
- **THEN** `monster-435` can be met, and it stops appearing after the mission is completed or abandoned

#### Scenario: A full target still appears while another is not full

- **GIVEN** `mission-307` has one full target and three targets that are not full
- **WHEN** the encounter pool is built
- **THEN** all four of its monsters can be met, and meeting the full one adds no progress

#### Scenario: A monster that is not a target appears without counting

- **GIVEN** `mission-267` targets `monster-438`, and `monster-434` is its mission monster but not a target
- **WHEN** the player beats `monster-434`
- **THEN** the mission's progress does not change

#### Scenario: A monster stops appearing when the mission is no longer open

- **GIVEN** a mission whose kill targets are all full and which also asks for items
- **WHEN** the encounter pool is built
- **THEN** its mission monsters do not appear
