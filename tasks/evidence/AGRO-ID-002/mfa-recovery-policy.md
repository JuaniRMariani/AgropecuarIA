# AGRO-ID-002 — Política MFA/recovery para desarrollo R1

Estado: aprobada por delegación del sponsor para desarrollo local. No autoriza despliegue compartido ni sustituye los gates del proveedor.

## Decisiones

- Auth0 es el adaptador objetivo y custodio de passkeys, semillas TOTP, recovery codes, tokens y factor IDs. AgropecuarIA no persiste ni registra ese material.
- La passkey es el método preferido resistente a phishing. TOTP es un segundo factor y fallback distinto. SMS no pertenece al alcance.
- Cualquier usuario verificado puede configurar factores cuando el proveedor lo admita. Owner, administrador y contador deberán usar autenticación fuerte cuando `AGRO-ID-003` entregue roles efectivos; hasta entonces no se infiere autorización desde claims del IdP ni strings de fixture.
- Alta, revocación o regeneración de factores requiere step-up ligado a usuario, sesión y propósito `manage_authentication_methods`, de consumo único y vigencia máxima de cinco minutos.
- Un step-up válido requiere un nuevo Authorization Code flow con PKCE, `max_age=0`, `acr_values=http://schemas.openid.net/pape/policies/2007/06/multi-factor`, `auth_time` firmado posterior al inicio y `amr` que contenga `mfa`.
- La frescura OIDC de `AGRO-ID-001` y la autenticación fuerte son hechos diferentes. Una sesión primaria nunca se eleva por tener solamente `auth_time` reciente.
- Completar el step-up consume el intento, revoca la sesión iniciadora y emite una sesión opaca nueva sin extender la expiración absoluta previa. El grant fuerte queda ligado a la nueva sesión y al propósito.
- Recovery codes son de un uso, se muestran una sola vez por el proveedor y nunca llegan a logs, analytics u outbox de AgropecuarIA. Tras recovery se deben revocar sesiones previas y recomponer un factor fuerte; ese recorrido permanece pendiente del sandbox.
- El canal candidato para recovery/notificación es el correo verificado administrado por el IdP. AgropecuarIA registra intención/evento sin email ni secreto; delivery real no se simula y pertenece al gate de entorno/FND-002.
- No existe reset manual, impersonación, pregunta insegura ni bypass de soporte.

## Contrato del primer sub-slice

`POST /api/identity/step-up-attempts` crea un intento one-shot para el actor y sesión derivados del servidor. `GET /api/identity/step-up/{attemptId}` inicia el challenge real. El callback OIDC valida issuer, subject, sesión, propósito, TTL, `auth_time`, `acr` y `amr` antes de rotar la sesión.

La respuesta de sesión expone solamente `primary|strong`, fechas normalizadas y el propósito permitido. No expone claims crudos, subject, factor IDs o tokens. El endpoint sintético de completado existe físicamente solo en `Development`/`Test`.

Replay, expiración, cambio de usuario/sesión, identidad ajena, sesión revocada, claim ausente, proveedor caído y concurrencia fallan cerrados. La auditoría/telemetría usa operación, resultado, propósito allow-listed y correlación; nunca PII o secretos.

## Rollout y compatibilidad

La migración es aditiva: sesiones existentes y writers N-1 quedan en assurance primaria; ninguna fila se promueve por default. Los intentos son efímeros y pueden expirar al volver a un binario anterior. El rollback operativo deshabilita el inicio de nuevos step-ups y vuelve al binario N-1; la historia de seguridad no se elimina.

## Gates externos para completar AGRO-ID-002

- Tenant/plan Auth0, Universal Login, Identifier First, custom domain/RP ID y conexión compatibles con passkeys.
- Claims `acr`/`amr` reales, passkey alta/uso/revocación, TOTP, recovery one-shot, pérdida de todos los factores y revocación de sesiones.
- Correo de notificación/recovery, browser/device matrix y comportamiento provider-down/rate limit.
- Enforcement por rol tras `AGRO-ID-003`; región, DPA, retención, SLA y exportabilidad antes de ambiente compartido.

No se depende del Default Policy de assurance de My Account API porque la documentación vigente lo clasifica Early Access.

## Fuentes primarias verificadas el 2026-08-10

- Auth0, step-up web: <https://dev.auth0.com/docs/secure/multi-factor-authentication/step-up-authentication/configure-step-up-authentication-for-web-apps>
- Auth0, My Account API: <https://auth0.com/docs/api/myaccount>
- Auth0, passkey policy: <https://auth0.com/docs/authenticate/database-connections/passkeys/configure-passkey-policy>
- Auth0, `max_age`/`auth_time`: <https://auth0.com/docs/authenticate/login/max-age-reauthentication>
