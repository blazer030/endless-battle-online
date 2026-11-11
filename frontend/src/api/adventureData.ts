import { attackKinds } from "../game/character"
import {
  AdvanceVo,
  combatSides,
  type AdvanceSummary,
  type BattleEvent,
  type MercenaryReport,
} from "../game/battleEvents"
import { CharacterConverter, toBaseStats } from "./characterData"
import {
  parseArray,
  parseBoolean,
  parseNullableNumber,
  parseNullableString,
  parseNumber,
  parseObject,
  parseOneOf,
  parseString,
  parseTimestamp,
} from "./parse"
import type { components } from "./schema"

export type AdvanceDto = components["schemas"]["AdvanceResponse"]

const eventTypes = [
  "encounter",
  "round",
  "attack",
  "skill",
  "potion",
  "defeat",
  "timeout",
  "reward",
  "levelUp",
  "rest",
  "mission",
  "npc",
  "petGuard",
  "petAttack",
  "capture",
  "escape",
] as const

const toEvent = (value: unknown, path: string): BattleEvent => {
  const dto = parseObject(value, path)
  const text = (key: string) => parseString(dto[key], `${path}.${key}`)
  const nullableText = (key: string) => parseNullableString(dto[key], `${path}.${key}`)
  const number = (key: string) => parseNumber(dto[key], `${path}.${key}`)
  const flag = (key: string) => parseBoolean(dto[key], `${path}.${key}`)
  const side = (key: string) => parseOneOf(dto[key], combatSides, `${path}.${key}`)
  const attackKind = () => parseOneOf(dto.kind, attackKinds, `${path}.kind`)
  const atSeconds = number("atSeconds")
  const type = parseOneOf(dto.type, eventTypes, `${path}.type`)
  switch (type) {
    case "encounter":
      return {
        type,
        atSeconds,
        monsterId: text("monsterId"),
        monsterName: text("monsterName"),
        monsterHealthPoints: number("monsterHealthPoints"),
        monsterMaxHealthPoints: number("monsterMaxHealthPoints"),
        playerHealthPoints: number("playerHealthPoints"),
        playerMaxHealthPoints: number("playerMaxHealthPoints"),
        playerManaPoints: number("playerManaPoints"),
        playerMaxManaPoints: number("playerMaxManaPoints"),
        petName: nullableText("petName"),
        petMonsterId: nullableText("petMonsterId"),
      }
    case "round":
      return { type, atSeconds, round: number("round") }
    case "attack":
      return {
        type,
        atSeconds,
        attacker: side("attacker"),
        kind: attackKind(),
        hit: flag("hit"),
        critical: flag("critical"),
        damage: number("damage"),
        elementMultiplier: number("elementMultiplier"),
        targetHealthPointsAfter: number("targetHealthPointsAfter"),
        stunned: flag("stunned"),
      }
    case "skill":
      return {
        type,
        atSeconds,
        caster: side("caster"),
        skillId: text("skillId"),
        skillName: text("skillName"),
        level: number("level"),
        manaPointsAfter: number("manaPointsAfter"),
        heal: number("heal"),
        damage: number("damage"),
        opponentHealthPointsAfter: parseNullableNumber(
          dto.opponentHealthPointsAfter,
          `${path}.opponentHealthPointsAfter`,
        ),
        healthPointsCost: number("healthPointsCost"),
        manaPointsCost: number("manaPointsCost"),
        moneyCost: number("moneyCost"),
        opponentManaPointsLoss: number("opponentManaPointsLoss"),
        manaPointsGain: number("manaPointsGain"),
        lasting: flag("lasting"),
      }
    case "potion":
      return {
        type,
        atSeconds,
        itemId: text("itemId"),
        name: text("name"),
        healthPoints: number("healthPoints"),
        manaPoints: number("manaPoints"),
        remaining: number("remaining"),
      }
    case "defeat":
      return { type, atSeconds, loser: side("loser") }
    case "timeout":
      return { type, atSeconds, rounds: number("rounds"), damageDealt: number("damageDealt") }
    case "reward":
      return {
        type,
        atSeconds,
        experience: number("experience"),
        money: number("money"),
        jobExperience: number("jobExperience"),
        drops: parseArray(dto.drops, `${path}.drops`, (item, itemPath) => {
          const drop = parseObject(item, itemPath)
          return {
            itemId: parseString(drop.itemId, `${itemPath}.itemId`),
            name: parseString(drop.name, `${itemPath}.name`),
            quantity: parseNumber(drop.quantity, `${itemPath}.quantity`),
            discarded: parseBoolean(drop.discarded, `${itemPath}.discarded`),
          }
        }),
      }
    case "levelUp":
      return {
        type,
        atSeconds,
        newLevel: number("newLevel"),
        growth: toBaseStats(dto.growth, `${path}.growth`),
      }
    case "rest":
      return {
        type,
        atSeconds,
        cost: number("cost"),
        experiencePenalty: flag("experiencePenalty"),
        waitSeconds: number("waitSeconds"),
      }
    case "mission":
      return {
        type,
        atSeconds,
        missionId: text("missionId"),
        title: text("title"),
        monsterId: text("monsterId"),
        monsterName: text("monsterName"),
        progress: number("progress"),
        required: number("required"),
        requiresDelivery: flag("requiresDelivery"),
        completed: flag("completed"),
        skillTooLow: flag("skillTooLow"),
      }
    case "npc":
      return {
        type,
        atSeconds,
        monsterId: text("monsterId"),
        name: text("name"),
        message: nullableText("message"),
      }
    case "petGuard":
      return {
        type,
        atSeconds,
        petName: text("petName"),
        damage: number("damage"),
        guardAfter: number("guardAfter"),
        retired: flag("retired"),
      }
    case "petAttack":
      return {
        type,
        atSeconds,
        petName: text("petName"),
        kind: attackKind(),
        hit: flag("hit"),
        critical: flag("critical"),
        damage: number("damage"),
        targetHealthPointsAfter: number("targetHealthPointsAfter"),
      }
    case "capture":
      return {
        type,
        atSeconds,
        itemName: text("itemName"),
        itemBroken: flag("itemBroken"),
        capturedMonsterId: nullableText("capturedMonsterId"),
        capturedMonsterName: nullableText("capturedMonsterName"),
        remaining: number("remaining"),
      }
    case "escape":
      return { type, atSeconds }
  }
}

const toSummary = (value: unknown, path: string): AdvanceSummary => {
  const dto = parseObject(value, path)
  const number = (key: string) => parseNumber(dto[key], `${path}.${key}`)
  return {
    fights: number("fights"),
    wins: number("wins"),
    losses: number("losses"),
    timeouts: number("timeouts"),
    experience: number("experience"),
    money: number("money"),
    jobExperience: number("jobExperience"),
    levelsGained: number("levelsGained"),
    skillsCast: number("skillsCast"),
    drops: number("drops"),
    potionsUsed: number("potionsUsed"),
    missionKills: number("missionKills"),
    petsCaptured: number("petsCaptured"),
    escapes: number("escapes"),
  }
}

const toMercenaryReport = (value: unknown, path: string): MercenaryReport => {
  const dto = parseObject(value, path)
  const number = (key: string) => parseNumber(dto[key], `${path}.${key}`)
  return {
    name: parseString(dto.name, `${path}.name`),
    fights: number("fights"),
    wins: number("wins"),
    experience: number("experience"),
    money: number("money"),
    levelsGained: number("levelsGained"),
    remaining: number("remaining"),
    dismissed: parseBoolean(dto.dismissed, `${path}.dismissed`),
    recent: parseArray(dto.recent, `${path}.recent`, (item, itemPath) => {
      const fight = parseObject(item, itemPath)
      return {
        at: parseTimestamp(fight.at, `${itemPath}.at`),
        monsterId: parseString(fight.monsterId, `${itemPath}.monsterId`),
        monsterName: parseString(fight.monsterName, `${itemPath}.monsterName`),
        won: parseBoolean(fight.won, `${itemPath}.won`),
        experience: parseNumber(fight.experience, `${itemPath}.experience`),
        money: parseNumber(fight.money, `${itemPath}.money`),
      }
    }),
  }
}

export const AdvanceConverter = {
  toVo(value: unknown, path = "advance"): AdvanceVo {
    const dto = parseObject(value, path)
    return AdvanceVo.create({
      events: parseArray(dto.events, `${path}.events`, toEvent),
      summary: toSummary(dto.summary, `${path}.summary`),
      eventsCoverAll: parseBoolean(dto.eventsCoverAll, `${path}.eventsCoverAll`),
      character: CharacterConverter.toVo(dto.character, `${path}.character`),
      mercenary:
        dto.mercenary === null ? null : toMercenaryReport(dto.mercenary, `${path}.mercenary`),
      areaPassExpiresAt:
        dto.areaPassExpiresAt === null
          ? null
          : parseTimestamp(dto.areaPassExpiresAt, `${path}.areaPassExpiresAt`),
    })
  },
}
