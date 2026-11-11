import { expect, test } from "vitest"
import { ParseError } from "./parse"
import { JobChoiceConverter, SkillOverviewConverter } from "./skillData"

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
      prerequisite: null,
      next: {
        level: 2,
        money: 100,
        proficiency: 50,
        summary: "機率 12%｜自己物攻 +7%",
        canLearn: false,
        reasons: ["notEnoughSkillPoints"],
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
      prerequisite: { skillId: "skill-1", name: "爆氣", level: 3 },
      next: null,
    },
  ],
  slots: [{ slot: 1, skillId: "skill-1" }],
}

const jobDto = {
  id: "job-14",
  name: "狂戰士",
  tier: 2,
  parentId: "job-6",
  parentName: "戰士",
  unlockLevel: 30,
  proficiencyRequired: 3500,
  statRequirements: [{ stat: "strength", minimum: 60 }],
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
  reasons: ["levelTooLow", "statTooLow:strength"],
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

test("SkillOverviewConverter_RejectsAnUnknownTiming", () => {
  const skills = [{ ...overviewDto.skills[0], timing: "Sometimes" }]

  expect(parseFailure(() => SkillOverviewConverter.toVo({ ...overviewDto, skills })).path).toBe(
    "skills.skills[0].timing",
  )
})

test("SkillOverviewConverter_IgnoresUnknownFields", () => {
  const overview = SkillOverviewConverter.toVo({ ...overviewDto, rank: "S" })

  expect(overview).not.toHaveProperty("rank")
})

test("JobChoiceConverter_ConvertsGrowthAndReasons", () => {
  const job = JobChoiceConverter.toVo(jobDto)

  expect(job.growth.strength).toEqual({ minimum: 3, maximum: 4 })
  expect(job.statRequirements).toEqual([{ stat: "strength", minimum: 60 }])
  expect(job.reasons).toEqual(["levelTooLow", "statTooLow:strength"])
  expect(job.parentName).toBe("戰士")
})

test("JobChoiceConverter_MissingGrowthNamesThePath", () => {
  const growth: Record<string, unknown> = { ...jobDto.growth }
  delete growth.faith

  expect(parseFailure(() => JobChoiceConverter.toVo({ ...jobDto, growth })).path).toBe(
    "job.growth.faith",
  )
})
