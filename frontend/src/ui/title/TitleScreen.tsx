import { useEffect, useRef, useState, type CSSProperties, type KeyboardEvent } from "react"
import { ApiError, fallbackDetail, type ApiClient } from "../../api/client"
import cardBlank from "../../assets/title/card-blank.webp"
import cardSealed from "../../assets/title/card-sealed.webp"
import { InkDotsDelaySeconds, RegisterSealSettleMilliseconds } from "../../motion/timings"
import { Seal } from "./Seal"
import { TitleBackdrop } from "./TitleBackdrop"
import "./TitleScreen.css"

export type TitleApi = Pick<ApiClient, "login" | "register">

// 讀角色：wait 等待中、fail 讀不到（字條＋重試）；null 是平常的標題
export type TitleLoading = null | "wait" | "fail"

export type SignInRoute = "login" | "reg"

type Props = {
  api: TitleApi
  // 自己那張登記證預先填好的帳號（例如剛註冊完離開建角）
  initialAccount?: string
  loading: TitleLoading
  onSignedIn: (account: string, route: SignInRoute) => void
  onRetry: () => void
}

type Card = { user: string; pass: string; error: string | null }

const emptyCard: Card = { user: "", pass: "", error: null }

const errorText = (error: unknown) => (error instanceof ApiError ? error.detail : fallbackDetail)

const onEnter = (action: () => void) => (event: KeyboardEvent) => {
  if (event.key === "Enter") action()
}

// 標題：桌上自己那張登記證（入城＝登入）與一疊空白證（登記＝註冊）；讀角色時登記證收下桌，看不到也點不到
export const TitleScreen = ({ api, initialAccount = "", loading, onSignedIn, onRetry }: Props) => {
  const [mode, setMode] = useState<SignInRoute>("login")
  const [own, setOwn] = useState<Card>({ ...emptyCard, user: initialAccount })
  const [blank, setBlank] = useState<Card>(emptyCard)
  const [busy, setBusy] = useState(false)
  const [sealed, setSealed] = useState(false)
  const settleTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined)

  useEffect(() => () => clearTimeout(settleTimer.current), [])

  const isRegistering = mode === "reg"
  const isLocked = busy || loading !== null

  const login = async () => {
    if (isLocked || own.user === "" || own.pass === "") return
    setBusy(true)
    setOwn((card) => ({ ...card, error: null }))
    try {
      await api.login(own.user, own.pass)
      setBusy(false)
      onSignedIn(own.user, "login")
    } catch (error) {
      setBusy(false)
      setOwn((card) => ({ ...card, error: errorText(error) }))
    }
  }

  // 註冊後用同一組帳密登入；成功就蓋上蠟封，停一下再讀角色
  const register = async () => {
    if (isLocked || sealed || blank.user === "" || blank.pass === "") return
    const { user, pass } = blank
    setBusy(true)
    setBlank((card) => ({ ...card, error: null }))
    try {
      await api.register(user, pass)
      await api.login(user, pass)
    } catch (error) {
      setBusy(false)
      setBlank((card) => ({ ...card, error: errorText(error) }))
      return
    }
    setBusy(false)
    setSealed(true)
    setOwn({ ...emptyCard, user })
    settleTimer.current = setTimeout(() => onSignedIn(user, "reg"), RegisterSealSettleMilliseconds)
  }

  const pick = (next: SignInRoute) => {
    if (isLocked || sealed) return
    setMode(next)
    setOwn((card) => ({ ...card, error: null }))
    setBlank((card) => ({ ...card, error: null }))
  }

  const rootClass = [
    "title",
    isRegistering && "title--reg",
    busy && "title--busy",
    sealed && "title--sealed",
    loading !== null && "title--loading",
    loading === "wait" && "title--waiting",
  ]
    .filter(Boolean)
    .join(" ")

  return (
    <div
      className={rootClass}
      style={{ "--ink-dots-delay": `${InkDotsDelaySeconds}s` } as CSSProperties}
    >
      <TitleBackdrop />
      <div className="title-stack" aria-hidden="true">
        <i />
        <i />
      </div>

      <section
        className="title-card title-card--own"
        aria-label="我的冒險者登記證"
        aria-hidden={loading !== null || undefined}
        inert={loading !== null}
      >
        <img className="title-paper" src={cardSealed} alt="" />
        <div className="title-card-inside">
          <h2>冒險者登記證</h2>
          <div className="title-card-english">Adventurer's Registry</div>
          <hr />
          <label className="title-field">
            <span>帳號</span>
            <input
              type="text"
              value={own.user}
              maxLength={32}
              autoComplete="username"
              disabled={isRegistering || isLocked}
              onChange={(event) => setOwn({ ...own, user: event.target.value, error: null })}
              onKeyDown={onEnter(() => void login())}
            />
          </label>
          <label className="title-field">
            <span>密碼</span>
            <input
              type="password"
              value={own.pass}
              autoComplete="current-password"
              disabled={isRegistering || isLocked}
              onChange={(event) => setOwn({ ...own, pass: event.target.value, error: null })}
              onKeyDown={onEnter(() => void login())}
            />
          </label>
          {own.error !== null && (
            <p className="title-error" role="alert">
              {own.error}
            </p>
          )}
          <Seal
            label="入城"
            disabled={own.user === "" || own.pass === "" || busy}
            onClick={() => void login()}
          />
        </div>
        {isRegistering && !isLocked && !sealed && (
          <button
            type="button"
            className="title-pick"
            aria-label="拿回我的登記證"
            onClick={() => pick("login")}
          />
        )}
      </section>

      <section
        className="title-card title-card--blank"
        aria-label="空白登記證"
        aria-hidden={loading !== null || undefined}
        inert={loading !== null}
      >
        <img className="title-paper" src={cardBlank} alt="" />
        <img className="title-paper title-wax" src={cardSealed} alt="" />
        <div className="title-card-inside">
          <h2>冒險者登記證</h2>
          <div className="title-card-english">Adventurer's Registry</div>
          <hr />
          <label className="title-field">
            <span>帳號</span>
            <input
              type="text"
              value={blank.user}
              maxLength={32}
              autoComplete="username"
              disabled={!isRegistering || isLocked || sealed}
              onChange={(event) => setBlank({ ...blank, user: event.target.value, error: null })}
              onKeyDown={onEnter(() => void register())}
            />
          </label>
          <label className="title-field">
            <span>密碼</span>
            <input
              type="password"
              value={blank.pass}
              autoComplete="new-password"
              disabled={!isRegistering || isLocked || sealed}
              onChange={(event) => setBlank({ ...blank, pass: event.target.value, error: null })}
              onKeyDown={onEnter(() => void register())}
            />
          </label>
          {blank.error !== null && (
            <p className="title-error" role="alert">
              {blank.error}
            </p>
          )}
          <Seal
            label="登記"
            disabled={blank.user === "" || blank.pass === "" || busy || sealed}
            onClick={() => void register()}
          />
        </div>
        {!isRegistering && !isLocked && (
          <button
            type="button"
            className="title-pick"
            aria-label="拿一張空白登記證"
            onClick={() => pick("reg")}
          />
        )}
      </section>

      <div className="title-wait" aria-hidden="true">
        <i />
        <i />
        <i />
      </div>
      {loading === "fail" && (
        <section className="title-fail" role="alert" aria-label="讀取失敗">
          <p>公會那邊沒有回音……</p>
          <Seal label="重試" className="seal--glow" onClick={onRetry} />
        </section>
      )}
    </div>
  )
}
