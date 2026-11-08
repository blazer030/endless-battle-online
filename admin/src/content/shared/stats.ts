// 七項能力：key 是物件欄位名稱（成長、加成），enumValue 是列舉值（能力需求）
export const contentStats = [
  { key: "strength", enumValue: "Strength", label: "力量" },
  { key: "intelligence", enumValue: "Intelligence", label: "智力" },
  { key: "life", enumValue: "Life", label: "生命" },
  { key: "vitality", enumValue: "Vitality", label: "體質" },
  { key: "agility", enumValue: "Agility", label: "敏捷" },
  { key: "charisma", enumValue: "Charisma", label: "魅力" },
  { key: "faith", enumValue: "Faith", label: "信仰" },
] as const

export type ContentStatKey = (typeof contentStats)[number]["key"]

export type ContentStatValue = (typeof contentStats)[number]["enumValue"]

export const contentStatValues = contentStats.map((stat) => stat.enumValue)

export const contentStatChoices = contentStats.map((stat) => ({
  value: stat.enumValue,
  label: stat.label,
}))
