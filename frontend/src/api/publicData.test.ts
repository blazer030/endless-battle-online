import { expect, test, vi } from "vitest"
import { createApiClient } from "./client"
import { ParseError } from "./parse"
import {
  CreationOptionsConverter,
  JobConverter,
  type JobDto,
  type PublicSettingsDto,
} from "./publicData"

const warriorDto: JobDto = {
  id: "job-6",
  name: "戰士",
  tier: 1,
  parentId: null,
  unlockLevel: 1,
  proficiencyRequired: 0,
  statRequirements: [],
  growth: {
    strength: { minimum: 1, maximum: 3 },
    intelligence: { minimum: 0, maximum: 1 },
    life: { minimum: 1, maximum: 3 },
    vitality: { minimum: 1, maximum: 3 },
    agility: { minimum: 0, maximum: 1 },
    charisma: { minimum: 1, maximum: 1 },
    faith: { minimum: 0, maximum: 1 },
  },
  skills: [
    {
      id: "skill-1",
      name: "爆氣",
      description: "",
      maxLevel: 2,
      prerequisite: null,
      levels: [
        { level: 1, summary: "機率 10%｜自己物攻 +5%" },
        { level: 2, summary: "機率 12%｜自己物攻 +7%" },
      ],
    },
    {
      id: "skill-61",
      name: "防禦心得",
      description: "",
      maxLevel: 1,
      prerequisite: { skillId: "skill-1", name: "爆氣", level: 3 },
      levels: [{ level: 1, summary: "常駐｜受到的物理傷害 -2%" }],
    },
  ],
}

const settingsDto: PublicSettingsDto = {
  sexes: [
    { id: 1, name: "男" },
    { id: 2, name: "女" },
  ],
  elements: [{ id: 3, name: "火", strongAgainst: 4, weakAgainst: 2 }],
  births: [{ id: 1, name: "中央大陸" }],
}

test("JobConverter_KeepsEveryFieldOfACompleteJob", () => {
  const job = JobConverter.toVo(warriorDto)

  expect(job.id).toBe("job-6")
  expect(job.name).toBe("戰士")
  expect(job.tier).toBe(1)
  expect(job.parentId).toBeNull()
  expect(job.growth.strength).toEqual({ minimum: 1, maximum: 3 })
  expect(job.growth.faith).toEqual({ minimum: 0, maximum: 1 })
  expect(job.skills.map((skill) => skill.name)).toEqual(["爆氣", "防禦心得"])
  expect(job.skills[0].prerequisite).toBeNull()
  expect(job.skills[1].prerequisite).toEqual({ skillId: "skill-1", name: "爆氣", level: 3 })
  expect(job.skills[0].levels).toEqual([
    { level: 1, summary: "機率 10%｜自己物攻 +5%" },
    { level: 2, summary: "機率 12%｜自己物攻 +7%" },
  ])
})

test("JobConverter_ThrowsWhenSkillsAreMissing", () => {
  const withoutSkills: Partial<JobDto> = { ...warriorDto }
  delete withoutSkills.skills

  expect(() => JobConverter.toVo(withoutSkills, "jobs[0]")).toThrow(ParseError)
  expect(() => JobConverter.toVo(withoutSkills, "jobs[0]")).toThrow("jobs[0].skills")
})

test("JobConverter_ThrowsWhenAGrowthStatIsMissing", () => {
  const growth: Partial<JobDto["growth"]> = { ...warriorDto.growth }
  delete growth.faith

  expect(() => JobConverter.toVo({ ...warriorDto, growth })).toThrow("job.growth.faith")
})

test("JobConverter_IgnoresUnknownFields", () => {
  const job = JobConverter.toVo({ ...warriorDto, icon: "warrior.webp" })

  expect(job.id).toBe("job-6")
  expect(Object.keys(job)).not.toContain("icon")
})

test("CreationOptionsConverter_KeepsOptionsAndElementAdvantage", () => {
  const options = CreationOptionsConverter.toVo(settingsDto)

  expect(options.sexes).toEqual([
    { id: 1, name: "男" },
    { id: 2, name: "女" },
  ])
  expect(options.elements).toEqual([{ id: 3, name: "火", strongAgainst: 4, weakAgainst: 2 }])
  expect(options.births).toEqual([{ id: 1, name: "中央大陸" }])
})

test("CreationOptionsConverter_ThrowsWhenElementAdvantageIsMissing", () => {
  const dto = { ...settingsDto, elements: [{ id: 3, name: "火", strongAgainst: 4 }] }

  expect(() => CreationOptionsConverter.toVo(dto)).toThrow("settings.elements[0].weakAgainst")
})

test("client_ReadsPublicJobsAndSettingsThroughConverters", async () => {
  const fetchMock = vi.fn().mockImplementation((path: string) =>
    Promise.resolve(
      new Response(JSON.stringify(path === "/api/public/jobs" ? [warriorDto] : settingsDto), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    ),
  )
  const client = createApiClient({ fetch: fetchMock, onUnauthorized: vi.fn() })

  const jobs = await client.getPublicJobs()
  const options = await client.getCreationOptions()

  expect(jobs.map((job) => job.id)).toEqual(["job-6"])
  expect(options.elements[0].strongAgainst).toBe(4)
  expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
    "/api/public/jobs",
    "/api/public/settings",
  ])
})
