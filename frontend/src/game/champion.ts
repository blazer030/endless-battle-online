import { describePlaybackItem, type LogEntry } from "./battleLog"
import { translateReasons } from "./reasons"
import type { BattleEvent, ChampionChallenge, ChampionView } from "./types"

const format = (value: number) => value.toLocaleString("zh-TW")
const pad = (value: number) => String(value).padStart(2, "0")

// 以玩家裝置的本地時間顯示
const dateText = (date: Date) =>
  `${date.getFullYear()}/${pad(date.getMonth() + 1)}/${pad(date.getDate())}`

export const lockText = (lockedUntil: string) => {
  const date = new Date(lockedUntil)
  return `${dateText(date)} ${pad(date.getHours())}:${pad(date.getMinutes())} 之前無法挑戰冠軍`
}

export type ChampionTitle = { title: string; subtitle: string | null; lock: string | null }

export const championTitle = (view: ChampionView, now: Date): ChampionTitle => ({
  title: `${view.name} Lv ${view.level}`,
  subtitle:
    view.isPlayer && view.crownedAt !== null
      ? `你自己（${dateText(new Date(view.crownedAt))}）`
      : null,
  lock:
    view.lockedUntil !== null && new Date(view.lockedUntil).getTime() > now.getTime()
      ? lockText(view.lockedUntil)
      : null,
})

export const championStats = (view: ChampionView): string[] => [
  `HP ${format(view.hp)}/${format(view.hpMax)}`,
  `物攻 ${format(view.at)}`,
  `魔攻 ${format(view.mat)}`,
  `物防 ${format(view.df)}`,
  `魔防 ${format(view.mdf)}`,
  `敏捷 ${format(view.agi)}`,
  `魅力 ${format(view.au)}`,
  `衛冕 ${format(view.defenses)} 次`,
  `挑戰費 ${format(view.fee)}`,
]

export const duelResultLine = (won: boolean, characterName: string) =>
  won ? `${characterName} 順利當上冠軍` : "挑戰失敗，冠軍保住了寶座。"

// 產生的型別 type 是選填，伺服器一律會帶
const isBattleEvent = (event: ChampionChallenge["events"][number]): event is BattleEvent =>
  event.type !== undefined

// 對戰事件沿用冒險戰報的敘述；決鬥沒有遭遇事件，對手一開始就是挑戰當下的冠軍。最後一行是勝負
export const duelLog = (challenge: ChampionChallenge, championName: string): LogEntry[] => {
  const opponent = () => championName
  const lines = challenge.events
    .filter(isBattleEvent)
    .flatMap((event) => describePlaybackItem({ kind: "event", event }, opponent, "champion"))
  return [
    ...lines,
    { text: duelResultLine(challenge.won, challenge.character.name), kind: "trigger" },
  ]
}

const reasonTexts: Record<string, string> = {
  championLocked: "冷卻中無法挑戰冠軍",
  notEnoughMoney: "挑戰冠軍最少需500",
  noHp: "HP 為 0，無法挑戰",
}

export const championErrorMessage = (reasons: string[], fallback: string) =>
  translateReasons(reasonTexts, reasons, fallback)
