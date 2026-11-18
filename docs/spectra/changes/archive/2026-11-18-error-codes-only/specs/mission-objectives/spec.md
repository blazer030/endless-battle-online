## MODIFIED Requirements

### Requirement: A regular mission can ask for a pet

A regular mission SHALL be able to name a pet in `deliverPet`. Completing it SHALL require a carried pet of that species, and that pet SHALL be removed on completion, the same as a daily pet mission. Without such a carried pet, completion SHALL fail with `PET_MISSING` and nothing SHALL change.

A regular mission SHALL have at least one of `kills`, `deliver` or `deliverPet`. A pet species SHALL NOT be an NPC, and a mission monster SHALL only be named by its own mission.

#### Scenario: Handing in the carried pet

- **GIVEN** `mission-16` asks for `monster-1`, and the player carries a pet of that species
- **WHEN** the player completes the mission
- **THEN** the carried pet is removed and the reward is given

#### Scenario: A pet in the stable does not count

- **GIVEN** `mission-16` asks for `monster-1`, and the player's only pet of that species is not carried
- **WHEN** the player completes the mission
- **THEN** the request fails with `PET_MISSING`, and the pet and the mission record are unchanged

#### Scenario: Another mission's monster is rejected

- **WHEN** content names a pet whose `missionId` points to a different mission
- **THEN** loading the content throws a `GameContentException`

---

### Requirement: A mission can require a skill level

A mission SHALL be able to name a skill and level in `requiredSkill`. This SHALL NOT block accepting the mission. Completing it SHALL require the learned level of that skill to be at least the required level; otherwise completion SHALL fail with `SKILL_TOO_LOW` and nothing SHALL change. The mission board SHALL report whether the condition is met, and a mission that does not meet it SHALL NOT be ready.

A mission progress event SHALL report `SkillTooLow` when the learned level is below the required level at the time of the fight. With `SkillTooLow` true, the battle log SHALL say the skill level is still needed and the stage SHALL NOT show the mission-achieved plate. The mission page's progress SHALL count a pet and a skill condition as one step each, so it reaches full only when the mission is ready.

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
| not learned | fails with `SKILL_TOO_LOW` |
| 1 | fails with `SKILL_TOO_LOW` |
| 2 | completes |
| 3 | completes |

#### Scenario: Filling the kills with the skill too low

- **GIVEN** a mission that requires `skill-414` Lv2, and the player has learned Lv1
- **WHEN** a win fills the last kill target
- **THEN** the progress event has `Completed` true and `SkillTooLow` true, the stage shows no mission-achieved plate, and the battle log says the skill level is still needed

#### Scenario: Filling the kills with the skill met

- **GIVEN** a mission that requires `skill-414` Lv2, and the player has learned Lv2
- **WHEN** a win fills the last kill target
- **THEN** the progress event has `Completed` true and `SkillTooLow` false, and the stage shows the mission-achieved plate

#### Scenario: Progress counts every condition

- **GIVEN** a mission on the mission page
- **WHEN** its progress is shown

##### Example: Progress with three kills required

| Kills | Pet | Skill | Progress |
| ----- | --- | ----- | -------- |
| 3/3 | none | not met | 3/4 |
| 3/3 | not carried | none | 3/4 |
| 3/3 | carried | met | full |

---
