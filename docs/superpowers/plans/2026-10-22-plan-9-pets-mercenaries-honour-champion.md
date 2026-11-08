# 計畫 9：寵物、傭兵、榮譽、冠軍 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 WOG 的寵物（捕捉、攜帶、餵食、飢餓、老化、參戰代擋＋恢復傷害）、傭兵（買下後在指定地區自己打的第二條離線時間軸，收益打折、次數制）、勳章與勳章所（勳章換道具或傭兵）、NPC 冠軍（500 金挑戰、贏了以自己的快照取代）搬進來；任務開放寵物獎勵與交寵物的每日任務，每日任務的獎勵恢復為勳章；前端新增「同伴」分頁、「勳章所」與「冠軍」。

**Architecture:** 新內容放 `backend/content/{mercenaries,honour-shop,champion}.json`，勳章與捕捉道具併入 `items.json`。Domain 新增 `Pets/`、`Mercenaries/`、`Honour/`、`Champion/`；寵物以 `PetCombatant` 進入 `WogBattle`（怪物命中時擲代擋、每回合末擲出手），捕捉在 `AdventureSimulator` 的勝場裡擲；傭兵由 `MercenarySimulator` 每 100 秒打一場，在 `AdventureService.AdvanceAsync` 裡先於角色自己的冒險結算，兩者的寫入同在一筆推進交易、角色版本更新最先。玩家狀態三張表：`character_pets`、`character_mercenaries`、`character_champions`（勳章就是背包道具，不另開表）。

**Tech Stack:** 沿用（.NET 10、EF Core Database-First、Flyway、xUnit v3、React 19、PixiJS 8、Vitest）。

**Spec:** `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §9（寵物／傭兵／榮譽商店／冠軍；任務的寵物獎勵）、§12 第 9 列、§2／§7.1（放置與離線結算）、§4（戰鬥）。研究依據 `docs/research/wog-research-world.md` §3～§5，並以 PHP 原始碼查證補充：`class/wog_act_pet.php`、`class/wog_fight_m.php`（`fight_pet_dm`、`guard`、`fight_count`、`get_exp`、`get_money`）、`class/wog_fight_select.php`（`fire` 的捕捉段、`fire_cp`、`fire_mercenary`）、`class/wog_fight_event.php`（`pet_stats`、`get_item` 的 `mercenary_topr`）、`class/wog_etc_peo.php`（飢餓）、`class/wog_act_mercenary.php`、`class/wog_etc_mercenary.php`、`class/wog_act_honor.php`、`class/wog_mission_tool.php`（`mission_pet_get`、交寵物）、`mission/wog_mission_222～225.php`、`wog_act_config.php:131-212`、`js/{wog_pet,wog_shop,wog_data,wog_fight,wog_chara}.js`、`language/*.php`（這幾個檔案實際是 UTF-8，不必 iconv）。

## Global Constraints

- 計畫 1～8 的 Global Constraints 全部適用。每個任務完成後都要經過 **test-runner**（獨立跑 `./scripts/check.sh`）與 **reviewer**。〔使用者要求〕
- **沒帶寵物、沒裝捕捉道具、沒有傭兵時，所有擲骰序列與計畫 8 完全相同**；既有的戰鬥、冒險、推進測試一律不改期望值。
- **寵物資料**（WOG `wog_pet`）：物種＝怪物 id（`pe_m_id`）、名字（預設怪物名，1～12 字）、個性 `PetPersonality` 1 積極 2 冷酷 3 鐵壁 4 危急（`pe_type`，名稱出自 `js/wog_pet.js` 的 `pet_type`）、`Attack`（AT／`pe_at`）、`Magic`（MT／`pe_mt`）、`Defense`（DEF／`pe_def`，也是代擋量）、`Spirit`（出擊值／`pe_fi`）、`Satiety`（飽腹／`pe_fu`）、`Intimacy`（親密度／`pe_he`）、`Carried`（攜帶＝`pe_st 0`、獸欄＝`pe_st 2`）、`BornAt`（`pe_b_dateline`）、`HungerSince`（`pe_dateline`：餵食與飢餓結算共用的計時起點）。**最多 3 隻**（攜帶＋獸欄，`PetRules.MaxPets = 3`）；**同時只攜帶 1 隻**，攜帶另一隻時原本攜帶的自動放回獸欄（WOG 是拒絕「無法同時攜帶兩隻寵物」〔Claude 的判斷〕）。放生＝刪除。
- **捕捉**（`wog_fight_select.php:273-310`、`pet_stats`）：捕捉道具放在**補給欄**（WOG 的 `d_item_id2`＝道具欄 2；spec 說的「副手」就是這一格〔裁定〕），新消耗品種類 `ConsumableKind.Capture`，`Amount` 是能力倍率：`capture-net` 捕獸網 ×1（WOG 258）、`capture-cage` 捕獸籠 ×8（1837）、`capture-crystal` 封印水晶 ×13（2236）。條件：**勝場**、**沒有攜帶寵物**、捕捉道具數量 > 0。擲骰順序（接在掉落之後、升級之前）：① `rand(1,40) == 1` → 道具損壞 −1；② 若還有道具：`rand(1,100) ≤ 12` → 若寵物數 < 3：道具 −1、`Attack`、`Magic`、`Defense` 依序各 `rand(1,10) × 倍率`、個性 `rand(1,4)`；寵物數已滿時照擲 ②、不建立寵物、不扣道具。道具因損壞歸零時不再擲 ②（PHP 會以 −1 個繼續捕捉〔Claude 的判斷〕）。新寵物：獸欄、`Spirit 1`、`Satiety 80`、`Intimacy 0`、`BornAt` = 擊殺時刻、`HungerSince` = 擊殺時刻 −20 秒（同 PHP `time-20`）。捕捉道具不會被當成補給品喝。傭兵戰鬥不捕捉（同 PHP）。
- **寵物參戰**（`wog_fight_m.php:503-525, 674-697, 405-445, 943-962`）：只有攜帶中的寵物；每場開打時建立 `PetCombatant`：個性加成 積極 `Attack×1.2`、冷酷 `Magic×1.2`、鐵壁 `Defense×1.2`（代擋量取 PHP round）、危急 `Spirit×1.5`；出擊率 `Activation = min(35, Intimacy/255×10 + Spirit'/255×10 + round(玩家 au / 怪物 au))`（au 用含裝備的有效值，怪物 au 為 0 時當 1；round 用 `DerivedStats.RoundLikePhp`）。**代擋**：怪物每次**命中**（未命中與暈眩不擲）且代擋量 > 0 時擲 `rand(1,100) < Activation` → 這一下全部由寵物承受（玩家 HP 不變）、代擋量 −傷害；代擋量 ≤ 0 → 寵物**退出這一場**（不再代擋、不再出手）。**出手**：每回合雙方都出手完、都沒倒下時，若寵物在場且 `rand(1,100) < Activation` → `rand(1,2)` 1 物理（`Attack` 對 df）／2 魔法（`Magic` 對 mdf）→ `rand(1,3) < 3` 命中、否則落空 → 暴擊比 `r = (玩家 au+agi)/(怪 au+agi)`（本回合套用技能修正後的值），`rand(1,100) < min(20, 10r)` 時倍率 `min(2, 1.2r)`、否則 1 → 傷害 `WogDamage.Fight(攻 × 倍率, 防, 1.0, random, bonusDamage: 1)`（PHP `fight(..., 1, 1, 1)`）。**WOG 在 FM:434 把寵物傷害歸零，這裡恢復**〔spec：Claude 的判斷〕。寵物傷害計入 `DamageDealtToMonster`，可以擊倒怪物（算玩家勝）。代擋量每場重置、不寫回（同 PHP）。冠軍挑戰沒有寵物；傭兵戰鬥帶攜帶中的寵物（同 PHP）。寵物不升級、不拿經驗。
- **餵食**（`wog_act_pet.php:106-286`）：食物是固定選單（同 WOG 寫死在程式裡，放 Domain 常數）：初級活力果實／香氣果實／甜味果實／野性果實 = `Attack`／`Magic`／`Defense`／`Spirit` +2、20 金、飽腹 +`rand(10,20)`；中級四種 +4、50 金、飽腹 +`rand(18,28)`；每次親密度 +`rand(2,6)`（先擲飽腹再擲親密度）；`HungerSince` = now。檢查依序、只回第一個成立的：`tooSoon`（`now − HungerSince < 10 秒`）、`full`（`Satiety ≥ 100`；餵完可以超過 100）、`overOwner`（餵完 `Attack > 主人 at`、`Magic > 主人 mat`、`Defense > max(主人 df, 主人 mdf)`；`Spirit` 不設上限；主人能力含裝備）、`notEnoughMoney`。VIP 果實 2319～2322 不做（商城道具）。
- **飢餓與老化**（`wog_etc_peo.php:20-38`、`wog_act_pet.php:300`）：每次推進（等同 WOG 的線上心跳）只檢查**攜帶中的寵物**：`now − HungerSince > 900 秒` → `Intimacy −1`、`Satiety −rand(8,12)`（下限 0）、`HungerSince = now`；**一次推進最多一次**（WOG 每次心跳最多一次，離線一整天回來也只扣一次）。歲數 `round((now − BornAt) / 864000 秒)`（PHP round），只顯示、不影響任何數值；自然死亡在 PHP 被註解掉，不做。
- **傭兵合約**（`wog_act_mercenary.php`、`js/wog_shop.js:498-590`）：同時一位（`mercenaryHired`，WOG「只能僱用一位傭兵」）；僱用扣價格、狀態休息中；`Price` 為 null 的傭兵只能在勳章所兌換（`honourOnly`）；名字 1～8 字（WOG 輸入框 maxlength 8）；出擊需指定地區（不存在 `placeUnknown`、禁區 `placeRestricted`），第一場在出擊時刻 +100 秒；停止＝回到休息中、尚未結算的場次不補；解除僱用＝刪除。
- **傭兵戰鬥**（`fire_mercenary`、`wog_fight_m.php:137,144`、`get_item`）：每 **100 秒**一場（WOG 前端 `mercenary_time = 100000` 毫秒輪詢）；用角色當下的能力（含裝備）**滿 HP／SP**、已裝備的技能、攜帶中的寵物；**不用補給、不耗行動力**（`f_down = 1`）、不影響角色的 HP／SP、不住宿；遇敵只看該地區自己的怪（**不含全域怪**、不分子地區；任務怪規則同計畫 8）。報酬：經驗、金錢先照一般公式算再 `MercenaryRules.Discount(值, 百分比) = round(值 × 百分比 / 100)`（PHP round）；職業熟練度全額（PHP `win_check` 不打折）；掉落分母 ×`(2 − 寶物% / 100)`（寶物% > 1 時；PHP `get_item`：寶物 30% 等於分母 ×1.7——WOG 介面說「打寶率為原來的 30%」與公式不符，照公式〔裁定〕，介面仍顯示 WOG 的欄位值）。勝場次數 −1 並計任務擊殺；敗北與 40 回合超限次數 −`DeathCost`（PHP 超限 `win=0` 一律算敗），超限仍給打折後的部分經驗。每場打完立刻套 `LevelUp.Apply`。次數 ≤ 0 → 合約刪除（WOG 系統信「戰鬥次數已滿,自動解除僱用」）。補算最多回溯 `AdventureSimulator.MaxSimulatedSeconds`（86 400 秒），更早的場次直接跳過。
- **傭兵結算位置**〔Claude 的設計〕：在 `AdvanceAsync` 的模擬階段**先**跑傭兵（從 `NextFightAt` 到 now），把經驗、金錢、升級、熟練度、任務進度套到角色上，**再**從這個角色跑自己的冒險；兩條時間軸各自依時間計數任務（每日任務以各自的擊殺時刻判斷過期）。寫入仍在同一筆交易、角色版本最先；掉落以「傭兵的在前、冒險的在後」一次放進背包（事件的丟棄旗標從尾端對齊，不受影響）。
- **勳章**（`js/wog_data.js:197-202`、`honor.php`）：新背包分類 `ItemCategory.Medal`（15 格、不能擴充、可堆疊 99）；五種：`continent-medal` 大陸勳章（WOG 1304）、`kingdom-medal` 王國勳章（1305）、`rainforest-medal` 雨林勳章（1306）、`royal-city-medal` 王城勳章（1307）、`silver-medal` 白銀勳章（2222）；`price 0`、`shopSellable false`。來源：每日任務（下列）、各地區 `meet 60` 的非全域怪加掉落該大區的勳章 `topr 60`（中央大陸→大陸、魔法王國→王國、熱帶雨林→雨林、末日王城→王城、禁區→白銀；WOG 資料遺失，研究稱來自 Boss 掉落〔Claude 的設計〕）。
- **勳章所**（`class/wog_act_honor.php`）：每筆兌換是「一件道具 × 數量」或「一位傭兵」，花費是一組勳章 × 數量；一次兌換一筆；原因依序 `honourUnknown`（單獨回傳）、`medalsMissing`、`mercenaryHired`（兌換傭兵時已有傭兵）、`bagFull`（付掉勳章後的背包放不下；PHP 的容量判斷用了未定義變數，這裡照背包規則）。勳章從背包扣，取用順序同任務交付（`Delivery.Plan`）。傭兵兌換後是休息中、次數為目錄值。WOG 的鑰匙類（h_type 2）等計畫 10 的禁區鑰匙；榮譽裝備（h_type 3～7）資料遺失、不做〔Claude 的簡化〕。
- **任務的寵物與勳章**：`PetReward(MonsterId, Level)` 開放——`Level` 是能力倍率（WOG `g_pet_lv`，`mission_pet_get` 的 `rand(1,10)×up`），新寵物同捕捉的初始值、放獸欄；完成時寵物數（交付後）已滿 → 400 `petsFull` 整筆不寫（PHP 不檢查上限〔Claude 的判斷〕）。每日任務新增 `dailyPets` 目標池（WOG 223／225 `mission_type 2`）：完成時**攜帶中的**寵物必須是該物種（PHP `pe_st=0 and pe_m_id`），否則 `petMissing`；交出的寵物刪除。每日任務的目標池 `dailyKills` 與 `dailyPets` 恰好一個非空，擲骰順序：目標池 → 報酬池。`daily-hunt-novice`／`daily-hunt-veteran` 的報酬池恢復為 WOG 222／224 的勳章（取代計畫 8 的金錢替代品）。
- **冠軍**（`fire_cp`、`wog_act_config.php:131,138`）：每個角色一個冠軍席位〔Claude 的設計；多人共用的冠軍是非同步 PvP，spec §1 非目標〕；沒有紀錄時是內容的 NPC 種子冠軍（滿血）。挑戰原因依序 `championLocked`（`now < LockedUntil`）、`notEnoughMoney`（< 500）、`noHp`（HP 0）；挑戰費 **500**，勝負都扣；沒有經驗、金錢、熟練度；不用補給、沒有寵物；玩家裝備的技能照常，冠軍沒有技能（怪物側沒有技能引擎〔Claude 的簡化〕）；戰後角色的 HP／SP 照戰鬥結果寫回。**勝**：席位換成挑戰者的快照（含裝備的有效能力、滿 HP／SP、`IsPlayer`、衛冕 0、`CrownedAt` now、`LockedUntil` now + 604 800 秒；PHP 把 7 天鎖在被推翻的冠軍主人身上，單人版被推翻的永遠是自己的席位〔裁定〕）。**敗或超限**：冠軍的 HP 留在戰後剩餘值（WOG 的消耗戰：`update wog_cp set hp=$cp_m_hp`）、衛冕 +1；WOG 給冠軍主人 70% 挑戰費，單人版沒有收款人，不做。可以挑戰自己過去的快照（WOG 以名字禁止挑戰自己；單人版禁止就等於只能打一次〔Claude 的設計〕）。
- **新事件**：`PetGuardEvent(AtSeconds, PetName, Damage, GuardAfter, Retired)` discriminator `"petGuard"`（取代那一下的怪物 `AttackEvent`）；`PetAttackEvent(AtSeconds, PetName, Kind, Hit, Critical, Damage, TargetHpAfter)` `"petAttack"`，時刻 `roundStart + SecondsPerRound × 0.95`；`CaptureEvent(AtSeconds, ItemName, ItemBroken, CapturedMonsterId?, Remaining)` `"capture"`（只在損壞或捕捉時產生，緊接在 `RewardEvent` 之後、任務進度之前）；`EncounterEvent` 加 `PetName?`、`PetMonsterId?`；`AdvanceSummary` 加 `PetsCaptured`；`AdvanceResponse` 加 `Mercenary?` 報告。
- **不做**：寵物拍賣、寵物競技（`pet_f_time` 無引用）、自訂寵物圖、特訓（被註解）、VIP 果實、寵物蛋（道具 case 10，計畫 10 的消耗品）、寵物技能 45～48（不在技能目錄）、`get_pet` 捕捉加成技能 84、英雄榜與英雄 NPC、排行榜、冠軍的 3% 額外攻擊 `p_cp_st`、傭兵戰報保留 1 小時的歷史表（只回傳本次推進的戰果）。
- **前端分頁**〔Claude 的判斷〕：底部第 6 個分頁「同伴」（順序 冒險／角色／技能／背包／任務／同伴），子頁「寵物」「傭兵」；勳章所是背包的子頁「勳章所」（WOG 的勳章所在商店選單裡）；冠軍是冒險頁地點標題旁的「冠軍」按鈕，打開對話框（同地圖面板的作法）。文案見 Task 5 與 Task 10。
- 程式碼註解中文、非必要不寫；commit 規則同前（英文、emoji、無 trailer）；生成檔只由 scaffold／`pnpm generate:api` 產生；migration 不得 DROP。

## Review Focus

- **代擋量打光的寵物**：一場中代擋量歸零 → 這一場之後的怪物命中全部打在玩家身上、寵物不再出手；下一場寵物以全額代擋量回到戰鬥。→ Task 3 `Battle_PetRetiresWhenGuardDepleted`、`Adventure_PetGuardResetsEachFight`。
- **已有 3 隻寵物時打贏**：捕捉照擲但不建立寵物、不扣道具（損壞仍會發生）；同一段推進中捕到第 3 隻後就不再增加。→ Task 4 `Adventure_CaptureSkipsWhenPetsFull`。
- **傭兵在離線補算中途用完次數**：只打到次數歸零的那一場，之後不再出擊、合約刪除、報告標示解除僱用；角色自己的冒險照常結算。→ Task 7 `Mercenary_StopsWhenFightsRunOut`、`Advance_RemovesMercenaryWhenFightsRunOut`。
- **推進途中放生寵物或解僱傭兵**：推進讀到的寵物或合約已被改動 → 推進 409，不寫入捕到的寵物、飢餓、合約次數。→ Task 7 `Advance_WritesNoCompanionState_WhenCharacterUpdateConflicts`。
- **寵物已滿時領寵物獎勵**：3 隻寵物時完成給寵物的任務 → 400 `petsFull`，金錢、經驗、道具、任務狀態、角色版本全部不變；交出 1 隻寵物的每日任務則以交出後的數量判斷。→ Task 8 `Complete_PetRewardRejectedWhenPetsFull`。

---

## File Structure

```
backend/
  content/items.json（勳章 5、捕捉道具 3）、monsters.json（meet 60 加勳章掉落）、missions.json（寵物獎勵、交寵物每日、勳章報酬）、
          mercenaries.json、honour-shop.json、champion.json（新）
  db/ddl/V000000009__add_companions.sql
  EndlessBattle.Domain/
    Items/{ItemCategory, ConsumableKind}.cs
    Pets/{Pet, PetBattle, PetCapture, PetCare, PetRules}.cs
    Mercenaries/{MercenaryDefinition, MercenaryContract, MercenaryRules, MercenarySimulator}.cs
    Honour/{HonourExchange, HonourShop}.cs
    Champion/{ChampionSeat, ChampionRules, ChampionDuel}.cs
    Battle/{WogBattle, BattleEvents}.cs、Adventure/{AdventureSimulator, AdventureRun, AdventureCompanions, DropRoller}.cs、Progression/RewardFormulas.cs
    Missions/{MissionDefinition, DailyMissions, MissionBoard, MissionRewards}.cs
  EndlessBattle.Application/
    Pets/{IPetRepository, PetService}.cs、Mercenaries/{IMercenaryRepository, MercenaryService}.cs、Honour/HonourService.cs、
    Champion/{IChampionRepository, ChampionService}.cs、Skills/EquippedSkills.cs、Adventure/{AdventureService, AdvanceResult}.cs、
    Missions/{MissionService, MissionCompletion}.cs、Inventory/InventoryService.cs、Content/IGameContent.cs、DependencyInjection.cs
  EndlessBattle.Infrastructure/Persistence/{PetRepository, MercenaryRepository, ChampionRepository}.cs、Content/JsonGameContent.cs、DependencyInjection.cs
  EndlessBattle.Api/Endpoints/{PetEndpoints, MercenaryEndpoints, HonourEndpoints, ChampionEndpoints, AdventureEndpoints, MissionEndpoints, Contracts}.cs、Program.cs
  EndlessBattle.Tests/TestContent/*、Support/{InMemoryPetRepository, InMemoryMercenaryRepository, InMemoryChampionRepository}.cs
docs/research/companion-table.md（新）、item-table.md、monster-roster.md、mission-table.md
frontend/src/
  api/{schema.ts, client.ts}；game/{types, inventory, battleLog, battlePlayback, stageModel, missions, pets, mercenaries, honour, champion}.ts
  render/{StageRenderer, stageLayout}.ts
  ui/{CompanionsScreen, PetsPage, MercenaryPage, HonourPage, ChampionPanel}.tsx（+css）、AdventureScreen.tsx、BagScreen.tsx、MissionsScreen.tsx
```

---

### Task 1: 同伴內容與定義

**Files:**
- Create: `backend/content/{mercenaries,honour-shop,champion}.json`、`Domain/Mercenaries/MercenaryDefinition.cs`、`Domain/Honour/HonourExchange.cs`、`Domain/Champion/ChampionSeat.cs`、`Tests/TestContent/{mercenaries,honour-shop,champion}.json`、`Tests/Content/CompanionTableTests.cs`、`docs/research/companion-table.md`
- Modify: `backend/content/{items,monsters}.json`、`Domain/Items/{ItemCategory, ConsumableKind}.cs`、`Application/Content/IGameContent.cs`、`Infrastructure/Content/JsonGameContent.cs`、`Tests/TestContent/items.json`、`docs/research/{item-table, monster-roster}.md`
- Test: `Tests/Infrastructure/JsonGameContentTests.cs`、`Tests/Content/CompanionTableTests.cs`、`Tests/Content/MonsterRosterTests.cs`、`Tests/Domain/Items/InventoryRulesTests.cs`

**Interfaces:**
- Consumes: `ItemDefinition`、`ItemQuantity`（`Domain.Missions`）、`CombatantState`
- Produces:

```csharp
public enum ItemCategory { Weapon, Head, Body, Hand, Foot, Accessory, Consumable, Stone, RefineStone, Material, Medal }      // Medal 加在最後
public enum ConsumableKind { None, Supply, StatPotion, StaminaPotion, BagExpansion, DepotExpansion, Capture }                // Capture：Amount = 能力倍率

namespace EndlessBattle.Domain.Mercenaries;
// Price 為 null：只能在勳章所兌換；百分比都是 1～100
public sealed record MercenaryDefinition(string Id, string Name, long? Price, int Fights, int MoneyPercent, int DropPercent, int ExpPercent, int DeathCost);

namespace EndlessBattle.Domain.Honour;
// ItemId 與 MercenaryId 恰好一個非 null；兌換傭兵時 Quantity 為 1；Cost 只能是勳章
public sealed record HonourExchange(string Id, string? ItemId, string? MercenaryId, int Quantity, IReadOnlyList<ItemQuantity> Cost);

namespace EndlessBattle.Domain.Champion;
public sealed record ChampionSeat(string Name, int Level, int Element, string SpecialAttackName, int Hp, int HpMax, int Sp, int SpMax,
    int At, int Mat, int Df, int Mdf, int Agi, int Au, bool IsPlayer = false, int Defenses = 0, DateTimeOffset? CrownedAt = null, DateTimeOffset? LockedUntil = null)
{
    public CombatantState ToCombatant();   // Str／Smart 填 0、SpecialAttackName 帶過去
}

// IGameContent 新增（內容檔順序）
IReadOnlyList<MercenaryDefinition> AllMercenaries { get; }
MercenaryDefinition GetMercenary(string mercenaryId);           // 找不到丟 GameContentException
IReadOnlyList<HonourExchange> HonourShop { get; }
ChampionSeat SeedChampion { get; }                              // Hp = HpMax、Sp = SpMax
```

JSON：`mercenaries.json` `{ "mercenaries": [{ id, name, price, fights, moneyPercent, dropPercent, expPercent, deathCost }] }`；`honour-shop.json` `{ "exchanges": [{ id, itemId | mercenaryId, quantity, cost: [{ itemId, quantity }] }] }`；`champion.json` `{ "champion": { name, level, element, specialAttackName, hp, sp, at, mat, df, mdf, agi, au } }`。

**載入驗證**（`GameContentException`，訊息含 id）：Capture 道具分類必須是 Consumable、`amount ≥ 1`、不得有 supplyHp／supplySp／bonus（`usesAmount` 加上 Capture）；Medal 分類 `consumable None`、非裝備規則同材料。傭兵：id 不重複且 ≤ 32、name 1～8 字、price 為 null 或 ≥ 1、fights ≥ 1、三個百分比 1～100、deathCost ≥ 1。勳章所：id 不重複且 ≤ 32、itemId／mercenaryId 恰好一個且存在、quantity 1～99（傭兵必須 1）、cost 至少一項、每項道具存在且分類是 Medal、數量 1～99、不重複。冠軍：name 1～12 字、level ≥ 1、hp ≥ 1、sp ≥ 0、能力 ≥ 0、element 0～6。三個檔案都是必要內容檔（測試 helper 寫空檔的作法同計畫 8 的 missions.json；冠軍必須有一筆）。

**內容（WOG 資料遺失，Claude 的設計；F 級列出自 WOG 傭兵所說明文字）：**

道具：勳章五種（見 Global Constraints，`shopLevel 1`、`price 0`、`shopSellable false`）；捕捉道具 `capture-net` 捕獸網（倍率 1、200 金、商店 1 級、販售）、`capture-cage` 捕獸籠（8、3000、4 級、販售）、`capture-crystal` 封印水晶（13、5000、6 級、不販售，勳章所兌換）。怪物：每隻 `meet 60` 且 `placeId ≠ 0` 的怪（含 3 隻任務怪）在 `drops` 最後加上所屬大區的勳章 `topr 60`。

| 傭兵 id | 名稱 | 價格 | 戰鬥次數 | 金錢% | 寶物% | 經驗% | 死亡消耗 |
|---|---|---|---|---|---|---|---|
| `mercenary-f` | F級傭兵 | 3000 | 100 | 30 | 30 | 65 | 5 |
| `mercenary-e` | E級傭兵 | 12000 | 150 | 40 | 40 | 70 | 4 |
| `mercenary-d` | D級傭兵 | 40000 | 200 | 50 | 50 | 75 | 3 |
| `mercenary-c` | C級傭兵 | 120000 | 300 | 60 | 60 | 80 | 2 |
| `mercenary-b` | B級傭兵 | null | 400 | 70 | 70 | 85 | 1 |
| `mercenary-a` | A級傭兵 | null | 500 | 80 | 80 | 90 | 1 |

勳章所（`大`＝大陸、`國`＝王國、`雨`＝雨林、`城`＝王城、`銀`＝白銀；數量都是 1）：`honour-mercenary-b` B級傭兵 ← 大3 國3 雨3 城3；`honour-mercenary-a` A級傭兵 ← 銀5 大3 國3 雨3 城3；`honour-capture-crystal` 封印水晶 ← 銀1 雨2；`honour-attack-stone` 攻擊魔石 ← 大2；`honour-sorcery-stone` 魔力魔石 ← 國2；`honour-guard-stone` 守護魔石 ← 雨2；`honour-ward-stone` 結界魔石 ← 城2；`honour-vitality-stone` 生命魔石 ← 大1 雨1；`honour-gale-stone` 疾風魔石 ← 國1 城1；`honour-chaos-stone` 混沌魔石 ← 銀2；`honour-prism-stone` 虹彩魔石 ← 銀2；`honour-iron-refine-5`／`honour-arcane-refine-5`／`honour-vigor-refine-5` 鋼鐵／秘法／活力精鍊石 Lv5 ← 大1 國1 雨1 城1。

冠軍種子（依戰士 Lv30 期望值＋商店 4 級裝備估算）：王都冠軍 格雷德、Lv30、屬性 1、必殺「王者之劍」、hp 1400、sp 175、at 180、mat 25、df 150、mdf 40、agi 30、au 25。

測試內容：`items.json` 加 `test-medal-a`、`test-medal-b`（Medal）、`test-capture-net`（Capture、amount 2、price 50、shopSellable true）；`mercenaries.json`：`test-mercenary`（price 100、fights 3、50／100／50、deathCost 2）、`test-honour-mercenary`（price null、fights 5、100／100／100、deathCost 1）；`honour-shop.json`：`test-exchange-ore`（`test-ore` ×1 ← `test-medal-a` ×2）、`test-exchange-mercenary`（`test-honour-mercenary` ← `test-medal-a` ×1、`test-medal-b` ×1）；`champion.json`：測試冠軍、Lv5、element 0、必殺「測試劍」、hp 120、sp 20、at 20、mat 5、df 8、mdf 5、agi 10、au 6。

- [ ] **Step 1: 寫失敗測試**（`JsonGameContentTests`）：`Load_TestContent_ExposesCompanionContent`（兩位傭兵、兩筆兌換、種子冠軍 `Assert.Equivalent(..., strict: true)`、`GetMercenary("test-mercenary").Fights == 3`、`GetItem("test-capture-net").Consumable == Capture`、`test-medal-a` 分類 Medal）、`Items_RejectCaptureWithoutAmount`、`Mercenaries_RejectDuplicateId`、`Mercenaries_RejectPercentOutOfRange`（0 與 101）、`HonourShop_RejectNonMedalCost`、`HonourShop_RejectBothItemAndMercenary`、`HonourShop_RejectMercenaryQuantityOtherThanOne`、`Champion_RejectZeroHp`；`InventoryRulesTests.Capacity_MedalIgnoresBagBonus`（`Capacity(Medal, 5) == 15`）。
- [ ] **Step 2: 寫失敗測試**（`CompanionTableTests`，真實內容）：`Medals_AreTheFiveWogMedals`（id、名稱、順序同上）、`CaptureItems_UseWogMultipliers`（1、8、13）、`Mercenaries_MatchTable`（六列逐欄）、`HonourShop_CostsOnlyMedals`（14 筆、每筆花費都是勳章、B／A 級傭兵只能從這裡取得）、`Champion_SeedMatchesTable`；`MonsterRosterTests.Roster_EliteMonstersDropTheirAreaMedal`（每隻 meet 60 非全域怪最後一項掉落是所屬大區勳章、topr 60；其他怪沒有勳章）。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*JsonGameContentTests" --filter-class "*CompanionTableTests" --filter-class "*MonsterRosterTests" --filter-class "*InventoryRulesTests"` → 編譯失敗或斷言失敗。
- [ ] **Step 4: 實作**：兩個 enum 值、三個 record、`JsonGameContent` 讀三個檔與上列驗證（勳章所在道具、傭兵之後驗證）、`IGameContent` 新成員；寫內容與測試內容；`docs/research/companion-table.md` 列捕捉道具、餵食選單、傭兵、勳章所、冠軍種子、勳章掉落，並加「與研究報告的差異」一節（捕捉道具在道具欄 2 而非副手；傭兵寶物% 是分母 ×(2−g/100) 的懲罰而非加成；傭兵滿血、不用補給、不遇全域怪、熟練度全額、超限算敗；飢餓只扣攜帶中的寵物、每次心跳最多一次；DEF 上限取 df／mdf 較大者、出擊值無上限；冠軍 7 天鎖在被推翻的主人身上；任務寵物獎勵 PHP 不檢查 3 隻上限；交寵物要攜帶中的；勳章名稱出自 `js/wog_data.js`）；`item-table.md` 補勳章與捕捉道具、`monster-roster.md` 補勳章掉落規則。
- [ ] **Step 5: 跑測試確認通過**：同 Step 3 → PASS；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit**

```bash
git add backend/content backend/EndlessBattle.Domain backend/EndlessBattle.Application backend/EndlessBattle.Infrastructure backend/EndlessBattle.Tests docs/research
git commit -m "✨ feat: load medals, capture items, mercenaries, the honour shop and the champion seed"
```

---

### Task 2: 同伴資料表與 repository

**Files:**
- Create: `backend/db/ddl/V000000009__add_companions.sql`、`Domain/Pets/Pet.cs`、`Domain/Mercenaries/MercenaryContract.cs`、`Application/Pets/IPetRepository.cs`、`Application/Mercenaries/IMercenaryRepository.cs`、`Application/Champion/IChampionRepository.cs`、`Infrastructure/Persistence/{PetRepository, MercenaryRepository, ChampionRepository}.cs`、`Tests/Support/{InMemoryPetRepository, InMemoryMercenaryRepository, InMemoryChampionRepository}.cs`
- Modify: scaffold 產物（`./scripts/scaffold-db.sh`）、`Infrastructure/DependencyInjection.cs`
- Test: `Tests/Infrastructure/CompanionRepositoryTests.cs`、`Tests/Infrastructure/DatabaseSchemaTests.cs`

```sql
CREATE TABLE character_pets (
    id           BIGINT      NOT NULL AUTO_INCREMENT,
    character_id BIGINT      NOT NULL,
    monster_id   VARCHAR(32) NOT NULL,             -- 物種（WOG pe_m_id）
    name         VARCHAR(12) NOT NULL,
    personality  TINYINT     NOT NULL,             -- PetPersonality：1 積極 2 冷酷 3 鐵壁 4 危急
    attack       INT         NOT NULL,             -- pe_at
    magic        INT         NOT NULL,             -- pe_mt
    defense      INT         NOT NULL,             -- pe_def，也是代擋量
    spirit       INT         NOT NULL,             -- pe_fi 出擊值
    satiety      INT         NOT NULL,             -- pe_fu 飽腹
    intimacy     INT         NOT NULL,             -- pe_he 親密度
    carried      BOOLEAN     NOT NULL,             -- 攜帶（pe_st 0）或獸欄（pe_st 2）
    born_at      DATETIME(6) NOT NULL,             -- 算歲數
    hunger_since DATETIME(6) NOT NULL,             -- 餵食與飢餓共用的計時起點（pe_dateline）
    PRIMARY KEY (id),
    KEY ix_character_pets_character (character_id),
    CONSTRAINT fk_character_pets_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_mercenaries (
    character_id  BIGINT      NOT NULL,
    mercenary_id  VARCHAR(32) NOT NULL,
    name          VARCHAR(8)  NOT NULL,
    remaining     INT         NOT NULL,            -- 剩餘戰鬥次數（me_count）
    place_id      INT         NULL,                -- 出擊地區；NULL＝休息中
    next_fight_at DATETIME(6) NULL,                -- 下一場的時刻；休息中為 NULL
    hired_at      DATETIME(6) NOT NULL,
    PRIMARY KEY (character_id),
    CONSTRAINT fk_character_mercenaries_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE character_champions (
    character_id        BIGINT      NOT NULL,      -- 沒有這一列＝內容的種子冠軍
    name                VARCHAR(12) NOT NULL,
    level               INT         NOT NULL,
    element             TINYINT     NOT NULL,
    special_attack_name VARCHAR(16) NOT NULL,
    hp INT NOT NULL, hp_max INT NOT NULL, sp INT NOT NULL, sp_max INT NOT NULL,
    at INT NOT NULL, mat INT NOT NULL, df INT NOT NULL, mdf INT NOT NULL, agi INT NOT NULL, au INT NOT NULL,
    is_player           BOOLEAN     NOT NULL,
    defenses            INT         NOT NULL,      -- 衛冕次數
    crowned_at          DATETIME(6) NULL,
    locked_until        DATETIME(6) NULL,          -- 這之前不能挑戰
    PRIMARY KEY (character_id),
    CONSTRAINT fk_character_champions_character FOREIGN KEY (character_id) REFERENCES characters (id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;
```

**Interfaces:**
- Consumes: Task 1 `ChampionSeat`
- Produces:

```csharp
namespace EndlessBattle.Domain.Pets;
public enum PetPersonality { Aggressive = 1, Cold = 2, Ironwall = 3, Desperate = 4 }
public sealed record Pet(long Id, string MonsterId, string Name, PetPersonality Personality, int Attack, int Magic, int Defense,
    int Spirit, int Satiety, int Intimacy, bool Carried, DateTimeOffset BornAt, DateTimeOffset HungerSince);

namespace EndlessBattle.Domain.Mercenaries;
// PlaceId 與 NextFightAt 同時有值＝出擊中
public sealed record MercenaryContract(string MercenaryId, string Name, int Remaining, int? PlaceId, DateTimeOffset? NextFightAt, DateTimeOffset HiredAt)
{
    public bool Fighting => PlaceId is not null && NextFightAt is not null;
}

namespace EndlessBattle.Application.Pets;
public interface IPetRepository
{
    Task<IReadOnlyList<Pet>> ListAsync(long characterId, CancellationToken cancellationToken);   // Id 升冪
    Task<long> AddAsync(long characterId, Pet pet, CancellationToken cancellationToken);         // 忽略 pet.Id，回傳新 Id
    Task UpdateAsync(long characterId, Pet pet, CancellationToken cancellationToken);            // 依 Id；別的角色的寵物不動
    Task RemoveAsync(long characterId, long petId, CancellationToken cancellationToken);
}
namespace EndlessBattle.Application.Mercenaries;
public interface IMercenaryRepository
{
    Task<MercenaryContract?> FindAsync(long characterId, CancellationToken cancellationToken);
    Task AddAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken);   // 已有一列 → ConflictException
    Task UpdateAsync(long characterId, MercenaryContract contract, CancellationToken cancellationToken);
    Task RemoveAsync(long characterId, CancellationToken cancellationToken);
}
namespace EndlessBattle.Application.Champion;
public interface IChampionRepository
{
    Task<ChampionSeat?> FindAsync(long characterId, CancellationToken cancellationToken);        // null＝種子冠軍
    Task SaveAsync(long characterId, ChampionSeat seat, CancellationToken cancellationToken);    // 新增或覆寫
}
// 測試替身 InMemory*Repository(List<string>? log = null) : I…Repository, IRestorableRepository；
// 寫入記 "pet:add|update|remove:{id}"、"mercenary:add|update|remove"、"champion:save"
```

- [ ] **Step 1: 寫失敗測試**：`Schema_HasCompanionTables`（三張表的欄位與型別、`character_mercenaries` 與 `character_champions` 以 `character_id` 為主鍵）、`Pets_RoundTrip`（個性、布林、`HungerSince` 微秒精度回讀一致）、`Pets_UpdateAndRemoveScopedToCharacter`、`Mercenary_RoundTripAndRejectSecond`（第二次 `AddAsync` → `ConflictException`；`Fighting` 依兩個欄位）、`Champion_SaveUpserts`（存兩次只剩一列、讀回第二次的值）。
- [ ] **Step 2: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*CompanionRepositoryTests" --filter-class "*DatabaseSchemaTests"` → FAIL。
- [ ] **Step 3: 實作**：新增 migration → `docker compose up -d` → `./scripts/scaffold-db.sh`；三個 repository（重複鍵用 `MySqlErrors` 轉 `ConflictException`）、DI 註冊、三個 in-memory 替身。
- [ ] **Step 4: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 5: Commit** `🚧 chore: add the pets, mercenaries and champion tables`

---

### Task 3: 寵物參戰（代擋與恢復傷害）

**Files:**
- Create: `Domain/Pets/PetBattle.cs`、`Domain/Adventure/AdventureCompanions.cs`
- Modify: `Domain/Battle/{WogBattle, BattleEvents}.cs`、`Domain/Adventure/AdventureSimulator.cs`、`Application/Adventure/AdventureService.cs`（建構子加 `IPetRepository pets`）
- Test: `Tests/Domain/Pets/PetBattleTests.cs`、`Tests/Domain/Battle/WogBattlePetTests.cs`、`Tests/Domain/Adventure/AdventureSimulatorTests.cs`、`Tests/Application/AdventureServiceTests.cs`

**Interfaces:**
- Consumes: Task 2 `Pet`、`IPetRepository.ListAsync`
- Produces:

```csharp
namespace EndlessBattle.Domain.Pets;
// 個性加成已套用；Guard 是這一場的代擋量
public sealed record PetCombatant(string Name, double Attack, double Magic, int Guard, double Activation);
public static class PetBattle
{
    public const double MaxActivation = 35;
    public static PetCombatant ToCombatant(Pet pet, CombatantState player, CombatantState monster);
}
// WogBattle 新多載（原多載轉呼叫 pet: null）
public static BattleOutcome Run(CombatantState player, CombatantState monster, AttackKind? playerPreferred,
    IReadOnlyList<EquippedSkill> skills, SupplyState? supply, IRandomSource random, PetCombatant? pet);
public sealed record PetGuardEvent(double AtSeconds, string PetName, int Damage, int GuardAfter, bool Retired) : BattleEvent(AtSeconds);       // "petGuard"
public sealed record PetAttackEvent(double AtSeconds, string PetName, AttackKind Kind, bool Hit, bool Critical, int Damage, int TargetHpAfter) : BattleEvent(AtSeconds); // "petAttack"
// EncounterEvent(..., bool StaminaDepleted, string? PetName = null, string? PetMonsterId = null)

namespace EndlessBattle.Domain.Adventure;
// Task 4 再加 PetCount 與 Capture
public sealed record AdventureCompanions(Pet? CarriedPet = null);
// AdventureSimulator.Run(..., IReadOnlyList<ActiveMission>? missions = null, AdventureCompanions? companions = null)
```

`WogBattle`：`GuardAfter = max(0, 代擋量)`；`PetGuardEvent` 用在被代擋的那一下（時刻同原本的怪物攻擊），不另外輸出 `AttackEvent`；寵物出手的擲骰與公式見 Global Constraints，時刻 `roundStart + SecondsPerRound × 0.95`，落空時 `Damage 0`、`Critical false`；寵物擊倒怪物時接 `DefeatEvent(Monster)`、勝方玩家。模擬器每場以 `PetBattle.ToCombatant(companions.CarriedPet, player, CombatantState.From(definition))` 建立寵物（每場重置），`EncounterEvent` 帶寵物名字與物種。`AdventureService`：模擬前 `pets.ListAsync`，攜帶中的那隻（`Carried`）放進 `AdventureCompanions`。

- [ ] **Step 1: 寫失敗測試**（`PetBattleTests`、`WogBattlePetTests`；腳本亂數用 `ScriptedRandom`）：
  - `Activation_FollowsWogFormula`：Intimacy 255、Spirit 255、積極、玩家 au 30、怪 au 10 → 23；危急 → 28；玩家 au 300 → 35（上限）；怪 au 0 → 視為 1。
  - `Personality_AppliesWogBonuses`（Theory）：積極 Attack ×1.2、冷酷 Magic ×1.2、鐵壁 Guard = round(Defense ×1.2)、危急只影響 Activation。
  - `Battle_PetGuardAbsorbsHit`：怪物命中、代擋擲骰 < Activation → `PetGuardEvent`（Damage = 那一下的傷害、GuardAfter = 代擋量 − 傷害）、玩家 HP 不變、沒有該下的怪物 `AttackEvent`。
  - `Battle_PetRetiresWhenGuardDepleted`：代擋量 10、怪物一下 15 → `Retired true`、`GuardAfter 0`；之後的怪物命中即使代擋擲骰成功也打在玩家身上，之後沒有任何 `PetAttackEvent`。
  - `Battle_PetAttackDealsRestoredDamage`：寵物 Attack 30、物理、命中、未暴擊、`WogDamage` 浮動擲 1、對測試雪精（df 6）→ `Damage == 146`、怪物 HP 減少 146。
  - `Battle_PetMissesOneInThree`：命中擲骰 `rand(1,3) == 3` → `Hit false`、`Damage 0`。
  - `Battle_PetCriticalUsesAuAgiRatio`：玩家 au 10＋agi 12、怪 au 6＋agi 9 → 暴擊時倍率 1.76（以傷害反推或檢查 `Critical true` 且傷害等於 `Fight(30 × 1.76, …)`）。
  - `Battle_PetKillWinsTheFight`：怪物剩 5 HP 時寵物命中 → `DefeatEvent(Monster)`、`Winner == Player`、`DamageDealtToMonster` 含寵物傷害。
  - `Battle_WithoutPetKeepsRandomSequence`：同一組腳本以 `pet: null` 跑新多載與舊多載 → 事件 `Assert.Equivalent`、`Consumed` 相同。
- [ ] **Step 2: 寫失敗測試**（模擬器／服務）：`Adventure_CarriedPetJoinsEveryFight`（攜帶 Intimacy 255、Spirit 255 的寵物、60 秒 → 每個 `EncounterEvent.PetName` 都是寵物名、至少一個 `PetGuardEvent` 與一個 `PetAttackEvent`）、`Adventure_PetGuardResetsEachFight`（代擋量 1 的寵物在第一場退場後，第二場仍出現並能再代擋一次）、`Advance_UsesCarriedPetOnly`（兩隻寵物、只有一隻攜帶 → 遭遇事件帶的是攜帶那隻；沒有攜帶 → `PetName null`、事件與沒有寵物時相同）。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*PetBattleTests" --filter-class "*WogBattlePetTests" --filter-class "*AdventureSimulatorTests" --filter-class "*AdventureServiceTests"` → 編譯失敗。
- [ ] **Step 4: 實作** 上列型別與修改（測試的 `InMemoryUnitOfWork` 一併登記 `InMemoryPetRepository`）。
- [ ] **Step 5: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit** `✨ feat: let the carried pet guard and strike in battle`

---

### Task 4: 冒險中的捕捉

**Files:**
- Create: `Domain/Pets/PetCapture.cs`
- Modify: `Domain/Adventure/{AdventureSimulator, AdventureRun, AdventureCompanions}.cs`、`Domain/Battle/BattleEvents.cs`、`Application/Adventure/AdventureService.cs`、`Application/Inventory/InventoryService.cs`
- Test: `Tests/Domain/Pets/PetCaptureTests.cs`、`Tests/Domain/Adventure/AdventureSimulatorTests.cs`、`Tests/Application/{AdventureServiceTests, InventoryServiceTests}.cs`

**Interfaces:**
- Consumes: Task 1 `ConsumableKind.Capture`、Task 2 `Pet`／`IPetRepository.AddAsync`、Task 3 `AdventureCompanions`
- Produces:

```csharp
namespace EndlessBattle.Domain.Pets;
public sealed record CaptureState(string ItemId, string Name, int Multiplier, int Quantity);     // 補給欄的捕捉道具
public sealed record CapturedPet(string MonsterId, string Name, PetPersonality Personality, int Attack, int Magic, int Defense, DateTimeOffset CapturedAt)
{
    public Pet ToPet();   // Id 0、Spirit 1、Satiety 80、Intimacy 0、獸欄、BornAt = CapturedAt、HungerSince = CapturedAt − 20 秒
}
public sealed record CaptureRoll(CaptureState Capture, bool ItemBroken, CapturedPet? Captured);
public static class PetCapture
{
    public const int BreakOneIn = 40, ChancePercent = 12, StatRollMax = 10;
    // 勝場且沒有攜帶寵物時才呼叫；擲骰順序見 Global Constraints
    public static CaptureRoll Roll(CaptureState capture, MonsterDefinition monster, int petCount, DateTimeOffset at, IRandomSource random);
    // 能力 rand(1,10) × multiplier（Attack、Magic、Defense 依序）、個性 rand(1,4)；任務的寵物獎勵也用這個
    public static CapturedPet NewPet(MonsterDefinition monster, int multiplier, DateTimeOffset at, IRandomSource random);
}
public sealed record CaptureEvent(double AtSeconds, string ItemName, bool ItemBroken, string? CapturedMonsterId, int Remaining) : BattleEvent(AtSeconds);  // "capture"

public sealed record AdventureCompanions(Pet? CarriedPet = null, int PetCount = 0, CaptureState? Capture = null);
// AdventureRun(..., IReadOnlyList<ActiveMission>? Missions = null, IReadOnlyList<CapturedPet>? Captured = null)；
// SupplyRemaining 改為「補給欄剩餘數量（補給品或捕捉道具）」
// AdvanceSummary(..., int MissionKills = 0, int PetsCaptured = 0)
```

`AdventureService`：補給欄是 Supply → `SupplyState`（同前）；是 Capture → `CaptureState(item.Id, item.Name, item.Amount, 數量)`、`SupplyState` 為 null；`PetCount = 全部寵物數`。交易寫入順序：角色 → 熟練度 → 任務進度 → 掉落 → 補給欄 → **捕到的寵物**（`pets.AddAsync(captured.ToPet())`，依捕捉順序）。`InventoryService.EquipAsync`：`ConsumableKind.Capture` 和 Supply 一樣走補給欄（可指定數量）。

- [ ] **Step 1: 寫失敗測試**（`PetCaptureTests`）：
  - `Roll_BreaksOneInForty`：`rand(1,40) == 1` → `ItemBroken`、數量 −1，接著照擲捕捉。
  - `Roll_CapturesAtTwelvePercent`：捕捉擲 12 → 捕到、數量再 −1；擲 13 → 沒捕到、數量不變。
  - `NewPet_RollsStatsTimesMultiplier`：倍率 8、擲 3、10、1、個性 4 → Attack 24、Magic 80、Defense 8、`Desperate`；`ToPet()` 的初始值同 Global Constraints。
  - `Roll_SkipsWhenPetsFull`：`petCount 3`、捕捉擲 1 → `Captured null`、數量不變、只消耗兩個亂數（損壞、捕捉）。
  - `Roll_StopsAfterLastItemBreaks`：數量 1、損壞 → 數量 0、`Captured null`、只消耗一個亂數。
- [ ] **Step 2: 寫失敗測試**（模擬器／服務）：
  - `Adventure_CapturesOnWinsOnly`：沿用敗北與超限的設定裝捕捉道具 → 沒有 `CaptureEvent`、數量不變。
  - `Adventure_NoCaptureWhileCarryingPet`：有攜帶寵物 → 不擲捕捉（事件與沒裝捕捉道具時相同）。
  - `Adventure_CaptureSkipsWhenPetsFull`：`PetCount 2`、捕捉必中的腳本、長時間 → `Captured.Count == 1`；`PetCount 3` → 0 且 `SupplyRemaining` 只因損壞減少。
  - `Adventure_CaptureEventFollowsReward`：`CaptureEvent` 緊接在同一 `AtSeconds` 的 `RewardEvent` 之後、`MissionProgressEvent` 之前；`Summary.PetsCaptured` 等於捕到的數量。
  - `Advance_PersistsCapturedPetsAndItemCount`：補給欄裝 `test-capture-net` ×5、固定種子 → 寵物表多出獸欄寵物（Satiety 80、Intimacy 0、Spirit 1、名字＝怪物名）、補給欄數量減少；log 中 `pet:add` 在補給欄寫入之後。
  - `Advance_NeverDrinksCaptureItem`：HP 低於 15% 也沒有 `PotionEvent`。
  - `Equip_CaptureItemGoesToSupplySlot`：裝備 `test-capture-net` ×3 → 補給欄數量 3，背包剩餘正確。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*PetCaptureTests" --filter-class "*AdventureSimulatorTests" --filter-class "*AdventureServiceTests" --filter-class "*InventoryServiceTests"` → FAIL。
- [ ] **Step 4: 實作** 上列型別與修改。
- [ ] **Step 5: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit** `✨ feat: capture pets with a capture item in the supply slot`

---

### Task 5: 前端：戰報與舞台上的寵物、捕捉

**Files:**
- Modify: `frontend/src/api/schema.ts`（`pnpm generate:api` 產生）、`src/game/{types, battleLog, battlePlayback, stageModel}.ts`、`src/render/{StageRenderer, stageLayout}.ts`
- Test: `src/game/{battleLog, battlePlayback, stageModel}.test.ts`

**Interfaces:**
- Consumes: Task 3、4 的 `petGuard`、`petAttack`、`capture` 事件、`EncounterEvent.petName`／`petMonsterId`、`summary.petsCaptured`
- Produces: `types.ts` 加 `PetGuardEvent`、`PetAttackEvent`、`CaptureEvent`（`Required<Schemas[...]>`）併入 `BattleEvent`；`StageState` 加 `petName: string | null`、`petMonsterId: string | null`、`petActive: boolean`；`StageSide` 加 `"pet"`；`StageEffect` 加 `{ type: "petRetire" }`；`PlaybackItem` 的 summary 加 `petsCaptured: number`。

**文案：**
- `petGuard`：`{寵物名}替你擋下了 {damage} 點傷害。`；`retired` 時再加一行 `{寵物名} 戰敗，退出了戰鬥。`（kind `muted`，WOG `pet_def_date` 的「戰敗」）。
- `petAttack`：命中 `{寵物名}發動攻擊，對{怪物名}造成 {damage} 點傷害。`；暴擊 `{寵物名}會心一擊！對{怪物名}造成 {damage} 點傷害。`；落空 `{寵物名}{flavor}`，flavor 依 `Math.floor(event.atSeconds) % 4` 取 WOG `pet_miss_date` 的 `[" 沒有擊中目標", " 發呆", " 東張西望", " 失去鬥志"]`（kind `muted`）。
- `capture`：損壞 `{道具名}受到損壞，數量減少1個！！`（muted）；捕到 `捕捉到 {怪物名}，{道具名}數量減少1個！！`（trigger）；兩者都有時兩行，損壞在前（WOG `pet_break`／`pet_get` 原文）。
- 摘要：在任務進度之後、升級之前加 `，捕捉寵物 {n} 隻`（只在 > 0 時）。
- 舞台：encounter 設定寵物三欄（沒有寵物時 null／false）；`petGuard` → `[{damageNumber side "pet"}, {hitShake side "pet"}]`，退場再加 `{ type: "petRetire" }` 並讓 `applyToStage` 設 `petActive false`；`petAttack` 命中 → `[{damageNumber side "monster", critical}, {hitShake side "monster"}]`、落空 → `[{miss side "monster"}]`，`applyToStage` 更新 `monsterHp = targetHpAfter`；`capture` 捕到 → `[{ type: "plate", text: "捕捉成功", tone: "gold" }]`。`stageLayout` 加 `PetBox = { left: 112, width: 60, height: 84 }`；`StageRenderer.showEncounter` 在 `petActive` 時於玩家右前方顯示寵物（有該物種的美術圖用圖，否則小號橫排名牌），`petRetire` 淡出。

- [ ] **Step 1: 寫失敗測試**：`battleLog` — `describes a pet guard`、`adds the retire line when the guard breaks`、`describes pet attacks, criticals and misses`（落空 flavor 依秒數取）、`describes capture break and success in order`、`summary mentions captured pets`；`battlePlayback` — summary 帶 `petsCaptured`；`stageModel` — encounter 帶入寵物、`petGuard` 退場時 `petActive false` 並產生 `petRetire`、`petAttack` 改怪物 HP 並產生 monster 側傷害數字、捕到時金色「捕捉成功」名牌。
- [ ] **Step 2: 跑測試確認失敗**：`cd frontend && pnpm vitest run src/game` → FAIL。
- [ ] **Step 3: 實作**：API 執行中 `pnpm generate:api`；補 `types.ts` 聯集與各 switch 的三個 case；`stageLayout`／`StageRenderer` 的寵物位置與效果。
- [ ] **Step 4: 跑測試確認通過**：`pnpm check` 通過；`./scripts/check.sh` exit 0。
- [ ] **Step 5: Commit** `✨ feat: show the pet and captures in the battle log and on stage`

---

### Task 6: 寵物照顧與寵物 API

**Files:**
- Create: `Domain/Pets/{PetCare, PetRules}.cs`、`Application/Pets/PetService.cs`、`Api/Endpoints/PetEndpoints.cs`
- Modify: `Application/Adventure/AdventureService.cs`（飢餓）、`Application/DependencyInjection.cs`、`Api/Endpoints/Contracts.cs`、`Api/Program.cs`
- Test: `Tests/Domain/Pets/{PetCareTests, PetRulesTests}.cs`、`Tests/Application/{PetServiceTests, AdventureServiceTests}.cs`、`Tests/Api/PetEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 2 `IPetRepository`、`DerivedStats`、`EquipmentBonusLoader.LoadBonusAsync`、`CharacterService`（端點讀角色，同 `BankEndpoints.ProfileAsync`）
- Produces:

```csharp
namespace EndlessBattle.Domain.Pets;
public enum PetStat { Attack, Magic, Defense, Spirit }
public sealed record PetFood(string Id, string Name, PetStat Stat, int Amount, long Cost, int SatietyMin, int SatietyMax);
public static class PetCare
{
    public const int FeedCooldownSeconds = 10, SatietyFull = 100, HungerIntervalSeconds = 900, SecondsPerYear = 864_000;
    // 依 WOG 選單順序：fruit-vigor-small 初級活力果實、fruit-aroma-small 初級香氣果實、fruit-sweet-small 初級甜味果實、fruit-wild-small 初級野性果實、
    // fruit-vigor-medium 中級活力果實、fruit-aroma-medium 中級香氣果實、fruit-sweet-medium 中級甜味果實、fruit-wild-medium 中級野性果實
    public static IReadOnlyList<PetFood> Foods { get; }
    public static IReadOnlyList<string> CanFeed(Pet pet, PetFood food, DerivedStats owner, long money, DateTimeOffset now);   // 只回第一個成立的原因
    public static Pet Feed(Pet pet, PetFood food, DateTimeOffset now, IRandomSource random);                                // 先擲飽腹、再擲親密度
    public static Pet Hunger(Pet pet, DateTimeOffset now, IRandomSource random);   // 獸欄或未超過 900 秒 → 回傳同一個物件、不擲骰
    public static int Age(Pet pet, DateTimeOffset now);
    public static DateTimeOffset NextFeedAt(Pet pet);                               // HungerSince + 10 秒
}
public static class PetRules
{
    public const int MaxPets = 3, NameMaxLength = 12;
    public static IReadOnlyList<Pet> Carry(IReadOnlyList<Pet> pets, long petId);    // 指定的攜帶、其餘全部放回獸欄
    public static IReadOnlyList<string> CanRename(string? name);                    // ["nameInvalid"]：去頭尾空白後 1～12 字
}

namespace EndlessBattle.Application.Pets;
public sealed record PetView(Pet Pet, int Age, DateTimeOffset NextFeedAt);
public sealed record PetOverview(IReadOnlyList<PetView> Pets, int MaxPets, IReadOnlyList<PetFood> Foods);
public sealed class PetService(ICharacterRepository characters, IPetRepository pets, IEquipmentRepository equipment, IGameContent content,
    IClock clock, IRandomSourceFactory randomFactory, IUnitOfWork unitOfWork)
{
    public Task<PetOverview> GetAsync(long accountId, CancellationToken cancellationToken);
    public Task<PetOverview> CarryAsync(long accountId, long petId, CancellationToken cancellationToken);
    public Task<PetOverview> StoreAsync(long accountId, long petId, CancellationToken cancellationToken);
    public Task<PetOverview> RenameAsync(long accountId, long petId, string? name, CancellationToken cancellationToken);
    public Task<PetOverview> ReleaseAsync(long accountId, long petId, CancellationToken cancellationToken);
    public Task<PetOverview> FeedAsync(long accountId, long petId, string foodId, CancellationToken cancellationToken);
}
// API（需登入）：GET /api/pets → PetsResponse；POST /api/pets/carry、/store、/release { petId }、/rename { petId, name } → PetsResponse；
// POST /api/pets/feed { petId, foodId } → PetFeedResponse(PetsResponse Pets, CharacterResponse Character)
public sealed record PetRequest(long? PetId, string? Name, string? FoodId);
public sealed record PetsResponse(IReadOnlyList<PetResponse> Pets, int MaxPets, IReadOnlyList<PetFoodResponse> Foods);
public sealed record PetResponse(long Id, string MonsterId, string Species, string Name, PetPersonality Personality, int Attack, int Magic,
    int Defense, int Spirit, int Satiety, int Intimacy, bool Carried, int Age, DateTimeOffset NextFeedAt);
public sealed record PetFoodResponse(string Id, string Name, PetStat Stat, int Amount, long Cost);
```

每個寫入型用例一筆交易：讀角色與寵物 → 驗證（不存在 → `petUnknown`；食物不存在 → `foodUnknown`）→ **`characters.UpdateAsync`（餵食時扣金錢，其餘只走版本鎖）** → 寫寵物表。`AdventureService`：模擬之後以同一個亂數源算攜帶寵物的 `PetCare.Hunger`，有變化時在交易中捕到的寵物之後 `pets.UpdateAsync`。

- [ ] **Step 1: 寫失敗測試**（Domain）：
  - `Foods_AreTheWogMenu`：8 筆、順序與名稱同上、初級 +2／20 金／飽腹 10～20、中級 +4／50 金／18～28。
  - `Feed_RaisesStatSatietyAndIntimacy`：擲飽腹 15、親密度 4 → 能力 +2、Satiety +15、Intimacy +4、`HungerSince == now`。
  - `Feed_AllowsEqualToOwnerRejectsAbove`：主人 at 180：Attack 178 → 可以；179 → `["overOwner"]`。
  - `Feed_DefenseCapIsTheHigherOfDfAndMdf`：df 50、mdf 80：Defense 78 → 可以；79 → `overOwner`。
  - `Feed_SpiritHasNoCap`：Spirit 999 → 可以。
  - `Feed_CooldownIsTenSeconds`：`HungerSince` 9.999 秒前 → `["tooSoon"]`；10 秒前 → 可以。
  - `Feed_RejectsWhenSatietyAtLeastHundred`：100 → `["full"]`；99 → 可以（餵完 > 100）。
  - `Feed_RejectsWithoutMoney`：金錢 19、初級 → `["notEnoughMoney"]`；條件同時不符時只回 `tooSoon`。
  - `Hunger_TicksOncePerCheck`：攜帶、`HungerSince` 1 小時前、擲 8 → Intimacy −1、Satiety −8、`HungerSince == now`（只扣一次）；恰好 900 秒 → 原物件；獸欄寵物 → 原物件。
  - `Hunger_FloorsAtZero`；`Age_IsTenDaysPerYear`：4.99 天 → 0、5 天 → 1、15 天 → 2。
  - `Carry_StoresThePreviouslyCarried`；`Rename_RejectsEmptyOrTooLong`（空白、13 字 → `nameInvalid`；12 字可以）。
- [ ] **Step 2: 寫失敗測試**（Application／API）：
  - `Feed_ChargesMoneyAndWritesCharacterFirst`：log 中 character update 在 `pet:update` 之前、金錢 −20。
  - `Feed_OverOwnerChangesNothing`：400 `overOwner`，金錢、寵物、角色版本都不變。
  - `Carry_SwitchesTheCarriedPet`、`Release_RemovesThePet`、`Pets_UnknownPetIsRejected`（`petUnknown`）。
  - `Advance_TicksCarriedPetHunger`：攜帶寵物 `HungerSince` 1 小時前 → 推進後 Intimacy −1、Satiety −8～−12、`HungerSince == now`；log 中 `pet:update` 在 `pet:add` 之後。
  - API：`Pets_GetListsAgeAndFoods`（8 種食物、`MaxPets 3`、`Species` 為怪物名）、`Pets_FeedReturnsCharacter`（`Character.Money` 已扣）、`Pets_RequireLogin`（401）。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*PetCareTests" --filter-class "*PetRulesTests" --filter-class "*PetServiceTests" --filter-class "*AdventureServiceTests" --filter-class "*PetEndpointsTests"` → FAIL。
- [ ] **Step 4: 實作** `PetCare`、`PetRules`、`PetService`、端點與 DTO、DI、`AdventureService` 的飢餓。
- [ ] **Step 5: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit** `✨ feat: feed, carry, rename and release pets`

---

### Task 7: 傭兵（第二條離線時間軸）

**Files:**
- Create: `Domain/Mercenaries/{MercenaryRules, MercenarySimulator}.cs`、`Application/Mercenaries/MercenaryService.cs`、`Api/Endpoints/MercenaryEndpoints.cs`
- Modify: `Domain/Progression/RewardFormulas.cs`、`Domain/Adventure/{AdventureSimulator, DropRoller}.cs`、`Application/Adventure/{AdventureService, AdvanceResult}.cs`、`Application/DependencyInjection.cs`、`Api/Endpoints/{AdventureEndpoints, Contracts}.cs`、`Api/Program.cs`
- Test: `Tests/Domain/Mercenaries/{MercenaryRulesTests, MercenarySimulatorTests}.cs`、`Tests/Domain/Adventure/DropRollerTests.cs`、`Tests/Application/{MercenaryServiceTests, AdventureServiceTests}.cs`、`Tests/Api/MercenaryEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 1 `MercenaryDefinition`、Task 2 `MercenaryContract`／`IMercenaryRepository`、Task 3 `PetBattle`、`KillCounter`、`MonsterPool`、`LevelUp`
- Produces:

```csharp
// 結構性抽出（先單獨 commit）：AdventureSimulator 的超限部分經驗
public static long TimeoutExp(MonsterDefinition monster, int damageDealt, int playerLevel, double fDown, IRandomSource random);   // RewardFormulas
// DropRoller.Roll(drops, fDown, random, double denominatorFactor = 1)：分母 floor(topr × 0.9 × 1.5(行動力不足) × factor)

namespace EndlessBattle.Domain.Mercenaries;
public enum MercenaryFightResult { Won, Lost, Timeout }
public sealed record MercenaryFight(DateTimeOffset At, string MonsterId, MercenaryFightResult Result, long Exp, long Money, IReadOnlyList<string> Drops);
// Contract 為 null：次數用完、已解除僱用
public sealed record MercenaryRun(Character Character, int LevelsGained, MercenaryContract? Contract, IReadOnlyList<MercenaryFight> Fights,
    long JobExp, IReadOnlyList<string> DroppedItemIds, IReadOnlyList<ActiveMission> Missions, int MissionKills);
public static class MercenaryRules
{
    public const double FightIntervalSeconds = 100;
    public const int NameMaxLength = 8;
    public static long Discount(long value, int percent);                     // round(value × percent / 100)，.5 遠離零
    public static double DropDenominatorFactor(int dropPercent);              // > 1 時 2 − dropPercent / 100，否則 1
    // 原因依序：mercenaryUnknown（單獨）、honourOnly、mercenaryHired、notEnoughMoney
    public static IReadOnlyList<string> CanHire(Character character, MercenaryDefinition? definition, MercenaryContract? current);
    public static IReadOnlyList<string> CanDeploy(PlaceDefinition? place);   // placeUnknown、placeRestricted
}
public static class MercenarySimulator
{
    // contract 必須是出擊中；placeMonsters 為 content.MonstersOf(placeId)，這裡排除全域怪；每場以當下角色重建滿血的 combatant
    public static MercenaryRun Run(Character character, JobDefinition job, MercenaryContract contract, MercenaryDefinition definition,
        IReadOnlyList<MonsterDefinition> placeMonsters, IReadOnlyList<EquippedSkill> skills, EquipmentBonus equipment, Pet? carriedPet,
        IReadOnlyList<ActiveMission> missions, DateTimeOffset now, IRandomSource random);
}

namespace EndlessBattle.Application.Mercenaries;
public sealed record MercenaryOverview(IReadOnlyList<MercenaryDefinition> Catalogue, MercenaryContract? Contract);
public sealed class MercenaryService(ICharacterRepository characters, IMercenaryRepository mercenaries, IGameContent content, IClock clock, IUnitOfWork unitOfWork)
{
    public Task<MercenaryOverview> GetAsync(long accountId, CancellationToken cancellationToken);
    public Task<MercenaryOverview> HireAsync(long accountId, string mercenaryId, CancellationToken cancellationToken);
    public Task<MercenaryOverview> DeployAsync(long accountId, int placeId, CancellationToken cancellationToken);   // NextFightAt = now + 100 秒
    public Task<MercenaryOverview> RestAsync(long accountId, CancellationToken cancellationToken);
    public Task<MercenaryOverview> RenameAsync(long accountId, string? name, CancellationToken cancellationToken);   // nameInvalid：1～8 字
    public Task<MercenaryOverview> DismissAsync(long accountId, CancellationToken cancellationToken);
}   // 沒有合約時 Deploy／Rest／Rename／Dismiss → noMercenary

namespace EndlessBattle.Application.Adventure;
public sealed record MercenaryReport(string Name, IReadOnlyList<MercenaryFight> Fights, long Exp, long Money, int LevelsGained, int Remaining, bool Dismissed);
// AdvanceResult(..., EquipmentBonus Equipment, MercenaryReport? Mercenary = null)：本次推進有傭兵戰鬥時才有值

// API（需登入）：GET /api/mercenaries → MercenaryOverviewResponse；POST /api/mercenaries/hire { mercenaryId } → MercenaryHireResponse；
// POST /deploy { placeId }、/rest、/rename { name }、/dismiss → MercenaryOverviewResponse
public sealed record MercenaryRequest(string? MercenaryId, int? PlaceId, string? Name);
public sealed record MercenaryOverviewResponse(IReadOnlyList<MercenaryCatalogueResponse> Catalogue, MercenaryContractResponse? Contract);
public sealed record MercenaryCatalogueResponse(string Id, string Name, long? Price, int Fights, int MoneyPercent, int DropPercent, int ExpPercent, int DeathCost);
public sealed record MercenaryContractResponse(string MercenaryId, string Name, int Remaining, int? PlaceId, string? PlaceName, bool Fighting, DateTimeOffset? NextFightAt);
public sealed record MercenaryHireResponse(MercenaryOverviewResponse Mercenaries, CharacterResponse Character);
// AdvanceResponse(..., CharacterResponse Character, MercenaryReportResponse? Mercenary)
public sealed record MercenaryReportResponse(string Name, int Fights, int Wins, long Exp, long Money, int LevelsGained, int Remaining, bool Dismissed,
    IReadOnlyList<MercenaryFightResponse> Recent);                           // Recent：最後 5 場，依時間
public sealed record MercenaryFightResponse(DateTimeOffset At, string MonsterId, string MonsterName, bool Won, long Exp, long Money);
```

模擬器：場次時刻 `NextFightAt, +100, …` 只要 ≤ now 且次數 > 0 就打；早於 `now − MaxSimulatedSeconds` 的場次跳過（`NextFightAt` 推到第一個不早於該時刻的格點）。每場：`MonsterPool.Pick(地區自己的怪, placeId, subPlace: null, random, SpawnableMissionIds(missions, 時刻))` → `WogBattle.Run(滿血 combatant, 怪, null, skills, supply: null, random, 寵物)` → 勝：經驗 `Discount(RewardFormulas.Exp(..., fDown 1), ExpPercent)`、金錢 `Discount(Money, MoneyPercent)`、熟練度全額、`DropRoller.Roll(..., DropDenominatorFactor(DropPercent))`、`KillCounter.Apply`（擊殺時刻＝場次時刻）、次數 −1；敗：次數 −`DeathCost`；超限：`Discount(TimeoutExp(...), ExpPercent)`、次數 −`DeathCost`；每場後 `LevelUp.Apply`。角色的 Hp／Sp 不變。

`AdventureService.AdvanceAsync`：讀合約（出擊中、內容有這位傭兵才算）→ 傭兵模擬 → 以傭兵結果的角色與任務跑冒險 → 飢餓。交易寫入順序：角色（兩條時間軸合併後）→ 熟練度（同職業合併成一筆）→ 任務進度（對照最初的追蹤狀態）→ 掉落（`傭兵的 ++ 冒險的` 一次 `DropApplier.Apply`）→ 補給欄 → 捕到的寵物 → 寵物飢餓 → **合約**（`Contract null` → `RemoveAsync`，否則 `UpdateAsync` 次數與 `NextFightAt`）。`MercenaryService` 的寫入一樣先 `characters.UpdateAsync`（僱用時扣錢）。

- [ ] **Step 1: 結構性變更**：把 `AdventureSimulator` 的超限經驗計算抽成 `RewardFormulas.TimeoutExp`，前後跑 `dotnet test --filter-class "*AdventureSimulatorTests" --filter-class "*RewardFormulasTests"` 都 PASS → commit `♻️ refactor: extract the timeout experience formula`。
- [ ] **Step 2: 寫失敗測試**（Domain）：
  - `DropRoller_AppliesDenominatorFactor`：topr 10、factor 1.7 → 分母 15（以腳本亂數驗證掉落界線）。
  - `Rules_DropFactorFollowsPhp`（30 → 1.7、100 → 1.0、1 → 1.0）、`Rules_DiscountRoundsLikePhp`（`Discount(155, 50) == 78`）、`CanHire_ReasonsInOrder`（honour-only 且已有合約且沒錢 → `["honourOnly","mercenaryHired","notEnoughMoney"]`）、`CanDeploy_RejectsRestricted`。
  - `Mercenary_FightsEveryHundredSeconds`：`NextFightAt = T`、now = T+250 → 3 場（T、T+100、T+200）、合約 `NextFightAt == T+300`。
  - `Mercenary_DiscountsExpAndMoney`：同一種子、寶物% 都 100，經驗金錢 100% 與 50% 各跑一次 → 每場 `Exp`／`Money` 後者等於 `Discount(前者, 50)`；熟練度相同。
  - `Mercenary_FightsAtFullHealthWithoutTouchingCharacterHp`：角色 Hp 1 → 照打、勝；`run.Character.Hp == 1`；沒有 `PotionEvent`、沒有住宿。
  - `Mercenary_LossesCostDeathCost`：能力極低的角色 → 每場 Lost 或 Timeout、次數每場 −`DeathCost`。
  - `Mercenary_StopsWhenFightsRunOut`：次數 2、到期 10 場、強角色 → 只打 2 場、`Contract null`。
  - `Mercenary_SkipsFightsOlderThanOneDay`：`NextFightAt = now − 2 天`、次數 10000 → 865 場、第一場時刻 `now − 86400 秒`。
  - `Mercenary_NeverMeetsGlobalMonsters`（手建怪物清單含一隻全域怪 → 遇敵從不是它）、`Mercenary_CountsMissionKills`、`Mercenary_LevelsUpTheCharacter`、`Mercenary_CarriedPetJoins`（傭兵不輸出戰鬥事件；以「玩家攻擊打不死、寵物出手擊倒怪物」的腳本亂數驗證該場 `Won`，同一腳本不帶寵物則不是 `Won`）。
- [ ] **Step 3: 寫失敗測試**（Application／API）：
  - `Hire_ChargesPriceAndStartsResting`、`Hire_RejectsSecondMercenary`、`Hire_RejectsHonourOnly`、`Deploy_SchedulesFirstFightAfterInterval`、`Deploy_RejectsRestrictedPlace`、`Rest_StopsFighting`、`Dismiss_RemovesContract`、`Rename_LimitsToEightCharacters`。
  - `Advance_SettlesMercenaryBeforeAdventure`：合約在 now − 300 秒出擊 → `Mercenary` 報告 3 場、角色經驗含兩條時間軸、合約次數減少；log 中 `mercenary:update` 在所有其他寫入之後、character update 最先。
  - `Advance_RemovesMercenaryWhenFightsRunOut`：次數 1 → 報告 `Dismissed true`、`mercenary:remove`、冒險照常有事件。
  - `Advance_WritesNoCompanionState_WhenCharacterUpdateConflicts`：出擊中的合約＋補給欄捕捉道具＋飢餓到期的攜帶寵物；照 `Advance_WritesNoMissionProgress_WhenCharacterUpdateConflicts` 的作法在讀取後改動角色版本並刪除合約（模擬同時解僱）→ `ConflictException`；log 沒有任何 `pet:`、`mercenary:` 寫入，合約仍不存在、寵物不變。
  - API：`Mercenaries_HireDeployAndAdvance`（`advance` 回應的 `mercenary.fights ≥ 1`、`recent` 帶怪物名）、`Mercenaries_SecondHireReturnsReason`（400 `["mercenaryHired"]`）、`Mercenaries_RequireLogin`。
- [ ] **Step 4: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*Mercenary*Tests" --filter-class "*DropRollerTests" --filter-class "*AdventureServiceTests"` → FAIL。
- [ ] **Step 5: 實作** 上列型別、`AdventureService` 的傭兵結算、`MercenaryService`、端點、`AdventureEndpoints` 的報告映射、DI。
- [ ] **Step 6: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 7: Commit** `✨ feat: hire mercenaries that fight on their own timeline`

---

### Task 8: 勳章所與任務的寵物、勳章

**Files:**
- Create: `Domain/Honour/HonourShop.cs`、`Application/Honour/HonourService.cs`、`Api/Endpoints/HonourEndpoints.cs`
- Modify: `Domain/Missions/{MissionDefinition, DailyMissions, MissionBoard, MissionRewards}.cs`、`Application/Missions/{MissionService, MissionCompletion}.cs`、`Infrastructure/Content/JsonGameContent.cs`、`Api/Endpoints/{MissionEndpoints, Contracts}.cs`、`Application/DependencyInjection.cs`、`Api/Program.cs`、`backend/content/missions.json`、`Tests/TestContent/missions.json`、`docs/research/mission-table.md`
- Test: `Tests/Domain/Honour/HonourShopTests.cs`、`Tests/Domain/Missions/{DailyMissionsTests, MissionBoardTests, MissionRewardsTests}.cs`、`Tests/Application/{HonourServiceTests, MissionServiceTests}.cs`、`Tests/Api/{HonourEndpointsTests, MissionEndpointsTests}.cs`、`Tests/Infrastructure/JsonGameContentTests.cs`、`Tests/Content/MissionTableTests.cs`

**Interfaces:**
- Consumes: Task 1 `HonourExchange`、Task 2 `IPetRepository`／`IMercenaryRepository`、Task 4 `PetCapture.NewPet`／`CapturedPet.ToPet`、`Delivery.Plan`／`After`／`Owned`、`InventoryRules.TryAdd`
- Produces:

```csharp
namespace EndlessBattle.Domain.Honour;
// Reasons 非空時其餘欄位不可用；Item 是放進付款後背包的結果
public sealed record HonourOutcome(DeliveryPlan Payment, AddResult? Item, MercenaryContract? Mercenary, IReadOnlyList<string> Reasons);
public static class HonourShop
{
    public static bool Affordable(HonourExchange exchange, IReadOnlyDictionary<string, int> owned);
    public static HonourOutcome Exchange(Character character, HonourExchange? exchange, IReadOnlyList<InventoryEntry> bag, MercenaryContract? current,
        Func<string, ItemDefinition> items, Func<string, MercenaryDefinition> mercenaries, DateTimeOffset now);
}
namespace EndlessBattle.Application.Honour;
public sealed record HonourOffer(HonourExchange Exchange, bool Affordable);
public sealed record HonourOverview(IReadOnlyList<HonourOffer> Offers, IReadOnlyDictionary<string, int> Medals);   // Medals：每種勳章（內容順序）的背包持有數
public sealed class HonourService(ICharacterRepository characters, IInventoryRepository inventory, IEquipmentRepository equipment,
    IMercenaryRepository mercenaries, IGameContent content, IClock clock, IUnitOfWork unitOfWork)
{
    public Task<HonourOverview> GetAsync(long accountId, CancellationToken cancellationToken);
    public Task<HonourOverview> ExchangeAsync(long accountId, string exchangeId, CancellationToken cancellationToken);
}
// API（需登入）：GET /api/honour → HonourShopResponse；POST /api/honour/exchange { exchangeId } → HonourShopResponse
public sealed record HonourExchangeRequest(string? ExchangeId);
public sealed record HonourShopResponse(IReadOnlyList<HonourMedalResponse> Medals, IReadOnlyList<HonourExchangeResponse> Exchanges);
public sealed record HonourMedalResponse(string ItemId, string Name, int Owned);
public sealed record HonourExchangeResponse(string Id, string Kind, string RewardId, string RewardName, int Quantity,
    IReadOnlyList<HonourCostResponse> Cost, bool Affordable);                 // Kind："item" | "mercenary"
public sealed record HonourCostResponse(string ItemId, string Name, int Quantity, int Owned);

// 任務
// MissionDefinition(..., IReadOnlyList<MissionReward> DailyRewards, IReadOnlyList<string>? DailyPets = null)，DailyPets 以 init 預設 []
// MissionObjective(string MissionId, KillTarget? Kill, IReadOnlyList<ItemQuantity> Deliver, MissionReward Reward, long Exp, string? DeliverPet = null)
// MissionBoard.Build(..., IReadOnlyDictionary<string, int> owned, DateTimeOffset now, string? carriedPetMonsterId = null)
//   Ready 再加：DeliverPet 為 null 或等於攜帶寵物的物種
// MissionRewardOutcome(..., LearnedSkill? GrantedSkill, IReadOnlyList<string> Reasons, CapturedPet? NewPet = null)
public static MissionRewardOutcome Apply(Character character, JobDefinition job, MissionObjective objective, IReadOnlyList<InventoryEntry> bagAfterDelivery,
    Func<string, ItemDefinition> items, Func<string, MonsterDefinition> monsters, IReadOnlyList<LearnedSkill> learned,
    int petCountAfterDelivery, DateTimeOffset now, IRandomSource random);    // Reasons 依序 bagFull、petsFull；寵物在升級擲骰之後擲
// MissionCompletion(..., int LevelsGained, PetReward? Pet = null)
// MissionResponse 加 MissionPetResponse? DeliverPet；MissionRewardResponse 與 MissionCompleteResponse 加 MissionRewardPetResponse? Pet
public sealed record MissionPetResponse(string MonsterId, string Name, bool Carried);
public sealed record MissionRewardPetResponse(string MonsterId, string Name, int Level);
```

`HonourService.ExchangeAsync` 一筆交易：讀角色、背包（`BagReader`）、合約 → `HonourShop.Exchange` 有 Reasons → `InputValidationException("無法兌換", reasons)` → `characters.UpdateAsync`（版本鎖）→ 扣勳章（`Payment` 的 Updated／Removed）→ 加道具或 `mercenaries.AddAsync`。`MissionService`（建構子加 `IPetRepository pets`）`CompleteAsync`：在 `itemsMissing` 之後檢查交寵物（攜帶中的物種 ≠ `DeliverPet` → `petMissing`）；`petCountAfterDelivery = 寵物數 − (有交寵物 ? 1 : 0)`；寫入在技能之後、任務完成之前：`pets.RemoveAsync`（交出的）→ `pets.AddAsync(NewPet.ToPet())`。看板的攜帶物種由 `pets.ListAsync` 取得。

**載入驗證變更**：`rewards.pet`／`dailyRewards[].pet` 改為允許——怪物存在、不是任務怪、`level` 1～280（WOG 寵物蛋倍率 280 為上限）；每日任務 `dailyKills` 與 `dailyPets` 恰好一個非空，`dailyPets` 怪物存在、不是任務怪、不重複；一般任務不得有 `dailyPets`。既有 `Missions_RejectPetReward` 改寫為 `Missions_AcceptPetReward`＋`Missions_RejectPetRewardForMissionMonster`。

**內容（missions.json）：**
- `daily-hunt-novice`（WOG 222）報酬池改為 5 筆，依序 `continent-medal`、`kingdom-medal`、`rainforest-medal`、`silver-medal`、`royal-city-medal` 各 ×1、金錢 0。
- `daily-hunt-veteran`（224）報酬池改為 5 筆：`medium-supply` ×3 ＋ 同上順序的勳章 ×2、金錢 0。
- 新 `daily-pet-plains`（223）：委託者 獸欄管理員、標題「每日委託：平原的寵物」、Lv10、說明「把指定種類的寵物帶來給我看看，今天想研究的是這一種。」、完成訊息「真是隻好孩子，牠就交給我照顧吧。」；`dailyPets` = 地區 1～4 的 meet 0 怪 `meadow-slime`、`lantern-bat`、`tunnel-goblin`、`bog-toad`、`breeze-fox`、`ruin-sentinel`；報酬池（WOG 1422 → 捕獸網〔Claude 的設計〕）：`capture-net` ×1＋大陸／王國／雨林勳章 ×1 各一筆、`silver-medal` ×1、`capture-net` ×1＋`royal-city-medal` ×1。
- 新 `daily-pet-frontier`（225）：委託者 獸欄管理員、標題「每日委託：邊境的寵物」、Lv30、說明與完成訊息同上；`dailyPets` = 地區 5～8 的 meet 0 怪 `vine-guardian`、`fallen-soldier`、`trial-knight`、`bridge-wraith`、`jungle-panther`、`trail-boar`；報酬池同上但捕獸網改 `capture-cage`、白銀那筆也附 `capture-cage` ×1（WOG 225 的第 4 筆有 1423）。
- 新一般任務 `tamer-trial`：魔法王國／8 幻獸森林、委託者 幻獸森林的馴獸師 莉亞、標題「馴獸師的考驗」、Lv18、擊敗 `jungle-panther` ×10、金錢 900、寵物 `jungle-panther` 倍率 8；說明「證明你能和森林的野獸打交道，我就送你一隻黑豹幼獸。」、完成訊息「牠很親近你呢，好好照顧牠。」
- 測試內容：`test-daily-pet`（每日、Lv1、`dailyPets [test-sprite, test-wolf]`、報酬池 `test-medal-a` ×1／`test-medal-b` ×1）、`test-pet-reward`（地區 1、Lv1、擊敗 `test-sprite` ×1、寵物 `test-wolf` 倍率 2）。

- [ ] **Step 1: 寫失敗測試**（Domain）：
  - `Exchange_PaysMedalsAndAddsItem`：`test-medal-a` ×3 → 兌換 `test-exchange-ore` → Payment 扣 2、Item 放進材料分類。
  - `Exchange_ReasonsInOrder`：未知 id → `["honourUnknown"]`；勳章不足且材料分類已滿 → `["medalsMissing","bagFull"]`；已有傭兵時兌換傭兵 → `["mercenaryHired"]`。
  - `Exchange_MercenaryStartsResting`：合約 `Remaining == 5`、`PlaceId null`、`HiredAt == now`。
  - `Resolve_DailyPetRollsSpeciesThenReward`：同參數結果相同；`DeliverPet` 非 null、`Kill` null；2026-10-01 起 30 個週期物種至少 2 種。
  - `Board_PetDeliveryReadyOnlyWithCarriedSpecies`：攜帶物種相符 → `Ready true`；不符或沒攜帶 → false。
  - `Apply_PetRewardRollsStatsTimesLevel`：倍率 2、擲 5、5、5、個性 1 → `NewPet` Attack／Magic／Defense 10、積極、名字＝怪物名。
  - `Apply_PetRewardRejectedWhenPetsFull`：`petCountAfterDelivery 3` → `["petsFull"]`；背包也滿 → `["bagFull","petsFull"]`。
- [ ] **Step 2: 寫失敗測試**（內容／Application／API）：
  - 內容：`Missions_AcceptPetReward`、`Missions_RejectPetRewardForMissionMonster`、`Missions_RejectDailyWithBothPools`、`Missions_RejectDailyPetsOnNormalMission`；`MissionTableTests` 改為一般任務 16 筆（魔法王國 5）、`MissionTable_HasEightDailies`、`MissionTable_HuntDailiesPayWogMedals`（novice 5 筆勳章順序、veteran 每筆 `medium-supply` ×3＋勳章 ×2）、`MissionTable_PetDailiesUseMeetZeroMonsters`。
  - `Exchange_WritesCharacterVersionFirst`（log：character update → inventory 寫入 → `mercenary:add`）、`Exchange_MissingMedalsChangesNothing`。
  - `Complete_DeliversCarriedPet`：攜帶 `test-wolf` 物種、`test-daily-pet` 擲到 `test-wolf` 的週期 → 完成後那隻寵物刪除、獲得勳章。
  - `Complete_RejectsWrongCarriedPet`：`["petMissing"]`，寵物、背包、任務、版本不變。
  - `Complete_PetRewardAddsStoredPet`：`test-pet-reward` → 寵物表多一隻獸欄 `test-wolf`、`MissionCompletion.Pet` 有值。
  - `Complete_PetRewardRejectedWhenPetsFull`：已有 3 隻 → 400 `["petsFull"]`；金錢、經驗、道具、任務狀態、角色版本不變。
  - API：`Honour_GetListsMedalsAndOffers`（兩筆、`Affordable` 依持有數）、`Honour_ExchangeReturnsShop`、`Honour_MissingMedalsReturnsReason`、`Honour_RequireLogin`；`Missions_PetDailyShowsSpecies`（`deliverPet.name` 為怪物名、`carried` 依攜帶狀態）。
- [ ] **Step 3: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*HonourShopTests" --filter-class "*HonourServiceTests" --filter-class "*HonourEndpointsTests" --filter-class "*Mission*Tests" --filter-class "*DailyMissionsTests" --filter-class "*JsonGameContentTests"` → FAIL。
- [ ] **Step 4: 實作** 上列型別與修改、內容與測試內容、`mission-table.md`（新三筆、勳章報酬恢復、與研究的差異：任務寵物獎勵上限、交寵物要攜帶中的）。
- [ ] **Step 5: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 6: Commit** `✨ feat: exchange medals and reward or request pets in missions`

---

### Task 9: NPC 冠軍

**Files:**
- Create: `Domain/Champion/{ChampionRules, ChampionDuel}.cs`、`Application/Skills/EquippedSkills.cs`、`Application/Champion/ChampionService.cs`、`Api/Endpoints/ChampionEndpoints.cs`
- Modify: `Application/Adventure/AdventureService.cs`（改用 `EquippedSkills`）、`Application/DependencyInjection.cs`、`Api/Endpoints/Contracts.cs`、`Api/Program.cs`
- Test: `Tests/Domain/Champion/{ChampionRulesTests, ChampionDuelTests}.cs`、`Tests/Application/ChampionServiceTests.cs`、`Tests/Api/ChampionEndpointsTests.cs`

**Interfaces:**
- Consumes: Task 1 `ChampionSeat`／`IGameContent.SeedChampion`、Task 2 `IChampionRepository`、`WogBattle.Run`、`CombatBridge`、`EquipmentBonusLoader`
- Produces:

```csharp
// 結構性抽出（先單獨 commit）：AdventureService.LoadEquippedSkillsAsync
public static class EquippedSkills
{
    public static Task<IReadOnlyList<EquippedSkill>> LoadAsync(ISkillLoadoutRepository loadout, ISkillBookRepository skillBook, IGameContent content,
        Character character, CancellationToken cancellationToken);
}
namespace EndlessBattle.Domain.Champion;
public static class ChampionRules
{
    public const long ChallengeFee = 500;
    public static readonly TimeSpan LockAfterWin = TimeSpan.FromSeconds(604_800);
    public static IReadOnlyList<string> CanChallenge(Character character, ChampionSeat seat, DateTimeOffset now);   // championLocked、notEnoughMoney、noHp
    public static ChampionSeat Snapshot(Character character, EquipmentBonus equipment, DateTimeOffset now);
}
public sealed record DuelResult(Character Character, ChampionSeat Seat, BattleOutcome Battle, bool Won);
public static class ChampionDuel
{
    // 呼叫前先以 CanChallenge 確認；勝負規則見 Global Constraints
    public static DuelResult Fight(Character character, EquipmentBonus equipment, IReadOnlyList<EquippedSkill> skills, ChampionSeat seat,
        DateTimeOffset now, IRandomSource random);
}
namespace EndlessBattle.Application.Champion;
public sealed record ChampionView(ChampionSeat Seat, IReadOnlyList<string> Reasons);            // Reasons：現在挑戰會被擋的原因
public sealed record ChampionChallengeResult(ChampionView View, IReadOnlyList<BattleEvent> Events, bool Won);
public sealed class ChampionService(ICharacterRepository characters, IChampionRepository champions, IEquipmentRepository equipment,
    ISkillLoadoutRepository loadout, ISkillBookRepository skillBook, IGameContent content, IClock clock, IRandomSourceFactory randomFactory, IUnitOfWork unitOfWork)
{
    public Task<ChampionView> GetAsync(long accountId, CancellationToken cancellationToken);
    public Task<ChampionChallengeResult> ChallengeAsync(long accountId, CancellationToken cancellationToken);
}
// API（需登入）：GET /api/champion → ChampionResponse；POST /api/champion/challenge → ChampionChallengeResponse
public sealed record ChampionResponse(string Name, int Level, int Element, int Hp, int HpMax, int Sp, int SpMax, int At, int Mat, int Df, int Mdf,
    int Agi, int Au, bool IsPlayer, int Defenses, DateTimeOffset? CrownedAt, DateTimeOffset? LockedUntil, long Fee, IReadOnlyList<string> Reasons);
public sealed record ChampionChallengeResponse(ChampionResponse Champion, IReadOnlyList<BattleEvent> Events, bool Won, CharacterResponse Character);
```

`ChallengeAsync` 一筆交易：讀角色、席位（null → `content.SeedChampion`）、裝備、技能 → `CanChallenge` 非空 → `InputValidationException("無法挑戰冠軍", reasons)` → `ChampionDuel.Fight` → **`characters.UpdateAsync(result.Character)`** → `champions.SaveAsync(result.Seat)`。事件的 `AtSeconds` 從 0 起算、不含 `EncounterEvent`。

- [ ] **Step 1: 結構性變更**：抽出 `EquippedSkills.LoadAsync`，`AdventureService` 改用它；`dotnet test --filter-class "*AdventureServiceTests"` 前後都 PASS → commit `♻️ refactor: share the equipped skill loader`。
- [ ] **Step 2: 寫失敗測試**（Domain）：
  - `CanChallenge_ReasonsInOrder`：鎖定中、金錢 499、HP 0 → `["championLocked","notEnoughMoney","noHp"]`；`now == LockedUntil` → 不鎖。
  - `Duel_WinReplacesSeatWithFullHealthSnapshot`：能力極高的角色打測試冠軍 → `Won`、席位名字＝角色名、`HpMax`／`At` 等於含裝備的有效值、`Hp == HpMax`、`IsPlayer`、`Defenses 0`、`LockedUntil == now + 7 天`。
  - `Duel_LossKeepsChampionDamage`：能力極低的角色 → 席位 `Hp` 等於戰後冠軍剩餘 HP（< HpMax）、`Defenses +1`、`LockedUntil` 不變。
  - `Duel_ChargesFeeEitherWay`：勝負都 `Money −500`；沒有經驗與金錢報酬；角色 `Hp` 等於戰後值。
  - `Duel_TimeoutCountsAsDefense`、`Duel_UsesNoSupplyAndNoPet`（補給欄有補給品、HP 低 → 沒有 `PotionEvent`）。
- [ ] **Step 3: 寫失敗測試**（Application／API）：`Get_ShowsSeedWhenNoSeat`、`Challenge_WritesCharacterVersionFirst`（log：character update → `champion:save`）、`Challenge_RejectsWithReasonsAndWritesNothing`（金錢 400）、`Challenge_SecondChallengeFacesWoundedChampion`（第一次輸 → 第二次開戰時冠軍 HP 是上次剩餘值）；API `Champion_GetAndChallenge`（回應 `events` 非空、`character.money` 已扣 500）、`Champion_RequireLogin`。
- [ ] **Step 4: 跑測試確認失敗**：`cd backend && dotnet test --filter-class "*Champion*Tests"` → FAIL。
- [ ] **Step 5: 實作** 上列型別、服務、端點、DI。
- [ ] **Step 6: 跑測試確認通過**；`./scripts/check.sh` exit 0。
- [ ] **Step 7: Commit** `✨ feat: challenge the champion for 500 and take the seat`

---

### Task 10: 前端：同伴、勳章所、冠軍與傭兵戰報

**Files:**
- Create: `frontend/src/game/{pets, mercenaries, honour, champion}.ts`（+test）、`src/ui/{CompanionsScreen, PetsPage, MercenaryPage, HonourPage, ChampionPanel}.tsx`、`CompanionsScreen.css`、`ChampionPanel.css`、`src/ui/{CompanionsScreen, HonourPage, ChampionPanel}.test.tsx`
- Modify: `src/api/schema.ts`（重新產生）、`src/api/client.ts`（+test）、`src/game/{types, inventory, missions, battleLog, battlePlayback}.ts`（+test）、`src/ui/{AdventureScreen, BagScreen, MissionsScreen}.tsx`（+test）
- 截圖：`docs/superpowers/screenshots/plan-9-pets.png`、`plan-9-stage-pet.png`、`plan-9-mercenary.png`、`plan-9-champion.png`

**Interfaces:**
- Consumes: Task 6～9 的 API、Task 7 的 `AdvanceResponse.mercenary`、Task 8 的任務欄位
- Produces:

```ts
// client.ts
getPets: () => Promise<PetsView>                                   // GET /api/pets
carryPet / storePet / releasePet: (petId: number) => Promise<PetsView>
renamePet: (petId: number, name: string) => Promise<PetsView>
feedPet: (petId: number, foodId: string) => Promise<PetFeedResult>
getMercenaries: () => Promise<MercenaryOverview>
hireMercenary: (mercenaryId: string) => Promise<MercenaryHireResult>
deployMercenary: (placeId: number) => Promise<MercenaryOverview>
restMercenary / dismissMercenary: () => Promise<MercenaryOverview>
renameMercenary: (name: string) => Promise<MercenaryOverview>
getHonourShop: () => Promise<HonourShop>
exchangeHonour: (exchangeId: string) => Promise<HonourShop>
getChampion: () => Promise<ChampionView>
challengeChampion: () => Promise<ChampionChallenge>
// game/*.ts（純函式）
pets.ts: personalityLabel(p) / petTitle(pet) / feedOptionLabel(food) / petErrorMessage(reasons, fallback)
mercenaries.ts: catalogueRow(entry) / contractStatus(contract) / mercenaryErrorMessage(reasons, fallback)
honour.ts: costText(cost) / honourErrorMessage(reasons, fallback)
champion.ts: championTitle(view, now) / lockText(lockedUntil) / championErrorMessage(reasons, fallback) / duelResultLine(won, characterName)
battleLog.ts: describeMercenaryReport(report: MercenaryReport): LogEntry[]
// PlaybackItem 加 { kind: "mercenary"; report: MercenaryReport }：enqueue 時排在該批最前面（batchStart），增量 { exp, money, levels: levelsGained }
```

**文案：**
- 分頁：底部「同伴」（第 6 個）；子頁「寵物」「傭兵」；背包子頁新增「勳章所」；背包分類加「勳章」（`itemCategories` 在材料之後）；`isSupply` 改為 Supply 或 Capture（捕捉道具可裝進補給欄）。
- 寵物頁：標題 `寵物 {n}/3`；卡片標題 `{名字}－{物種} {歲數}歲`；數值 `AT`、`MT`、`DEF`、`飽腹`、`個性`（積極／冷酷／鐵壁／危急）、`出擊值`、`親密度`；狀態「攜帶中」／「獸欄」；按鈕「攜帶」「放回獸欄」「改名」「放生」（再按「確定放生」／「取消」）；餵食選單項目 `{食物名}（{AT|MT|DEF|出擊值}+{n}，{cost} 金）` 與按鈕「餵食」（`NextFeedAt` 未到時停用）；說明（WOG 原文）`餵食初級須花費20元，中級須花費50元`、`選擇攜帶時，寵物會根據親密值及主人魅力值，隨機出現支援戰鬥或抵擋敵人攻擊`；空清單 `還沒有寵物。在補給欄裝上捕獸道具，打贏時有機會捕捉。`
- 傭兵頁：說明 `歡迎來到傭兵所，請選擇欲雇用的傭兵。傭兵的能力與技能和你相同，不同等級的傭兵戰鬥獲得的金錢、寶物與經驗比例不同；死亡消耗為傭兵戰敗時扣減的戰鬥次數。`；目錄欄 名稱／價格／戰鬥次數／金錢／寶物／經驗／死亡消耗（百分比加 `%`；`price null` 顯示 `勳章所兌換`、不顯示按鈕）、按鈕「僱用」；合約卡：名字輸入框＋「改名」、`狀態：休息中|戰鬥中`、`剩餘次數 {n}`、冒險地選單（非禁區地區）＋「開始戰鬥」、「停止戰鬥」、「解除僱用」（再按「確定解除」／「取消」）。
- 戰報（`describeMercenaryReport`，kind `muted`）：場數 ≤ 5 時逐場——勝 `【傭兵】{name} 戰勝 {怪物名}，獲得金錢 {money}，經驗值 {exp}`、敗或超限 `【傭兵】{name} 戰敗 {怪物名}`（WOG `wog_etc_mercenary_mg1/mg2`）；> 5 場時一行 `【傭兵】{name} 離開期間戰鬥 {n} 場（勝 {w}），獲得金錢 {m}，經驗值 {e}`；`dismissed` 時最後加 `【傭兵】{name} 戰鬥次數已滿，自動解除僱用`（mg3）。
- 勳章所：持有列 `{勳章名} ×{n}`（五種）；區塊「傭兵」「道具」；每筆 `{獎勵名} ×{數量}`、花費 `大陸勳章 ×2、白銀勳章 ×1`（不足的那項用 `--text-muted`）、按鈕「兌換」（`affordable` false 時停用）。
- 冠軍：冒險頁地點標題旁按鈕「冠軍」；對話框標題「冠軍」；卡片 `{name} Lv {level}`（`isPlayer` 時副標 `你自己（{crownedAt 的 yyyy/MM/dd}）`）、`HP {hp}/{hpMax}`、`物攻 {at}`…`魅力 {au}`、`衛冕 {defenses} 次`、`挑戰費 500`；按鈕「挑戰」（`reasons` 非空時停用並顯示原因）；鎖定 `{yyyy/MM/dd HH:mm} 之前無法挑戰冠軍`（WOG）；結果先列戰鬥經過（`describePlaybackItem` 以冠軍名當怪物名），再一行 `{角色名} 順利當上冠軍`（WOG `cp_end`）或 `挑戰失敗，冠軍保住了寶座。`，並呼叫 `onCharacterChanged`。
- 任務：目標行 `交付寵物 {怪物名}（需攜帶）`；報酬行 `寵物 {怪物名}`；完成訊息 `獲得寵物 {怪物名}`。
- 錯誤代碼：`petUnknown` 沒有寵物資料、`foodUnknown` 沒有選擇項目、`tooSoon` 10 秒內無法餵食、`full` 飽腹大於100無法餵食、`overOwner` 寵物屬性不能超過玩家、`nameInvalid` 名稱長度不符、`mercenaryUnknown` 沒有這位傭兵、`honourOnly` 這位傭兵只能在勳章所兌換、`mercenaryHired` 只能僱用一位傭兵、`noMercenary` 還沒有僱用傭兵、`placeUnknown` 沒有選擇冒險地點、`placeRestricted` 禁區需要鑰匙、`honourUnknown` 沒有這個兌換項目、`medalsMissing` 勳章不足、`championLocked` 冷卻中無法挑戰冠軍、`notEnoughMoney` 金錢不足（冠軍頁顯示 `挑戰冠軍最少需500`）、`noHp` HP 為 0，無法挑戰、`petMissing` 需要攜帶指定的寵物、`petsFull` 牧場已滿，最多只能擁有3隻寵物、`bagFull` 背包已滿（WOG 原文者照抄）；未知代碼用 `ApiError.detail`。

- [ ] **Step 1: 寫失敗測試**：`pets.test.ts`／`mercenaries.test.ts`／`honour.test.ts`／`champion.test.ts`（各文案函式與錯誤對照、fallback）；`battleLog.test.ts`（傭兵逐場、彙總、解除僱用三種）；`battlePlayback.test.ts`（有 `mercenary` 時排在該批最前、只有傭兵報告沒有事件也會排入）；`missions.test.ts`（交寵物目標、寵物報酬、完成行）；`client.test.ts`（新方法的路徑、方法、body）；`CompanionsScreen.test.tsx`（寵物子頁列出卡片與 `寵物 2/3`；按「攜帶」呼叫 `carryPet` 並重繪；餵食選擇中級香氣果實呼叫 `feedPet(id, "fruit-aroma-medium")`、`overOwner` 顯示 `寵物屬性不能超過玩家`；「放生」→「確定放生」才呼叫；傭兵子頁 honour-only 列沒有「僱用」、休息中可選地區並「開始戰鬥」）；`HonourPage.test.tsx`（持有數、不足時「兌換」停用、兌換後重繪）；`ChampionPanel.test.tsx`（種子冠軍卡片、挑戰後顯示戰鬥經過與 `{角色名} 順利當上冠軍`、鎖定文字）；`AdventureScreen.test.tsx`（第 6 個分頁「同伴」、「冠軍」按鈕打開對話框）；`BagScreen.test.tsx`（子頁「勳章所」、分類「勳章」、捕捉道具出現「裝備」）。
- [ ] **Step 2: 跑測試確認失敗**：`cd frontend && pnpm vitest run` → FAIL。
- [ ] **Step 3: 實作**：`pnpm generate:api`；`types.ts` 加 `PetsView`、`PetView`、`PetFeedResult`、`MercenaryOverview`、`MercenaryHireResult`、`MercenaryReport`、`HonourShop`、`ChampionView`、`ChampionChallenge`；純函式；畫面（重用 `bagWidgets` 的 `Tabs`；每次打開才載入；顏色只用 `theme.css` 變數）；`AdventureScreen` 加分頁與冠軍按鈕（`worldOpen` 同樣的開關作法）。
- [ ] **Step 4: 跑測試確認通過**：`pnpm check` 通過；`./scripts/check.sh` exit 0。
- [ ] **Step 5: 手動驗收（控制者）**：375×667 直式、暗色主題（目前只有暗色）：① 背包→商店 1 級買「捕獸網」→ 裝備頁裝進補給欄 → 在中央平原掛機，戰報出現 `捕捉到 …，捕獸網數量減少1個！！`（必要時多等幾場）；② 同伴→寵物看到 `寵物 1/3` 與卡片，按「攜帶」；回冒險，舞台玩家右前方出現寵物、戰報有 `…替你擋下了…`／`…發動攻擊…`；③ 餵食初級活力果實 → 金錢 −20、AT +2、餵食按鈕 10 秒內停用；④ 同伴→傭兵僱用 F級傭兵（3000）、選中央平原「開始戰鬥」，等 100 秒以上，戰報出現 `【傭兵】F級傭兵 戰勝 …`；⑤ 背包→勳章所看到五種勳章持有數，勳章不足時「兌換」停用（可用資料庫補發勳章再兌換一次攻擊魔石）；⑥ 冠軍對話框看到 王都冠軍 格雷德 Lv30，挑戰一次（金錢 −500），輸了再開時冠軍 HP 比上次少；⑦ 任務頁看到「每日委託：平原的寵物」與「馴獸師的考驗」；⑧ 360 與 430 寬無水平捲動、底部 6 個分頁不換行；console 無錯誤。截圖存 `docs/superpowers/screenshots/plan-9-pets.png`（寵物頁含攜帶中的卡片）、`plan-9-stage-pet.png`（舞台有寵物＋戰報寵物行）、`plan-9-mercenary.png`（出擊中的合約）、`plan-9-champion.png`（冠軍對話框與挑戰結果）。
- [ ] **Step 6: Commit**

```bash
git add frontend docs/superpowers/screenshots/plan-9-*.png
git commit -m "✨ feat: add the companions tab, the honour shop and the champion panel"
```

---

## 自我檢查

- Spec §9 寵物：最多 3 隻（T2 表、T4 捕捉上限、T6 `MaxPets`、T8 任務獎勵 `petsFull`）、捕捉道具 12%（T1 內容、T4）、餵食且不超過主人（T6，DEF 取 df／mdf 較大者、出擊值無上限照 PHP）、飢餓與老化（T6）、戰鬥代擋＋恢復傷害（T3）；傭兵：指定地區自動戰鬥、收益打折、次數制、第二條離線時間軸（T7）；榮譽商店：勳章換物品（T1 內容、T8）；冠軍：NPC 快照、500 金、贏了取代（T9）。任務的寵物獎勵（T8，計畫 8 的 `PetReward` 正式開放）、交寵物每日任務 223／225（T8）、每日報酬恢復勳章（T8）。§12 第 9 列：捕捉餵食（T4、T6）、寵物參戰（T3）、傭兵離線時間軸（T7）、勳章商店（T8）、NPC 冠軍（T9）、畫面（T5、T10）。§2／§7.1：傭兵與飢餓都在推進（離線結算）裡依時間進行（T6、T7）。§4：寵物與冠軍都走同一個 `WogBattle`（T3、T9）。§1 非目標：英雄榜、排行榜、PvP、拍賣不做。
- 與研究報告不一致處依 PHP（T1 的 `companion-table.md` 記錄）：捕捉道具在道具欄 2；傭兵寶物% 是分母懲罰、滿血、無補給、無全域怪、熟練度全額、超限算敗；飢餓每次心跳一次且只扣攜帶中的；冠軍 7 天鎖在被推翻者；任務寵物獎勵 PHP 不檢查上限；交寵物要攜帶中的。
- 型別一致：`Pet`／`PetPersonality`（T2）→ T3～T8；`PetCombatant`／`AdventureCompanions`（T3）→ T4、T7；`CaptureState`／`CapturedPet`／`PetCapture.NewPet`（T4）→ T8；`PetCare`／`PetRules`（T6）→ T7 不用、T10 文案；`MercenaryDefinition`（T1）／`MercenaryContract`（T2）→ T7、T8；`MercenaryReport`（T7）→ T10 `describeMercenaryReport`；`HonourExchange`（T1）→ T8；`ChampionSeat`（T1）→ T2、T9；`EquippedSkills`（T9 抽出）只在 T9 之後使用；事件 discriminator `petGuard`／`petAttack`／`capture`（T3、T4）→ T5。
- Review Focus 五項都有對應測試：T3 `Battle_PetRetiresWhenGuardDepleted`／`Adventure_PetGuardResetsEachFight`、T4 `Adventure_CaptureSkipsWhenPetsFull`、T7 `Mercenary_StopsWhenFightsRunOut`／`Advance_RemovesMercenaryWhenFightsRunOut`、T7 `Advance_WritesNoCompanionState_WhenCharacterUpdateConflicts`、T8 `Complete_PetRewardRejectedWhenPetsFull`。
- 順序理由：T5（前端戰報）緊接在後端開始送 `petGuard`／`petAttack`／`capture` 之後；T7 的 `AdvanceResponse.mercenary` 是新增欄位，T10 之前前端忽略它不會出錯；兩個結構性抽出（T7 `TimeoutExp`、T9 `EquippedSkills`）各自先單獨 commit。
