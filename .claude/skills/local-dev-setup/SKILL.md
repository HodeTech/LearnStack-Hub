---
name: local-dev-setup
description: >
  Bring up the LearnStack Hub local dev stack in the right order — the shared
  LearnStack core backends (Postgres, Valkey, Vault, Kafka, Keycloak, Mailpit) first,
  then the Hub-only services (Hub Dapr sidecar, Hub APISIX, the learnstack_hub
  database init), then the Hub API. USE FOR: first-time workstation setup, restoring a
  broken local environment, getting the entitlement/integration flows runnable. DO NOT
  USE FOR: production deployment, CI environment config (CI uses Testcontainers
  directly), or LearnStack core's own stack setup (that's the sibling repo's skill).
---

# Hub local dev setup

## Purpose

Get the Hub stack running. Hub's compose is deliberately minimal — it runs only Hub-specific services and consumes LearnStack core's shared backends via `host.docker.internal`. Boot order matters: LearnStack core compose first, then Hub.

## When to use

- First-time setup on a fresh workstation.
- Restoring a broken local environment.
- Needing the Hub stack runnable for an integration / manual check.

## When not to use

- Production deployment (separate; Helm, Phase 11).
- CI (uses Testcontainers directly, not compose).
- LearnStack core's stack (the sibling repo's own `local-dev-setup`).

## Prerequisites

- The sibling repo at `../learnstack` (Hub depends on it for shared backends + the Keycloak realms).
- Docker daemon (OrbStack / Docker Desktop).
- **.NET 10 SDK** at `~/.dotnet/dotnet` (system `dotnet` is .NET 9). `export PATH="$HOME/.dotnet:$PATH"`.
- pnpm 9.12.3 + Node ≥ 20.11 for the operator portal.

## Workflow

### Step 1 — Bootstrap (one-time)

```bash
cd ~/Documents/Projects/learnstack-hub
make install      # restores backend NuGet + frontend pnpm + activates .githooks/pre-commit
```

### Step 2 — LearnStack core compose FIRST (shared backends)

```bash
cd ../learnstack && make dev
```

This brings up Postgres (5432), Valkey (6379), Vault (8200), Kafka (9092), Keycloak (8080, with **both** the `learnstack` and `learnstack-hub` realms imported from `../learnstack/infra/keycloak/realms/`), and Mailpit. Hub does NOT run these — it reaches them via `host.docker.internal`.

### Step 3 — Hub compose

```bash
cd ../learnstack-hub && make dev
```

Brings up the Hub-only services: Dapr placement (50006), Dapr sidecar (3501/50002), Hub APISIX (9180/9543/9191), and the `postgres-hub-init` one-shot that creates the `learnstack_hub` database in the shared Postgres. If LearnStack core compose isn't up, `postgres-hub-init` waits up to 120s then fails with an actionable message — start LearnStack core first.

### Step 4 — Hub API

```bash
cd ../learnstack-hub/backend
~/.dotnet/dotnet run --project src/Core/LearnStack.Hub.Api    # binds 0.0.0.0:5181
```

### Step 5 — Operator portal (when working on it)

```bash
cd ../learnstack-hub/frontend && pnpm dev    # operator-portal on 3100
```

### Step 6 — Seed (optional)

```bash
cd ../learnstack-hub && make seed    # demo plans + demo tenant + demo operator (P02c-1+ fills real data)
```

## Health verification

```bash
curl -i http://localhost:9180/healthz                         # Hub APISIX
curl -i http://localhost:5181/healthz                         # Hub API direct
curl -s http://localhost:8080/realms/learnstack-hub/.well-known/openid-configuration | jq .issuer
# expect "http://localhost:8080/realms/learnstack-hub"
docker compose -f ../learnstack/infra/compose/dev.yml exec postgres psql -U learnstack -lqt | grep learnstack_hub
```

## Port map (no collisions with LearnStack core)

| Service                  | LearnStack core    | Hub                |
| ------------------------ | ------------------ | ------------------ |
| APISIX HTTP/HTTPS/Prom   | 9080 / 9443 / 9091 | 9180 / 9543 / 9191 |
| Dapr placement           | 50005              | 50006              |
| Dapr sidecar HTTP / gRPC | 3500 / 50001       | 3501 / 50002       |
| API host                 | 5080               | 5181               |
| Operator portal          | (apps/web 3000)    | 3100               |

Shared (Hub consumes via `host.docker.internal`): Postgres 5432, Valkey 6379, Vault 8200, Kafka 9092, Keycloak 8080, Mailpit 1025/8025.

## Common pitfalls

- **Starting Hub compose before LearnStack core.** `postgres-hub-init` hangs ~120s then fails. Start `../learnstack` first.
- **The dotnet PATH trap.** `dotnet run` fails on the SDK pin without `~/.dotnet` on PATH.
- **Expecting Hub to run its own Keycloak.** It doesn't — the `learnstack-hub` realm lives in LearnStack core's Keycloak (the realm JSON is in `../learnstack/infra/keycloak/realms/`).
- **Port confusion.** Hub deliberately offsets every port from LearnStack core; check the map above.
