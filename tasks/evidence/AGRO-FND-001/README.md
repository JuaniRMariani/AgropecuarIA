# AGRO-FND-001 — límites modulares y contratos compatibles

Evidencia ejecutable R0/R1 que ratifica 15 bounded contexts, ownership de datos, dependencias permitidas y la política N/N-1. Desde R1 el fitness está integrado en la solución raíz e inspecciona el runtime Identity real; no promueve los spikes `AGRO-DIS-003/004` ni implementa otros módulos.

## Decisiones centrales

- `National Catalog` y `Productive Core` son módulos distintos bajo WS-03.
- Productive Core posee identidad y ciclo de vida de `ManagementUnit`; Territory posee `SpatialRepresentationVersion`. La geometría es opcional y no existe FK ni consulta entre schemas.
- `Organization` es tenant. CUIT es una futura referencia legal y nunca una clave de partición/autorización.
- Todo scope es discriminado como `platform` o `tenant`; `tenantId` se deriva del contexto autenticado.
- Consumidores dependen de contratos públicos, nunca de tablas. El grafo usa dirección consumidor → proveedor.
- Cambios compatibles son aditivos y tolerados por N-1. Remover/cambiar tipo, volver un campo required o cerrar un enum es breaking.
- Eventos duplicados o fuera de orden no cambian estado. El módulo dueño reautoriza antes de evaluar `If-Match`.
- Todo evento público del runtime sale de un catálogo inmutable y tiene payload schema cerrado. El fitness compara el catálogo Identity de forma bidireccional con `consumer-map.json` y `runtime-map.json`.

## Verificación

Desde la raíz del repositorio:

```powershell
dotnet restore AgropecuarIA.slnx --locked-mode
dotnet build AgropecuarIA.slnx --configuration Release --no-restore
dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --minimum-expected-tests 114
dotnet format AgropecuarIA.slnx --verify-no-changes --no-restore
```

`runtime-map.json` registra los proyectos y composition roots productivos. La migración R1 aplica solo la fase `expand`: conserva la tabla física `identity.audit_events` para N-1, permite temporalmente escritores N-1 en el outbox y exige el envelope completo en el modelo/aplicación N. El `contract` de columnas/tablas, backfill reanudable sobre volumen y ensayo staging/backup/restore pertenecen a `AGRO-FND-003`/`AGRO-PLT-004`.

Resultados completos y revisión independiente: [`validation-report.md`](validation-report.md).
