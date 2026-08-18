# Validación — OwnerWorkspaceShellV1

Estado: `PASS` para `OwnerWorkspaceShellV1` integrado-local. `AGRO-FE-001` permanece `En curso` por el alcance residual del padre.

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

## Riesgos residuales

- El padre conserva roles/contextos adicionales, preferencias, matriz completa de navegadores y certificación manual.
- El backend sigue siendo la única autoridad; el shell no evita por sí solo BOLA ni sustituye SEC-002.
- No se aprueba deploy, PWA/offline, telemetría compartida ni almacenamiento de datos de negocio en cliente.
