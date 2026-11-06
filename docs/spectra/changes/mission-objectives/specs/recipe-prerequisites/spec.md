## Purpose

Defines recipes that can only be used after completing a mission, so the highest equipment recipes are unlocked by the mission chain.

## ADDED Requirements

### Requirement: A recipe can require a completed mission

A recipe SHALL be able to name a mission in `requiredMissionId`. Synthesizing it SHALL require that mission to be completed; otherwise the request SHALL fail with `missionIncomplete` and SHALL NOT consume materials or money. An active mission SHALL NOT count as completed. Content validation SHALL reject a required mission that does not exist or is a daily mission.

#### Scenario: Synthesis depends on the mission state

- **GIVEN** `recipe-174` requires `mission-317`, and the player has the materials and money
- **WHEN** the player synthesizes it

##### Example: Mission states

| `mission-317` state | Result |
| ------------------- | ------ |
| not accepted | fails with `missionIncomplete` |
| active | fails with `missionIncomplete` |
| completed | synthesis runs |

#### Scenario: A recipe without a prerequisite is unaffected

- **GIVEN** a recipe with no `requiredMissionId`
- **WHEN** the player synthesizes it with enough materials and money
- **THEN** synthesis runs

#### Scenario: A daily mission cannot be a prerequisite

- **WHEN** content names a daily mission in `requiredMissionId`
- **THEN** loading the content throws a `GameContentException`

### Requirement: The recipe list shows the prerequisite

The recipe list SHALL include locked recipes. Each recipe with a prerequisite SHALL report the mission's id, title and whether this character has completed it. The forge page SHALL disable a locked recipe and show the mission's title.

#### Scenario: A locked recipe is listed

- **GIVEN** the player has not completed `mission-317`
- **WHEN** the player opens the recipe list
- **THEN** `recipe-174` is listed with the mission's title and `Completed` false, and its synthesize button is disabled

#### Scenario: An unlocked recipe can be used

- **GIVEN** the player has completed `mission-317` and has the materials
- **WHEN** the player opens the recipe list
- **THEN** `recipe-174` reports `Completed` true, and its synthesize button is enabled
