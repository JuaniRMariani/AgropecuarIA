# AGRO-SEC-002 — arquitectura y fronteras auditadas

Fecha: 2026-08-11. Target: `B:\Xenova\AgropecuarIA`. Alcance del run: runtime integrado Identity + Territory. Output del security audit: este directorio.

## Aplicación y baseline

AgropecuarIA es una aplicación web SaaS multi-tenant para gestión agropecuaria argentina. El runtime actual es un bootstrap local: Identity/Tenancy ofrece sesión, linking, step-up, organizaciones privadas e invitaciones one-shot de co-owner; Territory ofrece referencia administrativa argentina compartida y autenticada. No existen todavía campos tenant, geometrías, mapa, superadmin, worker de delivery, storage, exports ni deploy compartido.

La comparación de seguridad útil es un portal B2B multi-tenant para Identity y un adaptador autenticado de geocoding/referencia para Territory. El baseline esperado es authz por recurso, BOLA neutral, sesión server-side, CSRF, step-up, revocación, aislamiento DB y auditoría; para Territory, datos base compartidos, parser cerrado, destino fijo, timeout/tamaño, fallback y minimización.

No hay runs previos bajo la skill `security-audit`. `tasks/evidence/AGRO-SEC-001/` es evidencia previa de threat modeling y debe usarse para buscar gaps, no como sustituto de esta auditoría.

## Stack y despliegue real

- .NET 10, ASP.NET Core Minimal APIs, EF Core/Npgsql y PostgreSQL 17.
- Next.js 16.3, React 19.2, TypeScript 6 y pnpm; proxy same-origin `/api` hacia ASP.NET Core.
- OIDC/Auth0 objetivo; la app consume authorization code + PKCE y emite una sesión opaca propia en cookie.
- OpenTelemetry in-process sin exporter/collector configurado.
- Runtime local con PostgreSQL; no hay CI, edge, worker ni deploy compartido.
- Migraciones automáticas sólo Development/Test. Georef externo está apagado por defecto.

Entrypoints: `apps/AgropecuarIA.Api/Program.cs`, `apps/AgropecuarIA.Api/IdentityEndpoints.cs`, `src/AgropecuarIA.Identity/Application/IdentityApplicationService.cs`, `src/AgropecuarIA.Territory/Delivery/TerritoryEndpoints.cs`, `src/AgropecuarIA.Territory/Application/TerritoryReferenceService.cs`, `apps/web/features/identity/identity-hub.tsx` y `apps/web/features/territory/territory-api.ts`.

## Actores y autoridad

- Anónimo: capabilities, antiforgery e inicio OIDC; no sesión privada ni Territory.
- Usuario con sesión válida: su sesión, step-up, revocación propia y Territory.
- Usuario verificado reciente: crear organizaciones, linking/unlinking y aceptar invitación bearer.
- Owner tenant activo: listar invitaciones; con step-up `manage_organization_owners`, crear/revocar invitaciones.
- Portador de enlace one-shot con sesión reciente: aceptar exactamente una membership owner.
- IdP: acredita identidad/assurance; nunca autoriza recursos tenant.
- Operador/configurador: controla environment, OIDC, HMAC, conexiones, proxies, migraciones y egress; es una raíz de confianza.
- Superadmin: no existe. `owner` nunca concede capacidad de plataforma o cross-tenant.

## Fronteras de confianza y controles

1. **Browser → Next → API.** Requests relativos, cookies con `credentials: include`, mutaciones con antiforgery y respuestas API `no-store`. React escapa contenido; no hay HTML raw/eval. `AGRO_API_ORIGIN` es configuración operativa privilegiada.
2. **OIDC → callback → sesión local.** Code+PKCE, tokens no persistidos, `auth_time`, issuer/subject y `acr/amr` cuando corresponde. La cookie `__Host-agro-session` es HttpOnly/Secure/SameSite=Lax y el DB guarda sólo SHA-256.
3. **IDs/body/header → contexto efectivo.** El cliente controla locators, payload, key e `If-Match`, pero endpoints derivan actor de claims; application vuelve a validar sesión, scope, tenant, assurance y membership.
4. **Tenant A/B → PostgreSQL.** Operaciones tenant usan transacción, `SET LOCAL ROLE agro_identity_app`, actor/scope/organization server-derived, grants mínimos y `FORCE RLS`.
5. **Token bearer → tenant.** Fragmento URL removido inmediatamente, 256 bits, formato canónico, HMAC en DB y resolver `SECURITY DEFINER` estrecho. El token resuelve un locator; la sesión y estado vuelven a autorizarse.
6. **Territory compartido.** `/api/territory/*` requiere sesión y rate limit, pero no tenant/owner: lee un snapshot oficial platform-owned con rol DB read-only. No persiste coordenadas.
7. **API → Georef.** Default-off. Si se habilita, host/base fijo, redirects/cookies/decompression/loggers deshabilitados, 5 s, 256 KiB, JSON cerrado y eco de coordenadas validado.
8. **Runtime/migrator.** Roles Identity/Territory separados en DB, todos `NOLOGIN/NOINHERIT/NOBYPASSRLS`; auto-migrate local/Test. La separación es modular/DB, no de proceso u OS.

## Superficies de entrada

Identity expone 18 operaciones HTTP declaradas, incluidas tres Development/Test; Territory expone dos GET autenticados. El callback `/signin-oidc` pertenece al middleware OIDC. Config JSON/env/CLI controla conexiones, OIDC, HMAC, feature flags, lifetimes, proxies y rate limits. PostgreSQL recibe queries EF/SQL parametrizadas. El browser procesa forms, API JSON, `location.hash`, `sessionStorage`, clipboard y cookies HttpOnly. Georef recibe lat/lon sólo tras validación y flag.

No existen uploads, webhooks, broker/consumer, gRPC/WebSocket, shell execution, plugins, filesystem request-driven, HTML raw ni endpoint de import Territory.

## Bypasses locales explícitos

Development/Test + `Identity:DevelopmentProvider:Enabled` habilita sign-in, linking proof y MFA sintéticos. El sign-in sintético es deliberadamente anónimo y puede emitir una sesión verificada; `appsettings.Development.json` lo habilita. Startup falla si el flag se activa fuera de Development/Test, y tests prueban ausencia en Production. Estos endpoints son un blocker de cualquier ambiente compartido rotulado Development.

## Focos Phase 2

- BOLA y oracle cross-tenant en owner-invitations, replay/idempotencia, accept bearer y membership revocada.
- Diferencias entre authz application y policies/grants/RLS; fuga de contexto en pool/job.
- Dev/Test mal clasificado y configuración operativa privilegiada.
- Lifecycle de sesión/OIDC/step-up y client-side bearer/hash/sessionStorage.
- Territory incorrectamente tratado como tenant o, a la inversa, aceptando tenant/PII; cache/telemetría/egress.
- SQL/SSRF/injection, errores neutrales, headers/cache y fallos sad-path.

Gates de despliegue no confirmables desde source — Auth0 real, edge/TLS, proxy, Data Protection compartida, limiter distribuido, secrets manager y query redaction end-to-end — se registran como `requires deployment testing`, no como vulnerabilidades confirmadas.
