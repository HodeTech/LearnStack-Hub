# Hub Operations

Hub-specific operational runbooks. Phase 11 fills this directory with the production-side procedures; P02c-0 ships only the placeholder.

## Planned runbooks (Phase 11)

- `deployment.md` — Hub Helm chart, environment config for three deployment modes, rollout cadence.
- `mtls-cert-rotation.md` — Hub client-cert rotation procedure (mTLS to LearnStack core's `/api/internal/*`).
- `hmac-secret-rotation.md` — HMAC body-signature shared-secret rotation between Hub and every LearnStack core deployment.
- `cert-manager-and-letsencrypt.md` — Custom-domain TLS provisioning ops (DNS-01 / HTTP-01, renewal job tuning, Let's Encrypt rate-limit handling).
- `license-key-issuance.md` — Operator runbook for issuing signed `.lic` files for Self-Hosted tenants.
- `incident-response.md` — Hub outage response — fall-back to cached entitlement projection grace period; operator portal degraded mode.
- `keycloak-realm-bootstrap.md` — Production `learnstack-hub` realm provisioning (Terraform / keycloak-config-cli).
- `webhook-replay.md` — Stripe / Iyzico webhook replay (Phase 09b).
- `database-backup.md` — Hub DB backup + restore procedure.

## Out of scope here

- LearnStack core operational runbooks (those live in `../../../LearnStack/docs/operations/` once Phase 11 adds them).
- Local-dev setup (see [../../README.md § Dev Workflow](../../README.md#dev-workflow) and [../../infra/compose/README.md](../../infra/compose/README.md)).
