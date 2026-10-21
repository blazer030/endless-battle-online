import { CatchUpThresholdSeconds, RestAfterBatchSeconds } from "../motion/timings"
import type { AdvanceResponse, BattleEvent, CharacterView } from "./types"

export type PlaybackItem =
  | { kind: "event"; event: BattleEvent }
  | { kind: "summary"; fights: number; exp: number; money: number }

interface ScheduledItem {
  item: PlaybackItem
  dueMilliseconds: number
  order: number
}

export class BattlePlayback {
  private confirmedCharacter: CharacterView
  private pending: ScheduledItem[] = []
  private timelineEnd = 0
  private nextOrder = 0

  constructor(character: CharacterView) {
    this.confirmedCharacter = character
  }

  enqueue(response: AdvanceResponse, receivedAtMilliseconds: number): void {
    this.confirmedCharacter = response.character
    if (response.events.length === 0) return

    // 產生的型別 type 是選填，伺服器一律會帶
    let events = response.events as BattleEvent[]
    const batchStart = Math.max(receivedAtMilliseconds, this.timelineEnd)
    const lengthSeconds = events[events.length - 1].atSeconds
    // 離線太久時，最後一場之前的戰鬥壓成一行摘要，只播最後一場
    if (lengthSeconds > CatchUpThresholdSeconds) {
      const lastEncounterIndex = events.findLastIndex((event) => event.type === "encounter")
      if (lastEncounterIndex > 0) {
        this.schedule(this.summarize(events.slice(0, lastEncounterIndex)), batchStart)
        const offset = events[lastEncounterIndex].atSeconds
        events = events
          .slice(lastEncounterIndex)
          .map((event) => ({ ...event, atSeconds: event.atSeconds - offset }))
      }
    }

    for (const event of events) {
      this.schedule({ kind: "event", event }, batchStart + event.atSeconds * 1000)
    }
    const last = events[events.length - 1].atSeconds
    this.timelineEnd = batchStart + (last + RestAfterBatchSeconds) * 1000
  }

  takeDue(nowMilliseconds: number): PlaybackItem[] {
    const due = this.pending
      .filter((scheduled) => scheduled.dueMilliseconds <= nowMilliseconds)
      .sort((a, b) => a.dueMilliseconds - b.dueMilliseconds || a.order - b.order)
    this.pending = this.pending.filter((scheduled) => scheduled.dueMilliseconds > nowMilliseconds)
    return due.map((scheduled) => scheduled.item)
  }

  // 顯示值 = 已確認的角色資料減去尚未播出的獎勵，數字隨戰報行逐步增加
  get displayedCharacter(): CharacterView {
    let exp = 0
    let money = 0
    for (const { item } of this.pending) {
      if (item.kind === "summary") {
        exp += item.exp
        money += item.money
      } else if (item.event.type === "reward") {
        exp += item.event.experience
        money += item.event.spiritStones
      }
    }
    return {
      ...this.confirmedCharacter,
      exp: Math.max(0, this.confirmedCharacter.exp - exp),
      money: Math.max(0, this.confirmedCharacter.money - money),
    }
  }

  private schedule(item: PlaybackItem, dueMilliseconds: number): void {
    this.pending.push({ item, dueMilliseconds, order: this.nextOrder++ })
  }

  private summarize(events: BattleEvent[]): PlaybackItem {
    let fights = 0
    let exp = 0
    let money = 0
    for (const event of events) {
      if (event.type === "encounter") fights++
      else if (event.type === "reward") {
        exp += event.experience
        money += event.spiritStones
      }
    }
    return { kind: "summary", fights, exp, money }
  }
}
