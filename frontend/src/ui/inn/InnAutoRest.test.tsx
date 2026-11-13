import { act, fireEvent, render, screen } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import type { AdventureSettings, CharacterVo } from "../../game/character"
import { InnThresholdSaveDelayMilliseconds } from "../../motion/timings"
import { makeCharacter } from "../../test/fixtures"
import { InnAutoRest } from "./InnAutoRest"

const character = (hp: number, sp: number): CharacterVo => {
  const base = makeCharacter({ level: 6 })
  return {
    ...base,
    settings: { ...base.settings, restHealthPointsPercent: hp, restManaPointsPercent: sp },
  }
}

const renderAutoRest = (
  updateSettings: (settings: AdventureSettings) => Promise<CharacterVo>,
  { leaving = false, initial = character(15, 15) } = {},
) => {
  const props = {
    api: { updateSettings: vi.fn(updateSettings) },
    character: initial,
    restFee: 0,
    onCharacterChange: vi.fn(),
    onStep: vi.fn(),
    onSaveFailed: vi.fn(),
  }
  const view = render(<InnAutoRest {...props} leaving={leaving} />)
  return {
    ...props,
    rerender: (next: { leaving: boolean }) => view.rerender(<InnAutoRest {...props} {...next} />),
  }
}

const hp = () => screen.getByTestId("threshold-hp")
const plus = (label: "HP" | "SP") =>
  fireEvent.click(screen.getByRole("button", { name: `${label} 門檻增加` }))
const waitDelay = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(InnThresholdSaveDelayMilliseconds)
  })

beforeEach(() => {
  vi.useFakeTimers()
})

afterEach(() => {
  vi.useRealTimers()
})

// 伺服器確認的門檻（例如被調整過）要反映到畫面
test("InnAutoRest_ShowsTheThresholdsTheServerConfirmed", async () => {
  renderAutoRest(async (settings) => ({
    ...character(15, 15),
    settings: { ...settings, restHealthPointsPercent: 25 },
  }))

  plus("HP")
  await waitDelay()

  expect(hp()).toHaveTextContent("25%")
})

// 較舊的存檔失敗時，已經按下的較新修改不能被退回
test("InnAutoRest_OlderFailureDoesNotUndoANewerStep", async () => {
  let rejectFirst: (error: Error) => void = () => {}
  const props = renderAutoRest(
    vi
      .fn<(settings: AdventureSettings) => Promise<CharacterVo>>()
      .mockImplementationOnce(() => new Promise((_, reject) => (rejectFirst = reject)))
      .mockImplementation(async (settings) => ({ ...character(15, 15), settings })),
  )

  plus("HP")
  await waitDelay()
  plus("HP")
  await act(async () => {
    rejectFirst(new TypeError("Failed to fetch"))
  })

  expect(hp()).toHaveTextContent("25%")
  expect(props.onSaveFailed).not.toHaveBeenCalled()
  await waitDelay()
  expect(props.api.updateSettings).toHaveBeenLastCalledWith(
    expect.objectContaining({ restHealthPointsPercent: 25 }),
  )
})

// 離開時（leaving 變成 true）立刻送出還沒送的修改
test("InnAutoRest_LeavingSendsAPendingStepAtOnce", () => {
  const props = renderAutoRest(async (settings) => ({ ...character(15, 15), settings }))

  plus("HP")
  props.rerender({ leaving: true })

  expect(props.api.updateSettings).toHaveBeenCalledWith(
    expect.objectContaining({ restHealthPointsPercent: 20 }),
  )
})
