# Validación — OwnSessionManagementV1

Estado: PASS integrado-local; `AGRO-ID-004` permanece `En curso`.

## Alcance a demostrar

- Inventario paginado de sesiones propias sin metadata de dispositivo ni secretos.
- Revocación individual de otra sesión con purpose `manage_sessions`, CSRF, `If-Match`, neutralidad y journal único.
- Invalidación inmediata de la cookie revocada en Identity y Productive Core.
- UI Cuenta accesible, UUID corto, desktop/móvil y aislamiento platform sin depender de la organización activa.
- Cierre atómico de todas las demás sesiones propias con purpose `manage_sessions`, CSRF, sesión actual preservada, 204 sin IDs/count y un journal por target efectivo.
- Cierre global atómico de todas las sesiones propias, incluida current, con el mismo purpose/CSRF, cookie eliminada sólo post-commit y respuesta 204 sin IDs/count.

## Gates

- Restore locked y build Release: PASS, 9 proyectos, 0 warnings y 0 errores.
- Suite raíz MTP: PASS `381/381`; Identity completa `146/146`; Architecture Fitness `135/135`.
- PostgreSQL revoke-all-own: PASS `18/18`; API own-session: PASS `14/14`; cubre 0/1/N, global×global/×individual/×bulk-others/×logout, login post-corte, journal-per-target/rollback, cancelación/pool, privilegios y N/N-1/rollback-forward. EF Identity/Territory/Productive Core `3/3` sin drift.
- Frontend frozen install, Prettier, ESLint, TypeScript y Next build: PASS; Vitest `225/225`.
- Playwright oficial: PASS `10/10` en Chromium desktop y mobile/390. El journey prueba revocación individual, bulk-others y cierre global: current y otra sesión reciben 401 tras el global, con confirmación accesible, privacidad y Axe. PostgreSQL/API/web temporales finalizaron sin listeners en 3000/5080.
- FND protocol `45/45`, SEC threat model `56/56` y SEC-002 `30/30` operaciones: PASS.
- NuGet audit en 9 proyectos y pnpm audit productivo: sin vulnerabilidades conocidas. UTF-8/JSON, parser PowerShell, secret scan, format y diff-check: PASS.

## Revisión

- Las selecciones de Iteración 33/34 fueron unánimes: primero `RevokeAllOtherOwnSessionsV1` y luego `RevokeAllOwnSessionsAndLogoutV1`. `ArchiveFieldDraft`, FE residual, Catalog y FND-002 permanecen NO-GO por decisiones, runtime o consumidor real ausentes.
- Las funciones individual y bulk son `SECURITY DEFINER`, fijan `search_path=pg_catalog` y comparten un advisory transaction lock por actor antes de revalidar current/version/purpose. La migración restaura la definición individual N-1 en `Down`. La carrera determinista A→B individual contra B→A bulk preserva al menos una current válida. El servicio valida la forma interna, agrega un journal por target y confirma todo en la misma transacción; un fallo de cualquier journal revierte el batch.
- La respuesta HTTP nunca expone target IDs ni count. La sesión actual se preserva y un login confirmado después del statement de actualización queda fuera del corte, por lo que la UI refresca sin afirmar un vacío permanente.
- El comando global comparte el mismo lock actor con logout, individual y bulk-others, incluye current y elimina su cookie sólo después del commit. Un login posterior al corte queda fuera; una respuesta perdida se reconcilia mediante 401/200 sin mensaje falso de éxito. El 401 sólo confirma current revocada: tras reingresar, el inventario es la autoridad sobre las demás sesiones.

- La primera ejecución E2E detectó colisiones entre perfiles sintéticos genéricos: contextos distintos podían representar usuarios diferentes o hacer coincidir owner y attacker. El setup quedó corregido con perfiles explícitos/disjuntos por proyecto y revocación por el short ID exacto de la sesión B; no se relajaron los 401/404.
- Una corrida raíz registró un único flake Productive preexistente bajo presión; el caso pasó 3/3 aislado y la repetición raíz completa pasó `358/358`. La revisión read-only no encontró evidencia que justificara un cambio productivo especulativo.
- La revisión independiente encontró y permitió cerrar cinco Medium antes del gate final: copy incorrecto de purpose, claims sin prueba ejecutable, precedencia revocada/expirada ambigua, inventario stale tras rotación y 401 tratado como falso MFA local. Dictamen final: GO, 0 Critical, 0 High y 0 Medium pendientes.

## Riesgos residuales

- El padre conserva dispositivos/familias, notificaciones, propagación distribuida, SLO y retención/purge.
- No se aprueba deploy compartido ni se afirma delivery, cache/job invalidation o aviso al usuario.

Publicación funcional: `45a7b91` (`feat(identity): manage own active sessions`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.

Publicación bulk: `36429af` (`feat(identity): close other active sessions`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.

Publicación global: `5b123e3` (`feat(identity): close all active sessions`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.
