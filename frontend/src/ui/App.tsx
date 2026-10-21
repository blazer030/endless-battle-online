import { useEffect, useState } from "react"
import { readAccessToken } from "../api/authStorage"
import type { ApiClient } from "../api/client"
import type { CharacterView, ContentNames } from "../game/types"
import { AdventureScreen } from "./AdventureScreen"
import { AuthScreen } from "./AuthScreen"
import { CreateCharacterScreen } from "./CreateCharacterScreen"

export type SessionExpiryListener = (listener: () => void) => () => void

type Phase =
  | { name: "auth" }
  | { name: "loading" }
  | { name: "create"; content: ContentNames }
  | { name: "adventure"; character: CharacterView }

type Props = {
  api: ApiClient
  subscribeToSessionExpiry: SessionExpiryListener
}

export const App = ({ api, subscribeToSessionExpiry }: Props) => {
  const [phase, setPhase] = useState<Phase>(() => ({
    name: readAccessToken() === null ? "auth" : "loading",
  }))

  useEffect(
    () => subscribeToSessionExpiry(() => setPhase({ name: "auth" })),
    [subscribeToSessionExpiry],
  )

  useEffect(() => {
    if (phase.name !== "loading") return
    let isCurrent = true
    api
      .getMyCharacter()
      .then(async (character): Promise<Phase> => {
        if (character !== null) return { name: "adventure", character }
        // 建角畫面的選項（職業、屬性等）來自內容，先取回再顯示
        return { name: "create", content: await api.getContent() }
      })
      .then((next) => {
        if (isCurrent) setPhase(next)
      })
      .catch(() => {
        if (isCurrent) setPhase({ name: "auth" })
      })
    return () => {
      isCurrent = false
    }
  }, [api, phase.name])

  switch (phase.name) {
    case "auth":
      return <AuthScreen api={api} onAuthenticated={() => setPhase({ name: "loading" })} />
    case "loading":
      return <p className="screen">載入中…</p>
    case "create":
      return (
        <CreateCharacterScreen
          api={api}
          content={phase.content}
          onCreated={(character) => setPhase({ name: "adventure", character })}
        />
      )
    case "adventure":
      return <AdventureScreen api={api} initialCharacter={phase.character} />
  }
}
