#!/usr/bin/env bash
# 從本機 MySQL（docker compose）重新產生 EF Core model 與 DbContext。
# 只是用固定參數呼叫內建的 dotnet ef dbcontext scaffold；產生的檔案不得手改。
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"

# compose 沒有 EBO_JWT_SIGNING_KEY 會拒絕執行，連 exec 也一樣
[[ -f .env ]] || { echo "Missing .env: run 'cp .env.example .env' first (compose refuses to run without EBO_JWT_SIGNING_KEY)." >&2; exit 1; }

table_names="$(docker compose exec -T mysql mysql -uebo -pebo_dev -N -e \
    "SELECT table_name FROM information_schema.tables WHERE table_schema = 'endless_battle' AND table_name <> 'flyway_schema_history' ORDER BY table_name" \
    2>/dev/null)"

if [ -z "$table_names" ]; then
    echo "No tables found. Run 'docker compose up -d' first." >&2
    exit 1
fi

table_arguments=()
while IFS= read -r table_name; do
    table_arguments+=(--table "$table_name")
done <<< "$table_names"

cd "$repository_root/backend"
dotnet tool restore > /dev/null
dotnet ef dbcontext scaffold "Name=ConnectionStrings:EndlessBattle" MySql.EntityFrameworkCore \
    --project EndlessBattle.Infrastructure --startup-project EndlessBattle.Api \
    --output-dir Persistence/Models --context-dir Persistence --context EndlessBattleDbContext \
    --namespace EndlessBattle.Infrastructure.Persistence.Models \
    --context-namespace EndlessBattle.Infrastructure.Persistence \
    --no-onconfiguring --force "${table_arguments[@]}"
