import type { ItemCategory } from "../game/inventory"
import accessory from "../assets/placeholders/items/accessory.svg"
import body from "../assets/placeholders/items/body.svg"
import consumable from "../assets/placeholders/items/consumable.svg"
import foot from "../assets/placeholders/items/foot.svg"
import hand from "../assets/placeholders/items/hand.svg"
import head from "../assets/placeholders/items/head.svg"
import key from "../assets/placeholders/items/key.svg"
import material from "../assets/placeholders/items/material.svg"
import medal from "../assets/placeholders/items/medal.svg"
import refineStone from "../assets/placeholders/items/refine-stone.svg"
import stone from "../assets/placeholders/items/stone.svg"
import weapon from "../assets/placeholders/items/weapon.svg"

// 道具分類的線條佔位圖：道具沒有內容圖或載入失敗時用
export const categoryPlaceholders: Record<ItemCategory, string> = {
  Weapon: weapon,
  Head: head,
  Body: body,
  Hand: hand,
  Foot: foot,
  Accessory: accessory,
  Consumable: consumable,
  Stone: stone,
  RefineStone: refineStone,
  Material: material,
  Medal: medal,
  Key: key,
}
