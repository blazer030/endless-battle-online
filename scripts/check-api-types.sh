#!/usr/bin/env bash
# build 會重新產生 backend/openapi/，再從文件產生兩份 schema.ts。內容因此改變，代表忘了產生或手改過；
# 和 git 不同，代表產生了但還沒 git add，commit 時可能漏掉。成敗以 exit code 為準，失敗時列出檔案
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

api_type_files=(backend/openapi frontend/src/api/schema.ts admin/src/api/schema.ts)
api_type_hashes() {
    git -C "$repository_root" ls-files --cached --others --exclude-standard -- "${api_type_files[@]}" | while read -r file; do
        echo "$(git -C "$repository_root" hash-object "$file") $file"
    done
}
api_types_before="$(api_type_hashes)"

(cd "$repository_root/backend" && dotnet build -warnaserror)
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
