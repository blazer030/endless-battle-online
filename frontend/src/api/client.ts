import type { AdvanceVo } from "../game/battleEvents"
import type { JobChoiceVo, SkillOverviewVo, SkillSlotCommand } from "../game/skillOverview"
import type { WorldAreaVo } from "../game/world"
import type { BuffVo } from "../game/buffs"
import type { InnBookVo, InnVo } from "../game/inn"
import type { HonourShopVo } from "../game/honour"
import type { MercenaryHireVo, MercenaryOverviewVo } from "../game/mercenaries"
import type { PetFeedVo, PetsVo } from "../game/pets"
import type { MissionBoardVo, MissionCompletionVo } from "../game/missions"
import type { ForgeResultVo, RecipeVo, SynthesisVo, UnsocketVo } from "../game/forge"
import type {
  DepotMoveVo,
  DepotVo,
  EquipmentSlot,
  InventoryVo,
  ItemCategory,
  ItemUseVo,
  ShopItemVo,
} from "../game/inventory"
import type { AdventureSettings, CharacterVo, CreateCharacterCommand } from "../game/character"
import type { CreationOptionsVo } from "../game/creationOptions"
import type { JobVo } from "../game/jobBook"
import { RequestTimeoutMilliseconds } from "../motion/timings"
import { clearAccessToken, readAccessToken, saveAccessToken } from "./authStorage"
import { parseArray } from "./parse"
import { AdvanceConverter } from "./adventureData"
import {
  AdventureSettingsConverter,
  CharacterConverter,
  CreateCharacterConverter,
} from "./characterData"
import {
  ForgeResultConverter,
  RecipeConverter,
  SynthesisConverter,
  UnsocketConverter,
} from "./forgeData"
import {
  DepotConverter,
  DepotMoveConverter,
  InventoryConverter,
  ItemUseConverter,
  ShopItemConverter,
} from "./inventoryData"
import {
  HonourShopConverter,
  MercenaryHireConverter,
  MercenaryOverviewConverter,
  PetFeedConverter,
  PetsConverter,
} from "./companionData"
import { BuffConverter, InnBookConverter, InnConverter } from "./lodgingData"
import { MissionBoardConverter, MissionCompletionConverter } from "./missionData"
import { CreationOptionsConverter, JobConverter } from "./publicData"
import { JobChoiceConverter, SkillOverviewConverter, SkillSlotsConverter } from "./skillData"
import { WorldConverter } from "./worldData"

export const fallbackDetail = "發生錯誤，請稍後再試"

export class ApiError extends Error {
  readonly status: number
  readonly detail: string
  // ProblemDetails 的 reasons 擴充欄位：驗證失敗（400）與衝突（409）的原因代碼
  readonly reasons: string[]

  constructor(status: number, detail: string, reasons: string[] = []) {
    super(detail)
    this.status = status
    this.detail = detail
    this.reasons = reasons
    this.name = "ApiError"
  }
}

export type ApiClient = {
  register: (username: string, password: string) => Promise<void>
  login: (username: string, password: string) => Promise<void>
  getMyCharacter: () => Promise<CharacterVo | null>
  createCharacter: (command: CreateCharacterCommand) => Promise<CharacterVo>
  advance: () => Promise<AdvanceVo>
  // 不用登入的公開資料
  getPublicJobs: () => Promise<JobVo[]>
  getCreationOptions: () => Promise<CreationOptionsVo>
  getJobs: () => Promise<JobChoiceVo[]>
  changeJob: (jobId: string) => Promise<CharacterVo>
  getSkills: () => Promise<SkillOverviewVo>
  learnSkill: (skillId: string) => Promise<SkillOverviewVo>
  setSkillSlots: (slots: SkillSlotCommand[]) => Promise<SkillOverviewVo>
  getWorld: () => Promise<WorldAreaVo[]>
  moveTo: (placeId: number, subPlace: number | null) => Promise<CharacterVo>
  getInventory: () => Promise<InventoryVo>
  // quantity 只用於補給品：從那一格分出幾個放到補給欄
  equip: (inventoryId: number, quantity?: number) => Promise<InventoryVo>
  unequip: (slot: EquipmentSlot) => Promise<InventoryVo>
  useItem: (inventoryId: number) => Promise<ItemUseVo>
  getShop: (category: ItemCategory, level: number) => Promise<ShopItemVo[]>
  buy: (itemId: string, quantity: number) => Promise<InventoryVo>
  sell: (inventoryId: number, quantity: number) => Promise<InventoryVo>
  getDepot: () => Promise<DepotVo>
  store: (inventoryId: number, quantity: number) => Promise<DepotMoveVo>
  take: (depotId: number, quantity: number) => Promise<DepotMoveVo>
  deposit: (amount: number) => Promise<CharacterVo>
  withdraw: (amount: number) => Promise<CharacterVo>
  refine: (
    inventoryId: number,
    stoneInventoryId: number,
    useSafeStone: boolean,
  ) => Promise<ForgeResultVo>
  // hole 從 1 起算
  socket: (inventoryId: number, hole: number, stoneInventoryId: number) => Promise<ForgeResultVo>
  unsocket: (inventoryId: number, hole: number) => Promise<UnsocketVo>
  drill: (inventoryId: number) => Promise<ForgeResultVo>
  mergeStones: (stoneInventoryId: number, quantityPairs: number) => Promise<ForgeResultVo>
  mergeCrossStones: (
    firstInventoryId: number,
    secondInventoryId: number,
    quantityPairs: number,
  ) => Promise<ForgeResultVo>
  getRecipes: () => Promise<RecipeVo[]>
  synthesize: (recipeId: string, attempts: number) => Promise<SynthesisVo>
  getMissions: () => Promise<MissionBoardVo>
  acceptMission: (missionId: string) => Promise<MissionBoardVo>
  abandonMission: (missionId: string) => Promise<MissionBoardVo>
  completeMission: (missionId: string) => Promise<MissionCompletionVo>
  getPets: () => Promise<PetsVo>
  carryPet: (petId: number) => Promise<PetsVo>
  storePet: (petId: number) => Promise<PetsVo>
  releasePet: (petId: number) => Promise<PetsVo>
  renamePet: (petId: number, name: string) => Promise<PetsVo>
  feedPet: (petId: number, foodId: string) => Promise<PetFeedVo>
  getMercenaries: () => Promise<MercenaryOverviewVo>
  hireMercenary: (mercenaryId: string) => Promise<MercenaryHireVo>
  deployMercenary: (placeId: number) => Promise<MercenaryOverviewVo>
  restMercenary: () => Promise<MercenaryOverviewVo>
  renameMercenary: (name: string) => Promise<MercenaryOverviewVo>
  dismissMercenary: () => Promise<MercenaryOverviewVo>
  getHonourShop: () => Promise<HonourShopVo>
  exchangeHonour: (exchangeId: string) => Promise<HonourShopVo>
  updateSettings: (settings: AdventureSettings) => Promise<CharacterVo>
  // 只含還沒到期的密藥
  getBuffs: () => Promise<BuffVo[]>
  getInn: () => Promise<InnVo>
  bookInn: (roomId: string) => Promise<InnBookVo>
}

type Problem = { detail: string; reasons: string[] }

const isStringArray = (value: unknown): value is string[] =>
  Array.isArray(value) && value.every((item) => typeof item === "string")

const readProblem = async (response: Response): Promise<Problem> => {
  const problem: Problem = { detail: fallbackDetail, reasons: [] }
  try {
    const body: unknown = await response.json()
    if (typeof body !== "object" || body === null) return problem
    if ("detail" in body && typeof body.detail === "string" && body.detail !== "")
      problem.detail = body.detail
    if ("reasons" in body && isStringArray(body.reasons)) problem.reasons = body.reasons
  } catch {
    // 內容不是 JSON 時使用預設訊息
  }
  return problem
}

export const createApiClient = (options: {
  fetch: typeof fetch
  onUnauthorized: () => void
  requestTimeoutMilliseconds?: number
}): ApiClient => {
  const send = async (path: string, method: string, body?: unknown): Promise<Response> => {
    const headers = new Headers()
    const token = readAccessToken()
    if (token !== null) headers.set("Authorization", `Bearer ${token}`)
    if (body !== undefined) headers.set("Content-Type", "application/json")

    const response = await options.fetch(path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      // 逾時會以 TimeoutError 拋出，不轉成 ApiError，由呼叫端當成一般連線錯誤
      signal: AbortSignal.timeout(options.requestTimeoutMilliseconds ?? RequestTimeoutMilliseconds),
    })
    if (response.ok) return response

    const { detail, reasons } = await readProblem(response)
    if (response.status === 401) {
      clearAccessToken()
      options.onUnauthorized()
    }
    throw new ApiError(response.status, detail, reasons)
  }

  const requestJson = async <T>(path: string, method: string, body?: unknown): Promise<T> =>
    (await send(path, method, body)).json() as Promise<T>

  const requestCharacter = async (path: string, method: string, body?: unknown) =>
    CharacterConverter.toVo(await requestJson<unknown>(path, method, body))

  const requestPets = async (path: string, method: string, body?: unknown) =>
    PetsConverter.toVo(await requestJson<unknown>(path, method, body))

  const requestMercenaries = async (path: string, method: string, body?: unknown) =>
    MercenaryOverviewConverter.toVo(await requestJson<unknown>(path, method, body))

  const requestMissions = async (path: string, method: string, body?: unknown) =>
    MissionBoardConverter.toVo(await requestJson<unknown>(path, method, body))

  const requestForge = async (path: string, method: string, body?: unknown) =>
    ForgeResultConverter.toVo(await requestJson<unknown>(path, method, body))

  const requestInventory = async (path: string, method: string, body?: unknown) =>
    InventoryConverter.toVo(await requestJson<unknown>(path, method, body))

  return {
    register: async (username, password) => {
      await send("/api/accounts/register", "POST", { username, password })
    },
    login: async (username, password) => {
      const { accessToken } = await requestJson<{ accessToken: string }>(
        "/api/accounts/login",
        "POST",
        { username, password },
      )
      saveAccessToken(accessToken)
    },
    getMyCharacter: async () => {
      try {
        return await requestCharacter("/api/characters/me", "GET")
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) return null
        throw error
      }
    },
    createCharacter: (command) =>
      requestCharacter("/api/characters", "POST", CreateCharacterConverter.toDto(command)),
    advance: async () =>
      AdvanceConverter.toVo(await requestJson<unknown>("/api/adventure/advance", "POST")),
    getPublicJobs: async () =>
      parseArray(await requestJson<unknown>("/api/public/jobs", "GET"), "jobs", (item, path) =>
        JobConverter.toVo(item, path),
      ),
    getCreationOptions: async () =>
      CreationOptionsConverter.toVo(await requestJson<unknown>("/api/public/settings", "GET")),
    getJobs: async () =>
      parseArray(await requestJson<unknown>("/api/jobs", "GET"), "jobs", (item, path) =>
        JobChoiceConverter.toVo(item, path),
      ),
    changeJob: (jobId) => requestCharacter("/api/jobs/change", "POST", { jobId }),
    getSkills: async () =>
      SkillOverviewConverter.toVo(await requestJson<unknown>("/api/skills", "GET")),
    learnSkill: async (skillId) =>
      SkillOverviewConverter.toVo(
        await requestJson<unknown>("/api/skills/learn", "POST", { skillId }),
      ),
    setSkillSlots: async (slots) =>
      SkillOverviewConverter.toVo(
        await requestJson<unknown>("/api/skills/slots", "PUT", SkillSlotsConverter.toDto(slots)),
      ),
    getWorld: async () =>
      parseArray(await requestJson<unknown>("/api/world", "GET"), "world", (item, path) =>
        WorldConverter.toVo(item, path),
      ),
    moveTo: (placeId, subPlace) =>
      requestCharacter("/api/world/location", "PUT", { placeId, subPlace }),
    getInventory: () => requestInventory("/api/inventory", "GET"),
    equip: (inventoryId, quantity) =>
      requestInventory("/api/inventory/equip", "POST", { inventoryId, quantity }),
    unequip: (slot) => requestInventory("/api/inventory/unequip", "POST", { slot }),
    useItem: async (inventoryId) =>
      ItemUseConverter.toVo(
        await requestJson<unknown>("/api/inventory/use", "POST", { inventoryId }),
      ),
    getShop: async (category, level) =>
      parseArray(
        await requestJson<unknown>(
          `/api/shop?${new URLSearchParams({ category, level: String(level) }).toString()}`,
          "GET",
        ),
        "shop",
        (item, path) => ShopItemConverter.toVo(item, path),
      ),
    buy: (itemId, quantity) => requestInventory("/api/shop/buy", "POST", { itemId, quantity }),
    sell: (inventoryId, quantity) =>
      requestInventory("/api/shop/sell", "POST", { inventoryId, quantity }),
    getDepot: async () => DepotConverter.toVo(await requestJson<unknown>("/api/depot", "GET")),
    store: async (inventoryId, quantity) =>
      DepotMoveConverter.toVo(
        await requestJson<unknown>("/api/depot/store", "POST", { inventoryId, quantity }),
      ),
    take: async (depotId, quantity) =>
      DepotMoveConverter.toVo(
        await requestJson<unknown>("/api/depot/take", "POST", { depotId, quantity }),
      ),
    deposit: (amount) => requestCharacter("/api/bank/deposit", "POST", { amount }),
    withdraw: (amount) => requestCharacter("/api/bank/withdraw", "POST", { amount }),
    refine: (inventoryId, stoneInventoryId, useSafeStone) =>
      requestForge("/api/forge/refine", "POST", {
        inventoryId,
        stoneInventoryId,
        useSafeStone,
      }),
    socket: (inventoryId, hole, stoneInventoryId) =>
      requestForge("/api/forge/socket", "POST", {
        inventoryId,
        hole,
        stoneInventoryId,
      }),
    unsocket: async (inventoryId, hole) =>
      UnsocketConverter.toVo(
        await requestJson<unknown>("/api/forge/unsocket", "POST", { inventoryId, hole }),
      ),
    drill: (inventoryId) => requestForge("/api/forge/drill", "POST", { inventoryId }),
    mergeStones: (stoneInventoryId, quantityPairs) =>
      requestForge("/api/forge/merge-stones", "POST", {
        stoneInventoryId,
        quantityPairs,
      }),
    mergeCrossStones: (firstInventoryId, secondInventoryId, quantityPairs) =>
      requestForge("/api/forge/merge-cross", "POST", {
        firstInventoryId,
        secondInventoryId,
        quantityPairs,
      }),
    getRecipes: async () =>
      parseArray(await requestJson<unknown>("/api/forge/recipes", "GET"), "recipes", (item, path) =>
        RecipeConverter.toVo(item, path),
      ),
    synthesize: async (recipeId, attempts) =>
      SynthesisConverter.toVo(
        await requestJson<unknown>("/api/forge/synthesize", "POST", { recipeId, attempts }),
      ),
    getMissions: () => requestMissions("/api/missions", "GET"),
    acceptMission: (missionId) => requestMissions("/api/missions/accept", "POST", { missionId }),
    abandonMission: (missionId) => requestMissions("/api/missions/abandon", "POST", { missionId }),
    completeMission: async (missionId) =>
      MissionCompletionConverter.toVo(
        await requestJson<unknown>("/api/missions/complete", "POST", { missionId }),
      ),
    getPets: () => requestPets("/api/pets", "GET"),
    carryPet: (petId) => requestPets("/api/pets/carry", "POST", { petId }),
    storePet: (petId) => requestPets("/api/pets/store", "POST", { petId }),
    releasePet: (petId) => requestPets("/api/pets/release", "POST", { petId }),
    renamePet: (petId, name) => requestPets("/api/pets/rename", "POST", { petId, name }),
    feedPet: async (petId, foodId) =>
      PetFeedConverter.toVo(
        await requestJson<unknown>("/api/pets/feed", "POST", { petId, foodId }),
      ),
    getMercenaries: () => requestMercenaries("/api/mercenaries", "GET"),
    hireMercenary: async (mercenaryId) =>
      MercenaryHireConverter.toVo(
        await requestJson<unknown>("/api/mercenaries/hire", "POST", { mercenaryId }),
      ),
    deployMercenary: (placeId) =>
      requestMercenaries("/api/mercenaries/deploy", "POST", { placeId }),
    restMercenary: () => requestMercenaries("/api/mercenaries/rest", "POST"),
    renameMercenary: (name) => requestMercenaries("/api/mercenaries/rename", "POST", { name }),
    dismissMercenary: () => requestMercenaries("/api/mercenaries/dismiss", "POST"),
    getHonourShop: async () =>
      HonourShopConverter.toVo(await requestJson<unknown>("/api/honour", "GET")),
    exchangeHonour: async (exchangeId) =>
      HonourShopConverter.toVo(
        await requestJson<unknown>("/api/honour/exchange", "POST", { exchangeId }),
      ),
    getBuffs: async () =>
      parseArray(
        await requestJson<unknown>("/api/characters/me/buffs", "GET"),
        "buffs",
        (item, path) => BuffConverter.toVo(item, path),
      ),
    getInn: async () => InnConverter.toVo(await requestJson<unknown>("/api/inn", "GET")),
    bookInn: async (roomId) =>
      InnBookConverter.toVo(await requestJson<unknown>("/api/inn/book", "POST", { roomId })),
    updateSettings: (settings) =>
      requestCharacter(
        "/api/characters/me/settings",
        "PUT",
        AdventureSettingsConverter.toDto(settings),
      ),
  }
}
