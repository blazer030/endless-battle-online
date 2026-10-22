# Endless Battle Online

現代奇幻風的單人放置 RPG，WOG 4.5 的單機翻新版。後端 .NET 10 Web API＋MySQL 8.4，前端 React＋PixiJS。

## 執行手冊

### 需求

- 只想玩：Docker 與 Docker Compose v2（`docker compose version` 有輸出即可）。
- 要開發：另需 .NET SDK 10.0.100 以上、Node 24、pnpm 11。

### 一鍵啟動

```bash
cp .env.example .env
# 編輯 .env：至少把 EBO_JWT_SIGNING_KEY 換成自己的隨機字串（至少 32 個字元）
docker compose --profile app up -d --build
```

啟動後開 <http://localhost:8080>。第一次會建置映像並建立資料庫，需要幾分鐘。

服務一覽：

| 服務     | 用途                                               | 對外的埠                    |
| -------- | -------------------------------------------------- | --------------------------- |
| `mysql`  | MySQL 8.4，存玩家資料（volume `mysql-data`）       | `EBO_MYSQL_PORT`（預設 3306） |
| `flyway` | 套用 `backend/db` 的 migration 後結束              | —                           |
| `api`    | ASP.NET Core API，flyway 成功結束後才啟動          | —（只在內部網路）           |
| `web`    | nginx：前端靜態檔，`/api/` 轉給 `api`              | `EBO_WEB_PORT`（預設 8080） |

確認狀態：

```bash
docker compose --profile app ps          # web 應為 healthy
curl http://localhost:8080/api/health    # {"status":"ok"}；資料庫連不上時回 503
```

### 設定（`.env`）

| 變數                   | 說明                                                                 |
| ---------------------- | -------------------------------------------------------------------- |
| `EBO_JWT_SIGNING_KEY`  | 登入 token 的簽章金鑰，至少 32 個字元。預設值只供開發，**對外開放前必須更換**。更換後既有登入全部失效。 |
| `EBO_DB_USER`          | 遊戲用的資料庫帳號                                                   |
| `EBO_DB_PASSWORD`      | 遊戲用的資料庫密碼（不要含分號）                                     |
| `EBO_DB_ROOT_PASSWORD` | MySQL root 密碼                                                      |
| `EBO_MYSQL_PORT`       | MySQL 對外的埠                                                       |
| `EBO_WEB_PORT`         | 網頁對外的埠                                                         |

資料庫帳密只在第一次建立 `mysql-data` volume 時生效；之後要改，得先清資料（見下方）或自己進 MySQL 改。

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

```bash
# 備份（密碼換成 .env 的 EBO_DB_PASSWORD）
docker compose exec -T mysql mysqldump -uebo -pebo_dev --no-tablespaces endless_battle > backup.sql

# 還原到執行中的 mysql
docker compose exec -T mysql mysql -uebo -pebo_dev endless_battle < backup.sql
```

### 疑難排解

- **3306 或 8080 已被占用**：在 `.env` 改 `EBO_MYSQL_PORT`／`EBO_WEB_PORT` 後重新 `up -d`。
- **`api` 起不來或一直重啟**：`docker compose logs api`。常見原因是 `EBO_JWT_SIGNING_KEY` 不到 32 個字元，或 `flyway` 失敗（`docker compose logs flyway`）。
- **網頁打得開但操作失敗**：`curl http://localhost:8080/api/health`；回 503 表示 API 連不上資料庫。
- **改了 `.env` 的資料庫帳密後連不上**：帳密只在 volume 第一次建立時寫入，見「設定」。

## 開發

日常開發不需要 `--profile app`：

```bash
docker compose up -d                                   # 只起 mysql 與 flyway
cd backend && dotnet run --project EndlessBattle.Api   # API：http://localhost:5062
cd frontend && pnpm install && pnpm dev                # 前端，/api proxy 到 5062
./scripts/check.sh                                     # 提交前檢查
./scripts/smoke-compose.sh                             # 改 Dockerfile、nginx 或 compose 後手動跑
```

其餘指令、結構與規則見 [`CLAUDE.md`](CLAUDE.md)。
