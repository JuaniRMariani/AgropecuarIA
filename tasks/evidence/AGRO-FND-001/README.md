# AGRO-FND-001 — límites modulares y contratos compatibles

Evidencia ejecutable R0 que ratifica 15 bounded contexts, ownership de datos, dependencias permitidas y la política N/N-1. No es código productivo, no contiene API/UI ni promueve los spikes `AGRO-DIS-003/004`.

## Decisiones centrales

- `National Catalog` y `Productive Core` son módulos distintos bajo WS-03.
- Productive Core posee identidad y ciclo de vida de `ManagementUnit`; Territory posee `SpatialRepresentationVersion`. La geometría es opcional y no existe FK ni consulta entre schemas.
- `Organization` es tenant. CUIT es una futura referencia legal y nunca una clave de partición/autorización.
- Todo scope es discriminado como `platform` o `tenant`; `tenantId` se deriva del contexto autenticado.
- Consumidores dependen de contratos públicos, nunca de tablas. El grafo usa dirección consumidor → proveedor.
- Cambios compatibles son aditivos y tolerados por N-1. Remover/cambiar tipo, volver un campo required o cerrar un enum es breaking.
- Eventos duplicados o fuera de orden no cambian estado. El módulo dueño reautoriza antes de evaluar `If-Match`.

## Verificación

Desde `fitness`:

```powershell
dotnet restore AgropecuarIA.ArchitectureFitness.slnx --locked-mode
dotnet build AgropecuarIA.ArchitectureFitness.slnx --no-restore
dotnet test --solution AgropecuarIA.ArchitectureFitness.slnx --no-build --minimum-expected-tests 42
dotnet format AgropecuarIA.ArchitectureFitness.slnx --verify-no-changes --no-restore
```

El ensayo real `expand → backfill → contract` sobre staging, backup/restore y roll-forward pertenece a `AGRO-FND-003`/`AGRO-PLT-004`; no se simula aquí.

Resultados completos y revisión independiente: [`validation-report.md`](validation-report.md).
