# Bounded production history reads

Productive API 2.1 adds two read-only routes:

- `GET /api/organizations/{organizationId}/fields/{fieldId}/cycles/page`
- `GET /api/organizations/{organizationId}/cycles/{cycleId}/timeline/page`

Both accept `limit` (default 20, range 1–100) and an optional opaque `cursor`. Omit the cursor on the first request. Responses contain `items` for cycles, or `cycle` and `events` for a timeline, plus `hasMore` and nullable `nextCursor`. Use `nextCursor` unchanged only for the same resource and history kind. Empty results are successful reads; they are not evidence that access was denied.

Each request revalidates the durable session, owner membership and resource inside its tenant/RLS transaction. A cursor is only a contextual position, not a credential or permission grant. Invalid bounds, malformed/oversized/unsupported cursors and cross-context cursors produce a neutral 400 after resource authorization. Foreign and missing resources remain indistinguishable. Responses are `private, no-store`.

## Ordering and consistency

Cycles are ordered by creation timestamp descending, then UUID descending. Observations are ordered by recording timestamp descending, then UUID descending. The observation's effective date is a separate business value and is not this pagination order. SQL reads at most `limit + 1` rows; the additional row determines continuation and is not returned.

The queries use keyset positions and matching compound indexes. The implementation follows the unique-ordering/index guidance in [EF Core pagination](https://learn.microsoft.com/en-us/ef/core/querying/pagination) and uses Npgsql's documented [row-value comparisons](https://www.npgsql.org/efcore/mapping/translations.html#row-value-comparisons), keeping SQL translation inside Infrastructure. No new pagination package or cross-module query was introduced.

Pages do not share a transaction or an immutable multi-request snapshot. Concurrent new records may appear only after refreshing from the first page, or in a later page according to their stored ordering keys. The cycle's lifecycle status can change between requests. No total count, arbitrary page number or cross-request consistency guarantee is provided.

History uses the cycle's stored catalog snapshot. It never resolves against the active catalog, never backfills legacy metadata and never confers specialized capabilities. Archived fields retain readable history. These APIs do not add archive discovery to the frontend, cycle closure over HTTP, writes, mutation retries or an idempotency ledger.

## Compatibility and operational limits

The original unpaged GET routes remain behaviorally unchanged and are explicitly deprecated in OpenAPI. They are still unbounded and must be retired through a reviewed consumer migration; adding bounded routes is not a claim that all runtime reads are now bounded. Existing Productive 2.0 consumers keep their response envelopes. The earlier 2.0 breaking writer rollout for catalog references remains documented separately.

Migration `20260904232102_AddProductionHistoryPagingIndexes` adds two indexes and does not modify historical rows, catalog references or grants. It uses normal transactional index creation: a shared deployment needs an operator-reviewed window appropriate to table size. No shared migration or deployment was executed. Its Down operation drops only these indexes, not business data.

## Verification

PostgreSQL tests seed 105 cycles and 105 observations with tied recording timestamps, traverse the entire history, assert exact UUID order and bounded repository results, and verify catalog-independent reads. HTTP tests exercise actual Program composition, pagination, empty histories, cursor mutations, session/owner denial, archive continuity and session revocation. The larger input matrix sets a rate limit of 100 only for its isolated fixture; production limits and dedicated limiter tests are unchanged.

Legacy history, cancellation, contracts, route authorization and integrated gates are tracked in [the working plan](../../tasks/todo%20.md). No frontend reader or completed CAT-003 parent is claimed by this API slice.
