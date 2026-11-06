## Why

原版的必殺技名稱只是暴擊時顯示的文字，沒有任何效果，玩家卻得在建立角色時必填。接下來要匯入怪物技能，如果也搬 406 種怪物必殺技名稱，只會把這個多餘的設定擴大。改成用傷害數字本身呈現暴擊，畫面更直接，也少一個玩家要處理的欄位。

## What Changes

- **BREAKING** 拿掉玩家的必殺技名稱：建立角色不再填寫、設定裡不能修改、角色資料頁不再顯示；`POST /api/characters` 與 `PUT /api/characters/me/settings` 不再收這個欄位，角色回應也不再帶，設定的錯誤原因 `nameInvalid` 一併移除。
- **BREAKING** 戰鬥事件 `AttackEvent` 不再帶 `specialAttackName`。
- 資料庫新增 migration 刪除 `characters` 的 `special_attack_name`。已存在的名稱不保留。
- 前提：`remove-champion` 要先完成，冠軍的名稱隨它一起移除，這個變更不處理。
- 暴擊改用傷害數字呈現：普通傷害改成白色；暴擊維持 34 號字、紅色金邊，出現時多一個放大再彈回的動作；拿掉暴擊時的字牌（`CRITICAL` 或玩家取的名稱）。怪物與玩家用同一套數字效果。
- 文字戰報的暴擊統一寫「暴擊」，取代玩家的自訂名稱與怪物的「重擊」。
- 原版怪物的必殺技名稱確定不搬：匯入腳本不再把它列進 `docs/research/wog45/gaps.md`，資料仍留在 `docs/research/wog45/data/monsters.json`。

## Non-Goals

- 不把必殺技做成取代普通攻擊的招式。
- 不改暴擊的機率、倍率與判定；傷害計算完全不變。
- 不動寵物的戰報文字（「會心一擊」）與其他字牌（`LEVEL UP`、暈眩、膠著）。
- 技能說明裡的「必殺技機率」是原版對暴擊率的稱呼，只存在匯入腳本的樣板與研究資料，不在這次處理。

## Capabilities

### New Capabilities

- `critical-hits`: 暴擊在舞台與文字戰報的呈現方式，以及玩家、怪物都沒有必殺技名稱。

### Modified Capabilities

- `wog-content-import`: 「Data the rules cannot use is listed」的怪物欄位不再列必殺技，因為這是決定不搬，不是規則還不支援。

## Impact

- Domain：`backend/EndlessBattle.Domain/Battle`、`backend/EndlessBattle.Domain/Characters`
- Application：`backend/EndlessBattle.Application/Characters/CharacterService.cs`
- Infrastructure：`backend/EndlessBattle.Infrastructure/Persistence`（重新 scaffold）
- Api：`backend/EndlessBattle.Api/Endpoints/CharacterEndpoints.cs`、`backend/EndlessBattle.Api/Endpoints/Contracts.cs`
- Tests：`backend/EndlessBattle.Tests`
- db：`backend/db/ddl` 新增一個 migration
- frontend：`frontend/src/game`、`frontend/src/render`、`frontend/src/motion/timings.ts`、`frontend/src/ui`、`frontend/src/api/schema.ts`（重新產生）
- 匯入：`scripts/import-wog45.py`、`docs/research/wog45/gaps.md`（重新產生）
