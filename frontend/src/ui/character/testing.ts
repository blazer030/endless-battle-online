import { vi } from "vitest"
import { BuffVo } from "../../game/buffs"
import { InnVo } from "../../game/inn"
import {
  SkillOverviewVo,
  type LearnableSkill,
  type SkillDetailRow,
  type SkillJob,
} from "../../game/skillOverview"
import { makeCharacter, makeCreationOptions } from "../../test/fixtures"
import type { CharacterScreenApi } from "./CharacterScreen"

// 角色頁測試共用的「現在」：倒數與住宿都從這裡算
export const Now = new Date("2026-11-19T12:00:00Z")
export const at = (seconds: number) => new Date(Now.getTime() + seconds * 1000)

// 設計稿示範的戰士：Lv 162、熟練度 2,450、當過術士
export const warrior = makeCharacter({
  name: "收尾者",
  level: 162,
  money: 128_400,
  jobId: "job-6",
  jobName: "戰士",
  sex: 1,
  element: 3,
  birth: 1,
  healthPoints: 6212,
  manaPoints: 980,
  experience: 42_000,
  nextLevelExperience: 100_000,
  jobProficiency: [
    { jobId: "job-6", proficiency: 2450, skillPoints: 2587 },
    { jobId: "job-7", proficiency: 3500, skillPoints: 1882 },
  ],
  stats: { ...makeCharacter().stats, strength: 430, maxHealthPoints: 9640, maxManaPoints: 1205 },
  baseStats: { ...makeCharacter().baseStats, strength: 412 },
})

export const rows = (trigger: string, cost: string, effect: string): SkillDetailRow[] => [
  { kind: "Trigger", text: trigger },
  { kind: "Cost", text: cost },
  { kind: "Effect", text: effect },
]

export const learnableSkill = (overrides: Partial<LearnableSkill> = {}): LearnableSkill => ({
  id: "skill-1",
  name: "爆氣",
  slotType: "Active",
  timing: "BeforeAttack",
  description: "爆發體內的鬥氣，接下來幾回合物攻提升。",
  icon: "skill-1",
  level: 6,
  maxLevel: 10,
  rows: rows("機率 90%", "SP 12", "自己物攻 +6%，3 回合"),
  prerequisite: null,
  next: {
    level: 7,
    money: 27_000,
    skillPoints: 2587,
    rows: rows("機率 100%", "SP 12", "自己物攻 +7%，3 回合"),
    canLearn: true,
    enoughMoney: true,
    enoughSkillPoints: true,
  },
  ...overrides,
})

export const warriorJob = (overrides: Partial<SkillJob> = {}): SkillJob => ({
  jobId: "job-6",
  jobName: "戰士",
  tier: 1,
  current: true,
  skillPoints: 2587,
  skills: [
    learnableSkill(),
    learnableSkill({
      id: "skill-61",
      name: "防禦心得",
      level: 3,
      prerequisite: { skillId: "skill-1", name: "爆氣", level: 3, met: true },
    }),
    learnableSkill({
      id: "skill-71",
      name: "破滅一擊",
      level: 1,
      prerequisite: { skillId: "skill-61", name: "防禦心得", level: 3, met: true },
    }),
    learnableSkill({
      id: "skill-77",
      name: "嗜血",
      level: 0,
      rows: null,
      prerequisite: { skillId: "skill-71", name: "破滅一擊", level: 3, met: false },
      next: {
        level: 1,
        money: 12_000,
        skillPoints: 2941,
        rows: rows("HP 低於 50%", "SP 10", "回復 HP 物攻×30%"),
        canLearn: false,
        enoughMoney: true,
        enoughSkillPoints: false,
      },
    }),
  ],
  ...overrides,
})

export const sorcererJob = (overrides: Partial<SkillJob> = {}): SkillJob => ({
  jobId: "job-7",
  jobName: "術士",
  tier: 1,
  current: false,
  skillPoints: 1882,
  skills: [learnableSkill({ id: "skill-11", name: "魔力增幅", level: 3, description: "" })],
  ...overrides,
})

export const overviewWith = (jobs: SkillJob[] = [warriorJob(), sorcererJob()]) =>
  SkillOverviewVo.create({ jobId: "job-6", skillPoints: 2587, skills: [], slots: [], jobs })

export const attackElixir = BuffVo.create({
  kind: "Attack",
  multiplier: 1.05,
  startsAt: at(-1200),
  endsAt: at(6000),
  itemId: "item-2257",
  itemName: "神力秘藥LV1",
})

export const loft = {
  id: "apprentice-loft",
  name: "學徒閣樓",
  kind: "Experience" as const,
  multiplier: 1.1,
  seconds: 28_800,
  price: 100,
}

export const innWithLodging = InnVo.create({
  rooms: [loft],
  lodging: { roomId: "apprentice-loft", startsAt: at(-3600), endsAt: at(25_200) },
  restFee: 0,
})

export const createCharacterApi = (
  overrides: Partial<CharacterScreenApi> = {},
): CharacterScreenApi => ({
  getMyCharacter: vi.fn().mockResolvedValue(warrior),
  getAccount: vi.fn().mockResolvedValue({ accountId: 1, username: "ethan" }),
  getBuffs: vi.fn().mockResolvedValue([attackElixir]),
  getInn: vi.fn().mockResolvedValue(innWithLodging),
  getSkills: vi.fn().mockResolvedValue(overviewWith()),
  getCreationOptions: vi.fn().mockResolvedValue(makeCreationOptions()),
  learnSkill: vi.fn(),
  ...overrides,
})
