import { describe, expect, it } from "vitest"
import type { AdvanceSummary, AdvanceVo, BattleEvent, MercenaryReport } from "./battleEvents"
import type { CharacterVo } from "./character"
import { makeCharacter, makeSummary } from "../test/fixtures"
import { BattlePlayback } from "./battlePlayback"

const startCharacter: CharacterVo = makeCharacter({ name: "test" })

function encounter(atSeconds: number): BattleEvent {
  return {
    type: "encounter",
    atSeconds,
    monsterId: "test-wolf",
    monsterName: "測試寒狼",
    monsterHealthPoints: 60,
    monsterMaxHealthPoints: 60,
    playerHealthPoints: 120,
    playerMaxHealthPoints: 120,
    playerManaPoints: 10,
    playerMaxManaPoints: 10,
    petName: null,
    petMonsterId: null,
  }
}

function reward(atSeconds: number, experience: number, money: number): BattleEvent {
  return { type: "reward", atSeconds, experience, money, jobExperience: 1, drops: [] }
}

function levelUp(atSeconds: number, newLevel: number): BattleEvent {
  return {
    type: "levelUp",
    atSeconds,
    newLevel,
    growth: {
      strength: 1,
      intelligence: 0,
      life: 0,
      vitality: 0,
      agility: 0,
      charisma: 0,
      faith: 0,
    },
  }
}

function rest(atSeconds: number, cost: number): BattleEvent {
  return { type: "rest", atSeconds, cost, experiencePenalty: false, waitSeconds: 15 }
}

function response(
  events: BattleEvent[],
  character: Partial<CharacterVo> = {},
  summary: Partial<AdvanceSummary> = {},
  eventsCoverAll = true,
  mercenary: MercenaryReport | null = null,
): AdvanceVo {
  return {
    events,
    summary: makeSummary(summary),
    eventsCoverAll,
    character: { ...startCharacter, ...character },
    mercenary,
    areaPassExpiresAt: null,
  }
}

const mercenaryReport: MercenaryReport = {
  name: "阿福",
  fights: 1,
  wins: 1,
  experience: 40,
  money: 7,
  levelsGained: 0,
  remaining: 99,
  dismissed: false,
  recent: [
    {
      at: new Date("2026-10-21T00:00:00Z"),
      monsterId: "test-wolf",
      monsterName: "測試寒狼",
      won: true,
      experience: 40,
      money: 7,
    },
  ],
}

describe("BattlePlayback", () => {
  it("confirm_ReplacesCharacterAndKeepsUnplayedGainsPending", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([reward(2, 10, 30)], { experience: 10, money: 30 }), 0)

    playback.confirm({
      ...startCharacter,
      experience: 10,
      money: 20,
      jobId: "knight",
      jobName: "騎士",
    })

    expect(playback.displayedCharacter).toMatchObject({
      experience: 0,
      money: 0,
      jobId: "knight",
      jobName: "騎士",
    })
  })

  it("discardQueued_DropsUnplayedItemsAndShowsTheConfirmedCharacter", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(
      response([encounter(0), reward(2, 10, 30), levelUp(3, 2)], {
        level: 2,
        experience: 5,
        money: 30,
      }),
      0,
    )
    playback.takeDue(0)

    expect(playback.discardQueued()).toBe(2)
    expect(playback.takeDue(60_000)).toEqual([])
    expect(playback.displayedCharacter).toEqual({
      ...startCharacter,
      level: 2,
      experience: 5,
      money: 30,
    })
  })

  it("discardQueued_NextBatchStartsWithoutWaitingForTheDiscardedTimeline", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([encounter(0), reward(30, 1, 1)]), 0)
    playback.discardQueued()
    const next = encounter(0)

    playback.enqueue(response([next]), 1000)

    expect(playback.takeDue(1000)).toEqual([{ kind: "event", event: next }])
  })

  it("takeDue_ReleasesEventsAtTheirOffsets", () => {
    const playback = new BattlePlayback(startCharacter)
    const first = encounter(0)
    const second = reward(1.5, 1, 1)
    playback.enqueue(response([first, second]), 1000)

    expect(playback.takeDue(1000)).toEqual([{ kind: "event", event: first }])
    expect(playback.takeDue(2499)).toEqual([])
    expect(playback.takeDue(2500)).toEqual([{ kind: "event", event: second }])
  })

  it("enqueue_ChainsBatchesBackToBack", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([encounter(0), reward(6, 0, 0)]), 0)
    const secondBatchFirst = encounter(0)
    playback.enqueue(response([secondBatchFirst]), 2000)

    playback.takeDue(6000)
    expect(playback.takeDue(6999)).toEqual([])
    expect(playback.takeDue(7000)).toEqual([{ kind: "event", event: secondBatchFirst }])
  })

  it("enqueue_LongBatch_PlaysEveryEventWithoutClientCompression", () => {
    const playback = new BattlePlayback(startCharacter)
    const late = reward(45, 5, 2)
    playback.enqueue(response([encounter(0), reward(5, 5, 2), encounter(40), late]), 0)

    expect(playback.takeDue(0)).toEqual([{ kind: "event", event: encounter(0) }])
    playback.takeDue(44_999)
    expect(playback.takeDue(45_000)).toEqual([{ kind: "event", event: late }])
  })

  it("enqueue_PartialEvents_LeadsWithServerSummaryAndPlaysTailFromBatchStart", () => {
    const playback = new BattlePlayback(startCharacter)
    const tailEncounter = encounter(92.4)
    const tailReward = reward(93.2, 5, 2)
    playback.enqueue(
      response(
        [tailEncounter, tailReward],
        {},
        {
          fights: 43,
          wins: 42,
          experience: 215,
          money: 86,
          levelsGained: 1,
          missionKills: 3,
          petsCaptured: 2,
        },
        false,
      ),
      1000,
    )

    expect(playback.takeDue(1000)).toEqual([
      {
        kind: "summary",
        fights: 43,
        wins: 42,
        experience: 215,
        money: 86,
        levels: 1,
        missionKills: 3,
        petsCaptured: 2,
      },
      { kind: "event", event: { ...tailEncounter, atSeconds: 0 } },
    ])
    expect(playback.takeDue(1799)).toEqual([])
    const due = playback.takeDue(1800)
    expect(due).toEqual([
      { kind: "event", event: { ...tailReward, atSeconds: expect.any(Number) } },
    ])
    expect(due[0].kind === "event" && due[0].event.atSeconds).toBeCloseTo(0.8)
  })

  it("enqueue_EventsCoverAll_HasNoSummary", () => {
    const playback = new BattlePlayback(startCharacter)
    const only = encounter(0)
    playback.enqueue(response([only], {}, { fights: 1, wins: 1 }, true), 0)

    expect(playback.takeDue(0)).toEqual([{ kind: "event", event: only }])
  })

  it("enqueue_PartialEventsWithoutFights_HasNoSummary", () => {
    const playback = new BattlePlayback(startCharacter)
    const only = rest(0, 0)
    playback.enqueue(response([only], {}, { fights: 0 }, false), 0)

    expect(playback.takeDue(0)).toEqual([{ kind: "event", event: only }])
  })

  it("enqueue_SummaryOnly_WhenNoEventsReturned", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(
      response(
        [],
        { experience: 50, money: 9 },
        { fights: 5, wins: 5, experience: 20, money: 4 },
        false,
      ),
      1000,
    )

    expect(playback.displayedCharacter).toMatchObject({ experience: 30, money: 5 })
    expect(playback.takeDue(999)).toEqual([])
    expect(playback.takeDue(1000)).toEqual([
      {
        kind: "summary",
        fights: 5,
        wins: 5,
        experience: 20,
        money: 4,
        levels: 0,
        missionKills: 0,
        petsCaptured: 0,
      },
    ])
    expect(playback.displayedCharacter).toMatchObject({ experience: 50, money: 9 })
    const next = encounter(0)
    playback.enqueue(response([next]), 1000)
    expect(playback.takeDue(1999)).toEqual([])
    expect(playback.takeDue(2000)).toEqual([{ kind: "event", event: next }])
  })

  it("enqueue_SummaryWaitsForPreviousBatch", () => {
    const playback = new BattlePlayback(startCharacter)
    const firstEvent = encounter(0)
    const lastOfFirstBatch = reward(9, 1, 1)
    playback.enqueue(response([firstEvent, lastOfFirstBatch]), 0)
    const tail = encounter(35)
    playback.enqueue(
      response([tail], {}, { fights: 2, wins: 1, experience: 1, money: 1 }, false),
      5000,
    )

    expect(playback.takeDue(5000)).toEqual([{ kind: "event", event: firstEvent }])
    expect(playback.takeDue(9000)).toEqual([{ kind: "event", event: lastOfFirstBatch }])
    expect(playback.takeDue(10000)).toEqual([
      {
        kind: "summary",
        fights: 2,
        wins: 1,
        experience: 1,
        money: 1,
        levels: 0,
        missionKills: 0,
        petsCaptured: 0,
      },
      { kind: "event", event: { ...tail, atSeconds: 0 } },
    ])
  })

  it("displayedCharacter_ExcludesUnplayedRewards", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([encounter(0), reward(2, 8, 0)], { experience: 13 }), 0)

    expect(playback.displayedCharacter.experience).toBe(5)

    playback.takeDue(2000)

    expect(playback.displayedCharacter.experience).toBe(13)
  })

  it("displayedCharacter_SummaryCountsOnlyTheSkippedPart", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(
      response(
        [encounter(35), reward(40, 3, 2)],
        { experience: 107, money: 2008 },
        { fights: 2, wins: 2, experience: 7, money: 8 },
        false,
      ),
      0,
    )

    expect(playback.displayedCharacter).toMatchObject({ experience: 100, money: 2000 })

    playback.takeDue(0)

    expect(playback.displayedCharacter).toMatchObject({ experience: 104, money: 2006 })

    playback.takeDue(5000)

    expect(playback.displayedCharacter).toMatchObject({ experience: 107, money: 2008 })
  })

  it("displayedCharacter_AddsBackUnplayedRestCost", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([rest(0, 300), encounter(15)], { money: 700 }), 0)

    expect(playback.displayedCharacter.money).toBe(1000)

    playback.takeDue(0)

    expect(playback.displayedCharacter.money).toBe(700)
  })

  it("displayedCharacter_HoldsLevelAndExpUntilLevelUpPlays", () => {
    const before = makeCharacter({ level: 1, experience: 990, nextLevelExperience: 1000 })
    const playback = new BattlePlayback(before)
    playback.enqueue(
      response([encounter(0), reward(2, 30, 0), levelUp(2, 2)], {
        level: 2,
        experience: 20,
        nextLevelExperience: 1092,
      }),
      0,
    )

    expect(playback.displayedCharacter).toMatchObject({
      level: 1,
      experience: 990,
      nextLevelExperience: 1000,
    })

    // 獎勵與升級同一時刻；只播到獎勵前一刻時仍是舊等級
    playback.takeDue(1999)
    expect(playback.displayedCharacter).toMatchObject({
      level: 1,
      experience: 990,
      nextLevelExperience: 1000,
    })

    playback.takeDue(2000)
    expect(playback.displayedCharacter).toMatchObject({
      level: 2,
      experience: 20,
      nextLevelExperience: 1092,
    })
  })

  it("displayedCharacter_FillsExpBarBeforePendingLevelUp", () => {
    const before = makeCharacter({ level: 1, experience: 900, nextLevelExperience: 1000 })
    const playback = new BattlePlayback(before)
    playback.enqueue(
      response([reward(0, 50, 0), reward(1, 80, 0), levelUp(1, 2)], {
        level: 2,
        experience: 30,
        nextLevelExperience: 1092,
      }),
      0,
    )

    playback.takeDue(0)

    expect(playback.displayedCharacter).toMatchObject({
      level: 1,
      experience: 950,
      nextLevelExperience: 1000,
    })
  })

  it("displayedCharacter_SummaryLevelsHoldLevelUntilSummaryPlays", () => {
    const before = makeCharacter({ level: 4, experience: 10, nextLevelExperience: 1200 })
    const playback = new BattlePlayback(before)
    playback.enqueue(
      response(
        [encounter(95)],
        { level: 6, experience: 5, nextLevelExperience: 1400 },
        { fights: 30, wins: 30, experience: 4000, money: 0, levelsGained: 2 },
        false,
      ),
      0,
    )

    expect(playback.displayedCharacter).toMatchObject({
      level: 4,
      experience: 10,
      nextLevelExperience: 1200,
    })

    playback.takeDue(0)

    expect(playback.displayedCharacter).toMatchObject({
      level: 6,
      experience: 5,
      nextLevelExperience: 1400,
    })
  })

  it("enqueue_EmptyEvents_UpdatesCharacterOnly", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([], { experience: 7, money: 3 }), 0)

    expect(playback.takeDue(100000)).toEqual([])
    expect(playback.displayedCharacter).toMatchObject({ experience: 7, money: 3 })
  })

  it("enqueue_Mercenary_LeadsTheBatchAndHoldsItsGains", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(
      response(
        [encounter(0), reward(2, 10, 30)],
        { experience: 50, money: 37 },
        {},
        true,
        mercenaryReport,
      ),
      1000,
    )

    expect(playback.displayedCharacter).toMatchObject({ experience: 0, money: 0 })
    const first = playback.takeDue(1000)
    expect(first).toEqual([
      { kind: "mercenary", report: mercenaryReport },
      { kind: "event", event: encounter(0) },
    ])
    expect(playback.displayedCharacter).toMatchObject({ experience: 40, money: 7 })
  })

  it("enqueue_MercenaryOnly_IsScheduledWithoutEvents", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([], { experience: 40, money: 7 }, {}, true, mercenaryReport), 500)

    expect(playback.displayedCharacter).toMatchObject({ experience: 0, money: 0 })
    expect(playback.takeDue(500)).toEqual([{ kind: "mercenary", report: mercenaryReport }])
    expect(playback.displayedCharacter).toMatchObject({ experience: 40, money: 7 })
  })
})
