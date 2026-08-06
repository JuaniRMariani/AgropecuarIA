# AGRO-ID-001 — reporte de validación

Fecha: 2026-08-05  
Estado: `En revisión`

## Resultado

El slice productivo registra identidades verificadas por `(issuer, subject)`, converge accesos email/Google sobre un único usuario global, mantiene membresías sin transferirlas, vincula una segunda credencial mediante doble prueba reciente y one-shot, permite desvincular sin eliminar la última identidad y revoca sesiones persistidas. El proveedor sintético solo existe en `Development`/`Test`; un ambiente compartido falla cerrado sin OIDC.

## Evidencia ejecutada

- `dotnet tool restore`: PASS; `dotnet-ef` 10.0.4 fijado por manifest.
- `dotnet restore AgropecuarIA.slnx --locked-mode`: PASS.
- `dotnet build AgropecuarIA.slnx -c Release --no-restore`: PASS, 0 warnings y 0 errores.
- `dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --report-trx`: PASS, 21/21, 0 failed/skipped; PostgreSQL 17 efímero real.
- `dotnet format AgropecuarIA.slnx --no-restore --verify-no-changes`: PASS.
- `pnpm install --frozen-lockfile`: PASS.
- `pnpm format`, `pnpm lint`, `pnpm typecheck`: PASS.
- `pnpm test`: PASS, 18/18.
- `pnpm build`: PASS, Next.js 16.3.0.
- `powershell -File scripts/identity/run-e2e.ps1`: PASS, 4/4 Playwright en Chromium desktop y Pixel 7; Axe WCAG 2.2 AA sin violaciones y sin overflow horizontal.
- `dotnet list AgropecuarIA.slnx package --vulnerable --include-transitive`: PASS, 0 paquetes vulnerables conocidos.
- `pnpm audit --audit-level=high`: PASS, 0 vulnerabilidades.
- scan dirigido de secretos y `git diff --check`: PASS.

## Cobertura relevante

- migración inicial, constraints, rollback/roll-forward y trigger append-only;
- sign-in concurrente y unicidad `(issuer, subject)`;
- linking verificado, replay, identidad ajena, proveedor caído y conexión deshabilitada;
- cookie `Secure`/`HttpOnly`/`SameSite`, token opaco hasheado, CSRF stale, revocación y sesión robada;
- rate limit preautenticación por IP/sesión, `Cache-Control: no-store` incluso en rechazos, autorización negativa y endpoints sintéticos ausentes en producción;
- telemetría sin email, subject, tokens, cookies ni labels;
- estados UI loading/empty/error/conflict/replay/provider-down/revoked, IDs abreviados y operación móvil/teclado.

## Migración y rollback

La migración `20260806002243_InitialIdentity` es aditiva y fue aplicada, revertida y reaplicada en PostgreSQL efímero. En servidores compartidos no se habilita auto-migrate ni se ejecuta `Down`: se deshabilita la conexión afectada mediante feature flags, se revocan sesiones comprometidas y se corrige por roll-forward preservando outbox y auditoría.

## Gate externo pendiente

Las credenciales no se versionan y, por decisión del sponsor, se cargarán en el servidor de prueba. Allí resta ejecutar Auth0 real para demostrar Authorization Code + PKCE, `state`/nonce, issuer/claims/email verificado, callback, conexiones email OTP/Google, linking, logout/revocación y provider-down. Hasta esa evidencia no hay autorización de deploy ni estado `Completada`.

Docker/Compose no se ejecutó porque el daemon no está disponible en la estación; las pruebas de persistencia y migración usaron PostgreSQL 17 efímero local. No hubo deploy.
