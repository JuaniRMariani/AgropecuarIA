# AGRO-GIS-001 — decisiones del sub-slice TerritoryReference v1

Fecha: 2026-08-11. Estado: aceptado para desarrollo local; ambiente compartido pendiente.

## Alcance aprobado

- `territory` es un módulo del monolito modular, dueño exclusivo del schema PostgreSQL `territory`.
- El snapshot oficial es inmutable y provider-neutral. Los niveles v1 son `province`, `department`, `municipality` y `locality`; `gobierno_local` de Georef v2 se normaliza técnicamente como `municipality`, pero la UI lo presenta como “Gobierno local” y no afirma una categoría municipal legal.
- El seed reproducible contiene las 23 provincias y CABA del fixture público de `AGRO-DIS-004`; sus centroides son metadata oficial de jurisdicciones, no coordenadas de campos.
- La búsqueda textual usa únicamente el snapshot activo local y mantiene orden determinista, fuente, versión y fecha de captura.
- La resolución explícita por coordenada usa el endpoint oficial Georef v2.0 `ubicacion` sobre un host/base fijos, límites de tiempo/tamaño/schema y caché derivable acotada.
- El egress al proveedor está deshabilitado por defecto en todos los ambientes, incluido Development. Sólo un override explícito posterior a los gates de proveedor/Legal lo habilita; los tests del adapter usan respuestas sintéticas/literales sin depender de red.
- Si Georef no responde y no existe caché válida, el resultado es `unavailable` y la UI ofrece búsqueda manual. Nunca se elige una jurisdicción por cercanía al centroide.
- Ambas operaciones requieren una sesión autenticada, aplican rate limit y son `no-store`. No reciben organización, tenant, campo, productor, CUIT ni otro identificador de negocio.

## Privacidad y precisión

- Las coordenadas de una resolución no se guardan en PostgreSQL, journal, outbox, logs ni métricas propias del módulo.
- La caché en memoria es derivable, bounded y expira; no constituye un registro de ubicación.
- Un resultado territorial es una referencia administrativa. No prueba catastro, dominio, límites de parcela, campo ni precisión agronómica.

## Fuera de alcance

- `AGRO-GIS-002`: creación de campos, geometrías, validación espacial y mapa.
- Tiles/MapLibre productivos, catastro legal, clima, restricciones agronómicas y búsqueda global pública.
- Scheduler/import automático, credenciales de proveedor, SLA/DPA/región, deploy y retención legal productiva.

## Fuente y compatibilidad

- Documentación oficial: <https://www.argentina.gob.ar/georef/documentacion-y-recursos-georef-v21>
- Endpoint oficial usado: <https://apis.datos.gob.ar/georef/api/v2.0/ubicacion>
- Descargas oficiales para una futura carga jerárquica completa: <https://www.argentina.gob.ar/georef/descarga-de-la-base-completa>
- El fixture R0 conserva su propia versión contractual. El adapter HTTP declara `georef/2.0`; no se reetiqueta el fixture histórico ni se inventan niveles faltantes.

## Estado de la tarea

Este incremento demuestra referencia local, importación, búsqueda y degradación. `AGRO-GIS-001` permanece `En curso` hasta incorporar una fuente jerárquica completa, operación administrada de actualización y gates reales del proveedor/ambiente compartido.
