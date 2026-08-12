# Security assessment — AGRO-SEC-002 Identity tenant v1

Fecha: 2026-08-11. Alcance: commit base `15ead58`, runtime integrado-local Identity + Territory. Método: reconnaissance repository-grounded, revisión por clases de ataque, validación de candidatos y gate ejecutable de superficie.

## Resultado ejecutivo

No se confirmaron vulnerabilidades de severidad crítica, alta o media explotables dentro del runtime local y configuración por defecto auditados. El registro cubre 20/20 operaciones HTTP, un callback OIDC framework-owned y cinco superficies futuras explícitamente ausentes.

La autorización tenant vigente tiene tres capas: autenticación HTTP, revalidación application de sesión/actor/scope/membership/assurance y rol PostgreSQL transaction-local con `FORCE RLS`. Territory es una capacidad platform-shared autenticada, no una frontera tenant.

## Hallazgos confirmados

Ninguno. `findings.json` es un array vacío validado contra el schema de la skill.

## Candidatos descartados o condicionados

- El cache de resolución Territory comparte coordenadas y `capturedAtUtc` entre usuarios. Georef está deshabilitado por defecto y no existe ambiente compartido, por lo que no hay explotación actual. Antes de habilitar egress multiusuario debe evitarse el oracle temporal mediante partición por actor o timestamps no correlacionables.
- Los endpoints sintéticos pueden emitir sesiones verificadas en Development/Test. Es un bypass intencional, doblemente gated y ausente en Production; cualquier ambiente compartido etiquetado Development sigue bloqueado operacionalmente.
- `AllowedHosts=*`, HSTS/edge, Data Protection persistente, limiter distribuido, credenciales productivas, Auth0 real y query redaction del collector requieren validación de despliegue. No se clasifican como vulnerabilidades confirmadas del runtime local.

## Controles positivos observados

- Cookie opaca revocable, CSRF, no-store y rate limits.
- OIDC code+PKCE, state/nonce framework, `auth_time`, issuer/subject y MFA purpose-bound.
- Actor y tenant derivados server-side; route IDs sólo son locators.
- Owner invitation bearer 256-bit, fragment removido, HMAC en DB, one-shot y reautorización.
- Grants mínimos, roles sin ownership/BYPASS, contexto `SET LOCAL` y `FORCE RLS` para la frontera tenant integrada.
- Georef default-off, destino fijo, sin redirects/cookies/loggers, timeout/tamaño/schema/echo de coordenadas acotados.
- Telemetría allow-listed sin tenant, bearer, coordenadas o nombres.

## Estado

PASS para este incremento Identity tenant v1. AGRO-SEC-002 permanece `En curso`: cada módulo/recurso nuevo debe sumar su fila, pruebas negativas y storage boundary; storage/export/jobs/retrieval/AI y deploy externo no están cubiertos.

