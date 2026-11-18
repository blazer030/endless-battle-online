## MODIFIED Requirements

### Requirement: A recipe can require a completed mission

A recipe SHALL be able to name a mission in `requiredMissionId`. Synthesizing it SHALL require that mission to be completed; otherwise the request SHALL fail with `MISSION_INCOMPLETE` and SHALL NOT consume materials or money. An active mission SHALL NOT count as completed. Content validation SHALL reject a required mission that does not exist or is a daily mission.

#### Scenario: Synthesis depends on the mission state

- **GIVEN** `recipe-174` requires `mission-317`, and the player has the materials and money
- **WHEN** the player synthesizes it

##### Example: Mission states

| `mission-317` state | Result |
| ------------------- | ------ |
| not accepted | fails with `MISSION_INCOMPLETE` |
| active | fails with `MISSION_INCOMPLETE` |
| completed | synthesis runs |

#### Scenario: A recipe without a prerequisite is unaffected

- **GIVEN** a recipe with no `requiredMissionId`
- **WHEN** the player synthesizes it with enough materials and money
- **THEN** synthesis runs

#### Scenario: A daily mission cannot be a prerequisite

- **WHEN** content names a daily mission in `requiredMissionId`
- **THEN** loading the content throws a `GameContentException`

---
