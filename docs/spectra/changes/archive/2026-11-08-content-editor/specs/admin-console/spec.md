## MODIFIED Requirements

### Requirement: Content pages

The admin console SHALL provide a list page for each of the six editable content kinds. Editing and creating a record SHALL happen in a dialog opened from the list page without navigating away. Creating SHALL start either from a blank form or from a copy of an existing record with its id cleared. Saving SHALL write to the draft through the admin API; on success the dialog closes and the list and draft status refresh, and on failure the dialog stays open and shows the API's error. Every field of a kind's record SHALL be editable through a labelled form control, and the console SHALL NOT offer raw JSON editing.

#### Scenario: Edit a record

- **WHEN** the developer opens a monster's edit dialog from the monsters list, changes its level, and saves
- **THEN** the admin API receives the update for that monster, the dialog closes, and the list shows the new level without a page change

#### Scenario: Create from a copy

- **WHEN** the developer starts a new item from a copy of `item-2` 木刀, enters the id `item-9001`, and saves
- **THEN** the admin API receives a new item `item-9001` with `item-2`'s other field values

#### Scenario: Save rejected

- **WHEN** the developer saves a new record whose id already exists and the API responds 409
- **THEN** the dialog stays open and shows that the id already exists

## ADDED Requirements

### Requirement: API responses become view objects

The admin console SHALL convert every content response from the admin API into view objects before any page uses it, and SHALL convert view objects back into the API shape when saving. A response missing a required field SHALL be reported as an error naming the field's path, and the page SHALL NOT show partial data. Fields the console does not recognize SHALL be ignored.

#### Scenario: Missing required field

- **WHEN** the monsters list response has a monster without `level`
- **THEN** the monsters page shows an error naming `monsters[index].level` and no monster rows

#### Scenario: Unknown field

- **WHEN** a monster in the response has an extra field the console does not know
- **THEN** the monsters page shows the monster normally

### Requirement: Related records are chosen, not typed

Fields that refer to other content SHALL be chosen from a list showing the related record's name, and SHALL save the related record's id. The choices SHALL come from the admin API so records added in the same draft are available. A monster's sub-places SHALL be chosen from the sub-place names of the monster's selected place.

#### Scenario: Choose a monster's place and sub-places

- **WHEN** the developer chooses place 中央平原 for a monster and ticks 綠風草地
- **THEN** the saved monster has the id of 中央平原 as `placeId` and that sub-place's index in `subPlaceIndexes`

##### Example: sub-places follow the place

- **GIVEN** place 1 中央平原 has sub-places 香茅綠地, 綠風草地, 白沙海灣, 眠獸洞穴 and a monster in place 1 has sub-places 綠風草地 and 白沙海灣 ticked
- **WHEN** the developer changes the monster's place to another place
- **THEN** the sub-place choices show the new place's sub-place names and none are ticked

#### Scenario: Choose drops and an exclusive mission

- **WHEN** the developer adds a drop by choosing the item 錢包 with drop rarity 90 and chooses the mission 打敗米列 as the exclusive mission
- **THEN** the saved monster has a drop with `item-204` and rarity 90, and `mission-2` as `exclusiveMissionId`

#### Scenario: Daily missions are not offered

- **WHEN** the developer opens a monster's exclusive mission choices
- **THEN** the choices list regular missions and no daily mission

#### Scenario: Choose the skills a monster can use

- **WHEN** the developer adds a skill to a monster by choosing 爆氣 with level 2 and activation rate 10
- **THEN** the saved monster has a skill with `skill-1`, level 2, and activation rate 10

#### Scenario: The server rejects a skill that no longer exists

- **WHEN** the developer saves a monster and the API responds 400 naming `skills[0].skillId`
- **THEN** the dialog stays open and shows that error

#### Scenario: Long choice lists can be searched

- **WHEN** the developer types 錢包 into a monster's drop item choices
- **THEN** the choices narrow to items whose name or id contains 錢包

#### Scenario: Choose a parent job

- **WHEN** the developer edits job 狂戰士
- **THEN** the parent job choices list every other job by name and not 狂戰士 itself

### Requirement: Structured fields are edited item by item

Grouped values SHALL be edited as individual labelled inputs rather than as text: job growth as a minimum and maximum for each of the seven stats, an item's stat bonuses and stat requirements as one input per stat, and a job's stat requirements as a list of rows that each choose a stat and its minimum. A skill's form SHALL set the maximum level first and then edit each level's learning cost, required proficiency, cooldown, conditions, costs, and effects; each condition, cost, and effect SHALL choose a kind and show only that kind's fields. An item's form SHALL show only the fields that belong to its category and consumable kind, and saving SHALL send only those fields. A monster marked as an NPC SHALL show only the fields an NPC uses.

#### Scenario: Edit job growth

- **WHEN** the developer sets warrior's 力量 growth to minimum 2 and maximum 4 and saves
- **THEN** the saved job's growth has strength `{ "minimum": 2, "maximum": 4 }` and the other stats unchanged

#### Scenario: Raising a skill's maximum level

- **WHEN** the developer raises a skill's maximum level from 10 to 11
- **THEN** the form has a new level 11 starting with level 10's values, and the saved skill has 11 levels

#### Scenario: Effect fields follow the kind

- **WHEN** the developer changes an effect's kind from `StatChange` to `Silence`
- **THEN** the form shows chance and rounds instead of target and stat
- **AND** the saved effect has only `kind`, `chance`, and `rounds`

#### Scenario: A skill with no conditions

- **WHEN** the developer removes every condition from a skill level and saves
- **THEN** the saved level has an empty `conditions` list

#### Scenario: Marking a monster as an NPC

- **WHEN** the developer marks a monster as an NPC and saves
- **THEN** the form shows name, place, sub-places, exclusive mission, encounter rate, and message
- **AND** the saved monster has zero combat stats and no drops or skills

#### Scenario: Item fields follow the category

- **WHEN** the developer changes a new item's category from weapon to key, chooses a place, and saves
- **THEN** the form shows the key's place and duration instead of bonus, requirements, and holes
- **AND** the saved item has a key section and no bonus or requirements

#### Scenario: Only a weapon has a weapon skill

- **WHEN** the developer edits a body armor
- **THEN** the form has no element or weapon skill fields

### Requirement: Lists can be searched and filtered

Each content list SHALL be paged and have a search box matching record id or name, case-insensitively and by partial text, and filters for that kind: items by category and shop level, monsters by place, jobs by tier, skills by job and slot type, places by area and restriction. Search and filters SHALL combine. Fields that refer to other content SHALL show the related record's name in the list.

#### Scenario: Search and filter together

- **WHEN** the developer filters monsters by place 中央平原 and searches for 史萊姆
- **THEN** the list shows only monsters in 中央平原 whose id or name contains 史萊姆

#### Scenario: Related names in the list

- **WHEN** the developer opens the monsters list
- **THEN** each monster's place column shows the place name rather than its id
