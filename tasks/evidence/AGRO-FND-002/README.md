# AGRO-FND-002 — Protocolo de mutaciones tenant-safe

Estado del incremento: contrato R1 aprobado. Estado de la tarea padre: `En curso` hasta demostrar el patrón sobre un consumidor real, su frontera tenant posterior y PostgreSQL productivo.

## Resultado acotado

Este incremento elimina ambigüedades de idempotencia, auditoría y delivery antes de crear persistencia compartida o un endpoint ficticio. Produce:

- política de idempotencia/transacción/outbox/inbox;
- política de auditoría, retención y amenazas;
- registro machine-readable del protocolo;
- validador con mutaciones negativas;
- secuencia explícita para que `AGRO-ID-003` implemente `CreateOrganization` como primer consumidor tenant.

No implementa organización, invitaciones, roles, RLS productiva, ledger, dispatcher, inbox, UI, migraciones ni worker. `AGRO-FND-002` no completa su DoD con estos documentos: debe permanecer `En curso` hasta que el consumidor real demuestre autorización, RLS, concurrencia, crash/replay, atomicidad, delivery y telemetría.

## Artefactos

- [`idempotency-and-delivery-policy.md`](idempotency-and-delivery-policy.md): contrato, estados, transacción, orden, delivery y rollout.
- [`audit-retention-and-threats.md`](audit-retention-and-threats.md): fail-closed local, proyección central, privacidad, retención y amenazas.
- [`foundation-protocol.json`](foundation-protocol.json): invariantes ejecutables del protocolo v1.
- [`validate-foundation-protocol.ps1`](validate-foundation-protocol.ps1): validación positiva y mutation tests.
- [`validation-report.md`](validation-report.md): comandos, resultados, revisión y riesgos residuales.

## Decisión de secuencia

1. `AGRO-FND-002` fija el contrato y los gates; no crea un bounded context Foundation ni persistencia común.
2. `AGRO-ID-003` implementa `CreateOrganization` en Identity/Tenancy como primer consumidor, junto con sus tablas, migraciones, roles, `FORCE RLS`, contexto transaccional y autorización.
3. La implementación del consumidor debe incorporar ledger, journal local y outbox en la misma transacción y aportar pruebas A/B/sin contexto/pool/job/crash/replay.
4. `AGRO-FND-002` permanece `En curso` hasta integrar esa evidencia; `AGRO-SEC-002` amplía la suite a las demás vías/superficies y no sustituye autorización de aplicación.

Esta secuencia rompe el ciclo de planificación sin ocultar trabajo de `AGRO-ID-003` dentro de FND-002 ni cambiar la Definition of Done.

## Fuentes primarias

- IETF Datatracker, `draft-ietf-httpapi-idempotency-key-header-07`: expiró y fue archivado el 2026-04-18 sin convertirse en RFC. Se usa únicamente como antecedente; AgropecuarIA define un contrato propio y versionado: <https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/07/>.
- EF Core, transacciones: una transacción agrupa escrituras atómicas y el retry implícito no debe mezclarse ciegamente con transacciones manuales: <https://learn.microsoft.com/ef/core/saving/transactions>.
- EF Core, connection resiliency: una desconexión durante commit deja resultado incierto y requiere verificación idempotente: <https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency>.
- PostgreSQL, locking clauses: `SKIP LOCKED` produce una vista inconsistente y solo corresponde a consumidores de tablas tipo cola: <https://www.postgresql.org/docs/current/sql-select.html>.
- PostgreSQL, row security: RLS habilitada sin policy aplica default-deny; owners y roles `BYPASSRLS` requieren tratamiento explícito: <https://www.postgresql.org/docs/current/ddl-rowsecurity.html>.

## Gate local

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tasks\evidence\AGRO-FND-002\validate-foundation-protocol.ps1 -SelfTest
```

Además se validan JSON, UTF-8 estricto, referencias, parser PowerShell, secretos y `git diff --check`. Los gates .NET/frontend/PostgreSQL/E2E son `N/A` para este incremento contractual porque no modifica runtime; pasan a ser obligatorios en el primer consumidor.
