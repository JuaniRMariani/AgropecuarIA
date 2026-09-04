# AgropecuarIA

Plataforma SaaS integral para productores, ingenieros agrónomos y equipos rurales de Argentina. El primer usuario de referencia es un ingeniero agrónomo que también trabaja como productor.

## Estado

El repositorio contiene la especificación, spikes R0 aislados bajo `tasks/evidence/` y el bootstrap productivo iniciado por `AGRO-ID-001`: API ASP.NET Core/.NET 10, módulo de identidad, PostgreSQL y aplicación Next.js/React con pnpm. Los spikes continúan siendo descartables y no se reutilizan como runtime del producto.

## Aplicación productiva

- Solución: `AgropecuarIA.slnx`.
- API: `apps/AgropecuarIA.Api`.
- Módulos actuales: Identity, Territory, ProductiveCore, Catalog y Weather bajo `src/`.
- Web: `apps/web` (Next.js App Router, React y TypeScript estricto).
- Contratos HTTP: los cinco OpenAPI bajo `contracts/`.
- Operación local y configuración Auth0: `docs/runbooks/identity-local-and-test.md`.
- Gates de CI y pruebas locales con PostGIS: [guía de verificación](docs/implementation/ci-quality-gates.md).
- Publicación, procedencia y lector autenticado de catálogo: [alcance técnico y recuperación](docs/implementation/catalog-publication-reader.md).
- Referencias de catálogo conservadas en ciclos: [contrato Productive 2.0, historia y límites](docs/implementation/production-cycle-catalog-reference.md).

Los secretos reales se cargan únicamente al publicar un ambiente compartido. El proveedor sintético está limitado a `Development`/`Test`; la aplicación falla cerrada fuera de esos ambientes si OIDC no está configurado.

Las decisiones marcadas como **pendientes** requieren validación del sponsor, usuarios de campo, contador y, según el alcance, asesores legales o regulatorios.

## Evidencia implementada

- `AGRO-DIS-001`: catálogo nacional, gobierno, contrato y prototipo de estados de soporte.
- `AGRO-DIS-003`: identidad, account linking, recovery, sesiones, tenant/RLS, frontend accesible y pruebas integradas.
- `AGRO-DIS-004`: contratos GIS/clima, PostGIS real efímero, parsers Open-Meteo/CAP/WRF y mapa accesible con degradación.
- `AGRO-DIS-005`: contratos de archivos, cuarentena fail-closed, restore PostgreSQL/PostGIS+objetos y prototipo accesible de estados.
- `AGRO-DIS-007`: escenarios sintéticos de capacidad, SLI/SLO, modelo FinOps fail-closed, política de telemetría y laboratorio de conectividad online.
- `AGRO-FND-001`: límites de 15 módulos, ownership de `ManagementUnit`/GIS, mapa de consumidores y fitness tests N/N-1.

Los comandos reproducibles y límites de cada spike se documentan en su propio `README.md` dentro de `tasks/evidence/<ID>/`.

## Lectura recomendada

1. [Visión y alcance](docs/01-vision-y-alcance.md)
2. [Dominio, actores y flujos](docs/02-dominio-actores-y-flujos.md)
3. [Requisitos funcionales](docs/03-requisitos-funcionales.md)
4. [Reglas de negocio y modelo de datos](docs/04-reglas-y-modelo-de-datos.md)
5. [Arquitectura propuesta](docs/05-arquitectura.md)
6. [Integraciones y normativa](docs/06-integraciones-y-normativa.md)
7. [Seguridad y privacidad](docs/07-seguridad-y-privacidad.md)
8. [Estrategia de inteligencia artificial](docs/08-estrategia-ia.md)
9. [Requisitos no funcionales y QA](docs/09-calidad-y-pruebas.md)
10. [MVP, backlog y roadmap](docs/10-mvp-roadmap.md)
11. [Preguntas para el sponsor](docs/11-preguntas-discovery.md)
12. [Fuentes consultadas](docs/12-fuentes.md)
13. [Clima y rotación ganadera](docs/13-clima-y-rotacion-ganadera.md)
14. [Catálogo productivo argentino](docs/14-catalogo-productivo-argentino.md)

## Principios rectores

- La trazabilidad se construye con eventos fechados, no sobrescribiendo el pasado.
- El sistema debe funcionar con conectividad rural inestable.
- La IA asiste y explica; no ejecuta decisiones productivas, sanitarias, fiscales o financieras críticas por sí sola.
- Los datos de cada organización están aislados y no se reutilizan para entrenar modelos compartidos sin consentimiento explícito.
- La contabilidad de gestión, la valuación estimada y la documentación fiscal son conceptos distintos.
- Un portal estatal visible no equivale a una API pública: cada integración oficial requiere factibilidad técnica y autorización.
- El pronóstico meteorológico es contexto probabilístico; no sustituye un pluviómetro, estación ni observación del lote.
- La rotación ganadera se recomienda desde oferta/demanda forrajera y restricciones reales, no solo por ubicación o calendario.
- Toda producción argentina puede registrarse mediante el núcleo común; la interfaz distingue catálogo, flujo genérico y especialización técnica validada.

## Convenciones

- Requisitos funcionales: `RF-<módulo>-NNN`.
- Reglas de negocio: `RN-<módulo>-NNN`.
- Requisitos no funcionales: `RNF-<área>-NNN`.
- Preguntas pendientes: `Q-NNN`.
- Prioridad: `Must`, `Should`, `Could`, `Won't now`.
- Las fechas efectivas, moneda, unidad y origen del dato se conservan siempre.

## Próximo hito

El [inventario verificable de las 81 tareas](docs/implementation/current-backlog-evidence-2026-09-04.md) separa implementación, evidencia y aprobaciones pendientes. La [reparación del runtime](docs/implementation/runtime-safety-repair-2026-09-04.md) documenta configuración, migraciones, permisos y límites de la entrega actual; no equivale a una certificación de producción.

Publicar y revisar `Catálogo Nacional v1` con referentes agrícolas y pecuarios. Luego realizar el primer taller con el ingeniero agrónomo/productor usando un campo real anonimizado para elegir qué perfiles se profundizan. Mediciones de forraje y lluvia se relevan si existen; el formato que recibe el contador continúa pendiente. Validar después con un segundo productor para no diseñar solo para un caso.
