import type {
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
} from "../game/types"
import type { WorldAreaVo, WorldPlace } from "../game/world"
import type { AdvanceSummary } from "../game/battleEvents"
import type { CharacterVo } from "../game/character"
import { CreationOptionsVo } from "../game/creationOptions"
import { JobVo } from "../game/jobBook"
import type { JobChoiceVo } from "../game/skillOverview"

export const makeCharacter = (overrides: Partial<CharacterVo> = {}): CharacterVo => ({
  name: "測試",
  level: 1,
  experience: 0,
  nextLevelExperience: 1000,
  money: 0,
  bank: 0,
  healthPoints: 50,
  manaPoints: 25,
  sex: 1,
  element: 1,
  birth: 1,
  jobId: "test-warrior",
  jobName: "測試戰士",
  stats: {
    strength: 11,
    intelligence: 11,
    life: 11,
    vitality: 10,
    agility: 11,
    charisma: 10,
    faith: 10,
    attack: 11,
    magicAttack: 11,
    defense: 10,
    magicDefense: 10,
    maxHealthPoints: 325,
    maxManaPoints: 88,
  },
  baseStats: {
    strength: 11,
    intelligence: 11,
    life: 11,
    vitality: 10,
    agility: 11,
    charisma: 10,
    faith: 10,
  },
  placeId: 1,
  placeName: "中央平原",
  subPlace: 1,
  subPlaceName: "香茅綠地",
  areaName: "中央大陸",
  jobProficiency: [],
  settings: {
    attack: null,
    potionHealthPointsPercent: 15,
    potionManaPointsPercent: 100,
    restHealthPointsPercent: 15,
    restManaPointsPercent: 15,
    escapeHealthPointsPercent: 0,
  },
  ...overrides,
})

export const makeSummary = (overrides: Partial<AdvanceSummary> = {}): AdvanceSummary => ({
  fights: 0,
  wins: 0,
  losses: 0,
  timeouts: 0,
  experience: 0,
  money: 0,
  jobExperience: 0,
  levelsGained: 0,
  missionKills: 0,
  petsCaptured: 0,
  skillsCast: 0,
  drops: 0,
  potionsUsed: 0,
  escapes: 0,
  ...overrides,
})

// 轉職頁的職業（/api/jobs）
export const makeJobChoice = (overrides: Partial<JobChoiceVo> = {}): JobChoiceVo => ({
  id: "warrior",
  name: "戰士",
  tier: 1,
  parentId: null,
  parentName: null,
  unlockLevel: 1,
  proficiencyRequired: 0,
  statRequirements: [],
  growth: {
    strength: { minimum: 1, maximum: 2 },
    intelligence: { minimum: 0, maximum: 1 },
    life: { minimum: 1, maximum: 1 },
    vitality: { minimum: 1, maximum: 2 },
    agility: { minimum: 0, maximum: 1 },
    charisma: { minimum: 0, maximum: 1 },
    faith: { minimum: 0, maximum: 1 },
  },
  current: false,
  proficiencyPercent: 0,
  eligible: true,
  reasons: [],
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
    recommended: { minimum: 1, maximum: 10 },
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
export const makeWorld = (): WorldAreaVo[] => [
  {
    id: "central",
    name: "中央大陸",
    places: [
      makePlace({ id: 1, name: "中央平原", current: true, recommendation: "Recommended" }),
      makePlace({
        id: 3,
        name: "黑暗沼澤",
        recommended: { minimum: 10, maximum: 20 },
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
        recommended: { minimum: 6, maximum: 15 },
        recommendation: "TooEasy",
      }),
    ],
  },
  {
    id: "forbidden",
    name: "禁區",
    places: [
      makePlace({
        id: 17,
        name: "廢棄洞窟",
        recommended: { minimum: 60, maximum: 80 },
        restricted: true,
      }),
      makePlace({ id: 20, name: "黑曜神廟", recommended: { minimum: 130, maximum: 160 } }),
    ],
  },
]

export const emptyBonus: StatBonusView = {
  strength: 0,
  intelligence: 0,
  life: 0,
  vitality: 0,
  agility: 0,
  charisma: 0,
  faith: 0,
  attack: 0,
  magicAttack: 0,
  defense: 0,
  magicDefense: 0,
  maxHealthPoints: 0,
}

export const makeItem = (overrides: Partial<InventoryItem> = {}): InventoryItem => ({
  inventoryId: 1,
  itemId: "test-sword",
  name: "測試劍",
  category: "Weapon",
  consumable: "None",
  quantity: 1,
  bonus: { ...emptyBonus, attack: 8 },
  requires: { strength: 0, intelligence: 0, agility: 0, charisma: 0 },
  jobId: null,
  supplyHealthPoints: 0,
  supplyManaPoints: 0,
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
  weaponSkill: null,
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
  bonus: { ...emptyBonus, attack: 8 },
  requires: { strength: 10, intelligence: 0, agility: 0, charisma: 0 },
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
  kills: [
    {
      monsterId: "grass-slime",
      monsterName: "草原史萊姆",
      placeId: 1,
      placeName: "中央平原",
      count: 8,
      progress: 3,
    },
  ],
  deliver: [],
  requirements: {
    jobId: null,
    sex: null,
    birth: null,
    prerequisiteId: null,
    prerequisiteTitle: null,
  },
  rewards: { exp: 190, money: 50, items: [], skill: null, pet: null },
  requiredSkill: null,
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

// 公開 API 的初階職業與建角選項（轉換後的 VO）
export const makePublicJob = (overrides: Partial<Parameters<typeof JobVo.create>[0]> = {}) =>
  JobVo.create({
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
        description: "爆發體內的鬥氣，接下來幾回合物攻提升。",
        maxLevel: 10,
        prerequisite: null,
        levels: Array.from({ length: 10 }, (_, index) => ({
          level: index + 1,
          summary: `爆氣 Lv${index + 1} 的效果`,
        })),
      },
      {
        id: "skill-61",
        name: "防禦心得",
        description: "看準來勢架開攻擊，減輕受到的物理傷害。",
        maxLevel: 5,
        prerequisite: { skillId: "skill-1", name: "爆氣", level: 3 },
        levels: Array.from({ length: 5 }, (_, index) => ({
          level: index + 1,
          summary: `防禦心得 Lv${index + 1} 的效果`,
        })),
      },
    ],
    ...overrides,
  })

export const makePublicJobs = () => [
  makePublicJob(),
  makePublicJob({ id: "job-7", name: "術士", skills: [] }),
  makePublicJob({ id: "job-8", name: "盜賊", skills: [] }),
  makePublicJob({ id: "job-9", name: "樂手", skills: [] }),
  makePublicJob({ id: "job-10", name: "狂戰士", tier: 2, parentId: "job-6", skills: [] }),
]

export const makeCreationOptions = () =>
  CreationOptionsVo.create({
    sexes: [
      { id: 1, name: "男" },
      { id: 2, name: "女" },
    ],
    elements: [
      { id: 1, name: "地", strongAgainst: 2, weakAgainst: 6 },
      { id: 2, name: "水", strongAgainst: 3, weakAgainst: 1 },
      { id: 3, name: "火", strongAgainst: 4, weakAgainst: 2 },
      { id: 4, name: "木", strongAgainst: 5, weakAgainst: 3 },
      { id: 5, name: "風", strongAgainst: 6, weakAgainst: 4 },
      { id: 6, name: "毒", strongAgainst: 1, weakAgainst: 5 },
    ],
    births: [
      { id: 1, name: "中央大陸" },
      { id: 2, name: "魔法王國" },
      { id: 3, name: "熱帶雨林" },
      { id: 4, name: "末日王城" },
    ],
  })
