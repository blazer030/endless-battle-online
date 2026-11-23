## Purpose

Skill learning lets a character spend money and a job's skill points to learn a skill or raise it by one level. It covers which skills a character can see and learn for its current job and for jobs it held before, the rejection reasons, and the skill overview the skills screens read.

## ADDED Requirements

### Requirement: Skill overview lists the current job's skills and other learned skills

`GET /api/skills` SHALL return `jobId` (the current job), `skillPoints` (the current job's skill points, 0 when the character has no proficiency row for it), `skills` and `slots`. `skills` SHALL list every skill the current job teaches in the order of `skills.json`, followed by every learned skill the current job does not teach in the order of `skills.json`. Each skill SHALL carry its level (0 when not learned), maximum level, the current level's summary (null when not learned), `taughtByCurrentJob`, its prerequisite with `met` (null when it has none), and `next` for the next level (null at the maximum level) with its money, its skill-point cost in `proficiency`, its summary, `enoughMoney`, `enoughProficiency` and `canLearn`. A skill whose learned level is above the content's maximum level SHALL be treated as being at the maximum level.

#### Scenario: Current job skills come first

- **GIVEN** a 戰士 (`job-6`) who has learned 魔力增幅 (`skill-11`, taught by 術士) at level 2 and 爆氣 (`skill-1`) at level 1
- **WHEN** the client requests `GET /api/skills`
- **THEN** `skills` lists 爆氣, 防禦心得, 破滅一擊, 嗜血, then 魔力增幅, and 魔力增幅 has `taughtByCurrentJob` false

#### Scenario: Unlearned skill

- **WHEN** a skill the current job teaches has not been learned
- **THEN** it has level 0, a null summary, and `next.level` 1

### Requirement: Learning raises a skill by one level

`POST /api/skills/learn` with `skillId` SHALL raise the skill by exactly one level when every condition is met, deduct the next level's money from the character and its skill-point cost from the chosen job's skill points, leave every job's proficiency unchanged, and return the updated skill overview. When no job is chosen the chosen job SHALL be the current job.

#### Scenario: Learning the first level

- **GIVEN** a 戰士 with 1,000 money and 100 skill points who has not learned 爆氣 (level 1 costs 500 money and 35 skill points)
- **WHEN** the client posts `POST /api/skills/learn { "skillId": "skill-1" }`
- **THEN** 爆氣 is level 1, the character has 500 money, the 戰士 has 65 skill points, and the 戰士's proficiency is unchanged

#### Scenario: Learning the next level

- **GIVEN** a 戰士 with 爆氣 at level 1, 5,000 money and 300 skill points (level 2 costs 2,000 money and 235 skill points)
- **WHEN** the client posts `POST /api/skills/learn { "skillId": "skill-1" }`
- **THEN** 爆氣 is level 2, the character has 3,000 money and the 戰士 has 65 skill points

### Requirement: Learning rejections and their order

Learning SHALL fail with `400` and every unmet reason in this order: `WRONG_JOB` (the chosen job does not teach the skill, or the chosen job is neither the current job nor a job the character held), `NOT_LEARNED` (the chosen job is a previous job and the skill is not learned), `PREREQUISITE` (the prerequisite skill's learned level is below the required level), `MAX_LEVEL` (the skill is at its maximum level), `NOT_ENOUGH_PROFICIENCY` (the chosen job's skill points are below the next level's cost) and `NOT_ENOUGH_MONEY` (the character's money is below the next level's money). At the maximum level the skill-point and money reasons SHALL NOT be reported. A job that is neither the current job nor a held job SHALL fail with `WRONG_JOB` only, without judging the other conditions. An unknown `skillId` SHALL fail with `SKILL_UNKNOWN`, an unknown `jobId` SHALL fail with `JOB_UNKNOWN`, and a request without `skillId` SHALL fail with `REQUEST_INVALID`. A rejected request SHALL change nothing.

#### Scenario: Every reason in order

- **WHEN** a request breaks several conditions at once
- **THEN** the reasons are listed in the order above

##### Example: rejection reasons

Unless stated, the character is a 戰士 with 1,000,000 money and 100,000 skill points.

| Situation | Reasons |
| --- | --- |
| learns 魔力增幅 (`skill-11`, taught by 術士) with no job chosen | `WRONG_JOB` |
| learns 防禦心得 (`skill-61`) with 爆氣 at level 2, 0 skill points and 0 money | `PREREQUISITE`, `NOT_ENOUGH_PROFICIENCY`, `NOT_ENOUGH_MONEY` |
| learns 爆氣 at level 10 (its maximum) with 0 money | `MAX_LEVEL` |
| was a 術士 with 100,000 skill points and 魔力增幅 at level 3; learns 魔力盾 (`skill-66`, not learned) choosing `job-7` | `NOT_LEARNED` |
| was never a 盜賊; learns 盜賊's `skill-21` choosing `job-8` | `WRONG_JOB` only |
| `skillId` `skill-9999` | `SKILL_UNKNOWN` |
| `jobId` `job-9999` | `JOB_UNKNOWN` |

#### Scenario: Rejection changes nothing

- **WHEN** learning fails for any reason
- **THEN** the character's money, every job's skill points and proficiency, and the learned skills are unchanged

### Requirement: Upgrading a learned skill with a previous job's skill points

A job the character held SHALL be any job, other than the current job, for which the character has a proficiency row. `POST /api/skills/learn` with `skillId` and `jobId` of a held job SHALL raise a skill that job teaches and the character has already learned by one level, spending that job's skill points and the character's money, under the same prerequisite, maximum-level, skill-point and money conditions as the current job. The character SHALL stay in its current job, and the current job's skill points SHALL be unchanged. A skill of a held job that the character has not learned SHALL NOT be learnable through that job.

#### Scenario: Upgrading a previous job's skill

- **GIVEN** a 戰士 with 3,000 money who was a 術士 with 300 skill points, and 魔力增幅 (`skill-11`) learned at level 1 (level 2 costs 2,000 money and 235 skill points)
- **WHEN** the client posts `POST /api/skills/learn { "skillId": "skill-11", "jobId": "job-7" }`
- **THEN** 魔力增幅 is level 2, the character has 1,000 money, the 術士 has 65 skill points, the 戰士's skill points are unchanged, and the character is still a 戰士

#### Scenario: A previous job's skill points are not enough

- **GIVEN** a 戰士 who was a 術士 with 100 skill points, and 魔力增幅 learned at level 1
- **WHEN** the client posts `POST /api/skills/learn { "skillId": "skill-11", "jobId": "job-7" }`
- **THEN** the request fails with `NOT_ENOUGH_PROFICIENCY` even when the 戰士 has enough skill points

### Requirement: Skill overview lists each held job's skills

The skill overview (from `GET /api/skills` and `POST /api/skills/learn`) SHALL carry `jobs`: the current job first, then every held job in the order of `jobs.json`. Each entry SHALL carry `jobId`, `jobName`, `tier`, `current`, `skillPoints` (that job's skill points) and `skills`. For the current job `skills` SHALL list every skill it teaches in the order of `skills.json`, learned or not; for a held job it SHALL list only the skills it teaches that the character has learned, in the same order. Each skill SHALL carry `id`, `name`, `slotType`, `timing`, `description`, `icon`, `level`, `maxLevel`, `rows` (the current level's detail rows, null when not learned), `prerequisite` with `met` (null when it has none), and `next` (null at the maximum level) with `level`, `money`, `skillPoints`, `rows`, `enoughMoney`, `enoughSkillPoints` and `canLearn`, all judged with that entry's job: `canLearn` SHALL be `true` exactly when learning that skill through that job would succeed. The entries SHALL NOT carry reason codes.

#### Scenario: A previous job lists only learned skills

- **GIVEN** a 戰士 who was a 術士 with 1,882 skill points, with 魔力增幅 learned at level 3 and 魔力盾 and 治癒術 not learned
- **WHEN** the client requests `GET /api/skills`
- **THEN** `jobs` has the 戰士 entry first with `current` true and its four skills, then the 術士 entry with `current` false, `skillPoints` 1882 and only 魔力增幅

#### Scenario: Each job judges with its own skill points

- **GIVEN** a 戰士 with 0 skill points and 1,000,000 money who was a 術士 with 5,000 skill points, with 魔力增幅 at level 1
- **WHEN** the client requests `GET /api/skills`
- **THEN** in the 術士 entry 魔力增幅's `next` has `enoughSkillPoints` true and `canLearn` true

#### Scenario: A held job without learned skills

- **WHEN** the character holds a job whose skills it never learned
- **THEN** that job's entry is listed with an empty `skills`

### Requirement: Skill level detail rows

Each level's detail rows SHALL be, in order: a `Trigger` row whose text is the conditions joined by `、`, or `常駐` when the level has no condition; a `Cost` row whose text is the costs joined by `、` (`SP N`, `SP 最大值×N%`, `全部 SP`, `HP N`, `HP 最大值×N%`, `金錢 N`), omitted when the level has no cost; an `Effect` row whose text is the effects joined by `、`, written as in the skill summary; and a `Cooldown` row with `N 回合`, omitted when the cooldown is 0. Each row SHALL carry `kind` (`Trigger`, `Cost`, `Effect` or `Cooldown`) and `text`; the client names the row.

#### Scenario: Rows of an active skill

- **WHEN** the rows of 爆氣 level 1 are built
- **THEN** they are `Trigger` `機率 40%`, `Cost` `SP 2`, `Effect` `自己物攻 +1%，3 回合`

#### Scenario: Rows of a passive skill without conditions or costs

- **WHEN** the rows of 華爾滋 (`skill-492`) level 1 are built
- **THEN** they are a `Trigger` row `常駐` and an `Effect` row, with no `Cost` and no `Cooldown` row

#### Scenario: Cooldown row

- **WHEN** a level has cooldown 4
- **THEN** its last row is `Cooldown` `4 回合`
