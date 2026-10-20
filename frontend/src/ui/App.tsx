import { useEffect, useState } from "react"
import { readAccessToken } from "../api/authStorage"
import type { ApiClient } from "../api/client"
import { AuthScreen } from "./AuthScreen"
import { CreateCharacterScreen } from "./CreateCharacterScreen"

export type SessionExpiryListener = (listener: () => void) => () => void

type Phase = "auth" | "loading" | "create" | "adventure"

type Props = {
  api: ApiClient
  subscribeToSessionExpiry: SessionExpiryListener
}

export const App = ({ api, subscribeToSessionExpiry }: Props) => {
  const [phase, setPhase] = useState<Phase>(() => (readAccessToken() === null ? "auth" : "loading"))

  useEffect(() => subscribeToSessionExpiry(() => setPhase("auth")), [subscribeToSessionExpiry])

  useEffect(() => {
    if (phase !== "loading") return
    let isCurrent = true
    api
      .getMyCharacter()
      .then((character) => {
        if (isCurrent) setPhase(character === null ? "create" : "adventure")
      })
      .catch(() => {
        if (isCurrent) setPhase("auth")
      })
    return () => {
      isCurrent = false
    }
  }, [api, phase])

  switch (phase) {
    case "auth":
      return <AuthScreen api={api} onAuthenticated={() => setPhase("loading")} />
    case "loading":
      return <p className="screen">載入中…</p>
    case "create":
      return <CreateCharacterScreen api={api} onCreated={() => setPhase("adventure")} />
    case "adventure":
      return <p>歷練中</p>
  }
}
