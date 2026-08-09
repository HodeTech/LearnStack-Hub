# P02c-6: License Key

> **Status: ⏳ Not started.** Depends on [P02c-1](p02c-1-hub-domain-core.md) and [P02c-2](p02c-2-internal-api-and-contract.md). **Cross-repo** — the LearnStack-side `SignedLicenseKeyEntitlementProvider` skeleton ships in a coordinated pull request; its operational hardening is [LearnStack Phase 11](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-11-production-hardening.md).

## Goal

Make the Hub able to issue, and later withdraw, a portable statement of what a customer
bought.

Every other entitlement path in this repository assumes a network. `HubEntitlementProvider`
calls the Hub; the projection push reaches LearnStack over HTTPS; a plan change propagates
in seconds. A Self-Hosted customer in a regulated or air-gapped environment has none of
that, and
[ADR-0020](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md)
answers it with an RSA-signed `.lic` file that embeds the same entitlement projection —
no schema fork, no second entitlement model, just a different transport with a longer
refresh interval and bounded trust.

P02c-6 builds the issuing half of that: the `LicenseKey` aggregate, the signing keypair
and its rotation seam, the file format, and the revocation mechanism. The consuming half
on the LearnStack side ships as a **functional skeleton** here and is hardened in
[LearnStack Phase 11](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-11-production-hardening.md),
whose trigger — per
[ADR-0035](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md) — is
*"a Self-Hosted contract is signed"*.

## Scope

### `LicenseKey` aggregate

`LearnStack.Hub.Modules.LicenseKeys` owns `LicenseKey : AuditableEntity<LicenseKeyId>`.

| Column | Notes |
|---|---|
| `id` | uuid PK. **This is the `license_id` claim.** Revocation is keyed on it |
| `tenant_id` | the tenant the licence was issued for |
| `kid` | the signing key that produced this licence; survives key rotation |
| `deployment_mode` | `SelfHostedOnline \| SelfHostedAirGapped` |
| `entitlement_generation` | the projection generation frozen into the payload at issuance |
| `issued_at` / `expires_at` / `grace_until` | the licence's own clock |
| `status` | `Issued \| Superseded \| Revoked` |
| `revoked_at` / `revocation_reason` | set on revocation; both audited |

Re-issuing for a tenant marks the previous key `Superseded` rather than deleting it — a
key that was valid yesterday needs a row today so that a customer presenting it gets an
accurate answer.

Issuing and revoking are both MUST-class audited operator actions
([P02c-4](p02c-4-operator-portal.md)).

### Signing keys

- **RSA-2048 minimum**, RS256. The verifier rejects anything weaker;
  `LicenseKey_Validation_Is_Pinned_RSA2048`
  ([ADR-0020 § Architecture tests](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md))
  asserts it.
- Private key lives in the Hub's secret store at
  `secret/learnstack-hub/license-signing-key`, read through `ISecretProvider`. It is never
  in the repository, never in a container image, never in an environment variable dump.
- Public keys are published as a JWKS-style key set addressed by `kid` and embedded into
  LearnStack releases at build time, so an air-gapped verifier needs no network to
  validate a signature.
- Rotation is a `kid` addition, not a cutover: both keys stay valid for a deprecation
  window, and a key retired mid-window still validates licences issued before its
  `RetiredAt`. The rotation *procedure* — key generation ceremony, custody, the release
  that ships the new public key — is
  [LearnStack Phase 11](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-11-production-hardening.md).
  What ships here is the data model that makes rotation possible without a format change.

### The `.lic` file

Wire format is `<base64url(header)>.<base64url(payload)>.<base64url(signature)>` — JWT
shape, distinguished by the header `typ: "LSL"` ("LearnStack License"), per
[Hybrid License Model § 1](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/26-hybrid-license-model.md).

Header: `{ "alg": "RS256", "typ": "LSL", "kid": "lsl-signing-key-v1" }`.

Payload claims:

| Claim | Purpose |
|---|---|
| `license_id` | **the licence's identity — see below** |
| `iss` | always `learnstack-hub` |
| `sub` | tenant id |
| `iat` / `exp` | issued-at and expiry, numeric |
| `deployment_mode` | `SelfHostedOnline` or `SelfHostedAirGapped` |
| `entitlement` | the full projection: `tier`, `features`, `limits`, `compliance.caps`, `generation` — byte-identical in shape to what `PUT /api/internal/tenants/{id}/entitlements` carries |
| `grace_until` | end of degraded-but-functional operation after `exp` |
| `phone_home_url` | refresh endpoint, absent for `SelfHostedAirGapped` |
| `revocation_list_url` | where the signed revocation bundle is published |
| `custom_domains` | optional; the hosts an air-gapped deployment should expect, since there is no host-mapping push in that mode ([P02c-5](p02c-5-custom-domain-lifecycle.md)) |

### The claim set must carry `license_id`

The documented payload does not have one, and everything downstream assumes it does.

[Hybrid License Model § 7](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/26-hybrid-license-model.md)
publishes a revocation bundle whose body is a list of `revoked_license_ids`, and its
verification sequence checks that the presented licence's `license_id` is not in that
set. The payload in § 1 of the same document carries `iss`, `sub`, `iat`, `exp`,
`deployment_mode`, `entitlement`, timestamps and two URLs — and no identifier for the
licence itself. A verifier holding that file cannot name the thing it is holding, so it
cannot look it up in a revocation list. Revocation, the entire break-glass mechanism
behind offline licensing, does not work against the payload as written.

P02c-6 fixes it:

- **`license_id` is a required claim**, carrying `LicenseKey.id`. Not optional, not
  inferred from `sub` — a tenant can hold several licences over time and revoking one
  must not revoke the tenant.
- `license-payload-v1.schema.json` is checked into **both** repositories and asserted by a
  snapshot test in each, exactly as `entitlement-v1.schema.json` is. The schema is the
  contract; prose in an architecture document is not.
- The same pass reconciles the payload's duplicate timestamp spellings: § 1 carries both
  numeric `iat` / `exp` and ISO-8601 `issued_at` / `expires_at` for the same two instants.
  One spelling is normative and the other is removed, in both repositories, in the same
  change. Two spellings of one fact is how the two sides end up disagreeing about when a
  licence expires.
- The payload shape sits inside
  [ADR-0020's](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md)
  Decision section, so the correction lands as a **dated Amendment** to that ADR, never as
  an edit to the Decision text.

### Revocation list

An issued licence is trusted until `exp` unless something withdraws it. That something is
a signed revocation bundle:

```json
{
  "generated_at": "2026-08-08T00:00:00Z",
  "revoked_license_ids": ["<uuid>", "<uuid>"],
  "signature": "base64url(RS256 over the canonicalised body)"
}
```

**It is published as a signed static artefact at a fixed URL, not as an internal-API
endpoint.**
[ADR-0020](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md)
describes it that way; [Hybrid License Model § 7](https://github.com/cemililik/LearnStack/blob/main/docs/architecture/26-hybrid-license-model.md)
shows it under `/api/v1/internal/license/revocations`, which is a path
[ADR-0034](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)'s
enumerated LearnStack → Hub set does not contain. The static-artefact form is chosen
because it keeps the contract surface unchanged and because it is the form an air-gapped
customer can actually consume — a file they can carry in on media, verify offline, and
place next to their licence. If the endpoint form is ever preferred instead, it needs an
ADR, and the two documents are reconciled onto the static form here either way.

The bundle is unauthenticated because its signature is its authentication and its contents
are opaque identifiers — no tenant name, no slug, no plan. An unauthenticated reader
learns how many licences have been revoked, not whose.

### Issuance and phone-home flows

- **Issue.** An operator picks a tenant; the Hub composes that tenant's current
  `Entitlement` projection, wraps it with the claim set above, signs it, records the
  `LicenseKey` row, and returns a `.lic` download. Issuance is a recompute trigger — the
  row marked `⏳ P02c-6` in
  [entitlement-projection.md § When recompute fires](../architecture/entitlement-projection.md)
  becomes live, and re-issuance sets `expires_at` and `grace_until` on the `Entitlement`
  row so the online and offline paths agree about the same tenant's expiry.
- **Revoke.** An operator marks the `LicenseKey` row `Revoked` with a reason; the next
  bundle generation includes its id.
- **Phone-home.** `POST /api/v1/internal/license/refresh` — already in ADR-0034's endpoint
  set and already handled since P02c-2 — gets its live caller. Each successful refresh
  calls `LearnStackTenant.RecordPhoneHome(at)`, the method P02c-1 shipped without a
  caller, so `last_phone_home_at` stops being permanently null and the operator portal can
  show which Self-Hosted deployments have gone quiet.

### Operator portal screens

Added to the [P02c-4](p02c-4-operator-portal.md) shell: Licenses → Active Keys,
Revocation List Generator, Phone-Home Activity.

### LearnStack side — a functional skeleton, and what it is not

`SignedLicenseKeyEntitlementProvider` ships in LearnStack as the third
`IEntitlementProvider` implementation
([ADR-0020](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0020-triple-deployment-hybrid-license.md)),
doing the work that cannot be faked:

- parse the `.lic` file, read `kid`, resolve it against the embedded public key set;
- verify the RS256 signature, reject weaker algorithms, reject an unknown or
  wrongly-retired `kid`;
- validate the payload against `license-payload-v1.schema.json`;
- read the embedded projection and serve feature and limit lookups from it;
- honour `exp` and `grace_until` — functional before `exp`, degraded within grace,
  read-only past it. This is the **licence-expiry** ladder and it is deliberately not
  the same as the Hub-outage ladder that
  [ADR-0034](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)
  defines. An outage is a control-plane failure the tenant did not cause, so ADR-0034
  answers it with a durable grace window plus a per-key fail-open / fail-closed class;
  an expired licence is the licence working as intended, so it degrades to read-only and
  stays there. Do not sweep this paragraph to match the ADR-0034 wording.

**Production hardening is [LearnStack Phase 11](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-11-production-hardening.md)**,
per [ADR-0035](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md):

- the signing-key rotation procedure and custody model;
- signed revocation-list distribution, the daily refresh job, and its offline fallback;
- the `SIGHUP` hot-reload runbook for dropping a new `.lic` into
  `/var/learnstack/license/` without a restart;
- grace-period behaviour under load, and the read-only degradation path exercised against
  a realistic workload;
- the air-gapped install runbook.

Each of those is an operational procedure that needs a real deployment to be worth
writing. The skeleton proves the format and the verification; the hardening proves the
operations.

## Deliverables

- `LearnStack.Hub.Modules.LicenseKeys` — aggregate, issue / re-issue / revoke commands,
  queries for the portal screens.
- RSA-2048 signing keypair provisioned through `ISecretProvider`; `kid`-addressed public
  key set published for embedding into LearnStack releases.
- `.lic` signer producing the `typ: "LSL"` three-segment format.
- `license-payload-v1.schema.json` in both repositories, with `license_id` required and
  the duplicate timestamp spellings reconciled; snapshot test on each side.
- Dated Amendment to ADR-0020 recording the claim-set correction.
- Signed revocation bundle generator publishing to a fixed URL.
- **Phone-home client certificate in the licence bundle.**
  [ADR-0034 § One auth chain, both directions](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0034-hub-contract-surface-invariant.md)
  extends the mTLS + JWT + HMAC chain to the LearnStack → Hub direction, replacing the
  per-instance API key. A `SelfHostedOnline` instance therefore cannot phone home
  without a client certificate the Hub's CA will validate, and this packet is the only
  one that hands a customer-run instance anything. The certificate is issued alongside
  the `.lic` file, carries the same `license_id`, and is re-issued by the same
  re-issue command on the same cadence — one artefact, one expiry, one revocation. A
  revoked licence revokes the certificate with it, so a revoked instance loses the
  transport before it loses the entitlement. `SelfHostedAirGapped` makes no outbound
  call and is issued no certificate.
- Live `POST /api/v1/internal/license/refresh` path calling `RecordPhoneHome`.
- Recompute trigger on licence issuance, setting `expires_at` and `grace_until` on the
  `Entitlement` row.
- Operator portal licence screens.
- LearnStack-side `SignedLicenseKeyEntitlementProvider` functional skeleton, in a
  coordinated pull request.

## Completion Criteria

- An operator issues a `.lic` for a tenant and downloads it; the `LicenseKey` row records
  `license_id`, `kid`, `entitlement_generation`, and the issuing operator.
- A LearnStack instance in `SelfHostedAirGapped` mode, with no network path to the Hub,
  loads that file, verifies it against the embedded public key set, and answers feature
  and limit lookups from the embedded projection.
- A file signed with a weaker algorithm, an unknown `kid`, or a tampered payload is
  rejected — and the rejection names which check failed in the log, without echoing the
  payload.
- `license_id` is present and required in `license-payload-v1.schema.json` in both
  repositories, and the snapshot tests agree.
- Revoking the licence and refreshing the revocation bundle causes the same instance to
  deny it **before** `exp`.
- Past `exp` but within `grace_until`, the instance is functional and shows the grace
  banner; past `grace_until`, it is read-only rather than broken.
- A successful phone-home from a `SelfHostedOnline` deployment updates
  `last_phone_home_at` and is visible on the Phone-Home Activity screen. It presents the
  client certificate issued with its licence bundle; the same call with no certificate,
  with an expired one, or with one belonging to a revoked licence is refused at the TLS
  handshake — before any handler runs and before the JWT or the HMAC signature is
  examined.
- No log line, span attribute or error envelope contains the private key or a full `.lic`
  payload.

## Risks

- **Signing-key compromise trusts every licence ever issued.** There is no per-licence
  secret; the keypair is the whole trust root. Mitigated by keeping the private key in the
  secret store only, by shipping the `kid` rotation seam from day one so rotation is a
  configuration change rather than a format change, and by treating the revocation bundle
  as the break-glass. The custody procedure itself is Phase 11.
- **Revocation is only as good as the refresh.** An air-gapped customer may never fetch
  the bundle, so an air-gapped licence is effectively trusted until `exp`. This is the
  price of air-gap and it is written down here rather than implied: revocation is
  effective at the next online window, and `exp` is the only bound that always holds.
  Licence terms for air-gapped customers should therefore be short enough that `exp` is a
  real control.
- **Clock manipulation.** Moving the system clock back extends a licence indefinitely.
  Mitigated by recording a monotonic last-seen timestamp on the LearnStack side and
  refusing to accept a wall clock that has moved backwards past it.
- **Two entitlement models drifting apart.** The licence path and the Hub path serve the
  same feature checks; if their payloads diverge, a Self-Hosted customer and a SaaS
  customer on the same plan get different behaviour. Mitigated by the licence embedding
  the projection verbatim and by both paths validating against the same
  `entitlement-v1.schema.json`.
- **The `license_id` omission recurring.** It survived from ADR-0020 through the
  architecture deep dive because prose is not executable. Mitigated by making the schema —
  not the document — the contract, asserted on both sides.
- **A skeleton read as finished.** `SignedLicenseKeyEntitlementProvider` verifying a file
  correctly looks a great deal like a supported deployment mode. It is not: without the
  rotation procedure, the revocation refresh and the hot-reload runbook, `SelfHostedOnline`
  and `SelfHostedAirGapped` remain prepared seams rather than supported deployments, which
  is exactly what
  [ADR-0035](https://github.com/cemililik/LearnStack/blob/main/docs/decisions/0035-demand-gated-infrastructure.md)
  says. Sales-facing material must say the same.

## Phase Exit Decision

P02c-6 is complete when a licence can be issued, verified offline, and withdrawn: an
operator issues a `.lic`, an air-gapped LearnStack instance runs on it with no network,
and revoking it plus refreshing the bundle denies that instance before its expiry — with
`license_id` a required claim in a schema both repositories assert.

The packet does **not** claim Self-Hosted as a supported deployment mode. That claim
belongs to
[LearnStack Phase 11](https://github.com/cemililik/LearnStack/blob/main/docs/roadmap/phase-11-production-hardening.md),
when the rotation, distribution and hot-reload procedures exist and a signed contract has
made them worth writing.

Next: [P02c-7 End-to-End Exit Gate](p02c-7-exit-gate.md).
