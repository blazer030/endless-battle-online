import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { readAccessToken } from "./authStorage"

beforeEach(() => localStorage.clear())
afterEach(() => vi.restoreAllMocks())

test("authStorage_ReturnsNull_WhenStorageThrows", () => {
  vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
    throw new Error("denied")
  })

  expect(readAccessToken()).toBeNull()
})
