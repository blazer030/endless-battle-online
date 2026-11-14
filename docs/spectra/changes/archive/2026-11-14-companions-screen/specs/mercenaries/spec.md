## Purpose

The mercenary hall lets the character hire one mercenary at a time who fights on its own at an adventure place and shares part of the rewards. These requirements describe the hall that the companions screen shows and acts on; the mercenary's fights are simulated during adventure settlement.

## ADDED Requirements

### Requirement: Mercenary catalogue

`GET /api/mercenaries` SHALL return the catalogue from `backend/content/mercenaries.json` with id, name, price (null for mercenaries obtained only at the honour hall), number of fights, money, drop and experience percents and death cost, and the current contract or null. A contract SHALL carry the mercenary id, name, remaining fights, place id and name, and whether it is fighting.

#### Scenario: Catalogue entries

- **WHEN** the catalogue is requested
- **THEN** the five grades are listed with their terms

##### Example: grades

| Grade | Price | Fights | Money | Drop | Experience | Death cost |
| --- | --- | --- | --- | --- | --- | --- |
| F | 500 | 850 | 30% | 30% | 65% | 5 |
| E | 100,000 | 900 | 40% | 40% | 75% | 4 |
| D | 1,000,000 | 950 | 50% | 50% | 85% | 3 |
| C | null | 900 | 70% | 70% | 100% | 2 |
| B | null | 900 | 80% | 80% | 100% | 1 |

### Requirement: Hiring a mercenary

Hiring SHALL report, in this order, `honourOnly` when the price is null, `mercenaryHired` when a contract exists, and `notEnoughMoney` when the price is null or above the money; an unknown mercenary SHALL be rejected with `mercenaryUnknown`. A successful hire SHALL deduct the price and start a resting contract with the catalogue's fights.

#### Scenario: Hiring F grade

- **WHEN** a character with 1,000 money and no contract hires `mercenary-1`
- **THEN** the character has 500 money and a resting contract with 850 fights

#### Scenario: Second hire refused

- **WHEN** a character with a contract hires another mercenary
- **THEN** the request fails with reason `mercenaryHired` and nothing changes

### Requirement: Deploying and resting

Deploying SHALL set the contract's place and make it fight, the first fight 100 seconds later; a restricted place SHALL be rejected with `placeRestricted` and an unknown place with `placeUnknown`. Resting SHALL stop fighting. Every contract command without a contract SHALL be rejected with `noMercenary`.

#### Scenario: Deploying to a restricted place

- **WHEN** the mercenary is deployed to a restricted place
- **THEN** the request fails with reason `placeRestricted`

### Requirement: Renaming and dismissing

Renaming the mercenary SHALL trim surrounding spaces and accept 1 to 8 characters counted as Unicode code points, otherwise reject with `nameInvalid`. Dismissing SHALL remove the contract.

#### Scenario: Name of nine characters

- **WHEN** the mercenary is renamed to a nine-character name
- **THEN** the request fails with reason `nameInvalid`

### Requirement: Contract ends when fights run out

When the remaining fights reach zero during settlement the contract SHALL be removed; a lost fight SHALL use the grade's death cost instead of one fight.

#### Scenario: Last fight

- **WHEN** a contract with one fight left fights and wins
- **THEN** the contract no longer exists
