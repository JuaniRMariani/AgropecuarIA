# Equipos, ownership y coordinación

Este documento asigna responsabilidades de implementación; no reemplaza los owners específicos de cada tarea. Una persona puede cubrir más de un rol solo si conserva revisión independiente en seguridad, QA y validación profesional.

## Modelo operativo

- Equipos orientados a capacidades/vertical slices, con arquitectura, QA, seguridad, UX y plataforma embebidos.
- Ownership de módulo implica contrato, datos, migraciones, telemetría, documentación y guardia; no autoriza consultar tablas de otro módulo.
- Product Manager integra alcance; Principal Architect integra límites/contratos; QA/Test Architect conserva autoridad del gate; especialistas validan reglas, no implementaciones.
- Cambios de contrato se coordinan mediante revisión de consumidores y compatibilidad N/N-1.
- Los documentos de `tasks/` son planificación compartida; el código futuro debe definir CODEOWNERS/ownership equivalente sin contradecir esta matriz.

## Workstreams

| WS | Equipo / rol owner | Ámbito | Colaboradores obligatorios | Entregables principales |
|---|---|---|---|---|
| WS-00 | Product/Delivery Lead | Backlog, outcomes, releases, riesgos, decisiones y aceptación. | Sponsor, todos los leads | EPIC-00/17, roadmap, gates, métricas y dependencias. |
| WS-01 | Principal Architecture | Bounded contexts, contratos, consistencia, ADR y fitness. | Backend, Data, AppSec, Platform | EPIC-01; ADR de módulos/RLS/transacciones/versionado. |
| WS-02 | Identity/Tenancy Team | Identidad, organización, membresía, alcance, sesión y factores. | AppSec, Frontend, QA | EPIC-02; authn/authz y onboarding. |
| WS-03 | Catalog/Productive Core Team | Fuentes, catálogo, perfiles, unidades/ciclos/eventos comunes. | Domain experts, Data, Frontend, QA | EPIC-03; baseline, gobierno y suite paramétrica. |
| WS-04 | GIS/Geospatial Team | Territory, Georef, geometrías, área, versiones, mapa/tiles. | Frontend, Data, Weather, QA | EPIC-04; 24 jurisdicciones e historia espacial. |
| WS-05 | Weather/Agroclimate Team | Open-Meteo adapter, snapshots, cache, CAP, lluvia local, WRF spike. | GIS, SRE, Product, agrónomo | EPIC-05; contratos/degradación/skill. |
| WS-06 | Operations/Agriculture Team | Campañas, órdenes/partes, labores, monitoreo, cosecha. | Inventory, Finance, Domain, QA | EPIC-06; recorridos R2/R3. |
| WS-07 | Livestock Team | Modos de seguimiento, identificadores, existencias, ubicaciones y eventos. | Veterinario, GIS, Inventory, QA | EPIC-07; trazabilidad pecuaria común. |
| WS-08 | Grazing/Forage Team | Potreros, mediciones, perfiles, motor, reservas y decisión humana. | Agrónomo, veterinario, Weather, AI | EPIC-08; evidencia/abstención. |
| WS-09 | Inventory/Assets Team | Ítems, depósitos, partidas, movimientos, reservas, activos/valuaciones. | Operations, Finance, QA | EPIC-09; kernel R2 y expansión R3–R5. |
| WS-10 | Commerce/Finance Team | Operaciones, tesorería, imputación, cierre, KPI y paquete canónico. | Contador, Inventory, Documents, QA | EPIC-10; exporte sin fiscalidad inventada. |
| WS-11 | Documents/Audit Team | Archivos, hash, cuarentena, vínculos, timeline y auditoría. | AppSec, Platform, todos módulos | EPIC-11; evidencia/retención/exporte. |
| WS-12 | Analytics/AI Team | Proyecciones, KPI, alertas, gateway, RAG, evals y feedback. | AppSec, Domain, QA, SRE | EPIC-12; IA read-only y kill switch. |
| WS-13 | Frontend/Product Design | Shell, design system, formularios/tablas/mapas, responsive/PWA/a11y. | Todos los equipos de dominio, QA | EPIC-13; experiencia consistente y UUID corto. |
| WS-14 | Integrations/Data Exchange | Ports/adapters, imports, inbox, territory/catalog sources y portabilidad. | Data, AppSec, SRE, módulos dueños | EPIC-14; contratos/fixtures/conciliación. |
| WS-15 | AppSec/Privacy | Threat model, auth review, RLS/BOLA, SSRF/files/IA, privacidad/supply chain. | Legal, todos los equipos | EPIC-15; gates de seguridad/privacidad. |
| WS-16 | Platform/SRE | Entornos, CI/CD, secretos, OTel, SLO, restore, runbooks y costos. | Data, AppSec, QA | EPIC-16; operación/release/DR. |
| WS-17 | QA/Test Architecture | Estrategia, fixtures, automatización, NFR, evidencia y release readiness. | Todos los equipos | EPIC-17 y matriz de trazabilidad. |
| WS-18 | Panel profesional | Aprobación agronómica, veterinaria y contable. | Product, Domain, QA, Legal | Perfiles, oráculos, casos de abstención y actas. |

## Ownership de módulos y contratos

| Módulo/dato | Owner | Consumidores por contrato | Prohibición clave |
|---|---|---|---|
| Identity/Tenancy | WS-02 | Todos | Ningún consumidor replica permisos ni confía en tenant del cliente. |
| Territory/GIS | WS-04 | Core, Weather, Agriculture, Livestock, Grazing | Sin escritura/lectura directa de tablas espaciales ajenas. |
| National Catalog | WS-03 | Core y especializaciones | Extensión tenant no altera baseline global. |
| Productive Core | WS-03 | Agriculture, Livestock, Analytics | No fuerza apiarios/estanques/galpones al modelo lote/rodeo. |
| Operations | WS-06 | Analytics, Notifications | Confirma stock/costo por contratos públicos, no tablas. |
| Agriculture | WS-06 | Analytics/Finance | No publica reglas no aprobadas como universales. |
| Livestock | WS-07 | Grazing/Analytics | No expone contadores editables como stock fuente. |
| Grazing/Forage | WS-08 | AI/Analytics | Aceptar recomendación no ejecuta movimiento. |
| Weather | WS-05 | Operations/Grazing/AI | Snapshots son inmutables; no altera datos del proveedor. |
| Inventory/Assets | WS-09 | Operations/Agriculture/Livestock/Finance | Saldo deriva de movimientos; destino productivo es referencia contractual. |
| Commerce/Finance | WS-10 | Analytics/Export | No mezcla documento fiscal futuro con operación/tesorería. |
| Documents | WS-11 | Todos | Autoriza con el módulo dueño del recurso; no deduplica contenido entre tenants. |
| Audit | WS-11 | Product/AppSec/Support autorizado | Append-only; separado de logs técnicos. |
| Analytics/AI | WS-12 | UI | No se convierte en fuente transaccional ni tool de mutación. |
| Integrations | WS-14 | Módulos por puerto | No contiene reglas de negocio ni escribe tablas publicadas directamente. |

## Ownership de rutas futuras

La estructura real se decidirá al implementar slices. La intención de ownership es:

| Área futura | Owner primario | Revisión requerida |
|---|---|---|
| `apps/web` o equivalente | WS-13 | Equipo del dominio del slice + QA/AppSec. |
| `apps/api` composition/host | WS-01 | WS-16/15. |
| `apps/worker` composition/host | WS-16 | WS-01/14/15. |
| `modules/identity` | WS-02 | AppSec/QA. |
| `modules/gis` | WS-04 | Data/QA. |
| `modules/catalog` y núcleo | WS-03 | Profesionales/QA. |
| `modules/operations/agriculture` | WS-06 | Inventory/Finance/QA. |
| `modules/livestock/grazing` | WS-07/08 con archivos disjuntos | Profesionales/QA. |
| `modules/inventory/assets` | WS-09 | Finance/QA. |
| `modules/finance` | WS-10 | Contador/QA. |
| `modules/weather` | WS-05 | GIS/SRE/QA. |
| `modules/documents/audit` | WS-11 | AppSec/Privacy. |
| `modules/analytics/ai` | WS-12 | AppSec/Domain/QA. |
| `contracts` | WS-01, con owner del contrato | Todos los consumidores. |
| `deploy/observability` o equivalente | WS-16 | AppSec/QA. |
| `tests` por suite | WS-17 define arquitectura; equipo del slice mantiene | QA aprueba evidencia. |

No se crean paquetes vacíos ni un directorio por aspiración; cada área nace con su primer slice.

## Dependencias entre equipos

| Proveedor | Consumidor | Contrato/artefacto de coordinación | Gate |
|---|---|---|---|
| WS-02 | Todos | Effective actor/tenant/permissions | Suite tenant negativa. |
| WS-03 | WS-06/07/08/12 | `CatalogEntryRef`, `ProfileVersionRef`, support matrix | Perfil compatible/aprobado. |
| WS-04 | WS-05/06/07/08 | `SpatialReferenceVersion`, área/punto representativo | Historia/precisión aprobadas. |
| WS-05 | WS-06/08/12 | `WeatherSnapshotRef`, CAP/frescura | Contrato/fixtures y degradación. |
| WS-09 | WS-06/07/10 | Availability/reservation/movement | Idempotencia/concurrencia. |
| WS-10 | WS-06/09/12 | Allocation/period/export contract | Política contador y conciliación. |
| WS-11 | Todos | Document link/audit event | Reautorización y retención. |
| WS-14 | Módulos | Inbound/outbound adapter + inbox status | Schema, fixtures y fallback. |
| WS-16 | Todos | Entorno, telemetría, flags, restore | SLO/rollback/runbook. |
| WS-17 | Todos | Test IDs, fixtures, evidencia | Release readiness. |
| WS-18 | WS-03/06/07/08/10/12 | Perfil/oráculo/aceptación nominada | Sin aprobación no hay especialización. |

## Paralelismo seguro

- R0: IdP, contrato espacial, proveedor clima, storage, RLS, baseline y contrato contable corren en paralelo con owners distintos.
- R1: Identity, Catalog, Platform y Frontend shell avanzan en paralelo después de acordar tenant/audit/error contracts.
- R2: GIS, Weather por punto, Operations y kernel Inventory/Cost avanzan en paralelo; CAP espera geometría para intersección.
- R3/R4: Agriculture y Livestock común avanzan en paralelo; Grazing espera contratos estables de Livestock/GIS/Weather.
- R5: Finance canónico y portabilidad avanzan mientras Analytics construye proyecciones; adaptador específico espera muestra.
- R6: casos IA de clima y rotación se evalúan independientemente con flags separados.

## Zonas de exclusión para evitar colisiones

- Un único owner modifica cada contrato público durante un slice; consumidores revisan, no editan en paralelo.
- Una migración por módulo tiene owner único y revisión Data/Architecture.
- Design system es propiedad WS-13; equipos de dominio aportan requisitos, no crean variantes paralelas.
- Fixtures transversales son propiedad WS-17; cada módulo aporta builders/datos de su dominio en archivos disjuntos.
- SLO/dashboards/runbooks son propiedad WS-16 con señales aportadas por cada módulo.
- Políticas RLS/autorización y privacidad requieren aprobación WS-15 aunque el módulo implemente la regla.

## RACI de decisiones sensibles

| Decisión | A | R | C | I |
|---|---|---|---|---|
| Alcance/release | Sponsor | WS-00 | Leads/WS-17/18 | Todos |
| Límite/ADR | Principal Architect | WS-01 | Owners/AppSec/SRE | Sponsor/Product |
| Catálogo/perfil | Sponsor/Product | WS-03 | WS-18/QA | Todos |
| Regla agronómica | Agrónomo nominado | WS-06/08 | QA/Product | Sponsor |
| Regla veterinaria/sanitaria | Veterinario nominado | WS-07/08 | QA/Product | Sponsor |
| Política gestión/contable | Contador nominado | WS-10 | Product/QA | Sponsor |
| Riesgo de seguridad/privacidad | Security/Privacy Lead | WS-15 | Legal/Architecture/SRE | Sponsor |
| Release readiness | Product + QA + SRE | WS-17/16 | AppSec/owners/profesionales | Sponsor |
| Go-live/aceptación residual | Sponsor | WS-00 | QA/SRE/AppSec/Domain | Todos |

## Cadencias

- Refinamiento de slices: semanal o por wave, con DoR y dependencias.
- Revisión de contratos/ADR: antes de iniciar cualquier consumidor nuevo.
- Comité de catálogo/perfiles: por publicación y cadencia a resolver en Q-062/Q-063.
- Riesgo/seguridad: por release y ante nuevo proveedor/frontera.
- Quality review: continua; gate formal por release.
- Operación/SLO/costos: semanal durante piloto, luego según error budget.
- Feedback profesional/IA: por dataset/modelo/perfil versionado.

## Escalamiento de bloqueos

Un equipo continúa con contrato/fixtures/fallback cuando falta un proveedor, pero no declara integración productiva. Una regla sin especialista queda en flujo genérico/abstención. Una decisión de sponsor que cambia alcance se registra en [`decisions-and-gaps.md`](decisions-and-gaps.md), replanifica dependencias y actualiza la trazabilidad antes de implementar.
