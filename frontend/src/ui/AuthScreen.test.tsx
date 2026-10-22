import { render, screen } from "@testing-library/react"
import { user } from "../test/user"
import { expect, test, vi } from "vitest"
import { ApiError } from "../api/client"
import { AuthScreen } from "./AuthScreen"

test("AuthScreen_RegisterThenLogin_CallsBothInOrder", async () => {
  const calls: string[] = []
  const register = vi.fn(async () => void calls.push("register"))
  const login = vi.fn(async () => void calls.push("login"))
  const onAuthenticated = vi.fn()
  render(<AuthScreen api={{ register, login }} onAuthenticated={onAuthenticated} />)

  await user.type(screen.getByLabelText("帳號"), "alice")
  await user.type(screen.getByLabelText("密碼"), "secret123")
  await user.click(screen.getByRole("button", { name: "註冊" }))

  await vi.waitFor(() => expect(onAuthenticated).toHaveBeenCalledTimes(1))
  expect(calls).toEqual(["register", "login"])
  expect(register).toHaveBeenCalledWith("alice", "secret123")
  expect(login).toHaveBeenCalledWith("alice", "secret123")
})

test("AuthScreen_ShowsServerDetail_OnError", async () => {
  const login = vi.fn().mockRejectedValue(new ApiError(401, "帳號或密碼錯誤"))
  render(<AuthScreen api={{ register: vi.fn(), login }} onAuthenticated={vi.fn()} />)

  await user.type(screen.getByLabelText("帳號"), "alice")
  await user.type(screen.getByLabelText("密碼"), "wrong")
  await user.click(screen.getByRole("button", { name: "登入" }))

  expect(await screen.findByRole("alert")).toHaveTextContent("帳號或密碼錯誤")
  expect(screen.getByLabelText("帳號")).toHaveValue("alice")
})

test("AuthScreen_ShowsTitleAndFantasySubtitle", () => {
  render(<AuthScreen api={{ register: vi.fn(), login: vi.fn() }} onAuthenticated={vi.fn()} />)

  expect(screen.getByRole("heading", { name: "EBO" })).toBeInTheDocument()
  expect(screen.getByText("Endless Battle Online")).toBeInTheDocument()
  expect(screen.getByText("奇幻放置 RPG")).toBeInTheDocument()
})
