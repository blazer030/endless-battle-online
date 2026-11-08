#!/usr/bin/env bash
# check-api-types.sh 的測試：先故意弄髒檔案，確認它會失敗並列出檔名。結束時一律還原
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
schema="admin/src/api/schema.ts"
stray_document="backend/openapi/stray-test-document.json"
document="backend/openapi/EndlessBattle.Api_admin.json"

restore() {
    rm -f "$repository_root/$stray_document" "$repository_root/$document.bak"
    (cd "$repository_root/admin" && pnpm generate:api) > /dev/null 2>&1
    # 文件可能在中途被手改：重新產生後放回暫存區
    if [[ "$document_touched" == true ]]; then
        rm -f "$repository_root/backend/EndlessBattle.Api/obj/EndlessBattle.Api.OpenApiFiles.cache"
        (cd "$repository_root/backend" && dotnet build EndlessBattle.Api) > /dev/null 2>&1
        git -C "$repository_root" add -- "$document"
    fi
}
document_touched=false
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
rm -f "$repository_root/$stray_document"

# 手改文件又 git add：程式沒變時建置不會重新產生，要靠檢查強制產生才抓得到
document_touched=true
sed -i.bak 's/"openapi"/"openapi" /' "$repository_root/$document"
rm -f "$repository_root/$document.bak"
git -C "$repository_root" add -- "$document"
expect_failure_naming "$document"
git -C "$repository_root" add -- "$document"
"$repository_root/scripts/check-api-types.sh" > /dev/null 2>&1 || fail "the check still failed after regenerating $document"

echo "check-api-types test passed"
