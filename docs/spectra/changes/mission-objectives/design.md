## Context

`docs/research/wog45/gaps.md` 列出 97 個沒匯入的任務與 73 隻沒匯入的怪物，現有規則做不到是主因。以下數字都是用原版資料（`docs/research/wog45/data/`）實際算出來的：

| 項目 | 數量 | 說明 |
|---|---|---|
| 多目標擊殺任務 | 15 | 同一任務的目標都在同一個地區 |
| 交寵物的一般任務 | 30 | 其中 5 個同時有擊殺目標 |
| 有技能條件的任務 | 8 | 任務 325 的技能沒匯入，實際寫入 7 個 |
| NPC 怪物 | 28 | 等級、HP、敏捷都是 0；27 隻是任務目標，7 隻有訊息 |
| 沒設子地區的怪（地區有子地區） | 53 | 一般怪 18 隻、任務怪 35 隻 |
| 有前置任務的配方 | 23 | 配方 174～196，前置是任務 313 或 317 |

依新規則模擬匯入：一般任務 240 → 336 個，只剩任務 130 不匯入（全域任務怪，使用者決定先不補）；怪物 391 → 461 隻，只剩 261（任務 130 的怪）、287（地點 99）、324（不是目標的 NPC）。

原版行為的出處（`WOG4.5_Releas_CT`）：遭遇與 NPC 在 `class/wog_fight_select.php`，回報檢查在 `class/wog_mission_tool.php` 的 `mission_check`，配方前置在 `class/wog_act_syn.php`。

硬規則：

- 內容只放 `backend/content/*.json`（遵守：只改匯入腳本後重新產生）。
- migration 由 Flyway 管理（新增 `V000000019`，不動已 commit 的版本；用的是 `DROP COLUMN`，不是需要逐次徵詢的 `DROP TABLE`／`VIEW`／`DATABASE`、`TRUNCATE`）。
- scaffold 產生檔不手改（執行 `scripts/scaffold-db.sh`）。
- `frontend/src/api/schema.ts` 用 `pnpm generate:api` 產生。
- admin 路由：不涉及；`admin/src` 沒有用到任務的 `kill` 欄位。

## Goals / Non-Goals

**Goals:**

- 一般任務支援多目標擊殺、交寵物、技能條件。
- NPC 遭遇與沒設子地區的怪依本文規則運作。
- 配方支援前置任務。
- 重新匯入後，現有任務、怪物、配方的原版數值不變，只多出新規則帶來的內容。

**Non-Goals:**

- 高等級技能獎勵（任務 240～257 的 Lv6～9）：使用者決定還沒做之前先不考慮，這些任務繼續不給技能，`gaps.md` 照列。
- 鎖定原版 `s_master=1` 的技能等級。已知現況：`無我` Lv6 等等級目前可以 0 元學到，這次不處理，之後做任務解鎖等級時一起看。
- 任務 130 與全域任務怪。
- 任務 275 的技能獎勵（沒限職業，但技能只有魔導師能學，維持不給）。
- 任務 182、192 的寵物獎勵（獎勵是任務怪，維持不給）。
- NPC 對話系統：只在遭遇時顯示一段原版訊息。
- 調整平衡：新怪加入後的勝率變化只記錄，不改數值。

## Decisions

### 擊殺進度存成 JSON 陣列欄位

`character_missions` 新增 `kill_progress JSON NOT NULL`，內容是依 `kills` 順序的已擊殺數，取代 `progress`。

替代方案是另開一張以怪物 id 為鍵的進度表，但資料庫沒有內容資料，現有紀錄的 `progress` 無法用 SQL 換成怪物 id；每日任務的目標又是擲骰算出來的。用索引對應時，舊紀錄只要轉成 `[progress]` 就不會遺失。讀取時索引超出範圍視為 0，多出的值忽略，內容增減目標也不會出錯。

### 任務怪以任務為單位出現

任務還有沒達成的擊殺目標，或是沒有擊殺目標但要交寵物時，該任務的所有任務怪都會出現。目標已滿的怪在同任務其他目標未滿時仍會出現，只是不再計數。這與原版相同，比起逐隻判斷也更簡單。

既有規則「任務怪只在任務尚未達成且未過期時出現」不變，只是把「達成」擴充到多目標，並納入只交寵物的任務（`捕捉行動`、`輝煌都市傳說(3)` 的任務怪要能遇到才抓得到）。

### 技能條件在回報時檢查

`requiredSkill` 不擋接取，回報時才檢查，未達等級回 `skillTooLow`。這與原版 `mission_check` 相同：任務可以先接，學到技能再回報。比對的是同一技能的已學等級是否達到要求。

### NPC 遭遇不算戰鬥

遇到 NPC 時不開打、不給報酬、HP／SP 不變、不計入場次，目標未滿才 +1（計入 `MissionKills`），推進一個 `BattleTiming.FightIntervalSeconds`。不擲額外亂數，所以沒有 NPC 出現時擲骰序列不變。

傭兵遇到 NPC 也會 +1，不扣次數、不記場次（原版只在打贏或打輸時扣次數），照常等下一個 100 秒。

新增事件 `NpcEncounterEvent`（type `npc`），帶 `MonsterId`、`Name`、`Message`。事件自帶名稱，舞台不用再查表。舞台顯示 NPC 名牌，戰報顯示遭遇與訊息；NPC 沒有血條，所以不走 `encounter`。

### 任務進度事件多一個 Completed

`MissionProgressEvent` 新增 `Completed`，表示所有擊殺目標都滿了。前端原本用 `progress === required` 判斷達成，多目標時會誤判。`RequiresDelivery` 擴充為「要交道具或寵物」。

### 配方前置要完成任務

`requiredMissionId` 指向的任務必須是已完成狀態才能合成，未完成回 `missionIncomplete`。

**這是刻意與原版不同的地方：** 原版只檢查有沒有這筆任務紀錄，接了就能做，`gaps.md` 卻寫成要先完成。使用者選擇要完成才能做。前置任務不能是每日任務（每日紀錄會過期）。

`GET /api/forge/recipes` 改成依角色回傳 `requiredMission`（id、標題、是否已完成），鎖住的配方照樣列出，與原版一樣看得到條件。

### 寵物獎勵等級上限調成 530

`MaximumPetRewardLevel` 從 280 調成 530。任務 265（450）與 312（530）的寵物獎勵會因這次的規則變更而匯入，530 是原版會寫入內容的最大值。

### NPC 訊息在匯入時整理

原始資料裡的換行被匯出成字面的 `rn`（只出現在怪物 265、268）。匯入時把連續的 `rn` 換成一個空格，並去掉頭尾空白；戰報是單行顯示，不需要保留換行。

## Implementation Contract

**內容資料：**

```json
{ "id": "mission-29", "kills": [{ "monsterId": "monster-111", "count": 3 }, { "monsterId": "monster-112", "count": 3 }] }
{ "id": "mission-16", "deliverPet": "monster-1" }
{ "id": "mission-318", "requiredSkill": { "skillId": "skill-414", "level": 2 } }
{ "id": "monster-265", "level": 0, "hp": 0, "agi": 0, "npc": true, "npcMessage": "我是倫魯 你想要前往熔炎火山…", "missionId": "mission-133" }
{ "id": "recipe-174", "requiredMissionId": "mission-317" }
```

**內容驗證（`JsonGameContent`，違反時丟 `GameContentException`）：**

- `kills`：怪物存在、`count ≥ 1`、同任務不重複，都在任務的 `placeId`；是任務怪時 `missionId` 必須是這個任務。
- 一般任務至少要有 `kills`、`deliver`、`deliverPet` 其中一項。每日任務不能有 `kills`、`deliverPet`、`requiredSkill`。
- `deliverPet`：怪物存在，不是 NPC；是任務怪時 `missionId` 必須是這個任務。
- `requiredSkill`：技能存在，等級在 1～`MaxLevel`。
- 任務怪：`missionId` 指向一般任務，不再要求是擊殺目標；不能是全域怪（維持現狀）。
- `npc: true`：必須有 `missionId`、是該任務的擊殺目標、沒有掉落與技能；可以不符合等級、HP、敏捷的下限。其他怪照舊要符合。
- 有子地區的地區，怪物的 `subPlaces` 可以是空的；沒有子地區的地區仍必須是空的。
- 配方的 `requiredMissionId`：任務存在且不是每日任務。

**Domain 形狀：**

- `MissionDefinition.Kills`（清單）取代 `Kill`，新增 `DeliverPet`、`RequiredSkill`。
- `MissionObjective.Kills` 取代 `Kill`；每日任務擲出的單一目標放成一個元素的清單。
- `MissionRecord.Progress` 是 `IReadOnlyList<int>`。
- `ActiveMission.Targets` 是目標與進度的清單，取代 `MonsterId`、`Required`、`Progress`。
- `MonsterDefinition` 新增 `Npc`、`NpcMessage`；`Recipe` 新增 `RequiredMissionId`。

**回報的錯誤代碼順序：** `missionNotActive`、`objectiveIncomplete`、`itemsMissing`、`petMissing`、`skillTooLow`、`bagFull`、`petsFull`。

**API：**

- `MissionResponse.Kill` 改成 `Kills`（清單）；新增 `RequiredSkill`（`SkillId`、`Name`、`Level`、`Met`，沒有條件時為 null）。
- `RecipeResponse` 新增 `RequiredMission`（`Id`、`Title`、`Completed`，沒有前置時為 null）。
- 合成未完成前置任務：400，reasons 含 `missionIncomplete`，不扣材料與金錢。

**Migration（`backend/db/ddl/V000000019__mission_kill_progress.sql`）：** 新增可為 null 的欄位 → `UPDATE` 成 `JSON_ARRAY(progress)` → 改成 `NOT NULL` 並移除 `progress`。步驟：migration → `docker compose up -d` → `scripts/scaffold-db.sh`。

**匯入：** 第二次執行 `git diff --exit-code` 為 0。原有任務、怪物、配方除了新規則的欄位外，原版數值不變。

**不在範圍：** 見 Non-Goals。

## Risks / Trade-offs

- [新怪加入一般遭遇池，改變既有地區的難度] → 18 隻一般怪的等級可能高於地區推薦等級（例如地區 7 的 `鮮血騎士`，HP 39150）。這是使用者決定的規則，平衡由其他人負責：只跑平衡報告記錄前後差異，不改數值。
- [任務 API 欄位改名會讓舊前端壞掉] → 同一個 commit 重新產生 `schema.ts` 並更新前端；沒有外部使用者。
- [內容改動量大，驗證可能擋下未預期的資料] → 上面的數字是模擬算的，不是實際載入結果；匯入後以 `JsonGameContent.Load` 通過為準，不符合就回來修正本文件。
- [NPC 怪的等級與 HP 是 0，其他程式可能拿來計算] → NPC 都有 `missionId` 且永不進入戰鬥分支；以 NPC 遭遇測試確認沒有呼叫戰鬥。
- [`Advance_SixHours_ReturnsSummaryQuickly` 偶爾因平行測試超時] → 既有的不穩定測試，失敗時單獨重跑並回報，不放寬門檻。
