import { formatNumber } from "./format"
import type { PlaybackItem } from "./battlePlayback"
import { baseStatLabels } from "./stats"
import type {
  AttackEvent,
  CaptureEvent,
  Drop,
  MercenaryReport,
  MissionProgressEvent,
  PetAttackEvent,
  PetGuardEvent,
  RewardEvent,
  SkillEvent,
} from "./battleEvents"
import type { BaseStats } from "./character"

export type LogLineKind = "normal" | "muted" | "drop" | "trigger"
export type LogLine = { id: number; text: string; kind: LogLineKind }
export type LogEntry = Omit<LogLine, "id">

export const MaxLogLines = 200

const describeAttack = (event: AttackEvent, monster: string): LogEntry => {
  const damage = formatNumber(event.damage)
  if (event.attacker === "Player") {
    if (event.stunned) return { text: "你暈眩中，無法行動。", kind: "muted" }
    if (!event.hit) return { text: "你的攻擊落空了。", kind: "muted" }
    if (event.critical) {
      return event.kind === "Magic"
        ? { text: `暴擊！你施放魔法，${monster}受到 ${damage} 點傷害！`, kind: "trigger" }
        : { text: `暴擊！你攻擊${monster}，造成 ${damage} 點傷害！`, kind: "trigger" }
    }
    return event.kind === "Magic"
      ? { text: `你施放魔法，${monster}受到 ${damage} 點傷害。`, kind: "normal" }
      : { text: `你攻擊 ${monster}，造成 ${damage} 點傷害。`, kind: "normal" }
  }
  if (event.stunned) return { text: `${monster}暈眩中，無法行動。`, kind: "muted" }
  if (!event.hit) return { text: `${monster}的攻擊落空了。`, kind: "normal" }
  return event.critical
    ? { text: `${monster}暴擊！你受到 ${damage} 點傷害！`, kind: "normal" }
    : { text: `${monster}攻擊，你受到 ${damage} 點傷害。`, kind: "normal" }
}

// 怪物的施放：回復與對玩家造成的傷害、削減的 SP，不寫消耗
const describeMonsterSkill = (event: SkillEvent, monster: string): LogEntry[] => {
  if (event.lasting)
    return [
      {
        text: `${event.skillName}持續作用，你受到 ${formatNumber(event.damage)} 點傷害。`,
        kind: "normal",
      },
    ]
  const lines: LogEntry[] = [{ text: `${monster}施展了${event.skillName}！`, kind: "trigger" }]
  if (event.heal > 0)
    lines.push({ text: `${monster}回復了 ${formatNumber(event.heal)} 點 HP。`, kind: "normal" })
  if (event.manaPointsGain > 0)
    lines.push({
      text: `${monster}回復了 ${formatNumber(event.manaPointsGain)} 點 SP。`,
      kind: "normal",
    })
  if (event.damage > 0)
    lines.push({ text: `你受到 ${formatNumber(event.damage)} 點傷害。`, kind: "normal" })
  if (event.opponentManaPointsLoss > 0)
    lines.push({
      text: `你失去 ${formatNumber(event.opponentManaPointsLoss)} 點 SP。`,
      kind: "normal",
    })
  return lines
}

// 玩家的施放：回復、對怪物造成的傷害（削減 HP）與削減的 SP；
// 已發動技能每回合的持續傷害只寫結果，不算再施展一次
const describeSkill = (event: SkillEvent, monster: string): LogEntry[] => {
  if (event.caster !== "Player") return describeMonsterSkill(event, monster)
  if (event.lasting)
    return [
      {
        text: `${event.skillName}持續作用，${monster}受到 ${formatNumber(event.damage)} 點傷害。`,
        kind: "normal",
      },
    ]
  const lines: LogEntry[] = [{ text: `你施展了${event.skillName}！`, kind: "trigger" }]
  if (event.heal > 0)
    lines.push({ text: `回復了 ${formatNumber(event.heal)} 點 HP。`, kind: "normal" })
  if (event.manaPointsGain > 0)
    lines.push({ text: `回復了 ${formatNumber(event.manaPointsGain)} 點 SP。`, kind: "normal" })
  const costs = [
    event.healthPointsCost > 0 ? `HP ${formatNumber(event.healthPointsCost)}` : null,
    event.manaPointsCost > 0 ? `SP ${formatNumber(event.manaPointsCost)}` : null,
    event.moneyCost > 0 ? `金錢 ${formatNumber(event.moneyCost)}` : null,
  ].filter((cost): cost is string => cost !== null)
  if (costs.length > 0) lines.push({ text: `消耗了 ${costs.join("、")}。`, kind: "muted" })
  if (event.damage > 0)
    lines.push({ text: `${monster}受到 ${formatNumber(event.damage)} 點傷害。`, kind: "normal" })
  if (event.opponentManaPointsLoss > 0)
    lines.push({
      text: `${monster}失去 ${formatNumber(event.opponentManaPointsLoss)} 點 SP。`,
      kind: "normal",
    })
  return lines
}

const describeDrop = (drop: Drop): LogEntry =>
  drop.discarded
    ? { text: `背包已滿，${drop.name}被丟棄了。`, kind: "muted" }
    : { text: `獲得 ${drop.name} ×${formatNumber(drop.quantity)}`, kind: "drop" }

const describeReward = (event: RewardEvent): LogEntry[] => [
  {
    text: `獲得經驗 ${formatNumber(event.experience)}、金錢 ${formatNumber(event.money)}、熟練度 ${formatNumber(event.jobExperience)}。`,
    kind: "drop",
  },
  ...(event.drops ?? []).map(describeDrop),
]

const describeMission = (event: MissionProgressEvent, monster: string): LogEntry[] => {
  const lines: LogEntry[] = [
    { text: `任務進度：${monster} ${event.progress}/${event.required}`, kind: "drop" },
  ]
  if (event.completed) {
    const remaining = [
      ...(event.requiresDelivery ? ["交付"] : []),
      ...(event.skillTooLow ? ["技能等級達到要求"] : []),
    ]
    lines.push({
      text:
        remaining.length > 0
          ? `任務「${event.title}」的擊殺目標已達成，${remaining.join("且")}後回任務頁回報。`
          : `任務「${event.title}」的目標已達成，回任務頁回報。`,
      kind: "trigger",
    })
  }
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

const describeCapture = (event: CaptureEvent): LogEntry[] => {
  const lines: LogEntry[] = []
  if (event.itemBroken)
    lines.push({ text: `${event.itemName}受到損壞，數量減少1個！！`, kind: "muted" })
  if (event.capturedMonsterName !== null)
    lines.push({
      text: `捕捉到 ${event.capturedMonsterName}，${event.itemName}數量減少1個！！`,
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
            ? `${prefix} 戰勝 ${fight.monsterName}，獲得金錢 ${formatNumber(fight.money)}，經驗值 ${formatNumber(fight.experience)}`
            : `${prefix} 戰敗 ${fight.monsterName}`,
          kind: "muted",
        }))
      : [
          {
            text: `${prefix} 離開期間戰鬥 ${formatNumber(report.fights)} 場（勝 ${formatNumber(report.wins)}），獲得金錢 ${formatNumber(report.money)}，經驗值 ${formatNumber(report.experience)}`,
            kind: "muted",
          },
        ]
  if (report.dismissed) lines.push({ text: `${prefix} 戰鬥次數已滿，自動解除僱用`, kind: "muted" })
  return lines
}

const describeGrowth = (growth: BaseStats) =>
  baseStatLabels
    .filter(({ key }) => growth[key] !== 0)
    .map(({ key, label }) => `${label}+${formatNumber(growth[key])}`)
    .join(" ")

// currentMonsterName 是目前對戰的怪物（沒有對戰時為 null）；playerName 只用於 WOG 原文帶角色名的句子（逃跑）
export const describePlaybackItem = (
  item: PlaybackItem,
  currentMonsterName: string | null,
  playerName = "你",
): LogEntry[] => {
  if (item.kind === "mercenary") return describeMercenaryReport(item.report)
  if (item.kind === "summary") {
    const gains = `離開期間戰鬥 ${formatNumber(item.fights)} 場（勝 ${formatNumber(item.wins)}）、獲得經驗 ${formatNumber(item.experience)}、金錢 ${formatNumber(item.money)}`
    const missions = item.missionKills > 0 ? `，任務進度 +${formatNumber(item.missionKills)}` : ""
    const pets = item.petsCaptured > 0 ? `，捕捉寵物 ${formatNumber(item.petsCaptured)} 隻` : ""
    const levels = item.levels > 0 ? `，升了 ${formatNumber(item.levels)} 級` : ""
    return [{ text: `${gains}${missions}${pets}${levels}。`, kind: "drop" }]
  }

  const event = item.event
  const current = currentMonsterName ?? ""
  switch (event.type) {
    case "encounter":
      return [{ text: `遭遇 ${event.monsterName}！`, kind: "normal" }]
    case "round":
      return []
    case "attack":
      return [describeAttack(event, current)]
    case "skill":
      return describeSkill(event, current)
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
          text: `使用了${event.name}，恢復 ${formatNumber(event.healthPoints)} HP／${formatNumber(event.manaPoints)} SP。`,
          kind: "normal",
        },
      ]
    case "petGuard":
      return describePetGuard(event)
    case "petAttack":
      return [describePetAttack(event, current)]
    case "capture":
      return describeCapture(event)
    case "reward":
      return describeReward(event)
    case "mission":
      return describeMission(event, event.monsterName)
    case "npc":
      return [
        {
          text:
            event.message === null
              ? `遇到 ${event.name}`
              : `遇到 ${event.name}：「${event.message}」`,
          kind: "normal",
        },
      ]
    case "levelUp": {
      const growth = describeGrowth(event.growth)
      const level = `升級！Lv ${event.newLevel}`
      return [{ text: growth === "" ? level : `${level}（${growth}）`, kind: "trigger" }]
    }
    case "rest": {
      const lines: LogEntry[] = [{ text: "你在旅館休息，恢復了體力。", kind: "muted" }]
      if (event.experiencePenalty) lines.push({ text: "付不起住宿費，經驗減少了。", kind: "muted" })
      else if (event.cost > 0)
        lines.push({ text: `付了 ${formatNumber(event.cost)} 金錢住宿。`, kind: "muted" })
      return lines
    }
  }
}
