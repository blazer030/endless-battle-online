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

api_type_files=(backend/openapi frontend/src/api/schema.ts admin/src/api/schema.ts)
api_type_hashes() {
    git -C "$repository_root" ls-files --cached --others --exclude-standard -- "${api_type_files[@]}" | while read -r file; do
        echo "$(git -C "$repository_root" hash-object "$file") $file"
    done
}
api_types_before="$(api_type_hashes)"

cd "$repository_root/backend"
dotnet build -warnaserror

# build 會重新產生 backend/openapi/，再從文件產生兩份 schema.ts。內容因此改變，代表忘了產生或手改過；
# 和 git 不同，代表產生了但還沒 git add，commit 時可能漏掉
(cd "$repository_root/frontend" && pnpm install --frozen-lockfile && pnpm generate:api)
(cd "$repository_root/admin" && pnpm install --frozen-lockfile && pnpm generate:api)
changed_api_files="$(diff <(echo "$api_types_before") <(api_type_hashes) | sed -nE 's/^> [0-9a-f]+ //p' || true)"
if [[ -n "$changed_api_files" ]]; then
    echo "check failed: API documents or types were out of date and have been regenerated. Review them and git add:" >&2
    echo "$changed_api_files" >&2
    exit 1
fi
unstaged_api_files="$(git -C "$repository_root" diff --name-only -- "${api_type_files[@]}"; git -C "$repository_root" ls-files --others --exclude-standard -- "${api_type_files[@]}")"
if [[ -n "$unstaged_api_files" ]]; then
    echo "check failed: regenerated API documents or types are not staged. Review them and git add:" >&2
    echo "$unstaged_api_files" >&2
    exit 1
fi

dotnet test

cd "$repository_root/frontend" && pnpm check

cd "$repository_root/admin" && pnpm check
