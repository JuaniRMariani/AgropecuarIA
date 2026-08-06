# Política de contratos R0/R1

## Fronteras

El módulo proveedor es owner del contrato, regla, autorización y persistencia. El consumidor envía referencias opacas; no conoce schemas, tablas, ORM ni migraciones del proveedor. `RequestScope` es contexto interno creado después de autenticar, no un DTO aceptado desde el navegador.

Los datos se clasifican explícitamente:

- `platform`: usuario base, directorio de organizaciones, baseline nacional y territorio oficial publicado;
- `tenant`: membresía, extensión privada, unidad/ciclo/evento productivo, geometría operativa, inventario, finanzas, documentos y recomendaciones;
- `mixed-explicit`: módulo que contiene ambos planos en agregados/tablas y operaciones separados; nunca un registro con scope ambiguo.

CUIT es dato fiscal/personal y no aparece en scope, cache key, métricas o autorización. `CorrelationId` tampoco concede autoridad.

El directorio global que asigna un identificador opaco a `Organization` es platform-scoped y solo admite capacidades de bootstrap acotadas. La configuración y los recursos de esa organización son tenant-scoped; ser owner de una organización nunca concede autoridad platform. Esta distinción no resuelve ni presupone cardinalidad Organization↔CUIT.

Cada módulo puede conservar un journal técnico append-only con hechos de seguridad propios para sostener la transacción local y la investigación de fallos. Ese journal permanece en el schema del módulo, no reemplaza `PlatformAuditEvent`/`TenantAuditEvent` ni permite consultar persistencia de Audit/Compliance. La proyección central futura cruza exclusivamente el puerto `AuditEventWriter`.

## HTTP

- `401`: no existe una sesión autenticada válida.
- `403`: el actor autenticado carece de una capacidad no enumerante o del step-up requerido.
- `404`: recurso inexistente, ajeno o no autorizado; mismo tipo/código y sin ID, tenant, coordenada ni diff.
- `409`: conflicto de dominio o idempotencia que no depende de una versión HTTP.
- `412`: ETag fuerte de `If-Match` no coincide, evaluado solo después de autenticar y reautorizar el recurso.

Los errores usan `application/problem+json` y un `code` estable. `detail`, stack traces, SQL, payload, foreign IDs y datos sensibles no forman parte del contrato público. Colecciones usan cursor opaco, `hasMore` y máximo 200; el cursor se valida y liga al filtro/scope en el productor.

## Compatibilidad N/N-1

Requests se validan estrictamente. Readers de responses/eventos ignoran campos desconocidos. Dentro de una major solo se permite agregar campos opcionales o valores de enum cuando el contrato los declara extensibles. Se rechaza eliminar/renombrar, cambiar tipo o semántica, convertir opcional en requerido, reutilizar un campo o cerrar un enum abierto.

Cada cambio revisa el mapa de consumidores y declara rango soportado. Primero se expande el schema, luego se ejecuta un backfill reanudable, después cambian writers/readers y el contract destructivo ocurre cuando N-1 dejó la ventana. La aplicación puede volver a N-1; los datos avanzan por roll-forward y nunca se borran hechos para simular rollback.

## Eventos

El envelope lleva `eventId`, tipo, versión, fuente, scope discriminado, tiempos `occurred/effective/recorded`, correlación/causación, agregado y versión monotónica. Un duplicado es no-op idempotente. Una versión anterior/repetida o un gap se cuarentena/ignora de forma observable sin mutación parcial. Un tipo/major desconocido no se reintenta indefinidamente.

Todo contrato implementado se registra en `runtime-map.json` junto con su proyecto owner y versión. El build raíz ejecuta el fitness contra proyectos, modelo EF y OpenAPI reales; los fixtures R0 ya no son una aprobación suficiente por sí solos.

## Extracción futura

Extraer un servicio exige evidencia de escala, aislamiento, resiliencia, despliegue u ownership. Antes de extraer: cero consultas/FK ajenas, contrato HTTP/evento versionado, outbox/inbox e idempotencia cuando correspondan, SLO y threat model propios, consumidor N-1 probado y plan de backfill/roll-forward. La extracción no cambia ownership ni permite transacciones distribuidas encubiertas.
