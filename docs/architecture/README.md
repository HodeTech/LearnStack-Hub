# Hub Architecture

Hub-internal architecture documentation. Cross-cutting architectural decisions (LearnStack ↔ Hub contracts, deployment models, entitlement projection, custom-domain lifecycle) live in the [LearnStack core architecture corpus](../../../LearnStack/docs/architecture/) — not duplicated here.

## Contents

| Doc                                                        | Status                                                                                                                                                                                    |
| ---------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [contract-with-learnstack.md](contract-with-learnstack.md) | ✅ P02c-0 — Pointers to LearnStack-side authoritative contracts (ADR-0019, Standards 20, Architecture 24)                                                                                 |
| [repository-layout.md](repository-layout.md)               | ✅ P02c-0 — Hub internal folder structure + module topology                                                                                                                               |
| [module-topology.md](module-topology.md)                   | ✅ P02c-1 (spec) — Hub module dependency graph; cross-module entitlement-rebuild flow; the "Hub does NOT use RLS" model; `hub` schema + `learnstack_hub` database                         |
| [cross-cutting-foundation.md](cross-cutting-foundation.md) | ✅ P02c-1 (spec) — Hub SharedKernel mirror surface; the 6-step Hub MediatR pipeline (vs LearnStack's 8); `OperatorId` (not `UserId`); exception handling; observability; composition root |
| [entitlement-projection.md](entitlement-projection.md)     | ✅ P02c-1 (spec) — projection wire-shape; `generation` monotonic counter; recompute algorithm + triggers; key-shape rules                                                                 |
| `learnstack-api-client.md`                                 | ⏳ P02c-2 — Outbound `LearnStackApiClient` design: mTLS client-cert provisioning, JWT issuance from the `learnstack-hub` realm service account, HMAC body signer, retry / backoff posture |
| `operator-portal.md`                                       | ⏳ P02c-4 — Operator portal information architecture, BFF flow, permission model                                                                                                          |

> **"(spec)" status:** the P02c-1 architecture docs above were authored as the design
> specification _before_ implementation. The agent executing P02c-1 implements against
> them; once the code lands they become the living description of what shipped.

The numbering convention is intentionally flat (no `NN-topic.md` numbering). Hub's architecture surface is much smaller than LearnStack core's, so alphabetical ordering reads cleanly enough.
