import { useId, useState, type FormEvent, type ReactNode } from "react"
import { ApiError, type ApiClient } from "../api/client"
import {
  AllocationTotalPoints,
  baseStatsFromAllocation,
  deriveStats,
  describeTopGrowth,
  MinimumAllocation,
} from "../game/stats"
import {
  CharacterNameMaxLength,
  isNameTooLong,
  NameLengthError,
  SpecialAttackNameMaxLength,
} from "../game/names"
import type { CharacterView, ContentNames, StatAllocation } from "../game/types"
import "./CreateCharacterScreen.css"

type Props = {
  api: Pick<ApiClient, "createCharacter">
  content: Pick<ContentNames, "jobs" | "elements" | "births" | "sexes">
  onCreated: (character: CharacterView) => void
}

type Option = { id: string; name: string }

const allocatableStats: { key: keyof StatAllocation; label: string }[] = [
  { key: "str", label: "力量" },
  { key: "smart", label: "智力" },
  { key: "life", label: "生命" },
  { key: "agi", label: "敏捷" },
]

const startingAllocation: StatAllocation = { str: 1, smart: 1, life: 1, agi: 1 }

const totalOf = (allocation: StatAllocation) =>
  Object.values(allocation).reduce((sum, points) => sum + points, 0)

// 內容的選項 id 是 "1"～"N"，請求要的是數字
const firstNumericId = (options: Option[]) => (options.length > 0 ? Number(options[0].id) : 1)

const ChoiceGroup = <T extends Option>({
  legend,
  options,
  selectedId,
  onSelect,
  className,
  renderOption = (option) => option.name,
}: {
  legend: string
  options: T[]
  selectedId: string
  onSelect: (id: string) => void
  className: string
  renderOption?: (option: T) => ReactNode
}) => (
  <fieldset className="choice-group">
    <legend>{legend}</legend>
    <div className={className}>
      {options.map((option) => (
        <button
          key={option.id}
          type="button"
          className="choice"
          data-option={option.id}
          aria-pressed={option.id === selectedId}
          onClick={() => onSelect(option.id)}
        >
          {renderOption(option)}
        </button>
      ))}
    </div>
  </fieldset>
)

type JobContent = ContentNames["jobs"][number]

// 內容只有數值，初階職業的定位文字在此維護；成長文字由 content 的 growth 算出
const jobRoles: Readonly<Partial<Record<string, string>>> = {
  warrior: "近身前排，攻守兼備",
  sorcerer: "遠距施法，魔攻見長",
  thief: "身手敏捷，先發制人",
  bard: "樂聲支援，攻守靈活",
}

const JobCard = ({ job }: { job: JobContent }) => {
  const role = jobRoles[job.id]
  return (
    <>
      <strong className="job-name">{job.name}</strong>
      {role !== undefined && <span className="job-role">{role}</span>}
      <span className="job-growth">{`成長：${describeTopGrowth(job.growth)}`}</span>
    </>
  )
}

export const CreateCharacterScreen = ({ api, content, onCreated }: Props) => {
  const startingJobs = content.jobs.filter((job) => job.tier === 1)
  const [name, setName] = useState("")
  const [specialAttackName, setSpecialAttackName] = useState("")
  const [sex, setSex] = useState(() => firstNumericId(content.sexes))
  const [element, setElement] = useState(() => firstNumericId(content.elements))
  const [birth, setBirth] = useState(() => firstNumericId(content.births))
  const [jobId, setJobId] = useState(() => (startingJobs.length > 0 ? startingJobs[0].id : ""))
  const [allocation, setAllocation] = useState(startingAllocation)
  const [errorDetail, setErrorDetail] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const nameErrorId = useId()
  const specialAttackErrorId = useId()
  const nameTooLong = isNameTooLong(name, CharacterNameMaxLength)
  const specialAttackTooLong = isNameTooLong(specialAttackName, SpecialAttackNameMaxLength)

  const remaining = AllocationTotalPoints - totalOf(allocation)
  const preview = deriveStats(baseStatsFromAllocation(allocation))

  const adjust = (key: keyof StatAllocation, delta: number) =>
    setAllocation((current) => {
      const next = { ...current, [key]: current[key] + delta }
      const isWithinBounds =
        next[key] >= MinimumAllocation && totalOf(next) <= AllocationTotalPoints
      return isWithinBounds ? next : current
    })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (nameTooLong || specialAttackTooLong) return
    setIsBusy(true)
    setErrorDetail(null)
    try {
      onCreated(
        await api.createCharacter({
          name,
          sex,
          element,
          birth,
          jobId,
          allocation,
          specialAttackName,
        }),
      )
    } catch (error) {
      setErrorDetail(error instanceof ApiError ? error.detail : "發生錯誤，請稍後再試")
      setIsBusy(false)
    }
  }

  return (
    <main className="screen create-screen">
      <h1 className="title">EBO</h1>
      <p className="tagline">Endless Battle Online</p>
      <p className="subtitle">建立你的冒險者</p>
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        <label>
          名稱
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            aria-invalid={nameTooLong || undefined}
            aria-describedby={nameTooLong ? nameErrorId : undefined}
            placeholder="2～12 字"
          />
        </label>
        {nameTooLong && (
          <p id={nameErrorId} className="error">
            {NameLengthError}
          </p>
        )}
        <label>
          必殺技名稱
          <input
            value={specialAttackName}
            onChange={(event) => setSpecialAttackName(event.target.value)}
            aria-invalid={specialAttackTooLong || undefined}
            aria-describedby={specialAttackTooLong ? specialAttackErrorId : undefined}
            placeholder="1～8 字"
          />
        </label>
        {specialAttackTooLong && (
          <p id={specialAttackErrorId} className="error">
            {NameLengthError}
          </p>
        )}

        <ChoiceGroup
          legend="性別"
          className="choice-row"
          options={content.sexes}
          selectedId={String(sex)}
          onSelect={(id) => setSex(Number(id))}
        />
        <ChoiceGroup
          legend="屬性"
          className="choice-row element-row"
          options={content.elements}
          selectedId={String(element)}
          onSelect={(id) => setElement(Number(id))}
        />
        <ChoiceGroup
          legend="出生地"
          className="choice-grid"
          options={content.births}
          selectedId={String(birth)}
          onSelect={(id) => setBirth(Number(id))}
        />
        <ChoiceGroup
          legend="職業"
          className="choice-grid job-grid"
          options={startingJobs}
          selectedId={jobId}
          onSelect={setJobId}
          renderOption={(job) => <JobCard job={job} />}
        />

        <fieldset className="choice-group">
          <legend>能力配點</legend>
          <p className="points-left">剩餘 {remaining} 點</p>
          <div className="allocation">
            <ul className="steppers">
              {allocatableStats.map(({ key, label }) => (
                <li key={key} className="stepper">
                  <span className="stepper-label">{label}</span>
                  <button
                    type="button"
                    aria-label={`減少${label}`}
                    disabled={allocation[key] <= MinimumAllocation}
                    onClick={() => adjust(key, -1)}
                  >
                    −
                  </button>
                  <span className="stepper-value">{allocation[key]}</span>
                  <button
                    type="button"
                    aria-label={`增加${label}`}
                    disabled={remaining <= 0}
                    onClick={() => adjust(key, 1)}
                  >
                    ＋
                  </button>
                </li>
              ))}
            </ul>
            <section className="preview" aria-label="能力預覽">
              <p>
                HP 上限 <strong>{preview.hpMax}</strong>
              </p>
              <p>
                SP 上限 <strong>{preview.spMax}</strong>
              </p>
            </section>
          </div>
        </fieldset>

        <div className="actions">
          <button type="submit" disabled={isBusy || nameTooLong || specialAttackTooLong}>
            踏上旅途
          </button>
        </div>
        {errorDetail !== null && (
          <p role="alert" className="error">
            {errorDetail}
          </p>
        )}
      </form>
    </main>
  )
}
