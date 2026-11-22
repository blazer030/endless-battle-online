import { act, fireEvent, render, screen, within } from "@testing-library/react"
import { afterEach, beforeEach, expect, test, vi } from "vitest"
import { makeCharacter } from "../../test/fixtures"
import { StageContext } from "../stage/stageContext"
import { CharacterScreen, type CharacterScreenApi } from "./CharacterScreen"
import { BuffVo } from "../../game/buffs"
import { InnVo } from "../../game/inn"
import {
  at,
  attackElixir,
  createCharacterApi,
  innWithLodging,
  learnableSkill,
  Now,
  overviewWith,
  warrior,
  warriorJob,
} from "./testing"

const flush = () =>
  act(async () => {
    await vi.advanceTimersByTimeAsync(0)
  })

type Options = {
  api?: CharacterScreenApi
  onBack?: () => void
  onCharacterChange?: () => void
  onSignOut?: () => void
  onOpenJobChange?: () => void
  height?: number
}

const renderCharacter = async ({
  api = createCharacterApi(),
  onBack = vi.fn(),
  onCharacterChange = vi.fn(),
  onSignOut = vi.fn(),
  onOpenJobChange = vi.fn(),
  height = 624,
}: Options = {}) => {
  render(
    <StageContext value={{ height, scale: 1 }}>
      <CharacterScreen
        api={api}
        character={warrior}
        onCharacterChange={onCharacterChange}
        onBack={onBack}
        onSignOut={onSignOut}
        onOpenJobChange={onOpenJobChange}
      />
    </StageContext>,
  )
  await flush()
  return { api, onBack, onCharacterChange, onSignOut, onOpenJobChange }
}

const card = () => screen.getByRole("group", { name: "冒險者登記證" })

beforeEach(() => {
  vi.useFakeTimers({ shouldAdvanceTime: false })
  vi.setSystemTime(Now)
})

afterEach(() => {
  vi.useRealTimers()
})

// 規格 character-screen「Loading and load failure」：打開時讀六個 API
test("LoadsSixRequestsOnOpen", async () => {
  const { api } = await renderCharacter()

  for (const call of [
    api.getMyCharacter,
    api.getAccount,
    api.getBuffs,
    api.getInn,
    api.getSkills,
    api.getCreationOptions,
  ]) {
    expect(call).toHaveBeenCalledTimes(1)
  }
})

// 規格「Loading」：登記證不寫字、手帳畫空白線、立繪不出
test("LoadingShowsFaintBlocks", async () => {
  await renderCharacter({
    api: createCharacterApi({ getSkills: vi.fn(() => new Promise<never>(() => {})) }),
  })

  expect(card()).toHaveAttribute("aria-busy", "true")
  expect(within(card()).queryByText("收尾者")).toBeNull()
  expect(screen.getByLabelText("讀取中")).toBeInTheDocument()
  expect(document.querySelector(".character__portrait img")).toBeNull()
  expect(screen.getByLabelText("持有金錢")).not.toHaveTextContent("128,400")
})

// 任何一個失敗就整頁讀取失敗；回 null（沒有角色）也算
test("AnyFailureShowsRetry", async () => {
  await renderCharacter({
    api: createCharacterApi({ getMyCharacter: vi.fn().mockResolvedValue(null) }),
  })

  expect(screen.getByText("登記證的資料讀不到。")).toBeInTheDocument()
  expect(screen.getByRole("button", { name: "再試一次" })).toBeInTheDocument()
})

// 規格「Retry after a failure」
test("RetryLoadsAgain", async () => {
  const getSkills = vi.fn().mockRejectedValueOnce(new Error("斷線"))
  const api = createCharacterApi()
  getSkills.mockImplementation(api.getSkills)
  await renderCharacter({ api: { ...api, getSkills } })

  fireEvent.click(screen.getByRole("button", { name: "再試一次" }))
  await flush()

  expect(getSkills).toHaveBeenCalledTimes(2)
  expect(api.getMyCharacter).toHaveBeenCalledTimes(2)
  expect(within(card()).getByText("收尾者")).toBeInTheDocument()
  expect(screen.queryByText("登記證的資料讀不到。")).toBeNull()
})

// 規格「Card values」：用讀回來的角色
test("CardShowsTheCharacter", async () => {
  await renderCharacter()

  const registration = card()
  for (const text of [
    "收尾者",
    "戰士",
    "Lv 162",
    "70%",
    "2,450 / 3,500",
    "男",
    "火",
    "中央大陸",
    "6,212 / 9,640",
    "980 / 1,205",
    "42%",
  ]) {
    expect(within(registration).getByText(text)).toBeInTheDocument()
  }
  expect(within(registration).getByRole("button", { name: "轉職" })).toBeInTheDocument()
})

// 規格「Portrait by sex」
test("PortraitBySex", async () => {
  await renderCharacter({
    api: createCharacterApi({
      getMyCharacter: vi
        .fn()
        .mockResolvedValue({ ...warrior, jobId: "job-7", jobName: "術士", sex: 2 }),
    }),
  })

  expect(document.querySelector(".character__portrait img")).toHaveAttribute(
    "data-asset",
    "portraits/job-7-female",
  )
})

// 規格「Job without a portrait」：清單沒有這個職業的立繪時顯示職業圖示＋立繪待補
test("JobWithoutPortraitShowsPlaceholder", async () => {
  await renderCharacter({
    api: createCharacterApi({
      getMyCharacter: vi.fn().mockResolvedValue({ ...warrior, jobId: "job-10", jobName: "狂戰士" }),
    }),
  })

  const portrait = document.querySelector(".character__portrait")!
  expect(portrait).toHaveTextContent("立繪待補")
  expect(portrait.querySelector("img")).toHaveAttribute("data-asset", "job-icons/job-10")
})

// 規格「Wallet」
test("WalletShowsMoney", async () => {
  await renderCharacter()

  expect(screen.getByLabelText("持有金錢")).toHaveTextContent("128,400")
})

// 規格「Open from the gate」：先停能力頁
test("OpensOnTheAbilitiesPage", async () => {
  await renderCharacter()

  expect(screen.getByRole("tab", { name: "能力" })).toHaveAttribute("aria-selected", "true")
  expect(screen.getByRole("tab", { name: "技能" })).toHaveAttribute("aria-selected", "false")
  fireEvent.click(screen.getByRole("tab", { name: "技能" }))
  expect(screen.getByRole("tab", { name: "技能" })).toHaveAttribute("aria-selected", "true")
})

// 「城鎮」返回；讀到的角色交回外層
test("BackReturnsToTown", async () => {
  const latest = makeCharacter({ ...warrior, money: 99 })
  const { onBack, onCharacterChange } = await renderCharacter({
    api: createCharacterApi({ getMyCharacter: vi.fn().mockResolvedValue(latest) }),
  })

  fireEvent.click(screen.getByRole("button", { name: "城鎮" }))

  expect(onBack).toHaveBeenCalledTimes(1)
  expect(onCharacterChange).toHaveBeenCalledWith(latest)
})

// 「轉職」交出角色給外層開舊外殼技能分頁
test("JobChangeButtonCallsBack", async () => {
  const { onOpenJobChange } = await renderCharacter()

  fireEvent.click(within(card()).getByRole("button", { name: "轉職" }))

  expect(onOpenJobChange).toHaveBeenCalledTimes(1)
})

// ── 能力頁 ──
const abilities = () => screen.getByRole("tabpanel", { name: "能力" })
const statRow = (label: string) =>
  within(abilities()).getByText(label).closest<HTMLElement>(".character-stat")!
const bonusList = () => within(abilities()).getByRole("list", { name: "密藥與住宿" })

// 規格「Stat with an equipment bonus」
test("StatShowsEquipmentBonus", async () => {
  await renderCharacter()

  expect(statRow("力量")).toHaveTextContent("412")
  expect(statRow("力量")).toHaveTextContent("+18")
})

// 規格「Stat without a bonus」
test("StatWithoutBonus", async () => {
  await renderCharacter()

  expect(statRow("智力").querySelector(".character-stat__bonus")).toBeNull()
  expect(within(abilities()).getByText("HP 上限").closest(".character-combat")).toHaveTextContent(
    "9,640",
  )
})

// 規格「Bonus list」：住宿第一、再來密藥，時間格式同城鎮
test("BonusListLodgingFirst", async () => {
  await renderCharacter()

  const items = within(bonusList()).getAllByRole("listitem")
  expect(items.map((item) => item.querySelector(".character-bonus__name")?.textContent)).toEqual([
    "學徒閣樓",
    "神力秘藥LV1",
  ])
  expect(items[0]).toHaveTextContent("07:00")
  expect(items[1]).toHaveTextContent("物攻 ×1.05")
  expect(items[1]).toHaveTextContent("01:40")
})

// 每秒倒數；不到 10 分鐘轉紅
test("BonusCountsDownEverySecond", async () => {
  const soon = BuffVo.create({ ...attackElixir, endsAt: at(601) })
  await renderCharacter({
    api: createCharacterApi({ getBuffs: vi.fn().mockResolvedValue([soon]) }),
  })
  const elixir = () => within(bonusList()).getAllByRole("listitem")[1]

  expect(elixir()).toHaveTextContent("10:01")
  expect(elixir()).not.toHaveAttribute("data-ending-soon")
  await act(async () => {
    await vi.advanceTimersByTimeAsync(2000)
  })

  expect(elixir()).toHaveTextContent("09:59")
  expect(elixir()).toHaveAttribute("data-ending-soon", "true")
})

// 規格「Nothing active」
test("NothingActiveText", async () => {
  await renderCharacter({
    api: createCharacterApi({
      getBuffs: vi.fn().mockResolvedValue([]),
      getInn: vi.fn().mockResolvedValue(InnVo.create({ ...innWithLodging, lodging: null })),
    }),
  })

  expect(within(abilities()).getByText("沒有生效中的密藥或住宿")).toBeInTheDocument()
})

// 到期的直接拿掉，不重讀
test("ExpiredBonusDisappearsWithoutRequest", async () => {
  const brief = BuffVo.create({ ...attackElixir, endsAt: at(2) })
  const { api } = await renderCharacter({
    api: createCharacterApi({ getBuffs: vi.fn().mockResolvedValue([brief]) }),
  })

  await act(async () => {
    await vi.advanceTimersByTimeAsync(3000)
  })

  expect(within(bonusList()).getAllByRole("listitem")).toHaveLength(1)
  expect(api.getBuffs).toHaveBeenCalledTimes(1)
})

// 規格「Sign out from the abilities page」
test("SignOutAsksWithUsername", async () => {
  await renderCharacter()

  fireEvent.click(within(abilities()).getByRole("button", { name: "登出" }))

  expect(within(abilities()).getByText("ethan")).toBeInTheDocument()
  expect(screen.getByRole("dialog", { name: "登出" })).toHaveTextContent("要登出 ethan 嗎？")
})

// 規格「Cancel sign out」
test("CancelSignOut", async () => {
  const { onSignOut } = await renderCharacter()

  fireEvent.click(within(abilities()).getByRole("button", { name: "登出" }))
  fireEvent.click(
    within(screen.getByRole("dialog", { name: "登出" })).getByRole("button", { name: "取消" }),
  )
  await act(async () => {
    await vi.advanceTimersByTimeAsync(150)
  })

  expect(screen.queryByRole("dialog", { name: "登出" })).toBeNull()
  expect(onSignOut).not.toHaveBeenCalled()
})

// 規格「Confirm sign out」：不送請求，交給外層清掉登入
test("ConfirmSignOutCallsBack", async () => {
  const { onSignOut } = await renderCharacter()

  fireEvent.click(within(abilities()).getByRole("button", { name: "登出" }))
  fireEvent.click(
    within(screen.getByRole("dialog", { name: "登出" })).getByRole("button", { name: "登出" }),
  )

  expect(onSignOut).toHaveBeenCalledTimes(1)
})

// ── 技能頁 ──
const skillsPanel = () => screen.getByRole("tabpanel", { name: "技能" })
const switcher = () => within(skillsPanel()).getByRole("button", { name: /^切換職業/ })
const cardOf = (name: string) =>
  within(skillsPanel())
    .getByRole("button", { name: new RegExp(`^${name}`) })
    .closest<HTMLElement>(".skill-card")!
const openSkills = async (api = createCharacterApi()) => {
  const rendered = await renderCharacter({ api })
  fireEvent.click(screen.getByRole("tab", { name: "技能" }))
  return rendered
}

// 規格「Skills page job switcher」：每次打開停目前職業
test("SkillsPageStartsOnCurrentJob", async () => {
  await openSkills()

  expect(switcher()).toHaveTextContent("戰士")
  expect(switcher()).toHaveTextContent("現職")
  expect(
    within(skillsPanel()).getAllByRole("button", { name: /^(爆氣|防禦心得|破滅一擊|嗜血)/ }),
  ).toHaveLength(4)
})

// 規格「Switcher of the current job」
test("SwitcherShowsSkillPoints", async () => {
  await openSkills()

  expect(switcher()).toHaveTextContent("Class I・見習・已學 3 / 4 招")
  expect(switcher()).toHaveTextContent("技能點")
  expect(switcher()).toHaveTextContent("2,587")
})

// 規格「Choosing a previous job」：清單依 Class 分組，選了只列那個職業學過的
test("ChoosingPreviousJobListsLearnedOnly", async () => {
  await openSkills()

  fireEvent.click(switcher())
  const picker = within(skillsPanel()).getByRole("group", { name: "當過的職業" })
  expect(picker).toHaveTextContent("CLASS I・見習")
  expect(within(picker).getByRole("button", { name: /術士/ })).toHaveTextContent("技能點 1,882")
  fireEvent.click(within(picker).getByRole("button", { name: /術士/ }))

  expect(within(skillsPanel()).queryByRole("group", { name: "當過的職業" })).toBeNull()
  expect(switcher()).toHaveTextContent("術士")
  expect(switcher()).not.toHaveTextContent("現職")
  expect(within(skillsPanel()).getByRole("button", { name: /^魔力增幅/ })).toBeInTheDocument()
  expect(within(skillsPanel()).queryByRole("button", { name: /^爆氣/ })).toBeNull()
})

// 技能頁不出現「熟練度」
test("SkillsPageNeverSaysProficiency", async () => {
  await openSkills()
  fireEvent.click(within(skillsPanel()).getByRole("button", { name: /^爆氣/ }))

  expect(skillsPanel()).not.toHaveTextContent("熟練度")
})

// 一次只展開一張
test("OneCardExpandedAtATime", async () => {
  await openSkills()
  const header = (name: string) =>
    within(skillsPanel()).getByRole("button", { name: new RegExp(`^${name}`) })

  fireEvent.click(header("爆氣"))
  expect(header("爆氣")).toHaveAttribute("aria-expanded", "true")
  fireEvent.click(header("防禦心得"))

  expect(header("爆氣")).toHaveAttribute("aria-expanded", "false")
  expect(header("防禦心得")).toHaveAttribute("aria-expanded", "true")
})

// 兩欄數值：目前與下一級，變了的標出來；花費行
test("CardShowsBothLevelColumns", async () => {
  await openSkills()
  fireEvent.click(within(skillsPanel()).getByRole("button", { name: /^爆氣/ }))
  const card = cardOf("爆氣")

  expect(card).toHaveTextContent("Lv 6/10")
  expect(card).toHaveTextContent("目前 Lv 6")
  expect(card).toHaveTextContent("下一級 Lv 7")
  expect(within(card).getByText("機率 100%")).toHaveClass("skill-card__value--changed")
  expect(within(card).getAllByText("SP 12")[1]).not.toHaveClass("skill-card__value--changed")
  expect(card).toHaveTextContent("升級需要27,000 金錢2,587 技能點")
  expect(within(card).getByRole("button", { name: "升級" })).toBeEnabled()
})

// 規格「Disabled reasons」
test("DisabledSealShowsReasons", async () => {
  await openSkills()
  fireEvent.click(within(skillsPanel()).getByRole("button", { name: /^嗜血/ }))
  const card = cardOf("嗜血")

  expect(card).toHaveTextContent("未習得")
  expect(card).toHaveTextContent("尚未學習")
  expect(within(card).getByRole("button", { name: "學習" })).toBeDisabled()
  expect(card).toHaveTextContent("前置技能：破滅一擊 Lv 3・技能點不足")
  expect(within(card).getByText("2,941 技能點")).toHaveClass("skill-card__cost--short")
})

// 規格「Maximum level」
test("MaxLevelShowsSeal", async () => {
  await openSkills(
    createCharacterApi({
      getSkills: vi
        .fn()
        .mockResolvedValue(
          overviewWith([warriorJob({ skills: [learnableSkill({ level: 10, next: null })] })]),
        ),
    }),
  )
  fireEvent.click(within(skillsPanel()).getByRole("button", { name: /^爆氣/ }))
  const card = cardOf("爆氣")

  expect(card).toHaveTextContent("已達最高等級")
  expect(card).toHaveTextContent("精通")
  expect(card).toHaveTextContent("目前 Lv 10（最高）")
  expect(within(card).queryByRole("button", { name: "升級" })).toBeNull()
})

// 沒有說明的技能寫「（說明待補）」
test("EmptyDescriptionShowsPending", async () => {
  await openSkills()
  fireEvent.click(switcher())
  fireEvent.click(within(skillsPanel()).getByRole("button", { name: /術士/ }))

  expect(cardOf("魔力增幅")).toHaveTextContent("（說明待補）")
})
