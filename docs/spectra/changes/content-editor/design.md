## Context

- admin 的內容 API（`backend/EndlessBattle.Api/Endpoints/Admin/AdminContentEndpoints.cs`）目前用一組 `/admin/content/{kind}` 路由處理六種內容，請求與回應本體都是 `JsonObject`，欄位名稱就是內容檔的縮寫（`at`、`mdf`、`be`、`topr`、`meet`）。這是上一個變更 content-admin 的決定「以 JSON 物件逐筆讀寫」。
- 因為本體沒有型別，admin 的 OpenAPI 文件裡紀錄只是一般物件；admin 前端（`admin/src/content/recordFields.ts`、`RecordForm.tsx`）把每個欄位猜成文字、數字、布林或 JSON 字串，關聯欄位要手填 id，巢狀欄位以 JSON 字串編輯。
- 內容檔的讀取與檢查在 `backend/EndlessBattle.Infrastructure/Content/JsonGameContent.cs`，檔案 DTO 都是 private（`ItemEntry`、`JobEntry`、`SkillEntry`、`PlaceEntry`…；怪物直接反序列化成 Domain 的 `MonsterDefinition`）。內容檔寫出統一經過 `ContentFileFormatter`（兩格縮排、陣列一筆一行、原始數字文字保留）。
- 兩個前端的 `pnpm generate:api` 都要先啟動 API，再從 `http://localhost:5062/openapi/*.json` 抓文件；沒有任何檢查會發現 `schema.ts` 過期。
- 遊戲前端直接把產生的型別給畫面用（`frontend/src/game/types.ts`），這次不改，留給下一個變更。
- 參考做法：`~/Project/game-web` 的 `src/data/dto/*Converter.ts`（`XxxConverter.toVo(dto)`）與 `src/data/vo/*Vo.ts`（private constructor ＋ `static create(params)` 的 class）。
- 實際內容（2026-11-07 量測自 `backend/content/`，共 12 個內容檔）：道具 3127 筆（`items.json` 744,347 bytes）、怪物 461（106 隻有任務、27 隻 NPC、69 隻沒設子地區、1 隻全域）、職業 12、技能 76（共 509 級）、大陸 5、地區 21；道具依分類只出現部分欄位（見「道具依分類顯示的欄位」）。
- 這份設計寫於內容匯入之前（2026-11-05），`wog45-content-import`、`monster-skills`、`mission-objectives` 之後依現況更新過：技能不再有 `type`、`effect`、`trigger`、`jobId`，道具沒有 `jobId`，多了關聯表與 NPC。

## Goals / Non-Goals

**Goals:**

- 分層清楚：內容檔格式只存在 Infrastructure；admin API 是有型別、完整欄位名稱的合約；admin 前端只在 Converter 碰 API 的形狀，畫面只用 VO。改其中一層的格式，只需要改相鄰的轉換。
- 內容檔讀成紀錄再寫回不失真，任何沒被紀錄涵蓋的欄位都會被測試擋下。
- 六種內容都能用表單完整編輯（包含技能的發動條件、消耗、效果，與怪物能用的技能），不需要知道 id 或 JSON 寫法。
- 列表可搜尋、可篩選，關聯欄位顯示名稱。
- 型別檔不必啟動 API 就能產生，過期會被 `check.sh` 擋下。

**Non-Goals:**

- 編輯任務（`missions.json`）；這次只提供唯讀的任務列表。配方、榮譽商店、傭兵、旅館也不開放編輯。
- 遊戲 API 與遊戲前端改成 DTO／VO；下一個變更 `game-api-dto-vo` 處理，這次只把遊戲前端的 `generate:api` 改成從檔案產生。
- 改內容檔的格式或欄位名稱：內容檔維持現狀，只當儲存載體。
- 平衡報告的改版。
- 刪除紀錄、多份草稿、權限（同 content-admin）。
- 保留原始 JSON 編輯的方式。
- 改動匯入腳本 `scripts/import-wog45.py` 或釘住原版數值的內容測試（見 Risks）。

## Decisions

### 三層資料：內容檔、API 紀錄、畫面 VO

**Supersedes**: content-admin / 以 JSON 物件逐筆讀寫

```
backend/content/*.json（縮寫欄位，儲存格式）
      ↕  Infrastructure：每種內容一個 codec（檔案 JSON ↔ 紀錄）
Application 的內容紀錄（完整欄位名稱）＝ admin API 的請求與回應本體
      ↓  建置時產生 backend/openapi/EndlessBattle.Api_admin.json → admin/src/api/schema.ts（DTO 型別）
admin：XxxConverter.toVo(dto) / toDto(vo)
      ↕
admin：XxxVo class → 畫面
```

- 內容紀錄放在 `backend/EndlessBattle.Application/Admin/Content/`，是純 C# record，不含檔案格式的知識；`ContentRecordService<TRecord>` 與 endpoint 只看見紀錄，`ContentAdminService` 只處理草稿狀態、發布與捨棄，`JsonObject` 與內容檔檔名不離開 Infrastructure。
- codec 放在 `backend/EndlessBattle.Infrastructure/Content/`，負責欄位名稱對應、省略規則（數值為 0 的加成不寫、沒有的巢狀物件不寫、`parentId` 為 null 時照寫 `null`）與欄位順序，寫出仍經過 `ContentFileFormatter`。
- 紀錄就是 admin API 的 response／request 格式（API DTO）。儲存載體與 response 之間的邊界在 Infrastructure：現在從 JSON 檔轉成紀錄，之後改成資料庫時只換 Infrastructure 的讀寫，紀錄的形狀不變，前端也不受影響。紀錄放在 Application，Infrastructure 才能直接產生它（依賴方向 Infrastructure → Application）。
- 替代方案：維持 `JsonObject`，只在前端加 Converter。否決，因為前端「忽略不認識的欄位」後存檔會把那些欄位丟掉，必須由後端提供完整而確定的形狀。

### 關聯表併進怪物與技能的紀錄

編輯怪物時直接選它能用的技能（使用者決定），所以 `monster-skills.json` 不是獨立的內容種類，而是怪物紀錄的 `skills`：每列 `{ skillId, level, activationRate }`。`job-skills.json` 同樣併進技能紀錄的 `jobIds`，對應舊設計「技能的職業下拉」，現況是每個技能恰好一個職業，但關聯表允許多個，所以用多選。

- 內容種類仍是六種。寫一筆怪物會改 `monsters.json` 與 `monster-skills.json`，寫一筆技能會改 `skills.json` 與 `job-skills.json`；寫入草稿要同時成功，不能只寫一個檔。
- 寫回關聯表時依主檔的順序重新產生。已量測：`monster-skills.json` 依怪物分組且順序與 `monsters.json` 相同，`job-skills.json` 順序與 `skills.json` 相同，所以來回轉換仍能逐字相同；雙向轉換測試會驗證這點。
- **寫入時就檢查引用（使用者決定）**：怪物的 `skills[].skillId` 指向不存在的技能、技能的 `jobIds[]` 指向不存在的職業，回 400 並指出欄位（例如 `skills[0].skillId`），草稿不變。這是「寫入後才檢查、不通過照樣保存」的例外，因為這兩個引用是在同一筆表單裡選的，沒有「之後才補上另一筆」的情況。其他規則（等級超過 `maxLevel`、NPC 不能有技能）照舊由 `JsonGameContent.Load` 回報。
- 替代方案：關聯表各開一個頁面。否決，使用者要在編輯怪物時選；而且 664 筆要靠篩選才找得到。

### 欄位完整名稱對照

紀錄與 API 一律用完整名稱；內容檔不變。

| 內容檔 | 紀錄與 API | 中文 |
| --- | --- | --- |
| `str`、`smart`、`life`、`vit`、`agi`、`au`、`be` | `strength`、`intelligence`、`life`、`vitality`、`agility`、`charisma`、`faith` | 力量、智力、生命、體質、敏捷、魅力、信仰 |
| `at`、`mat`、`df`、`mdf`、`hpMax` | `attack`、`magicAttack`、`defense`、`magicDefense`、`maxHitPoints` | 攻擊、魔攻、防禦、魔防、HP 上限 |
| 怪物 `hp`、`sp` | `hitPoints`、`skillPoints` | HP、SP |
| 怪物 `jobExp`、`meet` | `jobExperience`、`encounterRate` | 職業經驗、遇敵率 |
| 怪物 `subPlaces` | `subPlaceIndexes` | 出現的子地區 |
| 怪物 `missionId` | `exclusiveMissionId` | 限定任務（只在該任務進行中出現） |
| 怪物 `npc`、`npcMessage` | `isNpc`、`npcMessage` | 是否 NPC、遭遇訊息 |
| `monster-skills.json`、道具 `weaponSkill` 的 `rate` | `activationRate` | 發動率 |
| 掉落 `topr` | `dropRarity` | 掉落難度（越大越難掉） |
| 職業 `parentId`、`statRequirements[].stat` | `parentJobId`、`statRequirements[].stat`（列舉 `Strength`…`Faith`） | 上級職業、能力需求 |
| 成長與精鍊的 `[min, max]` | `{ minimum, maximum }` | 最小、最大 |
| 技能等級 `money`、`proficiency` | `learningCost`、`proficiencyRequired` | 學習費用、所需熟練度 |
| 地區 `subPlaces`、`recommended` | `subPlaceNames`、`recommendedLevel { minimum, maximum }` | 子地區、推薦等級 |
| 道具 `supplyHp`、`supplySp` | `supplyHitPoints`、`supplySkillPoints` | 回復 HP、回復 SP |
| 魔石 `min`、`max` | `minimum`、`maximum` | 最小、最大 |
| 屬性 `element`（0～6） | 列舉 `None`、`Earth`、`Water`、`Fire`、`Wood`、`Wind`、`Poison` | 無、地、水、火、木、風、毒（依 `ElementTable` 註解） |

其餘欄位（`id`、`name`、`level`、`tier`、`price`、`holes`、`slotType`、`timing`、`description`、`maxLevel`、`cooldown`、`prerequisite`、`boxClass`…）名稱已完整，照用。怪物沒有 `life`，加成才有。列舉在 API 一律以字串表示（`JsonStringEnumConverter`），例如道具分類 `Weapon`、消耗品種類 `Supply`、技能欄位 `Active`、時機 `BeforeAttack`、密藥種類 `Exp`。

### 內容檔與紀錄雙向轉換不失真

每個正式內容檔（`items.json`、`monsters.json`、`monster-skills.json`、`jobs.json`、`skills.json`、`job-skills.json`、`world.json`）經 codec 讀成紀錄、再寫回，結果必須與原檔逐字相同；`EndlessBattle.Tests/TestContent/` 的同名檔也一樣。這條測試同時保證：codec 涵蓋了內容檔現有的每個欄位、省略規則與欄位順序和現有檔案一致。`world.json` 的 `startingPlaceId`、`startingSubPlace` 不屬於任何紀錄，寫回時原樣保留。

### 技能的條件、消耗、效果

技能依 `docs/spectra/specs/skill-effects/spec.md` 的形狀：基本資料＋每一級各自的條件、消耗、效果。紀錄以 `kind` 區分種類（`[JsonPolymorphic]`），欄位與 `SkillPartsReader` 接受的相同，名稱已經完整，不改名。

| 部分 | 種類（`kind`） | 欄位 |
| --- | --- | --- |
| 條件 | `Chance`、`HitPointsBelow` | `percent` |
| 條件 | `HitPointsBelowOpponent` | 無 |
| 條件 | `StatAbove` | `stat` |
| 消耗 | `HitPoints`、`SkillPoints` | `amount`、`percent` 擇一；`SkillPoints` 另可 `all` |
| 消耗 | `Money` | `amount` |
| 效果 | `StatChange` | `target`、`stat`、`percent`／`amount`／`scaling` 擇一、`chance`（可省略） |
| 效果 | `BonusDamage` | `attackKind`、`scaling`、`chance`（可省略） |
| 效果 | `MultiHit` | `attackKind`、`hits` |
| 效果 | `DamageReduction` | `attackKind`、`percent` |
| 效果 | `Restore` | `resource`、`scaling`／`percentOfDamage`／`percentOfMaximum` 擇一 |
| 效果 | `Stun` | `chance`、`chanceByStat { stat, percent }`（可省略） |
| 效果 | `Silence` | `chance` |
| 效果 | `PercentDamage`、`ElementAdvantage` | `percent` |
| 效果 | `DamageOverTime` | `scaling` |
| 效果 | `ReduceSkillPoints` | `amount`、`percentOfMaximum` 擇一 |
| 效果 | `AttackElement`、`DefenseElement` | `element` |

每個效果都有 `rounds`。`scaling` 是 `{ basis, ratio }` 的清單。實作時以 `SkillPartsReader` 為準；上表與它不同就修正這張表。

### 每種內容各自註冊有型別的路由

OpenAPI 要描述每種內容的本體，所以路由改成每種內容各自註冊：`/admin/content/monsters`、`/admin/content/monsters/{id}`……，路徑與 HTTP 方法維持原樣，用一個泛型 helper 產生，避免六份重複程式碼。遷移期間舊的 `/admin/content/{kind}` 仍處理尚未遷移的種類（ASP.NET 的路由比對中，字面路徑優先於參數路徑），全部遷移完再刪除。

本體在反序列化時就檢查形狀：缺少必填欄位、列舉值不認識 → 400 並附欄位名稱；形狀正確後才寫進草稿，再由現有的 `JsonGameContent.Load` 檢查內容規則（例如引用不存在的地區），規則不通過照樣保存並回報，和現在一樣；例外見「關聯表併進怪物與技能的紀錄」。

### 唯讀的任務列表

`GET /admin/content/missions` 回傳 `{ id, title, daily }` 的陣列，有草稿讀草稿；只有 GET，PUT 與 POST 不存在（404）。用途只有怪物的「限定任務」下拉選單，選單只列非每日任務，因為 `missionId` 必須指向一般任務。

### OpenAPI 文件在建置時產生

**Supersedes**: content-admin / admin 前端：Refine + Ant Design（其中「API 型別用 openapi-typescript 從 `/openapi/admin.json` 產生」的來源）

- `EndlessBattle.Api` 加上 `Microsoft.Extensions.ApiDescription.Server`，`dotnet build` 時把 `v1` 與 `admin` 兩份文件寫到 `backend/openapi/`（`EndlessBattle.Api.json` 與 `EndlessBattle.Api_admin.json`，或以設定改成 `v1.json`、`admin.json`），檔案進 git。
- 產生文件時 admin 路由必須存在：工具不接受 `--environment`，所以 `Program.cs` 判斷進入組件是 `GetDocument.Insider` 時以 Development 設定啟動（已驗證可行，不需要替代方案）；admin 路由、連線字串、JWT 金鑰都來自 `appsettings.Development.json`。Release 建置不產生文件（`OpenApiGenerateDocuments` 為 false），Docker 映像不受影響。檔名採預設的 `EndlessBattle.Api.json`（v1）與 `EndlessBattle.Api_admin.json`（admin）。
- `frontend` 與 `admin` 的 `pnpm generate:api` 改成讀 `../backend/openapi/` 的檔案，不必啟動 API。
- `scripts/check.sh` 在後端 build 之後，對兩個前端各跑一次 `pnpm generate:api`，檢查 `backend/openapi/`、`frontend/src/api/schema.ts`、`admin/src/api/schema.ts`：重新產生前後內容不同（忘了產生或手改過）就失敗並列出檔案；與 git 暫存區不同或尚未加入 git（產生了但沒 `git add`）也失敗。只比 git 不夠，因為重新產生會蓋掉尚未暫存的手改。
- 替代方案（第 1 項任務驗證不可行時採用）：在 `EndlessBattle.Tests` 加一條測試，用 `EndlessBattleApiFactory` 取得兩份文件並與 `backend/openapi/` 的檔案比對，不同就失敗；設定環境變數 `EBO_UPDATE_OPENAPI=1` 時改成寫入檔案。其餘流程相同。

### admin 前端的 DTO、Converter、VO

- DTO：`admin/src/api/schema.ts` 產生的型別，只在 `admin/src/content/<kind>/<Kind>Converter.ts` 與 repository 使用。
- VO：`admin/src/content/<kind>/<Kind>Vo.ts`，class、private constructor、`static create(params)`，欄位是畫面好用的形狀（例如成長是七列的陣列、子地區是名稱與是否勾選）。畫面元件只 import VO，不 import `schema.ts`。
- Converter：`toVo(dto)` 檢查每個必填欄位存在且型別正確，缺少時丟出 `ContentParseError`，訊息含路徑（例如 `monsters[3].level`）；不認識的欄位直接忽略。`toDto(vo)` 產生完整的 DTO。共用的檢查函式放 `admin/src/content/shared/parse.ts`。
- 資料存取：每種內容一個 repository（`list`、`update`、`create`，回傳 VO；選單也從這裡讀；列表已經有每筆完整的資料，彈窗不用再讀一次，所以沒有 `get`），用 `@tanstack/react-query`。Refine 只保留版面、側欄與路由，不再用它的 dataProvider 處理內容（dataProvider 沒有型別，會繞過 Converter）。
- 列表遇到 `ContentParseError` 時顯示錯誤訊息，不顯示部分資料。
- 彈窗的選單（道具、技能、任務、地區）讀取失敗或回應有問題時，彈窗顯示錯誤並停用「儲存」：空的選單看起來像沒有資料，技能等級上限的檢查也會少了依據。

### 彈窗編輯與新增

- 列表每列的編輯按鈕開啟彈窗表單；存檔成功後關閉彈窗、更新列表與草稿狀態；失敗時彈窗保留，顯示 API 的錯誤（400 的欄位名稱、409 的 id 重複）。
- 「新增」開啟同一個表單：可以選一筆現有紀錄當起點（複製後清空 id），也可以從空白開始。
- 移除 `/<kind>/edit/:id` 與 `/<kind>/create` 路由。

### 關聯欄位用選的

| 欄位 | 選項來源 | 顯示 | 存回 |
| --- | --- | --- | --- |
| 怪物 `placeId` | 地區列表＋「全域（0）」 | 地區名稱 | id |
| 怪物 `subPlaceIndexes` | 所選地區的 `subPlaceNames` | 子地區名稱（勾選） | 序號；換地區時清空；都不勾代表每個子地區都會出現 |
| 怪物掉落 `itemId` | 道具列表（可搜尋） | 道具名稱 | id |
| 怪物 `skills[].skillId` | 技能列表（可搜尋） | 技能名稱 | id；等級不超過所選技能的 `maxLevel` |
| 怪物 `exclusiveMissionId` | 非每日任務列表（可搜尋）＋「無」 | 任務標題 | id 或 null |
| 職業 `parentJobId` | 職業列表（排除自己）＋「無」 | 職業名稱 | id 或 null |
| 職業 `statRequirements[].stat`、成長 | 七項能力 | 中文能力名 | 列舉 |
| 技能 `jobIds` | 職業列表（多選） | 職業名稱 | id 陣列 |
| 技能 `prerequisite.skillId` | 技能列表（排除自己）＋「無」 | 技能名稱 | id 或省略 |
| 大陸 `placeIds` | 地區列表（多選） | 地區名稱 | id 陣列 |
| 武器 `weaponSkill.skillId` | 技能列表（可搜尋）＋「無」 | 技能名稱 | id 或省略 |
| 鑰匙 `placeId` | 地區列表 | 地區名稱 | id |

選項一律從 admin API 讀（有草稿讀草稿），所以同一份草稿裡剛新增的紀錄也選得到。

### 結構化欄位逐項輸入

- 能力值表（共用元件，`admin/src/content/shared/StatFields.tsx`）：加成、需求、魔石固定值與範圍用 `StatNumberGrid`，空白代表 0；成長與精鍊範圍用 `StatRangeGrid`，每項最小、最大，精鍊留白代表不加。每格都有自己的標籤，只列該欄位支援的能力。職業的能力需求不是這張表，而是可增減的「能力＋最低值」列表。
- 技能等級（使用者定的形狀：先設等級上限，再逐級填）：改 `maxLevel` 時在最後增減等級，新增的一級複製前一級的內容，省下重填；每一級有學習費用、所需熟練度、冷卻，以及條件、消耗、效果三個列表。等級欄自動依順序，`levels` 筆數等於 `maxLevel`（已量測：76 個技能都成立）。
- 條件、消耗、效果：每列先選種類，再依「技能的條件、消耗、效果」的表只顯示該種類的欄位；三擇一的欄位用切換選擇。都可以新增、刪除；條件清空代表常駐。
- 掉落：列表，每列是道具下拉加掉落難度，可新增、刪除。怪物的技能同樣是列表：技能下拉、等級、發動率。
- 怪物勾選 NPC：只留名稱、地區、子地區、限定任務、遇敵率、遭遇訊息，其他戰鬥欄位歸零，掉落與技能清空；取消勾選才顯示回來。這符合 `mission-objectives` 的內容規則（NPC 可以不符合數值下限，但必須有任務、不能有掉落與技能）。

### 道具依分類顯示的欄位

依目前內容實際出現的欄位決定（共同欄位：id、名稱、分類、價格、商店等級、商店販售）：

| 分類 | 額外欄位 |
| --- | --- |
| 武器 | 加成、需求、孔數、精鍊上限、屬性、武器技能（技能、等級、發動率）、綁定 |
| 頭、身體、手、腳 | 加成、需求、孔數、精鍊上限、綁定 |
| 飾品 | 加成、需求、綁定 |
| 消耗品 | 消耗品種類與綁定，再依種類：補給 `Supply` → 回復 HP／SP；能力藥水 `StatPotion` → 加成；密藥 `Elixir` → 密藥（種類、倍率、秒數）；其他（`Capture`、`ExperienceCapsule`、`BagExpansion`、`DepotExpansion`、`ProficiencyBook`、`StoneBox`、`RefineBox`、`PetEgg`）→ 數值 `amount`（說明依 `ConsumableKind` 註解，例如捕捉道具是能力倍率） |
| 魔石 | 魔石（隨機、固定值或最小／最大、寶箱等級） |
| 精鍊石 | 精鍊（等級、系列、各能力範圍）；系列 `kind` 內容裡是 1～18，意義尚未確認，先用數字輸入 |
| 鑰匙 | 鑰匙（地區、秒數）、綁定 |
| 素材、勳章 | 綁定 |

切換分類時，不屬於新分類的欄位在存檔時不送出（toDto 依分類輸出）。魔石與精鍊石沒有綁定欄位，一律送 `false`。

### 搜尋與篩選

在前端處理。道具有 3127 筆，所以列表分頁（沿用現在的每頁 50 筆），項目多的下拉選單用 Ant Design `Select` 的搜尋與虛擬捲動。任務 4.1 量測（2026-11-08，本機 Development，3127 筆）：`GET /admin/content/items` 回應 1,104,065 bytes、30～70ms，遠低於 1 秒的門檻，所以不另外提供選項路由，選單直接讀這條路由。每頁一個搜尋框，比對 id 與名稱（不分大小寫、部分比對）；篩選：

| 頁面 | 篩選 |
| --- | --- |
| 道具 | 分類、商店等級 |
| 怪物 | 地區 |
| 職業 | 階級 |
| 技能 | 職業、欄位（主動／被動） |
| 大陸 | （只有搜尋） |
| 地區 | 大陸、是否禁區 |

列表上的關聯欄位顯示名稱（例如怪物的地區顯示「中央平原」）。

### 硬規則

- 規則 6（遊戲內容只放 `backend/content/*.json`）：內容檔仍是唯一的儲存處，格式與數值不變，兩張關聯表也還是獨立的檔案；雙向轉換測試保證不失真。
- 前端硬規則 9 與 admin 規則 6（`schema.ts` 是產生檔）：改成從 `backend/openapi/` 產生，仍不得手改；`check.sh` 會擋下手改或過期。
- admin 規則 1～5：新路由與任務列表都在 `backend/EndlessBattle.Api/Endpoints/Admin/`、`/admin` 群組下，只寫草稿，發布流程不變。
- 不涉及：資料表、migration、scaffold 產生檔、Domain。

## Implementation Contract

**admin API（全部在 `/admin` 群組、`Admin:Enabled` 為 `true` 時才存在）**

| 方法與路徑 | 本體 | 錯誤 |
|---|---|---|
| `GET /admin/content/{items\|monsters\|jobs\|skills\|areas\|places}` | 回傳該種類紀錄陣列（完整欄位名稱） | — |
| `GET /admin/content/<kind>/{id}` | 回傳一筆 | 找不到 → 404 |
| `PUT /admin/content/<kind>/{id}` | 該種類紀錄；回傳草稿狀態 | 形狀錯誤或引用不存在的技能、職業 → 400（含欄位名稱）；本體 id 與路徑不同 → 400；找不到 → 404 |
| `POST /admin/content/<kind>` | 該種類紀錄；回傳 201 與草稿狀態 | 形狀錯誤或引用不存在的技能、職業 → 400；id 已存在 → 409 |
| `GET /admin/content/missions` | `[{ id, title, daily }]` | — |
| 其他 kind（例如 `recipes`）| — | 404 |

草稿、發布、報告的 API 不變。

**建置與指令**

- `cd backend && dotnet build` 後 `backend/openapi/` 有 `v1` 與 `admin` 兩份文件；`v1` 不含 `/admin/` 路徑，`admin` 只含 `/admin/` 路徑且有每種內容的紀錄 schema。
- `cd frontend && pnpm generate:api`、`cd admin && pnpm generate:api` 不需要 API 執行中。
- `./scripts/check.sh` 在 `schema.ts` 或 `backend/openapi/` 與重新產生的結果不同時以非 0 結束。

**admin 前端**

- 每種內容有 `<Kind>Vo`、`<Kind>Converter`（`toVo`、`toDto`）、repository、列表、彈窗表單；畫面元件不 import `src/api/schema.ts`。
- `toVo` 缺必填欄位丟 `ContentParseError`（含路徑），不認識的欄位忽略；`toDto(toVo(dto))` 與原 DTO 相等（不含不認識的欄位）。
- 刪除 `admin/src/content/recordFields.ts`、`RecordForm.tsx`、`ContentPages.tsx` 與 `admin/src/api/dataProvider.ts` 的內容用途。

**驗收**

- 後端：雙向轉換測試（七個正式內容檔與 TestContent 逐字相同）、引用不存在的技能回 400、admin API 測試改用新欄位名稱、形狀錯誤 400、任務列表、OpenAPI 文件含紀錄 schema。
- admin：Converter 測試（缺欄位、忽略未知欄位、來回相等）、表單行為測試（關聯選了存 id、子地區隨地區連動、成長存回最小最大、道具依分類送出欄位）、列表搜尋與篩選測試；樣式不寫測試，以瀏覽器確認。
- `./scripts/check.sh` exit code 0。

**範圍內**：上述 API、codec、紀錄、OpenAPI 檔、兩個前端的產生方式、admin 的 DTO／Converter／VO、彈窗、關聯選單、結構化欄位、技能的條件消耗效果、怪物能用的技能、列表分頁與搜尋篩選、CLAUDE.md 指令段落。

**範圍外**：Non-Goals 所列各項；遊戲 API 回應與遊戲前端畫面；草稿、發布、報告的行為。

## Risks / Trade-offs

- [建置時產生文件需要執行 Program 的啟動流程：會讀內容檔、檢查 JWT 金鑰，admin 路由只在設定開啟時存在] → 第 1 項任務先驗證；不可行就改用設計裡的測試比對替代方案，後續任務不受影響。
- [紀錄與內容檔各有一套欄位名稱，codec 寫錯會默默改掉資料] → 雙向轉換逐字相同的測試涵蓋所有正式內容；新增欄位時這條測試會先失敗。
- [admin API 欄位改名是 BREAKING] → 只有 admin 前端使用，同一個變更裡一起改；遊戲 API 不受影響。
- [道具欄位最多、組合最雜] → 依實際內容的分類分組，道具放在最後兩項任務；能力值表等共用元件先在怪物、職業做好。
- [技能表單最複雜：4 種條件、3 種消耗、13 種效果，每一級都要填] → 拆成基本資料、條件與消耗、效果三項任務；新增等級時複製前一級。
- [道具 3127 筆，選單與列表可能變慢] → 分頁、虛擬捲動；回應大小與時間已量測（見「搜尋與篩選」），瀏覽器裡的操作在任務 5.2 確認。
- [`scripts/import-wog45.py` 會從原版資料重新產生內容檔，發布過的修改會被蓋掉；`WogImportTests`、`MonsterRosterTests`、`MissionTableTests`、`ItemTableTests` 等釘住原版數值，發布時改到那些紀錄 `check.sh` 會失敗] → 這次不處理，匯入腳本與測試都不動。開始用編輯器調內容之前要決定：以 `backend/content/` 為準、不再重新匯入，以及這些測試怎麼調整。
- [一次寫入改兩個檔案，中途失敗會留下不一致的草稿] → 先把兩個檔案都寫到暫存檔再一起換上，測試模擬第二個檔案寫入失敗時草稿不變。
- [「新增時複製現有紀錄」會帶入不想要的欄位] → 複製後清空 id，彈窗裡所有欄位都看得到、可改。
- [前端多一層 Converter／VO，程式碼變多] → 換來 API 形狀改變時只需改 Converter；這是這次的目標，接受。
