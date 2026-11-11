import type {
  BuffView,
  InnBookResult,
  InnView,
  HonourShop,
  MercenaryHireResult,
  MercenaryOverview,
  MissionBoard,
  MissionCompletion,
  PetFeedResult,
  PetsView,
} from "../game/types"
import type { AdvanceVo } from "../game/battleEvents"
import type { JobChoiceVo, SkillOverviewVo, SkillSlotCommand } from "../game/skillOverview"
import type { WorldAreaVo } from "../game/world"
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
import type { components } from "./schema"
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
  getMissions: () => Promise<MissionBoard>
  acceptMission: (missionId: string) => Promise<MissionBoard>
  abandonMission: (missionId: string) => Promise<MissionBoard>
  completeMission: (missionId: string) => Promise<MissionCompletion>
  getPets: () => Promise<PetsView>
  carryPet: (petId: number) => Promise<PetsView>
  storePet: (petId: number) => Promise<PetsView>
  releasePet: (petId: number) => Promise<PetsView>
  renamePet: (petId: number, name: string) => Promise<PetsView>
  feedPet: (petId: number, foodId: string) => Promise<PetFeedResult>
  getMercenaries: () => Promise<MercenaryOverview>
  hireMercenary: (mercenaryId: string) => Promise<MercenaryHireResult>
  deployMercenary: (placeId: number) => Promise<MercenaryOverview>
  restMercenary: () => Promise<MercenaryOverview>
  renameMercenary: (name: string) => Promise<MercenaryOverview>
  dismissMercenary: () => Promise<MercenaryOverview>
  getHonourShop: () => Promise<HonourShop>
  exchangeHonour: (exchangeId: string) => Promise<HonourShop>
  updateSettings: (settings: AdventureSettings) => Promise<CharacterVo>
  // 只含還沒到期的密藥
  getBuffs: () => Promise<BuffView[]>
  getInn: () => Promise<InnView>
  bookInn: (roomId: string) => Promise<InnBookResult>
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

  const requestForge = async (path: string, method: string, body?: unknown) =>
    ForgeResultConverter.toVo(await requestJson<unknown>(path, method, body))

  const requestInventory = async (path: string, method: string, body?: unknown) =>
    InventoryConverter.toVo(await requestJson<unknown>(path, method, body))

  // 回應裡夾帶的角色一律轉成 CharacterVo，其餘欄位還是產生的型別
  const requestWithCharacter = async <T extends { character: unknown }>(
    path: string,
    method: string,
    body?: unknown,
  ): Promise<Omit<T, "character"> & { character: CharacterVo }> => {
    const response = await requestJson<T>(path, method, body)
    return { ...response, character: CharacterConverter.toVo(response.character) }
  }

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
    getMissions: () => requestJson<MissionBoard>("/api/missions", "GET"),
    acceptMission: (missionId) =>
      requestJson<MissionBoard>("/api/missions/accept", "POST", { missionId }),
    abandonMission: (missionId) =>
      requestJson<MissionBoard>("/api/missions/abandon", "POST", { missionId }),
    completeMission: (missionId) =>
      requestWithCharacter<components["schemas"]["MissionCompleteResponse"]>(
        "/api/missions/complete",
        "POST",
        { missionId },
      ),
    getPets: () => requestJson<PetsView>("/api/pets", "GET"),
    carryPet: (petId) => requestJson<PetsView>("/api/pets/carry", "POST", { petId }),
    storePet: (petId) => requestJson<PetsView>("/api/pets/store", "POST", { petId }),
    releasePet: (petId) => requestJson<PetsView>("/api/pets/release", "POST", { petId }),
    renamePet: (petId, name) => requestJson<PetsView>("/api/pets/rename", "POST", { petId, name }),
    feedPet: (petId, foodId) =>
      requestWithCharacter<components["schemas"]["PetFeedResponse"]>("/api/pets/feed", "POST", {
        petId,
        foodId,
      }),
    getMercenaries: () => requestJson<MercenaryOverview>("/api/mercenaries", "GET"),
    hireMercenary: (mercenaryId) =>
      requestWithCharacter<components["schemas"]["MercenaryHireResponse"]>(
        "/api/mercenaries/hire",
        "POST",
        { mercenaryId },
      ),
    deployMercenary: (placeId) =>
      requestJson<MercenaryOverview>("/api/mercenaries/deploy", "POST", { placeId }),
    restMercenary: () => requestJson<MercenaryOverview>("/api/mercenaries/rest", "POST"),
    renameMercenary: (name) =>
      requestJson<MercenaryOverview>("/api/mercenaries/rename", "POST", { name }),
    dismissMercenary: () => requestJson<MercenaryOverview>("/api/mercenaries/dismiss", "POST"),
    getHonourShop: () => requestJson<HonourShop>("/api/honour", "GET"),
    exchangeHonour: (exchangeId) =>
      requestJson<HonourShop>("/api/honour/exchange", "POST", { exchangeId }),
    getBuffs: () => requestJson<BuffView[]>("/api/characters/me/buffs", "GET"),
    getInn: () => requestJson<InnView>("/api/inn", "GET"),
    bookInn: (roomId) =>
      requestWithCharacter<components["schemas"]["InnBookResponse"]>("/api/inn/book", "POST", {
        roomId,
      }),
    updateSettings: (settings) =>
      requestCharacter(
        "/api/characters/me/settings",
        "PUT",
        AdventureSettingsConverter.toDto(settings),
      ),
  }
}
