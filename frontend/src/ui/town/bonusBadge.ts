import type { BuffKind } from "../../game/buffs"
import type { BonusRow } from "../../game/town"

const kindClass: Record<BuffKind, string> = {
  Experience: "experience",
  JobExperience: "job-experience",
  Attack: "attack",
  Magic: "magic",
  Defense: "defense",
  MagicDefense: "magic-defense",
}

// 住宿與密藥圓章的樣式（TownBonuses.css）：住宿方角，密藥依種類換底色
export const bonusBadgeClass = (row: BonusRow) =>
  `town-bonus__disc town-bonus__disc--${row.isLodging ? "lodging" : kindClass[row.kind]}`
