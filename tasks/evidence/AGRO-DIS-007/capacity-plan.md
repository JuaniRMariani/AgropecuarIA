# Plan de capacidad y equipo

Estado: envelope R0 sintético, confianza baja; no representa demanda observada, benchmark del producto, compromiso de fecha ni aprovisionamiento.  
Owner de escenarios: Delivery/SRE/Product. Aprobación pendiente: Sponsor.  
Válido hasta: 2026-09-30 o hasta que cambie un driver, lo que ocurra primero.

## Escenarios versionados

El fixture `capacity-scenarios.json` define tres puntos de sensibilidad. `growth-10x` multiplica el piloto; `burst-2x` aplica una ráfaga adicional sobre crecimiento. El supuesto de distribución no modela estacionalidad, asimetría por tenant ni simultaneidad rural real.

| Driver | `pilot` | `growth-10x` | `burst-2x` |
|---|---:|---:|---:|
| Tenants | 10 | 100 | 100 |
| Usuarios registrados/concurrentes | 100 / 20 | 1.000 / 200 | 1.000 / 400 |
| Establecimientos/lotes | 200 / 2.000 | 2.000 / 20.000 | 2.000 / 20.000 |
| Documentos/mes | 10.000 | 100.000 | 200.000 |
| Filas importadas/día | 10.000 | 100.000 | 200.000 |
| Jobs/hora | 10 | 100 | 200 |
| Requests lectura/escritura por día | 100.000 / 20.000 | 1.000.000 / 200.000 | 2.000.000 / 400.000 |
| Factor pico | 8× | 8× | 10× |

Supuestos comunes: documento promedio 2,5 MiB; retención 12 meses; factor de versiones 1,25; worker sintético 250 filas/s. Son entradas editables, no límites reales aprobados.

## Proyección determinística

```text
average_rps = (daily_reads + daily_writes) / 86.400
peak_rps = average_rps × peak_factor
retained_object_gib = documents_per_month × average_mib × retention_months × version_factor / 1.024
daily_import_drain_seconds = import_rows_per_day / worker_rows_per_second
```

| Resultado | `pilot` | `growth-10x` | `burst-2x` |
|---|---:|---:|---:|
| Requests/s promedio | 1,39 | 13,89 | 27,78 |
| Requests/s pico sintético | 11,11 | 111,11 | 277,78 |
| Storage objeto retenido | 366,21 GiB | 3.662,11 GiB | 7.324,22 GiB |
| Drain diario de imports | 40 s | 400 s | 800 s |

El drain es aritmética sobre un throughput supuesto. No incluye cola, parseo, validación, transacción, índices, auditoría ni I/O y no demuestra el objetivo p95 de una importación de 1.000 filas. Los RPS tampoco prueban latencia, concurrencia de DB o capacidad de una instancia: se usarán como input para un ensayo posterior del monolito real.

## Criterios de entrada y salida por escenario

| Escenario | Uso autorizado | Evidencia mínima de salida |
|---|---|---|
| `pilot` | diseño inicial y staging, sin compromiso de producción | perfil del piloto firmado; carga con DB/PostGIS y storage reales; targets API/import/mapa/clima medidos; restore y costo completos |
| `growth-10x` | prueba de headroom y sensibilidad | p95/p75 y saturation estables, aislamiento tenant, jobs sin backlog no acotado, costo marginal y rollback ensayados |
| `burst-2x` | stress/fault test controlado | protección de cuota/backpressure, idempotencia, degradación visible, recuperación automática y sin pérdida/auditoría rota |

Si falla un target, no se divide el sistema por defecto: primero se mide query/índice, pool, cache, payload, cola y dependencia. Extraer un servicio requiere una restricción demostrada y un owner operativo.

## Envelope de equipo sin fecha falsa

Un **carril** es un único slice cohesivo en curso con ownership exclusivo; no equivale automáticamente a una persona ni a un sprint. Los roles mínimos son:

- Sponsor/Product/Domain: outcome, criterios, datos y decisiones de alcance;
- Architecture/Backend/Data: contrato, invariantes, API, persistencia y migración;
- Frontend/UX: flujo online, accesibilidad y estados degradados;
- QA/AppSec: estrategia de prueba, tenant/authz, seguridad y revisión independiente;
- Platform/SRE/FinOps: entornos, telemetría, resiliencia, costo y recuperación.

| Capacidad | Organización propuesta | Restricción |
|---|---|---|
| 1 carril | todos los roles atienden un slice de punta a punta, secuencialmente | menor WIP; ningún rol/gate se elimina |
| 2 carriles | un slice funcional y un enabler estrictamente vinculado, con QA/AppSec/SRE compartidos y agenda explícita | no abrir ambos si los revisores o contratos compartidos son cuello de botella |
| 3 carriles | hasta dos slices funcionales más un carril plataforma/datos que los desbloquea | exige owners nominados, archivos/módulos disjuntos e integración continua; no es autorización para tres tareas simultáneas del backlog |

Sin headcount, dedicación, skills, vacaciones, dependencias externas y throughput histórico no se estima calendario. Delivery debe limitar WIP al número de carriles realmente cubiertos y escalar capacidad/fecha si los 94 Must exceden el equipo; no recorta aceptación para sostener una promesa.

## Evidencia contextual, no extrapolable

- `AGRO-DIS-004` observó 14.758.413 bytes en una muestra WRF y más de 1 GiB para 73 plazos estimados; WRF sigue postergado por presupuesto/operación. Ese volumen no está incluido en el storage documental de esta proyección.
- `AGRO-DIS-005` reprodujo restore local rápido sobre 2 registros/2 objetos, pero declaró `UNPROVEN_WITHOUT_MANAGED_PITR`. No permite proyectar RTO/RPO a estos escenarios.
- No existe todavía API, UI funcional, base productiva ni proveedor cloud sobre los cuales afirmar percentiles, costo o saturación.

## Revalidación y triggers

Revalidar antes del 2026-09-30 y de inmediato si ocurre cualquiera de estos cambios:

- Sponsor confirma un volumen fuera del envelope o cambia retención/SLA/soporte;
- una medición difiere materialmente de documento promedio, factor pico, throughput o perfil de red;
- se selecciona región/proveedor/topología o se incorpora WRF/IA de forma operativa;
- cambia el número real de carriles o falta un rol de gate;
- series de telemetría, storage, egress, cola o costo alcanzan 75 % del presupuesto aprobado.

El 75 % es un punto de revisión propuesto, no un límite productivo confirmado. Cada revalidación versiona fixtures, fecha, owner, fuente y decisión; no sobreescribe la evidencia anterior.

## Condición de decisión

Este plan satisface un envelope técnico reproducible para Q-019/Q-020, pero no las cierra como hechos del piloto. El estado sigue `NO-GO` para fechas o aprovisionamiento hasta que Sponsor confirme equipo/presupuesto/volumen, SRE reproduzca carga real y FinOps complete el catálogo.
