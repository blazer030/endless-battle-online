import { describe, expect, test } from "vitest"
import { createMonsterNameResolver, describePlaybackItem } from "./battleLog"
import type { LogLineKind } from "./battleLog"
import type { PlaybackItem } from "./battlePlayback"
import { makeContent } from "../test/fixtures"
import type { BattleEvent } from "./types"

type DistributiveOmit<T, K extends PropertyKey> = T extends unknown ? Omit<T, K> : never

const resolver = createMonsterNameResolver(
  makeContent({ monsters: [{ id: "slime", name: "墨靈" }] }),
)

const event = (e: DistributiveOmit<BattleEvent, "atSeconds">): PlaybackItem => ({
  kind: "event",
  event: { ...e, atSeconds: 0 } as BattleEvent,
})

const cases: [string, PlaybackItem, string, LogLineKind][] = [
  [
    "encounter",
    event({ type: "encounter", monsterId: "slime", monsterMaxHealth: 10, playerMaxHealth: 20 }),
    "墨靈現身。",
    "normal",
  ],
  [
    "player attack",
    event({
      type: "attack",
      attacker: "Player",
      damage: 1234,
      isCritical: false,
      targetHealthAfter: 5,
    }),
    "你運劍出招，墨靈受到 1,234 點傷害。",
    "normal",
  ],
  [
    "player critical",
    event({
      type: "attack",
      attacker: "Player",
      damage: 31207,
      isCritical: true,
      targetHealthAfter: 5,
    }),
    "你一劍穿心，會心 31,207！",
    "trigger",
  ],
  [
    "monster attack",
    event({
      type: "attack",
      attacker: "Monster",
      damage: 7,
      isCritical: false,
      targetHealthAfter: 5,
    }),
    "墨靈反擊，你受到 7 點傷害。",
    "muted",
  ],
  [
    "monster critical",
    event({
      type: "attack",
      attacker: "Monster",
      damage: 9,
      isCritical: true,
      targetHealthAfter: 5,
    }),
    "墨靈猛然重擊，你受到 9 點傷害！",
    "muted",
  ],
  ["monster defeated", event({ type: "defeat", loser: "Monster" }), "墨靈化為一縷輕煙。", "normal"],
  [
    "player defeated",
    event({ type: "defeat", loser: "Player" }),
    "你力竭倒下，調息片刻再戰。",
    "muted",
  ],
  [
    "reward",
    event({ type: "reward", experience: 12000, spiritStones: 3 }),
    "獲得經驗 12,000、金錢 3。",
    "drop",
  ],
  [
    "summary",
    { kind: "summary", fights: 1500, exp: 20, money: 4 },
    "離開期間歷練 1,500 場，獲得經驗 20、金錢 4。",
    "drop",
  ],
]

describe("describePlaybackItem", () => {
  test.each(cases)("%s", (_name, item, text, kind) => {
    expect(describePlaybackItem(item, resolver, "slime")).toEqual({ text, kind })
  })

  test("describePlaybackItem_FallsBackToMonsterId_WhenNameUnknown", () => {
    const item = event({
      type: "encounter",
      monsterId: "mystery",
      monsterMaxHealth: 1,
      playerMaxHealth: 1,
    })
    expect(describePlaybackItem(item, resolver, null)?.text).toBe("mystery現身。")
  })
})
