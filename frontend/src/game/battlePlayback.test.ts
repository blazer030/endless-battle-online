import { describe, expect, it } from "vitest"
import type { AdvanceResponse, BattleEvent, CharacterView } from "./types"
import { makeCharacter } from "../test/fixtures"
import { BattlePlayback } from "./battlePlayback"

const startCharacter: CharacterView = makeCharacter({ name: "test", currentStageId: "test-stage" })

function encounter(atSeconds: number): BattleEvent {
  return {
    type: "encounter",
    atSeconds,
    monsterId: "test-wolf",
    monsterMaxHealth: 60,
    playerMaxHealth: 120,
  }
}

function reward(atSeconds: number, experience: number, spiritStones: number): BattleEvent {
  return { type: "reward", atSeconds, experience, spiritStones }
}

function response(events: BattleEvent[], character: Partial<CharacterView> = {}): AdvanceResponse {
  return {
    events,
    expGained: 0,
    moneyGained: 0,
    character: { ...startCharacter, ...character },
  }
}

describe("BattlePlayback", () => {
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

  it("enqueue_CompressesLongBatchIntoSummary", () => {
    const playback = new BattlePlayback(startCharacter)
    const lastEncounter = encounter(40)
    playback.enqueue(
      response([
        encounter(0),
        reward(5, 5, 2),
        encounter(20),
        reward(25, 5, 2),
        lastEncounter,
        reward(45, 5, 2),
      ]),
      1000,
    )

    const due = playback.takeDue(1000)

    expect(due).toEqual([
      { kind: "summary", fights: 2, experience: 10, spiritStones: 4 },
      { kind: "event", event: { ...lastEncounter, atSeconds: 0 } },
    ])
  })

  it("enqueue_SummaryWaitsForPreviousBatch", () => {
    const playback = new BattlePlayback(startCharacter)
    const firstEvent = encounter(0)
    const lastOfFirstBatch = reward(9, 1, 1)
    playback.enqueue(response([firstEvent, lastOfFirstBatch]), 0)
    const lastEncounter = encounter(35)
    playback.enqueue(
      response([encounter(0), reward(5, 1, 1), lastEncounter, reward(40, 1, 1)]),
      5000,
    )

    expect(playback.takeDue(5000)).toEqual([{ kind: "event", event: firstEvent }])
    expect(playback.takeDue(9000)).toEqual([{ kind: "event", event: lastOfFirstBatch }])
    expect(playback.takeDue(10000)).toEqual([
      { kind: "summary", fights: 1, experience: 1, spiritStones: 1 },
      { kind: "event", event: { ...lastEncounter, atSeconds: 0 } },
    ])
  })

  it("displayedCharacter_ExcludesUnplayedRewards", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([encounter(0), reward(2, 8, 0)], { exp: 13 }), 0)

    expect(playback.displayedCharacter.exp).toBe(5)

    playback.takeDue(2000)

    expect(playback.displayedCharacter.exp).toBe(13)
  })

  it("enqueue_EmptyEvents_UpdatesCharacterOnly", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([], { exp: 7, money: 3 }), 0)

    expect(playback.takeDue(100000)).toEqual([])
    expect(playback.displayedCharacter).toMatchObject({ exp: 7, money: 3 })
  })
})
