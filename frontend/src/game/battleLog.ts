import { formatNumber } from "./format"
import type { PlaybackItem } from "./battlePlayback"
import { baseStatLabels } from "./stats"
import type {
  AttackEvent,
  CaptureEvent,
  ContentNames,
  DropResult,
  GrowthStats,
  MercenaryReport,
  MissionProgressEvent,
  PetAttackEvent,
  PetGuardEvent,
  RewardEvent,
  SkillEvent,
} from "./types"

export type LogLineKind = "normal" | "muted" | "drop" | "trigger"
export type LogLine = { id: number; text: string; kind: LogLineKind }
export type LogEntry = Omit<LogLine, "id">
export type MonsterNameResolver = (monsterId: string) => string

export const MaxLogLines = 200

export const createMonsterNameResolver = (content: ContentNames): MonsterNameResolver => {
  const names = new Map<string, string>()
  for (const monster of content.monsters) names.set(monster.id, monster.name)
  return (monsterId) => names.get(monsterId) ?? monsterId
}

const describeAttack = (event: AttackEvent, monster: string): LogEntry => {
  const damage = formatNumber(event.damage)
  if (event.attacker === "Player") {
    if (!event.hit) return { text: "你的攻擊落空了。", kind: "muted" }
    if (event.critical) {
      const name = event.specialAttackName ?? "暴擊"
      return { text: `${name}！${monster}受到 ${damage} 點傷害！`, kind: "trigger" }
    }
    return event.kind === "Magic"
      ? { text: `你施放魔法，${monster}受到 ${damage} 點傷害。`, kind: "normal" }
      : { text: `你攻擊 ${monster}，造成 ${damage} 點傷害。`, kind: "normal" }
  }
  if (event.stunned) return { text: `${monster}暈眩中，無法行動。`, kind: "muted" }
  if (!event.hit) return { text: `${monster}的攻擊落空了。`, kind: "normal" }
  return event.critical
    ? { text: `${monster}重擊！你受到 ${damage} 點傷害！`, kind: "normal" }
    : { text: `${monster}攻擊，你受到 ${damage} 點傷害。`, kind: "normal" }
}

// 本計畫怪物沒有技能，只敘述玩家的施放
const describeSkill = (event: SkillEvent): LogEntry[] => {
  if (event.caster !== "Player") return []
  const lines: LogEntry[] = [{ text: `你施展了${event.skillName}！`, kind: "trigger" }]
  if (event.heal > 0)
    lines.push({ text: `回復了 ${formatNumber(event.heal)} 點 HP。`, kind: "normal" })
  return lines
}

const describeDrop = (drop: DropResult): LogEntry =>
  drop.discarded
    ? { text: `背包已滿，${drop.name}被丟棄了。`, kind: "muted" }
    : { text: `獲得 ${drop.name} ×${formatNumber(drop.quantity)}`, kind: "drop" }

const describeReward = (event: RewardEvent): LogEntry[] => [
  {
    text: `獲得經驗 ${formatNumber(event.exp)}、金錢 ${formatNumber(event.money)}、熟練度 ${formatNumber(event.jobExp)}。`,
    kind: "drop",
  },
  ...(event.drops ?? []).map(describeDrop),
]

const describeMission = (event: MissionProgressEvent, monster: string): LogEntry[] => {
  const lines: LogEntry[] = [
    { text: `任務進度：${monster} ${event.progress}/${event.required}`, kind: "drop" },
  ]
  if (event.progress === event.required)
    lines.push({
      text: event.requiresDelivery
        ? `任務「${event.title}」的擊殺目標已達成，交付道具後回任務頁回報。`
        : `任務「${event.title}」的目標已達成，回任務頁回報。`,
      kind: "trigger",
    })
  return lines
}

const PetMissFlavors = [" 沒有擊中目標", " 發呆", " 東張西望", " 失去鬥志"]

const describePetGuard = (event: PetGuardEvent): LogEntry[] => {
  const lines: LogEntry[] = [
    { text: `${event.petName}替你擋下了 ${formatNumber(event.damage)} 點傷害。`, kind: "normal" },
  ]
  if (event.retired) lines.push({ text: `${event.petName} 戰敗，退出了戰鬥。`, kind: "muted" })
  return lines
}

const describePetAttack = (event: PetAttackEvent, monster: string): LogEntry => {
  const damage = formatNumber(event.damage)
  if (!event.hit)
    return {
      text: `${event.petName}${PetMissFlavors[Math.floor(event.atSeconds) % PetMissFlavors.length]}`,
      kind: "muted",
    }
  return event.critical
    ? { text: `${event.petName}會心一擊！對${monster}造成 ${damage} 點傷害。`, kind: "trigger" }
    : { text: `${event.petName}發動攻擊，對${monster}造成 ${damage} 點傷害。`, kind: "normal" }
}

const describeCapture = (event: CaptureEvent, monsterName: MonsterNameResolver): LogEntry[] => {
  const lines: LogEntry[] = []
  if (event.itemBroken)
    lines.push({ text: `${event.itemName}受到損壞，數量減少1個！！`, kind: "muted" })
  if (event.capturedMonsterId !== null)
    lines.push({
      text: `捕捉到 ${monsterName(event.capturedMonsterId)}，${event.itemName}數量減少1個！！`,
      kind: "trigger",
    })
  return lines
}

// 報告的 recent 只帶最後 5 場，超過就改成一行彙總（WOG wog_etc_mercenary_mg1～mg3）
const MercenaryFightsListed = 5

export const describeMercenaryReport = (report: MercenaryReport): LogEntry[] => {
  const prefix = `【傭兵】${report.name}`
  const lines: LogEntry[] =
    report.fights <= MercenaryFightsListed
      ? report.recent.map((fight) => ({
          text: fight.won
            ? `${prefix} 戰勝 ${fight.monsterName}，獲得金錢 ${formatNumber(fight.money)}，經驗值 ${formatNumber(fight.exp)}`
            : `${prefix} 戰敗 ${fight.monsterName}`,
          kind: "muted",
        }))
      : [
          {
            text: `${prefix} 離開期間戰鬥 ${formatNumber(report.fights)} 場（勝 ${formatNumber(report.wins)}），獲得金錢 ${formatNumber(report.money)}，經驗值 ${formatNumber(report.exp)}`,
            kind: "muted",
          },
        ]
  if (report.dismissed) lines.push({ text: `${prefix} 戰鬥次數已滿，自動解除僱用`, kind: "muted" })
  return lines
}

const describeGrowth = (growth: GrowthStats) =>
  baseStatLabels
    .filter(({ key }) => growth[key] !== 0)
    .map(({ key, label }) => `${label}+${formatNumber(growth[key])}`)
    .join(" ")

// playerName 只用於 WOG 原文帶角色名的句子（逃跑）
export const describePlaybackItem = (
  item: PlaybackItem,
  monsterName: MonsterNameResolver,
  currentMonsterId: string | null,
  playerName = "你",
): LogEntry[] => {
  if (item.kind === "mercenary") return describeMercenaryReport(item.report)
  if (item.kind === "summary") {
    const gains = `離開期間戰鬥 ${formatNumber(item.fights)} 場（勝 ${formatNumber(item.wins)}）、獲得經驗 ${formatNumber(item.exp)}、金錢 ${formatNumber(item.money)}`
    const missions = item.missionKills > 0 ? `，任務進度 +${formatNumber(item.missionKills)}` : ""
    const pets = item.petsCaptured > 0 ? `，捕捉寵物 ${formatNumber(item.petsCaptured)} 隻` : ""
    const levels = item.levels > 0 ? `，升了 ${formatNumber(item.levels)} 級` : ""
    return [{ text: `${gains}${missions}${pets}${levels}。`, kind: "drop" }]
  }

  const event = item.event
  const current = currentMonsterId === null ? "" : monsterName(currentMonsterId)
  switch (event.type) {
    case "encounter": {
      const lines: LogEntry[] = [{ text: `遭遇 ${monsterName(event.monsterId)}！`, kind: "normal" }]
      if (event.staminaDepleted) lines.push({ text: "行動力耗盡，報酬減半。", kind: "muted" })
      return lines
    }
    case "round":
      return []
    case "attack":
      return [describeAttack(event, current)]
    case "skill":
      return describeSkill(event)
    case "defeat":
      return [
        event.loser === "Monster"
          ? { text: `${current}倒下了。`, kind: "normal" }
          : { text: "你倒下了……", kind: "muted" },
      ]
    case "timeout":
      return [{ text: "戰鬥陷入膠著，雙方收手。", kind: "muted" }]
    case "escape":
      // WOG end_date
      return [{ text: `${playerName} 逃跑成功`, kind: "muted" }]
    case "potion":
      return [
        {
          text: `使用了${event.name}，恢復 ${formatNumber(event.hp)} HP／${formatNumber(event.sp)} SP。`,
          kind: "normal",
        },
      ]
    case "petGuard":
      return describePetGuard(event)
    case "petAttack":
      return [describePetAttack(event, current)]
    case "capture":
      return describeCapture(event, monsterName)
    case "reward":
      return describeReward(event)
    case "mission":
      return describeMission(event, monsterName(event.monsterId))
    case "levelUp": {
      const growth = describeGrowth(event.growth)
      const level = `升級！Lv ${event.newLevel}`
      return [{ text: growth === "" ? level : `${level}（${growth}）`, kind: "trigger" }]
    }
    case "rest": {
      const lines: LogEntry[] = [{ text: "你在旅館休息，恢復了體力。", kind: "muted" }]
      if (event.expPenalty) lines.push({ text: "付不起住宿費，經驗減少了。", kind: "muted" })
      else if (event.cost > 0)
        lines.push({ text: `付了 ${formatNumber(event.cost)} 金錢住宿。`, kind: "muted" })
      return lines
    }
  }
}
