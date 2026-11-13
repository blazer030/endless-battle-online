import { useEffect, useRef, useState } from "react"
import type { ApiClient } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import { restFeeText, stepThreshold } from "../../game/inn"
import { InnThresholdSaveDelayMilliseconds } from "../../motion/timings"

type Thresholds = { hp: number; sp: number }

type Props = {
  api: Pick<ApiClient, "updateSettings">
  character: CharacterVo
  restFee: number
  onCharacterChange: (character: CharacterVo) => void
  onStep: () => void
  onSaveFailed: () => void
}

const thresholdsOf = (character: CharacterVo): Thresholds => ({
  hp: character.settings.restHealthPointsPercent,
  sp: character.settings.restManaPointsPercent,
})

const Row = ({
  label,
  value,
  onStep,
}: {
  label: "HP" | "SP"
  value: number
  onStep: (direction: 1 | -1) => void
}) => (
  <div className={`inn-threshold inn-threshold--${label.toLowerCase()}`}>
    <span className="inn-threshold__tag">{label}</span>
    <span>低於</span>
    <button
      type="button"
      aria-label={`${label} 門檻減少`}
      disabled={stepThreshold(value, -1) === null}
      onClick={() => onStep(-1)}
    >
      −
    </button>
    <span className="inn-threshold__gauge">
      <i style={{ width: `${Math.max(0, value)}%` }} />
    </span>
    <button
      type="button"
      aria-label={`${label} 門檻增加`}
      disabled={stepThreshold(value, 1) === null}
      onClick={() => onStep(1)}
    >
      +
    </button>
    <b
      className={
        value < 0 ? "inn-threshold__value inn-threshold__value--off" : "inn-threshold__value"
      }
      data-testid={`threshold-${label.toLowerCase()}`}
    >
      {value < 0 ? "略過" : `${value}%`}
    </b>
  </div>
)

// 自動住宿：HP／SP 門檻按了就存（最後一次按下後延遲送出，離開時立刻送），失敗退回上次存好的值
export const InnAutoRest = ({
  api,
  character,
  restFee,
  onCharacterChange,
  onStep,
  onSaveFailed,
}: Props) => {
  const [values, setValues] = useState(() => thresholdsOf(character))
  const saved = useRef(thresholdsOf(character))
  const pending = useRef<Thresholds | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const alive = useRef(true)
  // 送出時用最新的角色設定，只換兩個門檻
  const latest = useRef({ api, character, onCharacterChange, onSaveFailed })
  useEffect(() => {
    latest.current = { api, character, onCharacterChange, onSaveFailed }
  })

  const save = (thresholds: Thresholds) => {
    pending.current = null
    const current = latest.current
    current.api
      .updateSettings({
        ...current.character.settings,
        restHealthPointsPercent: thresholds.hp,
        restManaPointsPercent: thresholds.sp,
      })
      .then(
        (confirmed) => {
          saved.current = thresholds
          latest.current.onCharacterChange(confirmed)
        },
        () => {
          if (!alive.current) return
          setValues(saved.current)
          latest.current.onSaveFailed()
        },
      )
  }

  // 離開旅館：還沒送出的修改立刻送
  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
      if (timer.current !== null) clearTimeout(timer.current)
      if (pending.current !== null) save(pending.current)
    }
    // save 只讀 ref，卸載時用的就是最新的
  }, [])

  const step = (key: keyof Thresholds, direction: 1 | -1) => {
    const next = stepThreshold(values[key], direction)
    if (next === null) return
    onStep()
    const updated = { ...values, [key]: next }
    setValues(updated)
    pending.current = updated
    if (timer.current !== null) clearTimeout(timer.current)
    timer.current = setTimeout(() => {
      timer.current = null
      save(updated)
    }, InnThresholdSaveDelayMilliseconds)
  }

  return (
    <>
      <div className="inn-box" role="group" aria-label="自動住宿">
        <h4>自動住宿</h4>
        <Row label="HP" value={values.hp} onStep={(direction) => step("hp", direction)} />
        <Row label="SP" value={values.sp} onStep={(direction) => step("sp", direction)} />
      </div>
      <div className="inn-box" role="group" aria-label="住宿費">
        <h4>
          住宿費<small>{`Lv ${character.level}`}</small>
        </h4>
        <div className="inn-box__line">
          <span>自動住宿一次</span>
          <b className={restFee === 0 ? "inn-box__free" : undefined}>{restFeeText(restFee)}</b>
        </div>
      </div>
    </>
  )
}
