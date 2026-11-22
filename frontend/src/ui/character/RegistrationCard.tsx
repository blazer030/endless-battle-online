import { characterCard } from "../../game/characterScreen"
import { ContentImage } from "../ContentImage"
import type { CharacterData } from "./CharacterScreen"

type Props = {
  // null＝讀取中或讀取失敗：字與數字留位置不寫、條是空的
  data: CharacterData | null
  onOpenJobChange: () => void
}

const Bar = ({ ratio, tone }: { ratio: number; tone: string }) => (
  <span className={`registration__bar registration__bar--${tone}`}>
    <i style={{ width: `${Math.round(ratio * 1000) / 10}%` }} />
  </span>
)

// 冒險者登記證：名字、職業與 Lv、轉職、熟練度、性別／屬性／出生地、HP／SP／EXP
export const RegistrationCard = ({ data, onOpenJobChange }: Props) => {
  const card = data === null ? null : characterCard(data.character, data.options)
  const value = (text: string | undefined) => (
    <span className="registration__value">{text ?? ""}</span>
  )
  return (
    <div className="registration" role="group" aria-label="冒險者登記證" aria-busy={card === null}>
      <h2 className="registration__name">{value(card?.name)}</h2>
      <div className="registration__job">
        {data !== null && (
          <ContentImage category="job-icons" assetKey={data.character.jobId} alt="" />
        )}
        <b>{value(card?.jobName)}</b>
        <em>{value(card?.level)}</em>
        {card !== null && (
          <button type="button" className="registration__change" onClick={onOpenJobChange}>
            轉職
          </button>
        )}
      </div>
      <div className="registration__proficiency">
        <span>
          熟練度 <b>{value(card?.proficiencyPercent)}</b>
        </span>
        {value(card?.proficiencyText)}
      </div>
      <Bar ratio={card?.proficiencyRatio ?? 0} tone="proficiency" />
      <dl className="registration__info">
        <div>
          <dt>性別</dt>
          <dd>{value(card?.sexName)}</dd>
        </div>
        <div>
          <dt>屬性</dt>
          <dd>{value(card?.elementName)}</dd>
        </div>
        <div className="registration__info-wide">
          <dt>出生地</dt>
          <dd>{value(card?.birthName)}</dd>
        </div>
      </dl>
      <div className="registration__gauges">
        {(
          [
            ["HP", "hp", card?.healthPoints],
            ["SP", "sp", card?.manaPoints],
            ["EXP", "exp", card?.experience],
          ] as const
        ).map(([label, tone, gauge]) => (
          <div key={tone} className={`registration__gauge registration__gauge--${tone}`}>
            <p>
              <span>{label}</span>
              {value(gauge?.text)}
            </p>
            <Bar ratio={gauge?.ratio ?? 0} tone={tone} />
          </div>
        ))}
      </div>
    </div>
  )
}
