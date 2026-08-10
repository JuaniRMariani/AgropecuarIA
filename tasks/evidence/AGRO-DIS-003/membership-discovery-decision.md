# Decisión R0 — discovery de membresías antes de seleccionar tenant

Estado: aceptada para desarrollo R1; implementación productiva pendiente.

## Contexto

El actor se autentica como usuario global antes de elegir una organización. Las policies tenant del spike requieren `app.current_organization_id`, por lo que no pueden enumerar por sí solas las organizaciones activas del actor. El prototipo anterior resolvía 0/1/N con memoria y no demostraba el camino PostgreSQL.

La aplicación, no el IdP ni el cliente, sigue siendo autoridad de membresías. El `organizationId` recibido al cambiar contexto es solo un locator y se revalida contra la membresía vigente.

## Decisión

1. Mantener dos fronteras DB distintas:
   - `agro_membership_discovery`: login read-only, `NOINHERIT`, `NOBYPASSRLS`, sin ownership ni acceso a identidades globales o datos tenant;
   - `agro_app`/`agro_job`: operaciones tenant después de fijar contexto, también sin ownership ni `BYPASSRLS`.
2. El servidor obtiene el actor de la sesión opaca, abre una transacción sobre el pool de discovery y ejecuta `set_config('app.current_actor_id', ..., true)` antes de la primera consulta.
3. Policies `SELECT` exclusivas del rol de discovery permiten únicamente memberships activas propias y organizaciones activas asociadas. No se agrega una policy permisiva al rol tenant.
4. El resultado interno usa `membership-discovery.v1`: campos mínimos, orden determinista y máximo de 100. No incluye email, CUIT, propiedad contractual, claims IdP ni datos productivos.
5. Una organización se activa solo después de consultar nuevamente la membership vigente; la sesión rota y su `authorizationVersion` deriva de `security_version`.
6. RLS es defensa en profundidad contra errores de aplicación. No reemplaza autenticación, autorización por recurso, consultas parametrizadas ni protección frente a compromiso del principal DB.

## Alternativas

- `SECURITY DEFINER` con owner: viable, rechazada para este camino porque amplía el impacto de un error de función/search path y no es necesario con un rol RLS dedicado.
- Policy actor-scoped sobre `agro_app`: rechazada; las policies permisivas se combinan y podrían ampliar lecturas de otros casos tenant.
- Rol `BYPASSRLS`, owner runtime o RLS deshabilitada: rechazados.
- Claims de organizaciones emitidos por el IdP: rechazados; el IdP no decide tenancy y una revocación quedaría stale.
- Proyección eventual: diferida hasta existir outbox/dispatcher y una política explícita de staleness; discovery de seguridad usa la fuente transaccional.

## Compatibilidad y rollback

El artefacto vive únicamente en el clúster efímero de `AGRO-DIS-003`. La migración agrega un identificador estable de membership, rol, grants y policies sin copiarse al runtime productivo. El rollback es detener y eliminar `.runtime`; R1 debe crear su propia migración forward-safe después de revisar este resultado.

## Evidencia requerida

- PostgreSQL real: 0/1/N, actor ausente/ajeno, membership u organización inactiva, grants y `FORCE RLS`.
- Pool: commit, rollback, excepción y cancelación no conservan `app.current_actor_id`.
- HTTP: el request no aporta actor; switch ajeno/revocado es neutral y no rota sesión.
- Catálogo: owner/schema owner no son miembros de roles runtime; ningún principal runtime es superuser o `BYPASSRLS`.

## Fuentes primarias

- [PostgreSQL 17 — Row Security Policies](https://www.postgresql.org/docs/17/ddl-rowsecurity.html): default-deny, `FORCE ROW LEVEL SECURITY`, combinación de policies y límites de RLS.
- [PostgreSQL 17 — System Administration Functions](https://www.postgresql.org/docs/17/functions-admin.html): `set_config(..., true)` se limita a la transacción.
- [PostgreSQL 17 — Function Security](https://www.postgresql.org/docs/17/perm-functions.html): riesgos de funciones privilegiadas y `search_path`.
- [Npgsql — Basic Usage](https://www.npgsql.org/doc/basic-usage.html): `NpgsqlDataSource`, pooling y transacciones explícitas.

## Gaps que esta decisión no cierra

Auth0 real, región/DPA/plan/SLA/exportabilidad, roles/alcances definitivos, migrator productivo, jobs por capacidad, mutaciones exactly-once, auditoría central y toda la suite R1 de `AGRO-SEC-002` permanecen fuera del spike.
