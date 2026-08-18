# Security assessment — AGRO-SEC-002 tenant boundary v1

Fecha: 2026-08-18. Base publicada: `5f32e15`; alcance actual: worktree integrado-local Identity + Territory + Productive Core.

## Resultado ejecutivo

No se confirmaron vulnerabilidades críticas, altas o medias explotables dentro del runtime local y la configuración default-off auditados. El registro vigente cubre 26/26 operaciones HTTP, incluido create/list/detail/rename de `ManagementUnit` en Productive Core.

La frontera Productive Core combina autenticación cookie, CSRF para escritura, autorización owner revalidada por un puerto Identity estrecho, contexto PostgreSQL transaction-local y `FORCE RLS`. La organización de la ruta funciona sólo como locator: actor, sesión y autorización se obtienen del servidor antes de recurso, alias o ledger. Creación y rename idempotentes son atómicos; rename suma ETag/If-Match fuerte, revisión monotónica, 412 neutral y un evento sin nombres.

## Hallazgos confirmados

Ninguno. `findings.json` permanece como array vacío y debe seguir validando contra el schema de `security-audit`.

## Candidatos descartados o condicionados

- El parser y la UI congelan la idempotency key a 32..128 caracteres URL-safe; el backend fue alineado durante esta revisión. El valor sólo llega a HMAC versionado y nunca se persiste o registra en claro.
- La UI fue alineada con el dominio al recorte Unicode `White_Space`+`U+FEFF`, NFC, 2..120 escalares y rechazo de controles/surrogates aislados; las fronteras astrales 120/121 y los casos `U+0085`/`U+FEFF` quedaron verificados.
- El cache Territory comparte coordenadas y `capturedAtUtc`. Georef sigue default-off y no hay ambiente compartido, por lo que no existe un ataque activo reproducible. La partición/no correlación es gate previo al egress multiusuario.
- `AllowedHosts=*`, HSTS/edge, Data Protection persistente, limiter distribuido, credenciales productivas, Auth0 real y redaction del collector requieren validación de despliegue; no son vulnerabilidades confirmadas por el source local.

## Controles positivos observados

- Cookie opaca revocable, CSRF, `no-store`, rate limit y errores Problem neutrales.
- Autorización de owner vivo antes de cualquier lookup/replay y revalidación DB mediante UUID de versión de sesión.
- HMAC-SHA-256 versionado, aliases tenant-scoped, fingerprint ligado a actor/sesión/autorización/payload y recovery explícito de commit incierto.
- Unidad, ledger, aliases, journal y outbox en la misma transacción serializable; evento mínimo sin PII ni nombre.
- SQL dinámico limitado a placeholders generados internamente y valores Npgsql parametrizados; no se encontró flujo explotable de SQLi.
- DTOs frontend parseados desde `unknown`, UUID/enum/tenant revalidados y render de display name mediante escaping de React; no se encontró source→sink de XSS.
- Telemetría Productive Core sólo emite operación, outcome y cardinalidad acotados.

## Estado

PASS de seguridad para el sub-slice integrado-local, sujeto a los gates finales consignados en `validation-report.md`. `AGRO-SEC-002` permanece `En curso`: cada recurso, job, export, retrieval, IA o boundary nuevo debe incorporarse al registro en el mismo cambio que su runtime y contrato.
