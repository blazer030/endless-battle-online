## Purpose

The balance report turns a set of game content into numbers a designer can compare: when each job can wear each shop tier, and how a reference character fares in each place. It runs the game's own domain simulation, so formula changes show up without editing the report.

## ADDED Requirements

### Requirement: Report source

The system SHALL generate a balance report from either the official content or the draft, chosen per request. A report request for the draft when no draft exists SHALL return 404 Not Found. A report request for an invalid draft SHALL return 409 Conflict with the loader's error message.

#### Scenario: Report the draft

- **WHEN** a valid draft exists and the admin requests `POST /admin/reports` with source `draft`
- **THEN** the report is computed from the draft content and the official content is not read for it

#### Scenario: Same input, same report

- **WHEN** the admin requests the same report twice for unchanged content
- **THEN** both responses are identical

### Requirement: Wear levels

The report SHALL list, for every starting-tier job and every requirement stat, the lowest character level at which the reference character's base stats meet the lowest requirement of each shop level. Levels above 100 SHALL be reported as unreachable.

#### Scenario: Requirement beyond level 100

- **WHEN** a job's expected growth never reaches a shop level's requirement by level 100
- **THEN** that cell is reported as unreachable instead of a level

### Requirement: Adventure results per place

For every place that has monsters and every starting-tier job, the report SHALL simulate an adventure with the domain adventure simulator, using a fixed random seed and fixed duration, and report fights, win rate, losses, timeouts, experience per hour, gross and net money per hour, levels gained, and hours of net income needed for the next shop tier's full equipment set.

#### Scenario: Reference character for a place

- **WHEN** the report simulates place P whose recommended level range starts at level L
- **THEN** the reference character is level L, has expected base stats for that level, and wears the highest shop-level equipment it meets the requirements for in every slot

### Requirement: Skills in the report

Each adventure result SHALL be reported twice: without skills, and with the job's skills equipped. With skills, the reference character SHALL equip the job's active skills in active slots and passive skills in passive slots, in content order, up to the slot limits of the domain skill loadout. The skill level SHALL be chosen per request as either level 1 or each skill's maximum level, defaulting to maximum.

#### Scenario: Skill change shows in the report

- **WHEN** the admin raises a warrior skill's chance in the draft and reports the draft with skills
- **THEN** the warrior's with-skills results differ from the official report while the without-skills results are the same
