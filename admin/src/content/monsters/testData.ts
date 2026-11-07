import type { MonsterDto } from "./MonsterConverter"

export const monsterDto = (overrides: Partial<MonsterDto> = {}): MonsterDto => ({
  id: "monster-1",
  name: "史萊姆",
  level: 2,
  hitPoints: 32,
  skillPoints: 100,
  attack: 19,
  magicAttack: 15,
  defense: 8,
  magicDefense: 5,
  strength: 12,
  intelligence: 10,
  vitality: 6,
  agility: 2,
  charisma: 10,
  faith: 10,
  element: "Wind",
  jobExperience: 1,
  encounterRate: 0,
  placeId: 1,
  subPlaceIndexes: [1],
  drops: [{ itemId: "item-204", dropRarity: 90 }],
  skills: [{ skillId: "skill-1", level: 2, activationRate: 10 }],
  exclusiveMissionId: null,
  isNpc: false,
  npcMessage: null,
  ...overrides,
})

// 舊路由的地區與道具、技能紀錄，只讀 id 與名稱等選單需要的欄位
export const placeRecords = [
  { id: 1, name: "中央平原", subPlaces: ["香茅綠地", "綠風草地", "白沙海灣", "眠獸洞穴"] },
  { id: 2, name: "灼熱荒漠", subPlaces: ["黃沙古道", "枯骨谷"] },
]
