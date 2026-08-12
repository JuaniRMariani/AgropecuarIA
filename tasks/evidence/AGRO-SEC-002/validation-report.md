# Validation report — AGRO-SEC-002 Identity tenant v1

Fecha: 2026-08-11. Base: `15ead58`. Alcance: evidencia y fitness; cero cambios en runtime productivo.

## Resultado

PASS integrado-local. El gate registra y valida exactamente 20 operaciones HTTP de Identity/Territory, un callback OIDC framework-owned y cinco superficies futuras `not-present`. AGRO-SEC-002 permanece `En curso`.

## Gates reproducidos

- `dotnet restore AgropecuarIA.slnx --locked-mode`: PASS.
- `dotnet build AgropecuarIA.slnx --configuration Release --no-restore`: PASS, 0 warnings, 0 errores.
- `dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --minimum-expected-tests 240`: PASS `240/240`, 0 failed/skipped.
- Architecture Fitness: PASS `96/96`; `AuthorizationSurfaceContractTests` aporta 17 tests (caso publicado + 16 mutations negativas).
- `dotnet format AgropecuarIA.slnx --verify-no-changes --no-restore`: PASS.
- EF Identity y Territory con `has-pending-model-changes --context ...`: PASS, sin drift. La primera invocación sin `--context` fue ambigua por los dos DbContext y se repitió correctamente de forma explícita.
- `pnpm install --frozen-lockfile`, `format`, `lint`, `typecheck`, `test`, `build`: PASS; Vitest `79/79`.
- `scripts/identity/run-e2e.ps1`: PASS `6/6` desktop/mobile; PostgreSQL efímero detenido y eliminado por el runner.
- FND protocol: PASS `45/45` mutations.
- SEC threat model: PASS `41/41` mutations.
- Security findings schema: PASS, `0 findings valid`.
- NuGet SCA: 7/7 proyectos sin vulnerabilidades conocidas. pnpm audit productivo: sin vulnerabilidades conocidas.
- JSON parse, UTF-8 estricto, secret scan y `git diff --check`: PASS.

## Revisión

Tres revisiones read-only no confirmaron BOLA, bypass de sesión/OIDC/CSRF/step-up, SQLi, SSRF ni boundary confusion explotables con la configuración por defecto. Una cuarta revisión independiente detectó y permitió cerrar dos defectos del gate: extracción limitada de métodos/grupos y falta de enforcement del storage/egress shared-reference.

El cache singleton de resolución Territory puede revelar por `capturedAtUtc` que otro usuario consultó una coordenada exacta si Georef se habilita. Georef está default-off y no hay deploy compartido, por lo que se registra como condición previa a habilitar egress, no como finding activo.

El modelo actual sólo admite memberships `active`; por eso la evidencia no afirma una revocación de membership inexistente. El negativo vigente es sesión revocada; remove/demote/last-owner pertenece al siguiente slice AGRO-ID-003.
