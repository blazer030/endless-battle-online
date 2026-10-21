import type { AdvanceSummary, CharacterView, ContentNames } from "../game/types"

export const makeCharacter = (overrides: Partial<CharacterView> = {}): CharacterView => ({
  name: "測試",
  level: 1,
  exp: 0,
  nextExp: 1000,
  money: 0,
  hp: 50,
  sp: 25,
  sex: 1,
  element: 1,
  birth: 1,
  jobId: "test-warrior",
  jobName: "測試戰士",
  specialAttackName: "測試斬",
  stats: {
    str: 11,
    smart: 11,
    life: 11,
    vit: 10,
    agi: 11,
    au: 10,
    be: 10,
    at: 11,
    mat: 11,
    df: 10,
    mdf: 10,
    hpMax: 325,
    spMax: 88,
  },
  currentStageId: "test-stage-1",
  actNum: 50,
  actNumMax: 50,
  jobProficiency: [],
  ...overrides,
})

export const makeSummary = (overrides: Partial<AdvanceSummary> = {}): AdvanceSummary => ({
  fights: 0,
  wins: 0,
  losses: 0,
  timeouts: 0,
  exp: 0,
  money: 0,
  jobExp: 0,
  levelsGained: 0,
  ...overrides,
})

export const makeContent = (overrides: Partial<ContentNames> = {}): ContentNames => ({
  monsters: [],
  stages: [],
  jobs: [],
  elements: [],
  births: [],
  sexes: [],
  ...overrides,
})
