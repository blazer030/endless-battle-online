import { describe, expect, it } from "vitest"
import type { AdvanceResponse, BattleEvent, CharacterView } from "./types"
import { BattlePlayback } from "./battlePlayback"

const startCharacter: CharacterView = {
  name: "test",
  experience: 0,
  spiritStones: 0,
  currentStageId: "test-stage",
}

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
    experienceGained: 0,
    spiritStonesGained: 0,
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

  it("displayedCharacter_ExcludesUnplayedRewards", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([encounter(0), reward(2, 8, 0)], { experience: 13 }), 0)

    expect(playback.displayedCharacter.experience).toBe(5)

    playback.takeDue(2000)

    expect(playback.displayedCharacter.experience).toBe(13)
  })

  it("enqueue_EmptyEvents_UpdatesCharacterOnly", () => {
    const playback = new BattlePlayback(startCharacter)
    playback.enqueue(response([], { experience: 7, spiritStones: 3 }), 0)

    expect(playback.takeDue(100000)).toEqual([])
    expect(playback.displayedCharacter).toMatchObject({ experience: 7, spiritStones: 3 })
  })
})
