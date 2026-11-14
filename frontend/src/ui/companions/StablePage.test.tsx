import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { useState } from "react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { ApiError } from "../../api/client"
import type { CharacterVo } from "../../game/character"
import { PetFeedVo, type Pet } from "../../game/pets"
import { makeCharacter, makePet, makePets } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { createCompanionsApi } from "./testing"
import { StablePage } from "./StablePage"
import petSilhouette from "../../assets/companions/pet-silhouette.webp"

const now = new Date("2026-11-14T08:00:00Z")
const ready = new Date(now.getTime() - 60_000)

const rabbit = makePet({
  id: 1,
  monsterId: "monster-9",
  species: "飛兔",
  name: "小風",
  age: 3,
  personality: "Aggressive",
  attack: 46,
  magic: 38,
  defense: 30,
  spirit: 24,
  satiety: 72,
  intimacy: 312,
  activationPercent: 35,
  carried: true,
  nextFeedAt: ready,
})
const panther = makePet({
  id: 2,
  monsterId: "monster-20",
  species: "狂暴獅",
  name: "夜影",
  nextFeedAt: ready,
})

const Harness = ({
  api,
  initial,
}: {
  api: ReturnType<typeof createCompanionsApi>
  initial: CharacterVo
}) => {
  const [current, setCurrent] = useState(initial)
  return (
    <StageContext value={{ height: 624, scale: 1 }}>
      <StablePage api={api} character={current} onCharacterChange={setCurrent} onBack={vi.fn()} />
    </StageContext>
  )
}

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

const renderStable = async ({
  pets = [rabbit, panther] as Pet[],
  api = createCompanionsApi(makePets(pets)),
  money = 1000,
} = {}) => {
  render(<Harness api={api} initial={makeCharacter({ money })} />)
  await flush()
  return api
}

const fruit = (name: string) => screen.getByRole("button", { name: new RegExp(`^${name}`) })
const say = () => screen.getByRole("status")
const bar = (label: string) => screen.getByRole("progressbar", { name: label })

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
  vi.setSystemTime(now)
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 companions-screen：Stable／Bars for a pet
test("StableShowsTheSelectedPet", async () => {
  await renderStable()

  const details = screen.getByRole("region", { name: "寵物" })
  expect(within(details).getByText("小風")).toBeInTheDocument()
  expect(within(details).getByText("飛兔・3 歲")).toBeInTheDocument()
  expect(within(details).getByText("積極")).toBeInTheDocument()
  for (const [label, value] of [
    ["AT", "46"],
    ["MT", "38"],
    ["DEF", "30"],
    ["出擊值", "24"],
  ])
    expect(within(details).getByText(label).parentElement).toHaveTextContent(value!)
  expect(bar("飽腹")).toHaveStyle({ width: "72%" })
  expect(bar("親密")).toHaveStyle({ width: "100%" })
  expect(bar("出擊率")).toHaveStyle({ width: "100%" })
  expect(screen.getByRole("button", { name: /^小風/ })).toHaveTextContent("攜帶")
})

// 標頭的上限以伺服器給的 maxPets 為準
test("HeaderCountsAgainstTheServerLimit", async () => {
  await renderStable({ api: createCompanionsApi(makePets([rabbit], { maxPets: 4 })) })

  expect(screen.getByRole("region", { name: "寵物" })).toHaveTextContent("寵物 1 / 4")
})

test("EmptyStallsShowEmptyPlates", async () => {
  await renderStable()

  const stalls = within(screen.getByRole("list", { name: "柵欄" })).getAllByRole("button")
  expect(stalls).toHaveLength(3)
  expect(stalls[2]).toHaveAccessibleName("空的柵欄")
  expect(stalls[2]).toBeDisabled()
})

// R2 沒有怪物圖：寵物一律用通用剪影
test("PetImageFallsBackToTheSilhouette", async () => {
  await renderStable()

  const image = screen.getByRole("button", { name: /^小風/ }).querySelector("img")!
  expect(image).toHaveAttribute("src", petSilhouette)
  expect(image).toHaveAttribute("data-asset", "monsters/monster-9")
})

// 規格 companions-screen：Feeding a fruit——浮字、扣錢、不說話
test("FeedingFloatsTheEffectAndSaysNothing", async () => {
  const api = await renderStable({ pets: [{ ...rabbit, satiety: 40, intimacy: 88 }] })
  vi.mocked(api.feedPet).mockResolvedValue(
    PetFeedVo.create({
      pets: makePets([
        {
          ...rabbit,
          attack: 48,
          satiety: 55,
          intimacy: 92,
          nextFeedAt: new Date(now.getTime() + 10_000),
        },
      ]),
      character: makeCharacter({ money: 980 }),
    }),
  )

  fireEvent.click(fruit("初級活力果實"))
  await flush()

  expect(api.feedPet).toHaveBeenCalledWith(1, "fruit-vigor-small")
  expect(screen.getByTestId("feed-effect").textContent).toBe("AT +2　♥ +4")
  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("980")
  expect(screen.queryByRole("status")).not.toBeInTheDocument()
  expect(fruit("初級活力果實")).toBeDisabled()
})

// 規格 companions-screen：Fruits disabled
test("FruitsDisabledWhenFullCoolingOrPoor", async () => {
  await renderStable({ pets: [{ ...rabbit, satiety: 100 }] })

  for (const button of within(screen.getByRole("group", { name: "果實" })).getAllByRole("button"))
    expect(button).toBeDisabled()
})

test("FruitsDisabledWhenPoor", async () => {
  await renderStable({ pets: [{ ...rabbit, satiety: 40 }], money: 30 })

  expect(fruit("初級活力果實")).toBeEnabled()
  expect(fruit("中級活力果實")).toBeDisabled()
  expect(fruit("中級活力果實")).toHaveAttribute("data-short", "true")
})

// 規格 companions-screen：lines 表的 overOwner
test("FeedingRejectedOverOwnerIsSpoken", async () => {
  const api = await renderStable({ pets: [{ ...rabbit, satiety: 40 }] })
  vi.mocked(api.feedPet).mockRejectedValue(new ApiError(400, "不行", ["overOwner"]))

  fireEvent.click(fruit("中級活力果實"))
  await flush()

  expect(say()).toHaveTextContent("再吃下去，牠就要比你還強了呢。")
  expect(say()).toHaveClass("keeper-say--failed")
})

// 規格 companions-screen：Keepers speak——錢不夠時重讀角色
test("NotEnoughMoneyReloadsTheCharacter", async () => {
  const api = await renderStable({ pets: [{ ...rabbit, satiety: 40 }] })
  vi.mocked(api.feedPet).mockRejectedValue(new ApiError(400, "錢不夠", ["notEnoughMoney"]))
  vi.mocked(api.getMyCharacter).mockResolvedValue(makeCharacter({ money: 15 }))

  fireEvent.click(fruit("初級活力果實"))
  await flush()

  expect(say()).toHaveTextContent("哎呀，這顆果實的錢好像不太夠。")
  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("15")
})

// 規格 companions-screen：lines 表的攜帶與放回
test("CarryAndStoreAreSpoken", async () => {
  const api = await renderStable()
  vi.mocked(api.storePet).mockResolvedValue(makePets([{ ...rabbit, carried: false }, panther]))

  fireEvent.click(screen.getByRole("button", { name: "放回獸欄" }))
  await flush()

  expect(api.storePet).toHaveBeenCalledWith(1)
  expect(say()).toHaveTextContent("小風回欄裡休息囉。")
  vi.mocked(api.carryPet).mockResolvedValue(makePets([rabbit, panther]))
  fireEvent.click(screen.getByRole("button", { name: "攜帶" }))
  await flush()
  expect(say()).toHaveTextContent("小風會跟著你出門囉！")
})

// 規格 companions-screen：Pet actions——改名字數 n / 12
test("RenameCountsCharacters", async () => {
  const api = await renderStable()
  vi.mocked(api.renamePet).mockResolvedValue(makePets([{ ...rabbit, name: "風丸" }, panther]))

  fireEvent.click(screen.getByRole("button", { name: "改名" }))
  const input = screen.getByRole("textbox", { name: "寵物名字" })
  fireEvent.change(input, { target: { value: "  " } })
  expect(screen.getByText("0 / 12")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "確定" })).toBeDisabled()

  fireEvent.change(input, { target: { value: " 風丸 " } })
  expect(screen.getByText("2 / 12")).toBeInTheDocument()
  fireEvent.click(screen.getByRole("button", { name: "確定" }))
  await flush()

  expect(api.renamePet).toHaveBeenCalledWith(1, "風丸")
  expect(say()).toHaveTextContent("「風丸」，牠很喜歡喔！")
})

// 規格 companions-screen：Releasing needs confirmation
test("ReleaseNeedsConfirmation", async () => {
  const api = await renderStable()
  vi.mocked(api.releasePet).mockResolvedValue(makePets([panther]))

  fireEvent.click(screen.getByRole("button", { name: "放生" }))
  expect(api.releasePet).not.toHaveBeenCalled()
  fireEvent.click(screen.getByRole("button", { name: "取消" }))
  expect(screen.queryByRole("button", { name: "確定放生" })).not.toBeInTheDocument()

  fireEvent.click(screen.getByRole("button", { name: "放生" }))
  fireEvent.click(screen.getByRole("button", { name: "確定放生" }))
  await flush()

  expect(api.releasePet).toHaveBeenCalledWith(1)
  expect(say()).toHaveTextContent("小風回到野外去了，要保重喔。")
  expect(within(screen.getByRole("region", { name: "寵物" })).getByText("夜影")).toBeInTheDocument()
})

// 規格 companions-screen：Companions load failure（獸欄）
test("StableLoadFailureShowsOnlyTheRetrySeal", async () => {
  const api = createCompanionsApi()
  vi.mocked(api.getPets).mockRejectedValueOnce(new TypeError("Failed to fetch"))
  await renderStable({ api })

  expect(say()).toHaveTextContent("咦，欄裡的名簿一時找不到……")
  expect(screen.queryByRole("group", { name: "果實" })).not.toBeInTheDocument()
  expect(screen.queryByText(/寵物 \d/)).not.toBeInTheDocument()
  const stalls = within(screen.getByRole("list", { name: "柵欄" })).getAllByRole("button")
  expect(stalls.every((stall) => stall.getAttribute("aria-label") === "空的柵欄")).toBe(true)

  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(screen.getByRole("group", { name: "果實" })).toBeInTheDocument()
})

// 放生送出後改點了別隻：回應回來時停在剛點的那隻，不跳回第一隻
test("ReleaseKeepsTheSelectionMadeWhileWaiting", async () => {
  const snow = makePet({
    id: 3,
    monsterId: "monster-30",
    species: "雪狐",
    name: "雪球",
    nextFeedAt: ready,
  })
  const api = await renderStable({ pets: [rabbit, panther, snow] })
  let finish: () => void = () => {}
  vi.mocked(api.releasePet).mockReturnValue(
    new Promise((resolve) => {
      finish = () => resolve(makePets([rabbit, snow]))
    }),
  )

  fireEvent.click(screen.getByRole("button", { name: /^夜影/ }))
  fireEvent.click(screen.getByRole("button", { name: "放生" }))
  fireEvent.click(screen.getByRole("button", { name: "確定放生" }))
  fireEvent.click(screen.getByRole("button", { name: /^雪球/ }))
  finish()
  await flush()

  expect(within(screen.getByRole("region", { name: "寵物" })).getByText("雪球")).toBeInTheDocument()
})
