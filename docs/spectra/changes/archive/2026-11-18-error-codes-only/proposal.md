## Why

錯誤怎麼呈現是前端的事，但目前後端把中文句子放在錯誤回應的 `detail` 裡，標題頁、建角頁與舊畫面直接把它顯示給玩家；約 127 個丟錯誤的地方只有約 45 個帶原因代碼，其餘前端只能照登後端的句子。兩邊各寫一套說法，也讓前端沒辦法依畫面與角色口吻決定要說什麼。使用者 2026-11-15 要求任務板完成後優先改，2026-11-18 定案做法。

## What Changes

- **BREAKING** 遊戲 API 的錯誤回應（ProblemDetails）不再帶 `detail` 與中文 `title`；每個錯誤的 `reasons` 至少有一個原因代碼。原本的中文句子只留在伺服器端的例外訊息（紀錄用）。
- **BREAKING** 所有原因代碼改成全大寫底線，例如 `bagFull` → `BAG_FULL`、`statTooLow:strength` → `STAT_TOO_LOW:strength`、`missingTool:item-1378` → `MISSING_TOOL:item-1378`（冒號後的參數不變）。
- 現在沒有代碼的錯誤補上代碼：
  - 請求不完整或格式錯誤（缺少 id、數量、欄位，或內容不是正確的 JSON）一律 `REQUEST_INVALID`。
  - 帳號：`USERNAME_INVALID`、`PASSWORD_TOO_SHORT`、`USERNAME_TAKEN`、`AUTHENTICATION_FAILED`、`ACCOUNT_NOT_FOUND`。
  - 建角：`CHARACTER_NAME_LENGTH`、`CHARACTER_NAME_TOO_COMPLEX`、`JOB_UNKNOWN`、`JOB_NOT_FIRST_TIER`、`STAT_BELOW_ONE`、`POINTS_NOT_SPENT`、`SEX_INVALID`、`ELEMENT_INVALID`、`BIRTH_INVALID`。
  - 沒有角色時 `CHARACTER_NOT_FOUND`；`SKILL_UNKNOWN`、`RECIPE_UNKNOWN`；背包或道具找不到沿用 `ITEM_NOT_FOUND`。
  - 多個請求同時改同一份資料（死鎖、角色資料已被更新、鑰匙已被用掉、熟練度已變動）一律 `CONCURRENT_UPDATE`；任務記錄重複沿用 `MISSION_CONFLICT`，傭兵合約重複沿用 `MERCENARY_HIRED`。
- **BREAKING** 原因代碼只用在錯誤回應（2026-11-18 review 後追加）：職業清單 `GET /api/jobs` 與技能總覽 `GET /api/skills` 拿掉 `reasons`，改用狀態欄位表示每個條件達成了沒——職業的 `levelMet`、`parentProficiencyMet`、`statRequirements[].met`，技能的 `taughtByCurrentJob`、`prerequisite.met`、`next.enoughMoney`、`next.enoughProficiency`（`eligible`、`canLearn` 保留當總結）。轉職、學技能失敗時的錯誤照舊回代碼。後端的代碼常數類別 `ReasonCodes` 改名 `ErrorCodes`。
- 前端依代碼決定文字：新增一張共用的代碼文字表，內容照搬現在後端的中文句子，玩家看到的字不變；認不得的代碼說「發生錯誤，請稍後再試」。標題頁（登入、註冊）、建角頁、舊畫面背包、技能、設定、世界不再讀 `detail`；技能頁原本「後端句子：原因」的前綴改由前端依動作給。`ApiError` 拿掉 `detail`。
- 不推翻裁定彙整（docs/superpowers/specs/2026-10-22-wog-remake-rulings.md）的任何規則；遊戲規則、拒絕條件與檢查順序都不變，只改代碼的寫法與錯誤回應的內容。

## Non-Goals

- admin（`/admin/...` 與 admin 前端）的錯誤格式不在範圍：admin 是本機內容編輯器，欄位錯誤已經是結構化的 `ContentFieldError`。
- 不改錯誤文字的口吻：照搬原句，換成角色口吻之後跟各頁新設計一起改。
- 不改 HTTP 狀態碼。

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `game-api-contract`: 錯誤回應只帶原因代碼（不帶 `detail`、中文 `title`），原因代碼一律全大寫底線，請求不完整一律 `REQUEST_INVALID`；能力值不足的代碼改成 `STAT_TOO_LOW:<stat>`；職業清單與技能總覽用狀態欄位表示條件、不帶原因代碼。
- `accounts`: 註冊與登入的錯誤改用代碼 `USERNAME_INVALID`、`PASSWORD_TOO_SHORT`、`USERNAME_TAKEN`、`AUTHENTICATION_FAILED`。
- `character-creation-rules`: 建角錯誤改用代碼（名稱長度、職業、能力分配、選項），衝突代碼改大寫。
- `title-and-character-creation`: 標題頁與建角頁依代碼顯示文字，衝突代碼改大寫。
- `missions`: 接取、放棄、完成的拒絕代碼改大寫。
- `missions-screen`: 莉瑟台詞表的代碼改大寫。
- `mission-objectives`: `PET_MISSING`、`SKILL_TOO_LOW`。
- `recipe-prerequisites`: `MISSION_INCOMPLETE`。
- `honour`: 兌換拒絕代碼改大寫。
- `honour-screen`: 莉瑟台詞表的代碼改大寫。
- `inn`: `NOT_ENOUGH_MONEY`、`UNKNOWN_ROOM`。
- `inn-screen`: 瑪莎台詞與訂房失敗的代碼改大寫。
- `mercenaries`: 僱用、派駐、改名的拒絕代碼改大寫。
- `pets`: 餵食、攜帶、改名的拒絕代碼改大寫。
- `companions-screen`: 店員台詞表的代碼改大寫。
- `critical-hits`: 設定拒絕代碼 `INVALID_PERCENT`。
- `skill-effects`: 職業技能的學習條件代碼 `PREREQUISITE`、`WRONG_JOB`。

## Impact

- Affected specs: game-api-contract, accounts, character-creation-rules, title-and-character-creation, missions, missions-screen, mission-objectives, recipe-prerequisites, honour, honour-screen, inn, inn-screen, mercenaries, pets, companions-screen, critical-hits, skill-effects
- Affected code:
  - Modified:
    - Domain：backend/EndlessBattle.Domain（各規則產生原因代碼的地方、`CharacterCreationException`、`BaseStats.StatTooLow`、`ForgeTools.Missing`）
    - Application：backend/EndlessBattle.Application/Common/ApplicationExceptions.cs 與各 Service 丟錯誤的地方
    - Infrastructure：backend/EndlessBattle.Infrastructure/Persistence（Account、Character、Mission、Mercenary、JobProficiency Repository、EfUnitOfWork 的衝突）
    - Api：backend/EndlessBattle.Api/Errors/ApplicationExceptionHandler.cs、backend/EndlessBattle.Api/Endpoints/*.cs（缺少欄位的檢查）、backend/openapi/EndlessBattle.Api.json（若錯誤格式文件有變）
    - Tests：backend/EndlessBattle.Tests（代碼與錯誤回應的斷言）
    - 職業與技能狀態：backend/EndlessBattle.Domain/Jobs/JobChange.cs、backend/EndlessBattle.Domain/Skills/SkillLearning.cs、backend/EndlessBattle.Application/Jobs/JobService.cs、backend/EndlessBattle.Application/Skills/SkillOverview.cs 與 SkillService、backend/EndlessBattle.Api/Endpoints/Contracts.cs（`JobOptionResponse`、`SkillResponse`、`SkillPrerequisiteResponse`、`SkillNextLevelResponse`）、backend/openapi/EndlessBattle.Api.json、frontend/src/api/schema.ts、frontend/src/game/skillOverview.ts、frontend/src/game/skills.ts、frontend/src/ui/SkillsScreen.tsx
    - 代碼常數：backend/EndlessBattle.Domain/ReasonCodes.cs 改名 backend/EndlessBattle.Domain/ErrorCodes.cs
    - frontend：frontend/src/api/client.ts、frontend/src/game（inventory、settings、skills、missions、honour、inn、companions、world 等代碼文字表）、frontend/src/ui（TitleScreen、CreateScreen、BagScreen、SkillsScreen、SettingsPanel、WorldPanel、各新內頁）與對應測試
  - New:
    - frontend/src/game/errorText.ts（共用代碼文字表）
  - Removed: (none)
