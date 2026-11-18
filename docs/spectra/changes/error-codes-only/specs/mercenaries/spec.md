## MODIFIED Requirements

### Requirement: Hiring a mercenary

Hiring SHALL report, in this order, `HONOUR_ONLY` when the price is null, `MERCENARY_HIRED` when a contract exists, and `NOT_ENOUGH_MONEY` when the price is null or above the money; an unknown mercenary SHALL be rejected with `MERCENARY_UNKNOWN`. A successful hire SHALL deduct the price and start a resting contract with the catalogue's fights.

#### Scenario: Hiring F grade

- **WHEN** a character with 1,000 money and no contract hires `mercenary-1`
- **THEN** the character has 500 money and a resting contract with 850 fights

#### Scenario: Second hire refused

- **WHEN** a character with a contract hires another mercenary
- **THEN** the request fails with reason `MERCENARY_HIRED` and nothing changes

### Requirement: Deploying and resting

Deploying SHALL set the contract's place and make it fight, the first fight 100 seconds later; a restricted place SHALL be rejected with `PLACE_RESTRICTED` and an unknown place with `PLACE_UNKNOWN`. Resting SHALL stop fighting. Every contract command without a contract SHALL be rejected with `NO_MERCENARY`.

#### Scenario: Deploying to a restricted place

- **WHEN** the mercenary is deployed to a restricted place
- **THEN** the request fails with reason `PLACE_RESTRICTED`

### Requirement: Renaming and dismissing

Renaming the mercenary SHALL trim surrounding spaces and accept 1 to 8 characters counted as Unicode code points, otherwise reject with `NAME_INVALID`. Dismissing SHALL remove the contract.

#### Scenario: Name of nine characters

- **WHEN** the mercenary is renamed to a nine-character name
- **THEN** the request fails with reason `NAME_INVALID`
