## Context

- 城門口的「角色」紙籤目前打開舊外殼的「角色」分頁：`GameScreen` 的 `facilityViews.character` 對到 `{ tab: "character" }`，再由 `AdventureScreen` 顯示 `CharacterScreen`。
- 舊角色頁（`frontend/src/ui/CharacterScreen.tsx`）是文字清單，內容有：
  - 性別、屬性、出生地、職業熟練度 `N%（值）`；
  - HP、SP、EXP 條；
  - 七項裸點（裝備加成寫成 `（+N）`）、六項戰鬥數值；
  - 密藥（每分鐘重算）與一行住宿；
  - 「登出」，`App` 只清掉本機 token、回標題，不送請求。
  - 名稱表來自 `AdventureScreen` 讀的 `getPublicJobs`、`getCreationOptions`（`/api/public/settings`）。
- 技能 API 現況：
  - `GET /api/skills` 回 `{ jobId, skillPoints, skills, slots }`。`skills` 是目前職業教的技能（`job-skills.json` 順序）加上其他已學的技能；每筆帶 `summary`（`SkillSummary.Of` 用「｜」接起來的一行）、`taughtByCurrentJob`、`prerequisite.met`、`next`（`money`、`proficiency`＝技能點花費、`summary`、`canLearn`、`enoughMoney`、`enoughProficiency`）。
  - `POST /api/skills/learn { skillId }` 只能用目前職業的技能點；原因依序 `WRONG_JOB`、`PREREQUISITE`、`MAX_LEVEL`、`NOT_ENOUGH_PROFICIENCY`、`NOT_ENOUGH_MONEY`（Domain `SkillLearning`、`LearnCheck`）。
  - `job_proficiency` 每列有 `Proficiency`（轉職門檻，上限顯示 3500）與 `SkillPoints`（學技能扣）；只在該職業得到熟練度或技能點時才會產生那一列（`IJobProficiencyRepository.AddAsync`）。
  - 技能學習沒有規格。測試在 `SkillLearningTests`、`SkillServiceTests`、`SkillEndpointsTests`、`SkillSummaryTests`。
- 正式內容（實測 `backend/content/job-skills.json`）：
  - 12 個職業（Class I 4 個、Class II 8 個），沒有任何技能同時被兩個職業教；
  - 戰士教 `skill-1` 爆氣、`skill-61` 防禦心得、`skill-71` 破滅一擊、`skill-77` 嗜血，後三個各有前一個 Lv 3 的前置；
  - 爆氣 Lv1 花 500 金錢、35 技能點，Lv2 花 2,000、235，Lv3 花 5,000、549；
  - 被動 `skill-492` 華爾滋沒有條件、沒有消耗；正式內容沒有冷卻大於 0 的技能等級。
- 城鎮已經有密藥與住宿的合併清單（`game/town.ts` 的 `bonusRows`、`remainingText`、`isEndingSoon`）。
- 其他內頁已經做好的共用元件：
  - `RevealLayer`、`BackButton`、紅印章 `Seal`、`ContentImage` 的分類佔位圖、`screenImages.ts` 預載；
  - 標題頁的 `InkDots`（`InkDotsDelaySeconds` 0.45）。
- 使用者 2026-11-19 的 9 項決定、回饋三輪（v38 就地動畫被否決 → v39 等待墨點＋彈窗 → v40 失敗只寫原因、17px）都記在記憶 `ebo-character-screen-implementation`。

## Design Source

- 來源：新成品「EBO 新比例畫面」https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 `CharacterFit`（2026-11-19 v40 定案）。
  - EBO-design 依本變更的決定畫，底稿是舊畫布 HomeJ 的角色頁。
  - 產生器 `.design-work/work/characterfit/build.py`（資料 `build-data.py` → `character-data.json`，取正式內容）。
  - 點擊測試 `test-skills.js`、`test-job.js`。
- 快取：`.spectra/design-cache/character-screen/CharacterFit.dc.html`（git 忽略；素材 `/_blob/<id>`，原檔在 `.design-work/assets/<id>.*`）。
- 預覽：`cd .design-work && zsh work/characterfit/shots.sh <狀態>…`（624／700／780 三框並排）。狀態名以板子左上的切換 chip 為準：能力、技能、升級結果、職業、讀取。
- 板子上的職業列表、職業詳細、轉職成功彈窗屬於下一個變更，這次不做。
- 下表是主要版位。顏色、陰影、字級、轉角以快取檔的 `.ccard`、`.cbody`、`.skc`、`.jsw`、`.elix`、`.pop*`、`.ch-*` 規則為準。舞台寬 390、高 H＝624～780。

| 區塊 | 元素 | 數值 |
| --- | --- | --- |
| 進出 | 展開 | 圓心固定 (311, 193)（城門口「角色」紙籤中心；城門口那段不跟高度縮放）；0.55s，同其他內頁 |
| 進出 | 返回 | 左上「城鎮」（`.cback`，同旅館返回鍵位置，z 31） |
| 縮放 | 上半 | cs＝0.88 + (H − 624) / 156 × 0.12；dt＝16 + 329 × cs + 7 |
| 背景 | 裝備間 | `10caa783` 寬 390、top −40，上面再疊一層米色 .38 |
| 桌緣 | 木條 | `d137517f`，top dt、高 40，橫向重複 |
| 登記證 | 卡 | `e5a31933`，left 158、top 16、寬 224、比例 700/1029、padding 58 24 0，`rotate(1deg) scale(cs)`、以右上角為準、z 30 |
| 登記證 | 字 | 名字 Noto Serif TC 900 22px；職業列 13px（Lv 用 Cinzel 700）；熟練度 12.5px；性別等 13px（欄名 12.5px）；HP／SP／EXP 12.5px，條高 5 |
| 立繪 | 位置 | left −10、top dt + 6 − 336 × cs、寬 221 × cs、高 336 × cs、z 25；圖片 `translateX(−offset × 高)` |
| 立繪 | 雙腳偏移（男／女，以圖高為 1） | 戰士 0／+0.010、術士 +0.035／+0.046、盜賊 −0.020／+0.008、樂手 +0.011／−0.012；其他職業 0 |
| 立繪 | 待補 | 立繪區正中，職業圖示 44 + 「立繪待補」13px |
| 錢袋 | 位置 | left 16、top dt + 7、高 28；金幣 20；Cinzel 700 14px（同旅館、同伴） |
| 手帳 | 索引籤 | right 30、top dt + 4、寬 168、間距 8；兩張 `550cfa39`（未選）／`9ad83b52`（選中）；14px |
| 手帳 | 內頁 | dt + 40 到舞台底，頁內捲動 |
| 能力頁 | 段名 | Noto Serif TC 900 14px、字距 .24em、下框線 1.5px |
| 能力頁 | 能力格 | 兩欄、間距 6 14；名稱 13px、值 16px、加成 11px 綠 |
| 能力頁 | 密藥與住宿 | 圖示 26 圓（住宿方角 7）；名稱 13px、效果綠、剩餘時間 Cinzel 700 13px 寬 44，10 分鐘內紅 |
| 能力頁 | 登出列 | 右對齊、上方虛線；帳號 13px；「登出」Noto Serif TC 700 12px 紅框 |
| 技能頁 | 職業切換 | 黏在頁首；職業圓 42（圖示 28）；名稱 17px；副標 11px；技能點 11px＋數字 17px；展開鈕 28 圓 |
| 技能頁 | 技能卡 | 圖示圓 48（主動 `ce51ffe7`／被動 `bd41af08`）；名稱 17px；Lv 11px（數字 13px 粗）；菱形 10×11（`15677f71`／亮 `55a2cf6d`）；說明 13px；一次只展開一張 |
| 技能頁 | 卡底 | 花費 12px（不夠的紅）；升級紅印章 56×56（`759746ed`，直書 16px），停用灰；原因 13px 紅；「精通」雙框紅章 |
| 讀取 | 讀取中／失敗 | 讀取中：登記證的字與數字改淡色塊、條為空、立繪不出、錢袋數字淡色塊、手帳 8 行空白線；失敗：手帳中「登記證的資料讀不到。」＋「再試一次」紅印章 92×92 |
| 回饋 | 等待墨點 | 擋點擊的透明層；0.45s 後舞台正中深色小底板＋三個墨點（同標題頁）；淡入 0.2、至少停 0.3、淡出 0.15 |
| 回饋 | 彈窗 | 遮罩 z 95、`rgba(20,10,4,.55)`；紙框同登出確認框；淡入＋0.92→1 放大 0.22s、收 0.15s；「確定」寬 150 |
| 回饋 | 失敗彈窗 | 紅蠟封 44（白「！」24px）；紅字 17px、weight 500 |
| 回饋 | 成功彈窗 | 標題 19px（「升級成功」「習得成功」）；技能圓 64；名稱 18px；等級 Cinzel（新等級 22px 綠）；菱形 11×12；數值列 12px／值 13px、變的綠 |

素材 blob id 只寫前 8 碼，對照表在上。轉檔前先比對前端已有的同一張圖，相同就沿用、不重轉：

- 紅印章（`title/seal`）；
- 木條（`title/desk-edge`）；
- 金幣（`town/purse`）；
- 返回鍵（同其他內頁）。

技能圖示用內容圖（`ContentImage` 的 skills 類），職業圖示與立繪用內容圖（job-icons、portraits 類）。

## 功能對照

| 舊畫面或規則 | 新畫面 | 決定 |
| --- | --- | --- |
| 個人資料（性別、屬性、出生地、熟練度） | 登記證 | 照稿；熟練度寫 `N%` 與 `值 / 3,500` |
| HP、SP、EXP 條 | 登記證下半 | 照稿（名稱＋數字一行、條在下面） |
| 七項能力（`（+N）`） | 能力頁「能力」 | 照稿；加成寫成 `+N`／`-N`，沒有加成不寫 |
| 六項戰鬥數值 | 能力頁「戰鬥數值」 | 照稿 |
| 密藥（每分鐘重算）＋一行住宿 | 能力頁「密藥與住宿」 | 使用者 2026-11-19：合成一個清單、時間格式同城鎮、每秒倒數 |
| 登出 | 能力頁最下面＋確認框 | 使用者 2026-11-19：照設計稿 |
| （舊角色頁沒有）持有金錢 | 桌緣錢袋 | 使用者 2026-11-19：照稿（v38 新加） |
| 舊外殼「技能」分頁的職業、技能、技能欄 | 技能頁（職業切換＋技能卡） | 使用者 2026-11-19：技能欄留在舊外殼；轉職這段先帶去舊外殼技能分頁 |
| 技能卡的「熟練度」花費 | 「技能點」 | 使用者 2026-11-19：只改技能頁 |
| （規則）只能用目前職業學 | 以前的職業可以升級已學的 | 使用者 2026-11-19：只能升級已學的 |
| 學習的錯誤文字 | 等待墨點＋失敗彈窗；成功彈窗 | 使用者 2026-11-19：v40 定案 |
| 載入失敗、載入中 | 「登記證的資料讀不到。」＋「再試一次」；淡色塊與空白線 | 照提案 |
| 舊外殼「角色」分頁 | 拿掉 | 使用者 2026-11-19 |

## Goals / Non-Goals

**Goals:**

- 照 CharacterFit 做登記證、立繪、錢袋、手帳兩頁、技能卡、成功與失敗彈窗、讀取中與讀取失敗、進出。
- 技能可以用以前職業的技能點升級已學的技能；技能 API 給每個職業的清單與拆好的數值列。
- 做出全遊戲共用的等待墨點與失敗彈窗（這次只用在角色頁）。
- 補 `skill-learning` 規格，既有行為逐條對照測試。

**Non-Goals:**

- 職業列表、職業詳細、轉職成功彈窗與 `job-change` 規格：下一個變更做。這一段的「轉職」先打開舊外殼技能分頁。
- 技能欄（帶哪 5 招）：留在舊外殼技能分頁，冒險頁換新時再搬。
- 舊外殼技能分頁照舊（包括它自己的學習按鈕）；只跟著共用的原因文字把「技能熟練度不足」改成「技能點不足」。
- 旅館、同伴、勳章所、任務板維持由 NPC 傳話，這次不動。
- 不改技能內容與學習花費；不補 Class II 技能說明（顯示「（說明待補）」）；不做 Class II 立繪（顯示「立繪待補」）。

## Decisions

### 角色頁是 GameScreen 的第七種展開設施

- `GameScreen` 的 `revealed` 表加 `character: { origin: characterRevealOrigin, segment: 0 }`，展開、收回與帶最新角色都同其他內頁。
- `characterRevealOrigin` 固定回 (311, 193)，不隨舞台高度變。
- `facilityViews` 拿掉 `character`，`AdventureScreen` 拿掉「角色」分頁與 `onSignOut`。
- 新的 `CharacterScreen`（`src/ui/character/`）收：
  - `onSignOut`（`App` 原本那支：清 token、回標題）；
  - `onOpenJobChange`。`GameScreen` 收到後先把角色交回，再開舊外殼技能分頁（`{ tab: "skills" }`）；從舊外殼「回城鎮」照舊回城門口。

替代方案：在角色頁裡嵌舊的 `SkillsScreen` 轉職區。這一段只是過渡，下一個變更就換成職業頁，不採用。

### 打開時一次讀六個 API，任何一個失敗就整頁失敗

- 讀 `getMyCharacter`（回 null 時視為讀取失敗）、`getAccount`（新，`GET /api/accounts/me`）、`getBuffs`、`getInn`、`getSkills`、`getCreationOptions`（`/api/public/settings`），用 `Promise.all`。
- 舊畫面在密藥或旅館讀不到時只是不顯示那部分。新稿只有一種讀取失敗，所以改成任何一個失敗就顯示讀取失敗，按「再試一次」六個重讀。
- 不用 `/api/content` 與 `getPublicJobs`：職業名稱與 Class 由技能回應的 `jobs` 帶（遵守「新畫面用專屬 API」）。
- 讀到後，倒數只用本機時鐘每秒重算；到期的列直接拿掉、不重讀。角色頁停留時間短，跟城鎮不同。

### 學技能可以指定職業，條件仍在 Domain

- `SkillTeaching` 改成描述「用來學的那個職業」：
  - `Teaches`：那個職業教這個技能；
  - `ThroughPreviousJob`：那個職業不是目前職業；
  - `PrerequisiteLevel`：照舊。
- `LearnCheck` 加 `Learned`，`Reasons` 依序 `WRONG_JOB`、`NOT_LEARNED`、`PREREQUISITE`、`MAX_LEVEL`、`NOT_ENOUGH_PROFICIENCY`、`NOT_ENOUGH_MONEY`。`NOT_LEARNED` 只在用以前的職業、技能等級 0 時出現。
- `SkillLearning.Evaluate`／`Apply` 的 `skillPoints` 改成那個職業的技能點。
- `SkillService.LearnAsync(accountId, skillId, jobId?, …)` 的流程：
  1. `jobId` 為 null 或等於目前職業時，用目前職業；
  2. 內容沒有這個職業時，回 `JOB_UNKNOWN`；
  3. 不是目前職業、也沒有那個職業的 `job_proficiency` 列時，只回 `WRONG_JOB`，不判斷其他條件；
  4. 其他情況交給 Domain 判斷。
- 扣點改成 `SpendSkillPointsAsync(characterId, chosenJobId, cost)`，repository 不變（它本來就收職業 id，條件式扣點、不足 409）。
- `LearnSkillRequest` 加 `string? JobId`。
- 新原因不另造代碼，沿用 `ErrorCodes.NotLearned`（技能欄已在用）。

**Supersedes**：設計文件 §6「學習：未擁有或可升級、前置達標、`sk_<job>` ≥ …」。原本 `<job>` 是目前職業；現在以前職業的已學技能也可以用那個職業的 `sk_` 升級。裁定彙整沒有對應條目。

替代方案：
- 前端轉回以前的職業再學。轉職會卸下全部裝備，而且使用者要的是不用轉回去，不採用。
- 以前職業的技能點併成一個池。違反每個職業各自累積的規則，不採用。

### 技能回應加每個職業的清單，舊欄位不動

- `SkillOverview` 加 `Jobs: IReadOnlyList<JobSkills>`：
  - `JobSkills(JobId, JobName, Tier, Current, SkillPoints, Skills)`；
  - 目前職業排第一，其他持有的職業照 `jobs.json` 順序。
- 每個職業的技能是 `JobSkillView`：
  - id、名稱、`slotType`、`timing`、說明、圖示、等級、最高等級；
  - `Rows`：目前等級的數值列，未學為 null；
  - 前置技能（含 `met`）；
  - `Next`：`Level`、`Money`、`SkillPoints`、`Rows`、`CanLearn`、`EnoughMoney`、`EnoughSkillPoints`。
- 都用那個職業的技能點與 `SkillLearning.Evaluate` 判斷；以前的職業只列等級 ≥ 1 的。
- 欄位用「技能點」的名字（`skillPoints`、`enoughSkillPoints`），不沿用舊 `next.proficiency`、`enoughProficiency`。
- 頂層的 `jobId`、`skillPoints`、`skills`、`slots` 不變，舊外殼技能分頁照常運作。

替代方案：改掉頂層 `skills` 的形狀。舊畫面還在用，不採用。

### 數值列在 Application 拆，文字照 SkillSummary

- `SkillSummary.RowsOf(SkillLevel)` 回 `SkillDetailRow(Kind, Text)`，`Kind` 是 `Trigger`、`Cost`、`Effect`、`Cooldown` 四種。
- 條件、效果的文字與 `Of` 共用同一組私有函式。消耗的文字拿掉「消耗」前綴：`SP 2`、`全部 SP`、`金錢 N`。
- 列名由前端決定：發動、消耗、效果、冷卻。
- `SkillSummary.Of` 不變，舊畫面與戰鬥紀錄照用。

### 角色頁的資料由 `src/game/characterScreen.ts` 的純函式算出

新增 `src/game/characterScreen.ts`：

- `characterCard(character, settings)`：名字、職業、Lv、熟練度百分比與 `值 / 3,500`、性別等名稱、HP／SP／EXP 文字與比例。
- `abilityRows(character)`：七項裸點與帶正負號的加成。
- `combatRows(character)`：六項戰鬥數值。
- `jobSwitcher(overview)`：各職業的標頭與分組；Class 的羅馬數字與「見習、正式、精英、大師、傳奇」也在這裡。
- `skillCard(skill, current)`：Lv 文字、菱形、兩欄數值列與「變了」標記、花費文字與不夠的標記、停用原因、按鈕字。
- `learnedPopup(before, after)`：升級或習得、舊與新等級、精通。
- `learnFailureText(error)`：失敗彈窗的字。
- `characterLayout(H)`：cs、dt、立繪 top、雙腳中心、內頁 top。
- `portraitOffset(jobId, sex)`：雙腳偏移表。

密藥與住宿直接用 `game/town.ts` 的 `bonusRows`。

停用原因與失敗原因的字放在 `game/skills.ts`：
- `LearnConditionText.notEnoughProficiency` 改成「技能點不足」；
- 新增 `NOT_LEARNED` 的「尚未學會這個技能」；
- 新增角色頁用的 `WRONG_JOB`「不是這個職業的技能」。

### 共用回饋元件放 `src/ui/feedback/`

- `useActionFeedback()`：回傳 `run(send)`（送出時擋點擊，0.45 秒後出墨點，出現後至少 300 毫秒）、`failure`、`closeFailure`。
- `<ActionWait>`：透明擋點擊層＋墨點底板，墨點沿用 `InkDots`。
- `<FailurePopup text onClose>`：失敗彈窗。
- `<Popup>`：彈窗外框（遮罩、紙框、「確定」、點外面關），成功彈窗與登出確認共用。
- 時長進 `motion/timings.ts`：
  - `ActionWaitDelaySeconds`（＝`InkDotsDelaySeconds`）；
  - `ActionWaitMinimumMilliseconds` 300；
  - `ActionWaitFadeInSeconds` 0.2、`ActionWaitFadeOutSeconds` 0.15；
  - `PopupInSeconds` 0.22、`PopupOutSeconds` 0.15。

替代方案：每頁自己寫等待與錯誤。使用者要通用的，不採用。

### 硬規則

- Api 只轉型，條件在 Domain `SkillLearning`。
- 不涉及內容檔、資料表、migration、scaffold 產生檔、admin 路由。
- `schema.ts` 用 `pnpm generate:api` 重新產生。
- 色碼只進 `theme.css`。
- 內容圖只經 `contentAssets.ts`／`ContentImage`。

## Implementation Contract

**行為：**

- 城門口「角色」打開角色頁；「城鎮」回城門口；舊外殼沒有角色分頁。
- 角色頁顯示登記證、立繪、錢袋、能力頁、技能頁。技能頁可以切到以前的職業，用那個職業的技能點升級已學的技能。
- 送出時有等待墨點，成功跳升級或習得彈窗，失敗跳共用失敗彈窗。
- 登出只清本機 token、回標題，沒有等待與失敗（現況就是這樣；設計稿的登出失敗不會發生）。

**介面：**

- `POST /api/skills/learn`
  - 請求：`{ skillId: string, jobId?: string | null }`；
  - 回應：`SkillOverviewResponse`，加 `jobs: JobSkillsResponse[]`。
- `JobSkillsResponse`：`{ jobId, jobName, tier, current, skillPoints, skills: JobSkillResponse[] }`。
- `JobSkillResponse`：`{ id, name, slotType, timing, description, icon, level, maxLevel, rows: SkillDetailRowResponse[] | null, prerequisite: SkillPrerequisiteCheckResponse | null, next: JobSkillNextLevelResponse | null }`。
- `JobSkillNextLevelResponse`：`{ level, money, skillPoints, rows, canLearn, enoughMoney, enoughSkillPoints }`。
- `SkillDetailRowResponse`：`{ kind: "Trigger" | "Cost" | "Effect" | "Cooldown", text }`。
- 前端 client：
  - `getAccount(): Promise<AccountVo>`；
  - `learnSkill(skillId, jobId?)`；
  - `SkillOverviewVo` 加 `jobs`。

**失敗：**

- 學習失敗回 400，帶 `reasons`；前端取第一個認得的原因顯示，否則「連線失敗」。版本衝突 409 也是「連線失敗」。
- 讀取失敗顯示讀取失敗頁，不顯示部分資料。

**驗收：**

- 後端測試：`SkillLearningTests`、`SkillServiceTests`、`SkillEndpointsTests`、`SkillSummaryTests` 的新案例。
- 前端測試：`characterScreen.test.ts`、`skillData.test.ts`、`ui/character/*.test.tsx`、`ui/feedback/*.test.tsx`、`GameScreen.test.tsx`、`AdventureScreen.test.tsx`。
- 手動逐格截圖對照 CharacterFit。
- `./scripts/check.sh` exit 0。

**範圍：**

- 在範圍內：上述 API、角色頁、共用回饋、拿掉舊角色分頁與 `CharacterScreen`、技能學習規格。
- 不在範圍內：職業頁與轉職、技能欄搬家、其他內頁改用共用回饋。

## Risks / Trade-offs

- [舊外殼技能分頁與新技能頁同時存在，兩邊都能學] → 只是過渡；兩邊都打同一個 API、條件都在 Domain，結果一致。
- [拿掉舊角色分頁後，舊外殼沒有入口可以登出] → 登出移到新角色頁，城門口隨時可進。
- [`GET /api/skills` 回應變大（每個持有的職業多一份清單與數值列）] → 尚未實測；手動驗證時記錄 Lv 162、持有 3 個職業時的回應大小，超過 50 KB 再考慮拆 API。
- [雙腳偏移是量圖得到的常數，換立繪後要重量] → 表放在 `portraitOffset`，`docs/art/README.md` 註明換圖要重量。
