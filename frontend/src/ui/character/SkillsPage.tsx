import { useState } from "react"
import { jobSwitcher, skillCard, type DetailLine } from "../../game/characterScreen"
import type { LearnableSkill, SkillJob } from "../../game/skillOverview"
import { ContentImage } from "../ContentImage"

type Props = {
  jobs: readonly SkillJob[]
  // 按下升級／學習：交出技能與正在看的職業
  onLearn: (skill: LearnableSkill, job: SkillJob) => void
}

const Pips = ({ pips }: { pips: readonly boolean[] }) => (
  <span className="skill-pips" aria-hidden="true">
    {pips.map((lit, index) => (
      <i key={index} className={lit ? "skill-pips__lit" : undefined} />
    ))}
  </span>
)

const Column = ({
  title,
  rows,
  className = "",
}: {
  title: string
  rows: readonly DetailLine[] | null
  className?: string
}) => (
  <div className={`skill-card__column ${className}`}>
    <h4>{title}</h4>
    {rows === null ? (
      <div className="skill-card__none">尚未學習</div>
    ) : (
      rows.map((row) => (
        <div key={row.label} className="skill-card__row">
          <span>{row.label}</span>
          <b className={row.changed ? "skill-card__value--changed" : undefined}>{row.text}</b>
        </div>
      ))
    )}
  </div>
)

const SkillCardView = ({
  skill,
  open,
  onToggle,
  onLearn,
}: {
  skill: LearnableSkill
  open: boolean
  onToggle: () => void
  onLearn: () => void
}) => {
  const card = skillCard(skill)
  const { footer } = card
  const atMax = footer.kind === "max"
  return (
    <div className={`skill-card${open ? " skill-card--open" : ""}`}>
      <button type="button" className="skill-card__top" aria-expanded={open} onClick={onToggle}>
        <span
          className={`skill-card__disc skill-card__disc--${skill.slotType === "Active" ? "active" : "passive"}`}
        >
          <ContentImage category="skills" assetKey={skill.icon ?? skill.id} alt="" />
        </span>
        <span className="skill-card__body">
          <span className="skill-card__head">
            <span className="skill-card__name">{skill.name}</span>
            <span
              className={`skill-card__level${skill.level === 0 ? " skill-card__level--none" : ""}`}
            >
              {card.levelText}
            </span>
            <Pips pips={card.pips} />
            <span
              className={`skill-card__type skill-card__type--${skill.slotType === "Active" ? "active" : "passive"}`}
            >
              {card.typeLabel}
            </span>
          </span>
          <span className="skill-card__description">{card.description}</span>
        </span>
        <span className="skill-card__caret" aria-hidden="true" />
      </button>
      {open && (
        <div className="skill-card__detail">
          <div className={`skill-card__columns${atMax ? " skill-card__columns--one" : ""}`}>
            {atMax ? (
              <Column
                title={`目前 Lv ${skill.level}（最高）`}
                rows={card.currentRows}
                className="skill-card__column--max"
              />
            ) : (
              <>
                <Column
                  title={skill.level === 0 ? "目前" : `目前 Lv ${skill.level}`}
                  rows={card.currentRows}
                />
                <Column
                  title={`下一級 Lv ${skill.level + 1}`}
                  rows={card.nextRows}
                  className="skill-card__column--next"
                />
              </>
            )}
          </div>
          <div className="skill-card__foot">
            {footer.kind === "max" ? (
              <>
                <span className="skill-card__costs">已達最高等級</span>
                <span className="skill-card__mastered">精通</span>
              </>
            ) : (
              <>
                <span className="skill-card__costs">
                  <span>{footer.costLabel}</span>
                  <b className={footer.moneyShort ? "skill-card__cost--short" : undefined}>
                    {footer.money} 金錢
                  </b>
                  <b className={footer.skillPointsShort ? "skill-card__cost--short" : undefined}>
                    {footer.skillPoints} 技能點
                  </b>
                </span>
                <button
                  type="button"
                  className="skill-card__learn"
                  disabled={footer.disabled}
                  onClick={onLearn}
                >
                  {footer.button}
                </button>
                {footer.reasons !== null && <p className="skill-card__reasons">{footer.reasons}</p>}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

// 技能頁：上方切換正在看的職業（只列當過的、依 Class 分組），下面是那個職業的技能卡，一次展開一張
export const SkillsPage = ({ jobs, onLearn }: Props) => {
  const [shownJobId, setShownJobId] = useState(
    () => jobs.find((job) => job.current)?.jobId ?? jobs[0]?.jobId ?? "",
  )
  const [picking, setPicking] = useState(false)
  const [openSkillId, setOpenSkillId] = useState<string | null>(null)
  if (jobs.length === 0) return <div role="tabpanel" aria-label="技能" />
  const shown = jobs.find((job) => job.jobId === shownJobId) ?? jobs[0]
  const { header, groups } = jobSwitcher(jobs, shown.jobId)

  return (
    <div role="tabpanel" aria-label="技能" className="character-skills">
      <div className="job-switch">
        <button
          type="button"
          className="job-switch__button"
          aria-label={`切換職業：${header.name}`}
          aria-expanded={picking}
          onClick={() => setPicking((current) => !current)}
        >
          <span className="job-switch__disc">
            <ContentImage category="job-icons" assetKey={header.jobId} alt="" />
          </span>
          <span className="job-switch__body">
            <span className="job-switch__head">
              <b>{header.name}</b>
              {header.current && <small className="job-switch__now">現職</small>}
            </span>
            <span className="job-switch__subtitle">{header.subtitle}</span>
          </span>
          <span className="job-switch__points">
            技能點<b>{header.skillPoints}</b>
          </span>
          <span className="job-switch__caret" aria-hidden="true" />
        </button>
        {picking && (
          <div className="job-switch__picker" role="group" aria-label="當過的職業">
            {groups.map((group) => (
              <div key={group.title} className="job-switch__group">
                <div className="job-switch__group-title">
                  <i />
                  <span>{group.title}</span>
                  <small>{group.jobs.length}</small>
                  <i />
                </div>
                <div className="job-switch__grid">
                  {group.jobs.map((job) => (
                    <button
                      key={job.jobId}
                      type="button"
                      className="job-switch__job"
                      aria-pressed={job.selected}
                      onClick={() => {
                        setShownJobId(job.jobId)
                        setPicking(false)
                        setOpenSkillId(null)
                      }}
                    >
                      <ContentImage category="job-icons" assetKey={job.jobId} alt="" />
                      <span className="job-switch__job-text">
                        <b>{job.name}</b>
                        <small className={job.note === "現職" ? "job-switch__now" : undefined}>
                          {job.note}
                        </small>
                      </span>
                    </button>
                  ))}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
      {shown.skills.map((skill) => (
        <SkillCardView
          key={skill.id}
          skill={skill}
          open={openSkillId === skill.id}
          onToggle={() => setOpenSkillId((current) => (current === skill.id ? null : skill.id))}
          onLearn={() => onLearn(skill, shown)}
        />
      ))}
    </div>
  )
}
