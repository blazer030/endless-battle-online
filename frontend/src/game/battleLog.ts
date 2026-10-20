import type { PlaybackItem } from "./battlePlayback"
import type { ContentNames } from "./types"

export type LogLineKind = "normal" | "muted" | "drop" | "trigger"
export type LogLine = { id: number; text: string; kind: LogLineKind }
export type MonsterNameResolver = (monsterId: string) => string

export const MaxLogLines = 200

export const createMonsterNameResolver = (content: ContentNames): MonsterNameResolver => {
  const names = new Map<string, string>()
  for (const monster of content.monsters) names.set(monster.id, monster.name)
  return (monsterId) => names.get(monsterId) ?? monsterId
}

const format = (value: number) => value.toLocaleString("zh-TW")

export const describePlaybackItem = (
  item: PlaybackItem,
  monsterName: MonsterNameResolver,
  currentMonsterId: string | null,
): Omit<LogLine, "id"> | null => {
  if (item.kind === "summary") {
    return {
      text: `離開期間歷練 ${format(item.fights)} 場，獲得修為 ${format(item.experience)}、靈石 ${format(item.spiritStones)}。`,
      kind: "drop",
    }
  }

  const event = item.event
  const current = currentMonsterId === null ? "" : monsterName(currentMonsterId)
  switch (event.type) {
    case "encounter":
      return { text: `${monsterName(event.monsterId)}現身。`, kind: "normal" }
    case "attack": {
      const damage = format(event.damage)
      if (event.attacker === "Player") {
        return event.isCritical
          ? { text: `你一劍穿心，會心 ${damage}！`, kind: "trigger" }
          : { text: `你運劍出招，${current}受到 ${damage} 點傷害。`, kind: "normal" }
      }
      return event.isCritical
        ? { text: `${current}猛然重擊，你受到 ${damage} 點傷害！`, kind: "muted" }
        : { text: `${current}反擊，你受到 ${damage} 點傷害。`, kind: "muted" }
    }
    case "defeat":
      return event.loser === "Monster"
        ? { text: `${current}化為一縷輕煙。`, kind: "normal" }
        : { text: "你力竭倒下，調息片刻再戰。", kind: "muted" }
    case "reward":
      return {
        text: `獲得修為 ${format(event.experience)}、靈石 ${format(event.spiritStones)}。`,
        kind: "drop",
      }
  }
}
