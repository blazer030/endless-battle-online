## Purpose

Public game data is game content that any visitor can read without signing in, such as the job list with its skills and the options offered when creating a character. Each kind of data has its own endpoint under one public prefix, so a screen requests only what it shows and the data can later move to a database without one endpoint gathering unrelated tables.

## ADDED Requirements

### Requirement: Public data prefix

Endpoints that serve game data readable without signing in SHALL live under `/api/public/` and SHALL respond without requiring authentication. A request carrying an expired or invalid access token SHALL receive the same public response as a request without a token. New screens SHALL read game data from these endpoints and SHALL NOT read `/api/content`.

#### Scenario: Reading public data without signing in

- **WHEN** a client requests `GET /api/public/jobs` or `GET /api/public/settings` without an `Authorization` header
- **THEN** the API responds 200 with the data

#### Scenario: Expired token does not block public data

- **WHEN** a client requests `GET /api/public/settings` with an expired access token
- **THEN** the API responds 200 with the same body as a request without a token

### Requirement: Public job list with skills

`GET /api/public/jobs` SHALL return every job in the game content in content order. Each job SHALL carry its id, name, tier, parent job id (null for first-tier jobs), unlock level, required proficiency, stat requirements, and growth as a minimum and maximum per stat using full stat names (`strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma`, `faith`). Each job SHALL list the skills its job teaches, in the order of `backend/content/job-skills.json`; each skill SHALL carry its id, name, description, maximum level, prerequisite (skill id, skill name and required level, or null), and one entry per level with the level number and the effect summary text that the skill page shows for that level.

#### Scenario: Job carries its skills in teaching order

- **WHEN** a client requests `GET /api/public/jobs`
- **THEN** each job lists the skills that `backend/content/job-skills.json` assigns to it, in that file's order, each with a summary for every level from 1 to its maximum level

##### Example: warrior skills

- **GIVEN** the official content, where `job-6` (戰士) teaches `skill-1` 爆氣 (max level 10, no prerequisite), `skill-61` 防禦心得 (max level 5, prerequisite 爆氣 level 3), `skill-71` 破滅一擊 and `skill-77` 嗜血
- **WHEN** a client requests `GET /api/public/jobs`
- **THEN** the `job-6` entry lists 爆氣, 防禦心得, 破滅一擊, 嗜血 in that order; 防禦心得 has `maxLevel` 5, prerequisite `{ "skillId": "skill-1", "name": "爆氣", "level": 3 }` and 5 level entries; 爆氣 has prerequisite null and 10 level entries

#### Scenario: Level summary matches the skill page

- **WHEN** the job list includes a skill level
- **THEN** that level's summary text equals the summary the skill page shows for the same skill and level

### Requirement: Public character options

`GET /api/public/settings` SHALL return the options used to create and describe a character: sexes, elements and birthplaces, each as a numeric id with a name, in id order. Each element SHALL also name the element it is strong against and the element it is weak against, taken from the battle rules, so a screen can describe element advantage without repeating the rule.

#### Scenario: Options are listed with numeric ids

- **WHEN** a client requests `GET /api/public/settings`
- **THEN** the response lists sexes 1 男 and 2 女, elements 1 地 to 6 毒, and birthplaces 1 中央大陸 to 4 末日王城

#### Scenario: Element advantage comes from the battle rules

- **WHEN** a client reads an element from `GET /api/public/settings`
- **THEN** its strong-against and weak-against elements follow the battle rule that each element beats the next one in the cycle 地 → 水 → 火 → 木 → 風 → 毒 → 地

##### Example: fire

- **GIVEN** the element 火 with id 3
- **WHEN** a client reads it from `GET /api/public/settings`
- **THEN** it is strong against 木 (id 4) and weak against 水 (id 2)
