import { useEffect, useState } from "react"
import { clearAccessToken, readAccessToken } from "../api/authStorage"
import { ApiError, type ApiClient } from "../api/client"
import type { CreationOptionsVo } from "../game/creationOptions"
import type { JobVo } from "../game/jobBook"
import type { CharacterView } from "../game/types"
import { PassageMilliseconds } from "../motion/timings"
import { AdventureScreen } from "./AdventureScreen"
import { CreateScreen } from "./create/CreateScreen"
import { StageFrame } from "./stage/StageFrame"
import { Passage } from "./title/Passage"
import { TitleBackdrop } from "./title/TitleBackdrop"
import { TitleScreen, type TitleLoading } from "./title/TitleScreen"

export type SessionExpiryListener = (listener: () => void) => () => void

// 讀角色（title 的 loading）不論從登入、註冊或已登入打開網頁開始，結果都一樣：有角色先歡迎回來、沒有就建角
type Phase =
  | { name: "title"; loading: TitleLoading; account: string }
  | { name: "create"; jobs: JobVo[]; options: CreationOptionsVo; account: string }
  | { name: "passage"; title: string; subtitle: string; character: CharacterView }
  | { name: "game"; character: CharacterView }

type Props = {
  api: ApiClient
  subscribeToSessionExpiry: SessionExpiryListener
}

const titlePhase = (account = ""): Phase => ({ name: "title", loading: null, account })

const accountOf = (phase: Phase) =>
  phase.name === "title" || phase.name === "create" ? phase.account : ""

export const App = ({ api, subscribeToSessionExpiry }: Props) => {
  const [phase, setPhase] = useState<Phase>(() =>
    readAccessToken() === null ? titlePhase() : { name: "title", loading: "wait", account: "" },
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

  const isLoading = phase.name === "title" && phase.loading === "wait"
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
        return { name: "passage", title: "歡迎回來", subtitle: character.name, character }
      })
      .then((next) => {
        if (isCurrent) setPhase(next)
      })
      .catch((error: unknown) => {
        if (!isCurrent) return
        // 401 已由 client 清掉 token 並通知登出；其他失敗（斷線、5xx）攤字條讓玩家重試
        if (error instanceof ApiError && error.status === 401) setPhase(titlePhase(account))
        else setPhase({ name: "title", loading: "fail", account })
      })
    return () => {
      isCurrent = false
    }
  }, [api, isLoading, account])

  const passageCharacter = phase.name === "passage" ? phase.character : null
  useEffect(() => {
    if (passageCharacter === null) return
    const timer = setTimeout(
      () => setPhase({ name: "game", character: passageCharacter }),
      PassageMilliseconds,
    )
    return () => clearTimeout(timer)
  }, [passageCharacter])

  switch (phase.name) {
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
        </StageFrame>
      )
    case "passage":
      return (
        <StageFrame>
          <div className="title title--loading">
            <TitleBackdrop departing />
          </div>
          <Passage title={phase.title} subtitle={phase.subtitle} />
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
            onCreated={(character) => setPhase({ name: "game", character })}
            onLeave={() => setPhase(titlePhase(phase.account))}
          />
        </StageFrame>
      )
    case "game":
      return (
        <AdventureScreen
          api={api}
          initialCharacter={phase.character}
          onSignOut={() => {
            clearAccessToken()
            setPhase(titlePhase())
          }}
        />
      )
  }
}
