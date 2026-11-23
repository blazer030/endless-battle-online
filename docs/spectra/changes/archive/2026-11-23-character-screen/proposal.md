## Why

城鎮城門口的「角色」紙籤目前仍打開舊外殼的「角色」分頁（文字清單的個人資料、能力、密藥、登出）。旅館、同伴、勳章所、任務板之後，第五個換成新設計的內頁是角色頁：裝備間裡的立繪、冒險者登記證，桌上攤開的手帳（能力／技能兩頁）。這次只做第一段（登記證＋能力頁＋技能頁）；職業列表、職業詳細與轉職是下一個變更。同時，使用者 2026-11-19 決定技能頁可以切到以前當過的職業、直接用那個職業的技能點升級已學的技能，這是規則變動；技能學習目前也沒有規格。

## What Changes

- 新增角色頁畫面（設計稿：新成品 https://claude.ai/artifact/Y1nSuzweihhZm9NQEbZSts 的 CharacterFit，2026-11-19 v40 定案）：
  - 從城門口「角色」紙籤圓形展開（圓心固定）；左上「城鎮」收回同一點、回到城門口。
  - 上半：目前職業的立繪（依性別用男版或女版，腳底藏在桌緣後、依雙腳中心對齊；沒有立繪的職業顯示「立繪待補」）與冒險者登記證（名字、職業與 Lv、熟練度條、性別／屬性／出生地、HP／SP／EXP），跟著舞台高度縮放。
  - 登記證的「轉職」鈕這一段先打開舊外殼的「技能」分頁（舊分頁仍有轉職）。
  - 桌緣錢袋顯示持有金錢。
  - 手帳兩頁索引籤：
    - 能力頁：七項能力（裸點＋裝備加成）、六項戰鬥數值、「密藥與住宿」清單（住宿排第一、密藥依種類；剩餘時間格式同城鎮、每秒倒數、10 分鐘內轉紅；都沒有時「沒有生效中的密藥或住宿」）、最下面帳號名＋「登出」與確認框。
    - 技能頁：上方職業切換（只列當過的職業、依 Class 分組、寫各職業的技能點）；目前職業列全部技能（已學、未學），以前的職業只列已學的；技能卡點開看目前等級與下一級的數值（發動／消耗／效果／冷卻），下一級不同的綠字；升級列「升級需要 N 金錢 N 技能點」、不能按時下一行寫原因（前置技能：X Lv N・技能點不足・金錢不足）；滿級「已達最高等級」＋「精通」章。
  - 技能頁的花費與職業的數字從「熟練度」改叫「技能點」；登記證與職業頁的熟練度不變。
  - 打開時重讀角色、帳號、密藥、旅館、技能與名稱表；讀取中登記證用淡色塊、手帳畫空白的線；讀取失敗手帳中間一句「登記證的資料讀不到。」＋「再試一次」紅印章。
  - 升級、學習後：等伺服器時出共用的等待墨點；回來後畫面直接換成新狀態，跳「升級成功」或「習得成功」彈窗；失敗跳共用的失敗彈窗（只寫「連線失敗」或原因）。
  - 登出：確認框寫「要登出 ＜帳號＞ 嗎？」，按「登出」清掉本機登入、回標題。
- 新增全遊戲共用的動作回饋元件：等待墨點（0.45 秒後才出現、出現後至少停 0.3 秒、期間擋住點擊）與失敗彈窗（紅蠟封＋一句紅字＋「確定」）。這次只用在角色頁。
- **規則變動**（調整設計文件 `docs/superpowers/specs/2026-10-21-wog-remake-design.md` §6「學習：… `sk_<job>` ≥ …」；裁定彙整沒有對應條目）：學技能可以指定用哪個職業的技能點。用目前職業時規則不變；用以前當過的職業時，只能升級已學過、且那個職業會教的技能，扣那個職業的技能點與金錢；沒學過的技能要轉回那個職業才能學。
- 技能 API（只加欄位與選填參數，既有欄位不變）：
  - `GET /api/skills` 與 `POST /api/skills/learn` 的回應加 `jobs`：目前職業與每個當過的職業各一筆（職業 id、名稱、Class、是否目前職業、技能點、這個職業的技能清單，各自用那個職業的技能點判斷能不能升級）。
  - 技能的目前等級與下一級各帶一份拆好的數值列（發動、消耗、效果、冷卻）。
  - `POST /api/skills/learn` 加選填的 `jobId`；不帶時用目前職業。用以前的職業學沒學過的技能回 `NOT_LEARNED`；用沒當過的職業或那個職業不教的技能回 `WRONG_JOB`。
- 前端的「技能熟練度不足」改成「技能點不足」（技能頁的停用原因與失敗原因共用同一句）。
- 拿掉舊外殼的「角色」分頁與舊的 `CharacterScreen`；角色頁只從城門口紙籤進。舊外殼的「技能」分頁保留（技能欄與轉職還在那裡）。
- 補規格 `skill-learning`（描述現況：技能清單、學習條件與原因順序、花費、前置技能，加上這次的以前職業規則與回應欄位），逐條對照既有測試，缺的補上。`job-change` 的規格在下一個變更（職業頁）補。

## Capabilities

### New Capabilities

- `skill-learning`: 技能學習規則與技能 API：目前職業與以前職業的技能清單、學習與升級的條件與拒絕原因順序、花費（金錢＋該職業技能點）、前置技能、滿級、回應裡每個職業的技能清單與拆好的數值列。
- `character-screen`: 角色頁畫面：從城門口進出、立繪與登記證、錢袋、能力頁（能力、戰鬥數值、密藥與住宿、登出）、技能頁（職業切換、技能卡、升級與學習）、成功彈窗、讀取中與讀取失敗、版位隨舞台高度。
- `action-feedback`: 全遊戲共用的等待墨點與失敗彈窗：何時出現、擋點擊、最短停留、錯誤文字。

### Modified Capabilities

- `town-screen`: 「角色」改成打開角色頁、返回城門口；舊外殼不再有角色分頁。

## Impact

- Domain：`backend/EndlessBattle.Domain/Skills/SkillLearning.cs`（以前職業的條件與原因）。
- Application：`backend/EndlessBattle.Application/Skills/`（`SkillService` 的每職業清單與指定職業學習、`SkillSummary` 拆數值列、`SkillOverview`）。
- Api：`backend/EndlessBattle.Api/Endpoints/SkillEndpoints.cs`、`backend/EndlessBattle.Api/Endpoints/Contracts.cs`；`backend/openapi/` 重新產生。
- Tests：`backend/EndlessBattle.Tests` 的技能規格對照與新規則、新欄位測試。
- frontend：
  - `frontend/src/api/`：`schema.ts` 重新產生、技能轉換器加 `jobs` 與數值列、`learnSkill` 帶 `jobId`、新增讀帳號（`GET /api/accounts/me`）。
  - `frontend/src/game/`：角色頁的純模型（登記證、能力列、技能卡、職業切換、版位、立繪站位）；`skills.ts` 的原因文字。
  - `frontend/src/ui/character/`：角色頁、登記證、手帳兩頁、成功彈窗。
  - `frontend/src/ui/feedback/`：共用等待墨點與失敗彈窗。
  - `frontend/src/ui/GameScreen.tsx`：角色改成新畫面、回城門口；轉職鈕打開舊外殼技能分頁。
  - `frontend/src/ui/AdventureScreen.tsx`：拿掉角色分頁；刪 `frontend/src/ui/CharacterScreen.tsx`、`CharacterScreen.css` 與測試。
  - `frontend/src/assets/character/`：裝備間、登記證、手帳、錢袋等設計素材。
  - `frontend/src/ui/stage/screenImages.ts`：城鎮預載角色頁素材。
  - `frontend/src/motion/timings.ts`：等待墨點與彈窗的時長。
  - `frontend/src/ui/theme.css`：新色碼。
- 文件：`docs/art/README.md` 設計素材清單；`.claude/skills/ebo-canvas` 板子表加 CharacterFit；`docs/superpowers/specs/2026-10-23-deferred-backlog.md` 拿掉只跟舊 `CharacterScreen` 有關的條目。
