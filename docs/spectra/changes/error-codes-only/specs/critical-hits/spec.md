## MODIFIED Requirements

### Requirement: Nobody has a special attack name

Characters and monsters SHALL NOT have a special attack name. Creating a character and updating settings SHALL NOT take one, the character response SHALL NOT contain one, and battle events SHALL NOT carry one. The `characters` table SHALL NOT store one. The character creation screen, the settings panel and the character sheet SHALL NOT show one.

#### Scenario: A character is created without a name for its special attack

- **WHEN** a creation request lists the name, sex, element, birth, job and stat allocation and nothing else
- **THEN** the character is created, and the response has no `specialAttackName`

#### Scenario: Settings are saved without a name for the special attack

- **WHEN** a settings request lists only the attack choice and the five percent thresholds
- **THEN** the settings are saved, and an invalid threshold is still refused with reason `INVALID_PERCENT`

#### Scenario: Battle events carry no name

- **WHEN** a player with any character data lands a critical hit
- **THEN** the `attack` event has `critical` true and no `specialAttackName`

#### Scenario: The column is gone

- **WHEN** all migrations have been applied
- **THEN** the `characters` table has no `special_attack_name` column
