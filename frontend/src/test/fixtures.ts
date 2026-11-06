import type {
  AdvanceSummary,
  CharacterView,
  ContentNames,
  HonourShop,
  InventoryItem,
  InventoryView,
  MercenaryOverview,
  MissionBoard,
  MissionView,
  PetsView,
  PetView,
  ShopItem,
  StatBonusView,
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
  bank: 0,
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
  jobProficiency: [],
  settings: {
    attack: null,
    potionHpPercent: 15,
    potionSpPercent: 100,
    restHpPercent: 15,
    restSpPercent: 15,
    escapeHpPercent: 0,
  },
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
  missionKills: 0,
  petsCaptured: 0,
  skillsCast: 0,
  drops: 0,
  potionsUsed: 0,
  escapes: 0,
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
    keys: 0,
    passExpiresAt: null,
    ...overrides,
  }
  return place
}

// 中央大陸、魔法王國、禁區各取幾個地區；黑曜神廟當成一般地區，方便測沒有子地區的移動
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

export const emptyBonus: StatBonusView = {
  str: 0,
  smart: 0,
  life: 0,
  vit: 0,
  agi: 0,
  au: 0,
  be: 0,
  at: 0,
  mat: 0,
  df: 0,
  mdf: 0,
  hpMax: 0,
}

export const makeItem = (overrides: Partial<InventoryItem> = {}): InventoryItem => ({
  inventoryId: 1,
  itemId: "test-sword",
  name: "測試劍",
  category: "Weapon",
  consumable: "None",
  quantity: 1,
  bonus: { ...emptyBonus, at: 8 },
  requires: { str: 0, smart: 0, agi: 0, au: 0 },
  jobId: null,
  supplyHp: 0,
  supplySp: 0,
  amount: 0,
  refineLevel: 0,
  refineMax: 4,
  refineBonus: emptyBonus,
  holes: 0,
  sockets: [],
  price: 200,
  bound: false,
  refineStoneLevel: null,
  enhanced: false,
  ...overrides,
})

// 十一個分類都列出，道具依分類放進對應的格子；容量同後端預設 15
export const makeInventory = (
  items: InventoryItem[] = [],
  overrides: Partial<InventoryView> = {},
): InventoryView => ({
  bag: (
    [
      "Weapon",
      "Head",
      "Body",
      "Hand",
      "Foot",
      "Accessory",
      "Consumable",
      "Stone",
      "RefineStone",
      "Material",
      "Medal",
    ] as const
  ).map((category) => ({
    category,
    capacity: 15,
    items: items.filter((item) => item.category === category),
  })),
  equipment: [],
  bonus: emptyBonus,
  ...overrides,
})

export const makeShopItem = (overrides: Partial<ShopItem> = {}): ShopItem => ({
  id: "test-sword",
  name: "測試劍",
  category: "Weapon",
  price: 200,
  bonus: { ...emptyBonus, at: 8 },
  requires: { str: 10, smart: 0, agi: 0, au: 0 },
  holes: 0,
  refineMax: 4,
  ...overrides,
})

// 中央平原的擊殺任務：草原史萊姆 3/8
export const makeMission = (overrides: Partial<MissionView> = {}): MissionView => ({
  id: "test-hunt",
  giver: "村長",
  title: "綠地上的黏液",
  description: "綠地上的史萊姆越來越多了。",
  level: 1,
  placeId: 1,
  placeName: "中央平原",
  daily: false,
  kill: {
    monsterId: "grass-slime",
    monsterName: "草原史萊姆",
    placeId: 1,
    placeName: "中央平原",
    count: 8,
    progress: 3,
  },
  deliver: [],
  requirements: {
    jobId: null,
    sex: null,
    birth: null,
    prerequisiteId: null,
    prerequisiteTitle: null,
  },
  rewards: { exp: 190, money: 50, items: [], skill: null, pet: null },
  ready: false,
  expiresAt: null,
  deliverPet: null,
  ...overrides,
})

export const makeMissionBoard = (overrides: Partial<MissionBoard> = {}): MissionBoard => ({
  available: [],
  active: [],
  completed: [],
  activeLimit: 3,
  nextDailyReset: "2026-10-23T01:00:00Z",
  ...overrides,
})

export const makePet = (overrides: Partial<PetView> = {}): PetView => ({
  id: 1,
  monsterId: "grass-slime",
  species: "草原史萊姆",
  name: "小黏",
  personality: "Aggressive",
  attack: 12,
  magic: 8,
  defense: 5,
  spirit: 1,
  satiety: 80,
  intimacy: 3,
  carried: false,
  age: 2,
  nextFeedAt: "2026-10-21T00:00:00Z",
  activationPercent: 1.2,
  ...overrides,
})

// WOG 的八種果實，順序同後端 PetCare.Foods
export const makePets = (pets: PetView[] = [], overrides: Partial<PetsView> = {}): PetsView => ({
  pets,
  maxPets: 3,
  foods: [
    { id: "fruit-vigor-small", name: "初級活力果實", stat: "Attack", amount: 2, cost: 20 },
    { id: "fruit-aroma-small", name: "初級香氣果實", stat: "Magic", amount: 2, cost: 20 },
    { id: "fruit-sweet-small", name: "初級甜味果實", stat: "Defense", amount: 2, cost: 20 },
    { id: "fruit-wild-small", name: "初級野性果實", stat: "Spirit", amount: 2, cost: 20 },
    { id: "fruit-vigor-medium", name: "中級活力果實", stat: "Attack", amount: 4, cost: 50 },
    { id: "fruit-aroma-medium", name: "中級香氣果實", stat: "Magic", amount: 4, cost: 50 },
    { id: "fruit-sweet-medium", name: "中級甜味果實", stat: "Defense", amount: 4, cost: 50 },
    { id: "fruit-wild-medium", name: "中級野性果實", stat: "Spirit", amount: 4, cost: 50 },
  ],
  ...overrides,
})

// F 級可直接僱用，A 級只能在勳章所兌換
export const makeMercenaryOverview = (
  overrides: Partial<MercenaryOverview> = {},
): MercenaryOverview => ({
  catalogue: [
    {
      id: "mercenary-f",
      name: "F級傭兵",
      price: 3000,
      fights: 100,
      moneyPercent: 30,
      dropPercent: 30,
      expPercent: 65,
      deathCost: 5,
    },
    {
      id: "mercenary-a",
      name: "A級傭兵",
      price: null,
      fights: 500,
      moneyPercent: 80,
      dropPercent: 80,
      expPercent: 90,
      deathCost: 1,
    },
  ],
  contract: null,
  ...overrides,
})

// 持有大陸勳章 2、白銀勳章 0：魔石換得起，B 級傭兵換不起
export const makeHonourShop = (overrides: Partial<HonourShop> = {}): HonourShop => ({
  medals: [
    { itemId: "silver-medal", name: "白銀勳章", owned: 0 },
    { itemId: "continent-medal", name: "大陸勳章", owned: 2 },
    { itemId: "kingdom-medal", name: "王國勳章", owned: 0 },
    { itemId: "rainforest-medal", name: "雨林勳章", owned: 0 },
    { itemId: "royal-city-medal", name: "王城勳章", owned: 0 },
  ],
  exchanges: [
    {
      id: "honour-mercenary-b",
      kind: "mercenary",
      rewardId: "mercenary-b",
      rewardName: "B級傭兵",
      quantity: 1,
      cost: [
        { itemId: "continent-medal", name: "大陸勳章", quantity: 3, owned: 2 },
        { itemId: "kingdom-medal", name: "王國勳章", quantity: 3, owned: 0 },
      ],
      affordable: false,
    },
    {
      id: "honour-attack-stone",
      kind: "item",
      rewardId: "attack-stone",
      rewardName: "攻擊魔石",
      quantity: 1,
      cost: [{ itemId: "continent-medal", name: "大陸勳章", quantity: 2, owned: 2 }],
      affordable: true,
    },
  ],
  ...overrides,
})
