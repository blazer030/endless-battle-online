## Context

- 錯誤回應由 `ApplicationExceptionHandler`（backend/EndlessBattle.Api/Errors）把 Application 層例外轉成 ProblemDetails：`InputValidationException` 400、`AuthenticationFailedException` 401、`NotFoundException` 404、`ConflictException` 409、`BadHttpRequestException`（請求格式錯）用其狀態碼。`title` 是中文（「輸入不正確」等），`detail` 是例外訊息（中文句子），`reasons` 只在例外帶代碼時才有。
- 例外定義在 backend/EndlessBattle.Application/Common/ApplicationExceptions.cs：`InputValidationException` 與 `ConflictException` 的 reasons 可省略，`NotFoundException` 沒有代碼欄位，`AuthenticationFailedException` 固定訊息「帳號或密碼錯誤」。Domain 的 `CharacterCreationException` 只有訊息，`CharacterService` 接住後轉成沒有代碼的 `InputValidationException`。
- 盤點（2026-11-18 以 grep 實測）：Domain、Application、Infrastructure、Api 共 127 個 `throw new` 這些例外；沒有代碼的約 80 個——Api endpoint 的「缺少 X id／數量／欄位」與「請指定地區／職業」「設定資料不完整」「創角資料不完整」「技能欄位資料不完整」約 45 個、各 Service 的「尚未建立角色」16 個、帳號 4 個、建角 7 個（含 `CharacterCreationException` 4 個）、衝突 6 個、其他找不到 4 個。
- 原因代碼目前是小寫開頭的駝峰式字串，散寫在 Domain、Application、Infrastructure（不是集中常數），約 81 個（含 `statTooLow:<stat>`、`missingTool:<itemId>` 兩種帶參數的）；同一批代碼也出現在正常回應：`GET /api/jobs` 的每個職業 `reasons`、技能總覽下一級的 `reasons`。
- 前端：`frontend/src/api/client.ts` 的 `ApiError { status, detail, reasons }`，`readProblem` 讀 `detail`（缺時用 `fallbackDetail`「發生錯誤，請稍後再試」）與 `reasons`。直接顯示 `detail` 的有 `TitleScreen`（登入、註冊）、`CreateScreen`（400）；舊畫面在沒有代碼時退回 `detail`：`BagScreen`（`inventory.ts` 的 `errorMessage`）、`SkillsScreen`（`describeError` 是「detail：原因」）、`SettingsPanel`（`settingsErrorMessage`）、`WorldPanel`。新內頁（旅館、同伴、勳章所、任務板）只看 `reasons`。各畫面另有自己的代碼文字表（`inventory.ts` 的 `fixedReasonTexts`、`settings.ts`、`skills.ts`、`missions.ts`、`honour.ts`、`inn.ts`、`companions.ts` 等）。
- 不涉及：內容檔（backend/content 不含原因代碼；world.json 的 `"id": "restricted"` 是地區限制種類的內容 id，不是原因代碼，不改）、資料表與 migration（原因代碼不存資料庫）、scaffold 產生檔、admin 路由與 admin 前端。`frontend/src/api/schema.ts` 由 `pnpm generate:api` 重新產生，只有 OpenAPI 文件有變時才會變。

## Goals / Non-Goals

**Goals:**

- 遊戲 API 的每個錯誤回應只帶 HTTP 狀態與至少一個原因代碼，沒有給玩家看的句子。
- 原因代碼一律全大寫底線，錯誤與正常回應用同一套。
- 前端依代碼決定文字；這次照搬原句，玩家看到的字不變（下面〈前端文字照搬原句〉列出的合併例外除外）。

**Non-Goals:**

- admin 的錯誤格式（`ContentFieldError`）與 admin 前端。
- 改寫錯誤文字或改成角色口吻。
- 改 HTTP 狀態碼、拒絕條件或檢查順序。

## Decisions

### 錯誤回應只留 reasons

ProblemDetails 保留 `status` 與 `reasons`（陣列，至少一個代碼）；不再設定 `detail`，也不再設定中文 `title`（由 ASP.NET 的預設值補上英文的 HTTP 標準短語，例如 `Bad Request`，前端不用它）。例外訊息保留在例外物件上，只供伺服器紀錄與測試。

替代方案：改成單一 `code` 欄位（`{ code, reasons? }`）。沒選：前端各頁已經都讀 `reasons`，一個請求同時有多個原因（接取委託的 `TOO_MANY` 與 `LEVEL_TOO_LOW`）也需要清單；保留 `reasons` 改動最小。

### 原因代碼全大寫底線

代碼寫法改成全大寫、字之間用底線，由現有駝峰式機械轉換：每個大寫字母前加底線再全轉大寫（`bagFull` → `BAG_FULL`、`subPlaceOutOfRange` → `SUB_PLACE_OUT_OF_RANGE`、`full` → `FULL`）。帶參數的代碼只轉冒號前的部分，參數照舊：`STAT_TOO_LOW:strength`、`MISSING_TOOL:item-1378`。正常回應裡的 `reasons`（職業清單、技能下一級）用同一套。

取代 title-and-create-screens 設計〈409 原因與註冊補強〉沿用的駝峰式代碼（`accountHasCharacter`、`characterNameTaken`）；使用者 2026-11-18 決定全部改大寫底線。

替代方案：維持駝峰式、或只有新代碼用大寫。沒選：使用者要全部一致。

### 請求不完整一律 REQUEST_INVALID

endpoint 檢查必填欄位缺漏（缺少 id、數量、欄位、組數、孔位，「請指定地區／職業」，「設定資料不完整」「創角資料不完整」「技能欄位資料不完整」）與 `BadHttpRequestException`（內容不是正確的 JSON、型別不符）都回 400 `REQUEST_INVALID`，不分欄位。正常操作不會遇到；缺哪個欄位只留在例外訊息。Minimal API 預設只在 Development 環境把讀不懂的內容丟成 `BadHttpRequestException`，其他環境直接回沒有內容的 400；所以 `Program.cs` 設定 `RouteHandlerOptions.ThrowOnBadRequest = true`（所有環境），讓它一律經過 `ApplicationExceptionHandler`。`BadHttpRequestException` 沿用它自帶的狀態碼（例如 413），reasons 一律 `REQUEST_INVALID`。

替代方案：每個欄位一個代碼（約 30 個）。沒選：使用者決定統一。

### 新代碼

| 情況 | HTTP | 代碼 | 前端文字（照搬原句） |
| --- | --- | --- | --- |
| 帳號格式不對 | 400 | `USERNAME_INVALID` | 帳號必須是 3～32 個英數字或底線 |
| 密碼太短 | 400 | `PASSWORD_TOO_SHORT` | 密碼至少要 8 個字元 |
| 帳號已被使用 | 409 | `USERNAME_TAKEN` | 帳號已被使用 |
| 帳號或密碼錯誤 | 401 | `AUTHENTICATION_FAILED` | 帳號或密碼錯誤 |
| 權杖的帳號不存在 | 404 | `ACCOUNT_NOT_FOUND` | 找不到帳號 |
| 角色名稱長度 | 400 | `CHARACTER_NAME_LENGTH` | 角色名稱必須是 2～12 個字 |
| 角色名稱組合字元過多 | 400 | `CHARACTER_NAME_TOO_COMPLEX` | 角色名稱含有過多組合字元，請改用較簡單的字 |
| 不存在的職業（建角、轉職） | 400 | `JOB_UNKNOWN` | 找不到職業 |
| 建角選了非初階職業 | 400 | `JOB_NOT_FIRST_TIER` | 只能選擇初階職業 |
| 有能力低於 1 | 400 | `STAT_BELOW_ONE` | 每項能力至少要分配 1 點 |
| 點數沒分完 | 400 | `POINTS_NOT_SPENT` | 能力點數必須剛好分完 10 點 |
| 性別／屬性／出生地選項無效 | 400 | `SEX_INVALID`／`ELEMENT_INVALID`／`BIRTH_INVALID` | 性別選項無效／屬性選項無效／出生地選項無效 |
| 還沒有角色 | 404 | `CHARACTER_NOT_FOUND` | 尚未建立角色 |
| 不存在的技能 | 400 | `SKILL_UNKNOWN` | 不存在的技能 |
| 找不到配方 | 404 | `RECIPE_UNKNOWN` | 找不到配方 |
| 鍛造找不到道具 | 404 | `ITEM_NOT_FOUND`（沿用） | 找不到這件道具 |
| 同時更新衝突 | 409 | `CONCURRENT_UPDATE` | 資料正被其他請求使用，請重試 |
| 任務記錄重複 | 409 | `MISSION_CONFLICT`（沿用） | 沿用各畫面現有文字 |
| 傭兵合約重複 | 409 | `MERCENARY_HIRED`（沿用） | 沿用各畫面現有文字 |
| 請求不完整或格式錯 | 400 | `REQUEST_INVALID` | 發生錯誤，請稍後再試 |

`CONCURRENT_UPDATE` 涵蓋 `EfUnitOfWork` 的死鎖重試失敗、`CharacterRepository` 的樂觀鎖衝突、`WorldService` 的鑰匙已被用掉、`JobProficiencyRepository` 的熟練度已變動。

### 例外必須帶代碼

`InputValidationException(string message, IReadOnlyList<string> reasons)`、`ConflictException(string message, IReadOnlyList<string> reasons, Exception? innerException = null)` 的 reasons 改成必填、不得為空（空的丟 `ArgumentException`）；`NotFoundException(string message, string reason)` 加代碼並對外成為 reasons 的唯一一項；`AuthenticationFailedException` 固定代碼 `AUTHENTICATION_FAILED`；Domain 的 `CharacterCreationException(string message, string reason)` 帶代碼，`CharacterService` 轉成 `InputValidationException` 時沿用。這樣漏帶代碼在編譯期就擋下，不會有沒代碼的錯誤回應。代碼仍寫成字串（不集中成常數），跟現況一致。

### 前端共用代碼文字表

新增 `frontend/src/game/errorText.ts`：`errorText(code: string): string`，涵蓋上表的新代碼，認不得的代碼回「發生錯誤，請稍後再試」（原本的 `fallbackDetail`），以及 `errorTextOf(reasons: readonly string[]): string` 取第一個代碼的文字（沒有代碼時同上）。各畫面原有的代碼文字表不動、只改代碼寫法；舊畫面原本「沒代碼時用 `detail`」的地方，改成「這頁的表沒有這個代碼時用 `errorText`」。

- `TitleScreen`：登入、註冊失敗顯示 `errorTextOf(reasons)`；不是 `ApiError`（連線錯）維持現在的連線錯誤文字。
- `CreateScreen`：400 顯示 `errorTextOf(reasons)`；409 的 `ACCOUNT_HAS_CHARACTER`、`CHARACTER_NAME_TAKEN` 照現在的流程。
- `SkillsScreen`：原本的「detail：原因」改成「動作前綴：原因」，前綴由前端依動作給：轉職「轉職條件不符」、學技能「無法學習技能」、設定技能欄「技能欄位不正確」（照搬三個 Service 原本的訊息）；只有一個通用代碼（例如 `REQUEST_INVALID`）時只顯示 `errorText`。
- `BagScreen`（`errorMessage`）、`SettingsPanel`（`settingsErrorMessage`）、`WorldPanel`：沒有對應文字的代碼改用 `errorText`。
- `ApiError` 改成 `{ status, reasons }`，`message` 是代碼用逗號接起來（只給除錯與紀錄看）；`readProblem` 不再讀 `detail`。

### 前端文字照搬原句

前端文字就是現在後端的訊息原文，玩家看到的字不變。兩個例外，因為代碼合併：`CONCURRENT_UPDATE` 統一說「資料正被其他請求使用，請重試」（原本角色樂觀鎖說「角色資料已被其他請求更新，請重新整理」，但設定、世界兩個舊畫面本來就把 409 換成自己的「角色資料已被其他請求更新，請再試一次」，那兩處維持不變）；`REQUEST_INVALID` 說通用句（原本會顯示「缺少技能 id」之類，正常操作看不到）。

## Implementation Contract

**行為**

- 任何遊戲 API 錯誤（400、401、404、409）的回應 JSON 有 `status` 與非空的 `reasons`，沒有 `detail`；`title` 不是中文。
- 原因代碼都符合 `^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*(:.+)?$`（冒號後是參數）。錯誤回應與 `GET /api/jobs`、技能總覽的正常 `reasons` 都是。
- 玩家在標題頁、建角頁、背包、技能、設定、世界、旅館、同伴、勳章所、任務板看到的錯誤文字與改之前相同（〈前端文字照搬原句〉的兩個例外除外）。

**介面**

- 後端例外：見〈例外必須帶代碼〉的建構子簽名。
- `ApplicationExceptionHandler` 寫出 `ProblemDetails { Status, Extensions["reasons"] }`；`BadHttpRequestException` 的 reasons 是 `["REQUEST_INVALID"]`，狀態碼沿用例外自帶的。
- `RouteHandlerOptions.ThrowOnBadRequest` 在所有環境為 `true`。
- 前端：`ApiError(status: number, reasons: string[])`；`errorText(code)`、`errorTextOf(reasons)`。

**失敗模式**

- 前端收到認不得的代碼：說「發生錯誤，請稍後再試」，不顯示代碼本身。背包原本的「無法完成（code）」、技能頁與世界頁原本直接顯示代碼的寫法，一律改成查共用表 `errorText`（實作時決定，2026-11-18）。
- 500（未處理的例外）不在 handler 範圍，ASP.NET 預設 ProblemDetails 照舊，前端沒有 reasons 時用通用句。

**驗收**

- 後端：`ApplicationExceptionHandlerTests`（或對應的 Api 測試）斷言 400／401／404／409 的回應沒有 `detail`、有非空 `reasons`；`AccountEndpointsTests`、`CharacterEndpointsTests` 改斷言代碼；一條掃描測試以反射或樣本請求確認錯誤代碼都符合大寫底線格式。
- 例外建構子：reasons 為空時丟 `ArgumentException` 的單元測試。
- 前端：`errorText.test.ts`（每個新代碼與未知代碼）、`client.test.ts`（不讀 `detail`）、`TitleScreen`、`CreateScreen`、`SkillsScreen`、`BagScreen`、`SettingsPanel`、`WorldPanel` 的錯誤顯示測試改用代碼。
- `./scripts/check.sh` exit 0。

**範圍**

- 範圍內：遊戲 API 的錯誤回應與所有原因代碼（錯誤與正常回應）、前端 API client 與各畫面的錯誤文字來源、受影響的規格。
- 範圍外：admin、錯誤文字口吻、HTTP 狀態碼、遊戲規則。

## Risks / Trade-offs

- [機械改名漏掉或誤改字串（例如把內容 id、欄位名稱、事件種類當成代碼）] → 只改「出現在原因代碼位置」的字串：以代碼清單逐一替換帶引號的完整字串，改完用 grep 確認舊駝峰代碼不再出現在 reasons 位置，並讓前後端測試都跑過；`maxLevel` 同時是技能欄位名稱，只改 `SkillLearning` 產生的原因代碼。
- [前後端代碼不同步導致畫面掉到通用句] → 改名在同一個 commit 同時改後端、前端與測試，期間 check.sh 必須全綠。
- [`CONCURRENT_UPDATE` 合併讓一處文字變了] → 已在〈前端文字照搬原句〉列出；設定、世界兩頁本來就用自己的 409 文字。
- [部署時前端與後端版本不一致（舊前端遇到新後端）] → 單人遊戲同一次部署更新兩邊（NAS 一起建置），不做相容期。
