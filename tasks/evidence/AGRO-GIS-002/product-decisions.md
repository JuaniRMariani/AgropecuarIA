# Decisiones — campo borrador no espacial

Fecha: 2026-08-18. Estado: aceptado para desarrollo local integrado; `AGRO-GIS-002` permanece `En curso`.

## Ownership y alcance

- Productive Core posee `ManagementUnit`, su identidad y ciclo de vida. Territory posee únicamente representaciones espaciales futuras, opcionales y versionadas.
- El slice crea exclusivamente una unidad de tipo `field`, estado `draft` y `spatialStatus=not_configured`.
- No existe geometría, área declarada/calculada, mapa, tile, ubicación, establecimiento, parcela, lote, potrero, ciclo ni asignación de catálogo en este incremento.
- La UI usa “campo” como nombre de producto, pero no afirma parcela catastral, ubicación probada ni precisión agronómica.

## Autoridad y tenancy

- Sólo una membership `owner/active` puede crear, listar o abrir campos de su organización.
- El `organizationId` de ruta es un locator. Actor, sesión, membership y contexto tenant se derivan y revalidan en servidor y dentro de la transacción PostgreSQL.
- Un owner removido conserva su cuenta de plataforma pero pierde acceso inmediatamente.
- Recursos ausentes, ajenos o inaccesibles responden de forma neutral; no existe búsqueda global ni directorio cross-tenant.

## Nombre y estado inicial

- `displayName` recorta en ambos extremos el conjunto Unicode `White_Space` y `U+FEFF`, luego normaliza a NFC. Admite de 2 a 120 escalares Unicode y rechaza controles y surrogates aislados; backend y frontend aplican exactamente la misma regla.
- Los nombres duplicados se permiten dentro y entre organizaciones. El UUID opaco es la identidad; la UI sólo muestra sus primeros seis caracteres en mayúscula y sin guiones.
- Toda creación comienza en `draft/not_configured`; no hay transición de edición en este slice.
- El incremento local admite como máximo 100 campos persistidos por organización. Una creación nueva cuenta dentro de la misma transacción `SERIALIZABLE`, después de resolver un replay y antes de escribir ledger o negocio; carreras en el límite dejan exactamente 100 y la perdedora recibe el conflicto terminal `productive_core.management_unit_capacity_reached` sin efectos parciales.
- El listado lee como máximo 101 filas únicamente como sentinel de integridad y falla cerrado si datos privilegiados violan el límite; nunca trunca silenciosamente una lista válida.

## Atomicidad e idempotencia

- POST exige CSRF e `Idempotency-Key`. Se autoriza antes de buscar o reproducir el ledger.
- Mismo actor/organización/key/fingerprint reproduce el mismo recurso; cambiar payload produce conflicto neutral.
- ManagementUnit, ledger/aliases, journal local y outbox `ManagementUnitCreated` confirman en una transacción.
- El evento contiene sólo IDs internos, tipo, estado y fecha; omite nombre, coordenadas, actor, key, digest y payload.
- Retiro temprano de claves, alias split y commit incierto fallan cerrados y requieren reconciliación; no se promete exactly-once del transporte ni delivery runtime.

## Compatibilidad y rollout

- Migraciones aditivas y compatibles N/N-1; el binario anterior ignora el nuevo schema.
- El flag de creación queda apagado por defecto. Development/Test puede habilitar POST con secretos efímeros o locales explícitos; las lecturas continúan sujetas a sesión, owner vivo, schema aplicado y `FORCE RLS`.
- El rollback de aplicación apaga la escritura y conserva datos legibles por el binario compatible. `Down` destructivo sólo se permite en PostgreSQL efímero identificado; ambientes compartidos usan roll-forward.
- No hay deploy en este incremento. Auth0, edge, secretos administrados, principals de ambiente compartido y observabilidad operativa siguen como gates externos.
