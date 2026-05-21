# Hub Glossary

Hub-specific terms. Cross-cutting glossary terms (`Tenant`, `Organization`, `IModule`, `Entitlement`, etc.) live in the [LearnStack core glossary](../../learnstack/docs/glossary.md) — not duplicated here.

## Hub aggregates

**`LearnStackTenant`**
The Hub-side mirror of LearnStack core's `Tenant` aggregate. Carries metadata only — `id`, `slug`, `display_name`, `status` (Active / Suspended / Trial / Archived / Terminated), `deployment_mode` (SaaS / Dedicated / SelfHostedOnline / SelfHostedAirGapped), `created_at`, `last_phone_home_at`. Hub is the authoritative source for plan-related fields; LearnStack core is the authoritative source for operational fields. Lands in P02c-1.

**`Plan`**
The catalogue entry that defines a tier — `name`, `tier` (starter / growth / scale / enterprise / custom), `features` (JSONB), `limits` (JSONB), `compliance` (JSONB), `base_price`, `billing_cycle`, `currency`. Plans are authored by operators in the Hub UI; tenants don't see the plan catalogue directly. Lands in P02c-1.

**`HubSubscription`**
The per-tenant binding to a plan. Tracks the lifecycle state machine — `Trial → Active → PastDue → Canceled → Expired` — plus billing cycle anchors (`current_period_start`, `current_period_end`, `cancel_at_period_end`) and the payment-provider link (`stripe` / `iyzico`). Lands in P02c-1.

**`Entitlement`**
The flattened, denormalised projection of a tenant's effective feature + limit + compliance-cap set. Recomputed atomically whenever a `Plan`, `HubSubscription`, `CompliancePolicy`, or `LicenseKey` changes. Pushed to LearnStack core via `PUT /api/internal/tenants/{id}/entitlements` and mirrored into `platform_entitlement_cache`. The `generation` field is the monotonic version counter that cache invalidation rides on. Lands in P02c-1.

**`generation`**
Monotonic version counter on `Entitlement`. Incremented atomically on every recompute. Cache invalidation rides on the inequality `cached.generation >= received.generation` — older generations are rejected to prevent stale-overwrite. Never decrements.

**`LicenseKey`**
The metadata Hub stores for an RSA-2048-signed `.lic` file issued to a Self-Hosted tenant. Holds the public key hash, the embedded entitlement snapshot at issuance, `valid_from` / `valid_until` / `grace_until`, and revocation state. The signed file itself is delivered to the customer; Hub stores only metadata + the issuance audit trail. Lands in P02c-6.

**`CustomDomain`**
The per-tenant custom-domain registration. Tracks the lifecycle state machine — `Pending → Verifying → Active → Failed → Revoked` — plus DNS challenge type (`Dns01` / `Http01`), cert metadata (Vault key, expiry, last renewal), and verification attempt counter. Lands in P02c-5.

**`CompliancePolicy`**
Per-tenant compliance-cap overrides on top of the plan-default compliance set. Each row carries a `cap_key`, `allowed` (boolean), `forced` (boolean), and an optional `value`. Pushed to LearnStack core inside the entitlement projection's `compliance.caps` sub-field. Lands in P02c-5.

**`WebhookLedger`**
Append-only ingestion log for Stripe / Iyzico webhook events. Carries `provider_name`, `provider_event_id`, `event_type`, `raw_payload`, `received_at`, `processed_at`, `processing_status`. The `UNIQUE(provider_name, provider_event_id)` constraint enforces idempotency at the database level — Hub never processes the same webhook event twice. Lands in Phase 09b.

**`UsageAggregate`**
Rolled-up monthly tenant usage metric — `tenant_id`, `metric_key`, `period` (`YYYY-MM`), `value`. Hub aggregates incoming `POST /api/v1/usage/report` calls into these rows; the operator portal renders usage charts from them. Lands in P02c-2.

## Hub infrastructure terms

**`learnstack-hub` realm**
The Keycloak realm that authenticates LearnStack operators. Separate from the `learnstack` realm (which authenticates tenant users). MFA (TOTP) is required for every operator account. Tokens from this realm are rejected on tenant-facing endpoints; tokens from the `learnstack` realm are rejected on `/api/internal/*` Hub endpoints. The realm export lives in `../learnstack/infra/keycloak/realms/learnstack-hub.json` because LearnStack core's compose stack imports both realms at first boot.

**`LearnStackApiClient`**
The Hub-internal typed `HttpClient` wrapper that calls into LearnStack core's `/api/internal/*` endpoints. Carries the mTLS + RS256 JWT + HMAC-SHA256 body-signature chain. The **only** sanctioned outbound path from Hub to LearnStack — no module is allowed to call LearnStack core directly. Lands in P02c-2.

**Hub HTTPS Contract Surface**
The **closed four-endpoint** boundary between Hub and LearnStack core: `POST /api/internal/tenants`, `PUT /api/internal/tenants/{id}/entitlements`, `POST /api/v1/internal/license/verify`, `POST /api/v1/usage/report`. Adding a fifth endpoint requires a new ADR in `../learnstack/docs/decisions/`.

**Operator portal**
The Next.js single-page application (`apps/operator-portal`) deployed at `hub.learnstack.dev`. Tenant list, plan editor, custom-domain admin, license-key issuance UI, operator audit log. Single-brand (no tenant theming); accessed only via the `learnstack-hub` Keycloak realm with MFA. Lands in P02c-4.

**Hub APISIX**
The Hub-side APISIX gateway instance (port 9180 / 9543 / 9191). Separate from LearnStack core's APISIX (9080 / 9443 / 9091). Hub APISIX fronts `hub.learnstack.dev` — the operator portal BFF + Hub-side internal endpoints (license verify, usage report). LearnStack core's APISIX fronts the tenant surface + `/api/internal/*` (Hub → LearnStack direction).

**Hub Vault namespace**
The `learnstack-hub/*` path prefix inside the shared Vault instance. Hub's secrets live here so they stay namespace-isolated from LearnStack core's `learnstack/*` path prefix. Cert material from custom-domain provisioning, mTLS client certs, HMAC shared secrets all live under this prefix in production. The two namespaces share a Vault instance in dev; production may split them across separate Vault clusters.

**`learnstack.hub.*` topic prefix**
The Kafka topic-name prefix Hub uses when publishing Dapr pub/sub integration events. Examples: `learnstack.hub.entitlement`, `learnstack.hub.custom-domain.activated`, `learnstack.hub.custom-domain.deactivated`, `learnstack.hub.custom-domain.renewed`. LearnStack core publishes under `learnstack.{module}.*` without the `.hub.` segment. Topic-level ACLs in production enforce that Hub cannot publish under `learnstack.*` (non-hub) and LearnStack core cannot publish under `learnstack.hub.*`.
