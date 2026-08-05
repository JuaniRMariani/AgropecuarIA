# Matriz de decisión IdP — AGRO-DIS-003

Fecha de evaluación: 2026-08-05  
Estado: recomendación R0 condicionada; no autoriza producción ni contratación.

## Criterios de entrada

El proveedor debe exponer OIDC estándar, Google federation, email verificado, passkeys WebAuthn, TOTP/MFA, recuperación segura, linking explícito con reautenticación, usuarios multi-organización, auditoría, exportabilidad y contrato/región compatibles. AgropecuarIA conserva usuario, membresías, autorización y sesiones propias; el IdP no decide tenant ni permiso de recurso.

| Criterio | Auth0 | ZITADEL Cloud | AWS Cognito | Gate |
|---|---|---|---|---|
| OIDC y Google | Documentado | Documentado | Documentado | Probar issuer/audience/nonce/PKCE en sandbox. |
| Passkeys | Documentadas para database connections | Documentadas, dominio propio recomendado | Documentadas | Probar browsers/dispositivos objetivo y recuperación. |
| TOTP/MFA | Documentado | TOTP/passkeys/email OTP documentados | TOTP documentado | Verificar step-up y `acr`/`amr`. |
| Recovery codes | Documentados | Sin evidencia primaria suficiente en esta evaluación | Sin evidencia primaria suficiente en esta evaluación | Fallo de evidencia no se convierte en `PASS`. |
| Linking | Manual/sugerido; exige autenticar ambas cuentas | Linking configurable; el auto-link por email debe permanecer deshabilitado | Linking administrativo/federado | Probar conflicto, replay y secundaria ya ligada. |
| Organizaciones B2B | Producto Organizations; límites dependen del plan | Multi-tenancy nativa | Groups/custom claims requieren modelo propio | Tenant efectivo siempre se resuelve en AgropecuarIA. |
| Región/DPA/SLA | Depende de plan/acuerdo | Región y SLA publicados; contrato por validar | Región AWS elegible; DPA/servicios por validar | Privacy/Legal/Procurement antes de producción. |
| Exportabilidad/failover | Management APIs; probar exportación y disponibilidad | APIs/eventos; passkeys no migran directamente | APIs; probar semántica de identidades enlazadas | Ensayo con datos sintéticos y runbook. |
| Costo inicial | Free/Essentials disponibles; varias capacidades B2B dependen del plan | Uso/DAU/API/auditoría | MAU y servicios AWS | Presupuesto y cotización vigentes. |

## Decisión

**Auth0 es el candidato preferido para un sandbox posterior**, porque existe evidencia oficial conjunta de passkeys, MFA/recovery codes, Organizations y linking que exige autenticar ambas identidades. La decisión es un `GO CONDICIONAL` exclusivamente arquitectónico.

No se autoriza producción hasta demostrar:

1. convivencia real de email verificado, Google, passkey, TOTP y recovery en el plan seleccionado;
2. linking iniciado por usuario con reautenticación de ambas identidades, TTL, replay protection y conflicto seguro;
3. `state`, `nonce`, PKCE S256, issuer/audience y logout/revocación;
4. Organizations/costos/límites para el piloto;
5. región, DPA, subprocesadores, retención, exportación y SLA aceptados;
6. failover que mantenga los módulos transaccionales disponibles o claramente degradados.

ZITADEL queda como alternativa prioritaria si Auth0 falla costo/región/exportabilidad. AWS Cognito queda como comparador de costo/ecosistema, pero debe demostrar recovery y linking sin ampliar demasiado lógica propia.

## Fuentes primarias

- Auth0, account linking: <https://auth0.com/docs/manage-users/user-accounts/user-account-linking>
- Auth0, passkeys: <https://auth0.com/docs/authenticate/database-connections/passkeys>
- Auth0, factores MFA y recovery codes: <https://auth0.com/docs/secure/multi-factor-authentication/multi-factor-authentication-factors>
- Auth0, Organizations: <https://auth0.com/organizations>
- Auth0, pricing: <https://auth0.com/pricing>
- ZITADEL, passkeys: <https://zitadel.com/docs/concepts/features/passkeys>
- ZITADEL, account linking: <https://zitadel.com/docs/concepts/features/account-linking>
- ZITADEL Cloud: <https://zitadel.com/zitadel-cloud>
- AWS Cognito, autenticación: <https://docs.aws.amazon.com/cognito/latest/developerguide/authentication.html>
- AWS Cognito, consolidación de identidades: <https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-user-pools-identity-federation-consolidate-users.html>

