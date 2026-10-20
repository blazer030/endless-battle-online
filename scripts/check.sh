#!/usr/bin/env bash
# 提交前檢查：全部通過才可以 commit，成敗以 exit code 為準
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

"$repository_root/scripts/check-sql.sh"

cd "$repository_root/backend"
dotnet build -warnaserror
dotnet test

cd "$repository_root/frontend" && pnpm install --frozen-lockfile && pnpm check
