## Purpose

Defines how monsters without sub-places appear, and how NPC encounters work. An NPC is met like a monster but is never fought, and meeting it advances the mission it belongs to.

## ADDED Requirements

### Requirement: Monsters without sub-places appear everywhere in their place

In a place with sub-places, a monster with no `subPlaces` SHALL be eligible in every sub-place of that place. A monster with `subPlaces` SHALL be eligible only in those sub-places. In a place without sub-places, a monster SHALL NOT list `subPlaces`.

#### Scenario: Eligibility by sub-place

- **GIVEN** place 7 has sub-places, and the encounter roll allows every monster
- **WHEN** the candidates are built for sub-place 2

##### Example: Candidates in sub-place 2

| Monster `subPlaces` | Eligible |
| ------------------- | -------- |
| empty | yes |
| [2] | yes |
| [1, 3] | no |

#### Scenario: A place without sub-places rejects them

- **WHEN** content gives `subPlaces` to a monster in a place without sub-places
- **THEN** loading the content throws a `GameContentException`

### Requirement: An NPC encounter is not a fight

Meeting an NPC during an adventure SHALL NOT start a battle. It SHALL NOT give experience, money, drops or a capture, SHALL NOT change HP or SP, and SHALL NOT count as a fight, win or loss. It SHALL add 1 to the matching target of open missions when that target is not full, and SHALL take one fight interval. It SHALL NOT use additional random rolls. The event stream SHALL contain an NPC event with the NPC's id, name and message, followed by the mission progress event when progress was added.

#### Scenario: Meeting an NPC advances its mission

- **GIVEN** `mission-133` targets the NPC `monster-265` with count 1, and the player is in that NPC's sub-place
- **WHEN** the encounter picks `monster-265`
- **THEN** the events contain an NPC event and a mission progress event with `Completed` true, with no battle or reward events, and HP, SP, experience and money are unchanged

#### Scenario: An NPC whose target is full adds nothing

- **GIVEN** `mission-307` with a full target for the NPC `monster-486` and another target not full
- **WHEN** the encounter picks `monster-486`
- **THEN** the events contain an NPC event and no mission progress event, and the summary's `MissionKills` does not increase

### Requirement: A mercenary can meet an NPC

A mercenary meeting an NPC SHALL add 1 to the matching target of open missions when that target is not full. It SHALL NOT use up the mercenary's remaining count, SHALL NOT add a fight to the report, and SHALL wait for the next fight interval.

#### Scenario: A mercenary meets an NPC

- **GIVEN** a deployed mercenary with 5 fights remaining and an open mission targeting an NPC in its place
- **WHEN** the mercenary's fight turn picks that NPC
- **THEN** the mission's progress increases by 1, the remaining count is still 5, and the report has no fight for that turn

### Requirement: NPC content is validated

A monster with `npc` true SHALL have a `missionId`, SHALL be a kill target of that mission, and SHALL have no drops and no skills. It SHALL be exempt from the minimum level, HP and agility. Every other monster SHALL still meet those minimums. `npcMessage` SHALL be optional.

#### Scenario: Invalid NPCs are rejected

- **WHEN** content has an NPC without `missionId`, an NPC that is not a kill target of its mission, or an NPC with drops
- **THEN** loading the content throws a `GameContentException`

#### Scenario: Zero stats are only allowed for NPCs

- **WHEN** content has a monster with level 0 and hp 0 without `npc` true
- **THEN** loading the content throws a `GameContentException`

### Requirement: The battle screen shows NPC encounters

The stage SHALL show the NPC's name as a plate without an HP bar, and the battle log SHALL show the NPC's name, with its message when it has one. The mission-achieved plate SHALL appear only when a progress event has `Completed` true and no delivery is required.

#### Scenario: An NPC with a message

- **WHEN** the battle log receives an NPC event named 倫魯 with a message
- **THEN** the log shows 倫魯 and the message

#### Scenario: An NPC without a message

- **WHEN** the battle log receives an NPC event named 探險者 with no message
- **THEN** the log shows 探險者 and no empty quotation

#### Scenario: A partly finished mission shows no achievement

- **WHEN** a progress event fills one target with `Completed` false
- **THEN** the stage shows no mission-achieved plate
