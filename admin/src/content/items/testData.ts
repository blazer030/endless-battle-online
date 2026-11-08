import type { ItemDto } from "./ItemConverter"

export const itemDto = (overrides: Partial<ItemDto> = {}): ItemDto => ({
  id: "item-2",
  name: "木刀",
  category: "Weapon",
  price: 6,
  shopLevel: 1,
  shopSellable: true,
  bound: false,
  ...overrides,
})

export const stats = (
  overrides: Partial<NonNullable<ItemDto["bonus"]>> = {},
): NonNullable<ItemDto["bonus"]> => ({
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
  maxHitPoints: 0,
  ...overrides,
})

export const items: ItemDto[] = [
  itemDto({ bonus: stats({ attack: 15 }), holes: 1, refineMax: 3 }),
  itemDto({
    id: "item-3",
    name: "鐵劍",
    shopLevel: 2,
    bonus: stats({ attack: 30 }),
    holes: 2,
    refineMax: 3,
  }),
  itemDto({
    id: "item-10",
    name: "布衣",
    category: "Body",
    bonus: stats({ defense: 5 }),
    holes: 1,
    refineMax: 3,
  }),
  itemDto({
    id: "item-223",
    name: "回復劑",
    category: "Consumable",
    consumableKind: "Supply",
    supplyHitPoints: 50,
  }),
  itemDto({ id: "item-600", name: "鐵礦", category: "Material" }),
]
