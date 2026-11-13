import { formatNumber } from "./format"
import type { MercenaryCatalogueEntry, MercenaryContract } from "./mercenaries"
import type { Pet, PetFood, PetFoodStat } from "./pets"
import type { WorldAreaVo } from "./world"

// 同伴畫面（設計板 CompanionsFit）的純模型：管理人台詞、狀態小籤、量條比例、版位

export type Keeper = "mira" | "barton"

export type KeeperEvent =
  | { kind: "carried" | "stored" | "petRenamed" | "released"; name: string }
  | { kind: "petRejected"; reason: string }
  | { kind: "petFailed" | "stableLoadFailed" }
  | { kind: "hired"; name: string }
  | { kind: "deployed"; name: string; place: string }
  | { kind: "rested" | "dismissed" | "mercenaryRenamed"; name: string }
  | { kind: "mercenaryRejected"; reason: string; contractName?: string }
  | { kind: "mercenaryFailed" | "hallLoadFailed" }

export type KeeperLine = { keeper: Keeper; text: string; failed: boolean }

const MiraFailed = "咦，剛剛沒弄好……再試一次吧？"
const BartonFailed = "嘖，文書出了點差錯，再來一次。"

const miraRejections: Record<string, string> = {
  overOwner: "再吃下去，牠就要比你還強了呢。",
  notEnoughMoney: "哎呀，這顆果實的錢好像不太夠。",
  nameInvalid: "這名字牠好像記不住呢。",
  full: "牠已經吃得飽飽的了。",
  tooSoon: "慢慢來，讓牠先嚼完這顆。",
}

const bartonRejection = (reason: string, contractName: string | undefined): string | undefined => {
  switch (reason) {
    case "notEnoughMoney":
      return "錢不夠，這張契約簽不了。"
    case "mercenaryHired":
      return `一次只接一份契約，先把${contractName ?? "那位"}的處理掉。`
    case "honourOnly":
      return "那兩位得去勳章所換，我這兒不賣。"
    case "placeRestricted":
      return "那裡是禁區，我的人不去送死。"
    case "nameInvalid":
      return "這名字名冊上寫不了，換一個。"
    default:
      return undefined
  }
}

const mira = (text: string, failed = false): KeeperLine => ({ keeper: "mira", text, failed })
const barton = (text: string, failed = false): KeeperLine => ({ keeper: "barton", text, failed })

// 管理人在木台標頭（或羊皮紙裡）說的一句；伺服器不認得的原因與連線錯誤都用「沒弄好」那句
export function keeperLine(event: KeeperEvent): KeeperLine {
  switch (event.kind) {
    case "carried":
      return mira(`${event.name}會跟著你出門囉！`)
    case "stored":
      return mira(`${event.name}回欄裡休息囉。`)
    case "petRenamed":
      return mira(`「${event.name}」，牠很喜歡喔！`)
    case "released":
      return mira(`${event.name}回到野外去了，要保重喔。`)
    case "petRejected":
      return mira(miraRejections[event.reason] ?? MiraFailed, true)
    case "petFailed":
      return mira(MiraFailed, true)
    case "stableLoadFailed":
      return mira("咦，欄裡的名簿一時找不到……", true)
    case "hired":
      return barton(`契約簽好了，${event.name}歸你使喚。`)
    case "deployed":
      return barton(`${event.name}往${event.place}出發了。`)
    case "rested":
      return barton(`${event.name}回來歇口氣了。`)
    case "dismissed":
      return barton(`${event.name}的契約，我收回來了。`)
    case "mercenaryRenamed":
      return barton(`${event.name}？行，名冊上改好了。`)
    case "mercenaryRejected":
      return barton(bartonRejection(event.reason, event.contractName) ?? BartonFailed, true)
    case "mercenaryFailed":
      return barton(BartonFailed, true)
    case "hallLoadFailed":
      return barton("嘖，名冊一時翻不到……", true)
  }
}

export const MiraLoadFailedGreeting = "咦，獸欄的名簿一下子找不到了……"
export const BartonLoadFailedGreeting = "嘖，名冊一時翻不到……"

const MaxPets = 3

export function miraGreeting(pets: readonly Pet[]): string {
  if (pets.length === 0) return "獸欄空蕩蕩的……抓到寵物就帶來給我照顧吧！"
  const carried = pets.find((pet) => pet.carried)
  return carried === undefined
    ? "孩子們都在欄裡等你呢，今天要帶誰出門？"
    : `${carried.name}今天也很有精神喔！要來看看牠們嗎？`
}

export function stableChip(pets: readonly Pet[]): string {
  const carried = pets.find((pet) => pet.carried)
  return `寵物 ${pets.length} / ${MaxPets}${carried === undefined ? "" : `・${carried.name} 攜帶中`}`
}

export function bartonGreeting(contract: MercenaryContract | null): string {
  if (contract === null) return "想找人手？名冊上的傢伙個個都能打。"
  return contract.fighting
    ? `${contract.name}還在${contract.placeName ?? "外頭"}幹活，契約我幫你顧著。`
    : `${contract.name}在後頭磨刀，隨時可以再派出去。`
}

export const hallChip = (contract: MercenaryContract | null) =>
  contract === null ? "沒有契約" : `${contract.name}・${contract.fighting ? "戰鬥中" : "休息中"}`

const PanoramaHeight = 844

// 同伴畫面從城門口的「同伴」紙籤展開：圓心固定在舞台 (307, 317)
export const companionsRevealOrigin = () => ({ x: 307, y: 317 })

// 獸欄、傭兵所的木台上緣：844 高時 330，每矮 1 上移 0.45
export const companionsCounterTop = (stageHeight: number) =>
  Math.round(330 - (PanoramaHeight - stageHeight) * 0.45)

const ratio = (value: number, full: number) => Math.min(1, Math.max(0, value / full))

// 飽腹以 100 為滿、親密以 255 為滿（親密沒有上限，超過就停在滿）、出擊率以 35% 為滿
export const satietyRatio = (satiety: number) => ratio(satiety, 100)
export const intimacyRatio = (intimacy: number) => ratio(intimacy, 255)
export const activationRatio = (percent: number) => ratio(percent, 35)

// 名冊便條上的價格：滿一萬寫「n萬」
export const shortPrice = (price: number) =>
  price >= 10_000 ? `${formatNumber(price / 10_000)}萬` : formatNumber(price)

const statLabels: Record<PetFoodStat, string> = {
  Attack: "AT",
  Magic: "MT",
  Defense: "DEF",
  Spirit: "出擊值",
}

const statOf = (pet: Pet, stat: PetFoodStat) => {
  switch (stat) {
    case "Attack":
      return pet.attack
    case "Magic":
      return pet.magic
    case "Defense":
      return pet.defense
    case "Spirit":
      return pet.spirit
  }
}

export const fruitEffect = (food: PetFood) => `${statLabels[food.stat]}+${food.amount}`

// 餵食成功時從柵欄上浮起的字：加了哪項能力、親密加多少（由餵食前後的寵物算出）
export const feedEffect = (before: Pet, after: Pet, food: PetFood) =>
  `${statLabels[food.stat]} +${statOf(after, food.stat) - statOf(before, food.stat)}\u3000♥ +${after.intimacy - before.intimacy}`

// 果實停用：冷卻中、吃飽（飽腹 ≥ 100）、錢不夠；「不超過主人」交給伺服器
export const fruitDisabled = (pet: Pet, food: PetFood, money: number, now: Date) =>
  now.getTime() < pet.nextFeedAt.getTime() || pet.satiety >= 100 || money < food.cost

export const canHire = (
  entry: MercenaryCatalogueEntry,
  contract: MercenaryContract | null,
  money: number,
) => entry.price !== null && contract === null && money >= entry.price

export type DeployPlace = { id: number; name: string; level: string }
export type DeployGroup = { name: string; places: DeployPlace[] }

// 派遣清單：依大陸分組、排除禁區，附建議等級
export const deployGroups = (areas: readonly WorldAreaVo[]): DeployGroup[] =>
  areas
    .map((area) => ({
      name: area.name,
      places: area.places
        .filter((place) => !place.restricted)
        .map((place) => ({
          id: place.id,
          name: place.name,
          level: `Lv ${place.recommended.minimum}–${place.recommended.maximum}`,
        })),
    }))
    .filter((group) => group.places.length > 0)
