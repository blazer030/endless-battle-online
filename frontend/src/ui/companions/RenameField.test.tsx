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

// 超過上限時截到上限（貼上過長的名字也一樣），不整筆退回
test("RenameField_ClipsInputBeyondTheLimit", () => {
  renderField()
  const input = screen.getByRole("textbox", { name: "傭兵名字" })

  fireEvent.change(input, { target: { value: "  一二三四五六七八九十" } })

  expect(input).toHaveValue("  一二三四五六七八")
  expect(screen.getByText("8 / 8")).toBeInTheDocument()
})

// 注音、拼音選字時輸入框暫時多出符號或字母：選字中不擋，選完才截
test("RenameField_LetsCompositionFinishBeforeClipping", () => {
  renderField()
  const input = screen.getByRole("textbox", { name: "傭兵名字" })
  fireEvent.change(input, { target: { value: "一二三四五六七" } })

  fireEvent.compositionStart(input)
  fireEvent.change(input, { target: { value: "一二三四五六七ㄌㄟˊ" } })
  expect(input).toHaveValue("一二三四五六七ㄌㄟˊ")

  fireEvent.change(input, { target: { value: "一二三四五六七雷" } })
  fireEvent.compositionEnd(input)
  expect(input).toHaveValue("一二三四五六七雷")

  fireEvent.compositionStart(input)
  fireEvent.change(input, { target: { value: "一二三四五六七雷電" } })
  fireEvent.compositionEnd(input)
  expect(input).toHaveValue("一二三四五六七雷")
})
