## ADDED Requirements

### Requirement: A stun chance can depend on comparing a stat

A `Stun` effect SHALL be able to declare `chanceByStat` with a stat and a percent. When the caster's base stat is above the opponent's, the stun chance SHALL rise by the percent; otherwise, including a tie, it SHALL fall by the percent. A resulting chance of 0 or less SHALL never stun. Loading SHALL be rejected with a `GameContentException` when the percent is outside 1 to 100. The generated effect text SHALL show the adjustment.

#### Scenario: The stun chance follows the comparison

##### Example: 假死 level 1 has `Stun` chance 2 and `chanceByStat` intelligence 5

| Caster's intelligence | Opponent's intelligence | Stun chance |
| --- | --- | --- |
| 100 | 50 | 7 |
| 50 | 50 | never |
| 50 | 100 | never |

- **WHEN** 假死 is active and the opponent is about to attack
- **THEN** the stun chance is as in the table

##### Example: 假死 level 7 has `Stun` chance 14

| Caster's intelligence | Opponent's intelligence | Stun chance |
| --- | --- | --- |
| 100 | 50 | 19 |
| 50 | 100 | 9 |

- **WHEN** 假死 is active and the opponent is about to attack
- **THEN** the stun chance is as in the table

#### Scenario: An invalid adjustment is rejected

- **WHEN** a `Stun` effect has `chanceByStat` percent 0 or 101
- **THEN** loading throws a `GameContentException`

#### Scenario: The effect text shows the adjustment

- **WHEN** the effect text is generated for `Stun` chance 2 with `chanceByStat` intelligence 5
- **THEN** the text is `對手無法行動 機率 2%（智力比對手高 +5%，否則 -5%）`

### Requirement: Silence blocks the opponent during a battle

When a skill with a `Silence` effect activates in a battle, its chance SHALL be rolled after the other effect chances. On success, the opponent's skills SHALL NOT activate or roll for the effect's rounds, counting the current round, and for at least 1 round. Skills already active SHALL continue until they expire. Silence SHALL work for both the player and the monster.

#### Scenario: A player silences a monster

- **GIVEN** a player's skill with `Silence` chance 100 for 2 rounds, cast in round 1, and a monster skill at rate 100
- **WHEN** rounds 1 to 3 run
- **THEN** the monster casts no skill in rounds 1 and 2 and casts in round 3

#### Scenario: A monster silences a player

- **GIVEN** a monster skill with `Silence` chance 100 for 1 round at rate 100, cast in round 1, and a player's `RoundEnd` skill with `Chance` 100
- **WHEN** round 1 ends
- **THEN** the player's skill does not activate

#### Scenario: A failed silence roll does nothing

- **GIVEN** a skill with `Silence` chance 10
- **WHEN** it activates and the silence roll is 11
- **THEN** the opponent's skills are not blocked
