## Context

- 上一個變更 `game-api-dto-vo`（2026-11-12 封存）已完成：角色、冒險推進、技能、轉職、世界地圖的 API 與 Domain 用完整名稱；前端這五類經 Converter 轉成 Vo；共用的能力型別（`StatBonusResponse`、`EquipmentRequirementResponse`）在所有遊戲回應裡都已是完整名稱；夾帶在其他回應裡的 `character` 已由 `client.ts` 的 `requestWithCharacter` 轉成 `CharacterVo`。主規格 `game-api-contract` 記錄這些規則，但完整名稱與前端轉換的範圍只寫到那五類資源。
- 遊戲 OpenAPI（`backend/openapi/EndlessBattle.Api.json`）還剩的縮寫：`InventoryItemResponse` 的 `supplyHp`、`supplySp`，`MissionRewardResponse` 與 `MissionCompleteResponse` 的 `exp`，`MercenaryCatalogueResponse` 的 `expPercent`；`BuffKind` 列舉值 `Exp`、`JobExp`（`BuffResponse.kind`、`InnRoomResponse.kind` 直接序列化 Domain 的 `BuffKind`）。`atSeconds`、`endsAt`、`expiresAt`、`MercenaryFightResponse.at` 的 at／exp 是時間，不是縮寫。
- Domain 還有的縮寫：`ItemDefinition` 的 `SupplyHp`／`SupplySp`、`RefineStoneSpec` 的 `At`…`Be`、`MissionDefinition.Exp`／`ExpPerLevel`、`MissionObjective.Exp`、`MercenaryDefinition.ExpPercent`、`MercenaryFight.Exp`、`MercenaryRun.JobExp`、`BuffKind.Exp`／`JobExp`、`ConsumableUse` 的 `GainExp`、`PetBattle` 的區域變數；Application 的 `MissionCompletion.Exp`、傭兵戰報（`AdvanceResult.cs`）的 `Exp`、`AdventureService` 的 `mercenaryJobExp` 等。
- 儲存格式：內容檔由 Infrastructure 的 entry 型別讀（`ItemEntry`、`RefineEntry`、`ElixirEntry`、`MercenaryEntry`、`InnRoomEntry`），鍵與 Domain 已脫鉤；但 `BuffKind` 例外——內容的 `"kind": "Exp"`（`items.json` 的密藥、`inn.json` 的房間）用 `Enum.TryParse` 直接解析，資料庫 `character_buffs.kind`（`VARCHAR(16)`）存 `BuffKind.ToString()`、讀回用 `Enum.Parse`（`BuffRepository`）。Domain 一改名，這兩處都會讀不回來。
- 前端 `frontend/src/game/types.ts` 有 38 個產生型別的別名，由 8 個 `src/game` 模組、14 個畫面檔（含測試）、`client.ts` 與 `src/test/fixtures.ts` 使用。時間欄位目前是 ISO 字串，`buffs.ts`、`inn.ts`、`pets.ts`、`PetsPage.tsx`、`missions.ts` 各自 `Date.parse`／`new Date(...)`。
- 背包缺陷：`bagWidgets.tsx` 的 `Requirements` 對未達成的需求輸出 `className="bag__requirement crimson-text"`。`.crimson-text` 定義在 `theme.css`，`.bag__requirement { color: var(--green) }` 定義在 `BagScreen.css`；兩者權重相同，`BagScreen.css` 較晚載入，所以顯示綠色。`BagScreen.test.tsx` 只斷言有 `crimson-text` class，jsdom 不套用樣式表，測試抓不到。

## Goals / Non-Goals

**Goals:**

- 所有遊戲 API（`/api/...`）的欄位名稱與列舉值都用完整名稱。
- 道具、精鍊、任務、傭兵、寵物、密藥、道具使用的 Domain 與 Application 縮寫改成完整名稱，行為不變。
- 內容檔與資料庫的儲存格式不變，不需要 migration。
- 前端所有遊戲 API 回應都經 Converter 轉成 Vo；`frontend/src/game/types.ts` 刪除；`src/game`、`src/ui`、`src/render` 不再 import `src/api/schema`，由 eslint 擋住。
- 背包未達成的裝備需求顯示緋紅色。

**Non-Goals:**

- admin API 與 admin 前端：平衡報告的 `expPerHour`、admin 內容記錄的 `ContentElixirKind.Exp` 與 `supplyHp` 對照屬於內容檔的儲存格式與 admin OpenAPI，不在本變更。
- 內容 JSON、資料表欄位改名；畫面上的「SP」標籤改成「MP」。
- 只帶純量參數的請求（買賣、存取倉庫、鐵舖操作、任務接取等）不另寫 `toDto`：它們沒有對應的 `src/game` 命令型別，`client.ts` 直接組成請求本體。
- 遊戲畫面的版面或文案調整（除了背包需求的顏色）。

## Decisions

### 完整名稱對照（本變更新增）

沿用 `game-api-dto-vo` 的「完整名稱對照」表，另加：

| 縮寫 | Domain（C#） | API／前端（JSON） |
| --- | --- | --- |
| SupplyHp | SupplyHealthPoints | supplyHealthPoints |
| SupplySp | SupplyManaPoints | supplyManaPoints |
| ExpPercent | ExperiencePercent | experiencePercent |
| ExpPerLevel | ExperiencePerLevel | — |
| GainExp | GainExperience | — |
| BuffKind.Exp | BuffKind.Experience | `"Experience"` |
| BuffKind.JobExp | BuffKind.JobExperience | `"JobExperience"` |

`RefineStoneSpec` 的 `At`、`Mat`、`Df`、`Mdf`、`Str`、`Smart`、`Life`、`Vit`、`Agi`、`Au`、`Be` 依原表改成 `Attack`…`Faith`。任務與傭兵的 `Exp`／`JobExp` 改成 `Experience`／`JobExperience`。`MercenaryFight.At`、`atSeconds` 等時間欄位不改。

替代方案：`BuffKind` 的 API 值維持 `Exp`——前端仍得認得縮寫，與「API 只用完整名稱」衝突，不採用。

### 密藥種類的儲存代碼由 Infrastructure 對照

Infrastructure 新增一個密藥種類代碼對照（`Exp`↔`Experience`、`JobExp`↔`JobExperience`，其餘四種名稱相同），內容載入（密藥 `elixir.kind`、旅館房間 `kind`）與 `BuffRepository` 的讀寫都經過它。資料庫寫入仍用舊代碼（`Exp`、`JobExp`），讓新舊版本讀到同一筆資料；內容檔仍只接受舊代碼（大小寫不拘，與現在相同），不認識的代碼照舊在載入時失敗。

替代方案：新增 migration 把 `character_buffs.kind` 改成新名稱——要動資料表、回滾時舊版讀不回來，而且內容檔還是要對照，不採用。

### 前端 Converter 與 Vo 的位置

- Converter 依資源放在 `frontend/src/api/`：
  - `inventoryData.ts`：`InventoryConverter`（`InventoryResponse`，也供其他回應裡的 `inventory` 重用）、`ShopItemConverter`、`DepotConverter`、`DepotMoveConverter`、`ItemUseConverter`。
  - `forgeData.ts`：`ForgeResultConverter`、`UnsocketConverter`、`RecipeConverter`、`SynthesisConverter`。
  - `missionData.ts`：`MissionBoardConverter`、`MissionCompletionConverter`。
  - `companionData.ts`：`PetsConverter`、`PetFeedConverter`、`MercenaryOverviewConverter`、`MercenaryHireConverter`、`HonourShopConverter`。
  - `lodgingData.ts`：`BuffConverter`、`InnConverter`、`InnBookConverter`。
- Vo 放在 `src/game` 既有的資源模組（`inventory.ts`、`forge.ts`、`missions.ts`、`pets.ts`、`mercenaries.ts`、`honour.ts`、`inn.ts`、`buffs.ts`），比照 `world.ts`：每支 API 的回應本體是 class（private constructor＋`static create`），清單端點回傳 class 的陣列；巢狀資料用 `readonly` 物件型別；列舉值用 `as const` 陣列加 `parseOneOf` 檢查。
- 時間欄位一律轉成 `Date`（`parseTimestamp`／可為 null 時自行判斷）：密藥與住宿的 `endsAt`、任務的 `expiresAt` 與 `nextDailyReset`、寵物的 `nextFeedAt`、傭兵的 `nextFightAt`。`src/game` 的時間計算改吃 `Date`。
- 夾帶 `character` 的回應（旅館入住、任務完成、寵物餵食、傭兵僱用）由各自的 Converter 呼叫 `CharacterConverter.toVo`，`client.ts` 的 `requestWithCharacter` 與 `WithCharacter` 移除。
- 解析失敗的路徑從資源名稱開始：`inventory`、`shop`、`depot`、`depotMove`、`itemUse`、`forge`、`unsocket`、`recipes`、`synthesis`、`missions`、`missionCompletion`、`pets`、`petFeed`、`mercenaries`、`mercenaryHire`、`honour`、`buffs`、`inn`、`innBooking`；重用 `InventoryConverter` 時路徑接在外層之後（例如 `forge.inventory.bag[0].capacity`）。

### 用 eslint 擋住產生型別

`frontend/eslint.config.js` 對 `src/game/**`、`src/ui/**`、`src/render/**` 加 `no-restricted-imports`，禁止 import 任何解析到 `src/api/schema` 的路徑。`src/api` 與 `src/test` 不受限。這樣下一次有人在畫面直接用產生型別，`pnpm check` 會失敗。

替代方案：寫一條掃描 import 的 Vitest——做的是同一件事但多一份維護，lint 已經在 `pnpm check` 裡。

### 背包需求用自己的修飾 class

`Requirements` 對未達成的需求改用 `bag__requirement bag__requirement--unmet`，`BagScreen.css` 在 `.bag__requirement` 之後加 `.bag__requirement--unmet { color: var(--crimson-text); }`。不再在背包用全域的 `crimson-text`，避免再被元件樣式的載入順序蓋掉。依專案慣例，純樣式不寫自動測試，以畫面確認；既有測試的 class 斷言改成新 class，仍保證「未達成」這個判斷有傳到畫面。

替代方案：提高 `.crimson-text` 的權重（`!important` 或 `:where`）——全域工具 class 和元件樣式互搶，其他畫面也可能被影響。

### 硬規則

- 內容檔：不改任何 `backend/content/*.json`（硬規則 6），新舊名稱由 Infrastructure 對照。
- 資料表：不新增 migration、不動 `character_buffs`（硬規則 3），Domain 改名不影響資料庫值。
- scaffold 產生檔：不涉及（`BuffRepository` 是手寫的 repository，`Persistence/Models` 不動，硬規則 4）。
- `schema.ts`：只用 `pnpm generate:api` 重新產生（遊戲前端規則 9）；admin 的 `schema.ts` 不受影響。
- admin 路由：不涉及。

## Implementation Contract

**Behavior:**

- 遊戲 OpenAPI 文件裡不再有 `supplyHp`、`supplySp`、`expPercent` 與任務的 `exp` 欄位，改成 `supplyHealthPoints`、`supplyManaPoints`、`experiencePercent`、`experience`；`BuffKind` 的值是 `Experience`、`JobExperience`、`Attack`、`Magic`、`Defense`、`MagicDefense`。
- 已存在資料庫、`kind` 為 `Exp` 的密藥，`GET /api/characters/me/buffs` 回 `kind: "Experience"`；喝下經驗密藥後資料庫存的 `kind` 仍是 `Exp`。
- 內容檔不改，啟動時道具補給量、精鍊石範圍、密藥與旅館房間的種類、傭兵的經驗比例都與改名前相同。
- 前端：上述所有遊戲 API 的回應都先轉成 Vo；缺必要欄位或型別錯誤時請求失敗並丟出帶路徑的 `ParseError`，畫面照一般請求失敗處理；不認識的欄位忽略。各畫面的顯示與操作和改名前相同。
- 背包裡未達成的裝備需求顯示緋紅色（`--crimson-text`），已達成的仍是綠色。

**Interface / data shape:**

- 前端 `ApiClient`：`getInventory`、`equip`、`unequip`、`buy`、`sell` 回傳 `InventoryVo`；`useItem` 回傳 `ItemUseVo`；`getShop` 回傳 `ShopItemVo[]`；`getDepot` 回傳 `DepotVo`；`store`、`take` 回傳 `DepotMoveVo`；`refine`、`socket`、`drill`、`mergeStones`、`mergeCrossStones` 回傳 `ForgeResultVo`；`unsocket` 回傳 `UnsocketVo`；`getRecipes` 回傳 `RecipeVo[]`；`synthesize` 回傳 `SynthesisVo`；`getMissions`、`acceptMission`、`abandonMission` 回傳 `MissionBoardVo`；`completeMission` 回傳 `MissionCompletionVo`；`getPets`、`carryPet`、`storePet`、`releasePet`、`renamePet` 回傳 `PetsVo`；`feedPet` 回傳 `PetFeedVo`；`getMercenaries`、`deployMercenary`、`restMercenary`、`renameMercenary`、`dismissMercenary` 回傳 `MercenaryOverviewVo`；`hireMercenary` 回傳 `MercenaryHireVo`；`getHonourShop`、`exchangeHonour` 回傳 `HonourShopVo`；`getBuffs` 回傳 `BuffVo[]`；`getInn` 回傳 `InnVo`；`bookInn` 回傳 `InnBookVo`。
- 請求本體與端點路徑不變。
- 後端：`BuffKind` 列舉成員改名；Infrastructure 提供密藥種類代碼的雙向對照（舊代碼→列舉、列舉→舊代碼），未知代碼在內容載入時丟 `GameContentException`、在資料庫讀取時丟例外（資料損毀，不靜默略過）。

**Failure modes:**

- 回應缺欄位：`ParseError`，訊息含欄位路徑（例如 `inventory.bag[0].items[1].supplyHealthPoints`）。
- 內容檔出現不認識的密藥種類：啟動失敗，訊息指出道具或房間 id。
- 資料庫 `character_buffs.kind` 出現不認識的值：讀取密藥時丟例外。

**Acceptance criteria:**

- 後端：每個 API 改名都有 API 測試斷言新欄位存在、舊欄位不存在；`BuffRepository` 有讀舊代碼與寫入舊代碼的資料庫測試；內容載入測試確認經驗密藥、修練廂房的種類解析正確；Domain 改名的 commit 前後全部後端測試綠、OpenAPI 文件不變。
- 前端：每個 Converter 至少三條測試——完整回應轉換、缺欄位錯誤帶路徑、多餘欄位被忽略；有時間欄位的 Converter 測試它轉成 `Date`。
- `frontend/src/game/types.ts` 不存在；`pnpm check` 通過，且在 `src/ui` 任一檔案加入 `import type { components } from "../api/schema"` 時 eslint 報錯（手動確認一次）。
- `./scripts/check.sh` exit code 0。
- 手動驗證（瀏覽器）：背包（含使用道具、買賣、倉庫存取）、鐵舖（精鍊、鑲嵌、合成）、任務（接取、完成）、寵物（餵食）、傭兵（僱用、出擊）、勳章兌換、旅館入住、角色頁密藥都正常顯示與操作；力量不足的裝備需求顯示緋紅色。

**Scope boundaries:**

- In scope：上列遊戲 API、Domain／Application 的對應改名、Infrastructure 的密藥代碼對照、前端 Converter／Vo、刪除 `types.ts`、eslint 規則、背包需求顏色。
- Out of scope：admin、內容檔與資料表、畫面版面與文案、「SP」標籤。

## Risks / Trade-offs

- [前端改動面大（約 40 個 client 方法、8 個 `src/game` 模組、14 個畫面檔）] → 依資源分批，每批一個任務、各自跑 `pnpm check`；先加 Converter 與 Vo、再切換 `client.ts` 與畫面，最後才刪 `types.ts`，每一步都能編譯與通過測試。
- [時間欄位從字串改成 `Date`，比較與顯示可能寫錯] → `src/game` 的時間計算有既有測試，改型別時測試一併改成 `Date`；手動驗證密藥剩餘分鐘、寵物餵食倒數、傭兵下一場、任務重置時間。
- [`BuffKind` 改名漏了某條讀寫路徑，舊資料讀不回來] → 對照只放一處，`BuffRepository` 與內容載入都用它；資料庫測試直接寫入 `Exp` 字串再讀取。
- [eslint 規則把 `src/api` 自己的 import 也擋下] → 規則只套在 `src/game/**`、`src/ui/**`、`src/render/**`。

## Migration Plan

不需要資料搬移。部署後舊版寫入的 `Exp`／`JobExp` 照常讀取，新版也寫入同樣的代碼；回滾到前一版時資料庫內容相容。前端與後端同一次部署（API 欄位改名是破壞性變更，舊前端讀不到新欄位）。

## Open Questions

無。
