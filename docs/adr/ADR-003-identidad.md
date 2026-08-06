# ADR-003 — OIDC, passkeys y step-up MFA

- Estado: aceptado para desarrollo R1 — despliegue compartido condicionado
- Fecha: 2026-08-05

## Contexto

Se requiere email, Google y códigos de un solo uso. Hay acciones fiscales/patrimoniales de alto riesgo.

## Decisión

IdP administrado integrado por OIDC. Auth0 es el adaptador objetivo para R1; ZITADEL Cloud queda como alternativa y AWS Cognito como comparador. Email OTP y Google federado son los mecanismos de `AGRO-ID-001`; passkey WebAuthn, TOTP, recovery codes y step-up pertenecen a las tareas posteriores que los trazan. La aplicación mantiene una sesión opaca en cookie segura.

AgropecuarIA conserva usuario global, membresías por organización, permisos y sesión. El IdP no decide tenant. Linking usa `(issuer, subject)`, nunca email coincidente, y exige reautenticación de ambas identidades, TTL y consumo único.

## Alternativas

- Autenticación propia completa: rechazada por riesgo/costo.
- Solo password+TOTP: compatible, pero menos resistente a phishing.
- SMS: solo si un requisito de usuario lo justifica.

## Consecuencias

Menos secretos propios, dependencia controlada por estándar. Deben probarse account linking, recuperación, disponibilidad del IdP y exportación/migración.

El spike `AGRO-DIS-003` confirmó RLS fail-closed, sesión/contexto y controles internos con datos sintéticos. El sponsor decidió que secretos y credenciales reales se incorporen al publicar el servidor de prueba; no bloquean el desarrollo local. `AGRO-ID-001` implementa identidad externa one-to-many, sesión y linking persistidos con un adaptador sintético exclusivo de `Development`/`Test`. Cualquier ambiente compartido falla cerrado hasta probar Auth0 real con PKCE, `state`/`nonce`, claims, callback, logout/revocación y provider-down, y hasta resolver plan, región, DPA, retención, SLA y exportabilidad.
