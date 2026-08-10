# AGRO-DIS-003 — evidencia reproducible

Spike R0 aislado para reducir riesgo antes de implementar identidad productiva. Demuestra contratos, estados de UI, linking/recovery seguros a nivel de modelo, autorización por permiso, discovery de membresías actor-scoped y aislamiento PostgreSQL RLS. No es el bootstrap de R1 y no autoriza un despliegue.

## Contenido

- `contracts/`: schemas de discovery, contexto efectivo, linking y eventos.
- `spike/api`: Minimal API .NET 10 con fixtures solo en Debug + ambiente `Spike` + bind loopback.
- `spike/api.tests`: recorridos HTTP, CSRF, step-up, recovery, BOLA y pool Npgsql real.
- `spike/database`: modelo, RLS, fixtures y probes PostgreSQL 17.
- `spike/web`: prototipo Next.js 16/React 19 accesible y sin almacenamiento de tokens.
- `idp-decision-matrix.md`: shortlist y `GO CONDICIONAL` para sandbox Auth0.
- `AGRO-DIS-003-threat-model.md`: amenazas, controles y gaps.
- `membership-discovery-decision.md`: decisión reversible para resolver 0/1/N antes de seleccionar tenant.

## Ejecución

Desde `spike`:

```powershell
.\scripts\start-ephemeral-postgres.ps1
$db = Get-Content .runtime\postgres.env.json | ConvertFrom-Json
$env:ConnectionStrings__IdentitySpike = $db.appConnectionString
$env:IdentitySpike__JobConnectionString = $db.jobConnectionString
$env:IdentitySpike__DiscoveryConnectionString = $db.discoveryConnectionString
$env:IdentitySpike__OwnerConnectionString = $db.ownerConnectionString
dotnet restore .\AgropecuarIA.IdentitySpike.slnx
dotnet build .\AgropecuarIA.IdentitySpike.slnx --configuration Debug --no-restore
dotnet test --solution .\AgropecuarIA.IdentitySpike.slnx --configuration Debug --no-build --minimum-expected-tests 29
dotnet format .\AgropecuarIA.IdentitySpike.slnx --verify-no-changes --no-restore
.\scripts\stop-ephemeral-postgres.ps1
```

Desde `spike/web`, ejecutar los comandos del README local. Los fixtures API no se mapean en Release; en Debug requieren ambiente `Spike` y configuración de URL loopback. PostgreSQL escucha solo `127.0.0.1`, usa SCRAM-SHA-256 para conexiones locales/TCP y genera credenciales distintas por principal en cada ejecución. El archivo efímero de conexiones queda limitado por ACL al usuario actual y el script de cleanup lo elimina.

## Compatibilidad y rollback

Las migraciones son destructibles junto con el clúster efímero y no se aplican a una base existente. El rollback del spike es detener y eliminar `.runtime`; el código productivo futuro debe crear migraciones forward-safe nuevas. Stores de sesión, challenges y linking están deliberadamente en memoria y no se migran.

## Discovery antes del tenant

El servidor deriva `app.current_actor_id` de la sesión, lo establece con alcance transaccional sobre una conexión exclusiva y consulta como `agro_membership_discovery`. Antes de servir, valida fail-fast `current_user`/`session_user`, atributos, memberships y ownership del principal. Ese rol es read-only, `NOINHERIT`, `NOBYPASSRLS`, no posee objetos y solo puede leer las columnas del contrato. Una selección revalida la membresía activa antes de rotar la sesión; el ID del cliente es solo un locator.

El resultado interno está acotado a 100 membresías activas, usa orden determinista y no contiene email, CUIT, claims IdP ni datos productivos. Cero membresías no revoca la sesión en memoria, pero el contrato HTTP histórico del spike responde `403 active-membership-required`; una activa selecciona contexto y varias requieren elección explícita. La separación platform-scoped observable del runtime productivo no se atribuye a este spike.

## Límites para R1

La viabilidad técnica del discovery y la identidad externa one-to-many quedaron demostradas; su migración productiva pertenece a `AGRO-ID-003`/`AGRO-SEC-002`, no a este artefacto descartable. Persisten como gates: sandbox IdP real, callback OIDC/PKCE, headers/caché productivos, failover, DPA/región/plan/SLA/exportabilidad, roles/alcances definitivos, migrator separado, jobs por capacidad y mutaciones exactly-once.
