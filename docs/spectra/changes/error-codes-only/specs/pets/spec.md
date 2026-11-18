## MODIFIED Requirements

### Requirement: Feeding a pet

Feeding SHALL be rejected, checking in this order and reporting only the first reason: `TOO_SOON` before the pet's next feed time (10 seconds after it was last fed), `FULL` when satiety is 100 or more, `OVER_OWNER` when the raised stat would exceed the owner's effective stat including equipment (attack against physical attack, magic against magic attack, defense against the higher of physical and magic defense; spirit has no cap), and `NOT_ENOUGH_MONEY` when the money is below the fruit's cost. An unknown fruit SHALL be rejected with `FOOD_UNKNOWN`. Otherwise feeding SHALL deduct the cost, raise the stat by the fruit's amount, add a random satiety in the fruit's range (small 10–20, medium 18–28; satiety above 100 is kept), add a random intimacy of 2–6 (no upper limit), restart the 10-second wait, and return the updated pets and character.

#### Scenario: Feeding raises the pet

- **WHEN** a pet with attack 30 and satiety 40 is fed a small vigor fruit by a character with 100 money
- **THEN** the character has 80 money, the pet's attack is 32, its satiety rises by 10–20 and its intimacy by 2–6

#### Scenario: Feeding is refused

- **WHEN** feeding is attempted in each refused state
- **THEN** the first applicable reason is reported and nothing changes

##### Example: refusals

| State | Reason |
| --- | --- |
| fed 5 seconds ago | TOO_SOON |
| satiety 100 | full |
| pet attack 30, owner attack 31, medium vigor fruit | OVER_OWNER |
| 10 money, small fruit | NOT_ENOUGH_MONEY |

### Requirement: Carrying and storing pets

Carrying a pet SHALL make it the only carried pet and put every other pet in the stable; storing a pet SHALL put it in the stable. An unknown pet SHALL be rejected with `PET_UNKNOWN`.

#### Scenario: Switching the carried pet

- **WHEN** pet A is carried and the character carries pet B
- **THEN** B is carried and A is in the stable

### Requirement: Renaming and releasing pets

Renaming a pet SHALL trim surrounding spaces and accept 1 to 12 characters counted as Unicode code points, otherwise reject with `NAME_INVALID`. Releasing a pet SHALL remove it.

#### Scenario: Name length

- **WHEN** a pet is renamed
- **THEN** the trimmed name is accepted only when it has 1 to 12 characters

##### Example: names

| Name | Result |
| --- | --- |
| `  小風  ` | 小風 |
| (empty) | NAME_INVALID |
| 13 characters | NAME_INVALID |

#### Scenario: Releasing

- **WHEN** a pet is released
- **THEN** it is no longer listed
