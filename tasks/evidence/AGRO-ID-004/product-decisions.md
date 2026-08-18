# Decisiones — OwnSessionInventoryAndRevokeV1

Fecha: 2026-08-18. Estado: aceptado para desarrollo local integrado; `AGRO-ID-004` permanece `En curso`.

## Autoridad y alcance

- El recurso es platform-scoped y propio del usuario autenticado. Organización, rol owner y contexto tenant no participan.
- Actor y sesión actual se derivan exclusivamente de la cookie opaca revalidada en servidor. El cliente sólo identifica una sesión target mostrada previamente por el inventario propio.
- La sesión actual conserva el logout existente. El endpoint nuevo rechaza revocarla y la UI dirige al control “Cerrar sesión”.
- Revocar otra sesión requiere step-up purpose-bound exacto `manage_sessions`; un assurance obtenido para owners o métodos de autenticación no lo sustituye.

## Contrato y privacidad

- `GET /api/identity/sessions?offset=0&limit=20` pagina entre 1 y 50 sesiones propias activas, ordenadas por `authenticatedAtUtc DESC, sessionId ASC`, y devuelve `{items,total,offset,limit}`.
- Cada item contiene únicamente `sessionId`, `authenticatedAtUtc`, `expiresAtUtc`, `isCurrent` y `version`. No se expone token/hash, IP, user-agent, ubicación, fingerprint, proveedor ni organización.
- `DELETE /api/identity/sessions/{sessionId}` exige cookie, CSRF e `If-Match` fuerte. Target ajeno, ausente o activo-expirado falla con 404 neutral; current usa 409 tipado; precondición stale usa 412. Una sesión propia ya revocada conserva replay 204, aun si su expiración ocurre después, sin segundo journal ni rotación.
- La UI sólo renderiza el UUID corto de seis caracteres. El UUID completo viaja únicamente por el contrato autenticado y nunca se registra como tag de telemetría.

## Revocación y evidencia

- La transición activa→revocada es monotónica y concurrent-safe. El primer éxito escribe `RevokedAtUtc`, rota `Version` y agrega un único journal local dentro de la misma transacción.
- Replay sobre una sesión propia ya revocada devuelve éxito sin revivirla, rotar otra vez ni duplicar journal. Una sesión todavía activa que ya expiró no se presenta en inventario y responde 404 neutral; el replay revocado conserva la precedencia 204 anterior.
- La cookie revocada debe fallar en el siguiente request porque cada operación revalida la sesión durable. No se promete propagación a caches/jobs externos inexistentes.
- No se publica evento ni notificación en este sub-slice; no se inventa consumidor FND-002. La única migración amplía de forma aditiva los CHECK de purpose para `manage_sessions`; no agrega reason libre ni columnas de sesión, purge o plazo legal.

## Límites

- Quedan fuera revoke-all, familias, dispositivos, fingerprint/UA/IP, alertas/email, administración de sesiones ajenas, superadmin, cache distribuida, worker, retención/purge y deploy compartido.
- El gate local cubre API/PostgreSQL y Chromium desktop/móvil. Auth0 real, edge/TLS/proxy, Data Protection persistente y operación compartida continúan como gates externos.
