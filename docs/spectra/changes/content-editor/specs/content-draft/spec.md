## MODIFIED Requirements

### Requirement: Editable content kinds

The admin SHALL support reading, updating, and creating records of exactly six content kinds: items, monsters, jobs, skills, areas, and places. Each kind SHALL have its own typed record whose field names are complete words (for example `attack`, `magicDefense`, `faith`, `dropRarity`) rather than the abbreviations used in the content files, and enumerated values SHALL be strings. A monster's record SHALL include the skills it can use, stored in `monster-skills.json`, and a skill's record SHALL include the jobs that can learn it, stored in `job-skills.json`. Records SHALL be identified by their `id` field. Deleting records SHALL NOT be supported. The admin SHALL also offer a read-only list of missions with each mission's `id`, `title`, and whether it is daily.

#### Scenario: List records of a kind

- **WHEN** the admin requests `GET /admin/content/monsters`
- **THEN** the response lists every monster record with all of its fields under their complete names
- **AND** the records come from the draft when a draft exists, otherwise from the official content

##### Example: monster field names

- **GIVEN** the content files have monster `monster-1` with `"at": 19, "mdf": 5, "meet": 0`, a drop `{"itemId": "item-204", "topr": 90}`, and a `monster-skills.json` row `{"monsterId": "monster-1", "skillId": "skill-1", "level": 2, "rate": 10}`
- **WHEN** the admin requests `GET /admin/content/monsters/monster-1`
- **THEN** the record has `"attack": 19`, `"magicDefense": 5`, `"encounterRate": 0`, a drop `{"itemId": "item-204", "dropRarity": 90}`, and a skill `{"skillId": "skill-1", "level": 2, "activationRate": 10}`

#### Scenario: Unsupported kind

- **WHEN** the admin requests `GET /admin/content/recipes`
- **THEN** the response is 404 Not Found

#### Scenario: Missions are read-only

- **WHEN** the admin requests `GET /admin/content/missions`
- **THEN** the response lists every mission's `id`, `title`, and `daily`
- **AND** a `PUT` or `POST` to a mission path returns 404 Not Found

### Requirement: Edits only change the draft

Updating or creating a record SHALL write only to the draft directory configured by `Admin:DraftDirectory`. The first write SHALL create the draft as a full copy of the official content directory and record a fingerprint of every official file. Official content files and the content loaded by the running game SHALL NOT change until publish. A request body that does not match the kind's record shape, or that refers to a skill or job that does not exist in the draft through a monster's `skills` or a skill's `jobIds`, SHALL be rejected with 400 Bad Request naming the offending field, and SHALL NOT change the draft. Saving a monster SHALL update `monsters.json` and `monster-skills.json` together, and saving a skill SHALL update `skills.json` and `job-skills.json` together.

#### Scenario: First edit creates the draft

- **WHEN** no draft exists and the admin updates item `item-2`
- **THEN** the draft directory contains all twelve content files with the updated `item-2`
- **AND** `backend/content/items.json` is byte-identical to before the request

##### Example: one field changed

- **GIVEN** official `item-2` has `"price": 6`
- **WHEN** the admin updates `item-2` with `"price": 8`
- **THEN** the draft `items.json` has `item-2` with `"price": 8` and every other item unchanged
- **AND** the game content the running API loaded at startup still has `item-2` with price 6

#### Scenario: Create a new record

- **WHEN** the admin creates a monster whose `id` does not exist
- **THEN** the draft `monsters.json` contains the new record after the existing ones

#### Scenario: Create with an existing id

- **WHEN** the admin creates an item whose `id` already exists
- **THEN** the response is 409 Conflict and the draft is unchanged

#### Scenario: Body with the wrong shape

- **WHEN** the admin updates a skill with `"slotType": "Sometimes"`
- **THEN** the response is 400 Bad Request naming `slotType` and the draft is unchanged

#### Scenario: A monster refers to a skill that does not exist

- **WHEN** the admin updates a monster with a skill whose `skillId` is not in the draft's skills
- **THEN** the response is 400 Bad Request naming `skills[0].skillId` and the draft is unchanged

#### Scenario: A monster's skills are saved with the monster

- **WHEN** the admin updates `monster-1` with the skills `skill-1` at level 3 and activation rate 20
- **THEN** the draft `monster-skills.json` has only that row for `monster-1`, every other row is unchanged, and the draft `monsters.json` has the updated `monster-1`

### Requirement: Every draft write is checked

After each write the system SHALL load the draft with the same loader the game uses at startup and return the result. A write that makes the draft invalid SHALL still be saved, so related records can be fixed in later writes. The exception is a request the admin rejects before writing, as described in "Edits only change the draft".

#### Scenario: Write makes the draft invalid

- **WHEN** the admin updates a monster so its `placeId` points to a place that does not exist
- **THEN** the write is saved and the response reports the draft as invalid with the loader's error message

## ADDED Requirements

### Requirement: Lossless record conversion

Converting a content file into records and writing those records back SHALL produce a file byte-identical to the original, for every editable content file in `backend/content/` and in the test content. A field present in a content file but not represented in its kind's record SHALL make this conversion fail rather than be dropped.

#### Scenario: Official content round-trips

- **WHEN** each of `items.json`, `monsters.json`, `monster-skills.json`, `jobs.json`, `skills.json`, `job-skills.json`, and `world.json` in `backend/content/` is read into records and written back
- **THEN** every written file is byte-identical to the original

#### Scenario: Unknown field is not silently dropped

- **WHEN** a monster in a content file has a field the monster record does not represent
- **THEN** the round-trip check for that file fails and names the file
