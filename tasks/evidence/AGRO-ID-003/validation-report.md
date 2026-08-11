# AGRO-ID-003 — validación de CreateOrganization

Fecha de cierre: 2026-08-11  
Resultado del sub-slice: **PASS local integrado**  
Estado de la tarea padre: **En curso**

## Valor demostrado

Una persona con sesión verificada y autenticación reciente puede crear varias organizaciones privadas. El servidor deriva actor y scope, crea la organización y su membership `owner`, y confirma ledger idempotente, journal y outbox en una transacción PostgreSQL. El hub muestra 0/1/N organizaciones y permite crear otra sin bloquear el shell.

`owner` es un rol tenant operativo: no concede superadmin ni acceso cross-tenant. Invitaciones, otros roles, transferencia/democión, último owner, scopes por campo, GIS, superadmin y soporte JIT permanecen fuera de este sub-slice.

## Controles y arquitectura

- `POST /api/identity/organizations` acepta únicamente `displayName`; exige cookie, CSRF, rate limit e `Idempotency-Key` opaca.
- La sesión se revalida dentro de la transacción y antes de consultar aliases/ledger. Assurance no verificada, autenticación con edad mayor o igual a 15 minutos, sesión expirada o revocada fallan sin efecto.
- El ledger liga actor, sesión, versión de autorización y fingerprint canónico. Aliases HMAC multiversión permiten v1 → v1+v2 → v2-only; el retiro sin cobertura global falla cerrado.
- Organization, owner membership autoritativa y proyección N-1, ledger, aliases, journal y `OrganizationCreated` se confirman atómicamente.
- La migración aditiva conserva el writer N-1. Roles de privilegio mínimos, `SET LOCAL`, grants por columna, `FORCE RLS` y policies actor/scope protegen organización, membership y ledger.
- El frontend genera 128 bits CSPRNG por intento, conserva key/draft solo en `sessionStorage` durante reautenticación/retry y no muestra UUID completos.
- La telemetría usa operation/outcome allow-listed; no incluye nombre, UUID, actor/tenant, key, digest ni payload.

## Evidencia ejecutada hasta el gate integrado

| Gate | Resultado |
|---|---|
| Restore locked + build Release | PASS; 0 warnings, 0 errores. |
| Suite raíz MTP | PASS; 142/142, 0 failed, 0 skipped. |
| EF pending model changes | PASS final; no hay cambios de modelo pendientes. |
| Frontend pnpm frozen/format/lint/typecheck/build | PASS. |
| Vitest | PASS; 50/50. |
| Playwright desktop + Pixel 7 | PASS; 4/4, Axe, teclado y 390 px. |
| FND protocol | 45/45 mutations PASS; evidencia repository se reconcilia con el producer integrado. |
| SEC threat-model validator | PASS; 25/25 mutations rechazadas. |
| SCA NuGet + pnpm | PASS; 0 vulnerabilidades conocidas. |
| `git diff --check` | PASS; warning informativo CRLF del snapshot. |

## Fallos encontrados y corregidos

El gate combinado y la revisión independiente detectaron y corrigieron:

1. schema de `OrganizationCreated` fuera del catálogo ejecutable;
2. rollback con dependencias FK/RLS en orden incorrecto;
3. test histórico dependiente del fixture demo eliminado;
4. runner E2E sin bootstrap y con PostgreSQL `trust`;
5. contaminación entre tests por identidad sintética compartida;
6. rotación HMAC que podía perder la identidad lógica al retirar una versión;
7. grants completos sobre `users/sessions` y falta de RLS actor-scoped;
8. idempotency key UI con menos de 128 bits aleatorios;
9. estado de reautenticación sin recuperación accionable;
10. falta de pruebas de rollback atómico, estados terminal/expirado y telemetría acotada.
11. commit incierto sin reconciliación reproducible desde una conexión nueva;
12. runner E2E capaz de aceptar un servidor ajeno si el puerto estaba ocupado;
13. OpenAPI sin `Retry-After` para in-progress/reconciliación y descripción incorrecta de la key como `sf-string`.

## Compatibilidad, rollback y operación

- Estrategia `expand`, coexistencia N/N-1 y dual-write temporal de la proyección legacy.
- Rollback de aplicación: deshabilitar el endpoint/feature y ejecutar binario N-1; las tablas nuevas permanecen compatibles.
- `Down` destructivo está permitido únicamente por el guard de base efímera. Bases compartidas usan roll-forward.
- El harness E2E usa SCRAM-SHA-256, password y HMAC efímeras, ACL restringida, restaura variables y elimina el cluster.
- `AGRO-FND-002` conserva `runtimeImplemented=false`: dispatcher, inbox, retry/poison, delivery y conciliación operativa siguen pendientes.

## Riesgos residuales

- Auth0 real, edge, Data Protection compartida, rate limit distribuido, secretos administrados y principal/migrator de ambiente compartido siguen NO-GO de despliegue.
- Discovery general 0/1/N y cada nueva mutación tenant deben repetir autorización, roles, RLS y negativos; esta evidencia solo cubre `CreateOrganization`.
- Retención/legal hold/purge y Audit/Compliance central dependen de Q-060/VAL-LEG.
- La tarea padre `AGRO-ID-003` permanece `En curso` por invitaciones, matriz de roles, último owner y alcances por campo.

## Revisión final

- Resultado: `PASS` del sub-slice local `CreateOrganization`; cero hallazgos críticos, altos o medios abiertos.
- Backend: restore locked, build Release con 0 warnings/errores, suite raíz MTP 142/142, format y EF pending-model `PASS`.
- Frontend: instalación pnpm frozen, format, lint, typecheck y build `PASS`; Vitest 50/50 y Playwright 4/4 en Chromium desktop y Pixel 7 con Axe, teclado y viewport de 390 px.
- Contratos y seguridad: validadores FND 45/45 y SEC 25/25 `PASS`; SCA NuGet/pnpm sin vulnerabilidades conocidas; JSON, UTF-8, secret scan y `git diff --check` `PASS`.
- Revisión independiente Architecture, AppSec y QA: `PASS` tras corregir rotación HMAC, grants DB, atomicidad, recuperación UI, aislamiento E2E y cobertura de estados terminales.
- Estado: `AGRO-ID-003` continúa `En curso`. El sub-slice no implementa invitaciones, otros roles, último-owner runtime, scopes por campo, superadmin ni soporte cross-tenant.
- No hubo deploy. La publicación se limita al commit/push autorizado del repositorio.

## Refresh: invitación one-shot de co-owner — 2026-08-11

Resultado del sub-slice: **PASS local integrado**. Estado de `AGRO-ID-003`: **En curso**.

El owner activo puede crear, listar y revocar enlaces one-shot que conceden exclusivamente otra membership `owner`. Crear y revocar exigen step-up `manage_organization_owners`; aceptar exige identidad verificada y autenticación reciente. El token de 256 bits se entrega una sola vez en un fragmento URL, se persiste solo como digest HMAC versionado y no aparece en journal, outbox, telemetría ni respuestas posteriores.

La implementación demuestra creación concurrente con una sola divulgación del bearer, aceptación por un usuario distinto, replay neutral por otro actor, aceptación/revocación serializada, expiración exacta, rotación y retirada fail-closed de claves, rollback ante fallos de journal/outbox, dual-write N/N-1 y aislamiento PostgreSQL mediante `FORCE RLS` y principals mínimos. Los tres eventos tipados no contienen token, digest, nombre ni identidad del actor.

### Evidencia final

| Gate | Resultado |
|---|---|
| Restore locked + build Release | PASS; 0 warnings, 0 errores. |
| Suite raíz MTP | PASS; 170/170, 0 failed, 0 skipped. |
| Architecture fitness | PASS; 70/70 dentro de la suite raíz. |
| Format .NET + EF pending model | PASS; sin drift de modelo. |
| pnpm frozen/format/lint/typecheck/build | PASS. |
| Vitest | PASS; 67/67. |
| Playwright desktop + móvil | PASS; 4/4 con inviter/invitee/attacker, revocación, Axe, teclado y 390 px. |
| FND protocol | PASS; 45/45 mutations rechazadas. |
| SEC threat model | PASS; 26/26 mutations rechazadas; 14 amenazas con owner/test/gate. |
| SCA NuGet + pnpm | PASS; 0 vulnerabilidades conocidas. |
| JSON, UTF-8 y `git diff --check` | PASS; warning CRLF del snapshot solo informativo. |

La revisión independiente concluyó 0 hallazgos críticos, altos o medios abiertos. Se corrigieron durante los gates: deriva OpenAPI/ownership de agregado, constraint SQL que aceptaba NULL por semántica UNKNOWN, cobertura/grants de funciones estrechas, aislamiento de identidades E2E, rate limit del harness, journey SPA del atacante y confirmación de revocación.

Permanecen fuera: invitación dirigida por email/delivery, roles distintos de owner, scopes por campo, democión/remoción/último-owner runtime, superadmin/JIT y deploy. Auth0 real, secretos administrados, edge, limiter distribuido, Audit central y retención legal continúan como gates de ambiente compartido.
