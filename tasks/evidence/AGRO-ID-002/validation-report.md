# AGRO-ID-002 — Validación del sub-slice step-up MFA

Fecha: 2026-08-10  
Estado del resultado: `PASS` local para el sub-slice; la tarea padre permanece `En curso`.

## Valor demostrado

AgropecuarIA puede iniciar una verificación MFA ligada al usuario, sesión y propósito `manage_authentication_methods`, validar evidencia OIDC firmada y fresca, consumir el intento una sola vez y rotar la sesión sin ampliar su vencimiento absoluto. La UI distingue assurance primaria y fuerte, muestra su vencimiento y limita el loading a la región afectada.

La frescura OIDC de `AGRO-ID-001` no se interpreta como MFA. Auth0 conserva passkeys, TOTP, recovery codes, tokens y factor IDs; la aplicación persiste únicamente assurance gruesa y evidencia de auditoría sin PII.

## Contrato, seguridad y casos cubiertos

- Contrato OpenAPI para crear intento, iniciar challenge y consultar assurance; fixture de completado disponible solamente en `Development`/`Test`.
- Authorization Code + PKCE, `state`, nonce, `max_age=0`, ACR MFA, `amr=mfa` y `auth_time` firmado posterior al inicio.
- Binding y autorización por usuario, sesión, identidad externa y propósito allow-listed; el cliente no decide actor ni alcance.
- CSRF en mutaciones; rate limit por sesión; TTL de cinco minutos; consumo atómico `SERIALIZABLE`; replay y callback concurrente fallan cerrados.
- Casos negativos: purpose inválido, prueba débil o ajena, usuario/sesión cruzados, sesión revocada, intento vencido, CSRF ausente, replay, concurrencia y cookie anterior revocada.
- UI: loading regional, assurance primaria/fuerte, expiración, proveedor no disponible, reintento, teclado, viewport angosto y Axe.
- Telemetría, journal y outbox con operación/resultado/propósito acotados, sin email, subject, tokens, claims ni identificadores de factor.

## Migración y compatibilidad

`20260810195645_AddPurposeBoundStrongAuthentication` agrega columnas nullable a `identity.sessions` y la tabla efímera `identity.step_up_attempts`, con constraints, índices, FKs y concurrencia optimista. Filas existentes y writers N-1 quedan en assurance primaria; writer N puede registrar assurance fuerte válida. PostgreSQL 17 efímero validó previous→latest, writer N-1 posterior al expand, restricciones, rollback y roll-forward.

Rollback operativo: desactivar la capability, drenar challenges y volver al binario N-1. El `Down` de datos se limita a una base efímera; en un ambiente compartido se usa roll-forward y no se elimina el journal. Volver al esquema anterior pierde grants fuertes efímeros de forma segura, pero conserva sesiones e historial.

## Gates finales reproducidos

- `dotnet tool restore`: PASS (`dotnet-ef` 10.0.4).
- `dotnet restore AgropecuarIA.slnx --locked-mode`: PASS, 5 proyectos restaurados.
- `dotnet build AgropecuarIA.slnx --configuration Release --no-restore`: PASS, 0 warnings y 0 errores.
- `dotnet test --solution AgropecuarIA.slnx --configuration Release --no-build --minimum-expected-tests 100`: PASS, 100/100, 0 failed y 0 skipped.
- `dotnet format AgropecuarIA.slnx --no-restore --verify-no-changes`: PASS.
- `dotnet ef migrations has-pending-model-changes ... --configuration Release --no-build`: PASS, sin cambios pendientes.
- `pnpm install --frozen-lockfile`: PASS; lockfile vigente con pnpm 10.33.0.
- `pnpm format`, `pnpm lint`, `pnpm typecheck`, `pnpm test`: PASS; Vitest 23/23.
- `pnpm build`: PASS; Next.js 16.3.0 compiló y prerenderizó 3/3 rutas.
- `scripts/identity/run-e2e.ps1`: PASS; Playwright 4/4 en Chromium desktop y mobile con PostgreSQL real.
- NuGet vulnerable transitivo: 0; `pnpm audit --prod --audit-level high`: 0; secrets scan de líneas agregadas: 0.
- `git diff --check`: PASS; advertencia informativa de futura normalización LF del snapshot, sin error de whitespace.

Principal QA y AppSec/Arquitectura revisaron independientemente el estado integrado. AppSec: PASS, 0 hallazgos críticos/altos/medios. QA: el primer gate detectó CRLF en la migración, mojibake visible y cobertura negativa faltante; los tres hallazgos fueron corregidos y revalidados.

## Pendientes y decisión de estado

No se declara implementado el lifecycle real de passkey, TOTP o recovery. Antes de completar `AGRO-ID-002` faltan tenant/plan Auth0, custom domain/RP ID estable, alta/uso/revocación de factores, recovery one-shot y pérdida total, notificación, enforcement por roles de `AGRO-ID-003` y matriz de navegadores/dispositivos. También debe fijarse la retención/purga de intentos consumidos antes de un ambiente compartido.

Por ello el sub-slice queda aprobado localmente y la tarea padre permanece `En curso`, sin deploy.
