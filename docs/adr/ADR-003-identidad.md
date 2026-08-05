# ADR-003 — OIDC, passkeys y step-up MFA

- Estado: propuesto — `GO CONDICIONAL` R0 para sandbox, sin autorización productiva
- Fecha: 2026-08-05

## Contexto

Se requiere email, Google y códigos de un solo uso. Hay acciones fiscales/patrimoniales de alto riesgo.

## Decisión

IdP administrado integrado por OIDC. Auth0 es el candidato preferido para el próximo sandbox; ZITADEL Cloud queda como alternativa y AWS Cognito como comparador. Passkey WebAuthn preferida, Google federado, email OTP alternativo, TOTP como MFA y recovery codes. Step-up vigente para acciones sensibles. Sesión opaca en cookie segura.

AgropecuarIA conserva usuario global, membresías por organización, permisos y sesión. El IdP no decide tenant. Linking usa `(issuer, subject)`, nunca email coincidente, y exige reautenticación de ambas identidades, TTL y consumo único.

## Alternativas

- Autenticación propia completa: rechazada por riesgo/costo.
- Solo password+TOTP: compatible, pero menos resistente a phishing.
- SMS: solo si un requisito de usuario lo justifica.

## Consecuencias

Menos secretos propios, dependencia controlada por estándar. Deben probarse account linking, recuperación, disponibilidad del IdP y exportación/migración.

El spike `AGRO-DIS-003` confirmó RLS fail-closed, sesión/contexto y controles internos con datos sintéticos. La decisión permanece propuesta hasta probar sandbox real con PKCE, `state`/`nonce`, claims, logout/revocación, linking/recovery, pérdida de factor y failover; además requiere aprobación de plan, región, DPA, retención, SLA y exportabilidad. La identidad externa one-to-many y el discovery de membresías son gaps de persistencia para R1.
