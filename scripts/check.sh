#!/usr/bin/env bash
# 提交前檢查：全部通過才可以 commit，成敗以 exit code 為準
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$repository_root/scripts/check-sql.sh"

# 只驗證 compose 設定（含 profile app）能解析，不實際起容器；完整 stack 用 smoke-compose.sh
docker compose --project-directory "$repository_root" --file "$repository_root/docker-compose.yml" --profile app config --quiet

cd "$repository_root/backend"
dotnet build -warnaserror
dotnet test

cd "$repository_root/frontend" && pnpm install --frozen-lockfile && pnpm check
