# Contract with LearnStack core

Hub's boundary with LearnStack core is defined by a **closed four-endpoint HTTPS contract surface**. Adding a fifth endpoint requires a new ADR. This file is a pointer to the authoritative LearnStack-side documents — duplication is **deliberately avoided**.

## Authoritative source documents

| Source                                | Location                                                                                                                       |
| ------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| **Decision rationale**                | [ADR-0019 LearnStack Hub](../../../learnstack/docs/decisions/0019-learnstack-hub.md)                                           |
| **Architecture deep dive**            | [Architecture 24 LearnStack Hub](../../../learnstack/docs/architecture/24-learnstack-hub.md)                                   |
| **Closed surface rule**               | [Standards 20 § Hub HTTPS Contract Surface](../../../learnstack/docs/standards/20-infrastructure-stack.md)                     |
| **Auth strategy (realm boundary)**    | [ADR-0004 Amendment 1](../../../learnstack/docs/decisions/0004-authentication-strategy.md)                                     |
| **Triple deployment model + license** | [ADR-0020](../../../learnstack/docs/decisions/0020-triple-deployment-hybrid-license.md)                                        |
| **Feature-based entitlement**         | [ADR-0021](../../../learnstack/docs/decisions/0021-feature-based-entitlement.md)                                               |
| **Custom-domain + TLS lifecycle**     | [ADR-0022](../../../learnstack/docs/decisions/0022-custom-domain-tls.md) (Amendment 1 — Hub never writes LearnStack K8s state) |

## The four endpoints (quick reference)

| Direction        | Method + Path                                 | Purpose                                          | Hosted in           |
| ---------------- | --------------------------------------------- | ------------------------------------------------ | ------------------- |
| Hub → LearnStack | `POST /api/internal/tenants`                  | Create tenant + default org                      | **LearnStack core** |
| Hub → LearnStack | `PUT /api/internal/tenants/{id}/entitlements` | Push entitlement projection (incl. host mapping) | **LearnStack core** |
| LearnStack → Hub | `POST /api/v1/internal/license/verify`        | License verify                                   | **Hub**             |
| LearnStack → Hub | `POST /api/v1/usage/report`                   | Usage telemetry                                  | **Hub**             |

Every call carries:

- **mTLS** — LearnStack-internal CA-signed client cert
- **RS256 JWT** — `aud=learnstack-internal`, `exp ≤ 5min`, signed by the `learnstack-hub` Keycloak realm service account
- **HMAC-SHA256** body signature in `X-Signature` header — per-deployment shared secret from Vault

## What Hub MUST NOT do

- Add a fifth endpoint without a new ADR (filed in `../../../learnstack/docs/decisions/`).
- Hold Kubernetes credentials on the LearnStack cluster. Cert + route propagation flows through Dapr pub/sub events + the entitlement push (ADR-0022 Amendment 1).
- Trust `learnstack` realm JWTs on Hub endpoints, or expect LearnStack to trust `learnstack-hub` realm JWTs on tenant-facing endpoints.
- Store tenant content (courses, lessons, learners, enrollments, classroom sessions). Hub holds metadata only.

## Phase 02c packet ownership

| Packet | Endpoint work                                                                                                                                                             |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| P02c-2 | Hub-side: `POST /api/v1/internal/license/verify` + `POST /api/v1/usage/report` handlers. Outbound `LearnStackApiClient` with mTLS + JWT + HMAC chain.                     |
| P02c-3 | LearnStack-side (paired PR): `POST /api/internal/tenants` + `PUT /api/internal/tenants/{id}/entitlements` handlers. `HubEntitlementProvider` + `IUsageReporter` adapters. |
| P02c-5 | LearnStack-side custom-domain handler reacts to `learnstack.hub.custom-domain.activated/.deactivated/.renewed` Dapr events.                                               |
