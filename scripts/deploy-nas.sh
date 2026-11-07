#!/usr/bin/env bash
# 把目前的 commit 部署到 NAS（Synology）：上傳程式、第一次部署時產生 .env、在 NAS 上 build 並啟動，最後從本機確認服務。
# 只部署已 commit 的內容；玩家存檔在 docker volume ebo_mysql-data，重新部署不會動到。成敗以 exit code 為準。
#
# NAS 上的 docker 要 sudo，所以要在自己的終端機執行，中途會問 NAS 的密碼。
# 只想上傳、改用 Container Manager 重新建置專案 ebo 時，執行時加 < /dev/null：上傳完會停下並以 exit 1 結束。
# 可用環境變數覆寫：EBO_NAS_HOST、EBO_NAS_DIRECTORY；EBO_WEB_PORT 只在第一次產生 .env 時生效，之後改 NAS 上的 .env。
set -euo pipefail

nas_host="${EBO_NAS_HOST:-blazer@192.168.2.216}"
nas_directory="${EBO_NAS_DIRECTORY:-/volume1/docker/ebo}"
initial_web_port="${EBO_WEB_PORT:-8080}"
health_timeout_seconds=180

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

quoted_directory="$(printf %q "$nas_directory")"
nas_address="${nas_host#*@}"

if [[ -n "$(git status --porcelain --untracked-files=no)" ]]; then
    echo "注意：有未 commit 的修改，這些修改不會部署。"
fi
echo "部署 $(git log -1 --format='%h %s') 到 $nas_host:$nas_directory"

# 金鑰與密碼在 NAS 上產生，不經過本機也不輸出。資料庫帳密只在第一次建立 volume 時生效，所以 .env 存在就不覆寫。
ssh "$nas_host" "nas_directory=$quoted_directory initial_web_port=$initial_web_port sh -s" << 'REMOTE'
set -eu
mkdir -p "$nas_directory"
cd "$nas_directory"
if [ ! -f .env ]; then
    umask 077
    signing_key="$(openssl rand -hex 32)"
    database_password="$(openssl rand -hex 24)"
    database_root_password="$(openssl rand -hex 24)"
    cat > .env.new << ENVIRONMENT
EBO_JWT_SIGNING_KEY=$signing_key
EBO_DB_USER=ebo
EBO_DB_PASSWORD=$database_password
EBO_DB_ROOT_PASSWORD=$database_root_password
EBO_MYSQL_PORT=13306
EBO_WEB_PORT=$initial_web_port
ENVIRONMENT
    chmod 600 .env.new
    mv .env.new .env
    echo "已產生 $nas_directory/.env"
fi
REMOTE

# 先解到暫存目錄再換上去，上傳中斷時不會留下半套程式。
# compose 預設讀專案目錄的 .env；Container Manager 不帶 --profile，所以 profile 也寫在裡面。
git archive --format=tar HEAD docker-compose.yml backend frontend |
    ssh "$nas_host" "cd $quoted_directory && rm -rf app.new && mkdir app.new && tar -xf - -C app.new && cp -p .env app.new/.env && echo COMPOSE_PROFILES=app >> app.new/.env && rm -rf app && mv app.new app"
echo "程式已上傳"

if [[ ! -t 0 ]]; then
    echo "這裡沒有終端機可以輸入 sudo 密碼，容器沒有更新。用 Container Manager 的話，到那裡重新建置專案 ebo；要用腳本啟動，請在自己的終端機執行。" >&2
    exit 1
fi

compose_command="sudo /usr/local/bin/docker compose --project-name ebo"
# build 失敗時舊的容器會繼續跑
ssh -t "$nas_host" "cd $quoted_directory/app && $compose_command up -d --build --remove-orphans"

web_port="$(ssh "$nas_host" "sed -n 's/^EBO_WEB_PORT=//p' $quoted_directory/.env")"
base_url="http://$nas_address:$web_port"

deadline=$((SECONDS + health_timeout_seconds))
until curl -fsS --max-time 5 "$base_url/api/health" > /dev/null 2>&1; do
    if ((SECONDS >= deadline)); then
        echo "部署失敗：$base_url/api/health 在 ${health_timeout_seconds} 秒內沒有回應" >&2
        echo "看紀錄：ssh -t $nas_host \"cd $quoted_directory/app && $compose_command logs --tail 100 api web\"" >&2
        exit 1
    fi
    sleep 3
done

if [[ "$(curl -fsS --max-time 10 "$base_url/")" != *'<div id="root">'* ]]; then
    echo "部署失敗：$base_url/ 沒有回傳遊戲頁面" >&2
    exit 1
fi

if ! curl -fsS --max-time 30 -o /dev/null "$base_url/api/content"; then
    echo "部署失敗：$base_url/api/content 沒有回傳 200" >&2
    exit 1
fi

echo "部署完成：$base_url"
