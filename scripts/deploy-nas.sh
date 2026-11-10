#!/usr/bin/env bash
# 把目前的 commit 部署到 NAS（Synology）：上傳程式、第一次部署時產生 .env、在 NAS 上 build 並啟動，最後從本機確認服務。
# 只部署已 commit 的內容；玩家存檔在 docker volume ebo_mysql-data，重新部署不會動到。成敗以 exit code 為準。
#
# NAS 上的 docker 要 sudo，所以要在自己的終端機執行，中途會問 NAS 的密碼。
# 只想上傳、改用 Container Manager 重新建置專案 ebo 時，執行時加 < /dev/null：上傳完會停下並以 exit 1 結束。
# NAS 位置與 Cloudflare Tunnel 的 token 放本機 .env.nas.local（NAS_HOST、NAS_DIR、NAS_TUNNEL_TOKEN，範本 .env.nas.local.example）；
# 也可用環境變數 EBO_NAS_HOST、EBO_NAS_DIRECTORY 覆寫。NAS_HOST 可列好幾個位置（空白隔開，例如區網、Tailscale），用第一個連得上的。EBO_WEB_PORT 只在第一次產生 .env 時生效，之後改 NAS 上的 .env。
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

# 本機設定 .env.nas.local（git 忽略，範本 .env.nas.local.example）；不 source，逐欄讀出，去掉引號與 CR
local_setting() {
    local value=""
    if [[ -f .env.nas.local ]]; then
        value="$(sed -n "s/^$1=//p" .env.nas.local | tail -n 1 | tr -d '\r')"
        value="${value#[\"\']}"
        value="${value%[\"\']}"
    fi
    printf '%s' "$value"
}

# 優先順序：環境變數 → .env.nas.local → 預設值
nas_host="${EBO_NAS_HOST:-$(local_setting NAS_HOST)}"
nas_host="${nas_host:-blazer@192.168.2.216}"

# 列了好幾個位置時，依序用 ssh 金鑰實際登入試，用第一個連得上的；
# 主機金鑰對不上也算連不上（例如別的網路剛好有同一個區網位址），會換下一個
pick_reachable_host() {
    local candidate
    for candidate in "$@"; do
        if ssh -o BatchMode=yes -o ConnectTimeout=3 "$candidate" true 2> /dev/null; then
            printf '%s' "$candidate"
            return 0
        fi
    done
    return 1
}

read -r -a nas_host_candidates <<< "$nas_host"
if (( ${#nas_host_candidates[@]} > 1 )); then
    if ! nas_host="$(pick_reachable_host "${nas_host_candidates[@]}")"; then
        echo "連不到 NAS（試過：${nas_host_candidates[*]}）" >&2
        exit 1
    fi
fi
nas_directory="${EBO_NAS_DIRECTORY:-$(local_setting NAS_DIR)}"
nas_directory="${nas_directory:-/volume1/docker/ebo}"
tunnel_token="$(local_setting NAS_TUNNEL_TOKEN)"
initial_web_port="${EBO_WEB_PORT:-8080}"
health_timeout_seconds=180

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

# Cloudflare Tunnel 的 token：本機 .env.nas.local（git 忽略）有 NAS_TUNNEL_TOKEN 時，經 ssh stdin 寫進 NAS 的 .env，不出現在指令列也不輸出。
if [[ -n "$tunnel_token" ]]; then
    if [[ "$tunnel_token" =~ [[:space:]] ]]; then
        echo "部署失敗：.env.nas.local 的 NAS_TUNNEL_TOKEN 含有空白，請重新複製（--token 後面那一串）" >&2
        exit 1
    fi
    printf '%s\n' "$tunnel_token" |
        ssh "$nas_host" "cd $quoted_directory && umask 077 && IFS= read -r token && { grep -v '^TUNNEL_TOKEN=' .env || true; printf 'TUNNEL_TOKEN=%s\n' \"\$token\"; } > .env.new && chmod 600 .env.new && mv .env.new .env"
    echo "已更新 NAS .env 的 TUNNEL_TOKEN"
fi

# 先解到暫存目錄再換上去，上傳中斷時不會留下半套程式。
# compose 預設讀專案目錄的 .env；Container Manager 不帶 --profile，所以 profile 也寫在裡面。
# .env 有 TUNNEL_TOKEN 時才啟用 tunnel（Cloudflare Tunnel，https://ebo-test.blazer030.dev）。
git archive --format=tar HEAD docker-compose.yml backend frontend |
    ssh "$nas_host" "cd $quoted_directory && rm -rf app.new && mkdir app.new && tar -xf - -C app.new && cp -p .env app.new/.env && profiles=app && if grep -q '^TUNNEL_TOKEN=.' .env; then profiles=app,tunnel; fi && echo COMPOSE_PROFILES=\$profiles >> app.new/.env && echo \"啟用的 profile：\$profiles\" && rm -rf app && mv app.new app"
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
