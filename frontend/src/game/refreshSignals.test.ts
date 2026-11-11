import { describe, expect, it } from "vitest"
import type { PlaybackItem } from "./battlePlayback"
import type { BattleEvent, DropResult } from "./types"
import { refreshSignalsFor } from "./refreshSignals"

const none = { bag: false, missions: false, skills: false }

const event = (value: BattleEvent): PlaybackItem => ({ kind: "event", event: value })

const drop = (discarded: boolean): DropResult => ({
  itemId: "wolf-fang",
  name: "狼牙",
  quantity: 1,
  discarded,
})

const reward = (jobExp: number, drops: DropResult[] = []) =>
  event({ type: "reward", atSeconds: 0, exp: 1, money: 1, jobExp, drops })

describe("refreshSignalsFor", () => {
  it("沒有項目時都不重載", () => {
    expect(refreshSignalsFor([])).toEqual(none)
  })

  it("獎勵有留下的掉落時重載背包，熟練度增加時重載技能", () => {
    expect(refreshSignalsFor([reward(0, [drop(false)])])).toEqual({ ...none, bag: true })
    expect(refreshSignalsFor([reward(3)])).toEqual({ ...none, skills: true })
  })

  it("丟棄的掉落不算", () => {
    expect(refreshSignalsFor([reward(0, [drop(true)])])).toEqual(none)
  })

  it("喝藥水、捕捉與傭兵報告重載背包", () => {
    const potion = event({
      type: "potion",
      atSeconds: 0,
      itemId: "small-potion",
      name: "小藥水",
      hp: 50,
      sp: 0,
      remaining: 8,
    })
    const capture = event({
      type: "capture",
      atSeconds: 0,
      itemName: "捕獸網",
      itemBroken: true,
      capturedMonsterId: null,
      remaining: 2,
    })
    const mercenary: PlaybackItem = {
      kind: "mercenary",
      report: {
        name: "阿福",
        fights: 1,
        wins: 1,
        exp: 40,
        money: 7,
        levelsGained: 0,
        remaining: 99,
        dismissed: false,
        recent: [],
      },
    }
    expect(refreshSignalsFor([potion])).toEqual({ ...none, bag: true })
    expect(refreshSignalsFor([capture])).toEqual({ ...none, bag: true })
    expect(refreshSignalsFor([mercenary])).toEqual({ ...none, bag: true })
  })

  it("遇到 NPC 不重載任何東西，任務進度有自己的事件", () => {
    expect(
      refreshSignalsFor([
        event({ type: "npc", atSeconds: 0, monsterId: "monster-265", name: "倫魯", message: null }),
      ]),
    ).toEqual(none)
  })

  it("任務進度重載任務，升級重載技能", () => {
    const mission = event({
      type: "mission",
      atSeconds: 0,
      missionId: "m1",
      title: "清除雪精",
      monsterId: "slime",
      progress: 1,
      required: 3,
      requiresDelivery: false,
      skillTooLow: false,
      completed: false,
    })
    const levelUp = event({
      type: "levelUp",
      atSeconds: 0,
      newLevel: 2,
      growth: {
        strength: 1,
        intelligence: 0,
        life: 0,
        vitality: 0,
        agility: 0,
        charisma: 0,
        faith: 0,
      },
    })
    expect(refreshSignalsFor([mission])).toEqual({ ...none, missions: true })
    expect(refreshSignalsFor([levelUp])).toEqual({ ...none, skills: true })
  })

  it("一批裡多種事件時旗標合併", () => {
    const encounter = event({
      type: "encounter",
      atSeconds: 0,
      monsterId: "test-wolf",
      monsterHp: 60,
      monsterHpMax: 60,
      playerHp: 120,
      playerHpMax: 120,
      playerSp: 10,
      playerSpMax: 10,
      petName: null,
      petMonsterId: null,
    })
    expect(refreshSignalsFor([encounter, reward(2, [drop(false)])])).toEqual({
      bag: true,
      missions: false,
      skills: true,
    })
  })

  it("背景回來的摘要涵蓋沒逐筆播出的場次，重載背包與任務", () => {
    const summary: PlaybackItem = {
      kind: "summary",
      fights: 30,
      wins: 28,
      exp: 900,
      money: 300,
      levels: 0,
      missionKills: 0,
      petsCaptured: 0,
    }
    expect(refreshSignalsFor([summary])).toEqual({ bag: true, missions: true, skills: false })
  })
})
