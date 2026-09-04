# Catalog publication and authenticated reader

This is a bounded technical slice, not publication or professional approval of the national baseline. The current wire contract is [Catalog OpenAPI 2.0.0](../../contracts/catalog.openapi.yaml). The parent CAT-001/002/003 tasks retain their unmet acceptance criteria.

## Editorial workflow

Only server-appointed `Catalog:EditorialActorUserIds` may ingest, inspect staging diff, publish, or roll back. The default list is empty. Tenant ownership does not grant national editorial authority. Mutations require the authenticated session and antiforgery token; the server records the actor rather than accepting one in the body.

1. Ingest a complete JSON array as Base64-encoded, strictly valid UTF-8. The parser rejects the entire source on invalid rows, unknown properties, duplicate JSON properties, invalid categories, or duplicate normalized codes within a source. It does not silently skip rows or accept client-certified support levels.
2. Read the editorial diff. The candidate combines the latest complete snapshot of each canonical source, selected by database ingestion sequence rather than a client clock. Ambiguous normalized codes or aliases require editorial resolution. An empty source array can remove that source's entries; an entirely empty candidate cannot be published.
3. Submit `{versionTag, candidateHash}` using the fingerprint returned by that diff. The fingerprint binds the active version, selected source snapshots, and canonical candidate. A changed candidate or active version returns `409 catalog.candidate_stale`: inspect a fresh diff before approval.
4. Publication switches the active version and records immutable items, source references, an editorial audit, and the local outbox event in one transaction. Rollback switches the active pointer; it does not rewrite historical items or cycles.

Editorial operations share a PostgreSQL transaction-scoped lock acquired before reading publication state. A partial unique index permits at most one active version. Reads use a repeatable-read transaction so the version, active pointer, count, page, and provenance describe one database snapshot. A response is an observation at read time, not a promise that publication cannot change afterward.

Limits: 1 MiB decoded source, 10,000 rows per source, 64 selected sources and 50,000 candidate items; source ID 128 characters, code 64, display name 256, jurisdiction 64, and at most 20 aliases of 256 characters each. Codes and aliases are normalized for comparison, while original source bytes and published display values remain available for provenance. All newly ingested entries use generic support: there are no approved specialized rules, KPIs, or AI recommendations in this workflow.

Errors use bounded problem codes, not source payloads or SQL diagnostics. `503 catalog.unavailable` can mean an unknown write outcome. Do not automatically repeat an editorial mutation; reconcile the active version, history and diff first. Outbox persistence does not claim external delivery or an exactly-once distributed effect.

## Historical data and migration

The additive migration refuses ambiguous historical data, including multiple active versions, normalized duplicate codes and orphaned references. It does not choose a winner, delete records, or manufacture provenance. Operators must resolve such data through a separately reviewed migration/recovery procedure.

The database also rejects late inserts into already committed snapshots/releases and activation changes without matching fresh audit/outbox evidence. Catalog rollback is a logical operation, not an EF schema downgrade: the new migration's `Down` is deliberately limited to disposable test database names. Shared-environment schema recovery requires a reviewed forward-compatible procedure.

Legacy source snapshots without complete-ingestion evidence remain unverified and are excluded from candidates. Re-ingesting the same canonical source/hash returns `409 catalog.legacy_snapshot_unverified`; changing capitalization or surrounding whitespace cannot evade that check. A reviewed replay/backfill remains a separate task. Changing source content merely to bypass this check is not a recovery procedure.

Legacy published items can still be read, but missing verifiable source references are exposed as `legacy_unavailable` with nullable provenance. They are not relabeled as verified snapshots. Historical support labels never imply that specialized capabilities have been implemented or approved.

## Reader behavior

The authenticated workspace exposes a read-only Catalog tab. Search supports normalized code, name and aliases, with bounded filters and cancellable requests. The results region scrolls without moving the surrounding controls. Detail remains pinned to the version of its selected row; a later publication does not silently substitute another item.

`GET /api/catalog/versions` provides bounded version discovery, with a default page size of 20, maximum 100 and offset up to 10,000. Explicit historical queries preserve their version and identify the active version observed during that read. Unknown historical versions return 404. No active publication is a successful empty state, distinct from unavailable service or invalid response data.

The UI shows source, version, provenance, declared support and absent capabilities separately. IDs displayed to people use the existing six-character uppercase UUID convention. Full identifiers remain internal request/state values. The reader has no editorial buttons or write requests.

## Isolated browser fixtures

`scripts/identity/run-e2e.ps1` creates its own password-protected PostgreSQL/PostGIS cluster and runs the built API host. It signs in an explicit development fixture, restarts only its own API with that synthetic actor on the local editorial allowlist, then ingests and publishes two versions through the real authorized HTTP workflow. It revokes the editor sessions before Playwright runs and restores the caller's environment afterward.

The source is `e2e-synthetic-catalog`; versions `e2e-synthetic-v1` and `e2e-synthetic-v2` contain clearly synthetic crop/animal labels and aliases. They are neither a national catalog nor agronomic validation. No production editor, shared database, live provider or direct table seed is involved. The wrapper removes only its validated temporary run directory and stops only processes belonging to that run.

The PowerShell setup client explicitly supplies its own ephemeral session cookies to the hardcoded IPv4 loopback API: .NET's automatic cookie container otherwise omits `Secure` cookies on local HTTP. It retains the original cookie flags, uses real session/antiforgery checks, disables proxies and redirects, and neither logs nor persists cookie values. This is a local test-client adaptation, not a change to application cookies or a substitute for shared-environment HTTPS verification.

Verification results are recorded in [the working plan](../../tasks/todo%20.md), not inferred from the presence of fixtures or test files.
