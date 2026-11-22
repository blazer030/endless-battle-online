import type { LearnedPopup as LearnedPopupData } from "../../game/characterScreen"
import type { LearnableSkill } from "../../game/skillOverview"
import { ContentImage } from "../ContentImage"
import { Popup } from "../feedback/Popup"

type Props = { popup: LearnedPopupData; skill: LearnableSkill; onClose: () => void }

// 升級成功／習得成功：技能圖示（滿級加「精通」章）、名字、等級、菱形、新等級的數值（跟上一級不同的綠字）
export const LearnedPopup = ({ popup, skill, onClose }: Props) => (
  <Popup label={popup.title} className="learned-popup" onClose={onClose}>
    <h3 className="learned-popup__title">
      <i />
      {popup.title}
      <i />
    </h3>
    <span
      className={`learned-popup__disc skill-card__disc skill-card__disc--${skill.slotType === "Active" ? "active" : "passive"}`}
    >
      <ContentImage category="skills" assetKey={skill.icon ?? skill.id} alt="" />
      {popup.mastered && <span className="skill-card__mastered learned-popup__mastered">精通</span>}
    </span>
    <span className="learned-popup__name">{popup.name}</span>
    <span className="learned-popup__level" aria-label={popup.levelText}>
      {popup.fromLevel !== null && (
        <>
          <span className="learned-popup__from">Lv {popup.fromLevel}</span>
          <span className="learned-popup__from">→</span>
        </>
      )}
      <span className="learned-popup__to">Lv {popup.toLevel}</span>
      <small>/ {popup.maxLevel}</small>
    </span>
    <span className="skill-pips learned-popup__pips" aria-hidden="true">
      {popup.pips.map((lit, index) => (
        <i key={index} className={lit ? "skill-pips__lit" : undefined} />
      ))}
    </span>
    <div className="learned-popup__rows">
      {popup.rows.map((row) => (
        <div key={row.label} className="skill-card__row">
          <span>{row.label}</span>
          <b className={row.changed ? "skill-card__value--changed" : undefined}>{row.text}</b>
        </div>
      ))}
    </div>
  </Popup>
)
