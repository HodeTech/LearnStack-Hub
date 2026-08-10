# Hub local infrastructure

Hub's compose stack is intentionally minimal. Most backends (Postgres, Valkey, Vault, Kafka, Keycloak, Mailpit) live in LearnStack core's compose and are reached via `host.docker.internal`. Hub's own compose runs:

| Service              | Port(s)                                      | Purpose                                                                    |
| -------------------- | -------------------------------------------- | -------------------------------------------------------------------------- |
| `postgres-hub-init`  | — (one-shot)                                 | Creates the `learnstack_hub` database in the shared Postgres on first boot |
| `dapr-placement-hub` | 50006                                        | Dapr placement service (separate from LearnStack core's 50005)             |
| `dapr-sidecar-hub`   | 3501 (HTTP), 50002 (gRPC)                    | Dapr sidecar for `learnstack-hub-api` app                                  |
| `apisix-hub`         | 9180 (HTTP), 9543 (HTTPS), 9191 (Prometheus) | Hub gateway (separate from LearnStack core's APISIX on 9080)               |

Total: ~4 services. By contrast, LearnStack core's compose stack runs 15 services.

## Boot order

```bash
# 1. LearnStack core compose (shared backends)
cd ../LearnStack && make dev

# 2. Hub-side compose
cd ../LearnStack-Hub && make dev

# 3. LearnStack core API
# `~/.dotnet/dotnet`, not plain `dotnet`: both projects pin .NET 10 in
# global.json and the system dotnet on this workstation is .NET 9.
cd ../LearnStack/backend && ~/.dotnet/dotnet run --project src/LearnStack.Api

# 4. Hub API — see the note below; this line is not the whole step.
cd ../LearnStack-Hub/backend && ~/.dotnet/dotnet run --project src/Core/LearnStack.Hub.Api
```

**Step 4 needs `.env` exported into the shell first.** The Hub host does not load `.env`
itself, so without it the connection string falls back to defaults and the API cannot
reach Postgres. The full step — the `set -a; . ./.env; set +a` sequence and the
`POSTGRES_HOST=localhost` override — is written once, in
[the repository README's Dev Workflow](../../README.md#dev-workflow). Follow it there
rather than reconstructing it here; a second copy is what let this file drift out of step
in the first place.

The boot-order dependency is dev-only. Production deploys the two stacks independently.

## Health verification

```bash
# Hub APISIX (Hub gateway)
curl -i http://localhost:9180/healthz

# Hub API (direct, bypassing the gateway)
curl -i http://localhost:5181/healthz

# Verify the shared Keycloak's learnstack-hub realm is reachable
curl -s http://localhost:8080/realms/learnstack-hub/.well-known/openid-configuration | jq .issuer
```

## Demo operator credentials

Loaded from `../LearnStack/infra/keycloak/realms/learnstack-hub.json` at LearnStack core's Keycloak first boot:

- Username: `demo-operator@learnstack.test`
- Password: `demo-dev-secret`
- Required action: `CONFIGURE_TOTP` (set up MFA on first login)

P02c-4 wires the actual OIDC + PKCE BFF integration into the operator portal.
