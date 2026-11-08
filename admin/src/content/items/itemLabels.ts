import type { ConsumableKind, ElixirKind, ItemCategory, ItemStatKey } from "./ItemVo"

export const itemCategoryLabels: Record<ItemCategory, string> = {
  Weapon: "武器",
  Head: "頭部",
  Body: "身體",
  Hand: "手部",
  Foot: "腳部",
  Accessory: "飾品",
  Consumable: "消耗品",
  Stone: "魔石",
  RefineStone: "精鍊石",
  Material: "素材",
  Medal: "勳章",
  Key: "鑰匙",
}

export const consumableKindLabels: Record<ConsumableKind, string> = {
  Supply: "補給",
  StatPotion: "能力藥水",
  Elixir: "密藥",
  Capture: "捕捉道具",
  ExperienceCapsule: "經驗膠囊",
  BagExpansion: "背包擴充",
  DepotExpansion: "倉庫擴充",
  ProficiencyBook: "熟練手冊",
  StoneBox: "魔石寶箱",
  RefineBox: "精鍊石寶箱",
  PetEgg: "寵物蛋",
}

// 依 ConsumableKind 的註解；沒有列的種類，數值的意義還沒有文件
export const amountMeanings: Partial<Record<ConsumableKind, string>> = {
  Capture: "能力倍率",
  PetEgg: "能力倍率",
  StoneBox: "抽出的魔石顆數",
  RefineBox: "精鍊石等級",
  ProficiencyBook: "技能點",
  ExperienceCapsule: "經驗值",
}

export const elixirKindLabels: Record<ElixirKind, string> = {
  Exp: "經驗",
  JobExp: "職業經驗",
  Attack: "攻擊",
  Magic: "魔法",
  Defense: "防禦",
  MagicDefense: "魔防",
}

export const itemStatLabels: Record<ItemStatKey, string> = {
  strength: "力量",
  intelligence: "智力",
  life: "生命",
  vitality: "體質",
  agility: "敏捷",
  charisma: "魅力",
  faith: "信仰",
  attack: "攻擊",
  magicAttack: "魔攻",
  defense: "防禦",
  magicDefense: "魔防",
  maxHitPoints: "HP 上限",
}
