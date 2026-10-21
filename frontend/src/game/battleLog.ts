import type { PlaybackItem } from "./battlePlayback"
import type { AttackEvent, ContentNames, GrowthStats } from "./types"

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

const format = (value: number) => value.toLocaleString("zh-TW")

const growthLabels: [keyof GrowthStats, string][] = [
  ["str", "力量"],
  ["smart", "智力"],
  ["life", "生命"],
  ["vit", "體質"],
  ["agi", "敏捷"],
  ["au", "魅力"],
  ["be", "信仰"],
]

const describeAttack = (event: AttackEvent, monster: string): LogEntry => {
  const damage = format(event.damage)
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
  if (!event.hit) return { text: `${monster}的攻擊落空了。`, kind: "normal" }
  return event.critical
    ? { text: `${monster}重擊！你受到 ${damage} 點傷害！`, kind: "normal" }
    : { text: `${monster}攻擊，你受到 ${damage} 點傷害。`, kind: "normal" }
}

const describeGrowth = (growth: GrowthStats) =>
  growthLabels
    .filter(([key]) => growth[key] !== 0)
    .map(([key, label]) => `${label}+${format(growth[key])}`)
    .join(" ")

export const describePlaybackItem = (
  item: PlaybackItem,
  monsterName: MonsterNameResolver,
  currentMonsterId: string | null,
): LogEntry[] => {
  if (item.kind === "summary") {
    const gains = `離開期間戰鬥 ${format(item.fights)} 場（勝 ${format(item.wins)}）、獲得經驗 ${format(item.exp)}、金錢 ${format(item.money)}`
    const levels = item.levels > 0 ? `，升了 ${format(item.levels)} 級` : ""
    return [{ text: `${gains}${levels}。`, kind: "drop" }]
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
    case "defeat":
      return [
        event.loser === "Monster"
          ? { text: `${current}倒下了。`, kind: "normal" }
          : { text: "你倒下了……", kind: "muted" },
      ]
    case "timeout":
      return [{ text: "戰鬥陷入膠著，雙方收手。", kind: "muted" }]
    case "reward":
      return [
        {
          text: `獲得經驗 ${format(event.exp)}、金錢 ${format(event.money)}、熟練度 ${format(event.jobExp)}。`,
          kind: "drop",
        },
      ]
    case "levelUp": {
      const growth = describeGrowth(event.growth)
      const level = `升級！Lv ${event.newLevel}`
      return [{ text: growth === "" ? level : `${level}（${growth}）`, kind: "trigger" }]
    }
    case "rest": {
      const lines: LogEntry[] = [{ text: "你在旅館休息，恢復了體力。", kind: "muted" }]
      if (event.expPenalty) lines.push({ text: "付不起住宿費，經驗減少了。", kind: "muted" })
      else if (event.cost > 0)
        lines.push({ text: `付了 ${format(event.cost)} 金錢住宿。`, kind: "muted" })
      return lines
    }
  }
}
