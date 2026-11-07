#!/usr/bin/env bash
# check-api-types.sh 的測試：先故意弄髒檔案，確認它會失敗並列出檔名。結束時一律還原
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
schema="admin/src/api/schema.ts"
stray_document="backend/openapi/stray-test-document.json"

restore() {
    rm -f "$repository_root/$stray_document"
    (cd "$repository_root/admin" && pnpm generate:api) > /dev/null 2>&1
}
trap restore EXIT

fail() {
    echo "check-api-types test failed: $1" >&2
    exit 1
}

# 兩個前端都從文件檔產生型別，不需要 API 執行中
for project in frontend admin; do
    command="$(jq -r '.scripts["generate:api"]' "$repository_root/$project/package.json")"
    [[ "$command" == *"../backend/openapi/"* && "$command" != *"http"* ]] || fail "$project generates types from: $command"
done

expect_failure_naming() {
    local file="$1" output
    if output="$("$repository_root/scripts/check-api-types.sh" 2>&1)"; then
        fail "the check passed although $file is out of date"
    fi
    [[ "$output" == *"$file"* ]] || fail "the check did not name $file"
}

# 手改或忘了重新產生的 schema.ts
echo "// stale" >> "$repository_root/$schema"
expect_failure_naming "$schema"
git -C "$repository_root" diff --quiet -- "$schema" || fail "the check did not regenerate $schema"

# 產生了但還沒加入 git 的文件
echo "{}" > "$repository_root/$stray_document"
expect_failure_naming "$stray_document"

echo "check-api-types test passed"
