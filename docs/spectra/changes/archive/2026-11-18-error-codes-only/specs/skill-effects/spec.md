## MODIFIED Requirements

### Requirement: Jobs choose their skills

Skills SHALL NOT name a job, and jobs SHALL NOT list skills. Which job teaches which skill SHALL be a row in the job–skill relation table `job-skills.json`; one skill can appear in rows for several jobs, and a job–skill pair SHALL NOT repeat. Loading SHALL fail when a row names a missing job or skill, or repeats a pair. A character SHALL learn a skill only when a row pairs the character's current job with that skill and its prerequisite skill has reached the required level; learning SHALL spend the current job's skill points.

#### Scenario: A skill listed by the current job can be learned

- **GIVEN** a `job-skills.json` row pairs `job-6` 戰士 with `skill-1` 爆氣, and a 戰士 character with enough money and skill points
- **WHEN** the character learns 爆氣 level 1
- **THEN** learning succeeds and the 戰士 skill points drop by level 1's proficiency

#### Scenario: A skill not listed by the current job is refused

- **WHEN** a 戰士 character tries to learn a skill that no `job-skills.json` row pairs with 戰士
- **THEN** learning is refused with reason `WRONG_JOB`

#### Scenario: A relation row naming a missing skill is rejected

- **WHEN** a `job-skills.json` row pairs `job-6` with a skill id that does not exist
- **THEN** loading fails with a `GameContentException` that names the row's job and skill

#### Scenario: A missing prerequisite is refused

- **GIVEN** a skill whose prerequisite is 爆氣 level 3, and a character who has 爆氣 level 2
- **WHEN** the character tries to learn it
- **THEN** learning is refused with reason `PREREQUISITE`
