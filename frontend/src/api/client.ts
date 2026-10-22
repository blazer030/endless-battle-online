import type {
  AdvanceResponse,
  CharacterView,
  ContentNames,
  CreateCharacterCommand,
  DepotMove,
  DepotView,
  EquipmentSlot,
  ForgeResult,
  InventoryView,
  ItemCategory,
  JobOption,
  MissionBoard,
  MissionCompletion,
  Recipe,
  ShopItem,
  SkillOverview,
  SkillSlotCommand,
  SynthesisResult,
  UnsocketResult,
  WorldArea,
} from "../game/types"
import { RequestTimeoutMilliseconds } from "../motion/timings"
import { clearAccessToken, readAccessToken, saveAccessToken } from "./authStorage"

const fallbackDetail = "發生錯誤，請稍後再試"

export class ApiError extends Error {
  readonly status: number
  readonly detail: string
  // ProblemDetails 的 reasons 擴充欄位：驗證失敗的原因代碼
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
  getMyCharacter: () => Promise<CharacterView | null>
  createCharacter: (command: CreateCharacterCommand) => Promise<CharacterView>
  advance: () => Promise<AdvanceResponse>
  getContent: () => Promise<ContentNames>
  getJobs: () => Promise<JobOption[]>
  changeJob: (jobId: string) => Promise<CharacterView>
  getSkills: () => Promise<SkillOverview>
  learnSkill: (skillId: string) => Promise<SkillOverview>
  setSkillSlots: (slots: SkillSlotCommand[]) => Promise<SkillOverview>
  getWorld: () => Promise<WorldArea[]>
  moveTo: (placeId: number, subPlace: number | null) => Promise<CharacterView>
  getInventory: () => Promise<InventoryView>
  // quantity 只用於補給品：從那一格分出幾個放到補給欄
  equip: (inventoryId: number, quantity?: number) => Promise<InventoryView>
  unequip: (slot: EquipmentSlot) => Promise<InventoryView>
  useItem: (inventoryId: number) => Promise<InventoryView>
  getShop: (category: ItemCategory, level: number) => Promise<ShopItem[]>
  buy: (itemId: string, quantity: number) => Promise<InventoryView>
  sell: (inventoryId: number, quantity: number) => Promise<InventoryView>
  getDepot: () => Promise<DepotView>
  store: (inventoryId: number, quantity: number) => Promise<DepotMove>
  take: (depotId: number, quantity: number) => Promise<DepotMove>
  deposit: (amount: number) => Promise<CharacterView>
  withdraw: (amount: number) => Promise<CharacterView>
  refine: (
    inventoryId: number,
    stoneInventoryId: number,
    useSafeStone: boolean,
  ) => Promise<ForgeResult>
  // hole 從 1 起算
  socket: (inventoryId: number, hole: number, stoneInventoryId: number) => Promise<ForgeResult>
  unsocket: (inventoryId: number, hole: number) => Promise<UnsocketResult>
  drill: (inventoryId: number) => Promise<ForgeResult>
  mergeStones: (stoneInventoryId: number, quantityPairs: number) => Promise<ForgeResult>
  getRecipes: () => Promise<Recipe[]>
  synthesize: (recipeId: string, attempts: number) => Promise<SynthesisResult>
  getMissions: () => Promise<MissionBoard>
  acceptMission: (missionId: string) => Promise<MissionBoard>
  abandonMission: (missionId: string) => Promise<MissionBoard>
  completeMission: (missionId: string) => Promise<MissionCompletion>
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
        return await requestJson<CharacterView>("/api/characters/me", "GET")
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) return null
        throw error
      }
    },
    createCharacter: (command) => requestJson<CharacterView>("/api/characters", "POST", command),
    advance: () => requestJson<AdvanceResponse>("/api/adventure/advance", "POST"),
    getContent: () => requestJson<ContentNames>("/api/content", "GET"),
    getJobs: () => requestJson<JobOption[]>("/api/jobs", "GET"),
    changeJob: (jobId) => requestJson<CharacterView>("/api/jobs/change", "POST", { jobId }),
    getSkills: () => requestJson<SkillOverview>("/api/skills", "GET"),
    learnSkill: (skillId) => requestJson<SkillOverview>("/api/skills/learn", "POST", { skillId }),
    setSkillSlots: (slots) => requestJson<SkillOverview>("/api/skills/slots", "PUT", { slots }),
    getWorld: () => requestJson<WorldArea[]>("/api/world", "GET"),
    moveTo: (placeId, subPlace) =>
      requestJson<CharacterView>("/api/world/location", "PUT", { placeId, subPlace }),
    getInventory: () => requestJson<InventoryView>("/api/inventory", "GET"),
    equip: (inventoryId, quantity) =>
      requestJson<InventoryView>("/api/inventory/equip", "POST", { inventoryId, quantity }),
    unequip: (slot) => requestJson<InventoryView>("/api/inventory/unequip", "POST", { slot }),
    useItem: (inventoryId) =>
      requestJson<InventoryView>("/api/inventory/use", "POST", { inventoryId }),
    getShop: (category, level) =>
      requestJson<ShopItem[]>(
        `/api/shop?${new URLSearchParams({ category, level: String(level) }).toString()}`,
        "GET",
      ),
    buy: (itemId, quantity) =>
      requestJson<InventoryView>("/api/shop/buy", "POST", { itemId, quantity }),
    sell: (inventoryId, quantity) =>
      requestJson<InventoryView>("/api/shop/sell", "POST", { inventoryId, quantity }),
    getDepot: () => requestJson<DepotView>("/api/depot", "GET"),
    store: (inventoryId, quantity) =>
      requestJson<DepotMove>("/api/depot/store", "POST", { inventoryId, quantity }),
    take: (depotId, quantity) =>
      requestJson<DepotMove>("/api/depot/take", "POST", { depotId, quantity }),
    deposit: (amount) => requestJson<CharacterView>("/api/bank/deposit", "POST", { amount }),
    withdraw: (amount) => requestJson<CharacterView>("/api/bank/withdraw", "POST", { amount }),
    refine: (inventoryId, stoneInventoryId, useSafeStone) =>
      requestJson<ForgeResult>("/api/forge/refine", "POST", {
        inventoryId,
        stoneInventoryId,
        useSafeStone,
      }),
    socket: (inventoryId, hole, stoneInventoryId) =>
      requestJson<ForgeResult>("/api/forge/socket", "POST", {
        inventoryId,
        hole,
        stoneInventoryId,
      }),
    unsocket: (inventoryId, hole) =>
      requestJson<UnsocketResult>("/api/forge/unsocket", "POST", { inventoryId, hole }),
    drill: (inventoryId) => requestJson<ForgeResult>("/api/forge/drill", "POST", { inventoryId }),
    mergeStones: (stoneInventoryId, quantityPairs) =>
      requestJson<ForgeResult>("/api/forge/merge-stones", "POST", {
        stoneInventoryId,
        quantityPairs,
      }),
    getRecipes: () => requestJson<Recipe[]>("/api/forge/recipes", "GET"),
    synthesize: (recipeId, attempts) =>
      requestJson<SynthesisResult>("/api/forge/synthesize", "POST", { recipeId, attempts }),
    getMissions: () => requestJson<MissionBoard>("/api/missions", "GET"),
    acceptMission: (missionId) =>
      requestJson<MissionBoard>("/api/missions/accept", "POST", { missionId }),
    abandonMission: (missionId) =>
      requestJson<MissionBoard>("/api/missions/abandon", "POST", { missionId }),
    completeMission: (missionId) =>
      requestJson<MissionCompletion>("/api/missions/complete", "POST", { missionId }),
  }
}
