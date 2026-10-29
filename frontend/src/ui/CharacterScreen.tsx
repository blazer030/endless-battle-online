import { useEffect, useId, useState } from "react"
import type { ApiClient } from "../api/client"
import { buffLines } from "../game/buffs"
import { lodgingLine } from "../game/inn"
import { proficiencyPercent } from "../game/skills"
import { baseStatLabels, type DerivedStats } from "../game/stats"
import type { BuffView, CharacterView, ContentNames, InnView } from "../game/types"
import { formatNumber } from "../game/format"
import { BuffTickMilliseconds } from "../motion/timings"
import { StatBar } from "./StatBar"
import "./CharacterScreen.css"

type Props = {
  api: Pick<ApiClient, "getBuffs" | "getInn">
  character: CharacterView
  content: Pick<ContentNames, "elements" | "births" | "sexes">
}

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

const describeProficiency = (value: number) =>
  `${proficiencyPercent(value)}%（${formatNumber(value)}）`

const signed = (value: number) => (value > 0 ? `+${formatNumber(value)}` : formatNumber(value))

export function CharacterScreen({ api, character, content }: Props) {
  const profileId = useId()
  const baseStatsId = useId()
  const derivedStatsId = useId()
  const buffsId = useId()
  // 剩餘分鐘定時重算，到期的效果跟著消失
  const [now, setNow] = useState(() => new Date())
  const [buffs, setBuffs] = useState<BuffView[] | null>(null)
  const [buffsFailed, setBuffsFailed] = useState(false)
  // 讀不到旅館資料就不顯示住宿那一行，不影響密藥
  const [inn, setInn] = useState<InnView | null>(null)

  useEffect(() => {
    let active = true
    api.getBuffs().then(
      (loaded) => {
        if (active) setBuffs(loaded)
      },
      () => {
        if (active) setBuffsFailed(true)
      },
    )
    return () => {
      active = false
    }
  }, [api])

  useEffect(() => {
    let active = true
    api.getInn().then(
      (loaded) => {
        if (active) setInn(loaded)
      },
      () => {},
    )
    return () => {
      active = false
    }
  }, [api])

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), BuffTickMilliseconds)
    return () => clearInterval(timer)
  }, [])

  const activeBuffs = buffs === null ? [] : buffLines(buffs, now)
  const lodging = inn === null ? null : lodgingLine(inn.lodging, inn.rooms, now)
  const { stats, baseStats } = character
  const profile = [
    { label: "性別", value: nameOf(content.sexes, character.sex) },
    { label: "屬性", value: nameOf(content.elements, character.element) },
    { label: "出生地", value: nameOf(content.births, character.birth) },
    { label: "必殺技", value: character.specialAttackName },
    { label: "職業熟練度", value: describeProficiency(currentJobProficiency(character)) },
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
          {baseStatLabels.map(({ key, label }) => (
            <div key={key} className="character__entry">
              <dt>{label}</dt>
              {/* 裸點（+裝備）：括號內是有效數值減裸點，為 0 時不顯示 */}
              <dd>
                {formatNumber(baseStats[key])}
                {stats[key] !== baseStats[key] && (
                  <span className="character__bonus">{`（${signed(stats[key] - baseStats[key])}）`}</span>
                )}
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

      <section className="character__card" aria-labelledby={buffsId}>
        <h2 id={buffsId} className="character__card-title">
          密藥效果
        </h2>
        {buffsFailed ? (
          <p className="character__muted">無法讀取密藥效果</p>
        ) : buffs === null ? null : activeBuffs.length === 0 ? (
          <p className="character__muted">沒有生效中的密藥</p>
        ) : (
          <ul className="character__buffs">
            {activeBuffs.map((line) => (
              <li key={line}>{line}</li>
            ))}
          </ul>
        )}
        {lodging !== null && <p className="character__lodging">{lodging}</p>}
      </section>
    </div>
  )
}
