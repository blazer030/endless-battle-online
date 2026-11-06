# Endless Battle Online

現代奇幻風的單人放置 RPG，WOG 4.5 的單機翻新版。後端 .NET 10 Web API＋MySQL 8.4，前端 React＋PixiJS。

## Endless Battle Online 是什麼

把 WOG 4.5 的單人遊戲系統原汁原味搬過來（能力值、職業樹與轉職、6 格裝備、技能熟練度、21 個地區、商店、旅館、寵物、傭兵、任務），去掉公會、拍賣、PK、組隊、聊天、交易所等多人部分，把當年要靠外掛的掛機做成正式的放置玩法，畫面換成現代奇幻風格、手機直式優先。目標是老玩家一眼認得出「這是 WOG」：數字長法、職業名、地名都熟悉，但不用再開外掛，手機也能玩。

## 系統一覽

| 系統 | 內容 | 截圖 |
|---|---|---|
| 角色 | 七項能力與衍生值，創角選性別、屬性、出生地、職業並配點；角色頁顯示能力、裝備與生效中的密藥 | [創角](docs/superpowers/screenshots/plan-3-create.png)、[角色](docs/superpowers/screenshots/plan-3-character.png)、[密藥](docs/superpowers/screenshots/plan-10-elixir.png) |
| 戰鬥 | WOG 回合制（先攻、命中、暴擊、屬性、40 回合上限）、怪物技能與武器附帶技能、自動補給、死亡與住宿；離線時間在回來時一次結算，舞台播放最後一段戰鬥 | [戰鬥](docs/superpowers/screenshots/plan-4-battle.png)、[離線摘要](docs/superpowers/screenshots/plan-4-summary.png) |
| 職業與技能 | 職業樹與轉職、熟練度、16 個技能的學習、裝備與戰鬥施放 | [技能](docs/superpowers/screenshots/plan-5-skills.png) |
| 世界與禁區 | 21 個地區（四大區加禁區）與子地區、推薦等級；4 個禁區要鑰匙，進入後有 360 分鐘的通行時間 | [地圖](docs/superpowers/screenshots/plan-6-world.png)、[禁區](docs/superpowers/screenshots/plan-10-restricted.png) |
| 道具與經濟 | 背包、商店、販售、倉庫、銀行、補給品、精鍊／鑲嵌／合成（含異類精鍊石合成）；魔石寶箱、精鍊寶盒、寵物蛋、心得書、經驗膠囊、密藥 | [背包](docs/superpowers/screenshots/plan-7-bag.png)、[消耗品](docs/superpowers/screenshots/plan-10-consumables.png) |
| 任務 | 一般任務、每日任務（台北時間 09:00 重置）、殺怪計數與交付 | [任務](docs/superpowers/screenshots/plan-8-missions.png) |
| 同伴 | 捕捉、餵食寵物並帶著參戰；傭兵在背景打自己的時間軸；勳章所兌換道具與傭兵 | [寵物](docs/superpowers/screenshots/plan-9-pets.png)、[舞台上的寵物](docs/superpowers/screenshots/plan-9-stage-pet.png)、[傭兵](docs/superpowers/screenshots/plan-9-mercenary.png) |
| 冒險設定 | WOG 戰鬥設定面板：攻擊方式、補給／住宿／逃跑門檻 | [設定](docs/superpowers/screenshots/plan-10-settings.png) |

一鍵啟動後的畫面見 [plan-10-compose.png](docs/superpowers/screenshots/plan-10-compose.png)；較早的截圖（[計畫 2 冒險頁](docs/superpowers/screenshots/plan-2-adventure.png)、[計畫 3 冒險頁](docs/superpowers/screenshots/plan-3-adventure.png)、[計畫 3 主題](docs/superpowers/screenshots/plan-3-theme.png)）都在 `docs/superpowers/screenshots/`。

## 文件地圖

| 文件 | 內容 |
|---|---|
| [`docs/superpowers/specs/2026-10-21-wog-remake-design.md`](docs/superpowers/specs/2026-10-21-wog-remake-design.md) | 設計文件 v2：意圖、各系統規則、畫面方向、技術架構、路線圖 |
| [`docs/superpowers/specs/2026-10-22-wog-remake-rulings.md`](docs/superpowers/specs/2026-10-22-wog-remake-rulings.md) | 裁定彙整：各計畫的裁定與 Claude 的判斷／設計／簡化、明確不做的清單 |
| [`docs/superpowers/specs/2026-10-23-deferred-backlog.md`](docs/superpowers/specs/2026-10-23-deferred-backlog.md) | 計畫 10 結束時仍未處理的延後項目 |
| [`docs/superpowers/plans/`](docs/superpowers/plans/) | 計畫 1～10 的實作計畫（任務、測試名、內容表） |
| [`docs/research/README.md`](docs/research/README.md) | 研究文件索引：WOG 原始碼研究與內容總表 |
| [`docs/art/README.md`](docs/art/README.md) | 舞台美術規格、生圖提示詞範本（`prompts.md`）與匯入流程 |
| [`CLAUDE.md`](CLAUDE.md) | 專案結構、硬規則、指令、資料庫變更流程 |

## 執行手冊

### 需求

- 只想玩：Docker 與 Docker Compose v2（`docker compose version` 有輸出即可）。
- 要開發：另需 .NET SDK 10.0.100 以上、Node 24、pnpm 11。

### 一鍵啟動

```bash
cp .env.example .env
# 編輯 .env：把 EBO_JWT_SIGNING_KEY 換成自己的隨機字串（至少 32 個字元）；
# 要對外開放的話，EBO_DB_PASSWORD 與 EBO_DB_ROOT_PASSWORD 也要先換掉
docker compose --profile app up -d --build
```

沒有 `.env`（或其中沒有 `EBO_JWT_SIGNING_KEY`）時，`docker compose` 會直接報錯拒絕執行，不會用公開的預設金鑰簽 token。MySQL 的埠只綁在本機（`127.0.0.1`），對外只有網頁的埠。

啟動後開 <http://localhost:8080>。第一次會建置映像並建立資料庫，需要幾分鐘。

服務一覽：

| 服務     | 用途                                               | 對外的埠                    |
| -------- | -------------------------------------------------- | --------------------------- |
| `mysql`  | MySQL 8.4，存玩家資料（volume `mysql-data`）       | `EBO_MYSQL_PORT`（預設 3306，只綁 127.0.0.1） |
| `flyway` | 套用 `backend/db` 的 migration 後結束              | —                           |
| `api`    | ASP.NET Core API，flyway 成功結束後才啟動          | —（只在內部網路）           |
| `web`    | nginx：前端靜態檔，`/api/` 轉給 `api`              | `EBO_WEB_PORT`（預設 8080） |

確認狀態：

```bash
docker compose --profile app ps          # web 應為 healthy
curl http://localhost:8080/api/health    # {"status":"ok"}；資料庫連不上時回 503
```

`api` 沒有 healthcheck，`web` 只等它「啟動」而不是「就緒」，所以剛起來的幾秒內 `/api/` 可能回 502，稍等再試即可。

### 設定（`.env`）

| 變數                   | 說明                                                                 |
| ---------------------- | -------------------------------------------------------------------- |
| `EBO_JWT_SIGNING_KEY`  | 登入 token 的簽章金鑰，至少 32 個字元，**必填**（沒有時 compose 拒絕執行）。`.env.example` 的值只供開發，**對外開放前必須更換**。更換後既有登入全部失效。 |
| `EBO_DB_USER`          | 遊戲用的資料庫帳號                                                   |
| `EBO_DB_PASSWORD`      | 遊戲用的資料庫密碼（不要含分號）。範本值只供開發，**對外開放前必須更換** |
| `EBO_DB_ROOT_PASSWORD` | MySQL root 密碼。範本值只供開發，**對外開放前必須更換**              |
| `EBO_MYSQL_PORT`       | MySQL 在本機（127.0.0.1）的埠                                        |
| `EBO_WEB_PORT`         | 網頁對外的埠                                                         |

資料庫帳密只在第一次建立 `mysql-data` volume 時生效；之後要改，得先清資料（見下方）或自己進 MySQL 改。

值裡有 `$` 時，compose 會把它當成變數插值：寫成 `$$`（例如 `EBO_DB_PASSWORD=pa$$word` 代表 `pa$word`），或整個值用單引號包起來（`EBO_DB_PASSWORD='pa$word'`）。

### 更新

```bash
git pull
docker compose --profile app up -d --build
```

新的 migration 由 `flyway` 自動套用，`api` 會等它成功後才重啟。

### 看 log

```bash
docker compose --profile app logs -f api web
docker compose logs flyway               # migration 失敗時看這裡
```

### 停止與清資料

```bash
docker compose --profile app down        # 停止並移除容器，存檔保留
docker compose --profile app down -v     # 連 volume 一起刪：所有存檔都會消失
```

### 備份與還原

帳密直接用 `mysql` 容器裡的 `MYSQL_USER`／`MYSQL_PASSWORD`（由 `.env` 的 `EBO_DB_USER`／`EBO_DB_PASSWORD` 帶入），改過 `.env` 也不用改指令：

```bash
# 備份
docker compose exec -T mysql sh -c 'mysqldump -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" --no-tablespaces endless_battle' > backup.sql

# 還原到執行中的 mysql
docker compose exec -T mysql sh -c 'mysql -u"$MYSQL_USER" -p"$MYSQL_PASSWORD" endless_battle' < backup.sql
```

### 疑難排解

- **3306 或 8080 已被占用**：在 `.env` 改 `EBO_MYSQL_PORT`／`EBO_WEB_PORT` 後重新 `up -d`。
- **`required variable EBO_JWT_SIGNING_KEY is missing a value`**：還沒建立 `.env`，或其中沒有 `EBO_JWT_SIGNING_KEY`；先 `cp .env.example .env` 再修改。
- **`api` 起不來或一直重啟**：`docker compose logs api`。常見原因是 `EBO_JWT_SIGNING_KEY` 不到 32 個字元，或 `flyway` 失敗（`docker compose logs flyway`）。
- **網頁打得開但操作失敗**：`curl http://localhost:8080/api/health`；回 503 表示 API 連不上資料庫，剛啟動時回 502 表示 API 還沒就緒。
- **改了 `.env` 的資料庫帳密後連不上**：帳密只在 volume 第一次建立時寫入，見「設定」。

## 開發

日常開發不需要 `--profile app`，但同樣要先有 `.env`（`cp .env.example .env`，開發可沿用範本值）：

```bash
docker compose up -d                                   # 只起 mysql 與 flyway
cd backend && dotnet run --project EndlessBattle.Api   # API：http://localhost:5062
cd frontend && pnpm install && pnpm dev                # 前端，/api proxy 到 5062
./scripts/check.sh                                     # 提交前檢查（需要 jq）
./scripts/smoke-compose.sh                             # 改 Dockerfile、nginx 或 compose 後手動跑
```

其餘指令、結構與規則見 [`CLAUDE.md`](CLAUDE.md)。
