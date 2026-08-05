# ADR-006 — Catálogo productivo nacional y perfiles especializados

## Estado

Aceptado para el discovery.

## Contexto

El sponsor requiere que AgropecuarIA permita trabajar con las actividades, cultivos y especies productivas existentes en Argentina. Una enumeración rígida queda obsoleta y un único modelo lote/cultivo/rodeo no representa invernaderos, rodales, apiarios, galpones o estanques. A la vez, afirmar soporte técnico exhaustivo para cada producción sería inseguro e imposible de validar dentro del MVP.

## Decisión

- Mantener un catálogo nacional versionado, trazable y actualizable desde fuentes oficiales.
- Separar `CATALOGADA`, `FLUJO_GENERICO` y `ESPECIALIZADA_VALIDADA`.
- Incorporar un núcleo `ProductiveActivity` + `ManagementUnit` + `ProductionCycle/Event/Output`.
- Modelar agricultura, rodeos, apiarios, lotes acuáticos y otros como extensiones del núcleo.
- Habilitar reglas, KPI, IA y normativa solo mediante perfiles versionados con jurisdicción y aprobación.
- Permitir extensiones privadas sin alterar silenciosamente el catálogo nacional.

## Alternativas descartadas

- `enum` cerrado de cultivos/especies: exige despliegues, pierde sinónimos/vigencia y no cubre nuevas producciones.
- Construir un módulo especializado por cada entrada en el MVP: alcance no verificable y alto riesgo técnico/regulatorio.
- Forzar todas las producciones a lote/rodeo: genera unidades, eventos y recomendaciones engañosas.
- Campos libres sin esquema: flexibles al principio, pero imposibles de validar, comparar o migrar con seguridad.

## Consecuencias

- Toda producción nacional puede registrarse mediante un flujo común desde el MVP.
- La UI debe hacer visible el nivel de soporte y las capacidades ausentes.
- Catálogo y perfiles requieren gobierno editorial, changelog, pruebas parametrizadas y rollback.
- Los atributos especializados usan schemas versionados; los datos comunes siguen fuertemente tipados.
- El piloto decide qué perfiles se profundizan, no qué producciones existen en el producto.

## Revisión

Revisar antes de publicar `Catálogo Nacional v1`, ante cambios relevantes de CNA/RENSPA/INASE/INV o cuando una actividad de alta demanda requiera pasar de genérica a especializada.

### Candidato verificable de AGRO-DIS-001 — 2026-08-04

Se construyó `1.0.0-candidate.1` como baseline reproducible: contrato JSON, 154 entradas vegetales, 59 animales, 13 familias con 205 términos dimensionales, 10 fuentes oficiales, evidencia local de cobertura, excepciones, gobierno, manifiesto SHA-256 y validador fail-closed. Las dimensiones de una familia se mantienen separadas de la identidad de cada entrada y la búsqueda distingue resultados `ENTRY` de `FAMILY_DIMENSION`.

La evidencia está en [`tasks/evidence/AGRO-DIS-001`](../../tasks/evidence/AGRO-DIS-001/README.md). El candidato permanece sin publicación productiva y este ADR conserva el estado “Aceptado para el discovery” hasta contar con revisión nominada de agronomía y veterinaria para el baseline y con el acta editorial exigida por la Definition of Done. Las entradas reguladas o abiertas siguen en `REVIEW_REQUIRED`; ninguna eleva por sí sola el soporte a `ESPECIALIZADA_VALIDADA`.
