# Modelo de costo y gate FinOps

Estado: R0, catálogo de precios `incomplete`, resultado `NO-GO` para presupuesto o proveedor productivo. Ningún valor faltante se interpreta como cero.  
Owner del modelo: SRE/FinOps. Owners de aprobación: Sponsor, Delivery, Procurement/Legal y Product.  
Fecha de corte del fixture: 2026-08-05. Revalidar como máximo el 2026-09-30.

## Contrato mínimo de precio

Cada catálogo cotizable debe conservar:

- moneda ISO 4217 única (`currency`);
- proveedor/producto, región y fuente primaria verificable (`source`);
- fecha de vigencia (`asOf`) y fecha de expiración/revisión;
- impuestos incluidos o excluidos (`taxIncluded`);
- rango `low/base/high`, unidad, tiers, mínimos, descuentos y free tier explícitos;
- supuestos de FX separados y aprobados si el presupuesto se expresa en otra moneda.

No se mezclan monedas ni se usa un tipo de cambio implícito. Un catálogo vencido, sin región, sin fuente o con un driver requerido nulo produce `incomplete` y mantiene `NO-GO`.

## Drivers v1 contratados

| Driver | Cantidad mensual | Unidad de precio | Fuente de cantidad |
|---|---|---|---|
| `compute-hour` | instancias equivalentes × horas activas | instance-hour | topología aún no seleccionada |
| `storage-gib-month` | GiB promedio retenidos, incluyendo factor de versiones declarado | GiB-month | escenario de capacidad |
| `egress-gib` | GiB salientes facturables | GiB | medición por flujo/proveedor pendiente |
| `job-million-rows` | filas procesadas / 1.000.000 | million-rows | `importRowsPerDay × días` |
| `support-month` | plan de soporte por mes | month | plan/proveedor pendiente |

Para un escenario `s` y una banda de precio `b ∈ {low, base, high}`:

```text
monthly_cost(s,b) =
    compute_hours(s)        × price(compute-hour,b)
  + retained_object_gib(s)  × price(storage-gib-month,b)
  + egress_gib(s)           × price(egress-gib,b)
  + import_million_rows(s)  × price(job-million-rows,b)
  + support_months(s)       × price(support-month,b)
```

La salida solo puede ser `estimated` cuando todos los drivers y metadatos están completos, los rangos cumplen `0 ≤ low ≤ base ≤ high`, las cantidades son no negativas y la moneda es uniforme. De lo contrario devuelve lista de faltantes y no un total parcial presentado como TCO.

## Sensibilidad de demanda

Los escenarios `pilot`, `growth-10x` y `burst-2x` son tres envelopes de demanda de confianza baja; no son bandas estadísticas de precio. Cada escenario se calcula de forma independiente con las bandas del catálogo:

| Escenario | Storage objeto retenido | Filas/mes de referencia (30 d) | Drivers todavía sin cantidad |
|---|---:|---:|---|
| `pilot` | 366,21 GiB | 0,30 millones | compute, egress, soporte |
| `growth-10x` | 3.662,11 GiB | 3,00 millones | compute, egress, soporte |
| `burst-2x` | 7.324,22 GiB | 6,00 millones | compute, egress, soporte |

El storage proyectado solo incluye documentos, retención de 12 meses y factor de versiones 1,25 del fixture. No incluye base relacional, índices, WAL/PITR, backups separados, observabilidad, caches ni artefactos de jobs.

## Drivers obligatorios antes de hablar de TCO

El contrato v1 prueba la mecánica, pero una decisión de proveedor además debe cuantificar:

- PostgreSQL/PostGIS administrado: compute, storage, IOPS, WAL/PITR, backups y réplicas;
- object storage: requests, versiones, inventario, KMS, malware scan, lifecycle, backup y recuperación;
- observabilidad: logs, métricas, trazas, ingest, retención, consultas y cardinalidad;
- red: egress por servicio/región, NAT/CDN/DNS y transferencia interzona;
- IdP, correo/notificaciones, tiles/geocoder, clima, IA/LLM y soporte;
- entornos no productivos, CI/artefactos, seguridad/scans y contingencia;
- impuestos, FX, compromisos mínimos y costo operativo humano.

Un precio bundle solo puede cubrir un driver si la fuente lo declara expresamente. Free tiers y créditos se reportan aparte; no reducen el costo base estructural ni justifican lock-in.

## Presupuestos y alertas propuestos

No existe presupuesto aprobado, por lo que no se inventa un umbral monetario. Al seleccionarse proveedor, Sponsor/FinOps debe fijar por moneda y período:

- budget `warning`, `hard` y owner de excepción;
- costo por tenant/caso de uso cuando pueda atribuirse sin exponer identidad;
- variación mensual y costo marginal al pasar de `pilot` a `growth-10x`/`burst-2x`;
- acción segura: cache/cuota/sampling, degradar una integración opcional o detener rollout; nunca perder auditoría ni integridad para ahorrar.

Alertar por anomalía requiere monto absoluto y variación corroborada. No se pagina a guardia por centavos, datos atrasados o una única estimación sintética.

## Evidencia actual y decisión

`fixtures/unit-cost-catalog.incomplete.json` declara los cinco drivers con precios `null`, región pendiente y ninguna selección de proveedor. Por contrato, el cálculo productivo debe devolver `incomplete`; `null` no equivale a cero.

La evidencia previa solo aporta contexto:

- una muestra horaria oficial SMN WRF pesó 14.758.413 bytes; 73 plazos superan 1 GiB por corrida antes de productos adicionales;
- el drill local de `AGRO-DIS-005` mostró que el procedimiento puede ejecutarse en un dataset mínimo, pero no prueba PITR, costo, RPO ni volumen administrado.

Por lo tanto, la decisión vigente es **NO-GO para presupuesto, compromiso o proveedor productivo**. Es válido usar precios sintéticos exclusivamente en tests del algoritmo si la salida queda rotulada `synthetic-test-only` y nunca entra al reporte de decisión.

## Gate de aprobación

1. Sponsor confirma moneda de presupuesto, rango y horizonte; Procurement/Legal confirma impuestos/FX y fuentes.
2. Product confirma volumen piloto y retención; SRE mide cantidades faltantes con arquitectura candidata.
3. FinOps ejecuta `pilot`, `growth-10x` y `burst-2x`, documenta costo marginal y caducidad.
4. Security/Privacy confirma región, DPA, subencargados y restricciones de datos.
5. Delivery acepta costo, riesgo y acción ante exceso sin convertirlo en fecha de release falsa.

Hasta completar los cinco puntos, GAP-003 y RSK-027 permanecen abiertos.
