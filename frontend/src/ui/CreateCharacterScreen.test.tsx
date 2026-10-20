import { render, screen } from "@testing-library/react"
import userEvent from "@testing-library/user-event"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import { CreateCharacterScreen } from "./CreateCharacterScreen"

test("CreateCharacterScreen_ShowsConflictDetail", async () => {
  const createCharacter = vi.fn().mockRejectedValue(new ApiError(409, "你已經有角色了"))
  render(<CreateCharacterScreen api={{ createCharacter }} onCreated={vi.fn()} />)

  expect(screen.getByText("為你的修仙者取個名字（2～12 字）")).toBeInTheDocument()
  await userEvent.type(screen.getByLabelText("名稱"), "青衣")
  await userEvent.click(screen.getByRole("button", { name: "踏入仙途" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("你已經有角色了")
})

test("CreateCharacterScreen_CallsOnCreated_OnSuccess", async () => {
  const created = { name: "青衣", experience: 0, spiritStones: 0, currentStageId: "stage-1" }
  const createCharacter = vi.fn().mockResolvedValue(created)
  const onCreated = vi.fn()
  render(<CreateCharacterScreen api={{ createCharacter }} onCreated={onCreated} />)

  await userEvent.type(screen.getByLabelText("名稱"), "青衣")
  await userEvent.click(screen.getByRole("button", { name: "踏入仙途" }))

  await vi.waitFor(() => expect(onCreated).toHaveBeenCalledWith(created))
  expect(createCharacter).toHaveBeenCalledWith("青衣")
})
