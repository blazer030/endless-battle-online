## MODIFIED Requirements

### Requirement: Complete field names in the game API

Responses and requests of every game endpoint under `/api/` SHALL use complete words for field names and enumeration values. The following names SHALL replace the abbreviated names, and the abbreviated names SHALL NOT appear in those responses:

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
| `supplyHp` | `supplyHealthPoints` |
| `supplySp` | `supplyManaPoints` |
| `expPercent` | `experiencePercent` |

The same substitution SHALL apply where an abbreviation is part of a longer name (for example `playerHp` becomes `playerHealthPoints`, `potionHpPercent` becomes `potionHealthPointsPercent`, `spCost` becomes `manaPointsCost`). Level ranges SHALL use `minimum` and `maximum` instead of `min` and `max`. `life` SHALL stay `life`. Buff kinds SHALL be `Experience`, `JobExperience`, `Attack`, `Magic`, `Defense` and `MagicDefense`; `Exp` and `JobExp` SHALL NOT appear. Field names that denote a time (`atSeconds`, `at`, `endsAt`, `expiresAt`) are not abbreviations and SHALL stay as they are.

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

#### Scenario: Supply items use complete names

- **WHEN** a signed-in client requests `GET /api/inventory` holding a supply item that restores 50 health points and 25 mana points
- **THEN** that item has `supplyHealthPoints` 50 and `supplyManaPoints` 25, and has no `supplyHp` or `supplySp` field

#### Scenario: Mission rewards use complete names

- **WHEN** a signed-in client requests `GET /api/missions` and when it completes a mission
- **THEN** each mission's `rewards` object and the completion response have `experience` and no `exp` field

#### Scenario: Mercenary catalogue uses complete names

- **WHEN** a signed-in client requests `GET /api/mercenaries`
- **THEN** each catalogue entry has `experiencePercent` equal to the mercenary's experience share in `mercenaries.json`, and has no `expPercent` field

#### Scenario: Buff kinds use complete names

- **WHEN** a signed-in client requests `GET /api/inn`
- **THEN** the room that grants experience has `kind` `Experience` and the room that grants job experience has `kind` `JobExperience`

### Requirement: Stored formats stay readable

Renaming fields SHALL NOT change the format of stored data. Content files under `backend/content/` SHALL keep their existing keys and SHALL load with the same values as before. Enhancement data and buff kinds already stored for a player SHALL read back with the same values, and newly written enhancement data and buff kinds SHALL use the same keys and codes as data written before this change.

#### Scenario: Existing content loads unchanged

- **WHEN** the API starts with the current `backend/content/` files
- **THEN** content loading succeeds and each monster's attack, magic attack, defense, magic defense, health points and mana points equal the `at`, `mat`, `df`, `mdf`, `hp` and `sp` values in `monsters.json`

#### Scenario: Previously stored refine bonus reads back

- **WHEN** an inventory row's stored refine bonus is `{"str":2,"smart":0,"life":0,"vit":0,"agi":0,"au":0,"be":0,"at":3,"mat":0,"df":0,"mdf":0,"hpMax":0}`
- **THEN** that item's refine bonus reads back as strength 2 and attack 3, and writing it back stores the same keys

#### Scenario: Previously stored buff kind reads back

- **WHEN** a character has a stored buff whose kind is `Exp`
- **THEN** `GET /api/characters/me/buffs` returns that buff with `kind` `Experience`, and drinking another experience elixir stores the kind as `Exp`

### Requirement: Client converts responses before use

The game client SHALL convert each response of every game endpoint under `/api/` into its own value objects before any screen or game-state code reads it. Screens and game-state code SHALL NOT use the types generated from the OpenAPI document. Time values SHALL be converted to dates. A response missing a required field, or holding a value of the wrong type, SHALL reject the request with a parse error that names the field's path; the screen SHALL treat it like any other failed request. Fields the client does not know SHALL be ignored.

#### Scenario: Missing field is reported with its path

- **WHEN** `GET /api/characters/me` responds 200 with a body whose `stats` object lacks `strength`
- **THEN** the client rejects the request with a parse error whose path is `character.stats.strength`

#### Scenario: Unknown field is ignored

- **WHEN** `GET /api/world` responds 200 with an extra field on a place that the client does not know
- **THEN** the client returns the world map and the extra field does not appear on the converted place

#### Scenario: Missing field in an embedded inventory names its path

- **WHEN** `POST /api/forge/refine` responds 200 with a body whose `inventory.bag[0]` lacks `capacity`
- **THEN** the client rejects the request with a parse error whose path is `forge.inventory.bag[0].capacity`

#### Scenario: Buff end time becomes a date

- **WHEN** `GET /api/characters/me/buffs` responds with a buff whose `endsAt` is `2026-11-12T08:30:00Z`
- **THEN** the converted buff's end time is the date 2026-11-12 08:30 UTC
