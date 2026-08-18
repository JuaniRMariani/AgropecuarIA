# AGRO-GIS-002 — campo borrador no espacial

Este incremento integra el primer `ManagementUnit` tenant-owned de Productive Core. Permite crear, listar y abrir la ficha de un campo borrador sin geometría.

La evidencia no cierra `AGRO-GIS-002`: mapa, PostGIS, área, tiles, establecimiento/parcela/lote/potrero, edición y catálogo permanecen fuera hasta cerrar sus decisiones y dependencias.

Artefactos:

- `product-decisions.md`: contrato de producto y límites del slice.
- `validation-report.md`: resultados reproducibles y riesgos residuales.
- `contracts/productive-core.openapi.yaml`: contrato HTTP v1.
- `tasks/evidence/AGRO-FND-001/contracts/management-unit-created.v1.schema.json`: payload del evento.
