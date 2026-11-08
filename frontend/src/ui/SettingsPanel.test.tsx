import { render, screen, within } from "@testing-library/react"
import { user } from "../test/user"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import { makeCharacter } from "../test/fixtures"
import { SettingsPanel, type SettingsApi } from "./SettingsPanel"

const character = makeCharacter({ name: "青衣" })

const renderPanel = (
  api: SettingsApi = { updateSettings: vi.fn().mockResolvedValue(character) },
) => {
  const onCharacterChanged = vi.fn()
  const onClose = vi.fn()
  render(
    <SettingsPanel
      api={api}
      character={character}
      onCharacterChanged={onCharacterChanged}
      onClose={onClose}
    />,
  )
  return {
    api,
    onCharacterChanged,
    onClose,
    dialog: screen.getByRole("dialog", { name: "冒險設定" }),
  }
}

test("SettingsPanel_ShowsCurrentValues", () => {
  const { dialog } = renderPanel()

  expect(within(dialog).getByRole("combobox", { name: "攻擊方式" })).toHaveValue("auto")
  expect(within(dialog).getByRole("textbox", { name: "恢復道具 HP 門檻" })).toHaveValue("15")
  expect(within(dialog).getByRole("textbox", { name: "恢復道具 SP 門檻" })).toHaveValue("100")
  expect(within(dialog).getByRole("textbox", { name: "住宿 HP 門檻" })).toHaveValue("15")
  expect(within(dialog).getByRole("textbox", { name: "住宿 SP 門檻" })).toHaveValue("15")
  expect(within(dialog).getByRole("textbox", { name: "逃跑 HP 門檻" })).toHaveValue("0")
  expect(within(dialog).queryByRole("textbox", { name: "必殺技名稱" })).not.toBeInTheDocument()
  expect(dialog).toHaveTextContent("自動住宿(-1表示略過條件)")
  expect(dialog).toHaveTextContent(
    "恢復道具的 -1 也表示略過條件；SP 預設 100% 時，SP 全滿不會使用恢復道具。",
  )
})

test("SettingsPanel_SavesAndReportsTheCharacter", async () => {
  const saved = makeCharacter({ name: "青衣" })
  const { api, dialog, onCharacterChanged, onClose } = renderPanel({
    updateSettings: vi.fn().mockResolvedValue(saved),
  })
  const sp = within(dialog).getByRole("textbox", { name: "恢復道具 SP 門檻" })

  await user.clear(sp)
  await user.type(sp, "-1")
  await user.selectOptions(within(dialog).getByRole("combobox", { name: "攻擊方式" }), "Magic")
  await user.click(within(dialog).getByRole("button", { name: "儲存" }))

  expect(api.updateSettings).toHaveBeenCalledWith({
    attack: "Magic",
    potionHpPercent: 15,
    potionSpPercent: -1,
    restHpPercent: 15,
    restSpPercent: 15,
    escapeHpPercent: 0,
  })
  expect(onCharacterChanged).toHaveBeenCalledWith(saved)
  expect(onClose).toHaveBeenCalled()
})

test("SettingsPanel_RejectsDecimalBeforeCallingTheApi", async () => {
  const { api, dialog } = renderPanel()
  const rest = within(dialog).getByRole("textbox", { name: "住宿 HP 門檻" })

  await user.clear(rest)
  await user.type(rest, "12.5")
  await user.click(within(dialog).getByRole("button", { name: "儲存" }))

  expect(api.updateSettings).not.toHaveBeenCalled()
  expect(within(dialog).getByRole("alert")).toHaveTextContent(
    "門檻必須是 -1～100 的整數（逃跑 0～100）",
  )
})

test("SettingsPanel_ShowsInvalidPercentFromTheServer", async () => {
  const { dialog, onCharacterChanged, onClose } = renderPanel({
    updateSettings: vi.fn().mockRejectedValue(new ApiError(400, "設定無效", ["invalidPercent"])),
  })

  await user.click(within(dialog).getByRole("button", { name: "儲存" }))

  expect(await within(dialog).findByRole("alert")).toHaveTextContent(
    "門檻必須是 -1～100 的整數（逃跑 0～100）",
  )
  expect(onCharacterChanged).not.toHaveBeenCalled()
  expect(onClose).not.toHaveBeenCalled()
})

test("SettingsPanel_CloseButtonCloses", async () => {
  const { dialog, onClose } = renderPanel()

  await user.click(within(dialog).getByRole("button", { name: "關閉" }))

  expect(onClose).toHaveBeenCalled()
})
