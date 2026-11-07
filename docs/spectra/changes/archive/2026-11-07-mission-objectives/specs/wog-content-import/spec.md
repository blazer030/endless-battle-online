## ADDED Requirements

### Requirement: Missions, monsters and recipes use the extended rules

The import SHALL write several kill targets, pet objectives, skill conditions, NPCs, monsters without sub-places and recipe prerequisites from the original data. Every regular mission SHALL be imported except `mission-130`, whose target is a global mission monster. Every monster SHALL be imported except 261 (the monster of mission 130), 287 (a place that is not on the map) and 324 (an NPC that is not a kill target). The original values of content already imported SHALL NOT change, apart from conditions and rewards restored by these rules.

#### Scenario: Several kill targets are imported

- **GIVEN** the original mission 29 with the targets 111 ×3 and 112 ×3
- **WHEN** the import runs
- **THEN** `mission-29` has both in `kills`, in the original order

#### Scenario: A pet objective is imported

- **GIVEN** the original mission 16 asking for the pet 1
- **WHEN** the import runs
- **THEN** `mission-16` has `deliverPet` set to `monster-1`

#### Scenario: A skill condition is imported

- **GIVEN** the original mission 318 requiring the skill row 415, which is level 2 of 斯巴達
- **WHEN** the import runs
- **THEN** `mission-318` has `requiredSkill` with `skill-414` and level 2

#### Scenario: A skill condition that cannot be used is listed

- **GIVEN** the original mission 325 requiring 戰意驅使, which was not imported
- **WHEN** the import runs
- **THEN** `mission-325` has no `requiredSkill`, and `gaps.md` lists that field under mission 325

#### Scenario: An NPC is imported with a cleaned message

- **GIVEN** the original monster 265 倫魯 with level 0, and a message containing the literal text `rn` where line breaks were
- **WHEN** the import runs
- **THEN** `monster-265` has `npc` true and `missionId` `mission-133`, and `npcMessage` has a single space in place of each run of `rn`

#### Scenario: Monsters without sub-places are imported

- **GIVEN** the original monster 399 in place 6 with no sub-places
- **WHEN** the import runs
- **THEN** `monster-399` is in `monsters.json` with empty `subPlaces`

#### Scenario: A mission monster that is not a target is imported

- **GIVEN** the original monster 434 belonging to mission 267, which targets monster 438
- **WHEN** the import runs
- **THEN** `monster-434` is in `monsters.json` with `missionId` `mission-267`

#### Scenario: A recipe prerequisite is imported

- **GIVEN** the original recipe 174 requiring mission 317
- **WHEN** the import runs
- **THEN** `recipe-174` has `requiredMissionId` set to `mission-317`, and `gaps.md` has no row for recipe 174

#### Scenario: A pet reward above the old limit is imported

- **GIVEN** the original mission 312 with the pet reward 417 at level 530
- **WHEN** the import runs and the content loads
- **THEN** `mission-312` has that pet reward, and loading succeeds

## MODIFIED Requirements

### Requirement: Data the rules cannot use is listed

Original data that the current rules cannot use SHALL be left out of the content files and listed in `docs/research/wog45/gaps.md` with its original id, name and reason. This SHALL cover:

- unsupported fields;
- records the current content validation would reject;
- skills the effect catalogue cannot express;
- references removed because their target was left out.

A monster's special attack name SHALL be left out of the content files and SHALL NOT be listed per monster, because it was decided not to be used rather than waiting for a rule; `gaps.md` SHALL state this decision once in the monster mapping notes.

The official content SHALL load without errors after every import.

#### Scenario: An NPC that is not a kill target is listed, not imported

- **GIVEN** the original monster 324 錫得里克, an NPC that no mission targets
- **WHEN** the import runs
- **THEN** that monster is absent from `monsters.json`, and `gaps.md` lists its id and name with the reason that an NPC must be a kill target of its mission

#### Scenario: Unsupported monster fields are listed

- **GIVEN** the original monster 426 魔王史萊姆 with a drop of the item 2277 that was not imported
- **WHEN** the import runs
- **THEN** `monster-426` is imported without that drop, and `gaps.md` lists the drop under monster 426

#### Scenario: A weapon with a skill that was not imported is listed

- **GIVEN** the original item 1834 翼龍獸矛 with the pet skill 戰意驅使 at rate 10
- **WHEN** the import runs
- **THEN** `item-1834` has no weapon skill, and `gaps.md` lists that field under item 1834

#### Scenario: A monster with only a special attack is not listed

- **GIVEN** the original monster 13 白蛇龍 with the special attack 白龍巨浪 and no skills
- **WHEN** the import runs
- **THEN** `gaps.md` has no row for monster 13 in its list of fields that were not moved

#### Scenario: A reference to an excluded record is removed and listed

- **WHEN** a mission rewards an item that was excluded from the import
- **THEN** that reward is absent from the mission, and `gaps.md` records the removal and names the excluded item

#### Scenario: The prerequisite of mission 131 is removed and listed

- **GIVEN** the original mission 131 requiring mission 130, which is not imported
- **WHEN** the import runs
- **THEN** `mission-131` has no `prerequisiteId`, and `gaps.md` lists that field under mission 131

#### Scenario: A skill reward above level 1 is listed

- **GIVEN** the original mission 240 rewarding level 6 of 無我
- **WHEN** the import runs
- **THEN** `mission-240` has no skill reward, and `gaps.md` lists that field under mission 240

#### Scenario: Official content loads

- **WHEN** `JsonGameContent.Load` reads `backend/content` after the import
- **THEN** loading succeeds without a `GameContentException`
