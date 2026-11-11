## Summary

遊戲 API 的核心資源（角色、冒險與戰鬥事件、技能與轉職、世界地圖）改成完整名稱的欄位，Domain 一起改名；API 不再直接把 Domain 型別序列化出去；前端把這些回應經 `XxxConverter.toVo` 轉成 Vo 再交給畫面與 `src/game`；拆掉 `/api/content`。這是遊戲端 DTO→VO 分層的第一個變更，背包、商店、倉庫、鐵舖、任務、寵物、傭兵、勳章、旅館、銀行的 Vo 留給下一個變更。

## Motivation

- 前端除了公開資料（`/api/public/jobs`、`/api/public/settings`）之外，約 60 個型別直接拿 OpenAPI 產生的型別給畫面與 `src/game` 用（`frontend/src/game/types.ts`），API 欄位一改就牽動整個前端。使用者要的是「改 API 介面時只改 parse 的地方，資料的調整不直接影響核心邏輯」，admin 已在 `content-editor` 變更做到，遊戲端還沒有。
- API 欄位大量縮寫（`str`、`smart`、`au`、`be`、`at`、`mdf`、`hpMax`、`exp`），與專案「命名用完整字詞、不縮寫」的慣例不符；部分回應（`StatBonus`、`StatRequirement7`、戰鬥事件、`AdvanceSummary`）直接序列化 Domain record，Domain 一改名 API 就跟著變。
- `/api/content` 一支 API 混著怪物名、全部職業、性別／屬性／出生地，已決定新畫面不用它，要讓資料改由各資源自己的 API 提供。
- 接下來每一頁都要換新設計，先把核心資源的邊界整理好，換畫面時就不用再碰 API 型別。

## Proposed Solution

- **名稱**：能力值改成 `strength`、`intelligence`、`life`、`vitality`、`agility`、`charisma`、`faith`；戰鬥數值改成 `attack`、`magicAttack`、`defense`、`magicDefense`；HP 改成 `healthPoints`／`maxHealthPoints`；SP 改名為 MP，寫成 `manaPoints`／`maxManaPoints`；經驗值改成 `experience`、`jobExperience`、`nextLevelExperience`；等級範圍的 `min`／`max` 改成 `minimum`／`maximum`。原因代碼 `statTooLow:<能力>` 的能力也改成完整名稱。畫面上的「HP」「SP」標籤這次不動。
- **Domain 一起改名**：`BaseStats`、`StatBonus`、`StatRequirement7`、`DerivedStats`、`JobGrowth`、`MonsterDefinition`、`Character`、戰鬥狀態與戰鬥事件、`AdvanceSummary`、冒險設定的縮寫欄位改成同樣的完整名稱；用字串表示的能力代碼（`"str"`…）改成 Domain 列舉。行為不變。
- **儲存格式不變**：`backend/content/*.json` 與資料庫裡裝備強化的 JSON 欄位仍用原本的縮寫鍵；Infrastructure 用自己的 entry 型別讀寫，再轉成 Domain。不需要 migration，也不用改內容檔。
- **API 自己的回應型別**：遊戲 API 不再序列化 Domain record，一律在 endpoint 轉成 `*Response`。
- **前端 Converter／Vo**：角色、冒險推進（含戰鬥事件與傭兵戰報）、技能、轉職、世界地圖的回應在 `src/api` 經 Converter 轉成 Vo；缺必要欄位或型別不對丟出 `ParseError`，不認識的欄位忽略。畫面與 `src/game` 改用 Vo，不再 import 這些資源的產生型別。
- **拆掉 `/api/content`**：戰鬥事件直接帶怪物名稱；角色頁與任務頁的性別、屬性、出生地、職業名稱改從 `/api/public/settings` 與 `/api/public/jobs` 取。

## Non-Goals

- 背包、商店、倉庫、鐵舖、任務、寵物、傭兵、勳章、旅館、銀行的 Vo 與欄位改名（下一個變更）。它們的回應只因為共用的能力型別（例如道具的 `bonus`）改名而跟著換欄位名稱，前端照新名稱讀取，還不轉成 Vo。
- 畫面標籤從「SP」改成「MP」。
- admin 怪物記錄的 `SkillPoints`（其實是 SP）改名。
- 內容 JSON 與資料庫欄位改名。

## Alternatives Considered

- **一次改完全部遊戲 API**：一個變更裡的工作超過一次能審查的量，使用者選擇拆成兩個。
- **跟著換畫面逐頁做**：每頁的畫面變更都會夾帶 API 改名，審查時分不出哪些是行為改變；共用的能力型別也會出現半新半舊的狀態。
- **SP 寫成 `spiritPoints` 保留 SP**：使用者選擇改成 MP（`manaPoints`）。

## Impact

- Affected specs: 新增 `game-api-contract`；修改 `accounts`（不用登入就能呼叫的端點不再包含 `/api/content`）、`public-game-data`（`/api/content` 已移除）
- Affected code:
  - Modified:
    - backend/EndlessBattle.Domain（Characters、Content、Items、Battle、Adventure、Jobs、Equipment 等用到縮寫欄位的型別）
    - backend/EndlessBattle.Application（引用改名欄位的服務，含 Balance）
    - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs、backend/EndlessBattle.Infrastructure/Persistence/EnhancementJson.cs、角色 repository
    - backend/EndlessBattle.Api/Endpoints/Contracts.cs、CharacterEndpoints.cs、AdventureEndpoints.cs、SkillEndpoints.cs、JobEndpoints.cs、WorldEndpoints.cs，以及回傳共用能力型別的其他 endpoint
    - backend/EndlessBattle.Api/Program.cs（拿掉 /api/content 的註冊）
    - backend/openapi/EndlessBattle.Api.json
    - backend/EndlessBattle.Tests
    - frontend/src/api/client.ts、frontend/src/api/parse.ts、frontend/src/api/schema.ts（重新產生）
    - frontend/src/game（types.ts、stats.ts、battleLog.ts、battlePlayback.ts、stageModel.ts、skills.ts、world.ts、missions.ts、settings.ts、creationDraft.ts 等）
    - frontend/src/ui（AdventureScreen、CharacterScreen、SkillsScreen、WorldPanel、MissionsScreen、SettingsPanel、StatBar 等）
    - frontend/src/test/fixtures.ts
  - New:
    - frontend/src/api/characterData.ts、frontend/src/api/adventureData.ts、frontend/src/api/skillData.ts、frontend/src/api/worldData.ts
    - frontend/src/game/character.ts、frontend/src/game/battleEvents.ts
  - Removed:
    - backend/EndlessBattle.Api/Endpoints/ContentEndpoints.cs
