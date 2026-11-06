## Context

`backend/content/*.json` 是計畫 1～11 在 WOG 資料遺失時自訂的內容，由 `JsonGameContent.Load` 載入並驗證：

- id 不重複，引用都要存在。
- 怪物等級至少 1、hp 與 agi 大於 0。
- 每個地區至少一隻怪物。
- 怪物的 `missionId` 必須指向以牠為擊殺目標的任務。

`backend/EndlessBattle.Tests/Content/` 底下的表格測試釘住自訂規則，例如每地區 3 隻怪、kebab-case id、數值落在公式區間內。

`docs/research/wog45/data/*.json` 是 `export.py` 從原版 SQL 整理出來的資料：

- 怪物 464，其中任務怪 108、NPC 28。
- 道具 3139 列（1461 種名稱）。
- 職業 12；技能 597 列，共 88 種。
- 任務 345，其中每日 8。
- 配方 196、榮譽商店 142、傭兵 5。

原版資料的欄位名與現有內容檔不同（`hp`、`typeName`、`growth: "1,3"`、`kills[]`），而且有現有規則沒有的欄位（怪物 `skills`、`specialAttack`、`str`～`be`）。地點編號與 `world.json` 一致：`places.json` 的名稱本來就取自 `world.json`。

## Goals / Non-Goals

**Goals:**

- 正式內容改成由匯入腳本從原版資料產生，數值以原版為準，id 用原版編號。
- 匯入可以重跑：同一份輸入逐字產生同樣的內容檔與 `gaps.md`。
- 規則用不到的資料不會無聲消失，全部列進 `docs/research/wog45/gaps.md`。
- 每完成一個任務，遊戲內容都能載入，`check.sh` 通過。
- 技能改成「基本資料＋發動條件＋消耗＋效果」四個部分，條件、消耗、效果都能多選搭配，數值逐級寫在內容檔；職業與技能用關聯表 `job-skills.json` 連結，技能不指定職業。

**Non-Goals:**

- 技能以外的規則不改：不放寬內容驗證，也不新增怪物技能、寵物技能或任務條件。這些依 `gaps.md` 另開變更。
- 技能的 `description` 是另外寫的技能說明，匯入時留空；效果文字由條件、消耗、效果自動產生（見「效果文字自動產生」）。
- 不改 `EndlessBattle.Tests/TestContent/`，也不改用它的規則測試；例外見「硬規則」的規則 6。
- 不搬本機玩家存檔，資料庫重建即可。
- 不做 content-editor 的調整。

## Decisions

### 匯入腳本放 scripts/、用 Python、輸出固定格式

匯入腳本是 `scripts/import-wog45.py`，用 `python3 -I scripts/import-wog45.py` 執行：

- 讀 `docs/research/wog45/data/*.json` 與目前的 `backend/content/*.json`（取保留的自訂內容）。
- 寫回 `backend/content/*.json` 與 `docs/research/wog45/gaps.md`。

用 Python 是因為 `export.py` 也是 Python，原版欄位的對照表（`ELEMENTS`、`ITEM_TYPES` 等）可以直接參照。腳本不 import `export.py`，對照表各自定義，避免改研究腳本時影響匯入。

輸出格式與 `ContentFileFormatter` 相同：

- 兩格縮排。
- 頂層陣列一筆一行。
- 其餘寫成單行，分隔符 `", "` 與 `": "`。
- 不跳脫中文。

`ContentFileFormatterTests.OfficialEditableContentFiles_AreInCanonicalFormat` 會檢查這點。

紀錄依原版 id 由小到大排序，原版排完後才接保留的自訂內容，依原檔順序。`gaps.md` 依內容種類分節，節內依 id 排序，所以兩次執行的輸出逐字相同。

### id 用「種類-原版編號」

| 內容 | id |
|---|---|
| 怪物 | `monster-<id>` |
| 道具 | `item-<id>` |
| 職業 | `job-<id>` |
| 技能 | `skill-<第 1 級那一列的 id>` |
| 任務 | `mission-<id>` |
| 配方 | `recipe-<id>` |
| 榮譽商店 | `honour-<id>` |
| 傭兵 | `mercenary-<me_id>` |

規則：

- 長度都在 32 以內（`MaximumMissionIdLength`、`MaximumCompanionIdLength`）。
- 同名的道具變體（例如木刀 `item-660`、`item-928`）各自是一筆，名稱可以重複。
- 地點沿用數字 id。
- 保留的自訂內容維持原本的 kebab-case id，一看就分得出不是原版。

### 欄位對照只搬現有規則用得到的

| 內容 | 對照 | 列進 gaps.md 的 |
|---|---|---|
| 怪物 | `hp`、`sp`、`at`、`mat`、`df`、`mdf`、`agi`、`au`、`jobExp`、`meet`、`level`、`placeId`、`subPlaces`、`drops[].topr` 直接搬；`element` 的「地水火木風毒」轉 1～6，沒有則為 0；`missionId` 轉成 `mission-<id>` | `str`、`smart`、`life`、`vit`、`be`、`skills`、`specialAttack`、`npc`／`npcMessage`、`image`；驗證會拒絕的怪（等級 0、hp 或 agi 為 0） |
| 道具 | `typeName` 轉 `category`：武器、頭部、身體、手部、腳部分別轉 Weapon、Head、Body、Hand、Foot，道具欄（d_fie 5）有能力或需求的轉 Accessory、其餘轉 Material，即時使用物與補給品轉 Consumable（捕捉器 258／1837／2236 轉 Capture，倍率 1／8／13），另有 Stone、Medal、Key、RefineStone。`at`、`mat`、`df`、`mdf` 與 `bonus` 合併成 `bonus`；`requires`、`holes`、`refineMax`、`price`、`shopLevel`、`bound`、`element`、`recoverHp/Sp` 轉 `supplyHp/Sp`；魔石與精鍊石的數值取自 `magic-stones.json`、`refine-stones.json`；即時使用物依 `consumable-effects.json` 的種類對到現有 `consumable` | `skillId`／`skillRate`、裝備的回復量；對不到現有 `consumable` 種類的使用效果（行動力藥水、技能書、HP／SP 密藥、魔石寶箱的分級）；原版鑰匙在 4.1 與榮譽商店一起切換（每個禁區只能有一把鑰匙，擴充期間不能並存） |
| 職業 | `growth` 的 `"a,b"` 轉 `[a, b]`；`parentId` 有值為 2 轉、沒有為 1 轉；`unlockLevel`、`requiredProficiency` 轉 `proficiencyRequired` | `jobs-and-skills.md` 有、`jobs.json` 沒有的轉職條件 |
| 技能 | 見決策「原版技能的每級數值取自各級說明」、「原版技能的發動方式照現行規則」；每級的 `cost` 轉 `money`、`proficiency` 直接搬 | 效果目錄表達不了的技能（整個技能）；沒採用的原版欄位（被動的 `passiveRate`、常駐被動的 `duration`） |
| 任務 | 單一擊殺轉 `kill`；`needItems` 轉 `deliver`；`job`、`sex`、`requiresMissionId`、`excludesMissionId` 轉 `requirements`；`rewardMoney`、`rewardItems`、`rewardSkillId`、`rewardPet` 轉 `rewards`；`body`、`endMessage`、`npc`、`subject` 轉 `description`、`completionMessage`、`giver`、`title` | 多個擊殺目標（15 個）、`petId`、`needSkillId`、`storeId`、`areaId`、`repeatable`；引用了被排除紀錄的任務 |
| 配方、榮譽商店、傭兵 | 欄位逐一對照；榮譽商店的 `h_1`～`h_5` 依 `wog-research-world.md` 對到五種勳章道具 | 對不到現有兌換格式的商品 |

規則：

- 引用會連帶排除：一筆紀錄被排除時，引用它的掉落、材料、獎勵也要拿掉。被拿掉的這些在 `gaps.md` 寫成「因 X 被排除而移除」，現有驗證才不會因為引用不存在而失敗。
- 實作時發現對照表沒列到的欄位，一律寫進 `gaps.md`，不自行發明轉換。

### 每日任務沿用現有結構、改引用原版

原版每日任務是 222～225、271～274。它的目標池與報酬池寫在 PHP（`mission/wog_mission_*.php`），不在資料表裡；現有內容檔的每日任務是計畫 8、9 照那些 PHP 移植的。

做法：

- 每日任務改名為 `mission-222` 等，`dailyKills`、`dailyRewards`、`dailyPets` 的結構不變。
- 裡面引用的怪物與道具改成原版 id，對照表寫在腳本裡，依怪物與道具名稱對應。
- 對不到的寫進 `gaps.md`。
- 實作時發現自訂每日任務的怪物是自訂名稱，依名稱對不到原版。改成把原版 `mission/wog_mission_<id>.php` 的目標池與報酬池照抄成腳本裡的表：222、224 是固定的擊殺池；223、225 與 271～274 是「這些地點 meet 0 的一般怪」，由腳本從匯入的怪物算出。任務的標題、委託人、說明、等級取原版。271～274 的原版報酬是交易所物資（本作沒有），沿用計畫 8 的金錢報酬。
- 一般任務的 `placeId`：有擊殺目標的取目標所在的地點；沒有的用原版 `areaId`，是 0 或禁區時寫起始地點（只用來顯示）。擊殺目標是全域怪、在禁區、或沒有匯入（例如子地區遮罩為 0）的任務不匯入，任務怪跟著任務。
- 原版任務怪與任務的等級常差超過 10 級（54 隻裡 49 隻）。照原版保留，平衡之後另外處理；`BalanceTests` 原本「任務怪與任務相差 10 級內」是自訂內容的設計，自訂任務怪移除後一起拿掉。

### 保留原版沒有的自訂內容，逐筆寫原因

以下維持現狀：

- `inn.json`、`champion.json`。
- `world.json` 的 `areas`、`recommended`、`startingPlaceId`、`startingSubPlace`。`wog_area_lv` 的 id 與 `m_place` 不同，研究文件只當它是參考。

道具、怪物、傭兵只在同時符合兩點時保留：

- 現有功能需要它，例如飾品欄的飾品、捕捉道具、每日寵物任務用的 meet 0 怪物；
- 原版沒有承擔同樣角色的紀錄。

保留清單寫在腳本裡（自訂 id 與原因），`gaps.md` 的「保留的自訂內容」一節照列。其餘自訂內容在最後一步移除。

收尾時的結果：

- 保留 5 個自訂道具：魔石寶箱、大魔石寶箱（原版寶石箱的分級對不上本作寶箱）、精鍊寶盒、寵物蛋（原版沒有同樣的道具）、安全精鍊石（原版的 2284 不在匯出的資料裡），以及它們在勳章所的兌換；兌換改付原版勳章。自訂怪物、職業、技能全部由原版取代。
- 鍛造工具改成原版：拆除魔石用分離魔石LV1（`item-1378`）、打洞用劣質打孔石（`item-3361`）；`ForgeTools` 與前端 `inventory.ts` 一起改。
- 創角畫面的初階職業定位文字改用原版職業 id（`job-6`～`job-9`）。
- 釘住自訂公式的表格測試（裝備公式、怪物公式區間、每地區 3 隻、精英掉勳章、職業成長區間、騎士體質需求）一起移除，改成釘住匯入結果。`BalanceTests` 的起始子地區規則改看全部怪物，原版飛兔（`monster-9`，最大單擊 495）照使用者決定列為已知例外。

**Supersedes**：裁定彙整第 46、60、68、76、77、97 行（`docs/superpowers/specs/2026-10-22-wog-remake-rulings.md`）。那些裁定是因 WOG 資料遺失才自訂數值或不做；原版找回後，有原版值的一律以原版為準。

### 先擴充、再切換、最後收尾

內容檔之間有引用（掉落引用道具、任務引用怪物、技能引用職業），不能一次只換一個檔。順序是：

1. **擴充**：原版的道具、職業與技能、一般怪物先加在自訂內容旁邊，兩種 id 共存。
2. **切換**：配方、榮譽商店、傭兵、任務與任務怪改成原版，改引用原版 id。
3. **收尾**：移除不在保留清單上的自訂內容，改寫釘住自訂內容的表格測試。

每一步結束時 `JsonGameContent.Load(backend/content)` 都能成功、`check.sh` 通過。

切換勳章所時，自訂道具的兌換（寵物蛋、心得書、寶箱等）先保留，收尾時跟著保留清單處理；自訂傭兵與鑰匙直接換成原版。原版要先完成任務才能用的配方（`requiresMissionId`、任務配方），本作的配方沒有前置條件，照常可用，`gaps.md` 逐筆列出。擴充期間創角畫面會同時出現自訂與原版的一轉職業，收尾後恢復正常。

### 技能由四個部分組成

**Supersedes**：
- 裁定彙整第 46 行（技能每級數值公式）。
- 第 48 行（技能層級的 `trigger` 二擇一，hpBelow 時 chance 必為 0）。
- 現行 `SkillDefinition` 的單一 `SkillEffectKind`，以及技能自己指定 `JobId`。

冷卻規則不變（每級 `cooldown`，玩家出手回合計、每場歸零）。

一個技能由四個部分組成，條件、消耗、效果都可以多選，自由搭配；技能本身沒有分類。

1. **基本資料**：`name`、`icon`（選填，目前沒有圖示素材，先留欄位）、`description`、`slotType`（Active／Passive）、`timing`、`maxLevel`、`prerequisite`（選填）。
   - `description` 是給玩家看的說明文字，例如「使用火焰纏繞劍身」，與實際效果無關。效果的文字由系統依條件、消耗、效果自動產生，見決策「效果文字自動產生」。
   - `slotType` 只決定能裝在哪一種欄位：主動最多 3 個（欄位 1～3），被動最多 2 個（欄位 4～5）。欄位規則照裁定彙整第 47 行。其餘規則主動被動完全相同。
   - `prerequisite` 為 `{"skillId": ..., "level": ...}`，前置技能學到該等級才能學這個技能。
2. **發動條件**（每級的 `conditions`，可多個，全部成立才發動）：見決策「發動條件是一份清單，沒有條件就是常駐」。
3. **消耗**（每級的 `costs`，可多個）：見決策「消耗不夠就不能發動」。
4. **效果**（每級的 `effects`，可多個）：見決策「效果目錄」。

內容檔的形狀：

```json
{"id": "skill-1", "name": "爆氣", "icon": null, "description": "", "slotType": "Active", "timing": "BeforeAttack",
 "maxLevel": 10, "prerequisite": null,
 "levels": [{"level": 1, "money": 500, "proficiency": 35, "cooldown": 0,
             "conditions": [{"kind": "Chance", "percent": 40}],
             "costs": [{"kind": "SkillPoints", "amount": 2}],
             "effects": [{"kind": "StatChange", "target": "Caster", "stat": "attack", "percent": 1, "rounds": 3}]}]}
```

規則：

- 同一個技能的每一級，條件、消耗、效果的種類與順序都必須相同，只有數值不同。
- 技能明寫 `maxLevel`，`levels` 必須剛好是第 1 級到 `maxLevel` 各一級，不然載入失敗。
- 每一級的學習金錢、熟練度、冷卻、條件數值、消耗數值、效果數值都各自設定。
- Domain 不再有任何「依等級算數值」的公式：原本的 `1 + 0.01·lv`、發動率 `30 + 10·lv` 都移除。發動率公式只在匯入腳本裡，用來補原版缺的值。
- 戰鬥只依條件、消耗、效果的種類與數值計算，`SkillCaster` 不再出現技能名稱或寫死的係數。內容相同的兩個技能，戰鬥結果相同；主動與被動也一樣。
- 之後 content-editor 的技能表單照這個形狀做：先設等級上限，再逐級填數值；條件、消耗、效果都用選的再填數值。

### 職業與技能用關聯表連結，技能不指定職業

- 技能不再有 `jobId`。職業與技能的關係是一張獨立的關聯表 `backend/content/job-skills.json`，取代技能原本的職業欄位：

  ```json
  {"jobSkills": [{"jobId": "job-6", "skillId": "skill-1"}]}
  ```

  - 一列代表「這個職業能學這個技能」。同一個技能可以出現在多個職業的列裡，同一組職業與技能不能重複。
  - 載入時檢查職業與技能都存在。
  - 職業與技能本身的檔案都不需要知道對方。
- 學習規則：
  - 關聯表裡要有「**目前職業**＋這個技能」這一列（取代現行 `SkillLearning` 的「技能的職業＝目前職業」），學習時扣目前職業的技能熟練度。
  - 前置技能要達到指定等級。
  - 其餘照舊：金錢、熟練度、等級上限。
- 學過的技能轉職後仍可裝備，照現行規則。
- 怪物之後用另一張關聯表 `monster-skills.json`（`monsterId`、`skillId`、`level`），直接指定等級，不需要金錢與熟練度；怪物使用金錢消耗的技能時，視為金錢無限。怪物技能的戰鬥是後續變更，這次只定資料形狀，不建這張表。

### 發動時機

`timing` 三選一，條件與效果怎麼搭由設計的人決定，不限制：

- `BeforeAttack`：自己出手前。
- `AfterHit`：自己命中後，可以用「這次造成的傷害」當效果的基準，例如嗜血。
- `RoundEnd`：回合結束時。

規則：

- 「這次造成的傷害」在其他時機或沒命中時是 0。
- 不設「戰鬥結束」：原版戰鬥結束才發動的技能是神頌曲、地獄禮讚、詐騙（組隊）和精密陷阱（捕捉寵物），這次都不做。

### 發動條件是一份清單，沒有條件就是常駐

| 條件 | 參數 | 例子 |
|---|---|---|
| `Chance` 機率 | `percent` 1～100 | 爆氣第 1 級 40%；施毒 25% |
| `HitPointsBelow` 自己 HP 低於 | `percent` 1～99（最大 HP 的比例） | 魔力爆發：30% |
| `HitPointsBelowOpponent` 自己 HP 比對手低 | 無 | 復仇 |
| `StatAbove` 自己能力比對手高 | `stat`（strength、intelligence、vitality、charisma、magicAttack） | 劍雨：魔攻；煉獄：智力 |

規則：

- 全部成立才發動。例如「HP 低於 30% 而且 30% 機率」寫成 `[HitPointsBelow 30, Chance 30]`。
- 機率也是條件：只要發動條件成立，所有效果都發動。例如施毒寫成 `[Chance 25]`，效果是「對手物攻降低」與「持續傷害」兩個。
- 有些效果種類本身可以帶機率（`StatChange` 對對手時、`BonusDamage`、`Stun`、`Silence`），這個機率是效果的一環：技能發動了效果才有機會生效。它不是發動條件，也不影響同一技能的其他效果。擲骰的時間點依種類不同：
  - `StatChange`、`BonusDamage`：技能發動後擲一次，沒擲中這次發動就沒有這個效果。
  - `Stun`：持續期間，對手每次出手前擲一次，擲中那一下無法行動。例如斯巴達寫成條件 `[StatAbove strength, Chance 40]`，效果「物攻必殺率 +20%」與「`Stun` 機率 12%」：條件成立時兩個效果都發動，必殺率一定提升，對手每次出手前再擲 12%。
  - `Silence`：機率先存在戰鬥修正裡；怪物還不會用技能，要等怪物技能的後續變更才會擲。
- 哪些效果種類能帶機率由效果目錄決定（下表「參數」欄有 `chance` 的）。發動後擲一次的那幾種（`StatChange`、`BonusDamage`）在程式裡只有 Domain 的 `SkillPartRules.EffectChance` 一份對照，內容驗證、戰鬥擲骰共用；`Stun`、`Silence` 的機率是效果本身的參數，範圍各自驗證。之後要讓其他種類也在發動後擲一次，改目錄、`SkillPartRules.EffectChance`，以及讀取器讀 `chance` 的那一種效果。
- 沒有任何條件就是常駐：裝上就生效、不擲骰。這一級不能有消耗，效果的 rounds 與冷卻都必須是 0，否則載入失敗。
- 持續中不重複發動：技能發動後，在它最長的效果結束前，即使條件都成立也不會再發動，也不判斷條件、不擲骰。這是現行 `SkillCaster` 的行為，新格式照樣保留。冷卻另外計算。
- 判斷順序：
  1. 持續中、冷卻中：跳過。
  2. 不需要亂數的條件。
  3. 消耗夠不夠。
  4. 最後才擲 `Chance`。

  前面不成立時不耗用亂數，固定種子可以重現（裁定彙整第 45 行）。
- 回復已滿不發動：一個技能的效果如果全部是回復，而且要回復的資源已經滿了，就不發動。這是現行治癒、嗜血的規則，改成依效果種類判斷。

### 消耗不夠就不能發動

| 消耗 | 參數 | 例子 |
|---|---|---|
| `HitPoints` 生命 | `amount`（固定值）或 `percent`（最大 HP 的比例） | 滅世：45%；犧牲：10% |
| `SkillPoints` SP | `amount`、`percent`（最大 SP 的比例）或 `all`（全部） | 爆氣：2；魔力爆發：全部 |
| `Money` 金錢 | `amount` | — |

規則：

- 百分比一律以最大值為基準。
- 不夠扣就不能發動，固定值與百分比都一樣：
  - HP 消耗後至少要剩 1，也就是目前 HP 必須大於消耗量。
  - SP 與金錢要大於等於消耗量；`all` 要至少有 1 點 SP。
- 發動時先算出效果要用的數值（例如魔力爆發的「魔攻 + SP×0.1」用扣之前的 SP），再扣消耗、套用效果。
- 戰鬥紀錄的技能事件列出這次扣了哪些消耗。怪物的金錢消耗不扣，視為無限。
- 金錢消耗只在冒險戰鬥實際扣錢（冒險會更新角色的錢）；冠軍戰與傭兵出戰把金錢視為不限、不扣。原版技能沒有金錢消耗，這個限制目前不影響任何技能。
- 「能力比對手高」比的是雙方的基本能力；原版怪物匯入時帶 `str`、`smart`、`vit`、`be`，自訂怪物沒寫的當 0。
- 持續回合一律從發動的那一回合算起（含發動回合），不分時機，同一回合發動的效果就同時結束。代價是 `AfterHit`、`RoundEnd` 發動的持續效果在發動回合內作用得比較少；`rounds` 0 或 1 時完全沒有作用，設計技能時避開這種組合。等最終的技能設計定案再看要不要調整。
- 不做「該回合不攻擊」：原版只有魔力吸收用到。這次魔力吸收照樣匯入它的回復效果，不實作不攻擊，並在 `gaps.md` 註明。

### 效果目錄

每種效果有一個分類，只用來在編輯器裡分組，不是技能的屬性。每個效果都有 `rounds`：0 是當下生效一次，大於 0 是持續該回合數（含發動回合）。

| 效果分類 | 效果種類 | 參數 | 原版例子 |
|---|---|---|---|
| 能力 | `StatChange` 能力增減 | `target`（Caster／Opponent）、`stat`；`percent`、`amount`、`scaling` 三擇一（可為負）；對對手時可加 `chance` | 爆氣：自己物攻 +1%；石人陣：自己物防 +150；靡靡之音：對手速度 −1%；力量之舞：自己物攻 + 魅力×5%；明王附帶：對手物防 −6%，機率 10% |
| 傷害 | `BonusDamage` 追加傷害 | `attackKind`（Physical／Magic／Any）、`scaling`（可多項相加）；可加 `chance` | 戰神之怒：物理，力量×45%；劍雨：Any，力量×2% + 智力×2%；暗器附帶：物理，敏捷×15%，機率 20% |
| 傷害 | `MultiHit` 連續攻擊 | `attackKind`、`hits` | 流星拳：物理 5 次 |
| 傷害 | `PercentDamage` 削減對手 HP | `percent`（對手目前 HP 的比例） | 星芒：10% |
| 傷害 | `DamageOverTime` 持續傷害 | `scaling`（每回合） | 施毒：智力×10% |
| 傷害 | `ReduceSkillPoints` 削減對手 SP | `amount` 或 `percentOfMaximum` | 燃燒魔力：20 點 |
| 防護 | `DamageReduction` 減傷 | `attackKind`、`percent` | 防禦心得：物理 2% |
| 回復 | `Restore` 回復 | `resource`（HitPoints／SkillPoints）；`scaling`、`percentOfMaximum`、`percentOfDamage` 三擇一 | 治癒術：HP 智力×1；嗜血：HP 傷害×2%；魔力吸收：SP 最大值×2% |
| 控制 | `Stun` 使對手無法行動 | `chance` 1～100（效果發動後擲，擲中才生效） | 暈擊：機率 3%；斯巴達附帶：機率 12% |
| 控制 | `Silence` 封鎖對手技能 | `chance`、`rounds`（封鎖期間對手的技能不發動、不判斷條件） | 葬心：機率 5%；豪鬼附帶：機率 10% |
| 屬性 | `AttackElement`、`DefenseElement` 攻擊／防禦屬性轉換 | `element`（Earth、Water、Fire、Wood、Wind、Poison） | 魔劍-地、魔盾-水 |
| 屬性 | `ElementAdvantage` 剋制時屬性傷害加成 | `percent` | 屬性迫害：5% |

參數的細節：

- `stat` 可用：attack、magicAttack、defense、magicDefense、agility、charisma、hitRate、evasion、physicalCriticalChance、magicCriticalChance、physicalCriticalCap、magicCriticalCap、escapeRate。
- `scaling` 是 `[{"basis": ..., "ratio": ...}]`。`basis` 可用 strength、intelligence、vitality、agility、charisma、faith、level、attack、magicAttack、defense、skillPoints、damageDealt。
- `target` 只有 `StatChange` 可選；傷害、控制類一律對對手；回復、防護、屬性類一律對自己。
- 效果的 `chance` 是 1～100；目錄沒列 `chance` 的種類寫了就載入失敗。哪些能力是百分點（只能用 `amount` 或 `scaling`）同樣只有 `SkillPartRules.IsPercentagePoint` 一份，驗證與效果文字共用。
- 常駐（沒有條件）的效果，每到技能的時機都生效：能力增減、減傷、屬性整場持續；追加傷害、削減 SP 這類「一次性」效果每次到時機都再生效一次。例如毒傷陷阱常駐，每次出手都追加傷害。
- `Silence` 與 `ReduceSkillPoints` 對任何施放者的對手都成立。目前怪物不會用技能，玩家用這兩種效果時戰鬥裡看不出作用，要等怪物技能的後續變更；但規則與測試這次先完成，用「被封鎖的一方不發動技能」的單元測試釘住。
- 疊加：同一個技能裡同一項能力的百分比**相加**（兩個 +5% 是 +10%）；不同技能之間**相乘**（1.05 × 1.05）。固定值、`scaling` 一律相加。

### 效果文字自動產生

- Application 依條件、消耗、效果產生每一級的效果文字，例如「機率 40%｜消耗 SP 2｜自己物攻 +1%，3 回合」。遊戲 API 的技能回應帶 `summary`（目前等級與下一級各一份），技能頁顯示 `summary` 與 `description`。
- 原版的 `text` 其實是效果說明，不是 `description`：匯入時 `description` 留空，`text` 只用來擷取數值。
- 戰鬥紀錄的技能事件加上這次的消耗，畫面一起顯示，例如「爆氣（消耗 SP 2）」。

### 原版技能的發動方式照現行規則

原版的主動技能發動率由玩家裝備時自訂，被動用 `passiveRate` 擲骰。這次照現行規則：

- **原版主動技能**：
  - 說明裡寫了適用於全部效果的機率（例如施毒「25% 機率使敵人…」），就用它當 `Chance` 條件。
  - 沒寫的（包括機率只寫在 `Stun` 上的，例如暈擊），每級加 `Chance min(100, 30 + 10·lv)`（第 8 級起是 100），原版資料沒有這個值。
  - 說明帶的其他條件（例如「魔攻比敵人高時」）放在 `Chance` 前面。
  - 原版的 `sp` 轉成 `SkillPoints amount` 消耗，`duration` 轉成各效果的 `rounds`。
- **原版被動技能**：
  - 說明沒寫條件的就是常駐，不加 `Chance`、沒有消耗、rounds 為 0。
  - 原版的 `passiveRate`、`sp`、`duration` 不採用，`gaps.md` 的技能一節逐筆註明「原版被動發動率 X%，本作常駐」。
  - 說明帶條件的寫成條件，`sp`、`duration` 照搬。
- 說明裡的機率怎麼轉：
  - 寫在「使敵人無法行動」上的機率，轉成 `Stun` 效果自己的 `chance`，例如暈擊、斯巴達、瘟疫的微笑。這類技能另外照主動技能的規則加 `Chance` 條件。
  - 涵蓋全部效果的機率，轉成 `Chance` 條件，例如施毒、鋒針陷阱。
  - 寫在追加傷害、對手能力降低、封鎖技能上的機率，轉成那個效果自己的 `chance`，例如暗器、明王、豪鬼、葬心。
  - 寫在目錄不支援機率的效果上的，整個技能不匯入，寫進 `gaps.md`；依說明文字預估這次沒有。
- 原版有條件會改變機率的（假死：智力比敵人高時機率 +5%），寫進 `gaps.md`。
- `job-skills.json` 依原版技能的 `jobId` 產生；原版的 `need_sid` 轉成 `prerequisite`（指向那一列所屬的技能與等級）。

### 原版技能的每級數值取自各級說明

原版的效果數值只寫在每一級的 `text`，例如「物攻上升1%」到「物攻上升10%」。研究文件 `wog-research-battle.md` §3.4 只列出部分公式。做法：

- 匯入腳本為 88 種技能各寫一列樣板：條件、消耗、效果的種類，以及從 `text` 擷取數值的正規表示式。
- 每一級都用同一個樣板擷取；擷取失敗就是錯誤，腳本以非 0 結束，不猜數字。
- 樣板與 §3.4 有公式的技能逐一比對，數字對不上時以 `text` 為準，並在 `gaps.md` 註記差異。
- 表達不了的技能整個不匯入，寫進 `gaps.md`：
  - 寵物技能，等寵物系統的調整確定後再做：物攻命令、魔攻命令、戰意驅使、狂性連結、魔性連結、閃電痛擊、獸靈、精密陷阱。
  - 組隊技能，本作沒有隊友，不做：神頌曲、地獄禮讚、詐騙、友情之舞。
  - 條件改變機率的：假死。

這裡列的是依說明文字預估的，實際以腳本跑出的 `gaps.md` 為準。

### 舊效果先等價搬到新格式，再移除

1. **擴充**：載入同時接受舊格式（`effect`、`trigger`、`jobId`、每級 `chance`、`sp`、`rounds`）與新格式。
2. **搬移**：把 `TestContent/skills.json` 與正式內容的 16 個自訂技能改寫成數值等價的新格式，並依原本的 `jobId` 建立 `job-skills.json`（正式與 TestContent 各一份）。對照：
   - `trigger: chance` 與每級 `chance` 轉成 `Chance` 條件；`hpBelow` 轉成 `HitPointsBelow`；常駐被動不寫條件。
   - `sp` 轉成 `SkillPoints` 消耗；`rounds` 轉成各效果的 `rounds`。
   - 爆氣第 n 級寫成 `StatChange Caster attack percent n`。
   - 戰神之怒寫成 `BonusDamage scaling strength 0.45`，再加 `StatChange Caster hitRate amount (−75 + 5n)`。

   用到技能的戰鬥測試（`SkillCasterTests`、`WogBattleSkillTests`、`WogBattlePetTests`、`AdventureSimulatorTests`、`ChampionDuelTests`）不改斷言也要通過，證明搬移沒有改變行為。
3. **收尾**：移除 `SkillEffectKind`、`SkillTrigger`、技能的 `JobId` 與舊格式。

遊戲 API 的技能回應：

- 移除 `effect`、`trigger`、`chance`、`jobId`。
- 改成 `slotType`、`summary`、`description`、`icon`。
- 以 `pnpm generate:api` 重新產生 `frontend/src/api/schema.ts`；技能頁改用 `summary`。

### 新增 migration 改職業預設值

新增 `backend/db/ddl/V000000016__default_job_id.sql`：

```sql
ALTER TABLE characters ALTER COLUMN job_id SET DEFAULT 'job-6';
```

只改預設值，不改資料、不 DROP，`check-sql.sh` 會通過。

套用步驟：

1. 寫 migration。
2. `docker compose up -d`。
3. `./scripts/scaffold-db.sh`。只改預設值的話，產生的 model 應該不變；有變就一起 commit。

現有本機存檔裡的舊 id 不轉換。開發時用 `docker compose down -v` 再 `up -d` 重建。

### 硬規則

- **規則 6**：內容只放 `backend/content/*.json`，`TestContent/` 不動。例外：技能改成四部分時 `TestContent/skills.json` 改寫成新格式並新增 `job-skills.json`；鍛造工具的 id 寫死在 `ForgeTools`，改成原版 `item-1378`、`item-3361` 時 `TestContent/items.json` 同名的兩筆跟著改 id。
- **規則 3**：新增的 migration 用下一個版本號 000000016，不改已 commit 的 migration。
- **規則 4**：不手改 scaffold 產生檔。
- **規則 1**：技能條件、消耗、效果的計算都在 Domain（`SkillCaster`、`BattleModifiers`、`RoundResolver`、`SkillLearning`），Infrastructure 只負責讀取與驗證；效果文字是顯示用，放 Application。
- **規則 2**：Domain 不新增任何引用。
- **遊戲前端規則 9**：`frontend/src/api/schema.ts` 用 `pnpm generate:api` 重新產生，不手改。
- 不涉及：admin 路由。

## Implementation Contract

**Behavior**：

- 遊戲啟動後的怪物、道具、職業、技能、任務都是原版內容。例如新角色選「戰士」（`job-6`），到中央平原香茅綠地會遇到史萊姆（`monster-1`，Lv2、HP 32、風屬性）。
- 戰士學「爆氣」後，第 n 級的物攻是 +n%（原版說明的數字），由內容檔的 `StatChange Caster attack percent n` 決定；改內容檔的參數就改變戰鬥結果，不必改程式。
- 營運者執行 `python3 -I scripts/import-wog45.py` 會重新產生內容檔與 `gaps.md`，資料沒變時 `git diff` 為空。

**Interface / data shape**：

- `scripts/import-wog45.py` 不帶參數，以 repo 根目錄為基準，成功時 exit 0。
- 內容檔的格式與欄位名不變（仍是現有的 `hp`、`at`、`topr`）。改成完整名稱是 content-editor 的範圍，而且只改 admin API，不改儲存格式。
- `docs/research/wog45/gaps.md` 依內容種類分節，每列寫：原版 id、名稱、未搬的欄位或排除原因。最後一節是「保留的自訂內容」。

**Failure modes**：

- 原版資料缺少腳本必要的欄位時，腳本以非 0 結束、印出檔名與 id，不寫任何檔案。
- 對照後的紀錄若會被 `JsonGameContent` 拒絕，腳本自己先排除，並寫進 `gaps.md`，不靠載入時才失敗。

**Acceptance criteria**：

- `WogImportTests`：
  - 正式內容能載入。
  - 抽樣紀錄等於原版值：`monster-1` 史萊姆、`item-1` 布衣、`job-6` 戰士的成長、`mission-1` 製作漁網的交付。
  - 數量等於原版數量減去 `gaps.md` 排除的數量。
- 連跑兩次匯入腳本後 `git diff --exit-code backend/content docs/research/wog45/gaps.md` 為 0。
- `./scripts/check.sh` exit 0。
- 手動：重建資料庫、創角、在中央平原冒險一輪。

**Scope**：

- 範圍內：匯入腳本、正式內容檔、`gaps.md`、職業預設值的 migration、改寫釘住正式內容的測試、研究文件索引。
- 範圍內（技能）：
  - 四個部分的技能模型，以及條件、消耗、效果目錄裡每一種的戰鬥計算。
  - 職業與技能的關聯表 `job-skills.json`，以及前置技能的學習規則。
  - 技能內容的讀取與驗證、TestContent 技能的等價搬移。
  - 效果文字自動產生、技能回應與技能頁、戰鬥紀錄的消耗顯示。
- 範圍外：技能以外的規則與驗證、怪物技能、寵物技能、admin。

## Risks / Trade-offs

- **[風險] 原版數值不通過 `BalanceTests`。** 例如起始子地區的怪打掉新角色一半以上的 HP。
  **處理**：該測試失敗時暫停，回報使用者是要調整起始地區、放寬門檻，還是接受；不自行改門檻。
- **[風險] 說明文字和原版程式的實際公式不一致。** 參數取自 `text`，研究文件只有部分公式。
  **處理**：有公式的逐一比對，不一致的寫進 `gaps.md`；之後找到原始碼再修正。
- **[風險] 寵物系職業（獸師）幾乎沒有技能。** 估計 88 種技能裡有 13 種寫進 `gaps.md`（寵物 8、組隊 4、假死 1；依說明文字預估），實際數量以腳本輸出為準。獸師仍有毒傷陷阱、鋒針陷阱、獸騎可用。
  **處理**：寵物技能是後續變更，也沿用這次的效果分類。
- **[取捨] 新增的效果都要碰戰鬥公式。** 迴避、魔法必殺、必殺上限、屬性轉換、持續傷害、消耗都是新機制，每一種都有 `SkillEffectTests` 釘住。
- **[取捨] 只有部分效果種類能帶自己的機率。** 模型維持「條件成立＝所有效果發動」，機率是個別效果的一環；目前開放 `StatChange`（對對手）、`BonusDamage`、`Stun`、`Silence`，之後要開放其他種類只改效果目錄。
- **[取捨] `Silence`、`ReduceSkillPoints` 先做、暫時看不出作用。** 葬心、豪鬼、燃燒魔力因此能先匯入；怪物會用技能後才在戰鬥裡有作用。
- **[取捨] 擴充期間自訂與原版內容並存。** 中間的 commit 遊戲畫面會怪（兩個戰士），換來每一步都能驗證、都能 commit。
- **[取捨] 內容檔變大。** 例如 `items.json` 約 3000 行，是估計值，以匯入後 `wc -l` 為準。依固定格式一筆一行，diff 仍然看得懂。
- **[取捨] 匯入腳本與後台發布會互相覆蓋。** 後台發布直接改 `backend/content/`，之後再跑匯入腳本會蓋掉後台的修改。
  **處理**：匯入腳本只在「搬移＋補規則」階段使用，後續補規則的變更也重跑它，把新支援的欄位帶進來；content-editor 開始後，正式內容以後台為準，不再重跑。
  所以 `check.sh` 不檢查「內容檔等於匯入結果」，可重跑只在這些變更裡手動驗證。
