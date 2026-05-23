#!/usr/bin/env bash
# Appends the SerenAuth install + ship walkthrough to a Notion page.
#
# Usage:
#   NOTION_TOKEN=ntn_xxx ./infrastructure/scripts/push-to-notion.sh
#
# Optional:
#   NOTION_PAGE_ID  (defaults to the "SerenAuth Install" page)
#
# Requirements:
#   - curl, jq, python3 (for safe JSON encoding)
#   - The Notion integration whose token you pass must be shared on the page.

set -euo pipefail

: "${NOTION_TOKEN:?Set NOTION_TOKEN to your Notion integration secret (ntn_...)}"
PAGE_ID="${NOTION_PAGE_ID:-366d6da3-0d95-8007-b66d-c7a757aac6b6}"

NOTION_VERSION="2022-06-28"
API="https://api.notion.com/v1"

# --- Sanity check: can we read the page? ---------------------------------
echo "→ verifying access to page $PAGE_ID"
status=$(curl -sS -o /tmp/notion_page.json -w "%{http_code}" \
  -H "Authorization: Bearer $NOTION_TOKEN" \
  -H "Notion-Version: $NOTION_VERSION" \
  "$API/pages/$PAGE_ID")
if [[ "$status" != "200" ]]; then
  echo "✗ Notion API returned HTTP $status:" >&2
  jq -r '.code + ": " + .message' /tmp/notion_page.json 2>/dev/null >&2 || cat /tmp/notion_page.json >&2
  echo "" >&2
  echo "Fixes:" >&2
  echo "  - Confirm NOTION_TOKEN is the Internal Integration Secret (ntn_...)" >&2
  echo "  - On the page in Notion: Share → Invite → add your integration" >&2
  exit 1
fi
echo "✓ access OK"

# --- Build block payloads with python (handles JSON safely) --------------
python3 - "$PAGE_ID" <<'PY' > /tmp/notion_blocks.json
import json, sys

def h2(text):
    return {"object":"block","type":"heading_2","heading_2":{"rich_text":[rt(text)]}}
def h3(text):
    return {"object":"block","type":"heading_3","heading_3":{"rich_text":[rt(text)]}}
def p(text):
    return {"object":"block","type":"paragraph","paragraph":{"rich_text":[rt(text)]}}
def bullet(text):
    return {"object":"block","type":"bulleted_list_item","bulleted_list_item":{"rich_text":[rt(text)]}}
def todo(text):
    return {"object":"block","type":"to_do","to_do":{"rich_text":[rt(text)],"checked":False}}
def code(text, lang="shell"):
    return {"object":"block","type":"code","code":{"rich_text":[rt(text)],"language":lang}}
def divider():
    return {"object":"block","type":"divider","divider":{}}
def rt(text):
    return {"type":"text","text":{"content":text}}

blocks = []

blocks += [h2("0. Prerequisites")]
for b in [
    "Docker Desktop (Compose v2)",
    ".NET 8 SDK",
    "Node 20.x and npm 10+",
    "openssl, git",
]:
    blocks.append(bullet(b))
blocks.append(code(
"docker --version && docker compose version\n"
"dotnet --version       # 8.0.x\n"
"node --version         # v20.x",
    "shell"))

blocks += [h2("1. First-time setup")]
blocks.append(code(
"cd /Users/christianbryant/prod-ready/SerenAuth\n"
"cp .env.example .env\n"
"JWT=$(./infrastructure/scripts/gen-jwt-secret.sh)\n"
'sed -i \'\' "s|^Jwt__SigningKey=.*|Jwt__SigningKey=${JWT}|" .env\n'
"grep '^Jwt__SigningKey=' .env | cut -c1-40",
    "shell"))
blocks.append(p("Open .env and review the rest — Mongo__ConnectionString, Cors__AllowedOrigins, Seeding__Enabled (leave true for the demo, false in prod)."))

blocks += [h2("2. Local dev — Docker path (recommended)")]
blocks.append(code("./infrastructure/scripts/dev-up.sh", "shell"))
blocks.append(p("URLs:"))
for b in [
    "Web: http://localhost:3000",
    "GraphQL endpoint: http://localhost:8080/graphql",
    "Liveness: http://localhost:8080/health/live",
    "Readiness: http://localhost:8080/health/ready",
]:
    blocks.append(bullet(b))
blocks.append(h3("Stop"))
blocks.append(code(
"docker compose -f infrastructure/docker/docker-compose.yml --env-file ./.env down\n"
"# add -v to wipe the mongo-data volume",
    "shell"))
blocks.append(h3("Re-seed"))
blocks.append(code("./infrastructure/scripts/seed.sh", "shell"))
blocks.append(h3("Tail logs"))
blocks.append(code(
"docker compose -f infrastructure/docker/docker-compose.yml --env-file ./.env logs -f api\n"
"docker compose -f infrastructure/docker/docker-compose.yml --env-file ./.env logs -f web",
    "shell"))

blocks += [h2("3. Local dev — manual path")]
blocks.append(p("Start Mongo standalone:"))
blocks.append(code(
"docker run -d --name serenauth-mongo \\\n"
"  -p 27017:27017 \\\n"
"  -e MONGO_INITDB_ROOT_USERNAME=serenauth \\\n"
"  -e MONGO_INITDB_ROOT_PASSWORD=serenauth \\\n"
"  mongo:7",
    "shell"))
blocks.append(p("Run the API:"))
blocks.append(code(
"set -a; source .env; set +a\n"
'export Mongo__ConnectionString="mongodb://serenauth:serenauth@localhost:27017/?authSource=admin"\n'
"dotnet run --project src/SerenAuth.Api",
    "shell"))
blocks.append(p("Run the web app:"))
blocks.append(code(
"cd apps/web\n"
"npm ci\n"
'NEXT_PUBLIC_GRAPHQL_ENDPOINT="http://localhost:8080/graphql" npm run dev',
    "shell"))

blocks += [h2("4. Demo users (seeded)")]
blocks.append(code(
"admin@riverbend.example  / ChangeMe!123  (Admin)\n"
"clin@riverbend.example   / ChangeMe!123  (Clinician)\n"
"intake@riverbend.example / ChangeMe!123  (Intake)",
    "plain text"))
blocks.append(p("Set the bearer token in the browser:"))
blocks.append(code(
'localStorage.setItem("serenauth.token", "eyJhbGc...")\n'
"location.reload()",
    "javascript"))
blocks.append(p("Smoke-test GraphQL:"))
blocks.append(code(
'TOKEN="paste-jwt-here"\n'
"curl -sS http://localhost:8080/graphql \\\n"
'  -H "Authorization: Bearer $TOKEN" \\\n'
'  -H "Content-Type: application/json" \\\n'
'  -d \'{"query":"{ priorAuthorizations(limit:5){ id payer status aiConfidence } }"}\' | jq',
    "shell"))

blocks += [h2("5. Running tests")]
blocks.append(p("Backend (unit + integration via Testcontainers):"))
blocks.append(code('dotnet test SerenAuth.sln --collect:"XPlat Code Coverage"', "shell"))
blocks.append(p("Backend coverage HTML report:"))
blocks.append(code(
"dotnet tool install -g dotnet-reportgenerator-globaltool\n"
'export PATH="$PATH:$HOME/.dotnet/tools"\n'
"reportgenerator \\\n"
"  -reports:'**/TestResults/**/coverage.cobertura.xml' \\\n"
"  -targetdir:coverage/report \\\n"
"  -reporttypes:'HtmlSummary;TextSummary;Cobertura'\n"
"open coverage/report/index.html",
    "shell"))
blocks.append(p("Frontend:"))
blocks.append(code(
"cd apps/web\n"
"npm ci\n"
"npm run lint\n"
"npm run test:ci\n"
"npm run build",
    "shell"))

blocks += [h2("6. Shipping")]
blocks.append(h3("6.1 Pre-flight"))
for b in [
    "Rotate Jwt__SigningKey and store in a secret manager",
    "Set Seeding__Enabled=false",
    "Lock Cors__AllowedOrigins to your real web origin",
    "Terminate TLS at proxy; HSTS auto-on outside Development",
    "Use MongoDB Atlas with encryption-at-rest and a scoped DB user",
    "Confirm GraphQL introspection is off (default outside Development)",
]:
    blocks.append(bullet(b))

blocks.append(h3("6.2 Build production images"))
blocks.append(code(
"SHA=$(git rev-parse --short HEAD)\n"
"docker build -t ghcr.io/<org>/serenauth-api:$SHA -f infrastructure/docker/Dockerfile.api .\n"
"docker build -t ghcr.io/<org>/serenauth-web:$SHA -f infrastructure/docker/Dockerfile.web .",
    "shell"))

blocks.append(h3("6.3 Push images"))
blocks.append(code(
'echo "$GHCR_TOKEN" | docker login ghcr.io -u <user> --password-stdin\n'
"docker push ghcr.io/<org>/serenauth-api:$SHA\n"
"docker push ghcr.io/<org>/serenauth-web:$SHA\n"
"docker tag ghcr.io/<org>/serenauth-api:$SHA ghcr.io/<org>/serenauth-api:latest\n"
"docker push ghcr.io/<org>/serenauth-api:latest",
    "shell"))

blocks.append(h3("6.4 Deploy (single host, Compose-style)"))
blocks.append(p("Create docker-compose.prod.yml referencing the pushed images. Put secrets in /etc/serenauth.env (mode 600). Front the API and web with Caddy or Nginx for TLS termination."))
blocks.append(code(
"docker compose --env-file /etc/serenauth.env -f docker-compose.prod.yml up -d",
    "shell"))

blocks.append(h3("6.5 Production checklist"))
for b in [
    "BAAs signed with MongoDB Atlas and cloud provider",
    "Jwt__SigningKey ≥ 64 bytes, rotated quarterly, from secret manager",
    "TLS on every external hop; HSTS preloaded",
    "CORS allowlist is exactly the production origin(s)",
    "Seeding__Enabled=false in prod",
    "Atlas encryption-at-rest, IP allowlist, scoped DB user",
    "Backups verified by restore drills",
    "Audit-events retention aligned with IR plan",
    "Rate-limit + WAF in front of the API",
    "Logs streamed off-host with correlation IDs",
    "Health probes wired into orchestrator (/health/live, /health/ready)",
    "All CI gates (tests + coverage + CodeQL + dependency review + gitleaks) required on main",
]:
    blocks.append(todo(b))

blocks += [h2("7. Troubleshooting")]
for sym, fix in [
    ("Jwt__SigningKey must be set", "You did not replace the placeholder in .env. Re-run the gen-jwt-secret step."),
    ("Mongo:ConnectionString is required", ".env not loaded; run `set -a; source .env; set +a` first."),
    ("GraphQL returns Unauthorized.", "Token missing/expired or issuer/audience do not match what minted it."),
    ('Web shows "Failed to load authorizations."', "No bearer token in localStorage; set serenauth.token and reload."),
    ("Dashboard table is empty", "Seeding off on a fresh DB, or JWT org claim doesn't match a seeded org id."),
    ("Testcontainers cannot pull mongo:7", "Docker Desktop is down or you are offline."),
]:
    blocks.append(p(f"• {sym} — {fix}"))

# Notion appends in chunks of 100. We have <100, so a single call works.
print(json.dumps({"children": blocks}))
PY

# --- Append to the page ---------------------------------------------------
echo "→ posting blocks to page..."
status=$(curl -sS -o /tmp/notion_post.json -w "%{http_code}" -X PATCH \
  -H "Authorization: Bearer $NOTION_TOKEN" \
  -H "Notion-Version: $NOTION_VERSION" \
  -H "Content-Type: application/json" \
  -d @/tmp/notion_blocks.json \
  "$API/blocks/$PAGE_ID/children")

if [[ "$status" == "200" ]]; then
  echo "✓ posted to Notion (HTTP 200)"
  echo "  Page: https://www.notion.so/$PAGE_ID"
else
  echo "✗ HTTP $status:" >&2
  jq -r '.code + ": " + .message' /tmp/notion_post.json 2>/dev/null >&2 || cat /tmp/notion_post.json >&2
  exit 1
fi
