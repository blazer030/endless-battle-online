import type { HonourExchange } from "../game/honour"
import { makeHonourItem } from "./fixtures"

// backend/content/honour-shop.json 裡用到雨林或白銀勳章的兌換（2026-11-14 抽出，內容順序）；
// 前端測試不讀後端內容檔，規格 honour-screen 的 Rainforest rack、silver rack order 用這份對照
type Row = [id: string, name: string, category: string, quantity: number, cost: [string, number][]]

const rows: Row[] = [
  ["honour-3", "洞窟鑰匙", "Key", 1, [["item-1306", 2]]],
  ["honour-17", "聚能頭巾", "Head", 1, [["item-1306", 1]]],
  ["honour-18", "斑點法帽", "Head", 1, [["item-1306", 2]]],
  ["honour-19", "光彩法帽", "Head", 1, [["item-1306", 3]]],
  ["honour-20", "波特帽", "Head", 1, [["item-1306", 4]]],
  ["honour-21", "虎力頭盔", "Head", 1, [["item-1306", 1]]],
  ["honour-22", "獅王頭盔", "Head", 1, [["item-1306", 2]]],
  ["honour-23", "荒漠頭盔", "Head", 1, [["item-1306", 3]]],
  ["honour-24", "高等騎士頭盔", "Head", 1, [["item-1306", 4]]],
  ["honour-25", "射手眼鏡", "Head", 1, [["item-1306", 1]]],
  ["honour-26", "狼族頭帶", "Head", 1, [["item-1306", 3]]],
  ["honour-27", "怒海頭帶", "Head", 1, [["item-1306", 5]]],
  ["honour-28", "輕靈頭盔", "Head", 1, [["item-1306", 7]]],
  [
    "honour-101",
    "黑曜鑰匙",
    "Key",
    1,
    [
      ["item-1306", 8],
      ["item-1307", 5],
    ],
  ],
  [
    "honour-102",
    "白陽短劍",
    "Weapon",
    1,
    [
      ["item-1304", 8],
      ["item-1306", 8],
    ],
  ],
  [
    "honour-103",
    "偏斜針",
    "Weapon",
    1,
    [
      ["item-1304", 10],
      ["item-1306", 9],
    ],
  ],
  [
    "honour-104",
    "流影套索",
    "Weapon",
    1,
    [
      ["item-1304", 11],
      ["item-1306", 10],
    ],
  ],
  [
    "honour-105",
    "無雙魔劍",
    "Weapon",
    1,
    [
      ["item-1304", 12],
      ["item-1306", 12],
    ],
  ],
  [
    "honour-106",
    "映月羽扇",
    "Weapon",
    1,
    [
      ["item-1304", 10],
      ["item-1306", 10],
    ],
  ],
  [
    "honour-107",
    "龍吼",
    "Weapon",
    1,
    [
      ["item-1304", 13],
      ["item-1306", 13],
    ],
  ],
  [
    "honour-108",
    "源法盒",
    "Weapon",
    1,
    [
      ["item-1304", 9],
      ["item-1306", 10],
    ],
  ],
  [
    "honour-109",
    "絕望魔力",
    "Weapon",
    1,
    [
      ["item-1304", 13],
      ["item-1306", 14],
    ],
  ],
  [
    "honour-110",
    "重力長槍",
    "Weapon",
    1,
    [
      ["item-1304", 12],
      ["item-1306", 11],
    ],
  ],
  ["honour-111", "神氣眼鏡", "Head", 1, [["item-1306", 1]]],
  ["honour-112", "迷惑髮帶", "Head", 1, [["item-1306", 2]]],
  ["honour-113", "破銀面罩", "Head", 1, [["item-1306", 4]]],
  ["honour-133", "90000HP回復劑", "Consumable", 10, [["item-2222", 1]]],
  ["honour-134", "110000HP回復劑", "Consumable", 10, [["item-2222", 3]]],
  ["honour-135", "魔法捕捉器", "Consumable", 5, [["item-2222", 2]]],
  ["honour-136", "1500SP藥水", "Consumable", 10, [["item-2222", 2]]],
  ["honour-137", "3000SP藥水", "Consumable", 10, [["item-2222", 4]]],
  ["honour-138", "神力秘藥LV1", "Consumable", 1, [["item-2222", 6]]],
  ["honour-139", "能量秘藥LV1", "Consumable", 1, [["item-2222", 6]]],
  ["honour-140", "穩固秘藥LV1", "Consumable", 1, [["item-2222", 6]]],
  ["honour-141", "精神秘藥LV1", "Consumable", 1, [["item-2222", 6]]],
  [
    "honour-142",
    "劣質打孔石",
    "Material",
    3,
    [
      ["item-1304", 1],
      ["item-1305", 1],
      ["item-1306", 1],
      ["item-1307", 1],
      ["item-2222", 1],
    ],
  ],
  ["honour-large-stone-box", "大魔石寶箱", "Consumable", 1, [["item-2222", 1]]],
  [
    "honour-refine-box",
    "精鍊寶盒",
    "Consumable",
    1,
    [
      ["item-1305", 1],
      ["item-1306", 1],
    ],
  ],
  ["honour-pet-egg", "寵物蛋", "Consumable", 1, [["item-2222", 3]]],
]

export const rainforestAndSilverExchanges: HonourExchange[] = rows.map(
  ([id, name, category, quantity, cost]) => ({
    id,
    kind: category === "Mercenary" ? "mercenary" : "item",
    rewardId: id,
    rewardName: name,
    quantity,
    cost: cost.map(([itemId, need]) => ({ itemId, name: itemId, quantity: need, owned: 0 })),
    affordable: false,
    item:
      category === "Mercenary"
        ? null
        : makeHonourItem({ category: category as NonNullable<HonourExchange["item"]>["category"] }),
    mercenary: null,
  }),
)
