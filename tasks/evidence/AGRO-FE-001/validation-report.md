# Validación — OwnerWorkspaceShellV1 + OwnerFieldDeepLinkV1

Estado: `PASS` para `OwnerWorkspaceShellV1` y `OwnerFieldDeepLinkV1` integrado-local. `AGRO-FE-001` permanece `En curso` por el alcance residual del padre.

## Alcance a demostrar

- Contexto 0/1/N resuelto contra la sesión y URL corta sin autoridad propia.
- Una sola organización consultada, con cancelación/invalidez de respuestas anteriores.
- Navegación accesible por campos, equipo, territorio y cuenta; draft e intentos ambiguos preservados de forma tenant-safe.
- Desktop/móvil, teclado, Axe, 390 px y ausencia de UUID completos.

## Gates

- Restore locked y build Release de la solución: `PASS`, 0 warnings y 0 errores.
- Suite raíz .NET/MTP: `348/348 PASS`; Architecture Fitness mantiene `135/135 PASS` y el registro SEC-002 conserva `26/26` operaciones HTTP sin cambios de superficie.
- Frontend: instalación frozen, Prettier, ESLint, TypeScript y Next production build `PASS`; Vitest `179/179 PASS` en 9 archivos.
- Playwright oficial hermético: `8/8 PASS` en Chromium desktop y móvil, incluyendo selector 0/1/N, organizaciones con etiqueta duplicada, Back/Forward, Axe y viewport de 390 px. PostgreSQL temporal cerró por fast-shutdown y los puertos del runner quedaron libres.
- FND protocol: `45/45` mutations rechazadas; SEC threat model: `56/56` mutations rechazadas y 14 amenazas vigentes (7 critical, 7 high) con owner/test/gate.
- Supply chain: los 9 proyectos NuGet y las dependencias productivas pnpm no reportan vulnerabilidades conocidas.
- UTF-8/JSON/PowerShell parser, secret scan y `git diff --check`: `PASS`.

## Revisión

- Revisión independiente final: `PASS`, 0 Critical, 0 High y 0 Medium abiertos.
- Se detectaron y cerraron antes de publicar cinco hallazgos Medium: compensación Back/Forward sin destruir history, reflow de nombres largos, limpieza de anuncios stale, preservación tenant-safe de la key ante remoción ambigua y separación entre `429` terminal y outcomes ambiguos que sólo bloquean cambiar de organización.
- No se modificaron backend, schemas, OpenAPI, migraciones, eventos ni políticas de autorización; no hubo deploy.
- Publicación funcional: `0a170e0` (`feat(frontend): add owner workspace shell`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`.

## Riesgos residuales

- El padre conserva roles/contextos adicionales, preferencias, matriz completa de navegadores y certificación manual.
- El backend sigue siendo la única autoridad; el shell no evita por sí solo BOLA ni sustituye SEC-002.
- No se aprueba deploy, PWA/offline, telemetría compartida ni almacenamiento de datos de negocio en cliente.

## Refresh — OwnerFieldDeepLinkV1

- URL canónica `?org=ABCDEF&view=fields&field=123ABC`; el locator acepta sólo seis hexadecimales, no persiste UUID ni concede autoridad. El UUID completo se obtiene únicamente de la lista ya autorizada para la organización activa.
- Cero coincidencias, colisión o locator cross-tenant fallan cerrados sin GET detail. `401/404` limpian ficha y locator; offline, `429` y `503` preservan el locator corto para retry.
- La navegación usa history real con reload, Back/Forward, Escape y guards dirty/pending. Cada detail queda ligado a organización, campo y generación; al cambiar A→B se aborta/invalida la respuesta tardía aunque ambos campos compartan el mismo prefijo.
- Crear un campo conserva simultáneamente la confirmación y la ficha local previa, sin cambiar URL ni locator. Abrir una ficha desde la lista sí publica el locator canónico.
- Los IDs DOM de formularios y headings son opacos; no se muestran UUID completos en DOM de la aplicación, URL, errores o telemetría. Los reintentos one-shot preexistentes conservan sus IDs internos en `sessionStorage` sin que el deep link agregue storage.

### Gates del refresh

- Restore locked y build Release: `PASS`, 9 proyectos, 0 warnings y 0 errores. Suite raíz MTP: `381/381 PASS`; Architecture Fitness incluido en la suite conserva `135/135 PASS`.
- Frontend frozen install, Prettier, ESLint, TypeScript y Next production build: `PASS`; Vitest `252/252 PASS` en 9 archivos.
- Playwright oficial hermético: `12/12 PASS` en Chromium desktop y móvil. Incluye locator inválido/desconocido/colisionado, aislamiento entre organizaciones, reload/Back/Forward/Escape, foco, Axe y viewport 390 px. PostgreSQL temporal cerró por fast-shutdown y no quedaron listeners en los puertos del runner.
- EF Identity, Territory y Productive Core: `3/3 PASS`, sin pending model changes. FND protocol: `45/45` mutations rechazadas. SEC threat model: `56/56` mutations rechazadas.
- NuGet audit: 9/9 proyectos sin vulnerabilidades conocidas. `pnpm audit --prod --audit-level high`: sin vulnerabilidades conocidas. UTF-8, secret scan y `git diff --check`: `PASS`.

### Revisión del refresh

- Revisión independiente: `PASS`, 0 Critical, 0 High, 0 Medium y 0 Low abiertos.
- Antes del cierre se corrigieron la pérdida de foco en popstate, el reemplazo prematuro del éxito de creación, una carrera de detail A→B con el mismo prefijo y UUID completos usados en atributos DOM.
- El cambio es frontend-only: no modifica backend, OpenAPI, DB, grants/RLS, eventos, telemetría ni retención. No hubo deploy.
- Publicación funcional: `6956c43` (`feat(frontend): add field detail deep links`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`.
