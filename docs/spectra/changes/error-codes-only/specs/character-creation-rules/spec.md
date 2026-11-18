## MODIFIED Requirements

### Requirement: One named character per account

`POST /api/characters` SHALL create the signed-in account's only character. The name SHALL be trimmed of surrounding whitespace and SHALL then be 2 to 12 characters, counting each user-perceived character (including a combined emoji) as one; a name that fits that count but exceeds 12 code points SHALL also be rejected. A name with the wrong length SHALL respond 400 with the reason `CHARACTER_NAME_LENGTH`, and one over 12 code points with `CHARACTER_NAME_TOO_COMPLEX`. Character names SHALL be unique ignoring letter case, and an account SHALL have at most one character; either conflict SHALL respond 409, and the response's `reasons` SHALL name which one happened: `ACCOUNT_HAS_CHARACTER` when the account already has a character, or `CHARACTER_NAME_TAKEN` when another character has the name. `GET /api/characters/me` SHALL respond 404 with the reason `CHARACTER_NOT_FOUND` when the account has no character.

#### Scenario: Name is trimmed

- **WHEN** a character is created with the name `  艾倫  `
- **THEN** the character's name is `艾倫`

#### Scenario: Second character for the same account

- **WHEN** an account that already has a character creates another
- **THEN** the API responds 409 with the reason `ACCOUNT_HAS_CHARACTER` and the first character is unchanged

#### Scenario: Name used by another account

- **WHEN** a new account creates a character with the name of another account's character in a different letter case
- **THEN** the API responds 409 with the reason `CHARACTER_NAME_TAKEN` and no character is created

##### Example: name length

| Name | Response |
| --- | --- |
| `艾` | 400 `CHARACTER_NAME_LENGTH` |
| `艾倫` | 201 |
| 12 Chinese characters | 201 |
| 13 Chinese characters | 400 `CHARACTER_NAME_LENGTH` |

### Requirement: Creation choices and allocation

A new character SHALL choose a sex (1 男, 2 女), an element (1 地 to 6 毒), a birthplace (1 中央大陸 to 4 末日王城) and a first-tier job; any other value SHALL respond 400 with the reason `SEX_INVALID`, `ELEMENT_INVALID`, `BIRTH_INVALID`, `JOB_UNKNOWN` (no such job) or `JOB_NOT_FIRST_TIER` (a job whose tier is not 1). The player SHALL distribute exactly 10 points over strength, intelligence, life and agility with at least 1 point each. Any stat below 1 SHALL respond 400 with the reason `STAT_BELOW_ONE`; a total other than 10 SHALL respond 400 with the reason `POINTS_NOT_SPENT`.

#### Scenario: Unspent points are rejected

- **WHEN** a character is created with strength 1, intelligence 1, life 1 and agility 3
- **THEN** the API responds 400 with the reason `POINTS_NOT_SPENT` and no character is created

##### Example: allocation totals

| Strength / Intelligence / Life / Agility | Response |
| --- | --- |
| 1 / 1 / 1 / 3 (6) | 400 `POINTS_NOT_SPENT` |
| 4 / 2 / 3 / 1 (10) | 201 |
| 4 / 2 / 3 / 2 (11) | 400 `POINTS_NOT_SPENT` |
| 0 / 4 / 3 / 3 (10) | 400 `STAT_BELOW_ONE` |

#### Scenario: Advanced job at creation

- **WHEN** a character is created with a job whose tier is not 1
- **THEN** the API responds 400 with the reason `JOB_NOT_FIRST_TIER`
