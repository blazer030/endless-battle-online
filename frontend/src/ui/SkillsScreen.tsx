import { useCallback, useEffect, useId, useState } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  ActiveSlots,
  activationText,
  clearSlot,
  cooldownText,
  draftsFrom,
  durationText,
  equippedActivation,
  jobReasonText,
  learnReasonText,
  markCurrentJob,
  PassiveSlots,
  setSlot,
  slotReasonText,
  timingText,
  toSlotCommands,
  validateLoadout,
} from "../game/skills"
import type { SlotDraft, SlotNumber } from "../game/skills"
import { statLabel } from "../game/stats"
import type { CharacterView, JobOption, SkillOverview, SkillView } from "../game/types"
import { formatNumber } from "../game/format"
import "./SkillsScreen.css"
import { usePanelRefresh } from "./usePanelRefresh"

export type SkillsApi = Pick<
  ApiClient,
  "getJobs" | "changeJob" | "getSkills" | "learnSkill" | "setSkillSlots" | "getMyCharacter"
>

type Props = {
  api: SkillsApi
  character: CharacterView
  onCharacterChange: (character: CharacterView) => void
  // 戰報有升級或熟練度時遞增，開著的技能頁跟著重新載入
  refreshSignal?: number
}

const fallbackError = "發生錯誤，請稍後再試"

// 錯誤訊息後面接上原因代碼的中文
const describeError = (error: unknown, reasonText: (code: string) => string) => {
  if (!(error instanceof ApiError)) return fallbackError
  return error.reasons.length === 0
    ? error.detail
    : `${error.detail}：${error.reasons.map(reasonText).join("、")}`
}

const tierLabel = (tier: number) => (tier === 1 ? "基礎" : "進階")

type Requirement = { text: string; met: boolean }

const requirementsOf = (job: JobOption, jobs: JobOption[]): Requirement[] => {
  const unmet = new Set(job.reasons)
  const rows: Requirement[] = [{ text: `Lv ${job.unlockLevel}`, met: !unmet.has("levelTooLow") }]
  if (job.parentId !== null) {
    const parent = jobs.find((candidate) => candidate.id === job.parentId)
    rows.push({
      text: `${job.parentName ?? job.parentId}熟練度 ${parent?.proficiencyPercent ?? 0}%／100%`,
      met: !unmet.has("parentProficiencyTooLow"),
    })
  }
  for (const { stat, minimum } of job.statRequirements) {
    rows.push({
      text: `${statLabel(stat)} ≥ ${formatNumber(minimum)}`,
      met: !unmet.has(`statTooLow:${stat}`),
    })
  }
  return rows
}

const ReloadAfterChangeFailed = "已轉職，重新載入失敗，請稍後再試"

type Loaded = { jobs: JobOption[]; overview: SkillOverview }

const loadAll = async (api: SkillsApi): Promise<Loaded> => {
  const [jobs, overview] = await Promise.all([api.getJobs(), api.getSkills()])
  return { jobs, overview }
}

const reasonsTitle = (reasons: string[], text: (code: string) => string) =>
  reasons.length === 0 ? undefined : reasons.map(text).join("、")

export function SkillsScreen({ api, character, onCharacterChange, refreshSignal = 0 }: Props) {
  const jobsId = useId()
  const skillsId = useId()
  const slotsId = useId()
  const [jobs, setJobs] = useState<JobOption[] | null>(null)
  const [overview, setOverview] = useState<SkillOverview | null>(null)
  const [drafts, setDrafts] = useState<SlotDraft[]>([])
  const [loadError, setLoadError] = useState(false)
  const [busy, setBusy] = useState(false)
  const [jobError, setJobError] = useState<string | null>(null)
  const [learnError, setLearnError] = useState<string | null>(null)
  const [slotError, setSlotError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const show = useCallback((loaded: Loaded) => {
    setJobs(loaded.jobs)
    setOverview(loaded.overview)
    setDrafts(draftsFrom(loaded.overview))
    setLoadError(false)
  }, [])

  const load = async () => show(await loadAll(api))

  useEffect(() => {
    let isCurrent = true
    loadAll(api).then(
      (loaded) => {
        if (isCurrent) show(loaded)
      },
      () => {
        if (isCurrent) setLoadError(true)
      },
    )
    return () => {
      isCurrent = false
    }
  }, [api, show])

  // 背景重載只換職業與技能資料，不動還沒儲存的欄位；失敗就保留目前內容
  usePanelRefresh(refreshSignal, () => {
    loadAll(api).then(
      (loaded) => {
        setJobs(loaded.jobs)
        setOverview(loaded.overview)
      },
      () => {},
    )
  })

  // 同一時間只送一個請求，避免轉職與存欄位互相覆蓋
  const run = async (action: () => Promise<void>, onError: (error: unknown) => void) => {
    setBusy(true)
    try {
      await action()
    } catch (error) {
      onError(error)
    } finally {
      setBusy(false)
    }
  }

  const changeJob = (jobId: string) =>
    run(
      async () => {
        setJobError(null)
        const changed = await api.changeJob(jobId)
        onCharacterChange(changed)
        // 轉職已成功；重新載入失敗時先在本地標出新職業，不當成轉職失敗
        setJobs((current) => (current === null ? current : markCurrentJob(current, changed.jobId)))
        await load().catch(() => setJobError(ReloadAfterChangeFailed))
      },
      (error) => setJobError(describeError(error, jobReasonText)),
    )

  const learn = (skillId: string) =>
    run(
      async () => {
        setLearnError(null)
        setOverview(await api.learnSkill(skillId))
        // 學技能會扣金錢，重新讀角色讓頂欄同步；失敗時等下一次推進校正
        const refreshed = await api.getMyCharacter().catch(() => null)
        if (refreshed !== null) onCharacterChange(refreshed)
      },
      (error) => setLearnError(describeError(error, learnReasonText)),
    )

  const saveSlots = () =>
    run(
      async () => {
        setSlotError(null)
        setSaved(false)
        const updated = await api.setSkillSlots(toSlotCommands(drafts))
        setOverview(updated)
        setDrafts(draftsFrom(updated))
        setSaved(true)
      },
      (error) => setSlotError(describeError(error, slotReasonText)),
    )

  const editDrafts = (next: SlotDraft[]) => {
    setDrafts(next)
    setSaved(false)
  }

  if (loadError) {
    return (
      <div className="skills">
        <p className="error" role="alert">
          無法載入技能資料。
        </p>
        <div className="actions">
          <button type="button" onClick={() => void load().catch(() => setLoadError(true))}>
            重試
          </button>
        </div>
      </div>
    )
  }
  if (jobs === null || overview === null) {
    return <p className="skills__loading">載入中…</p>
  }

  const loadoutReasons = validateLoadout(drafts, overview)
  const learnedSkills = overview.skills.filter((skill) => skill.level > 0)
  // 主動與被動欄都不限職業：學過就能裝
  const activeOptions = learnedSkills.filter((skill) => skill.type === "Active")
  const passiveOptions = learnedSkills.filter((skill) => skill.type === "Passive")

  return (
    <div className="skills">
      <section className="skills__card" aria-labelledby={jobsId}>
        <h2 id={jobsId} className="skills__card-title">
          職業
        </h2>
        <ul className="skills__list">
          {jobs.map((job) => (
            <li key={job.id} className="skills__item">
              <div className="skills__item-head">
                <h3 className="skills__item-name">{`${job.name}（${tierLabel(job.tier)}）`}</h3>
                {job.current && <span className="skills__badge">目前</span>}
              </div>
              <ul className="skills__requirements">
                {requirementsOf(job, jobs).map(({ text, met }) => (
                  <li key={text} className="skills__requirement" data-met={met}>
                    <span
                      className="skills__mark"
                      role="img"
                      aria-label={met ? "已達成" : "未達成"}
                    >
                      {met ? "✓" : "✗"}
                    </span>
                    {text}
                  </li>
                ))}
              </ul>
              <button
                type="button"
                className="skills__action"
                disabled={!job.eligible || busy}
                title={reasonsTitle(job.reasons, jobReasonText)}
                onClick={() => void changeJob(job.id)}
              >
                轉職
              </button>
            </li>
          ))}
        </ul>
        {jobError !== null && (
          <p className="error" role="alert">
            {jobError}
          </p>
        )}
      </section>

      <section className="skills__card" aria-labelledby={skillsId}>
        <h2 id={skillsId} className="skills__card-title">
          技能
        </h2>
        <p className="skills__points">
          <span>{`技能熟練度 ${formatNumber(overview.skillPoints)}`}</span>
          <span>{`金錢 ${formatNumber(character.money)}`}</span>
        </p>
        <ul className="skills__list">
          {overview.skills.map((skill) => (
            <SkillCard key={skill.id} skill={skill} busy={busy} onLearn={learn} />
          ))}
        </ul>
        {learnError !== null && (
          <p className="error" role="alert">
            {learnError}
          </p>
        )}
      </section>

      <section className="skills__card" aria-labelledby={slotsId}>
        <h2 id={slotsId} className="skills__card-title">
          欄位
        </h2>
        <h3 className="skills__group-title">主動 1～3</h3>
        {ActiveSlots.map((slot) => (
          <SlotRow
            key={slot}
            draft={draftOf(drafts, slot)}
            activation={equippedActivation(drafts, overview, slot)}
            options={activeOptions}
            onChange={editDrafts}
            drafts={drafts}
            disabled={busy}
          />
        ))}
        <h3 className="skills__group-title">被動 4～5</h3>
        {PassiveSlots.map((slot) => (
          <SlotRow
            key={slot}
            draft={draftOf(drafts, slot)}
            activation={null}
            options={passiveOptions}
            onChange={editDrafts}
            drafts={drafts}
            disabled={busy}
          />
        ))}
        {loadoutReasons.length > 0 && (
          <ul className="skills__reasons">
            {loadoutReasons.map((code) => (
              <li key={code}>{slotReasonText(code)}</li>
            ))}
          </ul>
        )}
        <div className="actions">
          <button
            type="button"
            disabled={busy || loadoutReasons.length > 0}
            onClick={() => void saveSlots()}
          >
            儲存欄位
          </button>
        </div>
        {saved && (
          <p className="skills__saved" role="status">
            欄位已儲存。
          </p>
        )}
        {slotError !== null && (
          <p className="error" role="alert">
            {slotError}
          </p>
        )}
      </section>
    </div>
  )
}

const draftOf = (drafts: SlotDraft[], slot: SlotNumber): SlotDraft =>
  drafts.find((draft) => draft.slot === slot) ?? { slot, skillId: null }

const joinParts = (parts: (string | null)[]) =>
  parts.filter((part): part is string => part !== null).join("・")

type LevelNumbers = { rounds: number; chance: number; cooldown: number }

// 觸發型被動（有發動條件）列時機・條件・持續・冷卻；常駐被動只有冷卻（通常為 0、不顯示）
const passiveParts = (skill: SkillView, level: LevelNumbers): (string | null)[] =>
  skill.trigger === null
    ? [cooldownText(level.cooldown)]
    : [
        timingText(skill.timing),
        activationText(skill.trigger, level.chance),
        durationText(level.rounds),
        cooldownText(level.cooldown),
      ]

// 下一級：主動列 SP、持續回合、發動條件與冷卻；被動見 passiveParts；最後是學習費用
const nextLevelText = (skill: SkillView, next: NonNullable<SkillView["next"]>) => {
  const cost = `${formatNumber(next.money)} 金錢・${formatNumber(next.proficiency)} 熟練度`
  return skill.type === "Active"
    ? joinParts([
        `SP ${next.sp}`,
        `${next.rounds} 回合`,
        activationText(skill.trigger, next.chance),
        cooldownText(next.cooldown),
        cost,
      ])
    : joinParts([...passiveParts(skill, next), cost])
}

// 已學技能在標題列顯示目前等級的特性：主動顯示發動條件與冷卻，被動見 passiveParts
const currentParts = (skill: SkillView): string[] =>
  skill.level === 0
    ? []
    : (skill.type === "Active"
        ? [activationText(skill.trigger, skill.chance), cooldownText(skill.cooldown)]
        : passiveParts(skill, skill)
      ).filter((part): part is string => part !== null)

function SkillCard({
  skill,
  busy,
  onLearn,
}: {
  skill: SkillView
  busy: boolean
  onLearn: (skillId: string) => void
}) {
  const { next } = skill
  const current = currentParts(skill)
  return (
    <li className="skills__item">
      <div className="skills__item-head">
        <h3 className="skills__item-name">{`${skill.name} Lv ${skill.level}/${skill.maxLevel}`}</h3>
        <span className="skills__type">{skill.type === "Active" ? "主動" : "被動"}</span>
        {current.map((part) => (
          <span key={part} className="skills__chance-value">
            {part}
          </span>
        ))}
      </div>
      <p className="skills__description">{skill.description}</p>
      {next === null ? (
        <p className="skills__next">已達最高等級</p>
      ) : (
        <>
          <p className="skills__next">{nextLevelText(skill, next)}</p>
          {!next.canLearn && (
            <p className="skills__unmet">{next.reasons.map(learnReasonText).join("、")}</p>
          )}
          <button
            type="button"
            className="skills__action"
            disabled={!next.canLearn || busy}
            title={reasonsTitle(next.reasons, learnReasonText)}
            onClick={() => onLearn(skill.id)}
          >
            {skill.level === 0 ? "學習" : "升級"}
          </button>
        </>
      )}
    </li>
  )
}

// activation 是主動欄裝入技能的發動條件（發動率或 HP 門檻），空欄與被動欄為 null
function SlotRow({
  draft,
  drafts,
  activation,
  options,
  onChange,
  disabled,
}: {
  draft: SlotDraft
  drafts: SlotDraft[]
  activation: string | null
  options: SkillView[]
  onChange: (drafts: SlotDraft[]) => void
  disabled: boolean
}) {
  const { slot, skillId } = draft
  const label = `欄位 ${slot}`
  return (
    <div className="skills__slot">
      <span className="skills__slot-label">{label}</span>
      <select
        className="skills__select"
        aria-label={`${label} 技能`}
        disabled={disabled}
        value={skillId ?? ""}
        onChange={(event) =>
          onChange(
            event.target.value === ""
              ? clearSlot(drafts, slot)
              : setSlot(drafts, slot, event.target.value),
          )
        }
      >
        <option value="">（空）</option>
        {options.map((option) => (
          <option key={option.id} value={option.id}>
            {`${option.name} Lv ${option.level}`}
          </option>
        ))}
      </select>
      {activation !== null && <span className="skills__chance-value">{activation}</span>}
    </div>
  )
}
