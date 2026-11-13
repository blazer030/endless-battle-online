import { fireEvent, render, screen } from "@testing-library/react"
import { expect, test, vi } from "vitest"
import { RenameField } from "./RenameField"

const renderField = () =>
  render(
    <RenameField
      label="傭兵名字"
      initial="葛雷"
      maxLength={8}
      busy={false}
      onConfirm={vi.fn()}
      onCancel={vi.fn()}
    />,
  )

// 規格：名字長度以 Unicode 字元（code point）計，與伺服器一致；emoji 算一個字
test("RenameField_CountsEmojiAsOneCharacter", () => {
  renderField()
  const input = screen.getByRole("textbox", { name: "傭兵名字" })

  fireEvent.change(input, { target: { value: "😀😀😀😀😀😀😀😀" } })

  expect(input).toHaveValue("😀😀😀😀😀😀😀😀")
  expect(screen.getByText("8 / 8")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "確定" })).toBeEnabled()
})

test("RenameField_RefusesInputBeyondTheLimit", () => {
  renderField()
  const input = screen.getByRole("textbox", { name: "傭兵名字" })

  fireEvent.change(input, { target: { value: "一二三四五六七八" } })
  fireEvent.change(input, { target: { value: "一二三四五六七八九" } })

  expect(input).toHaveValue("一二三四五六七八")
  expect(screen.getByText("8 / 8")).toBeInTheDocument()
})
