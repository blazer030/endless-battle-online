export const contentElements = ["None", "Earth", "Water", "Fire", "Wood", "Wind", "Poison"] as const

export type ContentElement = (typeof contentElements)[number]

export const elementLabels: Record<ContentElement, string> = {
  None: "無",
  Earth: "地",
  Water: "水",
  Fire: "火",
  Wood: "木",
  Wind: "風",
  Poison: "毒",
}
