#!/usr/bin/env bash
# 提交前檢查：全部通過才可以 commit，成敗以 exit code 為準
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$repository_root/scripts/check-sql.sh"

# 只驗證 compose 設定（含 profile app），不實際起容器；完整 stack 用 smoke-compose.sh。
# 用 .env.example 而不是本機 .env，結果才不受個人設定影響；斷言需要 jq
compose_config() {
    env -u EBO_JWT_SIGNING_KEY docker compose --project-directory "$repository_root" --file "$repository_root/docker-compose.yml" \
        --profile app --env-file "$1" config "${@:2}"
}

command -v jq > /dev/null || { echo "check failed: jq is required (e.g. brew install jq)" >&2; exit 1; }
compose_config "$repository_root/.env.example" --quiet

mysql_host_ip="$(compose_config "$repository_root/.env.example" --format json | jq -r '.services.mysql.ports[0].host_ip // empty')"
if [[ "$mysql_host_ip" != "127.0.0.1" ]]; then
    echo "check failed: mysql must publish on 127.0.0.1 only (got '${mysql_host_ip:-all interfaces}')" >&2
    exit 1
fi

if compose_config /dev/null --quiet > /dev/null 2>&1; then
    echo "check failed: compose must refuse to start without EBO_JWT_SIGNING_KEY" >&2
    exit 1
fi

"$repository_root/scripts/check-api-types.sh"
# 自我測試要重複建置好幾次，只有這兩支腳本有改動時才跑
if [[ -n "$(git -C "$repository_root" status --porcelain -- scripts/check-api-types.sh scripts/check-api-types-test.sh)" ]]; then
    "$repository_root/scripts/check-api-types-test.sh"
fi

cd "$repository_root/backend"
dotnet test

cd "$repository_root/frontend" && pnpm check

cd "$repository_root/admin" && pnpm check

cd "$repository_root/tools/assets" && pnpm check

cd "$repository_root/tools/content-sheet" && pnpm check
