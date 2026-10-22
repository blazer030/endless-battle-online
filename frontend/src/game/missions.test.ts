import { describe, expect, it } from "vitest"
import {
  makeCharacter,
  makeContent,
  makeJob,
  makeMission,
  makeMissionBoard,
} from "../test/fixtures"
import {
  actionLabel,
  completionLines,
  MaxActiveMissions,
  missionErrorMessage,
  objectiveLines,
  progressRatio,
  requirementLines,
  resetHoursText,
  rewardLines,
} from "./missions"
import type { MissionCompletion } from "./types"

const content = makeContent({
  jobs: [makeJob({ id: "thief", name: "盜賊" })],
  sexes: [
    { id: "1", name: "男" },
    { id: "2", name: "女" },
  ],
  births: [
    { id: "1", name: "中央大陸" },
    { id: "2", name: "魔法王國" },
  ],
})

const delivery = makeMission({
  id: "test-delivery",
  kill: null,
  deliver: [
    { itemId: "herb", name: "藥草", quantity: 5, owned: 7 },
    { itemId: "fang", name: "狼牙", quantity: 3, owned: 1 },
  ],
})

describe("MaxActiveMissions", () => {
  it("is three", () => {
    expect(MaxActiveMissions).toBe(3)
  })
})

describe("requirementLines", () => {
  it("lists only the level when nothing else is required", () => {
    expect(requirementLines(makeMission({ level: 5 }), content)).toEqual(["Lv 5"])
  })

  it("names the job, sex, birth and prerequisite", () => {
    const mission = makeMission({
      level: 20,
      requirements: {
        jobId: "thief",
        sex: 2,
        birth: 2,
        prerequisiteId: "test-hunt",
        prerequisiteTitle: "綠地上的黏液",
      },
    })

    expect(requirementLines(mission, content)).toEqual([
      "Lv 20",
      "限 盜賊",
      "限 女性",
      "限 魔法王國出身",
      "需先完成「綠地上的黏液」",
    ])
  })

  it("falls back to ids when the names are not loaded", () => {
    const mission = makeMission({
      requirements: {
        jobId: "ninja",
        sex: 1,
        birth: 4,
        prerequisiteId: "test-hunt",
        prerequisiteTitle: null,
      },
    })

    expect(requirementLines(mission, makeContent())).toEqual([
      "Lv 1",
      "限 ninja",
      "限 1性",
      "限 4出身",
      "需先完成「test-hunt」",
    ])
  })
})

describe("objectiveLines", () => {
  it("describes a kill target with its place and progress", () => {
    expect(objectiveLines(makeMission())).toEqual(["擊敗 草原史萊姆（中央平原） 3/8"])
  })

  it("describes each delivery with what the bag holds", () => {
    expect(objectiveLines(delivery)).toEqual(["交付 藥草 7/5", "交付 狼牙 1/3"])
  })
})

describe("rewardLines", () => {
  it("lists exp, money, items and skill", () => {
    expect(
      rewardLines({
        exp: 19000,
        money: 1500,
        items: [{ itemId: "small-potion", name: "小補給藥劑", quantity: 5 }],
        skill: { skillId: "steal", name: "偷竊", level: 2 },
      }),
    ).toEqual(["經驗 19,000", "金錢 1,500", "小補給藥劑 ×5", "技能 偷竊 Lv2"])
  })

  it("skips zero exp and money", () => {
    expect(rewardLines({ exp: 0, money: 0, items: [], skill: null })).toEqual([])
  })
})

describe("progressRatio", () => {
  it("uses kill progress over the count", () => {
    expect(progressRatio(makeMission())).toBe(3 / 8)
  })

  it("caps kill progress at one", () => {
    const mission = makeMission()
    expect(
      progressRatio({ ...mission, kill: mission.kill && { ...mission.kill, progress: 12 } }),
    ).toBe(1)
  })

  it("counts delivered items without overshooting each line", () => {
    // min(7,5) + min(1,3) = 6，共需 8
    expect(progressRatio(delivery)).toBe(6 / 8)
  })

  it("is full for a ready mission without objectives", () => {
    expect(progressRatio(makeMission({ kill: null, ready: true }))).toBe(1)
    expect(progressRatio(makeMission({ kill: null }))).toBe(0)
  })
})

describe("actionLabel", () => {
  it("delivers when items are required, otherwise completes", () => {
    expect(actionLabel(delivery)).toBe("交付")
    expect(actionLabel(makeMission())).toBe("完成")
  })
})

describe("resetHoursText", () => {
  it("rounds the remaining time up to whole hours", () => {
    expect(resetHoursText("2026-10-23T01:00:00Z", new Date("2026-10-22T22:30:00Z"))).toBe(
      "※ 每日任務將在 3 小時後重置",
    )
  })

  it("never counts below zero", () => {
    expect(resetHoursText("2026-10-23T01:00:00Z", new Date("2026-10-23T02:00:00Z"))).toBe(
      "※ 每日任務將在 0 小時後重置",
    )
  })
})

describe("missionErrorMessage", () => {
  it.each([
    ["missionUnknown", "沒有這個任務"],
    ["alreadyActive", "已經接過此任務"],
    ["alreadyCompleted", "這個任務已經完成"],
    ["tooMany", "同時最多進行 3 個任務"],
    ["levelTooLow", "等級不足"],
    ["jobMismatch", "職業不符"],
    ["sexMismatch", "性別不符"],
    ["birthMismatch", "出生地不符"],
    ["prerequisiteMissing", "需先完成前置任務"],
    ["missionConflict", "已經選擇了另一條路線"],
    ["missionNotActive", "沒有進行中的這個任務"],
    ["objectiveIncomplete", "尚未完成任務"],
    ["itemsMissing", "交付的物品不足"],
    ["bagFull", "背包已滿"],
  ])("translates %s", (code, text) => {
    expect(missionErrorMessage([code], "無法接取任務")).toBe(text)
  })

  it("joins several reasons", () => {
    expect(missionErrorMessage(["levelTooLow", "jobMismatch"], "無法接取任務")).toBe(
      "等級不足、職業不符",
    )
  })

  it("falls back to the detail for unknown or missing codes", () => {
    expect(missionErrorMessage(["somethingNew"], "無法接取任務")).toBe("無法接取任務")
    expect(missionErrorMessage([], "無法完成任務")).toBe("無法完成任務")
  })
})

describe("completionLines", () => {
  const result: MissionCompletion = {
    board: makeMissionBoard(),
    character: makeCharacter(),
    completionMessage: "謝謝你，綠地又恢復平靜了。",
    exp: 1900,
    money: 50,
    items: [{ itemId: "small-potion", name: "小補給藥劑", quantity: 5 }],
    skill: { skillId: "steal", name: "偷竊", level: 1 },
    skillGranted: true,
    levelsGained: 2,
  }

  it("lists the message, rewards, skill and levels", () => {
    expect(completionLines(result)).toEqual([
      "謝謝你，綠地又恢復平靜了。",
      "獲得經驗 1,900、金錢 50",
      "獲得 小補給藥劑 ×5",
      "學會了偷竊 Lv1",
      "升了 2 級",
    ])
  })

  it("notes a skill already known and omits levels when none were gained", () => {
    expect(completionLines({ ...result, items: [], skillGranted: false, levelsGained: 0 })).toEqual(
      ["謝謝你，綠地又恢復平靜了。", "獲得經驗 1,900、金錢 50", "已經會偷竊，略過技能獎勵"],
    )
  })
})
