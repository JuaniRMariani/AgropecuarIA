# AGRO-SEC-002 — gate Identity tenant v1

Incremento integrado-local de la tarea multirelease AGRO-SEC-002. El padre permanece `En curso`.

## Qué queda comprobado

- Las 22 operaciones HTTP actuales de Identity y Territory coinciden exactamente entre OpenAPI, código de rutas y `authorization-surface-register.json`.
- Cada operación declara recurso, acción, frontera, autenticación, fuente de actor/tenant, autorización de aplicación, frontera de storage, error neutral, owner y prueba ejecutable.
- Las operaciones tenant de invitaciones y remoción de co-owner requieren tenant server-derived/revalidado, autorización owner, `FORCE RLS` y errores sin oracle cross-tenant.
- Territory queda como referencia compartida autenticada, sin autoridad tenant.
- Las tres rutas sintéticas permanecen `development-test-only`, con gate de ambiente+flag y prueba de ausencia en Production.
- `/signin-oidc` se registra como entrypoint framework-owned con state, PKCE y frescura.
- Jobs, storage, export, AI y retrieval se declaran explícitamente ausentes; el gate impide presentarlos como integrados.

## Artefactos

- `architecture.md`: reconnaissance y fronteras auditadas.
- `authorization-surface-register.json`: matriz machine-readable.
- `REPORT.md`, `FINDINGS-DETAIL.md`, `findings.json`: resultado del security audit.
- `validation-report.md`: comandos y resultados reproducibles.
- `AuthorizationSurfaceValidator.cs` y `AuthorizationSurfaceContractTests.cs` dentro del fitness FND: enforcement en la suite raíz.

## Límites honestos

Este incremento no agrega endpoints, roles, superadmin ni features. Tampoco demuestra Auth0, edge/TLS, proxy, Data Protection, limiter distribuido, secrets manager, exporter/collector ni egress Georef en un ambiente compartido. El cache de resolución Territory es global por coordenada; como Georef está default-off y no existe deploy compartido no constituye un hallazgo explotable actual, pero debe particionarse o dejar de exponer timestamps de consulta antes de habilitar egress multiusuario.

Siguientes superficies tenant deben entrar al registro en el mismo cambio que su OpenAPI/runtime. La remoción de otro co-owner ya protege concurrentemente el último owner; self-remove, transferencia, democión y roles no-owner siguen fuera del slice.
