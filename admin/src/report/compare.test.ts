import { describe, expect, it } from "vitest"
import type { BalanceReport } from "../api/report"
import { adventureTable, wearLevelTable } from "./compare"

const result = {
  fights: 100,
  wins: 90,
  winRate: 0.9,
  losses: 10,
  timeouts: 0,
  expPerHour: 1200,
  grossMoneyPerHour: 300,
  netMoneyPerHour: 250.4,
  levelsGained: 2,
  nextTierHours: 6.4,
  nextTierUnaffordable: false,
}

const official: BalanceReport = {
  assumptions: {
    allocation: { str: 4, smart: 4, life: 1, agi: 1 },
    seed: 1,
    simulatedHours: 8,
    maxReportedLevel: 100,
    skillLevel: "Max",
  },
  shopLevels: [1, 2],
  wearLevels: [
    {
      jobId: "warrior",
      jobName: "戰士",
      stat: "str",
      cells: [
        { shopLevel: 1, required: 10, level: 1 },
        { shopLevel: 2, required: 30, level: 8 },
      ],
    },
  ],
  adventures: [
    {
      placeId: 2,
      placeName: "洞窟",
      restricted: false,
      level: 6,
      jobId: "warrior",
      jobName: "戰士",
      equipmentShopLevels: [1, 1],
      withoutSkills: result,
      withSkills: result,
    },
  ],
}

describe("wearLevelTable", () => {
  it("marks only the cell whose level differs from the official report", () => {
    const draft: BalanceReport = {
      ...official,
      wearLevels: [
        {
          ...official.wearLevels[0],
          cells: [official.wearLevels[0].cells[0], { shopLevel: 2, required: 34, level: 10 }],
        },
      ],
    }

    const [row] = wearLevelTable(draft, official)

    expect(row.cells.map((cell) => [cell.value, cell.before])).toEqual([
      ["1", undefined],
      ["10", "8"],
    ])
  })

  it("shows unreachable levels as a dash and stats without requirements as blank", () => {
    const draft: BalanceReport = {
      ...official,
      wearLevels: [
        {
          ...official.wearLevels[0],
          cells: [
            { shopLevel: 1, required: null, level: null },
            { shopLevel: 2, required: 400, level: null },
          ],
        },
      ],
    }

    expect(wearLevelTable(draft).map((row) => row.cells.map((cell) => cell.value))).toEqual([
      ["", "—"],
    ])
  })
})

describe("adventureTable", () => {
  it("marks only the metric that changed", () => {
    const draft: BalanceReport = {
      ...official,
      adventures: [{ ...official.adventures[0], withSkills: { ...result, netMoneyPerHour: 410 } }],
    }

    const [row] = adventureTable(draft, official)

    expect(row.cells.filter((cell) => cell.before !== undefined)).toEqual([
      { key: "withSkills.netMoneyPerHour", value: "410", before: "250" },
    ])
  })
})
