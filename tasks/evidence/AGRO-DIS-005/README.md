# AGRO-DIS-005 — evidencia de storage y recuperación

Spike R0 aislado y descartable. Demuestra contratos, ciclo de cuarentena fail-closed, aislamiento tenant, integridad y restore conjunto; no provisiona cloud, no usa credenciales reales y no constituye la implementación R1 de documentos.

## Resultado de decisión

- **GO técnico condicionado** para probar AWS S3 + GuardDuty en sandbox detrás de ports.
- Azure Blob + Defender queda como alternativa; un proveedor S3-compatible debe demostrar feature parity.
- **NO-GO productivo** hasta cerrar región, DPA, subencargados, residencia, retención/legal hold, volumen/costo, KMS/backup separado, AV real y `VAL-LEG`.
- Q-058 no autoriza object storage internacional; su trazabilidad original a IA/clima queda registrada como gap.

## Artefactos

- `contracts/`: schemas JSON 2020-12 para archivo, upload, scan, download y manifest.
- `spike/src` + `spike/tests`: workflow .NET 10, ports, storage local aislado y pruebas de seguridad.
- `spike/web`: prototipo Next.js/React accesible, exclusivamente sintético y administrado con pnpm.
- `spike/postgres`: drill PostgreSQL 17/PostGIS 3.6.2 con dump/restore, objetos, hashes, geometría y auditoría.
- `provider-matrix.md`, `threat-model.md` y `runbook.md`: decisión, amenazas y operación inicial.
- `docs/adr/ADR-007-storage-retencion-recuperacion.md`: decisión arquitectónica canónica.

## Comandos reproducibles

Desde `tasks/evidence/AGRO-DIS-005/spike`:

```powershell
dotnet restore AgropecuarIA.StorageRecoverySpike.slnx --locked-mode
dotnet build AgropecuarIA.StorageRecoverySpike.slnx --no-restore
dotnet format AgropecuarIA.StorageRecoverySpike.slnx --no-restore --verify-no-changes
dotnet test --solution AgropecuarIA.StorageRecoverySpike.slnx --no-build
dotnet list AgropecuarIA.StorageRecoverySpike.slnx package --vulnerable --include-transitive
& '.\postgres\run-restore-drill.ps1' -Port 55435
```

Desde `spike/web`:

```powershell
pnpm install --frozen-lockfile --ignore-scripts
pnpm run validate:contracts
pnpm run format:check
pnpm run lint
pnpm run typecheck
pnpm test
pnpm run build
pnpm run test:e2e
pnpm audit --audit-level high
```

El drill usa `trust` únicamente en un clúster efímero atado a loopback y valida teardown/puerto. Reutiliza la herramienta de bootstrap PostGIS fijada por AGRO-DIS-004 cuando el runtime no está presente; no modifica el estado ni los artefactos fuente de esa tarea.

## Límites honestos

- El scanner es sintético y no contiene la firma EICAR real; valida el contrato y los fallos, no eficacia antivirus.
- El storage local valida aislamiento lógico, claves, inmutabilidad, hash y reconciliación; no valida políticas IAM/KMS del proveedor.
- El manifest y el drill ligan `tenant_id` con su referencia opaca y conservan tipo+ID del recurso; una purga de resultado ambiguo queda `PurgeUncertain` hasta reconciliación privilegiada.
- `pg_dump`/`pg_restore` demuestra reconstrucción e integridad. La ventana de captura local no demuestra PITR/RPO administrado; ese gate queda abierto.
- Los targets RPO 15 min/RTO 2 h son hipótesis de discovery. No hay SLA, volumen representativo ni retención contractual aprobados.
