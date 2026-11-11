## Context

- 遊戲 API 的回應有兩種來源：一種是 `backend/EndlessBattle.Api/Endpoints/Contracts.cs` 裡 API 自己的 `*Response` record，欄位照抄 Domain 的縮寫（`Str`、`Smart`、`Au`、`Be`、`At`、`Mdf`、`Hp`、`Sp`、`Exp`、`NextExp`）；另一種是直接序列化 Domain record：`StatBonus`、`StatRequirement7`、`AdvanceSummary`、`DropResult`、`LevelUpEvent.Growth`（`BaseStats`），以及整個 `BattleEvent` 階層（`[JsonDerivedType]` 寫在 Domain）。
- `/api/public/jobs`、`/api/public/settings` 已經用完整名稱（`strength`…`faith`、`minimum`／`maximum`），前端也已用 `JobConverter`／`CreationOptionsConverter` 轉成 `JobVo`／`CreationOptionsVo`（`frontend/src/api/publicData.ts`）。這是本變更要比照的寫法。
- 內容 JSON 有兩條讀取路徑：`JsonGameContent` 直接把怪物反序列化成 `MonsterDefinition`，道具的 `ItemEntry`／`StoneEntry` 直接用 Domain 的 `StatBonus`、`StatRequirement7`；職業有自己的 `GrowthEntry`。資料庫裝備強化的 JSON 欄位由 `EnhancementJson` 直接序列化 `StatBonus` 與 `SocketedStone`（鍵是 Web 預設的 camelCase：`str`、`at`、`hpMax`…）。所以 Domain 一改名，若不先隔開，內容與玩家資料都會讀不回來。
- 能力代碼字串 `"str"`、`"smart"`… 出現在 Domain 的 `StatRequirement.Stat`（職業需求）、`JobChange` 與 `EquipmentRules` 的 `statTooLow:<代碼>` 原因、`BalanceReportService`。
- `/api/content` 的前端使用者：`AdventureScreen`（載入名稱表給戰報與舞台的怪物名）、`CharacterScreen`（性別、屬性、出生地）、`MissionsScreen` 與 `game/missions.ts`（任務條件的職業、性別、出生地）。
- 使用者決定（2026-11-11）：拆成兩個變更，這是第一個；SP 改名為 MP（`manaPoints`）；Domain 一起改名。

## Goals / Non-Goals

**Goals:**

- 角色、冒險推進（含戰鬥事件與傭兵戰報）、技能、轉職、世界地圖的 API 欄位與 Domain 欄位都用完整名稱。
- 共用的能力型別（`BaseStats`、`StatBonus`、裝備需求）在所有遊戲回應裡都改成完整名稱。
- API 不再序列化 Domain 型別；內容檔與資料庫 JSON 的格式不變。
- 前端這五類資源經 Converter 轉成 Vo，畫面與 `src/game` 不再 import 它們的產生型別。
- 拿掉 `/api/content`。

**Non-Goals:**

- 背包、商店、倉庫、鐵舖、任務、寵物、傭兵、勳章、旅館、銀行回應的 Vo 化，以及它們自己的縮寫欄位（例如道具的 `supplyHp`、任務獎勵的 `exp`、傭兵目錄的 `expPercent`、精鍊石的能力範圍）：下一個變更。
- 畫面上的「SP」標籤改成「MP」。
- admin 的 `SkillPoints`（怪物 SP）與 admin 前端。
- 內容 JSON 鍵、資料表欄位改名；不新增 migration。

## Decisions

### 完整名稱對照

| 縮寫 | Domain（C#） | API／前端（JSON） |
| --- | --- | --- |
| Str | Strength | strength |
| Smart | Intelligence | intelligence |
| Life | Life | life |
| Vit | Vitality | vitality |
| Agi | Agility | agility |
| Au | Charisma | charisma |
| Be | Faith | faith |
| At | Attack | attack |
| Mat | MagicAttack | magicAttack |
| Df | Defense | defense |
| Mdf | MagicDefense | magicDefense |
| Hp | HealthPoints | healthPoints |
| HpMax | MaxHealthPoints | maxHealthPoints |
| Sp | ManaPoints | manaPoints |
| SpMax | MaxManaPoints | maxManaPoints |
| Exp | Experience | experience |
| JobExp | JobExperience | jobExperience |
| NextExp | NextLevelExperience | nextLevelExperience |
| Min／Max（等級範圍） | Minimum／Maximum | minimum／maximum |

複合名稱照同樣規則替換：`PlayerHp`→`PlayerHealthPoints`、`MonsterHpMax`→`MonsterMaxHealthPoints`、`SpAfter`→`ManaPointsAfter`、`TargetHpAfter`→`TargetHealthPointsAfter`、`OpponentHpAfter`→`OpponentHealthPointsAfter`、`HpCost`→`HealthPointsCost`、`SpCost`→`ManaPointsCost`、`OpponentSpLoss`→`OpponentManaPointsLoss`、`SpGain`→`ManaPointsGain`、`PotionHpPercent`→`PotionHealthPointsPercent`（設定的五個百分比同理）、`ExpPenalty`→`ExperiencePenalty`。`AtSeconds` 的 At 是時間，不改。

替代方案：SP 寫成 `spiritPoints`、保留 SP 縮寫——使用者選了 MP。`SkillPoints` 已經是「學技能用的點數」，不能拿來當 SP。

### 能力代碼改成 Domain 列舉

Domain 新增 `BaseStat` 列舉（Strength、Intelligence、Life、Vitality、Agility、Charisma、Faith），取代 `StatRequirement.Stat` 等處的字串；`JobChange`、`EquipmentRules`、`BalanceReportService` 用列舉比對。內容檔的 `"str"` 等代碼由 Infrastructure 解析成列舉，不認識的代碼照舊在載入時失敗。原因代碼對外寫成 `statTooLow:<camelCase 全名>`（例如 `statTooLow:strength`）；對外字串的產生集中在一個地方（Domain 的列舉擴充或 API 的轉換），前端 `statLabel` 改用全名對照中文。

替代方案：Domain 保留字串、只在 API 換——使用者要 Domain 一起改名，而且列舉讓打錯代碼在編譯期就被抓到。

### 儲存格式用 Infrastructure 的 entry 型別隔開

- 怪物：`JsonGameContent` 不再直接反序列化 `MonsterDefinition`，改讀內部 `MonsterEntry`（鍵維持 `hp`、`sp`、`at`、`mat`、`df`、`mdf`、`str`…），再轉成 Domain。admin 的 `MonsterCodec` 已有自己的 entry，不受影響。
- 道具：`ItemEntry`、`StoneEntry`、`RefineEntry` 改用內部的 `StatBonusEntry`、`StatRequirementEntry`（舊鍵），轉成 Domain。
- 職業：`GrowthEntry` 與 `StatRequirement` 的 JSON 仍是舊鍵，轉換時對應到改名後的 Domain。
- 資料庫：`EnhancementJson` 改序列化內部 `StoredStatBonus`、`StoredSocket`（鍵與現在完全相同：`str`、`smart`、`life`、`vit`、`agi`、`au`、`be`、`at`、`mat`、`df`、`mdf`、`hpMax`），讀寫都經過它。
- 角色資料表欄位（`hp`、`sp`、`exp`…）只在 `CharacterRepository` 的對應程式碼裡出現，scaffold model 不動。

替代方案：在 Domain 加 `[JsonPropertyName]`——Domain 不能有序列化細節，也讓 Domain 名稱和儲存格式綁在一起，否決。

### API 回應不再序列化 Domain 型別

新增 API 自己的 `StatBonusResponse`、`EquipmentRequirementResponse`、`BaseStatsResponse`（已有，改成完整名稱）、`AdvanceSummaryResponse`、`DropResponse`，以及戰鬥事件 `BattleEventResponse` 階層（多型鑑別值 `type` 與現在相同：`encounter`、`round`、`attack`…）。`[JsonDerivedType]` 從 Domain 的 `BattleEvent` 移到 API 的回應型別；Domain 的事件 record 不再帶序列化屬性。所有回傳道具加成或需求的 endpoint（背包、商店、倉庫、鐵舖等）也改用這兩個回應型別，所以它們的欄位名稱會在本變更一起換成完整名稱。

先在 Domain 改名之前，用舊欄位名稱建好這些回應型別（JSON 不變），再改 Domain，最後改 API 欄位——每一步都能單獨通過測試。

### 戰鬥事件帶怪物名稱

`EncounterEvent` 加 `MonsterName`、`CaptureEvent` 加 `CapturedMonsterName`、`MissionProgressEvent` 加 `MonsterName`；名稱在 Domain 產生事件時從 `MonsterDefinition.Name` 帶入（Domain 已持有定義，不需要另外查）。前端戰報與舞台直接用事件上的名稱，`createMonsterNameResolver` 移除。

替代方案：另開 `/api/public/monsters`——戰報只需要遇到的怪物名稱，事件自帶名稱最直接，也避免下載整份怪物表。

### 前端 Converter 與 Vo 的位置

- `frontend/src/api/<資源>Data.ts` 放 Converter（`CharacterConverter`、`AdvanceConverter`、`SkillOverviewConverter`、`JobChoiceConverter`、`WorldConverter`），只依賴 `parse.ts` 的檢查函式與 `src/game` 的 Vo；`client.ts` 對這些端點一律 `requestJson<unknown>` 再 `toVo`。
- Vo 放 `src/game`：資源本體用 class（private constructor＋`static create`，與 `JobVo` 相同）；巢狀的唯讀資料（能力值、事件）用 `readonly` 物件型別。戰鬥事件是以 `type` 區分的聯集型別，定義在 `frontend/src/game/battleEvents.ts`。
- `parse.ts` 補 `parseBoolean`、`parseNullableNumber`、`parseOneOf`（列舉字串）、`parseTimestamp`（ISO 字串）。檢查失敗丟 `ParseError`，路徑從資源名稱開始（例如 `character.stats.strength`）。
- 送出的請求（建角、冒險設定、技能欄）由 `src/game` 的命令型別經 Converter 的 `toDto` 轉成請求本體。
- `src/game/types.ts` 移除這五類資源的別名；下一個變更處理剩下的別名。

### 拿掉 /api/content

刪除 `ContentEndpoints`、`ContentResponse`、`NamedContent`、`JobContent`、`StatRequirementContent`、`JobGrowthContent`，以及前端 `getContent`、`ContentNames`。角色頁的性別、屬性、出生地用 `getCreationOptions()`（`/api/public/settings`）；任務頁的職業名用 `getPublicJobs()`、性別與出生地用 `getCreationOptions()`；名稱查不到時顯示原始 id，與現在相同。`accounts` 規格的匿名端點清單同步拿掉 `/api/content`。

### 硬規則

- 內容檔：不改 `backend/content/*.json`，只改讀取程式（硬規則 6）。
- 資料表：不新增 migration、不重新 scaffold；`Persistence/Models` 不動（硬規則 3、4）。
- `frontend/src/api/schema.ts` 與 `backend/openapi/EndlessBattle.Api.json` 只靠 `dotnet build` 與 `pnpm generate:api` 產生（遊戲前端硬規則 9）；`admin/src/api/schema.ts` 若因共用型別改名而變動，同樣只重新產生。
- admin 路由：不涉及。

## Implementation Contract

**行為**

- `GET /api/characters/me`、`POST /api/characters`、`PUT /api/characters/me/settings`、`POST /api/adventure/advance`、`GET /api/skills`、`POST /api/skills/learn`、`PUT /api/skills/slots`、`GET /api/jobs`、`POST /api/jobs/change`、`GET /api/world`、`PUT /api/world/location` 的請求與回應只用「完整名稱對照」表的名稱。
- 任何遊戲回應裡的道具加成（`bonus`、`refineBonus`、魔石的 `rolled`）與裝備需求（`requires`）用完整名稱。
- `statTooLow:<stat>` 的 `<stat>` 是完整名稱。
- 戰鬥事件：encounter 有 `monsterName`；capture 有 `capturedMonsterName`（沒捕到為 null）；missionProgress 有 `monsterName`。
- `GET /api/content` 回 404。
- 內容檔照原樣載入；資料庫裡既有的強化 JSON 照原樣讀回，新寫入的鍵與舊資料相同。

**介面／資料形狀**

- 前端 `ApiClient`：`getMyCharacter`、`createCharacter`、`updateSettings`、`changeJob`、`moveTo`、`deposit`、`withdraw` 回傳 `CharacterVo`；`advance` 回傳 `AdvanceVo`；`getSkills`、`learnSkill`、`setSkillSlots` 回傳 `SkillOverviewVo`；`getJobs` 回傳 `JobChoiceVo[]`；`getWorld` 回傳 `WorldAreaVo[]`。`getContent` 移除。
- 其他回應中夾帶的 `character`（任務完成、寵物餵食、傭兵僱用、旅館入住）在本變更仍是產生型別、但欄位已是完整名稱；下一個變更再轉成 `CharacterVo`。

**失敗模式**

- 回應缺必要欄位或型別不對：`ParseError`（訊息「API 回應的 <路徑> 應該是<型別>」），畫面照一般請求失敗處理，不會用 undefined 繼續算。
- 內容檔出現不認識的能力代碼：啟動時 `GameContentException`，與現在相同。

**驗收**

- 後端測試：每個改名的回應有 API 層測試斷言新鍵存在、舊鍵不存在（`CharacterEndpointsTests` 等既有測試改寫）；`EnhancementJson` 有讀舊格式字串、寫出同樣鍵的測試；內容載入測試比對 `monsters.json` 第一隻怪物的數值；`GET /api/content` 回 404 的測試；戰鬥事件帶怪物名稱的測試。
- 前端測試：每個 Converter 有「完整回應轉成 Vo」「缺欄位丟出含路徑的 ParseError」「多餘欄位被忽略」三種測試；戰報用事件上的名稱。
- `./scripts/check.sh` 通過（包含 OpenAPI 文件與 `schema.ts` 是最新的）。
- 手動：`pnpm dev` 登入既有角色，冒險頁戰報有怪物名、角色頁顯示性別／屬性／出生地、技能頁與轉職頁正常。

**範圍**

- 在範圍內：上述五類資源的後端與前端、共用能力型別、Domain 改名、儲存隔離、拿掉 `/api/content`。
- 不在範圍內：見 Non-Goals。

## Risks / Trade-offs

- [Domain 改名牽動約 30 個 Domain 檔案、近 50 個測試檔，容易漏改或改錯欄位] → 改名分批（能力型別一批、角色與戰鬥一批），每批只做純改名、前後跑全部測試證明行為未變，與行為性變更分開 commit。
- [資料庫強化 JSON 讀不回來會讓玩家裝備的精鍊值歸零] → 先加「讀舊格式字串」的測試再改 Domain；`StoredStatBonus` 的鍵用明確的 `JsonPropertyName`，不依賴命名規則。
- [背包、鐵舖等畫面在本變更只換欄位名稱，沒有 Converter，改漏時 TypeScript 會擋下（產生型別已變），但執行期不會檢查] → 下一個變更補 Vo；本變更以 `tsc -b` 與既有畫面測試把關。
- [前端與後端要同時部署，舊前端讀新 API 會壞] → 單人遊戲、前後端一起建置部署（compose、NAS 腳本都是同一個 commit），不需要相容期。
- [戰鬥事件多了名稱，`/api/adventure/advance` 回應略變大] → 每場最多三個事件帶名稱，影響可忽略（估計值，未量測；若之後要量，比較一次 advance 回應的位元組數）。
