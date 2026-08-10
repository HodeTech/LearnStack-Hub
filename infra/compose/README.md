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
cd ../LearnStack/backend && dotnet run --project src/LearnStack.Api

# 4. Hub API
cd ../LearnStack-Hub/backend && dotnet run --project src/LearnStack.Hub.Api
```

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
