# Runbook de identidad: local y servidor de prueba

## Alcance

`AGRO-ID-001` usa una cookie de aplicación respaldada por una sesión revocable en PostgreSQL. Auth0 es el proveedor OIDC objetivo para email OTP y Google. El proveedor sintético existe únicamente para desarrollo y pruebas automatizadas; no acepta claims arbitrarios ni representa una cuenta real.

## Desarrollo local

1. Copiar las variables necesarias desde `.env.example` al almacén local del proceso, sin versionar un `.env` real.
2. Iniciar PostgreSQL con `docker compose up -d postgres` o usar una instancia PostgreSQL 17 local aislada.
3. Ejecutar `dotnet tool restore` y aplicar la migración con `dotnet ef database update --project src/AgropecuarIA.Identity --startup-project apps/AgropecuarIA.Api`.
4. Iniciar la API con ambiente `Development` en `http://127.0.0.1:5080`.
5. Desde `apps/web`, ejecutar `pnpm install --frozen-lockfile` y `pnpm dev`.

El frontend accede a `/api` por el proxy same-origin de Next.js. Las mutaciones exigen el header `X-CSRF-TOKEN`, obtenido desde `/api/identity/antiforgery`. La cookie de sesión mantiene siempre `HttpOnly`, `SameSite=Lax`, `Secure`, `Path=/`, sin `Domain` y prefijo `__Host-`. Solo la cookie doble-submit de antiforgery usa `SameAsRequest` y nombre sin prefijo en `Development`/`Test` para soportar el proxy HTTP de loopback; fuera de esos ambientes conserva `Secure` y prefijo `__Host-`.

En desarrollo, Next usa `http://127.0.0.1:5080` si `AGRO_API_ORIGIN` no está definida. En un build productivo no existe ese fallback: un despliegue con procesos separados debe definir `AGRO_API_ORIGIN`; un ingress same-origin puede enrutar `/api` directamente y omitir el rewrite.

## Servidor de prueba

Antes de arrancar un ambiente distinto de `Development` o `Test`:

- crear la aplicación web confidencial en Auth0;
- habilitar Authorization Code + PKCE y registrar callback/logout exactos;
- confirmar que el proveedor devuelve `auth_time` firmado al recibir `max_age=0`; un callback sin esa prueba se rechaza;
- habilitar las conexiones email OTP y Google en Auth0;
- cargar Authority, ClientId y ClientSecret desde el secret manager del ambiente;
- mantener `Identity:DevelopmentProvider:Enabled=false`;
- usar TLS extremo a extremo y PostgreSQL no compartido con desarrollo;
- configurar únicamente las IP de proxies confiables en `ReverseProxy:KnownProxies`; nunca aceptar `X-Forwarded-*` desde redes abiertas;
- validar callback, logout, linking, replay, revocación y provider-down con la suite E2E.

La API falla al iniciar si un ambiente no local carece de configuración OIDC o intenta habilitar el proveedor sintético. Los placeholders de `.env.example` no son credenciales válidas.

Email y Google poseen flags independientes (`Identity:Oidc:EmailEnabled` y `Identity:Oidc:GoogleEnabled`). Deshabilitar uno lo retira de capabilities, rechaza login/link del servidor y permite un rollback acotado sin apagar la otra conexión.

La API limita por ventana fija tanto la IP (`Identity:RateLimits:PerIpPerMinute`, 120 por defecto) como la sesión opaca (`PerSessionPerMinute`, 30 por defecto). Ambos límites se evalúan antes de consultar PostgreSQL; la clave de partición de sesión es un hash y nunca el token. El flujo E2E crítico requiere menos de diez requests por minuto, por lo que queda margen para reintentos rurales sin permitir ráfagas ilimitadas. Antes del servidor compartido se deben revisar métricas `rate_limited`, capacidad del proxy y falsos positivos; el límite de sesión nunca puede superar al de IP.

Para vincular en Auth0, `POST /api/identity/link-attempts` devuelve un `authorizationUrl` que incluye el `linkAttemptId` opaco. El navegador completa OIDC code + PKCE usando `response_mode=query`; así la cookie de sesión `SameSite=Lax` acompaña el callback GET. Todo login registra el instante del challenge dentro de `AuthenticationProperties` protegido y envía `max_age=0`. El callback valida `state`, nonce, issuer, claims, email verificado y que el `auth_time` firmado no sea anterior al challenge ni futuro fuera de una tolerancia de un minuto. Solo entonces la sesión se marca con assurance verificada. La vinculación además exige la misma sesión iniciadora, adjunta la segunda prueba, consume el intento, persiste `IdentityLinked` y vuelve a `/?identityLinked=true`. El cliente no debe completar el intento otra vez.

Las sesiones creadas antes de la migración de assurance, o por un writer N-1 durante la ventana de despliegue, siguen sirviendo para lecturas y cierre de sesión, pero fallan cerradas con `identity.reauthentication_required` al vincular o desvincular identidades. El usuario debe iniciar sesión nuevamente; no se promueve una sesión legacy por timestamp local.

## Rollback y recuperación

Las migraciones son aditivas. `AddAuthenticationAssuranceToSessions` agrega un booleano `NOT NULL DEFAULT false`, por lo que conserva writers N-1 y no atribuye assurance a sesiones existentes. Ante fallo de rollout, deshabilitar los flags de conexión y volver a la versión previa sin eliminar tablas ni identidades. Revocar las sesiones emitidas por el rollout afectado antes de reabrir acceso. La reversión de esquema no se ejecuta sobre datos compartidos: se realiza roll-forward una vez preservada la auditoría y exportada la evidencia del incidente.

## Datos sensibles y diagnóstico

No registrar email, subject OIDC, token, OTP, cookie ni antiforgery token. Los logs y métricas usan resultado, conexión, correlación y latencia; los IDs internos solo se incluyen cuando la política de acceso a telemetría lo permite. Para anomalías, revisar las métricas de login/link/revocación y los eventos append-only —protegidos por trigger contra `UPDATE`/`DELETE`— antes de revocar sesiones.
