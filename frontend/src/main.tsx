import { StrictMode } from "react"
import { createRoot } from "react-dom/client"
import "./ui/theme.css"
import "./ui/screens.css"
import { createApiClient } from "./api/client"
import { App, type SessionExpiryListener } from "./ui/App"

const listeners = new Set<() => void>()

const subscribeToSessionExpiry: SessionExpiryListener = (listener) => {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

const api = createApiClient({
  fetch: (...args) => fetch(...args),
  onUnauthorized: () => listeners.forEach((listener) => listener()),
})

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App api={api} subscribeToSessionExpiry={subscribeToSessionExpiry} />
  </StrictMode>,
)
