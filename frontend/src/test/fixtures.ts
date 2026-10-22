import type {
  AdvanceSummary,
  CharacterView,
  ContentNames,
  WorldArea,
  WorldPlace,
} from "../game/types"

type JobContent = ContentNames["jobs"][number]

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
  baseStats: { str: 11, smart: 11, life: 11, vit: 10, agi: 11, au: 10, be: 10 },
  placeId: 1,
  placeName: "中央平原",
  subPlace: 1,
  subPlaceName: "香茅綠地",
  areaName: "中央大陸",
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
  skillsCast: 0,
  drops: 0,
  potionsUsed: 0,
  ...overrides,
})

export const makeContent = (overrides: Partial<ContentNames> = {}): ContentNames => ({
  monsters: [],
  jobs: [],
  elements: [],
  births: [],
  sexes: [],
  ...overrides,
})

export const makeJob = (overrides: Partial<JobContent> = {}): JobContent => ({
  id: "warrior",
  name: "戰士",
  tier: 1,
  parentId: null,
  unlockLevel: 1,
  proficiencyRequired: 0,
  statRequirements: [],
  growth: {
    str: [1, 2],
    smart: [0, 1],
    life: [1, 1],
    vit: [1, 2],
    agi: [0, 1],
    au: [0, 1],
    be: [0, 1],
  },
  ...overrides,
})

const subPlaceNames: Record<number, string[]> = {
  1: ["香茅綠地", "綠風草地", "白沙海灣", "眠獸洞穴"],
  2: ["燈影隧道", "晶礦源地", "狹窄地道", "地底空洞"],
  3: ["巨大沼澤", "腐植草原", "狂火祭壇", "狩者墳場"],
  17: ["幽暗礦坑", "濕漉岩壁", "魔怪階梯", "魔實驗場"],
}

export const makePlace = (overrides: Partial<WorldPlace> & Pick<WorldPlace, "id" | "name">) => {
  const place: WorldPlace = {
    subPlaces: (subPlaceNames[overrides.id] ?? []).map((name, index) => ({
      index: index + 1,
      name,
    })),
    recommended: { min: 1, max: 10 },
    restricted: false,
    current: false,
    recommendation: "None",
    ...overrides,
  }
  return place
}

// 中央大陸、魔法王國、禁區各取幾個地區；黑曜神廟當成已取得鑰匙，方便測沒有子地區的移動
export const makeWorld = (): WorldArea[] => [
  {
    id: "central",
    name: "中央大陸",
    places: [
      makePlace({ id: 1, name: "中央平原", current: true, recommendation: "Recommended" }),
      makePlace({
        id: 3,
        name: "黑暗沼澤",
        recommended: { min: 10, max: 20 },
        recommendation: "TooHard",
      }),
    ],
  },
  {
    id: "magic",
    name: "魔法王國",
    places: [
      makePlace({
        id: 2,
        name: "試鍊洞窟",
        recommended: { min: 6, max: 15 },
        recommendation: "TooEasy",
      }),
    ],
  },
  {
    id: "forbidden",
    name: "禁區",
    places: [
      makePlace({ id: 17, name: "廢棄洞窟", recommended: { min: 60, max: 80 }, restricted: true }),
      makePlace({ id: 20, name: "黑曜神廟", recommended: { min: 130, max: 160 } }),
    ],
  },
]
