# Decisiones — renombrar campo borrador

Fecha: 2026-08-18. Estado: aceptado para desarrollo local integrado; `AGRO-FND-003` permanece `En curso`.

## Alcance y autoridad

- Sólo una membership `owner/active` con sesión vigente puede renombrar un campo de su organización. El nombre borrador es corregible; creador y owners posteriores tienen la misma autoridad tenant.
- El slice sólo modifica `displayName` de un `ManagementUnit field/draft/not_configured`. No modifica geometría, área, territorio, tipo, estado, spatial status, catálogo ni identidad.
- `organizationId` y `fieldId` son locators. Actor, sesión, membership, tenant y autorización se derivan y revalidan en servidor y en PostgreSQL.
- Campo ausente/ajeno, organización ajena, owner removido, sesión revocada o contexto incompleto responden sin revelar existencia cross-tenant.

## Contrato y canonicalización

- `PATCH /api/organizations/{organizationId}/fields/{fieldId}` exige cookie, CSRF, `Idempotency-Key` y exactamente un `If-Match` fuerte con UUID entre comillas.
- El body cerrado contiene sólo `{displayName}`. Se recortan Unicode `White_Space` y `U+FEFF`, se normaliza a NFC, se admiten 2..120 escalares y se rechazan controles y surrogates aislados, igual que en CreateField.
- Nombres duplicados continúan permitidos. Renombrar al mismo nombre canónico es un request inválido y no crea versión, ledger, journal ni outbox.
- Un éxito devuelve la representación plana, `revision`, `isReplay` y un ETag fuerte nuevo. List/detail convergen al nombre confirmado.

## Conflicto e idempotencia

- El orden obligatorio es autorización viva, lookup/replay idempotente ligado, resolución del recurso y comparación `If-Match`, luego mutación.
- Un replay autorizado con mismo actor, tenant, field, sesión/auth version, key, nombre y versión esperada devuelve el resultado confirmado aunque el ETag original ya sea stale.
- Reusar key con otro nombre, field o `If-Match` devuelve 409 neutral. Un intento nuevo con ETag stale devuelve 412 sin representación actual ni last-write-wins.
- La rotación HMAC usa aliases de todas las versiones retenidas: durante overlap un replay agrega de forma lazy el alias nuevo; retirar una versión antes de cobertura global devuelve 503 antes de lookup/efecto y un alias split exige reconciliación sin elegir ledger.
- `Version` rota como UUID y `Revision` aumenta monotónicamente por rename confirmado. Dos escritores con el mismo ETag producen un único cambio.
- Commit incierto se reconcilia con un contexto nuevo: resultado confirmado se reproduce, ausencia comprobada admite un único retry y resultado ambiguo falla cerrado conservando la key.

## Atomicidad, eventos y privacidad

- Field, ledger/aliases, journal local y outbox confirman en una transacción PostgreSQL.
- `ManagementUnitDisplayNameChanged` contiene sólo `organizationId`, `managementUnitId`, revisión y fecha. No publica nombre anterior/nuevo, actor, sesión, key, digest ni payload.
- Journal y telemetría tampoco registran nombres o material idempotente; métricas usan únicamente operation/outcome acotados.
- Delivery del outbox, inbox, retry/poison y consumidores siguen pendientes de `AGRO-FND-002`; no se promete exactly-once de transporte.

## Compatibilidad y rollout

- La migración es expand: `Revision` usa default 1 y ledger/aliases/índices son aditivos. N-1 sigue leyendo/escribiendo campos creados y no conoce PATCH.
- El flag de rename queda apagado por defecto. Development/Test lo habilita con keyring local o efímero separado; ambientes compartidos requieren secretos y principals aprobados.
- Rollback de aplicación apaga PATCH y conserva el nombre confirmado; no intenta revertir un dato ya observado. `Down` destructivo sólo se prueba en PostgreSQL efímero identificado y ambientes compartidos usan roll-forward.
- No hay deploy en este incremento.
