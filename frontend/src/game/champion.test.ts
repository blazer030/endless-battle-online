import { describe, expect, it } from "vitest"
import { makeChampion, makeCharacter } from "../test/fixtures"
import {
  championErrorMessage,
  championStats,
  championTitle,
  duelLog,
  duelResultLine,
  lockText,
} from "./champion"

// 以本地時間建立，格式化結果與時區無關
const local = (year: number, month: number, day: number, hour = 0, minute = 0) =>
  new Date(year, month - 1, day, hour, minute).toISOString()

const now = new Date(local(2026, 10, 22, 12, 0))

describe("championTitle", () => {
  it("shows the seed champion's name and level", () => {
    expect(championTitle(makeChampion(), now)).toEqual({
      title: "王都冠軍 格雷德 Lv 30",
      subtitle: null,
      lock: null,
    })
  })

  it("marks the player's own seat with the crowning date", () => {
    const seat = makeChampion({
      name: "青衣",
      level: 12,
      isPlayer: true,
      crownedAt: local(2026, 10, 3, 9),
    })
    expect(championTitle(seat, now)).toMatchObject({
      title: "青衣 Lv 12",
      subtitle: "你自己（2026/10/03）",
    })
  })

  it("shows the lock only while it is in the future", () => {
    const seat = makeChampion({ lockedUntil: local(2026, 10, 29, 8, 5) })
    expect(championTitle(seat, now).lock).toBe("2026/10/29 08:05 之前無法挑戰冠軍")
    expect(championTitle(seat, new Date(local(2026, 10, 30))).lock).toBeNull()
  })
})

describe("lockText", () => {
  it("uses the WOG wording", () => {
    expect(lockText(local(2026, 1, 2, 3, 4))).toBe("2026/01/02 03:04 之前無法挑戰冠軍")
  })
})

describe("championStats", () => {
  it("lists hp, the four combat stats, agility, charm, defenses and the fee", () => {
    expect(championStats(makeChampion({ hp: 900, defenses: 2 }))).toEqual([
      "HP 900/1,400",
      "物攻 180",
      "魔攻 25",
      "物防 150",
      "魔防 40",
      "敏捷 30",
      "魅力 25",
      "衛冕 2 次",
      "挑戰費 500",
    ])
  })
})

describe("duelResultLine", () => {
  it("crowns the winner or keeps the champion", () => {
    expect(duelResultLine(true, "青衣")).toBe("青衣 順利當上冠軍")
    expect(duelResultLine(false, "青衣")).toBe("挑戰失敗，冠軍保住了寶座。")
  })
})

describe("championErrorMessage", () => {
  it("maps the champion codes with the champion's money wording", () => {
    expect(championErrorMessage(["championLocked", "notEnoughMoney", "noHp"], "x")).toBe(
      "冷卻中無法挑戰冠軍、挑戰冠軍最少需500、HP 為 0，無法挑戰",
    )
  })

  it("falls back to the detail for unknown or missing codes", () => {
    expect(championErrorMessage(["somethingNew"], "無法挑戰")).toBe("無法挑戰")
    expect(championErrorMessage([], "無法挑戰")).toBe("無法挑戰")
  })
})

describe("duelLog", () => {
  it("narrates the duel with the champion's name and ends with the result", () => {
    const lines = duelLog(
      {
        champion: makeChampion({ hp: 1300 }),
        events: [
          {
            type: "attack",
            atSeconds: 1,
            attacker: "Monster",
            kind: "Physical",
            hit: true,
            critical: false,
            stunned: false,
            damage: 120,
            elementMultiplier: 1,
            targetHpAfter: 205,
            specialAttackName: null,
          },
          {
            type: "attack",
            atSeconds: 2,
            attacker: "Player",
            kind: "Physical",
            hit: true,
            critical: false,
            stunned: false,
            damage: 90,
            elementMultiplier: 1,
            targetHpAfter: 1210,
            specialAttackName: null,
          },
          { type: "defeat", atSeconds: 3, loser: "Player" },
        ],
        won: false,
        character: makeCharacter({ name: "青衣" }),
      },
      "王都冠軍 格雷德",
    )
    expect(lines).toEqual([
      { text: "王都冠軍 格雷德攻擊，你受到 120 點傷害。", kind: "normal" },
      { text: "你攻擊 王都冠軍 格雷德，造成 90 點傷害。", kind: "normal" },
      { text: "你倒下了……", kind: "muted" },
      { text: "挑戰失敗，冠軍保住了寶座。", kind: "trigger" },
    ])
  })
})
