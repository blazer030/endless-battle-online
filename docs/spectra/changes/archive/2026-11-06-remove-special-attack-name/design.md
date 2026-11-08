## Context

必殺技名稱來自原版的 `p_sat_name`、`m_sat_name`：暴擊時多顯示一段文字，沒有任何效果。本作目前玩家在建立角色時必填、可在設定修改，並一路存進 `characters`、`CombatantState` 與 `AttackEvent`。怪物沒有名稱，暴擊時戰報寫「重擊」，舞台也沒有字牌。

這個變更假設 `remove-champion` 已完成：冠軍的名稱（`ChampionSeat`、`champion.json`、`character_champions`）隨它一起消失，這裡不處理，`character_champions` 也已不存在。

討論 `wog45-content-import` 之後的補規則時，使用者決定不搬怪物的 406 種名稱（`data/monsters.json` 統計，含 `NULL`），並連玩家的設定一起拿掉，改用傷害數字呈現暴擊。

目前舞台的數字：普通傷害 22 號字紅色，暴擊 34 號字紅色加金邊，所以「把暴擊改紅」本身分不出差別，普通傷害要換色。

## Goals / Non-Goals

**Goals:**

- 玩家、怪物都沒有必殺技名稱，資料與畫面都不留痕跡。
- 暴擊只靠傷害數字（舞台）與「暴擊」兩字（文字戰報）呈現，玩家與怪物一致。
- 每個階段都能單獨 commit，測試保持全綠。

**Non-Goals:**

- 不改暴擊機率、倍率與傷害計算；同樣的亂數種子，事件的數值不變。
- 不新增取代普通攻擊的招式。
- 不動 `LEVEL UP`、暈眩、膠著等其他字牌；不動寵物的「會心一擊」文字。
- 不改寫匯入腳本樣板與技能原文裡的「必殺技機率」，那是原版對暴擊率的稱呼。

## Decisions

### 暴擊只靠傷害數字呈現

`stageEffectsFor` 對玩家攻擊不再產生字牌，只留刀光或光束、`damageNumber`（`critical: true`）與 `hitShake`；怪物本來就沒有字牌，維持不變。替代方案是保留固定的 `CRITICAL` 字牌，但使用者要的是用數字呈現，而且字牌只有玩家有，兩邊會不一致。

寵物攻擊的 `damageNumber` 已經帶 `event.critical`，會自動套用暴擊樣式；寵物代擋的數字固定 `critical: false`，維持不變。

### 普通傷害改成白色

視覺值（只做畫面確認，不寫樣式測試）：

| 項目 | 普通傷害 | 暴擊傷害 |
|---|---|---|
| 字級 | 22 | 34 |
| 顏色 | `Palette.white` | `Palette.crimson` |
| 外框 | 無 | `Palette.gold`，寬 3 |
| 出現動作 | 無 | 從 1.5 倍縮回 1 倍，`back.out` 緩動，`CriticalNumberPopSeconds` 0.18 秒 |

位移與淡出沿用 `DamageNumberSeconds`。新常數放 `frontend/src/motion/timings.ts`。替代方案是維持普通傷害紅色、暴擊改別的顏色，但使用者指定暴擊用紅色。

### 文字戰報統一寫暴擊

文字戰報沒有數字特效，所以要寫出字來。玩家：`暴擊！你攻擊{怪物}，造成 {傷害} 點傷害！`（魔法：`暴擊！你施放魔法，{怪物}受到 {傷害} 點傷害！`）；怪物：`{怪物}暴擊！你受到 {傷害} 點傷害！`。log kind 維持玩家 `trigger`、怪物 `normal`。

### 拿掉名稱的順序：戰鬥資料、設定、角色

相依關係決定順序：建立角色的請求是最後用到 `Character.SpecialAttackName` 的來源，所以角色最後。前端顯示先改，後端拿掉事件欄位時前端才不會壞。替代方案是一次全刪，但那會是單一個超大 commit，不符合小而頻繁的 commit 門檻。

### 用 migration 刪除角色的名稱欄位

`V000000018__remove_character_special_attack_name.sql`，內容是 `ALTER TABLE characters DROP COLUMN special_attack_name`（`V000000017` 由 `remove-champion` 使用）。流程是 migration → `docker compose up -d` → `./scripts/scaffold-db.sh`。名稱不另外備份：它沒有遊戲效果，專案也還沒有正式玩家資料。

### 怪物必殺技記為決定不搬

`gaps.md` 是「規則還不支援、之後要補」的清單，繼續列 406 種名稱會誤導後續工作。匯入腳本不再逐隻列出，並在怪物的對照方式寫一行決定。只有必殺技、沒有技能的怪物（例如 `monster-13` 白蛇龍）因此不再出現在「沒有搬的欄位」。原始資料保留在 `docs/research/wog45/data/monsters.json`。

## Implementation Contract

**硬規則對照：**

- 內容檔：不涉及。
- 資料表：只用上述新 migration，不改既有 migration。
- scaffold 產生檔：`Persistence/Models/*.cs` 與 `EndlessBattleDbContext.cs` 只由 `scripts/scaffold-db.sh` 重新產生。
- `frontend/src/api/schema.ts`：API 執行中用 `pnpm generate:api` 重新產生，不手改。
- `admin/src/api/schema.ts`：沒有這個欄位（已查），不用重新產生；admin 路由不涉及。

**資料形狀：**

- `POST /api/characters`：`name`、`sex`、`element`、`birth`、`jobId`、`allocation`。
- `PUT /api/characters/me/settings`：`attack` 與五個門檻。舊客戶端多送 `specialAttackName` 時直接忽略，不回錯誤。錯誤原因 `nameInvalid` 不再出現，`invalidPercent` 不變。
- `CharacterResponse` 與 `attack` 事件沒有 `specialAttackName`。

**驗收：** `tasks.md` 列出的測試全綠、`./scripts/check.sh` exit code 0，並在 375×667 的實際戰鬥中看過普通與暴擊數字。

**範圍內：** 上述資料、畫面、戰報與匯入清單。**範圍外：** Non-Goals 所列項目、已由 `remove-champion` 處理的冠軍內容，以及 `monster-skills`、`mission-objectives` 兩個後續變更。

## Risks / Trade-offs

- [玩家少了自訂招式名稱這個小趣味] → 使用者已明確決定，認為設定多餘。
- [放大動作時大數字可能超出舞台邊緣；估計 7 位數傷害 34 號字約 170 px，放大 1.5 倍約 255 px，尚未量測] → 畫面確認時用 7 位數傷害看 375×667；超出就調低放大倍率，這是畫面調整，不影響規格。
- [白色數字在亮色背景上看不清楚] → 畫面確認時檢查；舞台背景目前是深色系。
- [`CombatantState` 的位置參數變動牽動多數戰鬥測試（已查約 24 個測試檔）] → 這些測試的數值不變，只拿掉參數；任何數值斷言改變都代表改錯。
