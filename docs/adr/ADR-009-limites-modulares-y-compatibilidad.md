# ADR-009 — Límites modulares y compatibilidad N/N-1

- Estado: aceptado para R0/R1
- Fecha: 2026-08-05
- Tarea: `AGRO-FND-001`

## Contexto

El monolito modular necesita ownership verificable antes de incorporar slices R1. La documentación combinaba National Catalog y Productive Core en una lista de 14 módulos, pero el plan y la matriz de equipos distinguían sus responsabilidades. También estaba pendiente dividir `ManagementUnit` de su geometría y definir compatibilidad sin convertir prototipos R0 en bootstrap.

## Decisión

Se ratifican 15 bounded contexts desplegados inicialmente como un monolito:

1. Identity/Tenancy
2. Territory/GIS
3. National Catalog
4. Productive Core
5. Operations
6. Agriculture
7. Livestock
8. Grazing/Forage
9. Inventory/Assets
10. Commerce/Finance
11. Weather/Agroclimate
12. Documents
13. Analytics/AI
14. Audit/Compliance
15. Integrations

Cada módulo posee su schema y agregados. Solo consume puertos públicos declarados; quedan prohibidos SQL, FK, `DbContext` y migraciones contra el schema de otro módulo. Integrations implementa adaptadores hacia puertos dueños del dominio y no incorpora reglas de negocio. Un servicio se extrae únicamente por escala, aislamiento, resiliencia o ownership medidos.

National Catalog posee el baseline nacional platform-scoped y las extensiones privadas explícitamente tenant-scoped. Productive Core posee `ManagementUnit`, su tipo/estado/vigencia y los ciclos/eventos/salidas comunes. Core depende del contrato publicado del catálogo; nunca de sus tablas.

Productive Core posee la identidad y el ciclo de vida de `ManagementUnit`. Territory posee `SpatialRepresentationVersion`, vigencia, geometría, área declarada/calculada y relaciones espaciales. La representación es opcional, referencia una unidad opacamente y no autoriza el recurso. Core consume el puerto de Territory después de autorizar; Territory no consulta Core. Un cambio geométrico crea versión y no cambia identidad ni borra historia.

`Organization` permanece como tenant. CUIT no selecciona contexto ni concede permisos. La cardinalidad y titularidad legal Organization↔CUIT siguen bajo Product/Legal y pueden agregarse aditivamente; esta ADR no afirma soporte multi-CUIT ni habilita ARCA.

El contexto interno es una unión discriminada `platform | tenant`. El servidor deriva tenant, actor y permisos de la sesión; correlation solo vincula diagnóstico. Operaciones platform usan permisos separados y nunca heredan privilegios tenant. Los errores de recurso ausente, ajeno o no autorizado convergen en `404`; `403` queda para capacidades no enumerantes, `409` para conflicto de dominio/idempotencia y `412` para `If-Match` fallido después de reautorizar.

HTTP usa OpenAPI 3.1.x, JSON Schema 2020-12 y Problem Details RFC 9457. Mutaciones concurrentes usan ETag fuerte/`If-Match` según RFC 9110. Las colecciones usan cursor opaco y límite máximo 200. Respuestas y eventos son tolerantes a campos aditivos; requests siguen validación estricta.

Dentro de una major, N y N-1 deben coexistir. Son breaking: eliminar/renombrar, cambiar tipo/semántica, hacer required un campo opcional o cerrar/agotar un enum. El producer registra consumidor y rango soportado. La evolución de datos sigue `expand → backfill reanudable → cambio de lectores/escritores → contract posterior`; rollback de aplicación no revierte hechos y la recuperación preferida es roll-forward. El ensayo operativo con staging/backup/restore queda como gate de `AGRO-FND-003`/`AGRO-PLT-004`.

Los eventos incluyen ID, tipo, versión, fuente, scope, tiempos, correlación/causación, agregado y versión monotónica. Duplicados se ignoran; versiones repetidas, atrasadas o con gap se cuarentenan sin mutación parcial y con telemetría acotada.

## Consecuencias

- La separación catálogo/core elimina una ambigüedad sin crear despliegues extra.
- Spatial data puede evolucionar sin contaminar lifecycle productivo y viceversa.
- Los clientes N-1 deben ignorar campos desconocidos; los schemas cerrados de los spikes no se promueven como contratos canónicos.
- El shared contract kernel se limita a IDs, scope, correlación y versionado; no contiene dominio ni persistencia.
- Cualquier nuevo edge, schema o contrato requiere actualizar registro, mapa y tests antes de integrar.

## Evidencia y fuentes

- Registro y mapa ejecutables: `tasks/evidence/AGRO-FND-001`.
- [RFC 9110 — HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110.html).
- [RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457.html).
- [OpenAPI 3.1.1](https://spec.openapis.org/oas/v3.1.1.html).
- [JSON Schema 2020-12](https://json-schema.org/draft/2020-12).

## Revisión

Revisar al incorporar un nuevo bounded context, cambiar una major de contrato, proponer extracción o detectar un edge no declarado. La aceptación de esta ADR no valida una migración productiva ni autoriza proveedor, hosting o identidad.
