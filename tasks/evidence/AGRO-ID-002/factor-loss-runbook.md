# AGRO-ID-002 — Runbook provisional de pérdida de factor

Estado: aplicable al desarrollo local del contrato de step-up. No es un procedimiento de soporte productivo hasta validar Auth0 y el canal de notificación.

## Comportamiento seguro actual

1. Un usuario con sesión primaria puede solicitar step-up para `manage_authentication_methods`.
2. El challenge exige reautenticación MFA en el IdP. Una sesión SSO previa, un `auth_time` viejo, claims sin `amr=mfa` o un intento vencido no elevan assurance.
3. Al completar, el intento queda consumido, la sesión iniciadora se revoca y se entrega una sesión nueva con grant fuerte de cinco minutos.
4. Replay, sesión revocada, cambio de usuario/sesión o proveedor no disponible fallan cerrados. Nunca se muestra ni registra material de factores.

## Pérdida de un factor durante desarrollo

- Usar otro factor ya enrolado en el IdP para satisfacer el step-up.
- Si no queda ningún factor válido, no crear bypass local, reset manual, pregunta de seguridad ni impersonación. La cuenta se mantiene sin capability fuerte.
- Registrar correlación, categoría de fallo y propósito sin email, subject, OTP, recovery code o token.
- Escalar al administrador del tenant Auth0 de prueba; cualquier recuperación manual ocurre en el IdP y debe invalidar códigos usados y sesiones previas antes de volver a la aplicación.

## Gate para soporte real

El procedimiento productivo requiere demostrar en sandbox: recovery code one-shot, pérdida de todos los factores, revocación global de sesiones de aplicación, recomposición de un factor fuerte, aviso por correo verificado, anti-enumeración, rate limit y auditoría. Hasta entonces la UI no ofrece alta/revocación/recovery ni promete que soporte pueda recuperar la cuenta.
