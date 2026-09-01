#!/usr/bin/env bash
# Boots Mongo + API + Web via docker-compose.
# Requires a .env file at the repo root (see .env.example).

set -euo pipefail

repo_root="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo_root"

if [[ ! -f .env ]]; then
  echo ".env not found. Run: cp .env.example .env && edit values" >&2
  exit 1
fi

# Pass .env into docker compose explicitly.
docker compose \
  --env-file ./.env \
  -f infrastructure/docker/docker-compose.yml \
  up --build "$@"
