#!/usr/bin/env bash
# 提交前檢查：全部通過才可以 commit，成敗以 exit code 為準
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$repository_root/backend"
dotnet build -warnaserror
dotnet test
