import { RestAfterBatchSeconds } from "../motion/timings"
import type {
  AdvanceResponse,
  AdvanceSummary,
  BattleEvent,
  CharacterView,
  MercenaryReport,
} from "./types"

export type PlaybackItem =
  | { kind: "event"; event: BattleEvent }
  // 傭兵在角色的冒險之前結算，排在該批最前面
  | { kind: "mercenary"; report: MercenaryReport }
  | {
      kind: "summary"
      fights: number
      wins: number
      exp: number
      money: number
      levels: number
      missionKills: number
      petsCaptured: number
    }

// 項目播出時角色數值的增量；尚未播出的增量從已確認的角色資料扣回
type Gain = { exp: number; money: number; levels: number }

interface ScheduledItem {
  item: PlaybackItem
  gain: Gain
  dueMilliseconds: number
  order: number
}

// 升級播出前，等級與經驗條停在升級前的值，期間播出的經驗往上加
type LevelBaseline = { exp: number; nextExp: number; gainedExp: number }

const noGain: Gain = { exp: 0, money: 0, levels: 0 }

const gainOf = (event: BattleEvent): Gain => {
  switch (event.type) {
    case "reward":
      return { exp: event.exp, money: event.money, levels: 0 }
    case "rest":
      return { exp: 0, money: -event.cost, levels: 0 }
    case "levelUp":
      return { exp: 0, money: 0, levels: 1 }
    default:
      return noGain
  }
}

const sumGains = (gains: Gain[]): Gain =>
  gains.reduce(
    (total, gain) => ({
      exp: total.exp + gain.exp,
      money: total.money + gain.money,
      levels: total.levels + gain.levels,
    }),
    noGain,
  )

export class BattlePlayback {
  private confirmedCharacter: CharacterView
  private pending: ScheduledItem[] = []
  private levelBaseline: LevelBaseline | null = null
  private timelineEnd = 0
  private nextOrder = 0

  constructor(character: CharacterView) {
    this.confirmedCharacter = character
  }

  enqueue(response: AdvanceResponse, receivedAtMilliseconds: number): void {
    const before = this.displayedCharacter
    this.confirmedCharacter = response.character
    // 產生的型別 type 是選填，伺服器一律會帶
    const events = response.events as BattleEvent[]
    // 伺服器只回最後一段的事件時，其餘場次由回應的摘要交代
    const showSummary = response.summary.fights > 0 && !response.eventsCoverAll
    const mercenary = response.mercenary
    if (events.length === 0 && !showSummary && mercenary === null) return

    const batchStart = Math.max(receivedAtMilliseconds, this.timelineEnd)
    const eventGains = events.map(gainOf)
    if (mercenary !== null) {
      this.schedule(
        { kind: "mercenary", report: mercenary },
        { exp: mercenary.exp, money: mercenary.money, levels: mercenary.levelsGained },
        batchStart,
      )
    }
    if (showSummary) {
      this.schedule(
        this.summarize(response.summary),
        this.skippedGain(response.summary, events),
        batchStart,
      )
    }

    // 摘要之後緊接著播最後一段，不必等到它原本的時間點
    const offset = showSummary && events.length > 0 ? events[0].atSeconds : 0
    events.forEach((event, index) => {
      const atSeconds = event.atSeconds - offset
      this.schedule(
        { kind: "event", event: offset === 0 ? event : { ...event, atSeconds } },
        eventGains[index],
        batchStart + atSeconds * 1000,
      )
    })
    const lastSeconds = events.length > 0 ? events[events.length - 1].atSeconds - offset : 0
    this.timelineEnd = batchStart + (lastSeconds + RestAfterBatchSeconds) * 1000

    if (this.levelBaseline === null && this.pendingGain().levels > 0) {
      this.levelBaseline = { exp: before.exp, nextExp: before.nextExp, gainedExp: 0 }
    }
  }

  // 戰鬥以外的操作（轉職、學技能）回傳的最新角色資料；尚未播出的增量照樣扣回
  confirm(character: CharacterView): void {
    this.confirmedCharacter = character
  }

  takeDue(nowMilliseconds: number): PlaybackItem[] {
    const due = this.pending
      .filter((scheduled) => scheduled.dueMilliseconds <= nowMilliseconds)
      .sort((a, b) => a.dueMilliseconds - b.dueMilliseconds || a.order - b.order)
    this.pending = this.pending.filter((scheduled) => scheduled.dueMilliseconds > nowMilliseconds)
    if (this.levelBaseline !== null) {
      this.levelBaseline.gainedExp += sumGains(due.map((scheduled) => scheduled.gain)).exp
      if (this.pendingGain().levels === 0) this.levelBaseline = null
    }
    return due.map((scheduled) => scheduled.item)
  }

  // 顯示值 = 已確認的角色資料減去尚未播出的增量，數字隨戰報行逐步變化
  get displayedCharacter(): CharacterView {
    const pending = this.pendingGain()
    const confirmed = this.confirmedCharacter
    const money = Math.max(0, confirmed.money - pending.money)
    // 跨等級時經驗會歸零重算，無法單純扣回，改用升級前的經驗條
    if (pending.levels > 0 && this.levelBaseline !== null) {
      const { exp, nextExp, gainedExp } = this.levelBaseline
      return {
        ...confirmed,
        level: confirmed.level - pending.levels,
        exp: Math.min(exp + gainedExp, nextExp),
        nextExp,
        money,
      }
    }
    return { ...confirmed, exp: Math.max(0, confirmed.exp - pending.exp), money }
  }

  private pendingGain(): Gain {
    return sumGains(this.pending.map((scheduled) => scheduled.gain))
  }

  private schedule(item: PlaybackItem, gain: Gain, dueMilliseconds: number): void {
    this.pending.push({ item, gain, dueMilliseconds, order: this.nextOrder++ })
  }

  private summarize(summary: AdvanceSummary): PlaybackItem {
    return {
      kind: "summary",
      fights: summary.fights,
      wins: summary.wins,
      exp: summary.exp,
      money: summary.money,
      levels: summary.levelsGained,
      missionKills: summary.missionKills,
      petsCaptured: summary.petsCaptured,
    }
  }

  // 摘要涵蓋整段的獎勵與升級，扣掉之後還會逐筆播出的部分，避免重複計算
  private skippedGain(summary: AdvanceSummary, shownEvents: BattleEvent[]): Gain {
    const shown = sumGains(
      shownEvents
        .filter((event) => event.type === "reward" || event.type === "levelUp")
        .map(gainOf),
    )
    return {
      exp: summary.exp - shown.exp,
      money: summary.money - shown.money,
      levels: summary.levelsGained - shown.levels,
    }
  }
}
