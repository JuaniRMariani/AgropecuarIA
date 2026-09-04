# CI quality gates

`.github/workflows/ci.yml` runs on pull requests, pushes to `main`, and manual dispatch. It is verification-only: no deployment, package publication, production credentials, external notifications, or repository-settings mutations.

Backend uses the SDK selected by `global.json`, locked restore, high/critical NuGet dependency auditing, warnings-as-errors Release build, and the entire Microsoft.Testing.Platform solution suite. Testcontainers provides isolated PostgreSQL/PostGIS instances with generated ephemeral passwords. `AGRO_TEST_POSTGIS=true` provisions the extension only in fresh disposable test databases; application runtime does not install extensions.

The backend runner emits TRX reports. An always-run diagnostic step publishes only bounded static class/method names for failed cases as check annotations; it excludes parameter/display values, stdout, failure messages and stack traces. The parser rejects DTDs and is tested independently. It does not replace or suppress the test runner's failing exit status, and no raw report artifacts are uploaded.

Frontend uses Node 24 and the exact pnpm version declared by `apps/web/package.json`, frozen lockfile install, formatting, lint, type checking, all unit/component tests, production build, and high/critical package audit. Actions are pinned to reviewed commit SHAs, repository-token access is read-only, and checkout does not persist credentials. See official [checkout](https://github.com/actions/checkout), [setup-dotnet](https://github.com/actions/setup-dotnet), [setup-node](https://github.com/actions/setup-node), and [pnpm v10 setup](https://github.com/pnpm/action-setup) documentation for their respective inputs.

`pnpm exec next typegen` runs before the standalone type check so a clean checkout generates its route declarations rather than relying on a developer's existing `.next` directory. The generated `next-env.d.ts` rewrite is not a hand-maintained source change.

The PostgreSQL 17/PostGIS 3.6 Alpine image is listed in the [official PostGIS image repository](https://github.com/postgis/docker-postgis). The tag selects the maintained image line; unlike action SHAs it is not an immutable image digest. Native local regression testing uses the separately pinned discovery runtime.

## Local reproduction

From the repository root in PowerShell, use a disposable test environment (Docker with the PostGIS image, or the existing native discovery bundle):

```powershell
$env:AGRO_TEST_POSTGIS = 'true'
# Optional when the native discovery bundle is already present:
$env:AGRO_IDENTITY_POSTGRES_BIN = (Resolve-Path 'tasks/evidence/AGRO-DIS-004/spike/postgis/.runtime/postgresql-17-postgis-3.6.2/bin').Path
dotnet restore AgropecuarIA.slnx --locked-mode -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=high
dotnet build AgropecuarIA.slnx --configuration Release --no-restore
dotnet test --solution AgropecuarIA.slnx --configuration Release --no-restore --no-build
./scripts/identity/run-e2e.ps1 -ApiPort 5097
```

Omit the native binary assignment when using Docker. These assignments affect only the current shell and its child processes; do not configure a shared database or install PostGIS into the system server to run this gate. The native E2E wrapper is Windows-specific and requires the PostGIS-enabled binaries.

The wrapper also publishes two explicitly synthetic Catalog versions through the real editorial HTTP workflow, using a temporary local editor configuration and revoked setup sessions. This enables reader/history browser regression without a production grant or direct table seed. See the [Catalog slice and fixture boundaries](catalog-publication-reader.md).

## Remaining release gates

This workflow does not claim completed release certification. Browser regression remains the local isolated `scripts/identity/run-e2e.ps1` gate, pending a portable hosted E2E runner. Branch protection/required-check enforcement, shared environments, promotion, deployment and rollback require explicit repository/environment administration and remain separate. A workflow file alone is not proof that a remote run passed: its first hosted execution must be inspected after push.
