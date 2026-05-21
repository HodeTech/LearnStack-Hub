## Summary

<!--
What does this PR change? Link the relevant Phase 02c packet
(P02c-0..P02c-7) or LearnStack ADR (../learnstack/docs/decisions/NNNN-*).
For cross-repo work, link the paired LearnStack core PR.
-->

## Phase 02c packet

<!--
Which packet does this PR advance? Pick one:
  - P02c-0: Repository bootstrap
  - P02c-1: Hub domain core
  - P02c-2: Hub-side internal API + outbound LearnStackApiClient
  - P02c-3: LearnStack core PR (paired)
  - P02c-4: Operator portal MVP
  - P02c-5: Custom domain lifecycle
  - P02c-6: License key skeleton
  - P02c-7: End-to-end exit gate
  - Other: ___________
-->

## ADRs / standards touched

<!--
Any LearnStack ADR or Hub-internal ADR (HUB-NNNN) cited or amended? Any
Hub HTTPS Contract Surface change (new endpoint)? Adding a fifth endpoint
to the Hub HTTPS Contract Surface requires a new ADR in
../learnstack/docs/decisions/ FIRST.
-->

## Cross-repo coordination

<!--
If this PR changes the contract between Hub and LearnStack core (e.g.
P02c-3 wires HubEntitlementProvider on the LearnStack side + Hub-side
outbound client here), link the paired PR in the other repo. Both PRs
must merge in the same session to keep the contract intact.

Paired LearnStack PR: <link or "n/a">
-->

## Test plan

- [ ] `make build` succeeds
- [ ] `make test-backend` succeeds (unit + architecture + contract)
- [ ] `make lint-backend` succeeds (dotnet format verify)
- [ ] `make lint-frontend` succeeds (ESLint)
- [ ] `make typecheck` succeeds
- [ ] `make build-frontend` succeeds
- [ ] Architecture tests green (including any new rules this PR introduces)
- [ ] If Hub HTTPS Contract Surface changed: paired LearnStack PR exists and is reviewed

## Risks / rollout notes

<!--
Anything to flag for the reviewer — e.g. backwards-incompatible schema
change, secret rotation needed, cross-repo coordination window.
-->
