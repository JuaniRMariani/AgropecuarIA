# Authoritative catalog references in production cycles

Implemented contract for iteration 55. Local gates passed: 635 backend tests with real PostgreSQL/PostGIS, 346 web tests, and 20 desktop/mobile browser scenarios. Exact verification and delivery status are recorded in [the working plan](../../tasks/todo%20.md). This slice does not close the full CAT-003 workflow, financial/document integrations, or professional baseline certification.

## Consumer migration: Productive API 2.0

Starting a cycle no longer accepts `catalogDisplayName` or `supportLevel` from the caller. Those properties, and other unknown properties, are rejected rather than silently treated as authoritative metadata. Existing consumers of this operation must update their request body:

```json
{
  "catalogCode": "E2E-CULTIVO",
  "purpose": "Registro demostrativo",
  "system": "Sistema declarado por el usuario",
  "startDateUtc": "2026-09-04T00:00:00Z"
}
```

The example code belongs only to the synthetic E2E fixture. Real callers select a code from their authenticated catalog query. An optional `expectedCatalogVersionId` carries the version the caller reviewed; it is a precondition against the active version observed by the server, not permission to create a new cycle against any historical release.

The HTTP operation remains `POST /api/organizations/{organizationId}/fields/{fieldId}/cycles`, with session authentication and antiforgery. The major OpenAPI version makes this request incompatibility explicit; unchanged field and cycle-read operations do not require invented data migrations.

This requires a coordinated breaking rollout for cycle writers. Additive columns do not make old writers compatible: after migration, new unresolved rows are rejected, and this slice does not serve a parallel v1 creation route. Mixed-version write compatibility is not demonstrated; the broader FND-003 evolution gate remains open. No shared-environment deployment is performed here.

## Authority and observation time

Productive Core owns `IProductionCatalogResolver`. A scoped adapter in the API composition calls Catalog's public Application service and translates its result. Productive Core does not reference Catalog's assembly, access its private tables, or create cross-module foreign keys.

The service first authorizes the organization and field and rejects an archived field. Only then does it resolve Catalog. One Catalog repeatable-read transaction observes the active version, compares the optional expected version, and resolves that version's item and source provenance. If the expected version differs, including when no active version exists, the result is stale before an item lookup.

The persisted snapshot contains version/item IDs, version tag, canonical code/display name, declared catalog support, nullable source provenance, and a server resolution timestamp. This timestamp describes the observation; it is not official effective time, provider publication time, or an identifier for the exact MVCC snapshot.

Catalog publication may change after this read and before Productive Core commits. The new cycle deliberately retains the version actually resolved. These are two local transactions, not a distributed transaction, and the operation does not promise that the referenced version remains active at commit time.

## History and support

| Cycle/reference case | `catalogReferenceStatus` | Snapshot/provenance | Effective capabilities |
|---|---|---|---|
| Cycle created before this migration | `legacy_unresolved` | Null snapshot; original stored values preserved | Generic only |
| New cycle resolved against a legacy publication | `resolved_publication` | Real version/item IDs; `legacy_unavailable`, null source fields | Generic only |
| New cycle resolved against a verified publication | `resolved_publication` | Real version/item IDs; `verified_snapshot`, complete source fields | Generic only |

Existing cycles are not backfilled by matching their code to today's catalog. Their original code, name and support label remain historical data, not newly certified assertions. New cycles store generic effective support even when a historical catalog label claims a higher level.

Responses add `catalogSnapshot`, `catalogReferenceStatus`, `effectiveSupportLevel`, `capabilities` and `absentCapabilities`. Effective support is `FLUJO_GENERICO`; capabilities are empty and specialized rules, specialized KPIs and AI recommendations are explicitly absent. The declared catalog label is separately preserved inside the resolved snapshot.

The additive Productive migration permits the existing unresolved rows, requires a resolved snapshot for subsequent inserts, and prevents rewriting reference metadata. Legitimate cycle closure preserves that snapshot. List, timeline and closure read the stored data without calling Catalog, so a catalog outage or logical rollback cannot erase the cycle's history. SQL verifies local coherence and immutability, not independent authority over another module's data.

Closure currently exists as an application service, not an HTTP route. This slice verifies that service and PostgreSQL behavior; it does not claim a new browser or HTTP closure workflow.

## Failures and limits

- No active publication without an expected version: `409 productive_core.catalog_not_published`.
- Expected version differs from the active version observed: `409 productive_core.catalog_version_stale`.
- No matching active catalog item: `404 productive_core.catalog_item_not_found`.
- Catalog unavailable: `503 productive_core.catalog_unavailable`, without a client-metadata fallback.

These resolution failures must not insert a cycle. Authorization and archive rejection must not call the resolver. Mutation outcomes can still be ambiguous after a database commit failure: this slice does not add an idempotency ledger and must not automatically retry cycle creation.

Required evidence includes real HTTP/CSRF/tenant checks, PostgreSQL migration and immutability tests, a barrier-controlled publication after resolution but before cycle commit, and historical reads/closure while Catalog is unavailable. Test doubles alone do not establish the public adapter and database integration.
