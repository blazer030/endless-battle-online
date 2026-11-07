import { waitFor } from "@testing-library/react"
import userEvent from "@testing-library/user-event"

// 打開 Ant Design 的下拉選單並選一項；選項文字必須符合畫面上顯示的內容
export async function chooseOption(select: HTMLElement, option: string) {
  await userEvent.click(select)
  await userEvent.click(await findOption(option))
}

export async function findOption(option: string) {
  let found: HTMLElement | undefined
  await waitFor(() => {
    // 剛關閉的選單可能還在收合動畫中，要選最後打開的那一個
    found = [...document.querySelectorAll<HTMLElement>(".ant-select-item-option-content")]
      .filter((element) => element.textContent === option && isInOpenDropdown(element))
      .at(-1)
    if (found === undefined) {
      throw new Error(`下拉選單沒有「${option}」`)
    }
  })
  return found!
}

const isInOpenDropdown = (element: HTMLElement) =>
  element.closest(".ant-select-dropdown-hidden") === null

export const visibleOptions = () =>
  [...document.querySelectorAll<HTMLElement>(".ant-select-item-option-content")]
    .filter(isInOpenDropdown)
    .map((element) => element.textContent)
