# AGRO-ID-003 — decisiones de producto para CreateOrganization

Fecha: 2026-08-10  
Estado: aceptadas para el sub-slice R1 local; la tarea padre permanece `En curso`.

## Registro, privacidad y tenancy

- El registro de cuenta es público: una persona puede crear su cuenta mediante una identidad verificada.
- Las organizaciones y sus datos no son públicos. No existe directorio global ni búsqueda cross-tenant en este slice.
- Un mismo `PlatformUser` puede crear y pertenecer a múltiples organizaciones.
- `Organization` es el tenant técnico. CUIT, titularidad legal y propiedad del establecimiento no se infieren ni autorizan acceso.
- Los nombres se validan, pero no son únicos globalmente.

## Owner

- Quien crea la organización queda como único `owner` activo inicial.
- `owner` es un rol tenant de control operativo. No concede privilegios de plataforma ni acceso a otras organizaciones.
- No se inventa un límite comercial de organizaciones por usuario; el borde aplica controles técnicos de abuso.
- La regla para mutaciones posteriores es conservar al menos un owner activo: el último owner no puede removerse, demoverse ni abandonar.

## Autenticación de CreateOrganization

- El actor se deriva de la cookie de sesión durable, nunca del body o de claims tenant del IdP.
- Se exige assurance verificada y autenticación reciente estrictamente menor a 15 minutos.
- No se exige MFA fuerte para el bootstrap. Las mutaciones privilegiadas posteriores pueden usar step-up purpose-bound cuando su política y ciclo real estén aprobados.

## Superadmin y diagnóstico

- Un posible superadmin futuro es una capacidad de plataforma separada, aprovisionada y auditada; no es `owner` ni una membership tenant.
- Este slice no implementa superadmin, impersonación, acceso permanente cross-tenant, collector global ni tablero de bugs.
- El acceso futuro a datos privados para soporte deberá ser explícito, temporal/JIT, con motivo, MFA, trazabilidad y los límites de `AGRO-ID-005`/seguridad aplicables.
- La telemetría de este slice usa resultados acotados y no incluye nombres, UUID, usuario, tenant, key, digest ni payload.

## Alcance exacto

Incluido:

- `CreateOrganization` idempotente;
- organización privada;
- membership owner activa inicial;
- journal local y outbox;
- onboarding y listado 0/1/N.

Fuera del sub-slice:

- invitaciones y su ciclo de vida;
- otros roles, transferencia/democión y mutaciones de último owner;
- alcances por campo/módulo/acción/estado;
- creación de campos o geometrías, que pertenece a `AGRO-GIS-002`;
- CUIT/titularidad contractual;
- superadmin y observabilidad global.
