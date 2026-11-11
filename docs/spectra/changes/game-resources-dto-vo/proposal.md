## Summary

遊戲 API 剩下的資源（背包、道具使用、商店、倉庫、鐵舖與合成、任務、寵物、傭兵、勳章、旅館、密藥）改成完整名稱的欄位，Domain 裡這些資源的縮寫一起改名；前端把這些回應經 `XxxConverter.toVo` 轉成 Vo，刪掉 `frontend/src/game/types.ts`，並用 lint 規則擋住畫面與 `src/game` 再 import 產生的型別。順便修掉背包裝備需求未達成卻顯示綠色的缺陷。這是遊戲端 DTO→VO 分層的第二個、也是最後一個變更（第一個是已封存的 `game-api-dto-vo`）。

## Motivation

- `game-api-dto-vo` 只處理了角色、冒險、技能、轉職、世界地圖；`frontend/src/game/types.ts` 還有 38 個別名直接把 OpenAPI 產生的型別交給 8 個 `src/game` 模組與 14 個畫面檔（含測試）使用，這些 API 一改仍會牽動畫面與遊戲邏輯。
- 遊戲 OpenAPI 還剩五個縮寫欄位：道具的 `supplyHp`、`supplySp`，任務獎勵與任務完成的 `exp`，傭兵目錄的 `expPercent`；密藥與旅館房間的種類列舉值還是 `Exp`、`JobExp`。
- Domain 的 `ItemDefinition`、`RefineStoneSpec`、`MissionDefinition`、`MercenaryDefinition`、`MercenaryFight`、`BuffKind` 等仍用縮寫，與專案「命名用完整字詞」的慣例不符；上一個變更已在 design 註明留到這裡。
- 背包裡未達成的裝備需求（例如「力量 ≥ 30」）應顯示緋紅色，實際卻是綠色：`bagWidgets.tsx` 給未達成的需求加上全域的 `crimson-text`，但 `BagScreen.css` 的 `.bag__requirement { color: var(--green) }` 與它權重相同、載入順序在後，蓋掉了緋紅色。測試只檢查 class 名稱，所以沒有抓到。

## Proposed Solution

- **API 名稱**：`supplyHp`→`supplyHealthPoints`、`supplySp`→`supplyManaPoints`、任務的 `exp`→`experience`、傭兵目錄的 `expPercent`→`experiencePercent`；`BuffKind` 的 `Exp`→`Experience`、`JobExp`→`JobExperience`（密藥與旅館房間回應都用這個列舉）。
- **Domain 一起改名**：道具、精鍊石範圍、任務、傭兵、密藥、道具使用的縮寫欄位與區域變數改成完整名稱，行為不變。
- **儲存格式不變**：內容檔（`items.json` 的 `supplyHp`、精鍊範圍的 `at`…、`inn.json` 與密藥的 `"Exp"`、任務與傭兵檔）和資料庫 `character_buffs.kind` 裡已存的 `Exp`／`JobExp` 都不改；Infrastructure 讀寫時對照新舊名稱。不需要 migration。
- **前端 Converter／Vo**：上述資源的每支 API 回應在 `src/api` 轉成 Vo；時間欄位（密藥與住宿的到期、任務期限、寵物下次餵食、傭兵下次出戰）轉成 `Date`。`src/game` 與畫面改用 Vo，`frontend/src/game/types.ts` 刪除。
- **擋住回頭路**：前端 eslint 加規則，`src/game`、`src/ui`、`src/render` 不得 import `src/api/schema`。
- **背包需求顏色**：未達成的需求改用背包自己的修飾 class，並寫在 `.bag__requirement` 之後，讓緋紅色生效。

## Non-Goals

（寫在 design.md）

## Alternatives Considered

- **背包顏色另開一個修缺陷的變更**：改動只有一個 class 與一段 CSS，使用者選擇併進這次一起修。
- **把 `BuffKind` 的 API 值保留成 `Exp`**：列舉值也是 API 的一部分，留著會讓前端仍要認得縮寫；資料庫裡的舊值由 Infrastructure 對照即可。

## Impact

- Affected specs: 修改 `game-api-contract`（完整名稱涵蓋所有遊戲 API、儲存格式包含密藥種類、前端轉換涵蓋所有遊戲 API）
- Affected code:
  - Modified:
    - backend/EndlessBattle.Domain（Items、Forge、Missions、Mercenaries、Pets 用到縮寫的型別；Items/Buffs.cs 的 BuffKind）
    - backend/EndlessBattle.Application（Missions、Adventure 的傭兵結果、Items）
    - backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs、backend/EndlessBattle.Infrastructure/Persistence/BuffRepository.cs
    - backend/EndlessBattle.Api/Endpoints/Contracts.cs 與回傳上述欄位的 endpoint
    - backend/openapi/EndlessBattle.Api.json
    - backend/EndlessBattle.Tests
    - frontend/eslint.config.js
    - frontend/src/api/client.ts、frontend/src/api/schema.ts（重新產生）
    - frontend/src/game（inventory.ts、forge.ts、missions.ts、pets.ts、mercenaries.ts、honour.ts、inn.ts、buffs.ts）
    - frontend/src/ui（BagScreen、bagWidgets、BagScreen.css、ForgePage、MissionsScreen、PetsPage、MercenaryPage、HonourPage、InnPage、CharacterScreen、CompanionsScreen）
    - frontend/src/test/fixtures.ts
  - New:
    - frontend/src/api/inventoryData.ts、frontend/src/api/forgeData.ts、frontend/src/api/missionData.ts、frontend/src/api/companionData.ts、frontend/src/api/lodgingData.ts（各自的測試檔）
    - frontend/src/game 下各資源的 Vo 型別（依 design 的對照放進既有模組或新檔）
  - Removed:
    - frontend/src/game/types.ts
