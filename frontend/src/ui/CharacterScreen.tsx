import { useId } from "react"
import type { BaseStats, DerivedStats } from "../game/stats"
import type { CharacterView, ContentNames } from "../game/types"
import { formatNumber } from "./formatNumber"
import { StatBar } from "./StatBar"
import "./CharacterScreen.css"

type Props = {
  character: CharacterView
  content: Pick<ContentNames, "elements" | "births" | "sexes">
}

const baseStats: { key: keyof BaseStats; label: string }[] = [
  { key: "str", label: "力量" },
  { key: "smart", label: "智力" },
  { key: "life", label: "生命" },
  { key: "vit", label: "體質" },
  { key: "agi", label: "敏捷" },
  { key: "au", label: "魅力" },
  { key: "be", label: "信仰" },
]

const derivedStats: { key: keyof DerivedStats; label: string }[] = [
  { key: "at", label: "物攻" },
  { key: "mat", label: "魔攻" },
  { key: "df", label: "物防" },
  { key: "mdf", label: "魔防" },
  { key: "hpMax", label: "HP 上限" },
  { key: "spMax", label: "SP 上限" },
]

// 內容的 id 是字串 "1"～"N"；名稱表沒載到時直接顯示 id
const nameOf = (options: { id: string; name: string }[], id: number) =>
  options.find((option) => option.id === String(id))?.name ?? String(id)

const currentJobProficiency = (character: CharacterView) =>
  character.jobProficiency.find((entry) => entry.jobId === character.jobId)?.proficiency ?? 0

export function CharacterScreen({ character, content }: Props) {
  const profileId = useId()
  const baseStatsId = useId()
  const derivedStatsId = useId()
  const { stats } = character
  const profile = [
    { label: "性別", value: nameOf(content.sexes, character.sex) },
    { label: "屬性", value: nameOf(content.elements, character.element) },
    { label: "出生地", value: nameOf(content.births, character.birth) },
    { label: "必殺技", value: character.specialAttackName },
    { label: "職業熟練度", value: formatNumber(currentJobProficiency(character)) },
  ]
  const bars = [
    { label: "HP", value: character.hp, max: stats.hpMax, tone: "hp" },
    { label: "SP", value: character.sp, max: stats.spMax, tone: "sp" },
    { label: "EXP", value: character.exp, max: character.nextExp, tone: "exp" },
  ] as const

  return (
    <div className="character">
      <section className="character__card" aria-labelledby={profileId}>
        <div className="character__identity">
          <h2 id={profileId} className="character__name">
            {character.name}
          </h2>
          <span className="character__job">{character.jobName}</span>
          <span className="character__level">{`Lv ${character.level}`}</span>
        </div>
        <dl className="character__grid character__profile">
          {profile.map(({ label, value }) => (
            <div key={label} className="character__entry">
              <dt>{label}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>
        <div className="character__bars">
          {bars.map(({ label, value, max, tone }) => (
            <div key={label} className="character__bar">
              <span className="character__bar-label">{label}</span>
              <StatBar label={label} value={value} max={max} tone={tone} />
              <span className="character__bar-value">{`${formatNumber(value)} / ${formatNumber(max)}`}</span>
            </div>
          ))}
        </div>
      </section>

      <section className="character__card" aria-labelledby={baseStatsId}>
        <h2 id={baseStatsId} className="character__card-title">
          能力
        </h2>
        <dl className="character__grid">
          {baseStats.map(({ key, label }) => (
            <div key={key} className="character__entry">
              <dt>{label}</dt>
              {/* 括號內是裝備加成，裝備系統上線前一律 +0 */}
              <dd>
                {formatNumber(stats[key])}
                <span className="character__bonus">(+0)</span>
              </dd>
            </div>
          ))}
        </dl>
      </section>

      <section className="character__card" aria-labelledby={derivedStatsId}>
        <h2 id={derivedStatsId} className="character__card-title">
          戰鬥數值
        </h2>
        <dl className="character__grid">
          {derivedStats.map(({ key, label }) => (
            <div key={key} className="character__entry">
              <dt>{label}</dt>
              <dd>{formatNumber(stats[key])}</dd>
            </div>
          ))}
        </dl>
      </section>
    </div>
  )
}
