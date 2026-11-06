#!/usr/bin/env bash
# 擋下不符命名規則的 migration 檔名
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
migration_directory="${1:-$repository_root/backend/db}"

found_problem=0

while IFS= read -r -d '' migration_file; do
    file_name="$(basename "$migration_file")"
    if [[ ! "$file_name" =~ ^V[0-9]{9}__[a-z0-9_]+\.sql$ ]]; then
        echo "Invalid migration file name: $migration_file"
        found_problem=1
    fi
done < <(find "$migration_directory" -type f -name "*.sql" -print0)

if [ "$found_problem" -ne 0 ]; then
    exit 1
fi

echo "SQL check passed"
