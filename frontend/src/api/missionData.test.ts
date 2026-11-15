import { expect, test } from "vitest"
import { MissionBoardVo, MissionCompletionVo } from "../game/missions"
import { makeCharacter, makeMission, makeMissionBoard } from "../test/fixtures"
import { MissionBoardConverter, MissionCompletionConverter } from "./missionData"
import { ParseError } from "./parse"

const asDto = (value: unknown): Record<string, unknown> => JSON.parse(JSON.stringify(value))

const daily = makeMission({
  id: "test-daily",
  daily: true,
  expiresAt: new Date("2026-10-23T01:00:00Z"),
  deliverPet: { monsterId: "monster-3", name: "飛兔", carried: true },
  requiredSkill: { skillId: "skill-1", name: "爆氣", level: 2, met: false, learnedLevel: null },
  rewards: {
    experience: 0,
    money: 100,
    items: [{ itemId: "potion", name: "藥水", quantity: 2, category: "Material" }],
    skill: { skillId: "skill-2", name: "冰箭", level: 1 },
    pet: { monsterId: "monster-3", name: "飛兔", level: 1 },
  },
})
const board = makeMissionBoard({ available: [makeMission()], active: [daily] })
const boardDto = asDto(board)

const parseFailure = (convert: () => unknown) => {
  try {
    convert()
  } catch (error) {
    if (error instanceof ParseError) return error
    throw error
  }
  throw new Error("沒有丟出 ParseError")
}

test("MissionBoardConverter_ConvertsTheWholeResponse", () => {
  const converted = MissionBoardConverter.toVo(boardDto)

  expect(converted).toBeInstanceOf(MissionBoardVo)
  expect(converted).toEqual(board)
})

test("MissionBoardConverter_ConvertsTimesToDates", () => {
  const converted = MissionBoardConverter.toVo(boardDto)

  expect(converted.nextDailyReset).toEqual(new Date(Date.UTC(2026, 9, 23, 1)))
  expect(converted.active[0].expiresAt).toEqual(new Date(Date.UTC(2026, 9, 23, 1)))
  expect(converted.available[0].expiresAt).toBeNull()
})

test("MissionBoardConverter_MissingRewardExperienceNamesThePath", () => {
  const dto = asDto(boardDto)
  const [mission] = dto.active as { rewards: Record<string, unknown> }[]
  delete mission.rewards.experience

  expect(parseFailure(() => MissionBoardConverter.toVo(dto)).path).toBe(
    "missions.active[0].rewards.experience",
  )
})

test("MissionBoardConverter_IgnoresUnknownFields", () => {
  const dto = asDto(boardDto)
  ;(dto.available as Record<string, unknown>[])[0].difficulty = "easy"

  expect(MissionBoardConverter.toVo(dto).available[0]).not.toHaveProperty("difficulty")
})

const completionDto = asDto({
  board,
  character: makeCharacter({ experience: 190 }),
  completionMessage: "謝謝你。",
  experience: 190,
  money: 50,
  items: [],
  skill: null,
  skillGranted: false,
  levelsGained: 0,
  pet: null,
})

test("MissionCompletionConverter_ConvertsTheWholeResponse", () => {
  const converted = MissionCompletionConverter.toVo(completionDto)

  expect(converted).toBeInstanceOf(MissionCompletionVo)
  expect(converted.board).toEqual(board)
  expect(converted.character.experience).toBe(190)
  expect([converted.experience, converted.money, converted.completionMessage]).toEqual([
    190,
    50,
    "謝謝你。",
  ])
})

test("MissionCompletionConverter_MissingCharacterFieldNamesThePath", () => {
  const dto = asDto(completionDto)
  delete (dto.character as Record<string, unknown>).money

  expect(parseFailure(() => MissionCompletionConverter.toVo(dto)).path).toBe(
    "missionCompletion.character.money",
  )
})

test("MissionCompletionConverter_IgnoresUnknownFields", () => {
  expect(MissionCompletionConverter.toVo({ ...completionDto, fanfare: true })).not.toHaveProperty(
    "fanfare",
  )
})

// 規格 missions「Board details for the mission board screen」
const detailDto = () => {
  const dto = asDto(makeMissionBoard({ completed: [makeMission({ id: "mission-1" })] }))
  const mission = (dto.completed as Record<string, unknown>[])[0]
  mission.completionMessage = "感謝您的努力"
  mission.deliver = [
    { itemId: "item-331", name: "蜘蛛絲", quantity: 1, owned: 0, category: "Material" },
  ]
  mission.rewards = {
    experience: 3800,
    money: 0,
    items: [{ itemId: "item-342", name: "白色布料", quantity: 1, category: "Material" }],
    skill: null,
    pet: null,
  }
  mission.requirements = {
    jobId: "job-6",
    sex: 2,
    birth: null,
    prerequisiteId: null,
    prerequisiteTitle: null,
    jobName: "戰士",
    sexName: "女",
    birthName: null,
  }
  mission.requiredSkill = {
    skillId: "skill-414",
    name: "施毒",
    level: 2,
    met: false,
    learnedLevel: 1,
  }
  dto.firstDailyLevel = 50
  return { dto, mission }
}

test("MissionBoardConverter_ReadsScreenDetails", () => {
  const converted = MissionBoardConverter.toVo(detailDto().dto)

  const mission = converted.completed[0]
  expect(converted.firstDailyLevel).toBe(50)
  expect(mission.completionMessage).toBe("感謝您的努力")
  expect(mission.deliver[0].category).toBe("Material")
  expect(mission.rewards.items[0].category).toBe("Material")
  expect(mission.requirements).toMatchObject({ jobName: "戰士", sexName: "女", birthName: null })
  expect(mission.requiredSkill?.learnedLevel).toBe(1)
})

test("MissionBoardConverter_RejectsAnUnknownCategory", () => {
  const { dto, mission } = detailDto()
  ;(mission.rewards as { items: Record<string, unknown>[] }).items[0].category = "Hat"

  expect(parseFailure(() => MissionBoardConverter.toVo(dto)).message).toContain(
    "missions.completed[0].rewards.items[0].category",
  )
})

test("MissionCompletionConverter_ReadsItemCategory", () => {
  const dto = asDto(
    MissionCompletionVo.create({
      board: makeMissionBoard(),
      character: makeCharacter(),
      completionMessage: "",
      experience: 190,
      money: 50,
      items: [{ itemId: "test-stone", name: "測試攻擊魔石", quantity: 1, category: "Stone" }],
      skill: null,
      skillGranted: false,
      levelsGained: 0,
      pet: null,
    }),
  )

  expect(MissionCompletionConverter.toVo(dto).items[0].category).toBe("Stone")
})
