## Purpose

The game API contract defines how the game client and the API exchange player data: field names are complete words rather than abbreviations, stored content and player data keep their existing formats behind the server, and the client converts every response into its own value objects before screens or game logic use it. This keeps a change to the API's shape confined to the conversion layer on each side.

## ADDED Requirements

### Requirement: Complete field names in the game API

Responses and requests of the character, adventure, skill, job and world endpoints, and every stat or stat-bonus object returned by any game endpoint, SHALL use complete words for field names. The following names SHALL replace the abbreviated names, and the abbreviated names SHALL NOT appear in those responses:

| Abbreviated | Complete |
| --- | --- |
| `str` | `strength` |
| `smart` | `intelligence` |
| `vit` | `vitality` |
| `agi` | `agility` |
| `au` | `charisma` |
| `be` | `faith` |
| `at` | `attack` |
| `mat` | `magicAttack` |
| `df` | `defense` |
| `mdf` | `magicDefense` |
| `hp` | `healthPoints` |
| `hpMax` | `maxHealthPoints` |
| `sp` | `manaPoints` |
| `spMax` | `maxManaPoints` |
| `exp` | `experience` |
| `jobExp` | `jobExperience` |
| `nextExp` | `nextLevelExperience` |

The same substitution SHALL apply where an abbreviation is part of a longer name (for example `playerHp` becomes `playerHealthPoints`, `potionHpPercent` becomes `potionHealthPointsPercent`, `spCost` becomes `manaPointsCost`). Level ranges SHALL use `minimum` and `maximum` instead of `min` and `max`. `life` SHALL stay `life`.

#### Scenario: Character stats use complete names

- **WHEN** a signed-in client requests `GET /api/characters/me`
- **THEN** the character object has `healthPoints`, `manaPoints`, `experience` and `nextLevelExperience`, its `stats` object has `strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma`, `faith`, `attack`, `magicAttack`, `defense`, `magicDefense`, `maxHealthPoints` and `maxManaPoints`, and none of the abbreviated names appear

##### Example: new character

- **GIVEN** a character created with strength 4, intelligence 2, life 3 and agility 1
- **WHEN** the client requests `GET /api/characters/me`
- **THEN** `baseStats` is `{"strength":14,"intelligence":12,"life":13,"vitality":10,"agility":11,"charisma":10,"faith":10}`, `healthPoints` is 50 and `manaPoints` is 25

#### Scenario: Character creation takes complete names

- **WHEN** a client creates a character with `allocation` `{"strength":1,"intelligence":1,"life":1,"agility":7}`
- **THEN** the API responds 201 and the created character's base strength is 11 and base agility is 17

#### Scenario: Battle events use complete names

- **WHEN** a signed-in client advances an adventure that produced at least one fight
- **THEN** each encounter event has `monsterHealthPoints`, `monsterMaxHealthPoints`, `playerHealthPoints`, `playerMaxHealthPoints`, `playerManaPoints` and `playerMaxManaPoints`, each reward event has `experience` and `jobExperience`, and the summary has `experience` and `jobExperience`

#### Scenario: Item bonuses use complete names

- **WHEN** a signed-in client requests `GET /api/inventory` holding a weapon whose content bonus is 8 attack
- **THEN** that item's `bonus` object has `attack` 8 and has no `at` field

#### Scenario: World level ranges use complete names

- **WHEN** a signed-in client requests `GET /api/world`
- **THEN** each place's `recommended` object has `minimum` and `maximum`

### Requirement: Stat reason codes name the stat in full

A reason code that reports a stat below a requirement SHALL be `statTooLow:<stat>`, where `<stat>` is one of `strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma` and `faith`.

#### Scenario: Job change blocked by a stat

- **WHEN** a signed-in client requests `GET /api/jobs` and an advanced job requires 50 strength while the character's base strength is 20
- **THEN** that job's `reasons` contains `statTooLow:strength`

#### Scenario: Equipping blocked by a stat

- **WHEN** a signed-in client equips an item that requires more charisma than the character's base charisma
- **THEN** the API responds 400 and the `reasons` list contains `statTooLow:charisma`

### Requirement: Stored formats stay readable

Renaming fields SHALL NOT change the format of stored data. Content files under `backend/content/` SHALL keep their existing keys and SHALL load with the same values as before. Enhancement data already stored for a player's items SHALL read back with the same values, and newly written enhancement data SHALL use the same keys as data written before this change.

#### Scenario: Existing content loads unchanged

- **WHEN** the API starts with the current `backend/content/` files
- **THEN** content loading succeeds and each monster's attack, magic attack, defense, magic defense, health points and mana points equal the `at`, `mat`, `df`, `mdf`, `hp` and `sp` values in `monsters.json`

#### Scenario: Previously stored refine bonus reads back

- **WHEN** an inventory row's stored refine bonus is `{"str":2,"smart":0,"life":0,"vit":0,"agi":0,"au":0,"be":0,"at":3,"mat":0,"df":0,"mdf":0,"hpMax":0}`
- **THEN** that item's refine bonus reads back as strength 2 and attack 3, and writing it back stores the same keys

### Requirement: Battle events carry monster names

Battle events that refer to a monster SHALL carry the monster's display name alongside its id: the encounter event SHALL have `monsterName`, the capture event SHALL have `capturedMonsterName` (null when nothing was captured), and the mission progress event SHALL have `monsterName`. The client SHALL show these names in the battle log without another request.

#### Scenario: Encounter names the monster

- **WHEN** a signed-in client advances an adventure and the character met the monster `monster-1` named 史萊姆
- **THEN** the encounter event has `monsterId` `monster-1` and `monsterName` 史萊姆, and the battle log shows 遭遇 史萊姆！

### Requirement: Content catalogue endpoint removed

`GET /api/content` SHALL NOT exist. Screens SHALL take monster names from battle events, and sex, element, birthplace and job names from `/api/public/settings` and `/api/public/jobs`.

#### Scenario: Catalogue request

- **WHEN** a client requests `GET /api/content`
- **THEN** the API responds 404

#### Scenario: Character page names come from public data

- **WHEN** the character page is shown for a character with sex 2, element 3 and birth 4
- **THEN** it shows the sex, element and birthplace names that `/api/public/settings` lists for ids 2, 3 and 4

#### Scenario: Mission requirement names the job

- **WHEN** the mission board shows a mission limited to the job `job-6`
- **THEN** its requirement line shows the name that `/api/public/jobs` lists for `job-6`

### Requirement: Client converts responses before use

The game client SHALL convert each response of the character, adventure, skill, job and world endpoints into its own value objects before any screen or game-state code reads it. A response missing a required field, or holding a value of the wrong type, SHALL reject the request with a parse error that names the field's path; the screen SHALL treat it like any other failed request. Fields the client does not know SHALL be ignored.

#### Scenario: Missing field is reported with its path

- **WHEN** `GET /api/characters/me` responds 200 with a body whose `stats` object lacks `strength`
- **THEN** the client rejects the request with a parse error whose path is `character.stats.strength`

#### Scenario: Unknown field is ignored

- **WHEN** `GET /api/world` responds 200 with an extra field on a place that the client does not know
- **THEN** the client returns the world map and the extra field does not appear on the converted place
