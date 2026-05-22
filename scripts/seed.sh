#!/usr/bin/env bash
# LearnStack Hub — local dev seed.
#
# Idempotent. P02c-0 ships only the orchestrator shape — no real seed data
# yet. As Hub modules land, this script grows to provision:
#   - P02c-1: Demo plans (Starter, Growth, Scale, Enterprise) via the Plans
#     module HTTP API.
#   - P02c-1: Demo tenant + default subscription via the TenantLifecycle
#     module.
#   - P02c-4: Demo operator account (already loaded from the Keycloak realm
#     export; this step verifies MFA setup).
#
# Pre-requisites (asserted below):
#   1. LearnStack core compose is running (Postgres + Keycloak + Vault +
#      Kafka + Valkey reachable on host.docker.internal).
#   2. Hub compose is running (Dapr sidecar + APISIX + postgres-hub-init
#      completed).
#   3. Hub API is running (localhost:5181).

set -eu -o pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

# ─── Color helpers ──────────────────────────────────────────────────────
if [[ -t 1 ]]; then
    CYAN='\033[36m'
    RED='\033[31m'
    GREEN='\033[32m'
    RESET='\033[0m'
else
    CYAN=''
    RED=''
    GREEN=''
    RESET=''
fi

info()  { printf "${CYAN}[seed] %s${RESET}\n" "$*"; }
ok()    { printf "${GREEN}[seed] %s${RESET}\n" "$*"; }
fail()  { printf "${RED}[seed] %s${RESET}\n" "$*" >&2; exit 1; }

# ─── Pre-flight checks ──────────────────────────────────────────────────
# The P02c-1 seed is DB-only (plans + demo tenant via the in-process seeder),
# so it needs only Postgres. Keycloak / APISIX are informational here; they
# become hard requirements once the operator-portal + API-bound seed steps land.
info "Pre-flight checks..."

if curl -fsS http://localhost:8080/realms/learnstack-hub/.well-known/openid-configuration > /dev/null 2>&1; then
    ok "  Keycloak learnstack-hub realm: OK"
else
    info "  Keycloak learnstack-hub realm not reachable (not required for the P02c-1 DB seed)"
fi

if curl -fsS http://localhost:9180/healthz > /dev/null 2>&1; then
    ok "  Hub APISIX: OK"
else
    info "  Hub APISIX not reachable (not required for the P02c-1 DB seed)"
fi

# ─── Seed data (P02c-1: 4 plan tiers + demo tenant) ─────────────────────
# Load .env so POSTGRES_* (incl. the password) come from the dev environment,
# never a literal here. The in-process seeder (dotnet run -- --seed) applies
# migrations then provisions the catalogue idempotently. Host-run reaches
# Postgres on localhost rather than the container's host.docker.internal.
if [[ -f .env ]]; then
    set -a
    # shellcheck disable=SC1091
    . ./.env
    set +a
fi
export POSTGRES_HOST=localhost

info "Seeding plan catalogue + demo tenant (dotnet run -- --seed)..."
if command -v dotnet > /dev/null 2>&1; then
    dotnet run --project backend/src/Core/LearnStack.Hub.Api/LearnStack.Hub.Api.csproj -- --seed
    ok "  Plan catalogue + demo tenant seeded."
else
    info "  dotnet not on PATH — run manually: dotnet run --project backend/src/Core/LearnStack.Hub.Api -- --seed"
fi

# Demo credentials printed for the operator:
cat <<EOF

${GREEN}Hub dev demo credentials:${RESET}
  ─────────────────────────────────────────────────────
  Operator portal: http://localhost:3100 (run: cd frontend && pnpm dev)
  Keycloak realm:  learnstack-hub
  Username:        demo-operator@learnstack.test
  Password:        demo-dev-secret
  Required action: CONFIGURE_TOTP (set up MFA on first login)
  ─────────────────────────────────────────────────────

${GREEN}Hub services:${RESET}
  Hub APISIX:      http://localhost:9180
  Hub API:         http://localhost:5181
  Hub Dapr HTTP:   http://localhost:3501

${GREEN}Shared services (from LearnStack core compose):${RESET}
  Postgres:        host.docker.internal:5432 (db: learnstack_hub)
  Keycloak:        http://localhost:8080 (learnstack-hub realm)
  Vault:           http://localhost:8200
  Mailpit UI:      http://localhost:8025

EOF

ok "Seed complete."
