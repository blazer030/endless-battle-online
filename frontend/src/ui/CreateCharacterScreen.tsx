import { useState, type FormEvent } from "react"
import { ApiError, type ApiClient } from "../api/client"
import type { CharacterView } from "../game/types"

type Props = {
  api: Pick<ApiClient, "createCharacter">
  onCreated: (character: CharacterView) => void
}

export const CreateCharacterScreen = ({ api, onCreated }: Props) => {
  const [name, setName] = useState("")
  const [errorDetail, setErrorDetail] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setIsBusy(true)
    setErrorDetail(null)
    try {
      onCreated(await api.createCharacter(name))
    } catch (error) {
      setErrorDetail(error instanceof ApiError ? error.detail : "發生錯誤，請稍後再試")
      setIsBusy(false)
    }
  }

  return (
    <main className="screen">
      <h1 className="title">墨渡</h1>
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        <p>為你的修仙者取個名字（2～12 字）</p>
        <label>
          名稱
          <input value={name} onChange={(event) => setName(event.target.value)} maxLength={12} />
        </label>
        <div className="actions">
          <button type="submit" disabled={isBusy}>
            踏入仙途
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
