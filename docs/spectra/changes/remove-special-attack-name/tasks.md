## 1. 暴擊的畫面與戰報

- [x] 1.1 依「暴擊只靠傷害數字呈現」與 Requirement「Critical hits are shown by the damage number」，讓暴擊不再產生字牌。先寫失敗測試 `stageEffectsFor_PlayerCritical_ShowsNoPlate`、`stageEffectsFor_MonsterCritical_ShowsNoPlate`（取代 `stageEffectsFor_PlayerCritical_ShowsSpecialAttackPlate` 與備用字測試），再從 `frontend/src/game/stageModel.ts` 移除字牌與 `CriticalPlateText`。以 `cd frontend && pnpm check` 驗證。
- [ ] 1.2 依「文字戰報統一寫暴擊」與 Requirement「The battle log calls every critical hit 暴擊」，先改 `frontend/src/game/battleLog.test.ts` 的表格案例（玩家物理、玩家魔法、怪物，文字照 spec 的 Example），確認失敗後改 `describeAttack`，不再讀 `specialAttackName`、不再寫「重擊」；寵物的「會心一擊」案例維持通過。以 `cd frontend && pnpm check` 驗證。
- [ ] 1.3 依「普通傷害改成白色」，改 `playDamageNumber` 的顏色與暴擊放大動作，新增 `CriticalNumberPopSeconds` 到 `frontend/src/motion/timings.ts`，並從 `StageRenderer` 的字型預載拿掉 `CRITICAL`、更新提到玩家自訂名稱的註解。這是樣式，不寫測試；以 `pnpm dev` 在 375×667 的實際戰鬥確認普通白色、暴擊紅色金邊有放大彈回，7 位數傷害不超出舞台。 [after: 1.1]

## 2. 戰鬥資料

- [ ] 2.1 依「拿掉名稱的順序：戰鬥資料、設定、角色」與 Requirement「Nobody has a special attack name」，讓戰鬥事件不帶名稱。先寫失敗測試 `AttackEvent_SerializesWithoutSpecialAttackName`，再從 `AttackEvent`、`CombatantState`、`WogBattle`、`CombatBridge` 移除，並改掉測試呼叫（數值斷言一律不動），API 執行中跑 `pnpm generate:api` 更新 `frontend/src/api/schema.ts`、移除前端測試資料的這個欄位。以 `cd backend && dotnet test` 與 `cd frontend && pnpm check` 驗證。 [after: 1.1, 1.2]

## 3. 設定

- [ ] 3.1 依 Requirement「Nobody has a special attack name」，讓設定不再收名稱。先寫失敗的 API 測試 `UpdateSettings_WithoutSpecialAttackName_SavesSettings`（門檻無效仍回 `invalidPercent`），再改 `AdventureSettingsRequest`、`CharacterEndpoints`、`CharacterService.UpdateSettingsAsync`，移除 `nameInvalid`。前端移除 `SettingsPanel` 的欄位、`game/settings.ts` 的表單欄位與錯誤文字，並重新產生 `schema.ts`，對應的 `SettingsPanel`、`AdventureScreen`、`settings` 測試一起更新。以 `cd backend && dotnet test` 與 `cd frontend && pnpm check` 驗證。 [after: 2.1]

## 4. 角色

- [ ] 4.1 依「用 migration 刪除角色的名稱欄位」與 Requirement「Nobody has a special attack name」，讓建立角色不再收名稱、角色回應不再帶。先寫失敗的 API 測試 `Create_WithoutSpecialAttackName_CreatesCharacter`（回應沒有 `specialAttackName`），再改 `CreateCharacterRequest`、`CreateCharacterCommand`、`CharacterFactory`（移除 `ValidateSpecialAttackName`）、`Character`、`CharacterResponse`、`CharacterRepository`。新增 `backend/db/ddl/V000000018__remove_character_special_attack_name.sql`，`docker compose up -d` 後跑 `./scripts/scaffold-db.sh`，並以 Testcontainers 測試 `Characters_HasNoSpecialAttackNameColumn` 確認欄位已刪。前端在 4.2 才移除欄位，這段期間多送的欄位會被 API 忽略。以 `cd backend && dotnet test` 驗證。 [after: 3.1]
- [ ] 4.2 依 Requirement「Nobody has a special attack name」，讓前端不再顯示名稱。先改 `CreateCharacterScreen.test.tsx`、`CharacterScreen.test.tsx`，斷言建立角色的請求與畫面、角色資料頁都沒有必殺技，確認失敗後改 `CreateCharacterScreen`、`CharacterScreen`，移除 `names.ts` 的 `SpecialAttackNameMaxLength` 與超長測試，重新產生 `schema.ts`、清掉 `frontend/src/test/fixtures.ts` 等測試資料。以 `cd frontend && pnpm check` 驗證。 [after: 4.1]

## 5. 匯入清單

- [ ] 5.1 依「怪物必殺技記為決定不搬」與 Requirement「Data the rules cannot use is listed」，先在 `WogImportTests` 寫失敗測試 `Gaps_DoesNotListMonsterSpecialAttacks`（`gaps.md` 沒有「必殺技 猛烈一擊」、仍有「爆氣 Lv2 rate 10」，`monster-13` 不在沒有搬的欄位，怪物對照方式寫明決定），再改 `scripts/import-wog45.py` 的怪物段落，執行 `python3 -I scripts/import-wog45.py` 重新產生 `docs/research/wog45/gaps.md`，測試轉綠後 commit。接著再跑一次腳本，以 `git diff --exit-code backend/content docs/research/wog45/gaps.md` exit code 0 確認第二次沒有變動。

## 6. 收尾

- [ ] 6.1 實際跑一次遊戲確認資料流：建立新角色（畫面沒有必殺技欄位）、開設定（沒有名稱欄位，門檻能存）、看角色資料頁（沒有必殺技列）、打一場戰鬥（文字戰報寫暴擊）。以 `pnpm dev` 搭配 Development API 手動確認。 [after: 1.3, 4.2]
- [ ] 6.2 執行 `./scripts/check.sh`，確認 exit code 0。 [after: 5.1, 6.1]
