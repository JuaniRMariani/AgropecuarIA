# AGRO-SEC-002 — gate tenant Identity + Productive Core v1

Incremento integrado-local de la tarea multirelease `AGRO-SEC-002`. El padre permanece `En curso`.

## Qué queda comprobado

- Las 26 operaciones HTTP actuales de Identity, Territory y Productive Core coinciden entre OpenAPI, rutas y `authorization-surface-register.json`.
- Cada operación declara recurso, acción, frontera, autenticación, fuentes de actor/tenant, autorización de aplicación, frontera de storage, error neutral, owner y prueba ejecutable.
- Las cuatro operaciones de Productive Core —crear, listar, abrir y renombrar un campo— son tenant-scoped y sólo admiten owner activo con sesión viva revalidada antes de cualquier lookup de recurso o idempotencia.
- `POST /api/organizations/{organizationId}/fields` exige cookie, CSRF y `Idempotency-Key` URL-safe de 32..128 caracteres. Actor, sesión, tenant, tipo `field` y estados `draft/not_configured` se fijan en servidor.
- La creación conserva unidad, ledger y aliases HMAC versionados, journal y `ManagementUnitCreated` en una única transacción; replay, fingerprint distinto, carrera y commit incierto fallan cerrados o recuperan el mismo resultado.
- El rename exige CSRF, key idempotente y ETag/If-Match fuerte; un intento stale recibe 412 neutral. Rota versión, incrementa revisión y escribe `ManagementUnitDisplayNameChanged` sin nombre anterior/nuevo.
- PostgreSQL usa el puerto estrecho `identity.authorize_productive_owner()`, contexto transaction-local, principal propio, privilegios mínimos y `FORCE RLS`; Productive Core no consulta tablas Identity desde Application.
- Lista y ficha responden neutralmente ante organización, membership o field ausentes/ajenos. Las respuestas privadas usan `no-store` y rate limit por sesión.
- `ManagementUnitCreated` y `ManagementUnitDisplayNameChanged` son tenant-scoped y no contienen nombre, actor, idempotency key/digest ni geometría. La telemetría usa tags allow-listed sin UUID, tenant o display name.
- El frontend valida la frontera JSON, usa React escaping, conserva la misma idempotency key durante retry/reauth/reconciliación y sólo muestra UUID corto.
- Territory continúa como referencia compartida autenticada, sin autoridad tenant. Las rutas sintéticas permanecen `development-test-only` y ausentes en Production.

## Artefactos

- `architecture.md`: reconnaissance y fronteras auditadas.
- `authorization-surface-register.json`: matriz machine-readable vigente de 26 operaciones.
- `REPORT.md`, `FINDINGS-DETAIL.md`, `findings.json`: resultado del security audit.
- `validation-report.md`: comandos, resultados y límites reproducibles.
- Fitness FND/SEC: enforcement de rutas, contratos, eventos y storage boundaries.

## Límites honestos

Este resultado prueba el runtime integrado local; no autoriza un deploy compartido. No demuestra Auth0 real, edge/TLS/proxy, Data Protection persistente, limiter distribuido, secrets manager, exporter/collector, backup/restore operativo ni egress Georef multiusuario. Tampoco prueba todavía geometría, PostGIS productivo, área, mapa, edición distinta del nombre o borrado de campos: esas capacidades quedan fuera de este sub-slice.

El cache de resolución Territory continúa global por coordenada. Con Georef default-off no hay explotación actual confirmada, pero debe particionarse o dejar de exponer timestamps correlacionables antes de habilitar egress compartido.
