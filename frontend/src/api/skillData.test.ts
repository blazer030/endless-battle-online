import { expect, test } from "vitest"
import { ParseError } from "./parse"
import { JobChoiceConverter, SkillOverviewConverter, SkillSlotsConverter } from "./skillData"

const range = (minimum: number, maximum: number) => ({ minimum, maximum })

const overviewDto = {
  jobId: "job-6",
  skillPoints: 120,
  skills: [
    {
      id: "skill-1",
      name: "爆氣",
      slotType: "Active",
      timing: "BeforeAttack",
      description: "提升攻擊",
      icon: null,
      level: 1,
      maxLevel: 5,
      summary: "機率 10%｜自己物攻 +5%",
      taughtByCurrentJob: true,
      prerequisite: null,
      next: {
        level: 2,
        money: 100,
        proficiency: 50,
        summary: "機率 12%｜自己物攻 +7%",
        canLearn: false,
        enoughMoney: true,
        enoughProficiency: false,
      },
    },
    {
      id: "skill-61",
      name: "防禦心得",
      slotType: "Passive",
      timing: "RoundEnd",
      description: "",
      icon: "skill-61",
      level: 0,
      maxLevel: 1,
      summary: null,
      taughtByCurrentJob: false,
      prerequisite: { skillId: "skill-1", name: "爆氣", level: 3, met: false },
      next: null,
    },
  ],
  slots: [{ slot: 1, skillId: "skill-1" }],
  jobs: [
    {
      jobId: "job-7",
      jobName: "術士",
      tier: 1,
      current: false,
      skillPoints: 1882,
      skills: [
        {
          id: "skill-11",
          name: "魔力增幅",
          slotType: "Active",
          timing: "BeforeAttack",
          description: "",
          icon: "skill-11",
          level: 3,
          maxLevel: 10,
          rows: [
            { kind: "Trigger", text: "機率 60%" },
            { kind: "Cost", text: "SP 6" },
            { kind: "Effect", text: "自己魔攻 +3%，3 回合" },
          ],
          prerequisite: null,
          next: {
            level: 4,
            money: 9000,
            skillPoints: 941,
            rows: [{ kind: "Trigger", text: "機率 70%" }],
            canLearn: true,
            enoughMoney: true,
            enoughSkillPoints: true,
          },
        },
      ],
    },
  ],
}

const jobDto = {
  id: "job-14",
  name: "狂戰士",
  tier: 2,
  parentId: "job-6",
  parentName: "戰士",
  unlockLevel: 30,
  levelMet: false,
  proficiencyRequired: 3500,
  parentProficiencyMet: true,
  statRequirements: [{ stat: "strength", minimum: 60, met: false }],
  growth: {
    strength: range(3, 4),
    intelligence: range(0, 1),
    life: range(2, 3),
    vitality: range(0, 1),
    agility: range(1, 2),
    charisma: range(0, 1),
    faith: range(0, 1),
  },
  current: false,
  proficiencyPercent: 40,
  eligible: false,
}

const parseFailure = (convert: () => unknown): ParseError => {
  try {
    convert()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("SkillOverviewConverter_ConvertsTheWholeResponse", () => {
  const overview = SkillOverviewConverter.toVo(overviewDto)

  expect(overview.jobId).toBe("job-6")
  expect(overview.skillPoints).toBe(120)
  expect(overview.skills).toEqual(overviewDto.skills)
  expect(overview.slots).toEqual([{ slot: 1, skillId: "skill-1" }])
})

test("SkillOverviewConverter_ReadsJobs", () => {
  const overview = SkillOverviewConverter.toVo(overviewDto)

  expect(overview.jobs).toEqual(overviewDto.jobs)
})

test("SkillOverviewConverter_RejectsUnknownRowKind", () => {
  const [job] = overviewDto.jobs
  const [skill] = job.skills
  const jobs = [{ ...job, skills: [{ ...skill, rows: [{ kind: "Flavor", text: "?" }] }] }]

  expect(parseFailure(() => SkillOverviewConverter.toVo({ ...overviewDto, jobs })).path).toBe(
    "skills.jobs[0].skills[0].rows[0].kind",
  )
})

test("SkillOverviewConverter_RejectsAnUnknownTiming", () => {
  const skills = [{ ...overviewDto.skills[0], timing: "Sometimes" }]

  expect(parseFailure(() => SkillOverviewConverter.toVo({ ...overviewDto, skills })).path).toBe(
    "skills.skills[0].timing",
  )
})

test("SkillOverviewConverter_MissingSkillPointsNamesThePath", () => {
  const dto: Record<string, unknown> = { ...overviewDto }
  delete dto.skillPoints

  expect(parseFailure(() => SkillOverviewConverter.toVo(dto)).path).toBe("skills.skillPoints")
})

test("SkillOverviewConverter_IgnoresUnknownFields", () => {
  const overview = SkillOverviewConverter.toVo({ ...overviewDto, rank: "S" })

  expect(overview).not.toHaveProperty("rank")
})

test("JobChoiceConverter_ConvertsGrowthAndConditions", () => {
  const job = JobChoiceConverter.toVo(jobDto)

  expect(job.growth.strength).toEqual({ minimum: 3, maximum: 4 })
  expect(job.statRequirements).toEqual([{ stat: "strength", minimum: 60, met: false }])
  expect(job.levelMet).toBe(false)
  expect(job.parentProficiencyMet).toBe(true)
  expect(job.parentName).toBe("戰士")
})

test("JobChoiceConverter_IgnoresUnknownFields", () => {
  const job = JobChoiceConverter.toVo({ ...jobDto, rank: "S" })

  expect(job).not.toHaveProperty("rank")
})

test("SkillSlotsConverter_SendsTheSlotsAsTheRequestBody", () => {
  const commands = [{ slot: 1, skillId: "skill-1" }]

  expect(SkillSlotsConverter.toDto(commands)).toEqual({ slots: [{ slot: 1, skillId: "skill-1" }] })
})

test("JobChoiceConverter_MissingGrowthNamesThePath", () => {
  const growth: Record<string, unknown> = { ...jobDto.growth }
  delete growth.faith

  expect(parseFailure(() => JobChoiceConverter.toVo({ ...jobDto, growth })).path).toBe(
    "job.growth.faith",
  )
})
