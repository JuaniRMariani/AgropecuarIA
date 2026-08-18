# Decisiones — OwnSessionManagementV1

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
- `DELETE /api/identity/sessions/others` exige cookie, CSRF y purpose exacto `manage_sessions`; no recibe body, IDs, organización ni `If-Match`, y siempre responde 204 sin count ni identificadores. La sesión actual queda excluida por el servidor.
- La UI sólo renderiza el UUID corto de seis caracteres. El UUID completo viaja únicamente por el contrato autenticado y nunca se registra como tag de telemetría.

## Revocación y evidencia

- La transición activa→revocada es monotónica y concurrent-safe. El primer éxito escribe `RevokedAtUtc`, rota `Version` y agrega un único journal local dentro de la misma transacción.
- Replay sobre una sesión propia ya revocada devuelve éxito sin revivirla, rotar otra vez ni duplicar journal. Una sesión todavía activa que ya expiró no se presenta en inventario y responde 404 neutral; el replay revocado conserva la precedencia 204 anterior.
- El comando bulk serializa por actor y revoca únicamente sesiones propias activas, no expiradas y distintas de la actual visibles al `UPDATE`. Cada target cambia una vez con un timestamp común y una nueva `Version`; cada transición agrega su propio journal `session_revoked` en la misma transacción. Cero targets y replay devuelven 204 sin journal adicional.
- El corte es lineal al statement de actualización. Una sesión cuya emisión se confirma después no pertenece al comando anterior y puede permanecer activa; por eso la UI refresca el inventario y no afirma un vacío global permanente.
- La cookie revocada debe fallar en el siguiente request porque cada operación revalida la sesión durable. No se promete propagación a caches/jobs externos inexistentes.
- No se publica evento ni notificación en estos sub-slices; no se inventa consumidor FND-002. La migración bulk agrega una función `SECURITY DEFINER` execute-only y reemplaza de forma compatible la función individual para compartir el mismo lock por actor; `Down` restaura exactamente la definición N-1. No cambia tablas/modelo, reason, dispositivo, purge o plazo legal, y ningún rollback reactiva sesiones ya revocadas.

## Límites

- Quedan fuera cierre de la sesión actual mediante bulk, familias, dispositivos, fingerprint/UA/IP, alertas/email, administración de sesiones ajenas, superadmin, cache distribuida, worker, retención/purge y deploy compartido.
- El gate local cubre API/PostgreSQL y Chromium desktop/móvil. Auth0 real, edge/TLS/proxy, Data Protection persistente y operación compartida continúan como gates externos.
