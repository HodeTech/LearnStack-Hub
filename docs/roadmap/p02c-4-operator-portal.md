# P02c-4: Operator Portal MVP

> **Status: ⏳ Not started.** Depends on [P02c-1](p02c-1-hub-domain-core.md) and [P02c-2](p02c-2-internal-api-and-contract.md). Runs in parallel with [P02c-3](p02c-3-learnstack-integration.md) — this packet touches no LearnStack code.

## Goal

Give a Hub operator a browser.

Everything [P02c-1](p02c-1-hub-domain-core.md) and
[P02c-2](p02c-2-internal-api-and-contract.md) build is reachable only through `curl` and
`psql`. Tenant provisioning, plan binding and entitlement recompute are real operations
with real consequences, and until this packet ships they are performed by whoever is
willing to hand-write a JSON body. That is not an operations model — it is a temporary
condition that quietly becomes permanent.

P02c-4 turns the Hub domain into an application: an authenticated, MFA-gated,
audit-recorded operator surface where provisioning a tenant is a form and reading a
tenant's entitlement is a page. It also lands the two modules that make operator actions
attributable — `Operators` (identity and permissions) and `Audit` (the operator audit
stream) — which is why several shells left open in P02c-1 close here rather than earlier.

## Scope

### Authentication — `learnstack-hub` realm, PKCE, BFF

- OIDC Authorization Code flow with **PKCE** against the `learnstack-hub` Keycloak realm.
  The `learnstack` realm is never accepted; the two-realm boundary from
  [ADR-0004 Amendment 1](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0004-authentication-strategy.md)
  is enforced on both sides of the login — the portal validates issuer and `azp`, and the
  Hub API rejects any token whose issuer is the tenant realm.
- **Backend-for-frontend session.** Tokens are held server-side by the Next.js route
  handlers under `apps/operator-portal/src/app/api/` and surfaced to the browser only as
  an `HttpOnly`, `Secure`, `SameSite=Lax` session cookie. No access token, refresh token
  or id token ever reaches `localStorage`, `sessionStorage`, or a client component. An
  operator session is a platform-administrator session; an XSS bug that could read a
  token would be a full control-plane compromise, so the token never sits where script
  can read it.
- **MFA is enforced, not offered.** The realm's browser flow requires OTP, and the
  `CONFIGURE_TOTP` required action is set on the operator realm role. The realm export
  checked into `infra/keycloak/` carries this configuration, and an integration test
  asserts that a login attempt that skips the OTP step does not produce a session. A
  realm where MFA is optional is a misconfiguration the portal reports at startup rather
  than tolerates.
- Refresh happens in the route handler on a short access-token lifetime; the session
  cookie's lifetime is bounded by the refresh token, and logout revokes at the realm.

### Operators module — identity and permissions

`LearnStack.Hub.Modules.Operators` maps realm roles onto Hub permission keys and
resolves the `OperatorContext` that the rest of the pipeline reads.

- Permission keys follow LearnStack's convention — `{module}.{resource}.{action}` with the
  closed action set `read | write | delete | admin`
  ([Standards 19](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/19-permissions.md)) — and are **all
  Platform scope**. Hub has no tenant or organization scope: an operator either has a
  capability across the control plane or does not have it. Examples:
  `tenants.tenant.read`, `tenants.tenant.write`, `plans.plan.read`, `audit.entry.read`.
- `OperatorId` (not `UserId`) is the actor type throughout, per
  [cross-cutting foundation § OperatorId](../architecture/cross-cutting-foundation.md).
- The `AuthorizationBehavior` shell from P02c-1 lights up: it reads the resolved
  `OperatorContext` and the handler's declared permission requirement and returns
  `Result.Fail(forbidden)` rather than throwing.
- Server-side enforcement is the authority. The portal hides what an operator cannot do,
  but hiding is a courtesy — every mutation is checked again in the handler.

### Audit module — the operator stream

`LearnStack.Hub.Modules.Audit` ships the `AuditEntry` aggregate and lights up the
`AuditLogBehavior` shell from P02c-1.

- Every entry carries **`actor.hubOperator = true`**. This flag is the discriminator that
  lets a reader looking at Hub and LearnStack audit records side by side tell a
  control-plane action from a tenant action. Without it, "who suspended this tenant" and
  "who suspended this learner" become the same shape.
- Durability follows [ADR-0033](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0033-audit-durability-model.md):
  MUST-class operator audit rows are enrolled in the **same `SaveChanges`** as the
  business write, so a crash between the write and the audit cannot happen. Hub has no
  Row Level Security, so ADR-0033's RLS-visibility argument does not apply here — but its
  crash-loss argument applies in full, and the two systems having different audit
  durability semantics would be a difference nobody could remember which way round.
  SHOULD/MAY-class stays best-effort with the accepted loss written down.
- The inline MUST/SHOULD/MAY matrices carried in each P02c-1 module doc
  ([tenant-lifecycle](../modules/tenant-lifecycle.md), [plans](../modules/plans.md),
  [subscriptions](../modules/subscriptions.md), [entitlements](../modules/entitlements.md))
  are consumed by the live writer here. Whether they move to per-module `audit.md` files
  or stay inline is decided in this packet and recorded in
  [docs/modules/README.md](../modules/README.md).

### Screens

The MVP is a deliberate subset of the full portal tree in
[Architecture 24 § 6](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md):

| Screen | Content |
|---|---|
| **Dashboard** | Tenant count by status and deployment mode, subscriptions by state, recent operator actions. No revenue KPIs — there is no revenue data until [hub-billing.md](hub-billing.md) |
| **Tenants → List** | Cursor-paginated, filterable by status, deployment mode and bound plan; slug and display-name search |
| **Tenants → Detail** | Identity (id, slug, display name, deployment mode, status), subscription summary, status-transition actions (activate / suspend / archive), and the entitlement viewer |
| **Tenants → Detail → Entitlement viewer** | **Read-only.** Renders the projection exactly as [entitlement-projection.md](../architecture/entitlement-projection.md) defines it: `tier`, the `features` map, the `limits` map with `-1` rendered as "unlimited", `compliance.caps` (empty until [P02c-5](p02c-5-custom-domain-lifecycle.md)), `expires_at`, `grace_until`, and the monotonic `generation`. It is a window onto a derived value; the projection is never edited here, because editing a projection instead of its inputs is how projections stop being derivable |
| **Plans → List** | Read-only list of the plan catalogue: name, tier, price, billing cycle, active flag, and the count of subscriptions bound to each |
| **Audit stream** | Filterable operator audit log — actor, action, target, before/after snapshot |

Cursor pagination, RFC 7807 Problem Details rendering and idempotency-key handling follow
[Standards 04](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/04-api-design.md) through the generated
`@learnstack-hub/sdk` client from P02c-2. The portal never hand-rolls `fetch` against the
Hub API.

### The plan editor is not in this packet

[Architecture 24 § 6](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md) shows a
plan editor with feature toggles, limit inputs, price and billing cycle. A plan's feature
payload and its price are edited on one form — splitting them yields two editors for one
aggregate and a second place to get the `FeatureKey` registry wrong. The editor therefore
ships whole, in **[hub-billing.md](hub-billing.md)**, alongside the pricing fields it
shares a form with.

Until then plans are authored by the P02c-1 seed data and changed through
`CreatePlanCommand` / `UpdatePlanCommand` over the API. That is enough for
[P02c-7](p02c-7-exit-gate.md)'s propagation gate, which flips a plan feature and watches
it reach LearnStack.

### Frontend app — `operator-portal`, and a stale name

The app is **`frontend/apps/operator-portal`**.

LearnStack-side documents call it `learnstack-hub-web`
([Architecture 24 § 6](https://github.com/HodeTech/LearnStack/blob/main/docs/architecture/24-learnstack-hub.md) and
LearnStack's `CLAUDE.md` among them). **That name is stale.** The Hub repository ships
`apps/operator-portal`, and
[`RepositoryLayoutTests.Frontend_Has_Only_The_OperatorPortal_App`](../../backend/tests/LearnStack.Hub.Tests.Architecture/RepositoryLayoutTests.cs)
fails the build if a second frontend app appears or that directory is renamed. P02c-4
corrects the LearnStack-side references in the coordinated cross-repo pass described in
[CLAUDE.md § Cross-repo coordination](../../CLAUDE.md) — one name, asserted on the side
that owns the directory.

Also landing here, per [repository-layout.md](../architecture/repository-layout.md):

- `@learnstack-hub/ui` design-system primitives, previously an empty package.
- ESLint flat-config migration and the Next 16 upgrade the P02c-0 scaffold deferred.
- WCAG 2.2 AA across every screen
  ([Standards 16](https://github.com/HodeTech/LearnStack/blob/main/docs/standards/16-accessibility.md)). An internal
  tool is still a tool someone uses all day.

### Observability

`OperatorContextSpanProcessor` lands here — the enricher
[cross-cutting foundation § 4](../architecture/cross-cutting-foundation.md) defers to
P02c-4 because it needs the Operators module to exist. It tags every span with
`operator.id` and `correlation.id`. It does **not** tag `tenant.id`: Hub spans describe
operator actions, and a Hub span carrying a tenant id would be the first step toward Hub
code thinking in tenant context.

### Out of scope, with owners

| Deferred | Owner |
|---|---|
| Plan editor, invoice viewer, billing tab, bulk export | [hub-billing.md](hub-billing.md) |
| Custom-domain pending queue, active list, renewal watch, compliance caps editor | [P02c-5](p02c-5-custom-domain-lifecycle.md) |
| Active licence keys, revocation-list generator, phone-home activity | [P02c-6](p02c-6-license-key.md) |
| Marketplace listing review queue | [hub-marketplace.md](hub-marketplace.md) |
| Usage metrics dashboards | [hub-billing.md](hub-billing.md) — `UsageAggregate` has no aggregation job before then |

One item from Architecture 24 § 6 is **out of the roadmap entirely**: the
**"Read-only Tenant View"** support tool. Reading a tenant's settings from the Hub means
the Hub holds or caches tenant content, which
[ADR-0034](https://github.com/HodeTech/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)'s
first invariant forbids. There is no phase that owns it, and there should not be one
until an ADR explains how a support read happens without the Hub touching tenant data.

## Deliverables

- `LearnStack.Hub.Modules.Operators` — realm-role to permission-key mapping,
  `OperatorContext` resolution, live `AuthorizationBehavior`.
- `LearnStack.Hub.Modules.Audit` — `AuditEntry` aggregate, `IAuditStore`, live
  `AuditLogBehavior` writing MUST-class entries inside the business transaction.
- Hub-side permission catalogue registered per module, with default operator role grants.
- `frontend/apps/operator-portal` — BFF session route handlers, dashboard, tenant list,
  tenant detail with the read-only entitlement viewer, plan list, audit stream.
- `@learnstack-hub/ui` primitives; ESLint flat config; Next 16.
- `infra/keycloak/` realm export carrying the MFA-required browser flow and the operator
  roles.
- `OperatorContextSpanProcessor`.
- Test coverage: unit tests for permission mapping; integration tests for the login flow
  (including the MFA-skip rejection and the wrong-realm rejection); an audit test
  asserting that a MUST-class command that fails rolls back its audit row with the
  business write.

## Completion Criteria

- An operator authenticates against the `learnstack-hub` realm with OTP and reaches the
  dashboard. A token from the `learnstack` realm is rejected at the portal and again at
  the Hub API.
- No token material is readable from the browser: `localStorage`, `sessionStorage` and
  `document.cookie` contain no JWT.
- The tenant list shows every tenant created in P02c-1, filters by status and deployment
  mode, and pages with cursors rather than offsets.
- Tenant detail renders the entitlement projection with the same field names and value
  semantics as `entitlement-v1.schema.json`, including a `generation` that visibly
  increments after a plan change.
- Every mutating action performed through the portal produces an `AuditEntry` with
  `actor.hubOperator = true`, the operator's id, and a before/after snapshot for the
  columns the module's matrix marks.
- An operator without `tenants.tenant.write` cannot suspend a tenant — the button is
  absent **and** a direct API call returns `403`.
- Every screen passes an automated WCAG 2.2 AA audit with no violations at the error
  level.
- `LearnStack.Hub.Tests.Architecture` green, including
  `Frontend_Has_Only_The_OperatorPortal_App` and `Hub_NeverStores_TenantData`.

## Risks

- **Token storage drifting to the client.** A future screen needs a token "just for this
  one call", and the BFF boundary erodes. Mitigated by keeping the SDK client
  session-cookie-based with no token parameter, so there is no signature that accepts one.
- **MFA becoming advisory.** A developer disables OTP locally for convenience and the
  realm export follows. Mitigated by asserting the required action in an integration test
  against the checked-in realm export, not against a running instance an operator
  configured by hand.
- **The entitlement viewer becoming an editor.** The most requested next feature will be
  "let me just override this one flag for this one tenant". Doing so makes the projection
  no longer a projection and breaks `generation` monotonicity as a cache-coherency
  primitive. The answer is a compliance cap ([P02c-5](p02c-5-custom-domain-lifecycle.md))
  or a custom plan — both of which are inputs, not overrides of an output.
- **A screen mutating outside MediatR.** A route handler that writes through the DbContext
  directly skips validation, authorization, transaction and audit in one move. Mitigated
  by the portal having no database access at all — it holds an HTTP client and nothing
  else.
- **Permission keys accreting free-form verbs.** `tenants.tenant.suspend` reads naturally
  and breaks the closed action set. Suspension is `tenants.tenant.write`; if it needs
  separate authority it becomes a sub-resource, not a new verb.

## Phase Exit Decision

P02c-4 is complete when an operator can run the control plane from a browser without a
terminal: log in with MFA against the `learnstack-hub` realm, find a tenant by slug,
read its entitlement projection, suspend and reactivate it, and see both actions in the
audit stream attributed to themselves with `actor.hubOperator = true` — while a
`learnstack` realm token is refused at every door.

The packet does **not** exit on screen count. A portal that renders every box in
Architecture 24 § 6 but writes audit rows outside the business transaction, or holds a
token where script can read it, has not passed.

Next: [P02c-5 Custom Domain Lifecycle](p02c-5-custom-domain-lifecycle.md).
