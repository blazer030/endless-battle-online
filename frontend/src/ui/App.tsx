import { useEffect, useState } from "react"
import { clearAccessToken, readAccessToken } from "../api/authStorage"
import { ApiError, type ApiClient } from "../api/client"
import type { CreationOptionsVo } from "../game/creationOptions"
import type { JobVo } from "../game/jobBook"
import type { CharacterVo } from "../game/character"
import { PassageLeaveMilliseconds, PassageMilliseconds } from "../motion/timings"
import { GameScreen } from "./GameScreen"
import { preloadImages } from "./stage/preloadImages"
import { townImages } from "./stage/screenImages"
import { timeOfDay } from "../game/timeOfDay"
import { CreateScreen } from "./create/CreateScreen"
import { StageFrame } from "./stage/StageFrame"
import { Passage } from "./title/Passage"
import { TitleBackdrop } from "./title/TitleBackdrop"
import { TitleScreen, type TitleLoading } from "./title/TitleScreen"

export type SessionExpiryListener = (listener: () => void) => () => void

// 讀角色：登入、註冊後在標題上讀（title 的 loading），已登入打開網頁時停在過場背景上讀（opening）；
// 結果都一樣：有角色先歡迎回來、沒有就建角
type Phase =
  | { name: "opening" }
  | { name: "title"; loading: TitleLoading; account: string }
  | { name: "create"; jobs: JobVo[]; options: CreationOptionsVo; account: string }
  // fromOpening：接在打開網頁的過場背景後面，背景已經在了
  | { name: "passage"; character: CharacterVo; fromOpening: boolean }
  // arrival：進城前停的過場（歡迎回來、踏上旅途），城鎮從正中間展開蓋過它
  | { name: "game"; character: CharacterVo; arrival: "歡迎回來" | "踏上旅途" }

type Props = {
  api: ApiClient
  subscribeToSessionExpiry: SessionExpiryListener
}

const titlePhase = (account = ""): Phase => ({ name: "title", loading: null, account })

const accountOf = (phase: Phase) =>
  phase.name === "title" || phase.name === "create" ? phase.account : ""

export const App = ({ api, subscribeToSessionExpiry }: Props) => {
  const [phase, setPhase] = useState<Phase>(() =>
    readAccessToken() === null ? titlePhase() : { name: "opening" },
  )

  // token 失效（任何請求 401）：回到登記證；已經在登記證上（例如密碼打錯）就不動，留著證上的錯誤
  useEffect(
    () =>
      subscribeToSessionExpiry(() =>
        setPhase((current) =>
          current.name === "title" && current.loading === null
            ? current
            : titlePhase(accountOf(current)),
        ),
      ),
    [subscribeToSessionExpiry],
  )

  // 打開網頁沒角色或讀不到（含 401 登出通知）：下一個畫面已在底下，過場背景留在上面淡出
  const [previousPhaseName, setPreviousPhaseName] = useState(phase.name)
  const [isPassageLeaving, setIsPassageLeaving] = useState(false)
  if (previousPhaseName !== phase.name) {
    setPreviousPhaseName(phase.name)
    setIsPassageLeaving(
      previousPhaseName === "opening" && (phase.name === "title" || phase.name === "create"),
    )
  }
  useEffect(() => {
    if (!isPassageLeaving) return
    const timer = setTimeout(() => setIsPassageLeaving(false), PassageLeaveMilliseconds)
    return () => clearTimeout(timer)
  }, [isPassageLeaving])
  const leavingPassage = isPassageLeaving && <Passage leaving />

  const isOpening = phase.name === "opening"
  const isLoading = isOpening || (phase.name === "title" && phase.loading === "wait")
  const account = accountOf(phase)

  useEffect(() => {
    if (!isLoading) return
    let isCurrent = true
    api
      .getMyCharacter()
      .then(async (character): Promise<Phase> => {
        if (character === null) {
          // 建角要職業介紹冊與性別、屬性、出生地，先取回再打開
          const [jobs, options] = await Promise.all([api.getPublicJobs(), api.getCreationOptions()])
          return { name: "create", jobs, options, account }
        }
        return { name: "passage", character, fromOpening: isOpening }
      })
      .then((next) => {
        if (isCurrent) setPhase(next)
      })
      .catch((error: unknown) => {
        if (!isCurrent) return
        // 打開網頁時讀不到，不論原因都先當成登入失敗：清 token、回登記證（之後再做錯誤頁）
        if (isOpening) {
          clearAccessToken()
          setPhase(titlePhase())
          return
        }
        // 401 已由 client 清掉 token 並通知登出；其他失敗（斷線、5xx）攤字條讓玩家重試
        if (error instanceof ApiError && error.status === 401) setPhase(titlePhase(account))
        else setPhase({ name: "title", loading: "fail", account })
      })
    return () => {
      isCurrent = false
    }
  }, [api, isLoading, isOpening, account])

  const passageCharacter = phase.name === "passage" ? phase.character : null
  useEffect(() => {
    if (passageCharacter === null) return
    // 過場停留的這段時間先把城鎮的圖載好，進城時不會一張張冒出來
    void preloadImages(townImages(timeOfDay(new Date().getHours())))
    const timer = setTimeout(
      () => setPhase({ name: "game", character: passageCharacter, arrival: "歡迎回來" }),
      PassageMilliseconds,
    )
    return () => clearTimeout(timer)
  }, [passageCharacter])

  switch (phase.name) {
    case "opening":
      return (
        <StageFrame>
          <Passage immediate />
        </StageFrame>
      )
    case "title":
      return (
        <StageFrame>
          <TitleScreen
            api={api}
            initialAccount={phase.account}
            loading={phase.loading}
            onSignedIn={(signedIn) =>
              setPhase({ name: "title", loading: "wait", account: signedIn })
            }
            onRetry={() => setPhase({ ...phase, loading: "wait" })}
          />
          {leavingPassage}
        </StageFrame>
      )
    case "passage":
      // 接在打開網頁後面時與 opening 同一個結構，React 沿用同一個背景，只把墨點換成歡迎詞
      if (phase.fromOpening)
        return (
          <StageFrame>
            <Passage immediate title="歡迎回來" subtitle={phase.character.name} />
          </StageFrame>
        )
      return (
        <StageFrame>
          <div className="title title--loading">
            <TitleBackdrop departing />
          </div>
          <Passage title="歡迎回來" subtitle={phase.character.name} />
        </StageFrame>
      )
    case "create":
      return (
        <StageFrame>
          <CreateScreen
            api={api}
            jobs={phase.jobs}
            options={phase.options}
            account={phase.account}
            onCreated={(character) => setPhase({ name: "game", character, arrival: "踏上旅途" })}
            onLeave={() => setPhase(titlePhase(phase.account))}
          />
          {leavingPassage}
        </StageFrame>
      )
    case "game":
      return (
        <GameScreen
          api={api}
          initialCharacter={phase.character}
          arrival={<Passage settled title={phase.arrival} subtitle={phase.character.name} />}
          onSignOut={() => {
            clearAccessToken()
            setPhase(titlePhase())
          }}
        />
      )
  }
}
