#!/usr/bin/env bash
# The API seeds itself when Seeding__Enabled=true. This script re-applies
# the seed by wiping the DB and bouncing the API. Dev-only.
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo_root"

docker compose -f infrastructure/docker/docker-compose.yml stop api >/dev/null
docker compose -f infrastructure/docker/docker-compose.yml exec -T mongo \
  mongosh --quiet --eval 'db.getSiblingDB("serenauth").dropDatabase()'
docker compose -f infrastructure/docker/docker-compose.yml start api
echo "Seed cycle complete."
