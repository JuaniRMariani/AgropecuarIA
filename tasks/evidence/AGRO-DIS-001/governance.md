# Gobierno editorial de Catálogo Nacional v1

## Ownership y RACI

| Actividad | Accountable | Responsible | Consulted | Informed |
|---|---|---|---|---|
| Alcance y publicación | Sponsor/owner | WS-03 Product/Catalog Lead | WS-18, QA, Data | Equipos consumidores |
| Ingesta y evidencia | WS-03 | Data/Integrations | QA | Sponsor |
| Conflictos taxonómicos | WS-03 | Editor técnico | Agrónomo/veterinario según dominio | QA/Sponsor |
| Entrada regulada | Sponsor/owner | WS-03 | Especialista competente + Legal cuando aplique | Equipos consumidores |
| Validación del dataset | QA/Test Architect | QA Automation | Data/Product | Sponsor |
| Rollback de versión | Sponsor/owner | WS-03 | QA/Architecture | Consumidores |

El sponsor/owner es el usuario solicitante. La delegación permite resolver gobierno operativo, pero no atribuirle una firma agronómica, veterinaria o legal no declarada.

## Cadencia

- Publicación ordinaria trimestral.
- Revisión extraordinaria ante cambio oficial crítico, vulnerabilidad, error material o conflicto que afecte entradas activas.
- Revisión de fuentes y frescura en cada publicación.
- La versión candidata no se activa hasta completar revisión editorial/QA y las firmas profesionales necesarias para entradas reguladas o especializadas.

## Workflow

1. Capturar fuente, URL, fecha, metadatos HTTP, hash informativo del remoto y snapshot editorial local con locator por familia.
2. Ingresar a staging sin sobrescribir el valor de origen.
3. Normalizar nombre/código y proponer aliases.
4. Detectar duplicados, ambigüedades, cambios e inactivaciones.
5. Resolver o exceptuar con owner, motivo, evidencia y fecha.
6. Ejecutar el validador reproducible.
7. Revisar segregación, soporte, regulaciones y resultados.
8. Aprobar una versión inmutable y producir changelog.
9. Activar por referencia de versión; nunca modificar históricos.

## Segregación y autorización futura

- Ingesta, revisión y publicación son permisos distintos.
- El editor propone; el aprobador publica o rechaza.
- Extensiones tenant se almacenarán fuera del baseline global y no podrán sobrescribir códigos nacionales.
- La publicación y el rollback producirán auditoría append-only con actor, versión, hash, motivo y correlación.
- Ningún cliente define `tenant_id` ni convierte una extensión privada en entrada nacional.

## Conflictos y excepciones

Cada registro conserva: ID, fuente/versión, entradas afectadas, tipo, motivo, decisión, aprobador, fecha y estado. Las decisiones válidas son normalizar, conservar como alias, separar conceptos, inactivar/suceder o exceptuar. Nunca se borra una entrada usada ni se eleva soporte por excepción.

## Compatibilidad y rollback

- Los códigos internos son estables e irreutilizables.
- `reviewStatus` (`APPROVED`/`REVIEW_REQUIRED`) se separa de `lifecycleStatus` (`ACTIVE`/`INACTIVE`/`SUCCEEDED`).
- Una corrección incompatible crea una nueva entrada y `successorCode`; la anterior pasa a `SUCCEEDED`. El sucesor debe existir, pertenecer al mismo dominio y no formar ciclos.
- Rollback cambia la versión activa, no reescribe eventos históricos ni elimina entradas.
- El dataset candidato no requiere migración productiva. La futura publicación deberá probar lectura N/N-1 y referencias históricas.

## Criterios de publicación

- 100 % del denominador normalizado o exceptuado.
- Códigos únicos, fuentes existentes, aliases controlados y nivel de soporte explícito.
- Cero entrada regulada presentada como habilitación o especialización.
- Validación automatizada en verde y revisión QA independiente.
- Acta del sponsor y revisión profesional nominada antes de publicación productiva.
