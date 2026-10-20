import { useEffect, useState } from "react"
import { readAccessToken } from "../api/authStorage"
import type { ApiClient } from "../api/client"
import type { CharacterView } from "../game/types"
import { AdventureScreen } from "./AdventureScreen"
import { AuthScreen } from "./AuthScreen"
import { CreateCharacterScreen } from "./CreateCharacterScreen"

export type SessionExpiryListener = (listener: () => void) => () => void

type Phase =
  | { name: "auth" }
  | { name: "loading" }
  | { name: "create" }
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
      .then((character) => {
        if (isCurrent)
          setPhase(character === null ? { name: "create" } : { name: "adventure", character })
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
          onCreated={(character) => setPhase({ name: "adventure", character })}
        />
      )
    case "adventure":
      return <AdventureScreen api={api} initialCharacter={phase.character} />
  }
}
