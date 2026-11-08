# 計畫 2：前端地基與歷練主畫面 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建好 React 前端。玩家可以在手機或電腦的瀏覽器上註冊、登入、建立角色，然後看到水墨風的歷練主畫面：上半部對戰舞台、下半部戰報，持續播放伺服器結算出來的戰鬥。

**Architecture:**
- 前端是獨立的 Vite＋React＋TypeScript 專案，開發時用 Vite proxy 把 `/api` 轉到本機的 .NET API，所以不需要 CORS。
- 遊戲狀態的唯一來源是純 TypeScript 的「播放模型」（`src/game/`）：它把推進 API 回來的事件排上時間軸，React 的戰報和 Pixi 的舞台都只是訂閱它的視圖。這直接實現設計文件 §6.3「兩邊吃同一份事件、永遠一致」。
- 舞台用一個 Pixi `Application`，特效用 GSAP；其餘介面是一般 DOM。
- 後端只做兩處小改：遭遇事件帶上雙方血量上限、新增唯讀的內容 API（名稱查詢）。

**Tech Stack:**
- 前端：React 19、TypeScript 6.0.x（`typescript-eslint` 目前只支援 `<6.1.0`，2026-10-21 以 `npm view` 確認）、Vite 8、Vitest 5、Testing Library、PixiJS 8、GSAP 3、`openapi-typescript` 7、pnpm 11、Node 24
- 後端：沿用計畫 1

**Spec:** `docs/superpowers/specs/2026-10-21-inkbound-design.md`；視覺驗收基準：`docs/superpowers/mockups/battle-v2.html`

## Global Constraints

- 計畫 1 的 Global Constraints 全部繼續適用（後端）。
- 前端在 `frontend/`，套件管理用 pnpm；TypeScript `strict`，禁用 `any`。
- 手機直式優先：以 375×667 為主要設計尺寸，360～430 寬度都不能出現水平捲動；電腦上整個畫面置中、最大寬度 480px。
- 視覺基準是 `battle-v2.html`：
  - 宣紙底色 `#f3ead8` → `#e6d9bd` 漸層、墨色 `#2b2620`、朱紅 `#b8322a`、金 `#a87a12`／劍氣 `#d4a017`；
  - 字型「霞鶩文楷 TC」（`LXGW WenKai TC`，Google Fonts，支援繁體），標題與妖怪名稱用 700 字重；
  - 顏色只定義在 `src/ui/theme.css` 的 CSS 變數，其他地方不得寫死色碼（Pixi 端從 `src/render/palette.ts` 取，數值與 CSS 變數一致）。
- 遊戲狀態只存在 `src/game/` 的純 TS 物件；React state 與 Pixi 物件都不持有權威狀態。`src/game/` 不得 import React、Pixi、GSAP 或 `src/api/` 以外的瀏覽器 API。
- 整個 app 只有一個 Pixi `Application`；Pixi `Text` 一律明確指定 `resolution: window.devicePixelRatio`。
- 動畫時長、輪詢間隔等常數集中在 `src/motion/timings.ts`。
- 美術圖放 `frontend/src/assets/art/<內容 id>.webp`，以 `import.meta.glob` 建索引；沒有圖的 id 走書法字退路。本計畫不加入任何美術圖。
- API 型別由 `pnpm generate:api` 從執行中的 API 的 `/openapi/v1.json` 產生到 `src/api/schema.ts`，產生檔不得手改。
- 介面文字暫定：底部導覽「歷練、本命、經脈、行囊」，後三個本計畫顯示為停用。
- `./scripts/check.sh` 擴充為同時跑前端的 lint、format 檢查、typecheck、test。
- Commit 規則同計畫 1。

## Review Focus

- **很久沒上線後第一次推進**（最多 5 分鐘、上百個事件）：不能把幾分鐘的戰鬥全部慢慢播，要先顯示一行「離開期間」摘要，只播最後一場。→ Task 7 `enqueue_CompressesLongBatchIntoSummary`。
- **連續兩次輪詢的事件不能重疊或亂序**：第二批要接在第一批播完之後。→ Task 7 `enqueue_ChainsBatchesBackToBack`。
- **畫面上的修為／靈石要跟著戰報走**：伺服器回應先到，但數字要等對應的獎勵事件播出才增加。→ Task 7 `displayedCharacter_ExcludesUnplayedRewards`。
- **token 過期或被清掉**：任何 API 回 401 都回到登入畫面，不卡在錯誤狀態。→ Task 5 `request_ClearsTokenAndNotifies_On401`、Task 6 `App_ShowsLogin_WhenSessionExpires`。
- **兩個分頁同時輪詢造成 409**：忽略這次，下一輪照常；畫面不出錯。→ Task 10 `adventureLoop_IgnoresConflictAndKeepsPolling`。

---

## File Structure

```
backend/
  Inkbound.Domain/Combat/BattleEvents.cs                 EncounterEvent 加上血量上限（Task 2）
  Inkbound.Domain/Adventure/AdventureSimulator.cs         （Task 2）
  Inkbound.Application/Content/IGameContent.cs            加 AllMonsters / AllStages（Task 3）
  Inkbound.Infrastructure/Content/JsonGameContent.cs      （Task 3）
  Inkbound.Api/Endpoints/ContentEndpoints.cs              GET /api/content（Task 3）
frontend/
  package.json, vite.config.ts, tsconfig*.json, eslint.config.js, .prettierrc, index.html
  src/main.tsx
  src/api/schema.ts                 產生檔
  src/api/client.ts                 fetch 包裝、token、ApiError
  src/api/authStorage.ts            localStorage 存取（包 try/catch）
  src/game/types.ts                 從 schema 取出的 BattleEvent、CharacterView 等別名
  src/game/battlePlayback.ts        播放模型（時間軸、壓縮、顯示用數值）
  src/game/battleLog.ts             事件 → 中文戰報行
  src/game/stageModel.ts            事件 → 舞台狀態與特效清單
  src/motion/timings.ts             時長與間隔常數
  src/render/palette.ts             Pixi 用色
  src/render/artIndex.ts            import.meta.glob 美術索引與退路判斷
  src/render/StageRenderer.ts       單一 Pixi Application、立繪／書法字、特效
  src/render/PixiStage.tsx          把 StageRenderer 掛進 React
  src/ui/theme.css                  CSS 變數與全域樣式
  src/ui/App.tsx                    依登入與角色狀態切換畫面
  src/ui/AuthScreen.tsx             登入／註冊
  src/ui/CreateCharacterScreen.tsx  建立角色
  src/ui/AdventureScreen.tsx        歷練主畫面（頂欄、舞台、戰報、導覽）
  src/ui/BattleLog.tsx              戰報與分頁
  src/ui/useAdventureLoop.ts        輪詢與播放迴圈
  src/test/setup.ts                 Testing Library 設定
scripts/check.sh                    加入前端檢查
```

---

### Task 1: 固定後端套件版本

**Files:** Modify `backend/Inkbound.Infrastructure/Inkbound.Infrastructure.csproj`, `backend/Inkbound.Api/Inkbound.Api.csproj`

計畫 1 最終審查延後的 Minor：浮動的 `10.*` 版本可能讓 build 在新版發佈時突然失敗（警告視為錯誤）。

- [ ] **Step 1:** 把 `MySql.EntityFrameworkCore` 固定為 `10.0.9`、`Microsoft.EntityFrameworkCore.Design` 固定為 `10.0.13`（2026-10-21 實際解析到的版本）。
- [ ] **Step 2:** `./scripts/check.sh`，預期 56/56 通過、零警告。
- [ ] **Step 3: Commit** `🚧 chore: pin EF Core MySQL and design package versions`

---

### Task 2: 遭遇事件帶上雙方血量上限

**Files:**
- Modify: `backend/Inkbound.Domain/Combat/BattleEvents.cs`, `backend/Inkbound.Domain/Adventure/AdventureSimulator.cs`
- Test: `backend/Inkbound.Tests/Domain/AdventureSimulatorTests.cs`

**Interfaces:**
- Produces: `public sealed record EncounterEvent(double AtSeconds, string MonsterId, int MonsterMaxHealth, int PlayerMaxHealth) : BattleEvent(AtSeconds);`
- 原因：前端要畫血條，但攻擊事件只有「剩餘血量」。

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void Run_EncounterCarriesMaxHealthOfBothSides()
{
    var run = AdventureSimulator.Run(TestStats.Make(maxHealth: 120, attack: 10), [Monster("sprite", 20, 1)], availableSeconds: 1, new ScriptedRandom(0.99));
    var encounter = run.Events.OfType<EncounterEvent>().First();
    Assert.Equal(20, encounter.MonsterMaxHealth);
    Assert.Equal(120, encounter.PlayerMaxHealth);
}
```

- [ ] **Step 2:** `cd backend && dotnet test --filter-class "*AdventureSimulatorTests"`，預期編譯失敗。
- [ ] **Step 3:** 擴充 record，`AdventureSimulator` 建立遭遇事件時填入 `monster.Stats.MaxHealth` 與 `player.MaxHealth`。
- [ ] **Step 4:** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 5: Commit** `✨ feat: include max health in encounter events`

---

### Task 3: 內容名稱 API

**Files:**
- Modify: `backend/Inkbound.Application/Content/IGameContent.cs`, `backend/Inkbound.Infrastructure/Content/JsonGameContent.cs`, `backend/Inkbound.Api/Program.cs`, `backend/Inkbound.Api/Endpoints/Contracts.cs`
- Create: `backend/Inkbound.Api/Endpoints/ContentEndpoints.cs`
- Test: `backend/Inkbound.Tests/Api/ContentEndpointsTests.cs`

**Interfaces:**
- Produces:
  - `IGameContent` 新增 `IReadOnlyList<MonsterDefinition> AllMonsters { get; }`、`IReadOnlyList<StageDefinition> AllStages { get; }`（依 JSON 中的順序）。
  - `GET /api/content`（不需登入）→ `200 ContentResponse`：

```csharp
public sealed record ContentResponse(IReadOnlyList<NamedContent> Monsters, IReadOnlyList<NamedContent> Stages);
public sealed record NamedContent(string Id, string Name);
```

  - 只回傳 id 與名稱，不外洩數值。

- [ ] **Step 1: Write the failing test** `ContentEndpointsTests.GetContent_ReturnsMonsterAndStageNames_WithoutLogin`：不帶 token 呼叫 → 200；`monsters` 等於 `[{ id: "test-sprite", name: "測試雪精" }, { id: "test-wolf", name: "測試寒狼" }]`；`stages` 等於 `[{ id: "test-stage-1", name: "測試關卡・一" }]`；回應 JSON 文字不含 `"maxHealth"`。
- [ ] **Step 2:** 執行，預期 404 失敗。
- [ ] **Step 3:** 實作介面屬性與端點。
- [ ] **Step 4:** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 5: Commit** `✨ feat: expose content names for the client`

---

### Task 4: 前端專案骨架

**Files:** Create `frontend/` 下的設定檔、`index.html`、`src/main.tsx`、`src/ui/App.tsx`、`src/ui/theme.css`、`src/test/setup.ts`；Modify `scripts/check.sh`、`CLAUDE.md`、`.gitignore`

**Interfaces:**
- Produces: `pnpm check` = `eslint . && prettier --check . && tsc -b && vitest run`；`pnpm dev`（含 proxy）；`pnpm generate:api`。

- [ ] **Step 1: 建立專案**

```bash
cd ~/Project/Inkbound
pnpm create vite frontend --template react-ts
cd frontend
pnpm add pixi.js gsap
pnpm add -D typescript@~6.0.2 openapi-typescript vitest jsdom @testing-library/react @testing-library/dom @testing-library/user-event @testing-library/jest-dom prettier eslint-config-prettier
```

- 範本產生的示範元件、圖片、`App.css` 刪除。
- `package.json` scripts：`dev`、`build`（`tsc -b && vite build`）、`test`（`vitest run`）、`typecheck`（`tsc -b`）、`lint`（`eslint .`）、`format`、`format:check`、`check`（上述四項）、`generate:api`（`openapi-typescript http://localhost:5062/openapi/v1.json -o src/api/schema.ts`）。
- `vite.config.ts`：
  - `server.host: true`（手機在同一個 Wi-Fi 可以連）；
  - `server.proxy`：`/api` 與 `/openapi` 轉到 `http://localhost:5062`；
  - `test`：`environment: "jsdom"`、`setupFiles: ["src/test/setup.ts"]`（載入 `@testing-library/jest-dom/vitest`）。
- `eslint.config.js` 在範本設定上加入 `eslint-config-prettier`；`.prettierrc` 用 `{ "semi": false, "printWidth": 100 }`。
- `index.html`：`lang="zh-Hant"`、`<title>墨渡</title>`、`viewport` 含 `viewport-fit=cover`、Google Fonts 載入 `LXGW WenKai TC` 400/700。
- `src/ui/theme.css`：上面 Global Constraints 的色票寫成 CSS 變數（`--paper-light`、`--paper-dark`、`--ink`、`--vermilion`、`--gold`、`--sword-gold`），`body` 用宣紙漸層與霞鶩文楷；`#root` 置中、`max-width: 480px`、`min-height: 100dvh`。

- [ ] **Step 2: Write the failing test** `src/ui/App.test.tsx`：`render(<App />)` 後，`screen.getByRole("heading", { name: "墨渡" })` 存在。
- [ ] **Step 3:** `pnpm test`，預期失敗（範本的 App 沒有這個標題）。
- [ ] **Step 4:** `App.tsx` 先只渲染 `<h1>墨渡</h1>`。
- [ ] **Step 5:** `pnpm check`，預期全部通過；`scripts/check.sh` 在後端檢查之後加上 `cd "$repository_root/frontend" && pnpm install --frozen-lockfile && pnpm check`。
- [ ] **Step 6:** `CLAUDE.md` 補上前端結構、`pnpm dev`、`pnpm generate:api` 與 Global Constraints 中的前端規則；`.gitignore` 確認含 `node_modules/`、`dist/`。
- [ ] **Step 7:** `./scripts/check.sh`，預期全部通過。
- [ ] **Step 8: Commit** `🚧 chore: scaffold React frontend with Vite and tooling`

---

### Task 5: API 客戶端與 token 保存

**Files:**
- Create: `frontend/src/api/schema.ts`（產生）、`frontend/src/api/client.ts`、`frontend/src/api/authStorage.ts`、`frontend/src/game/types.ts`
- Test: `frontend/src/api/client.test.ts`, `frontend/src/api/authStorage.test.ts`

**Interfaces:**
- Produces:

```ts
// authStorage.ts —— 所有存取都包 try/catch；無痕模式等讀不到時回傳 null
export const readAccessToken: () => string | null
export const saveAccessToken: (token: string) => void
export const clearAccessToken: () => void

// client.ts
export class ApiError extends Error { constructor(readonly status: number, readonly detail: string) }
export type ApiClient = {
  register: (username: string, password: string) => Promise<void>
  login: (username: string, password: string) => Promise<void>        // 成功後 saveAccessToken
  getMyCharacter: () => Promise<CharacterView | null>                  // 404 → null
  createCharacter: (name: string) => Promise<CharacterView>
  advance: () => Promise<AdvanceResponse>
  getContent: () => Promise<ContentNames>
}
export const createApiClient: (options: { fetch: typeof fetch; onUnauthorized: () => void }) => ApiClient

// game/types.ts —— 全部從 schema.ts 的 components["schemas"] 取別名
export type CharacterView, AdvanceResponse, BattleEvent, EncounterEvent, AttackEvent, DefeatEvent, RewardEvent, ContentNames
```

規則：
- 有 token 時每個請求加 `Authorization: Bearer <token>`。
- 回應不是 2xx：讀 ProblemDetails 的 `detail`（讀不到就用 `"發生錯誤，請稍後再試"`），丟 `ApiError`。
- 401：先 `clearAccessToken()`，再呼叫 `onUnauthorized()`，然後丟 `ApiError`。

- [ ] **Step 1: 產生型別**：`docker compose up -d`，另開終端機 `cd backend && dotnet run --project Inkbound.Api`，再 `cd frontend && pnpm generate:api`。確認 `schema.ts` 中有 `EncounterEvent` 的 `monsterMaxHealth` 與 `/api/content`。
- [ ] **Step 2: Write the failing tests**（`fetch` 用 `vi.fn` 回傳 `new Response(...)`）
  - `request_AddsBearerToken_WhenLoggedIn`：先 `saveAccessToken("abc")`，呼叫 `advance()`，斷言 fetch 收到的 headers 含 `Authorization: Bearer abc`。
  - `login_SavesToken`：fetch 回 `{ accessToken: "xyz" }` → `readAccessToken() === "xyz"`。
  - `getMyCharacter_ReturnsNull_On404`。
  - `request_ThrowsApiErrorWithDetail_OnConflict`：409 加 `{ detail: "角色名稱已被使用" }` → `ApiError`，`status === 409`、`detail` 相同。
  - `request_ClearsTokenAndNotifies_On401`：401 → token 被清掉、`onUnauthorized` 被呼叫一次。
  - `authStorage_ReturnsNull_WhenStorageThrows`：用 `vi.spyOn(Storage.prototype, "getItem")` 讓它丟例外 → `readAccessToken() === null`。
- [ ] **Step 3:** `pnpm test`，預期失敗。
- [ ] **Step 4:** 實作。
- [ ] **Step 5:** `pnpm check`，預期通過。
- [ ] **Step 6: Commit** `✨ feat: add typed API client with token handling`

---

### Task 6: 登入、註冊與建立角色畫面

**Files:**
- Create: `frontend/src/ui/AuthScreen.tsx`, `frontend/src/ui/CreateCharacterScreen.tsx`
- Modify: `frontend/src/ui/App.tsx`, `frontend/src/main.tsx`
- Test: `frontend/src/ui/App.test.tsx`（取代 Task 4 的測試）, `frontend/src/ui/AuthScreen.test.tsx`, `frontend/src/ui/CreateCharacterScreen.test.tsx`

**Interfaces:**
- Consumes: `ApiClient`, `ApiError`, `readAccessToken`
- Produces:

```ts
export type SessionExpiryListener = (listener: () => void) => () => void   // 回傳取消訂閱
export const App: (props: { api: ApiClient; subscribeToSessionExpiry: SessionExpiryListener }) => JSX.Element
```

`main.tsx` 建立一個 listener 集合，`createApiClient` 的 `onUnauthorized` 通知所有 listener；App 在掛載時訂閱，收到通知就回到登入畫面。

畫面流程：
- 沒有 token → `AuthScreen`。
- 有 token → 呼叫 `getMyCharacter()`：`null` → `CreateCharacterScreen`；有角色 → `AdventureScreen`（本任務先用 `<p>歷練中</p>` 佔位，Task 10 換成真正的畫面）。
- 任何時候收到 `onUnauthorized` → 回到 `AuthScreen`。

`AuthScreen`：
- 標題「墨渡」（書法字、大字、朱紅印章樣式的副標「水墨修仙・放置」）；
- 帳號、密碼兩個欄位，`登入`、`註冊` 兩個按鈕；註冊成功後自動登入；
- `ApiError.detail` 顯示在表單下方（`role="alert"`）。

`CreateCharacterScreen`：
- 說明「為你的修仙者取個名字（2～12 字）」、名稱欄位、`踏入仙途` 按鈕；
- 錯誤顯示方式同上；成功後切到歷練畫面。

- [ ] **Step 1: Write the failing tests**（`api` 傳入以 `vi.fn` 組成的假 client）
  - `App_ShowsLogin_WhenNoToken`。
  - `App_ShowsCreateCharacter_WhenLoggedInWithoutCharacter`。
  - `App_ShowsAdventure_WhenCharacterExists`。
  - `App_ShowsLogin_WhenSessionExpires`：先進入歷練畫面，再呼叫測試自己傳入的 `subscribeToSessionExpiry` 所收到的 listener → 回到登入畫面。
  - `AuthScreen_RegisterThenLogin_CallsBothInOrder`。
  - `AuthScreen_ShowsServerDetail_OnError`：`login` 丟 `new ApiError(401, "帳號或密碼錯誤")` → 畫面出現該文字。
  - `CreateCharacterScreen_ShowsConflictDetail`：`createCharacter` 丟 409 → 畫面出現 detail。
- [ ] **Step 2:** 執行，預期失敗。
- [ ] **Step 3:** 實作。
- [ ] **Step 4:** `pnpm check`，預期通過。
- [ ] **Step 5: Commit** `✨ feat: add sign-in and character creation screens`

---

### Task 7: 播放模型

**Files:**
- Create: `frontend/src/game/battlePlayback.ts`, `frontend/src/motion/timings.ts`
- Test: `frontend/src/game/battlePlayback.test.ts`

**Interfaces:**
- Consumes: `AdvanceResponse`, `BattleEvent`, `CharacterView`
- Produces:

```ts
// motion/timings.ts
export const PollIntervalMilliseconds = 10_000
export const CatchUpThresholdSeconds = 30
export const RestAfterBatchSeconds = 1

// game/battlePlayback.ts
export type PlaybackItem =
  | { kind: "event"; event: BattleEvent }
  | { kind: "summary"; fights: number; experience: number; spiritStones: number }
export class BattlePlayback {
  constructor(character: CharacterView)
  enqueue(response: AdvanceResponse, receivedAtMilliseconds: number): void
  takeDue(nowMilliseconds: number): PlaybackItem[]
  get displayedCharacter(): CharacterView
}
```

演算法：
- 事件為空：只更新已確認的角色資料。
- 一批事件的長度（最後一個事件的 `atSeconds`）超過 `CatchUpThresholdSeconds`：
  - 最後一個 `encounter` 之前的事件不播放，改成一個立即到期的 `summary`（場數 = 那段的 encounter 數，修為與靈石 = 那段 reward 的加總）；
  - 從最後一個 `encounter` 開始的事件，時間平移成從 0 開始，照常排程。
- 排程：`batchStart = max(receivedAt, timelineEnd)`；每個事件在 `batchStart + atSeconds × 1000` 到期；`timelineEnd = batchStart + (最後 atSeconds + RestAfterBatchSeconds) × 1000`。
- `takeDue` 依到期時間、再依原順序回傳並移除到期項目。
- `displayedCharacter` = 最新回應的角色資料，減去「尚未播出的 reward」的修為與靈石。

- [ ] **Step 1: Write the failing tests**
  - `takeDue_ReleasesEventsAtTheirOffsets`：在 t=1000 enqueue 事件 atSeconds 0、1.5 → `takeDue(1000)` 得到第一個、`takeDue(2499)` 得到空、`takeDue(2500)` 得到第二個。
  - `enqueue_ChainsBatchesBackToBack`：第一批最後事件在 6 秒，於 t=0 enqueue；第二批於 t=2000 enqueue，其第一個事件 atSeconds 0 → 在 t=7000 才到期（6 秒＋休息 1 秒）。
  - `enqueue_CompressesLongBatchIntoSummary`：一批含 3 場（encounter 在 0、20、40，各有 reward 5/2，最後事件 45 秒）→ `takeDue(receivedAt)` 第一項是 `{ kind: "summary", fights: 2, experience: 10, spiritStones: 4 }`，接著是最後一場的 encounter（平移後 atSeconds 0）。
  - `displayedCharacter_ExcludesUnplayedRewards`：回應角色修為 13，批次中有 reward 8（尚未到期）→ 顯示 5；`takeDue` 播出後 → 顯示 13。
  - `enqueue_EmptyEvents_UpdatesCharacterOnly`。
- [ ] **Step 2:** `pnpm test`，預期失敗。
- [ ] **Step 3:** 實作。
- [ ] **Step 4:** `pnpm check`，預期通過。
- [ ] **Step 5: Commit** `✨ feat: schedule battle events on a playback timeline`

---

### Task 8: 戰報

**Files:**
- Create: `frontend/src/game/battleLog.ts`, `frontend/src/ui/BattleLog.tsx`
- Test: `frontend/src/game/battleLog.test.ts`, `frontend/src/ui/BattleLog.test.tsx`

**Interfaces:**
- Produces:

```ts
export type LogLineKind = "normal" | "muted" | "drop" | "trigger"
export type LogLine = { id: number; text: string; kind: LogLineKind }
export type MonsterNameResolver = (monsterId: string) => string   // 找不到名稱時回傳 id
export const createMonsterNameResolver: (content: ContentNames) => MonsterNameResolver   // ContentNames 是 Task 5 的 API 回應型別
export const describePlaybackItem: (item: PlaybackItem, monsterName: MonsterNameResolver, currentMonsterId: string | null) => Omit<LogLine, "id"> | null
export const MaxLogLines = 200
export const BattleLog: (props: { lines: LogLine[] }) => JSX.Element
```

固定文案（`{}` 內為變數；數字用 `toLocaleString("zh-TW")`）：

| 項目 | 文字 | kind |
|---|---|---|
| encounter | `{妖怪}現身。` | normal |
| 玩家攻擊 | `你運劍出招，{妖怪}受到 {傷害} 點傷害。` | normal |
| 玩家會心 | `你一劍穿心，會心 {傷害}！` | trigger |
| 妖怪攻擊 | `{妖怪}反擊，你受到 {傷害} 點傷害。` | muted |
| 妖怪會心 | `{妖怪}猛然重擊，你受到 {傷害} 點傷害！` | muted |
| 妖怪被擊敗 | `{妖怪}化為一縷輕煙。` | normal |
| 玩家被擊敗 | `你力竭倒下，調息片刻再戰。` | muted |
| reward | `獲得修為 {修為}、靈石 {靈石}。` | drop |
| summary | `離開期間歷練 {場數} 場，獲得修為 {修為}、靈石 {靈石}。` | drop |

`BattleLog` 元件：
- 上方分頁「全部／掉落／觸發」（`role="tab"`），「掉落」只顯示 `drop`、「觸發」只顯示 `trigger`；
- 最新一行在最下方，新行加入時自動捲到底；上緣用 CSS mask 淡出（同樣稿）；
- `muted` 降低不透明度，`drop`、`trigger` 的數字用朱紅或金色。

- [ ] **Step 1: Write the failing tests**
  - `describePlaybackItem` 針對上表每一列各一個案例（用 `test.each`），斷言文字與 kind。
  - `describePlaybackItem_FallsBackToMonsterId_WhenNameUnknown`。
  - `BattleLog_FiltersByTab`：三行（normal、drop、trigger），點「掉落」只剩 drop 那行。
- [ ] **Step 2:** 執行，預期失敗。
- [ ] **Step 3:** 實作。
- [ ] **Step 4:** `pnpm check`，預期通過。
- [ ] **Step 5: Commit** `✨ feat: narrate battle events in the log`

---

### Task 9: 對戰舞台

**Files:**
- Create: `frontend/src/game/stageModel.ts`, `frontend/src/render/palette.ts`, `frontend/src/render/artIndex.ts`, `frontend/src/render/StageRenderer.ts`, `frontend/src/render/PixiStage.tsx`
- Test: `frontend/src/game/stageModel.test.ts`, `frontend/src/render/artIndex.test.ts`

**Interfaces:**
- Produces:

```ts
// game/stageModel.ts
export type StageState = { monsterId: string | null; monsterHealth: number; monsterMaxHealth: number; playerHealth: number; playerMaxHealth: number }
export type StageEffect =
  | { type: "slash" }
  | { type: "damageNumber"; side: "player" | "monster"; value: number; critical: boolean }
  | { type: "seal"; text: string }
  | { type: "hitShake"; side: "player" | "monster" }
  | { type: "inkSplash" }
export const initialStageState: StageState
export const applyToStage: (state: StageState, event: BattleEvent) => StageState
export const stageEffectsFor: (event: BattleEvent) => StageEffect[]

// render/artIndex.ts
export const createArtResolver: (globResult: Record<string, string>) => (contentId: string) => string | null
export const resolveArt: (contentId: string) => string | null   // 以 import.meta.glob("../assets/art/*.webp", { eager: true, query: "?url", import: "default" }) 建立

// render/StageRenderer.ts
export class StageRenderer {
  static create(container: HTMLElement): Promise<StageRenderer>
  showEncounter(monsterId: string, monsterName: string, state: StageState): void
  updateHealth(state: StageState): void
  play(effects: StageEffect[]): void
  destroy(): void
}
// render/PixiStage.tsx —— 掛載與卸載 StageRenderer，透過 ref 暴露給 AdventureScreen
```

特效對應：
- 玩家攻擊 → `slash`、`damageNumber(monster)`、`hitShake(monster)`；會心時再加 `seal("會心")`。
- 妖怪攻擊 → `damageNumber(player)`、`hitShake(player)`。
- 妖怪被擊敗 → `inkSplash`。
- encounter、reward、玩家被擊敗 → 無特效（玩家被擊敗只在戰報呈現）。

舞台繪製（對照 `battle-v2.html`）：
- 高度 220px、寬度隨容器；遠山水墨剪影（Pixi `Graphics`，墨色 18% 不透明度）。
- 左上、右上各一條血條與名字（DOM 疊層也可以，以實作簡單為準，但數值來自 `StageState`）。
- 玩家：有 `resolveArt("player")` 就用圖，否則用 `Graphics` 畫的墨色剪影（頭、身、劍）。
- 妖怪：有圖就用圖；否則顯示**直書**的書法名字（`Text`，霞鶩文楷 700、約 52px、`resolution: devicePixelRatio`）。
- 呼吸晃動：立繪或名字用 GSAP 做 `scaleY` 1↔1.015、3 秒循環。
- 特效時長取自 `motion/timings.ts`（新增 `HitShakeSeconds = 0.25`、`SlashSeconds = 0.3`、`DamageNumberSeconds = 0.9`、`SealSeconds = 1.2`、`InkSplashSeconds = 0.8`）。

- [ ] **Step 1: Write the failing tests**
  - `applyToStage_EncounterResetsBothHealthBars`。
  - `applyToStage_AttackUpdatesTargetHealth`（玩家攻擊改妖怪血量；妖怪攻擊改玩家血量）。
  - `stageEffectsFor_PlayerCritical_IncludesSeal`、`stageEffectsFor_MonsterAttack_ShakesPlayer`、`stageEffectsFor_MonsterDefeat_SplashesInk`、`stageEffectsFor_Reward_HasNoEffects`。
  - `artResolver_ReturnsUrl_ForKnownId_AndNull_Otherwise`：`createArtResolver({ "../assets/art/snow-sprite.webp": "/x.webp" })("snow-sprite") === "/x.webp"`，`("frost-wolf") === null`。
- [ ] **Step 2:** 執行，預期失敗。
- [ ] **Step 3:** 實作 `stageModel`、`artIndex`，再實作 `StageRenderer` 與 `PixiStage`（Pixi 繪製本身不寫單元測試，Task 10 以實際畫面驗收）。
- [ ] **Step 4:** `pnpm check`，預期通過。
- [ ] **Step 5: Commit** `✨ feat: render the ink-wash battle stage with fallback calligraphy`

---

### Task 10: 歷練主畫面與輪詢迴圈

**Files:**
- Create: `frontend/src/ui/AdventureScreen.tsx`, `frontend/src/ui/useAdventureLoop.ts`
- Modify: `frontend/src/ui/App.tsx`（佔位換成 `AdventureScreen`）
- Test: `frontend/src/ui/useAdventureLoop.test.tsx`, `frontend/src/ui/AdventureScreen.test.tsx`

**Interfaces:**
- Consumes: 前面所有任務
- Produces:

```ts
export const useAdventureLoop: (options: {
  api: ApiClient
  playback: BattlePlayback
  onItems: (items: PlaybackItem[]) => void
  now?: () => number                    // 預設 Date.now，測試注入
}) => { error: string | null }
export const AdventureScreen: (props: { api: ApiClient; initialCharacter: CharacterView }) => JSX.Element
```

輪詢迴圈：
- 掛載時立刻呼叫一次 `advance()`，之後每 `PollIntervalMilliseconds` 呼叫一次；分頁重新變為可見時（`visibilitychange`）也立刻呼叫一次。
- 每次回應 `playback.enqueue(response, now())`。
- 用 `requestAnimationFrame` 迴圈呼叫 `playback.takeDue(now())`，有項目就交給 `onItems`。
- `ApiError` 409 → 忽略，等下一輪；其他錯誤 → `error` 顯示「連線不穩，稍後自動重試」，下一輪成功時清除；401 由 client 處理。
- 卸載時清掉 interval、animation frame 與事件監聽。

`AdventureScreen` 版面（由上而下，對照 `battle-v2.html`）：
- 頂欄：角色名、修為（`displayedCharacter.experience`）、靈石；
- 關卡名（`GET /api/content` 取得，置中、字距加寬）；
- `PixiStage`（220px）；
- `BattleLog`（填滿剩餘高度）；
- 底部導覽：歷練（選中）、本命、經脈、行囊（後三者 `disabled`，`title="尚未開放"`）。
- 每個 `PlaybackItem`：先用 `applyToStage`、`stageEffectsFor` 更新舞台，再用 `describePlaybackItem` 加一行戰報（保留最近 `MaxLogLines` 行），頂欄數字從 `playback.displayedCharacter` 讀。

- [ ] **Step 1: Write the failing tests**（`vi.useFakeTimers()`，假 client）
  - `adventureLoop_AdvancesOnMountAndEveryInterval`：掛載後呼叫 1 次；前進 10 秒 → 2 次；前進 20 秒 → 4 次。
  - `adventureLoop_IgnoresConflictAndKeepsPolling`：第一次 `advance` 丟 `ApiError(409, "...")`，`error` 仍為 null，前進 10 秒後再呼叫一次。
  - `adventureLoop_ShowsRetryMessage_OnNetworkError_AndClearsOnSuccess`。
  - `adventureLoop_StopsOnUnmount`：卸載後前進 30 秒，呼叫次數不變。
  - `AdventureScreen_ShowsLogLineAndUpdatedTotals`：假 `advance` 回傳 encounter（atSeconds 0）＋ reward（atSeconds 0、修為 8），戰報出現「測試寒狼現身。」與「獲得修為 8、靈石 3。」，頂欄修為顯示 8。測試中 `PixiStage` 以 `vi.mock` 換成空元件（jsdom 沒有 WebGL）。
- [ ] **Step 2:** 執行，預期失敗。
- [ ] **Step 3:** 實作。
- [ ] **Step 4:** `pnpm check` 與 `./scripts/check.sh`，預期全部通過。
- [ ] **Step 5: 手動驗收**（需要畫面，用瀏覽器實際操作）
  1. `docker compose up -d`；`cd backend && dotnet run --project Inkbound.Api`；`cd frontend && pnpm dev`。
  2. 用瀏覽器開發者工具切到 375×667，註冊、登入、建角色，看至少 30 秒：
     - 戰報逐行出現、舞台血條與特效同步、頂欄數字隨獎勵增加；
     - 妖怪沒有圖，顯示直書書法名字；
     - 和 `battle-v2.html` 的「還沒有圖時」並排比較，配色、字型、版面比例一致；
     - 360 與 430 寬度都沒有水平捲動。
  3. 關掉分頁 2 分鐘後再打開：先出現「離開期間」摘要，只播最後一場。
  4. 截圖存到 `docs/superpowers/screenshots/plan-2-adventure.png` 並 commit，供使用者檢視。
- [ ] **Step 6: Commit** `✨ feat: assemble the adventure screen with live playback`
