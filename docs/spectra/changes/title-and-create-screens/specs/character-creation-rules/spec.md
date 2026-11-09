## Purpose

Character creation rules decide what a new account can choose for its single character and what the character starts with. This spec records the rules that existed before Spectra was adopted, which follow the original WOG 4.5 creation rules, except that all 10 points must now be allocated.

## ADDED Requirements

### Requirement: One named character per account

`POST /api/characters` SHALL create the signed-in account's only character. The name SHALL be trimmed of surrounding whitespace and SHALL then be 2 to 12 characters, counting each user-perceived character (including a combined emoji) as one; a name that fits that count but exceeds 12 code points SHALL also be rejected. Invalid names SHALL respond 400. Character names SHALL be unique ignoring letter case, and an account SHALL have at most one character; either conflict SHALL respond 409 with the detail 此帳號已有角色，或角色名稱已被使用. `GET /api/characters/me` SHALL respond 404 when the account has no character.

#### Scenario: Name is trimmed

- **WHEN** a character is created with the name `  艾倫  `
- **THEN** the character's name is `艾倫`

#### Scenario: Second character for the same account

- **WHEN** an account that already has a character creates another
- **THEN** the API responds 409 and the first character is unchanged

##### Example: name length

| Name | Response |
| --- | --- |
| `艾` | 400 |
| `艾倫` | 201 |
| 12 Chinese characters | 201 |
| 13 Chinese characters | 400 |

### Requirement: Creation choices and allocation

A new character SHALL choose a sex (1 男, 2 女), an element (1 地 to 6 毒), a birthplace (1 中央大陸 to 4 末日王城) and a first-tier job; any other value SHALL respond 400. The player SHALL distribute exactly 10 points over strength, intelligence, life and agility with at least 1 point each. Any stat below 1 SHALL respond 400 with the detail 每項能力至少要分配 1 點; a total other than 10 SHALL respond 400 with the detail 能力點數必須剛好分完 10 點.

#### Scenario: Unspent points are rejected

- **WHEN** a character is created with strength 1, intelligence 1, life 1 and agility 3
- **THEN** the API responds 400 with the detail 能力點數必須剛好分完 10 點 and no character is created

##### Example: allocation totals

| Strength / Intelligence / Life / Agility | Response |
| --- | --- |
| 1 / 1 / 1 / 3 (6) | 400 能力點數必須剛好分完 10 點 |
| 4 / 2 / 3 / 1 (10) | 201 |
| 4 / 2 / 3 / 2 (11) | 400 能力點數必須剛好分完 10 點 |
| 0 / 4 / 3 / 3 (10) | 400 每項能力至少要分配 1 點 |

#### Scenario: Advanced job at creation

- **WHEN** a character is created with a job whose tier is not 1
- **THEN** the API responds 400 with the detail 只能選擇初階職業

### Requirement: Starting state

A new character SHALL start at level 1 with 0 experience, 1000 experience to the next level, 2000 money, 50 HP and 25 SP, at the content's starting place. Its base strength, intelligence, life and agility SHALL each be its allocated points plus 10, and its vitality, charisma and faith SHALL each be 10.

#### Scenario: Base stats from allocation

- **WHEN** a character is created with strength 4, intelligence 2, life 3 and agility 1
- **THEN** its base stats are strength 14, intelligence 12, life 13, agility 11, vitality 10, charisma 10 and faith 10, and it starts at level 1 with 2000 money, 50 HP and 25 SP at the starting place
