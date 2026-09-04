# Runtime safety repair — 2026-09-04

This delivery repairs integration defects found at `dd808e4`. It does not certify the 81-task roadmap as complete. See [the evidence inventory](current-backlog-evidence-2026-09-04.md).

## Configuration and migration

The API requires explicit `ConnectionStrings:Identity`, `Territory`, `ProductiveCore`, `Catalog`, and `Weather`. Local tests may point these at one ephemeral PostgreSQL database; there is no Weather localhost fallback. Shared environments must use provisioned credentials, not test superusers.

Identity, Productive Core and Weather schemas must reside in the same PostgreSQL database: their RLS policies invoke the local Identity authorization port. Their connection strings may use different restricted principals, but cannot point to isolated databases with this implementation.

Apply Identity migrations before Productive Core and Weather. Identity grants the Weather runtime role access only to the existing owner-authorization function, not Identity tables. Catalog migrations preserve staging while adding published tables. Weather migrations create its schema and FORCE RLS on rain/rules; requests set transaction-local scope and switch to the restricted Weather role. Ordinary Weather requests cannot insert global alerts, update historical rain, or delete records.

Productive Core's initial-geometry migration now requires PostGIS to have been provisioned in that database by an administrator. The application does not install the extension and fails the migration if it is absent. Local integration tests opt in with `AGRO_TEST_POSTGIS=true`; the E2E wrapper provisions the extension only in its newly created disposable cluster. Its existing local PostGIS bundle is preferred over the system PostgreSQL installation. Nothing here authorizes modifying a shared database.

`*:ApplyMigrations=true` is accepted only in Development/Test. Production/shared environments require an explicit migration step with a separate provisioned principal; this repair does not deploy or grant login credentials. The E2E runner configures all five modules in its isolated local database and restores environment variables on exit.

Global editorial permissions are server configuration, not organization roles:

- `Catalog:EditorialActorUserIds`: UUIDs of explicitly appointed catalog editors.
- `Weather:AlertIngestionActorUserIds`: UUIDs of explicitly appointed alert-ingestion operators.

Both lists default to empty (deny). Invalid UUIDs fail startup. Operators must change trusted server configuration and restart to alter this appointment. Do not expose these settings in tenant-editable forms. Publication records the authenticated session's user, never a body-supplied author. Mutations require antiforgery. Catalog and Weather responses are private/no-store; weather HTTP-client logging of coordinate-bearing URLs is removed.

## Verified scope and important limits

- Weather owner/resource authorization and database scope precede data access. This intentionally retains the existing owner-only authorization model; additional roles/field scopes are not implicitly activated.
- No configured enabled activity rule means `insufficient_data`, `isSuitable=false`, and an explicit reason. No agronomic defaults are invented. User-entered thresholds are not professional certification; suitability is a deterministic comparison, not an approved recommendation.
- Weather forecasts/alert queries still accept coordinates; authoritative persisted-field geometry and official CAP polygon/lifecycle integration remain pending.
- Configured and archived field summaries are readable in the UI. Archival has a confirmation step, strong ETag, scoped idempotency recovery and atomic journal/outbox; active lists exclude archived fields. Historical data is retained. A fresh archived short-ID deep link is still unavailable; no restore/unarchive operation is provided.
- Initial geometry accepts only a 2D Polygon/MultiPolygon GeoJSON string and declared area. PostGIS validates topology, stores an immutable native MultiPolygon/SRID4326 snapshot and calculates spheroidal area and planar centroid on the server. Invalid geometry is rejected, never silently repaired. Transport caps are 1MiB UTF-8 and 10,000 positions including ring closure; declared hectares require a positive value with at most four decimal places. These are technical limits, not professionally approved area tolerances. A centroid is not guaranteed to be inside the polygon.
- Geometry configuration uses one strong-ETag transition with same-transaction field/history/journal/outbox. It has no idempotency ledger: after an ambiguous response, read the authorized geometry GET to reconcile; never automatically repeat the write. Reconfiguration and configuration of archived fields are rejected. Editing/subdivision/map UI and official territorial intersection remain pending; official province/department codes are not inferred. Field creation remains nonspatial.
- Legacy configured rows are not silently backfilled or certified by this migration. Without a validated native snapshot they remain unavailable through geometry GET, even if their existing summary says configured. A separately reviewed reconciliation/backfill is required for any such preexisting data; do not clear or overwrite it to force configuration.
- Cycle operations now revalidate owner/resource scope in the same RLS transaction. New cycles on archived fields are rejected by both the application and the restricted database role; existing history stays readable. Iteration 55 replaces caller-supplied catalog metadata with an authoritative immutable reference; its [contract, rollout limits and verification status](production-cycle-catalog-reference.md) are tracked separately. Journal/outbox, idempotent mutation protocols and the full lifecycle policy for existing cycles remain pending.
- The inbox primitive claims a unique key before invoking its handler and commits the marker and **same-database** effects together. Real PostgreSQL tests cover duplicate races and rollback/retry. This is not exactly-once delivery to external systems, nor a provisioned dispatcher/job identity. Handlers must not perform irreversible external effects or use another context/transaction.
- Local TOTP enrollment remains Development/Test-only. Enrollment tokens are protected, session/user-bound and expire after ten minutes; setup/enable/disable/recovery consumption require CSRF and recent authentication. Disabling requires a current TOTP. Recovery-code consumption is atomic. Complete login step-up, production lifecycle/audit/RLS, passkeys and unauthenticated account recovery are not delivered by these component endpoints.

## Verification evidence

Acceptance requires locked restore/build, all backend and frontend suites, executable operation-register evidence, and existing browser regression flows. Empty test bodies no longer count as contract evidence. Per-slice evidence is recorded in `tasks/todo .md`; a green component test is not a completed roadmap task.
