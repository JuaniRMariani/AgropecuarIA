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

## Invitación one-shot de co-owner

Fecha de decisión: 2026-08-11. Estado: aceptada para el siguiente sub-slice local de `AGRO-ID-003`.

- El slice agrega exclusivamente otro `owner` activo. En la experiencia se lo denomina `co-owner`; no se crea un rol nuevo ni se inventan permisos por módulo/campo.
- Solo un owner activo puede crear, listar o revocar invitaciones. Crear y revocar requieren step-up purpose-bound `manage_organization_owners`; listar no revela tokens.
- La invitación no se dirige por email: es un enlace bearer que el owner comparte fuera de banda. El producto advierte que quien controle el enlace y una cuenta verificada podrá convertirse en owner.
- El token tiene 256 bits CSPRNG, se muestra una sola vez, viaja en el fragmento del enlace y se persiste únicamente como digest versionado. Nunca aparece en query string, logs, telemetría, journal o eventos.
- TTL inicial configurable: 7 días. `pending` puede pasar una sola vez a `accepted` o `revoked`; `expired` se deriva cuando `now >= expiresAtUtc`. No hay renovación silenciosa ni purga automática.
- Aceptar exige sesión verificada y autenticación reciente menor a 15 minutos. Replay del mismo aceptante devuelve la misma membership; otro actor, token inválido/revocado/expirado o recurso ajeno recibe una respuesta neutral.
- Aceptación crea membership autoritativa y proyección N-1, journal y outbox en una transacción. Crear/revocar también son atómicos e idempotentes.
- El slice solo agrega owners; no implementa democión, remoción, abandono ni transferencia. Por ello conserva estructuralmente el invariante de al menos un owner, pero no declara completado el runtime de último owner.
- Invitaciones dirigidas por email, roles no-owner, scopes por campo/módulo/acción y delivery permanecen bloqueados hasta sus decisiones/tareas correspondientes.
- Un owner existente puede aceptar el enlace de forma idempotente: la invitación queda consumida sin crear una segunda membership. Esto no reemplaza el journey principal con un invitado distinto.
- La rotación conserva todas las versiones HMAC retenidas y exige cobertura global antes de retirar una versión. La transición v1 → v1+v2 → v2-only falla cerrada si quedan filas sin alias resoluble; no se conserva el token crudo para backfill.

## Remoción de otro co-owner activo

Fecha de decisión: 2026-08-18. Estado: aceptada para un sub-slice local de `AGRO-ID-003`; la tarea padre permanece `En curso`.

- Todos los owners activos son simétricos. El creador no es fundador ni primary owner y no obtiene privilegios especiales.
- Un owner activo puede listar los co-owners activos de su organización y remover exclusivamente a **otro** owner activo. Self-remove, abandono, transferencia, democión y roles no-owner quedan fuera de este slice.
- La remoción exige step-up vigente con propósito exacto `manage_organization_owners`, CSRF, versión esperada e idempotencia. Actor, sesión y tenant siempre se derivan del servidor.
- La organización conserva al menos un owner activo. La decisión se serializa y protege dentro de PostgreSQL; dos remociones concurrentes no pueden dejar cero owners.
- La membership autoritativa se conserva como `removed`, con fecha/actor de remoción y versiones incrementadas. Su proyección legacy se elimina atómicamente, por lo que el usuario conserva su cuenta de plataforma pero pierde acceso tenant inmediatamente.
- Las invitaciones `pending` creadas por el owner removido se revocan en la misma decisión. Crear invitaciones y remover owners se serializan por organización para que no sobreviva un bearer emitido por un actor removido.
- Las invitaciones ya `accepted` permanecen como historial. Una membership removida no se reactiva en este slice: una nueva aceptación falla cerrada hasta que exista una transición de reactivación explícita y revisada.
- El directorio de co-owners expone sólo display name, membership ID, rol/estado y versiones necesarias. No expone userId, email, identities externas ni un directorio cross-tenant.
- El evento de remoción omite nombre, email y userId; el journal conserva sólo identificadores internos mínimos y la telemetría usa outcomes acotados.
- Superadmin, soporte cross-tenant, email/delivery, scopes por campo y observabilidad global continúan fuera de alcance.
