import { placeDto } from "../world/testData"
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

export const placeRecords = [
  placeDto({
    id: 1,
    name: "中央平原",
    subPlaceNames: ["香茅綠地", "綠風草地", "白沙海灣", "眠獸洞穴"],
  }),
  placeDto({ id: 2, name: "灼熱荒漠", subPlaceNames: ["黃沙古道", "枯骨谷"] }),
]

// 舊路由的道具紀錄，只讀 id 與名稱
export const itemRecords = [
  { id: "item-204", name: "錢包" },
  { id: "item-660", name: "黏液" },
  { id: "item-1", name: "布衣" },
  { id: "item-2", name: "布衣" },
]

export const skillRecords = [
  { id: "skill-1", name: "爆氣", maxLevel: 5 },
  { id: "skill-66", name: "水彈", maxLevel: 3 },
]

export const missionRecords = [
  { id: "mission-2", title: "打敗米列", daily: false },
  { id: "mission-222", title: "[每日]治安維持任務", daily: true },
]
