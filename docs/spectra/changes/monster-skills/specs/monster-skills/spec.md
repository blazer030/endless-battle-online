## Purpose

Monsters use skills in battle with the same rules as players, each at its own activation rate. Skill effects work in both directions, and the battle stage and log show what a monster casts.

## ADDED Requirements

### Requirement: Monster skills are listed in a content file

The content file `monster-skills.json` SHALL list each monster's skills as rows of monster id, skill id, level and activation rate, and SHALL be required for content to load. A monster's skills SHALL be tried in file order. Loading SHALL be rejected with a `GameContentException` when a row references an unknown monster or skill, when the level is outside 1 to the skill's maximum level, when the rate is outside 1 to 100, or when one monster lists the same skill twice.

#### Scenario: A row is loaded onto its monster

- **GIVEN** `monster-skills.json` with the row monster `monster-1`, skill `skill-1`, level 2, rate 10
- **WHEN** the content loads
- **THEN** `monster-1` has that skill at level 2, and its level 2 activation is a single `Chance` 10

#### Scenario: Invalid rows are rejected

##### Example: rejected rows

| Row | Reason |
| --- | --- |
| monster `monster-x` | unknown monster |
| skill `skill-x` | unknown skill |
| level 0 | level below 1 |
| level above the skill's maximum level | level too high |
| rate 0 | rate below 1 |
| rate 101 | rate above 100 |
| two rows for `monster-1` and `skill-1` | duplicate skill |

- **WHEN** the content loads with one of these rows
- **THEN** loading throws a `GameContentException` that names the monster or skill

#### Scenario: The file is required

- **WHEN** the content directory has no `monster-skills.json`
- **THEN** loading throws a `GameContentException`

### Requirement: The activation rate replaces the Chance condition

A monster's activation rate SHALL replace every `Chance` condition of its skill. The skill's other conditions SHALL still apply. A skill with no conditions SHALL activate only when the rate roll succeeds. A monster SHALL pay `SkillPoints` and `HitPoints` costs and SHALL NOT pay `Money` costs. A chance belonging to an effect SHALL still be rolled after activation, so the effect's real chance is the rate multiplied by that chance.

#### Scenario: The rate decides activation

##### Example: 爆氣 level 2 has Chance 50, the monster's rate is 10

| Activation roll | Result |
| --- | --- |
| 10 | activates |
| 11 | does not activate |

- **WHEN** the monster tries the skill with enough SP
- **THEN** the skill activates only as in the table

#### Scenario: Not enough SP means no roll

- **GIVEN** a monster with 3 SP and a skill costing 4 SP
- **WHEN** the monster tries the skill
- **THEN** the skill does not activate and no random number is consumed

#### Scenario: Other conditions still apply

- **GIVEN** a skill with conditions `HitPointsBelow` 50 and `Chance` 40, and a monster rate of 100
- **WHEN** the monster has 60% HP
- **THEN** the skill does not activate

#### Scenario: An always-on skill rolls the rate

- **GIVEN** 燃燒魔力, which has no conditions, on a monster with rate 12
- **WHEN** the activation roll is 13
- **THEN** the skill does not activate and the player loses no SP

#### Scenario: An effect's own chance is rolled after activation

- **GIVEN** 暈擊 level 2 with `Stun` chance 6 on a monster with rate 10
- **WHEN** the activation roll is 10 and the stun roll before the player's attack is 7
- **THEN** the skill activates and the player still attacks

#### Scenario: Money is not paid

- **GIVEN** a monster skill with a `Money` cost of 500
- **WHEN** it activates
- **THEN** it activates and the battle's money spent is 0

### Requirement: Skill effects work in both directions

A monster's skills SHALL use the same casting rules as a player's, with the monster as the caster and the player as the opponent. A monster's effects on itself SHALL change its own attack, defence, speed, hit rate, critical rate, damage, damage taken and elements, and its effects on the opponent SHALL change the player's. Monster skills SHALL be used in adventure fights and mercenary fights. A monster with no skills SHALL fight exactly as before and SHALL consume the same random numbers.

#### Scenario: A monster's buff raises its damage

- **GIVEN** a monster with a skill raising its attack by 100% for 3 rounds at rate 100
- **WHEN** the skill is active and the monster hits
- **THEN** the damage is computed from the doubled attack

#### Scenario: A monster's debuff lowers the player's stat

- **GIVEN** a monster with a skill lowering the opponent's attack by 50% for 3 rounds at rate 100
- **WHEN** the skill is active and the player hits
- **THEN** the damage is computed from the player's halved attack

#### Scenario: Both sides' changes combine

- **GIVEN** a player with base attack 100 and a skill raising their own attack by 100%, and a monster with a skill lowering the opponent's attack by 50%, both active
- **WHEN** the player hits
- **THEN** the damage is computed from an attack of 100

#### Scenario: A monster's damage reduction applies to player and pet attacks

- **GIVEN** a monster with an active `DamageReduction` of 50% for physical attacks
- **WHEN** the player's physical hit or the pet's physical hit deals 100 before reduction
- **THEN** the monster loses 50 HP

#### Scenario: A monster heals itself

- **GIVEN** a monster at 100 of 500 HP with a skill restoring 50 HP
- **WHEN** the skill activates
- **THEN** the monster has 150 HP and the event has caster `Monster` and heal 50

#### Scenario: A monster's after-hit skill uses its damage

- **GIVEN** a monster with an `AfterHit` skill restoring HP from 50% of its damage at rate 100
- **WHEN** its physical hit deals 40 to the player
- **THEN** the monster recovers 20 HP

#### Scenario: A guarded hit triggers no after-hit skill

- **GIVEN** the same monster and a pet that guards the hit
- **WHEN** the monster's hit is guarded
- **THEN** the monster recovers no HP

#### Scenario: A monster's multi-hit attacks several times

- **GIVEN** a monster with an active `MultiHit` of 3 hits for physical attacks
- **WHEN** it attacks physically
- **THEN** it rolls hit, critical and damage for each of up to 3 hits and stops when the player falls

#### Scenario: A stunned player does not attack

- **GIVEN** a monster with an active `Stun` chance 100
- **WHEN** the player's turn to attack comes
- **THEN** the player does not attack and an attack event with attacker `Player` and stunned true is recorded

#### Scenario: Monster damage over time can defeat the player

- **GIVEN** a monster with an active `DamageOverTime` and a player whose HP is below that damage
- **WHEN** the round ends
- **THEN** a skill event with caster `Monster` and lasting true is recorded, followed by a defeat event for the player, and the monster wins

#### Scenario: A monster's skill damage skips the pet's guard

- **GIVEN** a carried pet with guard left and a monster skill with `PercentDamage` 10
- **WHEN** the skill activates with the player at 1000 HP
- **THEN** the player has 900 HP and the pet's guard does not change

#### Scenario: Monster skills apply in mercenary fights

- **GIVEN** a mercenary fighting a monster with a skill at rate 100
- **WHEN** the mercenary's fight runs
- **THEN** the monster's skill is cast during that fight

#### Scenario: A monster with no skills consumes the same random numbers

- **GIVEN** a scripted random source and a monster with no skills
- **WHEN** the battles in the existing battle tests run
- **THEN** their expected events and consumed random numbers do not change

### Requirement: Monster casts happen after the player's

At the start of a round, the monster SHALL cast after the player and before the first attacker is decided. If the player's cast defeats the monster, the monster SHALL NOT cast. If the monster's cast defeats the player, the round SHALL end with the player's defeat. After-hit skills SHALL be tried by the side that hit. At the end of a round, the player's skills SHALL be tried before the monster's, followed by the player's and then the monster's damage over time.

#### Scenario: A monster's speed change affects the same round

- **GIVEN** a monster casting a speed increase at the start of the round
- **WHEN** the first attacker is decided in that round
- **THEN** the decision uses the monster's raised speed

#### Scenario: A monster defeated by the player's cast does not cast

- **GIVEN** a player's `PercentDamage` skill that brings the monster to 0 HP, and a monster skill at rate 100
- **WHEN** the round starts
- **THEN** no skill event with caster `Monster` is recorded and the player wins

### Requirement: The stage and log show monster skills

The battle log SHALL describe a monster's skill with the monster's name and the amounts it healed, restored, dealt and removed, and SHALL NOT describe the monster's costs. The battle stage SHALL show a monster's cast as a cyan plate with the skill's name over the monster, with no full-screen flash, and SHALL update both sides' HP from the event. A stunned player SHALL be shown with a muted 暈眩 plate over the player and a log line.

#### Scenario: Log lines for a monster's skill

##### Example: skill events with caster `Monster` against 史萊姆

| Event | Log lines |
| --- | --- |
| 爆氣 cast, no amounts | `史萊姆施展了爆氣！` |
| 治癒術 cast, heal 50 | `史萊姆施展了治癒術！`, `史萊姆回復了 50 點 HP。` |
| 魔力吸收 cast, SP gain 30 | `史萊姆施展了魔力吸收！`, `史萊姆回復了 30 點 SP。` |
| 星芒 cast, damage 120 | `史萊姆施展了星芒！`, `你受到 120 點傷害。` |
| 燃燒魔力 cast, opponent SP loss 40 | `史萊姆施展了燃燒魔力！`, `你失去 40 點 SP。` |
| 施毒 lasting, damage 25 | `施毒持續作用，你受到 25 點傷害。` |
| 爆氣 cast, SP cost 4 | `史萊姆施展了爆氣！` |

- **WHEN** the log describes the event
- **THEN** the lines are exactly as in the table, and the cast line has kind `trigger`

#### Scenario: A stunned player's log line

- **WHEN** an attack event has attacker `Player` and stunned true
- **THEN** the log line is `你暈眩中，無法行動。` with kind `muted`

#### Scenario: Stage effects of a monster's cast

- **WHEN** a skill event with caster `Monster` and name 爆氣 is shown, not lasting
- **THEN** the stage effects are one cyan plate 爆氣 over the monster, with no skill flash

#### Scenario: Stage effects of a stunned player

- **WHEN** an attack event has attacker `Player` and stunned true
- **THEN** the stage effects are one muted plate 暈眩 over the player

#### Scenario: Stage HP follows a monster's skill

##### Example: stage at monster HP 100 of 500 and player HP 800 of 1000

| Event with caster `Monster` | Monster HP | Player HP |
| --- | --- | --- |
| heal 50 | 150 | 800 |
| heal 450 | 500 | 800 |
| HP cost 10 | 90 | 800 |
| opponent HP after 700 | 100 | 700 |

- **WHEN** the event is applied to the stage
- **THEN** the HP values are as in the table

#### Scenario: The summary ignores monster casts

- **GIVEN** a skill event with caster `Monster`
- **WHEN** the adventure summary counts skills cast
- **THEN** the event is not counted as a skill cast
