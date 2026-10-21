import { useState, type FormEvent } from "react"
import { ApiError, type ApiClient } from "../api/client"

type Props = {
  api: Pick<ApiClient, "register" | "login">
  onAuthenticated: () => void
}

export const AuthScreen = ({ api, onAuthenticated }: Props) => {
  const [username, setUsername] = useState("")
  const [password, setPassword] = useState("")
  const [errorDetail, setErrorDetail] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const run = async (action: () => Promise<void>) => {
    setIsBusy(true)
    setErrorDetail(null)
    try {
      await action()
      onAuthenticated()
    } catch (error) {
      setErrorDetail(error instanceof ApiError ? error.detail : "發生錯誤，請稍後再試")
    } finally {
      setIsBusy(false)
    }
  }

  const handleLogin = (event: FormEvent) => {
    event.preventDefault()
    void run(() => api.login(username, password))
  }

  const handleRegister = () =>
    void run(async () => {
      await api.register(username, password)
      await api.login(username, password)
    })

  return (
    <main className="screen">
      <h1 className="title">EBO</h1>
      <p className="tagline">Endless Battle Online</p>
      <p className="subtitle">奇幻放置 RPG</p>
      <form className="form" onSubmit={handleLogin}>
        <label>
          帳號
          <input
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            autoComplete="username"
          />
        </label>
        <label>
          密碼
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            autoComplete="current-password"
          />
        </label>
        <div className="actions">
          <button type="submit" disabled={isBusy}>
            登入
          </button>
          <button type="button" disabled={isBusy} onClick={handleRegister}>
            註冊
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
