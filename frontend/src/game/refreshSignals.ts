import type { PlaybackItem } from "./battlePlayback"

// 播出的項目會讓哪些開著的分頁過時
export type RefreshSignals = { bag: boolean; missions: boolean; skills: boolean }

const none: RefreshSignals = { bag: false, missions: false, skills: false }

const signalsOf = (item: PlaybackItem): RefreshSignals => {
  if (item.kind === "mercenary") return { ...none, bag: true }
  if (item.kind !== "event") return none
  const { event } = item
  switch (event.type) {
    case "reward":
      return {
        ...none,
        bag: (event.drops ?? []).some((drop) => !drop.discarded),
        skills: event.jobExp > 0,
      }
    case "potion":
    case "capture":
      return { ...none, bag: true }
    case "mission":
      return { ...none, missions: true }
    case "levelUp":
      return { ...none, skills: true }
    default:
      return none
  }
}

export const refreshSignalsFor = (items: PlaybackItem[]): RefreshSignals =>
  items.map(signalsOf).reduce(
    (total, signals) => ({
      bag: total.bag || signals.bag,
      missions: total.missions || signals.missions,
      skills: total.skills || signals.skills,
    }),
    none,
  )
