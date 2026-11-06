import { describe, expect, it } from "vitest"
import { applyToStage, initialStageState, stageEffectsFor } from "./stageModel"
import type { StageState } from "./stageModel"
import type {
  AttackEvent,
  BattleEvent,
  CaptureEvent,
  EncounterEvent,
  PetAttackEvent,
  PetGuardEvent,
  SkillEvent,
} from "./types"

const encounter: EncounterEvent = {
  type: "encounter",
  monsterId: "snow-sprite",
  monsterHp: 90,
  monsterHpMax: 100,
  playerHp: 150,
  playerHpMax: 200,
  playerSp: 10,
  playerSpMax: 20,
  atSeconds: 0,
}

const attack = (overrides: Partial<AttackEvent> = {}): BattleEvent => ({
  type: "attack",
  attacker: "Player",
  kind: "Physical",
  hit: true,
  critical: false,
  damage: 30,
  elementMultiplier: 1,
  targetHpAfter: 70,
  stunned: false,
  atSeconds: 1,
  ...overrides,
})

const skill = (overrides: Partial<SkillEvent> = {}): BattleEvent => ({
  type: "skill",
  caster: "Player",
  skillId: "burst",
  skillName: "爆氣",
  level: 1,
  spAfter: 3,
  heal: 0,
  damage: 0,
  opponentHpAfter: null,
  hpCost: 0,
  spCost: 0,
  moneyCost: 0,
  opponentSpLoss: 0,
  spGain: 0,
  lasting: false,
  atSeconds: 1,
  ...overrides,
})

const midFight: StageState = {
  monsterId: "frost-wolf",
  monsterHp: 5,
  monsterHpMax: 50,
  playerHp: 12,
  playerHpMax: 80,
  petName: null,
  petMonsterId: null,
  petActive: false,
}

const withPet = (event: EncounterEvent): EncounterEvent => ({
  ...event,
  petName: "小狼",
  petMonsterId: "wolf",
})

const petGuard = (overrides: Partial<PetGuardEvent> = {}): BattleEvent => ({
  type: "petGuard",
  petName: "小狼",
  damage: 12,
  guardAfter: 30,
  retired: false,
  atSeconds: 1,
  ...overrides,
})

const petAttack = (overrides: Partial<PetAttackEvent> = {}): BattleEvent => ({
  type: "petAttack",
  petName: "小狼",
  kind: "Physical",
  hit: true,
  critical: false,
  damage: 20,
  targetHpAfter: 60,
  atSeconds: 1,
  ...overrides,
})

describe("stageModel", () => {
  it("applyToStage_EncounterSetsBothHealthBarsFromEvent", () => {
    expect(applyToStage(midFight, encounter)).toEqual({
      monsterId: "snow-sprite",
      monsterHp: 90,
      monsterHpMax: 100,
      playerHp: 150,
      playerHpMax: 200,
      petName: null,
      petMonsterId: null,
      petActive: false,
    })
  })

  it("applyToStage_EncounterWithPet_ShowsPet", () => {
    expect(applyToStage(midFight, withPet(encounter))).toMatchObject({
      petName: "小狼",
      petMonsterId: "wolf",
      petActive: true,
    })
  })

  it("applyToStage_PetGuard_KeepsPlayerHealthAndRetiresPetWhenBroken", () => {
    const started = applyToStage(initialStageState, withPet(encounter))
    const guarded = applyToStage(started, petGuard())
    expect(guarded.playerHp).toBe(150)
    expect(guarded.petActive).toBe(true)
    expect(applyToStage(started, petGuard({ retired: true })).petActive).toBe(false)
  })

  it("applyToStage_PetAttack_SetsMonsterHealth", () => {
    const started = applyToStage(initialStageState, withPet(encounter))
    expect(applyToStage(started, petAttack()).monsterHp).toBe(60)
  })

  it("applyToStage_Capture_LeavesStateUnchanged", () => {
    const capture: BattleEvent = {
      type: "capture",
      itemName: "捕捉網",
      itemBroken: false,
      capturedMonsterId: "wolf",
      remaining: 2,
      atSeconds: 2,
    }
    expect(applyToStage(midFight, capture)).toBe(midFight)
  })

  it("stageEffectsFor_PetGuard_ShowsDamageOnPetAndRetiresWhenBroken", () => {
    expect(stageEffectsFor(petGuard())).toEqual([
      { type: "damageNumber", side: "pet", value: 12, critical: false },
      { type: "hitShake", side: "pet" },
    ])
    expect(stageEffectsFor(petGuard({ retired: true }))).toEqual([
      { type: "damageNumber", side: "pet", value: 12, critical: false },
      { type: "hitShake", side: "pet" },
      { type: "petRetire" },
    ])
  })

  it("stageEffectsFor_PetAttack_HitsOrMissesMonster", () => {
    expect(stageEffectsFor(petAttack({ critical: true }))).toEqual([
      { type: "damageNumber", side: "monster", value: 20, critical: true },
      { type: "hitShake", side: "monster" },
    ])
    expect(stageEffectsFor(petAttack({ hit: false }))).toEqual([{ type: "miss", side: "monster" }])
  })

  it("stageEffectsFor_CaptureSuccess_ShowsGoldPlate", () => {
    const capture = (overrides: Partial<CaptureEvent>): BattleEvent => ({
      type: "capture",
      itemName: "捕捉網",
      itemBroken: false,
      capturedMonsterId: "wolf",
      remaining: 2,
      atSeconds: 2,
      ...overrides,
    })
    expect(stageEffectsFor(capture({}))).toEqual([
      { type: "plate", text: "捕捉成功", tone: "gold" },
    ])
    expect(stageEffectsFor(capture({ capturedMonsterId: null, itemBroken: true }))).toEqual([])
  })

  it("applyToStage_AttackUpdatesTargetHealth", () => {
    const started = applyToStage(initialStageState, encounter)

    const afterPlayer = applyToStage(started, attack())
    expect(afterPlayer.monsterHp).toBe(70)
    expect(afterPlayer.playerHp).toBe(150)

    const afterMonster = applyToStage(started, attack({ attacker: "Monster" }))
    expect(afterMonster.playerHp).toBe(70)
    expect(afterMonster.monsterHp).toBe(90)
    expect(started.monsterHp).toBe(90)
  })

  it("applyToStage_RestRefillsPlayerHealth", () => {
    const rest: BattleEvent = {
      type: "rest",
      cost: 0,
      expPenalty: false,
      waitSeconds: 15,
      atSeconds: 3,
    }
    expect(applyToStage({ ...midFight, playerHp: 0 }, rest)).toEqual({
      ...midFight,
      playerHp: 80,
    })
  })

  it("applyToStage_RoundTimeoutLevelUpLeaveStateUnchanged", () => {
    const events: BattleEvent[] = [
      { type: "round", round: 2, atSeconds: 1 },
      { type: "timeout", rounds: 100, damageDealt: 3, atSeconds: 80 },
      {
        type: "levelUp",
        newLevel: 2,
        growth: { str: 1, smart: 0, life: 0, vit: 0, agi: 0, au: 0, be: 0 },
        atSeconds: 2,
      },
    ]
    for (const event of events) expect(applyToStage(midFight, event)).toBe(midFight)
  })

  it.each([
    [{ heal: 50 }, 150, 800],
    [{ heal: 450 }, 500, 800],
    [{ hpCost: 10 }, 90, 800],
    [{ opponentHpAfter: 700 }, 100, 700],
  ])("applyToStage_MonsterSkill_UpdatesBothSides %o", (overrides, monsterHp, playerHp) => {
    const state: StageState = {
      ...midFight,
      monsterHp: 100,
      monsterHpMax: 500,
      playerHp: 800,
      playerHpMax: 1000,
    }

    const next = applyToStage(state, skill({ caster: "Monster", ...overrides }))

    expect([next.monsterHp, next.playerHp]).toEqual([monsterHp, playerHp])
  })

  it("applyToStage_PlayerHpCostLowersPlayerHealth", () => {
    const state: StageState = { ...midFight, playerHp: 800, playerHpMax: 1000 }

    expect(applyToStage(state, skill({ hpCost: 10 })).playerHp).toBe(790)
  })

  it("applyToStage_HealSkillRaisesPlayerHealthUpToMax", () => {
    expect(applyToStage(midFight, skill({ heal: 30 })).playerHp).toBe(42)
    expect(applyToStage(midFight, skill({ heal: 500 })).playerHp).toBe(80)
    expect(applyToStage(midFight, skill())).toBe(midFight)
  })

  it("applyToStage_DamagingSkillSetsMonsterHealth", () => {
    expect(applyToStage(midFight, skill({ damage: 50, opponentHpAfter: 10 })).monsterHp).toBe(10)
  })

  it("applyToStage_PotionRaisesPlayerHealthUpToMax", () => {
    const potion = (hp: number): BattleEvent => ({
      type: "potion",
      itemId: "small-potion",
      name: "小藥水",
      hp,
      sp: 25,
      remaining: 8,
      atSeconds: 1,
    })
    expect(applyToStage(midFight, potion(50))).toEqual({ ...midFight, playerHp: 62 })
    expect(applyToStage(midFight, potion(800)).playerHp).toBe(80)
  })

  it("stageEffectsFor_Potion_FlashesPlayerGreen", () => {
    const potion: BattleEvent = {
      type: "potion",
      itemId: "small-potion",
      name: "小藥水",
      hp: 50,
      sp: 25,
      remaining: 8,
      atSeconds: 1,
    }
    expect(stageEffectsFor(potion)).toEqual([{ type: "potionFlash" }])
  })

  it("stageEffectsFor_RewardDrops_FloatEachAcceptedItemName", () => {
    const reward: BattleEvent = {
      type: "reward",
      exp: 5,
      money: 3,
      jobExp: 1,
      drops: [
        { itemId: "iron-ore", name: "鐵礦石", quantity: 2, discarded: false },
        { itemId: "beast-hide", name: "野獸毛皮", quantity: 1, discarded: true },
        { itemId: "magic-dust", name: "魔法粉塵", quantity: 1, discarded: false },
      ],
      atSeconds: 2,
    }
    expect(stageEffectsFor(reward)).toEqual([
      { type: "dropFloat", text: "鐵礦石" },
      { type: "dropFloat", text: "魔法粉塵" },
    ])
    expect(stageEffectsFor({ ...reward, drops: null })).toEqual([])
  })

  it("stageEffectsFor_PlayerSkill_FlashesCyanWithSkillPlate", () => {
    expect(stageEffectsFor(skill())).toEqual([
      { type: "skillFlash" },
      { type: "plate", text: "爆氣", tone: "cyan" },
    ])
  })

  it("stageEffectsFor_LastingSkillDamage_HasNoCastEffects", () => {
    expect(stageEffectsFor(skill({ damage: 50, opponentHpAfter: 10, lasting: true }))).toEqual([])
  })

  it("stageEffectsFor_StunnedMonster_ShowsMutedPlateOverMonster", () => {
    const stunned = attack({ attacker: "Monster", hit: false, damage: 0, stunned: true })
    expect(stageEffectsFor(stunned)).toEqual([
      { type: "plate", text: "暈眩", tone: "muted", over: "monster" },
    ])
  })

  it("stageEffectsFor_PlayerPhysicalHit_SlashesMonster", () => {
    expect(stageEffectsFor(attack())).toEqual([
      { type: "slash" },
      { type: "damageNumber", side: "monster", value: 30, critical: false },
      { type: "hitShake", side: "monster" },
    ])
  })

  it("stageEffectsFor_PlayerMagicHit_UsesBeamInsteadOfSlash", () => {
    expect(stageEffectsFor(attack({ kind: "Magic" }))).toEqual([
      { type: "beam" },
      { type: "damageNumber", side: "monster", value: 30, critical: false },
      { type: "hitShake", side: "monster" },
    ])
  })

  it("stageEffectsFor_PlayerCritical_ShowsNoPlate", () => {
    expect(stageEffectsFor(attack({ critical: true, damage: 500 }))).toEqual([
      { type: "slash" },
      { type: "damageNumber", side: "monster", value: 500, critical: true },
      { type: "hitShake", side: "monster" },
    ])
  })

  it("stageEffectsFor_MonsterCritical_ShowsNoPlate", () => {
    expect(stageEffectsFor(attack({ attacker: "Monster", critical: true, damage: 45 }))).toEqual([
      { type: "damageNumber", side: "player", value: 45, critical: true },
      { type: "hitShake", side: "player" },
    ])
  })

  it("stageEffectsFor_PlayerMiss_ShowsMissOverMonster", () => {
    expect(stageEffectsFor(attack({ hit: false, damage: 0 }))).toEqual([
      { type: "slash" },
      { type: "miss", side: "monster" },
    ])
  })

  it("stageEffectsFor_MonsterAttack_ShakesPlayer", () => {
    expect(stageEffectsFor(attack({ attacker: "Monster" }))).toEqual([
      { type: "damageNumber", side: "player", value: 30, critical: false },
      { type: "hitShake", side: "player" },
    ])
  })

  it("stageEffectsFor_MonsterMiss_ShowsMissOverPlayer", () => {
    expect(stageEffectsFor(attack({ attacker: "Monster", hit: false, damage: 0 }))).toEqual([
      { type: "miss", side: "player" },
    ])
  })

  it("stageEffectsFor_MonsterDefeat_PlaysImpactBurst", () => {
    expect(stageEffectsFor({ type: "defeat", loser: "Monster", atSeconds: 2 })).toEqual([
      { type: "impactBurst" },
    ])
  })

  it("stageEffectsFor_Escape_FadesTheMonster", () => {
    expect(stageEffectsFor({ type: "escape", atSeconds: 0.8 })).toEqual([{ type: "monsterFade" }])
  })

  it("applyToStage_EscapeLeavesStateUnchanged", () => {
    expect(applyToStage(midFight, { type: "escape", atSeconds: 0.8 })).toBe(midFight)
  })

  it("stageEffectsFor_Timeout_StepsBackWithMutedPlate", () => {
    expect(
      stageEffectsFor({ type: "timeout", rounds: 100, damageDealt: 3, atSeconds: 80 }),
    ).toEqual([{ type: "standOff" }, { type: "plate", text: "膠著", tone: "muted" }])
  })

  it("stageEffectsFor_LevelUp_FlashesPlayerWithPlate", () => {
    const levelUp: BattleEvent = {
      type: "levelUp",
      newLevel: 2,
      growth: { str: 1, smart: 0, life: 0, vit: 0, agi: 0, au: 0, be: 0 },
      atSeconds: 2,
    }
    expect(stageEffectsFor(levelUp)).toEqual([
      { type: "levelUpFlash" },
      { type: "plate", text: "LEVEL UP", tone: "gold" },
    ])
  })

  it("stageEffectsFor_MissionCompleted_ShowsGoldPlate", () => {
    const mission: BattleEvent = {
      type: "mission",
      missionId: "m1",
      title: "清除雪精",
      monsterId: "slime",
      progress: 3,
      required: 3,
      requiresDelivery: false,
      completed: true,
      atSeconds: 2,
    }
    expect(stageEffectsFor(mission)).toEqual([{ type: "plate", text: "任務達成", tone: "gold" }])
    expect(stageEffectsFor({ ...mission, progress: 2, completed: false })).toEqual([])
    expect(applyToStage(midFight, mission)).toBe(midFight)
  })

  it("stageEffectsFor_OneTargetFullOfSeveral_ShowsNoPlate", () => {
    const mission: BattleEvent = {
      type: "mission",
      missionId: "m3",
      title: "冒牌傳說",
      monsterId: "slime",
      progress: 3,
      required: 3,
      requiresDelivery: false,
      completed: false,
      atSeconds: 2,
    }
    expect(stageEffectsFor(mission)).toEqual([])
  })

  it("stageEffectsFor_MixedMissionKillsMet_ShowsNoPlate", () => {
    const mission: BattleEvent = {
      type: "mission",
      missionId: "m2",
      title: "幽靈船長的寶藏",
      monsterId: "slime",
      progress: 3,
      required: 3,
      requiresDelivery: true,
      completed: true,
      atSeconds: 2,
    }
    expect(stageEffectsFor(mission)).toEqual([])
  })

  it("stageEffectsFor_Rest_FadesStageOverWait", () => {
    const rest: BattleEvent = {
      type: "rest",
      cost: 0,
      expPenalty: false,
      waitSeconds: 15,
      atSeconds: 3,
    }
    expect(stageEffectsFor(rest)).toEqual([{ type: "fade", seconds: 15 }])
  })

  it("stageEffectsFor_EncounterRoundReward_HaveNoEffects", () => {
    const reward: BattleEvent = {
      type: "reward",
      exp: 5,
      money: 3,
      jobExp: 1,
      drops: [],
      atSeconds: 2,
    }
    const round: BattleEvent = { type: "round", round: 1, atSeconds: 0 }
    for (const event of [encounter, round, reward]) expect(stageEffectsFor(event)).toEqual([])
  })

  it("stageEffectsFor_MonsterSkill_ShowsPlateOverMonster", () => {
    expect(stageEffectsFor(skill({ caster: "Monster" }))).toEqual([
      { type: "plate", text: "爆氣", tone: "cyan", over: "monster" },
    ])
  })

  it("stageEffectsFor_MonsterLastingSkill_ShowsNothing", () => {
    expect(
      stageEffectsFor(
        skill({ caster: "Monster", lasting: true, damage: 25, opponentHpAfter: 700 }),
      ),
    ).toEqual([])
  })

  it("stageEffectsFor_StunnedPlayer_ShowsPlateOverPlayer", () => {
    expect(stageEffectsFor(attack({ hit: false, damage: 0, stunned: true }))).toEqual([
      { type: "plate", text: "暈眩", tone: "muted", over: "player" },
    ])
  })
})
