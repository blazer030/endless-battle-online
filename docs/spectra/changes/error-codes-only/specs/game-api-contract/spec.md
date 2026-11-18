## ADDED Requirements

### Requirement: Error responses carry only reason codes

Every error response of the game API (`/api/...`) with status 400, 401, 404 or 409 SHALL be a ProblemDetails body with `status` and a non-empty `reasons` list of reason codes, and SHALL NOT contain `detail`; its `title`, when present, SHALL NOT be a sentence meant for players. Every reason code SHALL be upper-case words joined by underscores, optionally followed by `:` and a parameter that keeps its own spelling. A request missing a required field, or whose body cannot be read, SHALL respond 400 with the reason `REQUEST_INVALID`. A request without a valid access token (missing, expired or malformed) to an endpoint that needs one SHALL respond 401 with `AUTHENTICATION_REQUIRED`, distinct from `AUTHENTICATION_FAILED` for a wrong username or password at sign-in. A request from an account without a character, to an endpoint that needs one, SHALL respond 404 with `CHARACTER_NOT_FOUND`. A path or HTTP method the API does not have is outside this rule and responds 404 or 405 without a body. A write that collides with another request changing the same data SHALL respond 409 with `CONCURRENT_UPDATE`. The game front end SHALL choose the text it shows from the reason codes; a screen without its own line for a code SHALL show the shared text for it, and 發生錯誤，請稍後再試 for a code the shared text does not know (screens where a keeper speaks, such as the inn, companions, honour hall and mission board, keep their own failure lines).

#### Scenario: Missing field

- **WHEN** a signed-in client posts `{}` to `POST /api/skills/learn`
- **THEN** the API responds 400 with `reasons` equal to `["REQUEST_INVALID"]` and no `detail`

#### Scenario: Unreadable body

- **WHEN** a signed-in client posts a body that is not JSON to `POST /api/inventory/equip`
- **THEN** the API responds 400 with `reasons` equal to `["REQUEST_INVALID"]`

#### Scenario: No access token

- **WHEN** a client requests `GET /api/inventory` without an access token
- **THEN** the API responds 401 with `reasons` equal to `["AUTHENTICATION_REQUIRED"]`

#### Scenario: No character yet

- **WHEN** a signed-in account without a character requests `GET /api/inventory`
- **THEN** the API responds 404 with `reasons` equal to `["CHARACTER_NOT_FOUND"]` and no `detail`

#### Scenario: Unknown code in the front end

- **WHEN** the bag's equip request responds 400 with `reasons` equal to `["SOMETHING_NEW"]`
- **THEN** the bag shows 發生錯誤，請稍後再試

##### Example: reason code spelling

| Before | After |
| --- | --- |
| `bagFull` | `BAG_FULL` |
| `subPlaceOutOfRange` | `SUB_PLACE_OUT_OF_RANGE` |
| `full` | `FULL` |
| `statTooLow:strength` | `STAT_TOO_LOW:strength` |
| `missingTool:item-1378` | `MISSING_TOOL:item-1378` |

### Requirement: Job list and skill overview report conditions as status

Successful game API responses SHALL NOT carry reason codes. `GET /api/jobs` SHALL report, for each job, `current`, `levelMet` (the character's level is at least `unlockLevel`), `parentProficiencyMet` (the parent job's proficiency is at least `proficiencyRequired`, and `true` for a job without a parent), `met` on each entry of `statRequirements` (the base stat is at least `minimum`), and `eligible`, which SHALL be `true` only when the job is not current and every condition is met; it SHALL NOT have `reasons`. `GET /api/skills` SHALL report, for each skill, `taughtByCurrentJob`, `met` on `prerequisite` when the skill has one, and on `next` (absent at the maximum level) `enoughMoney`, `enoughProficiency` and `canLearn`, which SHALL be `true` only when the skill is taught by the current job, its prerequisite is met, and money and skill points are enough; `next` SHALL NOT have `reasons`. Changing job or learning a skill that does not meet these conditions SHALL still fail with the reason codes in the error response.

#### Scenario: A job reports which conditions are met

- **GIVEN** a level 20 character whose base strength is 20, and an advanced job with `unlockLevel` 30, a parent job whose proficiency the character has reached, and a strength requirement of 50
- **WHEN** the client requests `GET /api/jobs`
- **THEN** that job has `levelMet` false, `parentProficiencyMet` true, its strength requirement has `met` false, `eligible` false, and no `reasons`

#### Scenario: The current job is not eligible

- **WHEN** the client requests `GET /api/jobs`
- **THEN** the character's current job has `current` true and `eligible` false

#### Scenario: A skill's next level reports money and skill points

- **GIVEN** a skill taught by the current job without a prerequisite, whose next level costs more skill points than the character has and less money than the character has
- **WHEN** the client requests `GET /api/skills`
- **THEN** that skill has `taughtByCurrentJob` true, and `next` has `enoughMoney` true, `enoughProficiency` false, `canLearn` false and no `reasons`

#### Scenario: A skill reports its prerequisite

- **GIVEN** a skill whose prerequisite is 爆氣 level 3, and a character with 爆氣 level 2
- **WHEN** the client requests `GET /api/skills`
- **THEN** that skill's `prerequisite` has `met` false and `next.canLearn` is false

## MODIFIED Requirements

### Requirement: Stat reason codes name the stat in full

A reason code that reports a stat below a requirement SHALL be `STAT_TOO_LOW:<stat>`, where `<stat>` is one of `strength`, `intelligence`, `life`, `vitality`, `agility`, `charisma` and `faith`.

#### Scenario: Job change blocked by a stat

- **WHEN** a signed-in client posts `POST /api/jobs/change` for an advanced job that requires 50 strength while the character's base strength is 20
- **THEN** the API responds 400 and the `reasons` list contains `STAT_TOO_LOW:strength`

#### Scenario: Equipping blocked by a stat

- **WHEN** a signed-in client equips an item that requires more charisma than the character's base charisma
- **THEN** the API responds 400 and the `reasons` list contains `STAT_TOO_LOW:charisma`
