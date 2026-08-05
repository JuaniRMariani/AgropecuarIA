# AGRO-DIS-003 — evidencia reproducible

Spike R0 aislado para reducir riesgo antes de implementar identidad productiva. Demuestra contratos, estados de UI, linking/recovery seguros a nivel de modelo, autorización por permiso y aislamiento PostgreSQL RLS. No es el bootstrap de R1 y no autoriza un despliegue.

## Contenido

- `contracts/`: schemas de contexto efectivo, linking y eventos.
- `spike/api`: Minimal API .NET 10 con fixtures solo en Debug + ambiente `Spike` + bind loopback.
- `spike/api.tests`: recorridos HTTP, CSRF, step-up, recovery, BOLA y pool Npgsql real.
- `spike/database`: modelo, RLS, fixtures y probes PostgreSQL 17.
- `spike/web`: prototipo Next.js 16/React 19 accesible y sin almacenamiento de tokens.
- `idp-decision-matrix.md`: shortlist y `GO CONDICIONAL` para sandbox Auth0.
- `AGRO-DIS-003-threat-model.md`: amenazas, controles y gaps.

## Ejecución

Desde `spike`:

```powershell
.\scripts\start-ephemeral-postgres.ps1
$db = Get-Content .runtime\postgres.env.json | ConvertFrom-Json
$env:ConnectionStrings__IdentitySpike = $db.appConnectionString
$env:IdentitySpike__JobConnectionString = $db.jobConnectionString
dotnet restore .\AgropecuarIA.IdentitySpike.slnx
dotnet build .\AgropecuarIA.IdentitySpike.slnx --no-restore
dotnet test --solution .\AgropecuarIA.IdentitySpike.slnx --no-build
dotnet format .\AgropecuarIA.IdentitySpike.slnx --verify-no-changes --no-restore
.\scripts\stop-ephemeral-postgres.ps1
```

Desde `spike/web`, ejecutar los comandos del README local. Los fixtures API no se mapean en Release; en Debug requieren ambiente `Spike` y configuración de URL loopback. PostgreSQL usa `trust` únicamente en el clúster efímero que escucha `127.0.0.1`.

## Compatibilidad y rollback

Las migraciones son destructibles junto con el clúster efímero y no se aplican a una base existente. El rollback del spike es detener y eliminar `.runtime`; el código productivo futuro debe crear migraciones forward-safe nuevas. Stores de sesión, challenges y linking están deliberadamente en memoria y no se migran.

## Límites para R1

Persisten como gates: sandbox IdP real, identidad externa one-to-many persistida, discovery seguro de membresías, callback OIDC/PKCE, headers/caché productivos, failover, DPA/región/plan/SLA/exportabilidad y aprobación final AppSec/Architecture.
