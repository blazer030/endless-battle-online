#!/usr/bin/env bash
# 刪除測試殘留的 MySQL 容器（只動 label ebo.tests=true 的容器；沒有時不報錯）。
set -euo pipefail

ids="$(docker ps -aq --filter "label=ebo.tests=true")"
if [ -z "$ids" ]; then
  echo "沒有殘留的測試容器"
  exit 0
fi
# shellcheck disable=SC2086
docker rm -f -v $ids
