#!/usr/bin/env bash
# 煙霧測試：用獨立的專案名與埠起整個 stack（--profile app），經 nginx 打 API 與前端，結束一律拆掉並刪 volume。
# 不影響開發用的 docker compose（專案名不同）。成敗以 exit code 為準。
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

export EBO_WEB_PORT=18080
export EBO_MYSQL_PORT=13306
# compose 沒有金鑰會拒絕執行；煙霧測試一律用專用的值，不依賴 .env（只影響這個臨時 stack）
export EBO_JWT_SIGNING_KEY="${EBO_JWT_SIGNING_KEY:-smoke-only-signing-key-0123456789abcdef}"
base_url="http://localhost:$EBO_WEB_PORT"
timeout_seconds=180

compose() {
    docker compose --project-name ebo-smoke --profile app "$@"
}

trap 'compose down -v --remove-orphans > /dev/null 2>&1 || true' EXIT

compose up -d --build

deadline=$((SECONDS + timeout_seconds))
until curl -fsS "$base_url/api/health" > /dev/null 2>&1; do
    if ((SECONDS >= deadline)); then
        echo "smoke failed: $base_url/api/health did not answer within ${timeout_seconds}s" >&2
        compose logs --tail 50 api web >&2 || true
        exit 1
    fi
    sleep 3
done

mysql_binding="$(compose port mysql 3306)"
if [[ "$mysql_binding" != 127.0.0.1:* ]]; then
    echo "smoke failed: mysql is published on $mysql_binding, expected 127.0.0.1 only" >&2
    exit 1
fi

index_html="$(curl -fsS "$base_url/")"
if [[ "$index_html" != *'<div id="root">'* ]]; then
    echo "smoke failed: $base_url/ did not serve the SPA" >&2
    exit 1
fi

if ! curl -fsS -o /dev/null "$base_url/api/content"; then
    echo "smoke failed: $base_url/api/content did not return 200" >&2
    exit 1
fi

# 經 Cloudflare Tunnel 對外的測試環境不讓搜尋引擎收錄：頁面、靜態檔與 API 都要帶 noindex
asset_path="$(grep -o '/assets/[^"]*\.js' <<< "$index_html" | head -n 1)"
for path in / "$asset_path" /api/health; do
    if ! curl -fsS -o /dev/null -D - "$base_url$path" | grep -qi '^x-robots-tag: noindex, nofollow'; then
        echo "smoke failed: $base_url$path has no X-Robots-Tag: noindex, nofollow" >&2
        exit 1
    fi
done

echo "smoke passed"
