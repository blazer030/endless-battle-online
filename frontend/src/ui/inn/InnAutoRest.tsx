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
  // 玩家按了「市集」：還沒送出的修改立刻送
  leaving: boolean
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

// 自動住宿：HP／SP 門檻按了就存（最後一次按下後延遲送出，離開時立刻送），失敗退回上次存好的值。
// 旅館裡一直掛著（切分頁只隱藏），存檔結果才不會因為切分頁而遺失；只有最後一次送出、且之後沒有新修改時才更新畫面
export const InnAutoRest = ({
  api,
  character,
  restFee,
  onCharacterChange,
  onStep,
  onSaveFailed,
  leaving,
}: Props) => {
  const [values, setValues] = useState(() => thresholdsOf(character))
  const saved = useRef(thresholdsOf(character))
  const pending = useRef<Thresholds | null>(null)
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const alive = useRef(true)
  // 每次送出加一；回應只在它還是最後一次送出時才影響畫面
  const request = useRef(0)
  // 已經成功回來的最新一次送出；比它舊的成功回應不再更新「上次存好的值」與外層角色
  const confirmedRequest = useRef(0)
  // 送出時用最新的角色設定，只換兩個門檻
  const latest = useRef({ api, character, onCharacterChange, onSaveFailed })
  useEffect(() => {
    latest.current = { api, character, onCharacterChange, onSaveFailed }
  })

  const save = (thresholds: Thresholds) => {
    pending.current = null
    request.current += 1
    const sent = request.current
    const current = latest.current
    current.api
      .updateSettings({
        ...current.character.settings,
        restHealthPointsPercent: thresholds.hp,
        restManaPointsPercent: thresholds.sp,
      })
      .then(
        (confirmed) => {
          if (sent <= confirmedRequest.current) return
          confirmedRequest.current = sent
          const stored = thresholdsOf(confirmed)
          saved.current = stored
          latest.current.onCharacterChange(confirmed)
          if (alive.current && sent === request.current && pending.current === null)
            setValues(stored)
        },
        () => {
          if (!alive.current || sent !== request.current || pending.current !== null) return
          setValues(saved.current)
          latest.current.onSaveFailed()
        },
      )
  }

  // 還沒送出的修改立刻送；放在 ref 裡，effect 不必依賴每次重新建立的函式
  const flush = useRef(() => {})
  useEffect(() => {
    flush.current = () => {
      if (timer.current !== null) clearTimeout(timer.current)
      timer.current = null
      if (pending.current !== null) save(pending.current)
    }
  })

  useEffect(() => {
    if (leaving) flush.current()
  }, [leaving])

  // 卸載時（例如直接關掉旅館）還沒送出的修改也立刻送
  useEffect(() => {
    alive.current = true
    return () => {
      alive.current = false
      flush.current()
    }
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
