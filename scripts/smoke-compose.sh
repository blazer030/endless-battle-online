#!/usr/bin/env bash
# 煙霧測試：用獨立的專案名與埠起整個 stack（--profile app），經 nginx 打 API 與前端，結束一律拆掉並刪 volume。
# 不影響開發用的 docker compose（專案名不同）。成敗以 exit code 為準。
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

export EBO_WEB_PORT=18080
export EBO_MYSQL_PORT=13306
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

index_html="$(curl -fsS "$base_url/")"
if [[ "$index_html" != *'<div id="root">'* ]]; then
    echo "smoke failed: $base_url/ did not serve the SPA" >&2
    exit 1
fi

if ! curl -fsS -o /dev/null "$base_url/api/content"; then
    echo "smoke failed: $base_url/api/content did not return 200" >&2
    exit 1
fi

echo "smoke passed"
