# Hub Keycloak realm

Hub does **not** run its own Keycloak instance. The `learnstack-hub` realm is imported by LearnStack core's compose stack at first boot from:

```
../learnstack/infra/keycloak/realms/learnstack-hub.json
```

This shared-instance + two-realm topology is the dev-time convenience documented in [ADR-0004 Amendment 1](../../../learnstack/docs/decisions/0004-authentication-strategy.md):

- **`learnstack` realm** — tenant users (admins, instructors, learners). Lives in `../learnstack/infra/keycloak/realms/learnstack.json`.
- **`learnstack-hub` realm** — LearnStack operators. Lives in `../learnstack/infra/keycloak/realms/learnstack-hub.json`. MFA (TOTP) required for every operator account.

The realm boundary is **non-negotiable**:

- Tenant-facing endpoints **reject** `learnstack-hub` realm tokens (`iss` claim check).
- Hub `/api/internal/*` endpoints **reject** `learnstack` realm tokens.
- Architecture test `Hub_Operator_JWT_NeverAccepted_On_LearnStack_Routes` enforces (lands in P02c-3 LearnStack-side PR).

## Operator portal client

The `learnstack-hub` realm carries a single public PKCE client:

| Field              | Value                                                      |
| ------------------ | ---------------------------------------------------------- |
| `clientId`         | `learnstack-hub-web`                                       |
| Auth flow          | Authorization Code + PKCE (S256)                           |
| Redirect URI (dev) | `http://localhost:3100/*`                                  |
| MFA                | Required (`CONFIGURE_TOTP` `requiredAction` on every user) |

## Roles

| Role                 | Scope                                                                                        |
| -------------------- | -------------------------------------------------------------------------------------------- |
| `hub-platform-admin` | Full Hub administration — plan CRUD, tenant lifecycle, custom-domain admin, license issuance |
| `hub-operator`       | Day-to-day operator — tenant support, entitlement overrides, audit review                    |
| `hub-billing-viewer` | Read-only access to billing + invoice surfaces (Phase 09b lights up the surfaces)            |

## Production posture

Phase 11 revisits whether Hub deploys its own Keycloak instance or continues to share LearnStack core's instance with realm separation. The dev-time shared instance is a convenience; the realm-token rejection rule is independent of deployment topology.

## Ownership of the realm JSON

The realm JSON (`learnstack-hub.json`) physically lives in the LearnStack core repo because LearnStack's compose stack imports both realms at first boot. Phase 11 may move authoritative ownership to this repo (with LearnStack's compose mounting from `../learnstack-hub/infra/keycloak/realms/`) — but as long as the dev-time shared Keycloak holds both realms, single-source-of-truth keeps the file in LearnStack core.
