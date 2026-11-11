import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode } from "react"
import { clearAccessToken } from "../../api/authStorage"
import { ApiError, type ApiClient } from "../../api/client"
import cardBlank from "../../assets/title/card-blank.webp"
import cardSealed from "../../assets/title/card-sealed.webp"
import clerk from "../../assets/title/clerk.webp"
import type { CreationOptionsVo } from "../../game/creationOptions"
import {
  acceptName,
  adjustStat,
  canConfirmStats,
  confirmStats,
  emptyDraft,
  isReady,
  pointsLeft,
  previewStats,
  toCommand,
  type AllocationStat,
  type CreationDraft,
} from "../../game/creationDraft"
import type { JobVo } from "../../game/jobBook"
import { baseStatLabels, baseStatsFromAllocation } from "../../game/stats"
import type { CharacterVo } from "../../game/character"
import { PassageMilliseconds } from "../../motion/timings"
import { ContentImage } from "../ContentImage"
import { Passage } from "../title/Passage"
import { Seal } from "../title/Seal"
import { elementIcons } from "./art"
import { birthLines, jobFlavors, Lines, Questions, sexAddress } from "./flavor"
import { JobBook } from "./JobBook"
import "./CreateScreen.css"

export type CreateApi = Pick<ApiClient, "createCharacter" | "getMyCharacter">

type Props = {
  api: CreateApi
  jobs: readonly JobVo[]
  options: CreationOptionsVo
  account: string
  onCreated: (character: CharacterVo) => void
  onLeave: () => void
}

// idle：都沒打開；其餘是正在回答的那一欄（job 是職業介紹冊、leave 是離開確認）
type Step = "idle" | "name" | "sex" | "birth" | "element" | "job" | "stats" | "leave"

const askingSteps: readonly Step[] = ["name", "sex", "birth", "element", "stats", "leave"]

const allocationStats: readonly AllocationStat[] = ["strength", "intelligence", "life", "agility"]

// 可分配的四項，名稱與顯示順序同角色頁
const statRows = baseStatLabels.filter((row): row is { key: AllocationStat; label: string } =>
  (allocationStats as readonly string[]).includes(row.key),
)

const optionName = (options: readonly { id: number; name: string }[], id: number | null) =>
  options.find((option) => option.id === id)?.name ?? ""

// 建角：公會櫃檯，莉瑟問、玩家自己點登記證的欄位來填，不照順序
export const CreateScreen = ({ api, jobs, options, onCreated, onLeave }: Props) => {
  const firstTierJobs = jobs.filter((job) => job.tier === 1)
  const [draft, setDraft] = useState<CreationDraft>(emptyDraft)
  const [step, setStep] = useState<Step>("idle")
  const [say, setSay] = useState<string | null>(null)
  const [nameInput, setNameInput] = useState("")
  const [page, setPage] = useState(firstTierJobs[0]?.id ?? "")
  const [busy, setBusy] = useState(false)
  // 送出後莉瑟的回覆（400 的訊息、沒有回應）；點任何一欄或再按出發就換掉
  const [notice, setNotice] = useState<string | null>(null)
  const [created, setCreated] = useState<CharacterVo | null>(null)
  const passageTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  useEffect(() => () => clearTimeout(passageTimer.current), [])

  const ready = isReady(draft)
  const left = pointsLeft(draft)
  const { maxHealthPoints, maxManaPoints } = previewStats(draft)
  const baseStats = baseStatsFromAllocation(draft.allocation)
  const asking = askingSteps.includes(step)

  const open = (next: Step) => {
    setSay(null)
    setNotice(null)
    if (step === next) {
      setStep("idle")
      return
    }
    setStep(next)
    setNameInput(draft.name ?? "")
    setPage(draft.jobId ?? page)
  }

  const close = () => {
    setStep("idle")
    setSay(null)
  }

  const answer = (next: CreationDraft, line: string | null = null) => {
    setDraft(next)
    setStep("idle")
    setSay(line)
    setNotice(null)
  }

  const submitName = () => {
    const next = acceptName(draft, nameInput)
    if (next === null) setSay(Lines.badName)
    else answer(next, Lines.goodName(next.name ?? ""))
  }

  const adjust = (stat: AllocationStat, delta: 1 | -1) => {
    setDraft(adjustStat(draft, stat, delta))
    setSay(null)
  }

  // 成功：印章退場、蓋上蠟封，「踏上旅途＋名字」停一下再進遊戲
  const depart = async () => {
    if (!ready || busy) return
    setBusy(true)
    setNotice(null)
    const enter = (character: CharacterVo) => {
      setCreated(character)
      passageTimer.current = setTimeout(() => onCreated(character), PassageMilliseconds)
    }
    try {
      enter(await api.createCharacter(toCommand(draft)))
    } catch (error) {
      // 帳號已經有角色（例如上一次出發其實建好了、只是回應逾時）：讀那個角色，照成功進遊戲
      const existing =
        error instanceof ApiError && error.reasons.includes("accountHasCharacter")
          ? await api.getMyCharacter().catch(() => null)
          : null
      if (existing !== null) {
        enter(existing)
        return
      }
      setBusy(false)
      setSay(null)
      if (
        error instanceof ApiError &&
        error.status === 409 &&
        !error.reasons.includes("accountHasCharacter")
      ) {
        // 名字重複：打開名字欄讓玩家改
        setStep("name")
        setNameInput(draft.name ?? "")
        setSay(Lines.nameTaken(draft.name ?? ""))
        return
      }
      setStep("idle")
      setNotice(error instanceof ApiError && error.status === 400 ? error.detail : Lines.noResponse)
    }
  }

  const leave = () => {
    clearAccessToken()
    onLeave()
  }

  const filled = [
    draft.name,
    draft.sex,
    draft.birth,
    draft.element,
    draft.jobId,
    draft.statsConfirmed || null,
  ]
  const idleLine = ready
    ? Lines.allDone(draft.name ?? "")
    : (say ?? (filled.some((value) => value !== null) ? Lines.keepGoing : Lines.greeting))
  const line =
    step === "idle"
      ? (notice ?? idleLine)
      : (say ?? (step === "job" ? Questions.job : Questions[step]))

  const job = firstTierJobs.find((candidate) => candidate.id === draft.jobId)
  const rows: { key: Step; label: string; value: string; icon?: ReactNode }[] = [
    { key: "name", label: "名字", value: draft.name ?? "" },
    { key: "sex", label: "性別", value: optionName(options.sexes, draft.sex) },
    { key: "birth", label: "出身", value: optionName(options.births, draft.birth) },
    {
      key: "element",
      label: "屬性",
      value: optionName(options.elements, draft.element),
      icon: draft.element !== null && <img src={elementIcons[draft.element]} alt="" />,
    },
    {
      key: "job",
      label: "職業",
      value: job?.name ?? "",
      icon: job !== undefined && <ContentImage category="job-icons" assetKey={job.id} alt="" />,
    },
  ]

  const onEnter = (event: KeyboardEvent) => {
    if (event.key === "Enter") submitName()
  }

  return (
    <section
      className={`create${created !== null ? " create--departing" : ""}`}
      aria-label="建立角色"
    >
      <div className="create-hall" aria-hidden="true" />
      <button
        type="button"
        className="create-back"
        aria-label="離開，回到標題"
        disabled={busy}
        onClick={() => open("leave")}
      >
        <svg
          aria-hidden="true"
          width="18"
          height="18"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.4"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M14.5 5.5c-2.2 2.3-4.4 4.4-6.5 6.4 2 2.1 4.2 4.3 6.6 6.6" />
        </svg>
        離開
      </button>
      <img className="create-clerk" src={clerk} alt="公會接待員 莉瑟" />
      <p className="create-bubble" aria-live="polite" data-testid="lise-line">
        {line}
      </p>
      <div className="create-desk" aria-hidden="true" />

      {asking && <div className="create-dim" aria-hidden="true" onClick={close} />}
      {asking && (
        <div className="create-ask" role="dialog" aria-label="回答">
          <div className={`create-options${step === "element" ? " create-options--three" : ""}`}>
            {step === "name" && (
              <>
                <input
                  className="create-name"
                  type="text"
                  value={nameInput}
                  maxLength={24}
                  aria-label="名字"
                  onChange={(event) => {
                    setNameInput(event.target.value)
                    setSay(null)
                  }}
                  onKeyDown={onEnter}
                />
                <button type="button" className="create-chip create-chip--go" onClick={submitName}>
                  就這樣
                </button>
              </>
            )}
            {step === "sex" &&
              options.sexes.map((sex) => (
                <button
                  key={sex.id}
                  type="button"
                  className={`create-chip create-chip--wide${draft.sex === sex.id ? " create-chip--on" : ""}`}
                  onClick={() => answer({ ...draft, sex: sex.id })}
                >
                  {sexAddress[sex.id] ?? sex.name}
                </button>
              ))}
            {step === "birth" &&
              options.births.map((birth) => (
                <button
                  key={birth.id}
                  type="button"
                  className={`create-chip create-chip--wide${draft.birth === birth.id ? " create-chip--on" : ""}`}
                  onClick={() =>
                    answer({ ...draft, birth: birth.id }, birthLines[birth.id] ?? null)
                  }
                >
                  {birth.name}
                </button>
              ))}
            {step === "element" &&
              options.elements.map((element) => (
                <button
                  key={element.id}
                  type="button"
                  className={`create-chip${draft.element === element.id ? " create-chip--on" : ""}`}
                  aria-pressed={draft.element === element.id}
                  onClick={() =>
                    answer(
                      { ...draft, element: element.id },
                      Lines.element(
                        element.name,
                        options.elementName(element.strongAgainst) ?? "",
                        options.elementName(element.weakAgainst) ?? "",
                      ),
                    )
                  }
                >
                  <img className="create-element-icon" src={elementIcons[element.id]} alt="" />
                  {element.name}
                </button>
              ))}
            {step === "leave" && (
              <>
                <button type="button" className="create-chip create-chip--wide" onClick={close}>
                  繼續填寫
                </button>
                <button
                  type="button"
                  className="create-chip create-chip--go create-chip--wide"
                  onClick={leave}
                >
                  先離開
                </button>
              </>
            )}
            {step === "stats" && (
              <>
                {statRows.map(({ key, label }) => (
                  <div key={key} className="create-stat">
                    <span>{label}</span>
                    <button
                      type="button"
                      aria-label={`減少${label}`}
                      disabled={draft.allocation[key] <= 1}
                      onClick={() => adjust(key, -1)}
                    >
                      −
                    </button>
                    <b>{baseStats[key]}</b>
                    <button
                      type="button"
                      aria-label={`增加${label}`}
                      disabled={left <= 0}
                      onClick={() => adjust(key, 1)}
                    >
                      ＋
                    </button>
                  </div>
                ))}
                <span className="create-left">剩 {left} 點</span>
                <button
                  type="button"
                  className="create-chip create-chip--go create-chip--wide"
                  disabled={!canConfirmStats(draft)}
                  onClick={() => answer(confirmStats(draft))}
                >
                  就這樣
                </button>
              </>
            )}
          </div>
        </div>
      )}

      <div className="create-card">
        <img src={cardBlank} alt="" />
        <img className="create-wax" src={cardSealed} alt="" />
        <div className="create-card-inside">
          <h2>冒險者登記證</h2>
          <hr />
          {rows.map((row) => (
            <button
              key={row.key}
              type="button"
              className={`create-row${step === row.key ? " create-row--now" : ""}`}
              aria-label={row.value === "" ? row.label : `${row.label} ${row.value}`}
              disabled={busy}
              onClick={() => open(row.key)}
            >
              <span>{row.label}</span>
              <b>
                {row.icon}
                {row.value}
              </b>
            </button>
          ))}
          <button
            type="button"
            className={`create-row create-row--stats${step === "stats" ? " create-row--now" : ""}`}
            aria-label="能力"
            disabled={busy}
            onClick={() => open("stats")}
          >
            <span>
              能力
              {left > 0 && <em className="create-owe">剩 {left} 點</em>}
            </span>
            <b>
              {statRows.map(({ key, label }) => (
                <span key={key}>
                  <i>{label}</i>
                  {baseStats[key]}
                </span>
              ))}
            </b>
          </button>
          <div className="create-hpsp" aria-label="生命與精神上限">
            <span>
              <i>HP</i>
              {maxHealthPoints}
            </span>
            <span>
              <i>SP</i>
              {maxManaPoints}
            </span>
          </div>
          <Seal
            label="出發"
            className="seal--glow"
            disabled={!ready || busy}
            onClick={() => void depart()}
          />
        </div>
      </div>

      <JobBook
        open={step === "job"}
        jobs={firstTierJobs}
        page={page}
        sex={draft.sex}
        onPage={(jobId) => {
          setPage(jobId)
          setSay(jobFlavors[jobId]?.line ?? null)
        }}
        onPick={(picked) => answer({ ...draft, jobId: picked.id }, Lines.jobPicked(picked.name))}
      />
      {created !== null && <Passage title="踏上旅途" subtitle={created.name} />}
    </section>
  )
}
