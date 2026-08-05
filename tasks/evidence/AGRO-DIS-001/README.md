# Evidencia de AGRO-DIS-001 — Catálogo Nacional v1

Versión candidata: `1.0.0-candidate.1`  
Fecha de corte: 2026-08-04  
Estado: candidato para revisión editorial/profesional  
Accountable: sponsor/owner del producto  
Responsable operativo: WS-03 Product/Catalog Lead

## Propósito

Este directorio congela un denominador reproducible para `Catálogo Nacional v1`. No es código productivo, no publica el catálogo en una aplicación y no afirma automatización técnica o regulatoria. Cada entrada comienza en `CATALOGADA`; `FLUJO_GENERICO` y `ESPECIALIZADA_VALIDADA` requieren tareas posteriores y, para la última, un perfil profesional aprobado.

## Artefactos

- `catalog-entry.schema.json`: contrato único de ambos datasets.
- `catalog-plants-v1.json`: denominador vegetal explícito.
- `catalog-animals-v1.json`: denominador animal explícito.
- `sources-v1.json`: fuentes rectoras y evidencia de accesibilidad.
- `source-evidence-v1.json`: snapshot editorial local y mapeo reproducible fuente→familia.
- `exceptions-v1.json`: excepciones, placeholders y ambigüedades aceptadas o pendientes.
- `catalog-v1.manifest.json`: hashes y conteos del baseline congelado.
- `coverage-oracle-v1.json`: oráculo independiente de dimensiones animales explícitas en discovery.
- `governance.md`: RACI, workflow, publicación, rollback y cadencia.
- `catalog-publication-contract.json`: contrato conceptual de diff y `ProductCatalogPublished`, sin endpoint productivo.
- `validate-catalog.ps1`: validador reproducible del baseline.
- `validation-report.md`: evidencia de comandos, conteos y hallazgos.
- `prototype/`: prototipo Next.js/React R0 aislado y descartable para validar búsqueda, soporte, estados y accesibilidad; no es el bootstrap de la aplicación.

## Definición del denominador

El denominador v1 contiene cada identidad taxonómica o grupo productivo explícito de las tablas “Línea base agrícola v1” y “Línea base pecuaria v1” de `docs/14-catalogo-productivo-argentino.md`. Sistemas, orientaciones, propósitos, unidades de seguimiento y productos se conservan en `familyDimensions`, separados de especies/identidades; nunca se cuelgan de una entrada representativa arbitraria. Frases abiertas como “otros”, “otras autorizadas” y categorías que no designan un taxón único se representan como placeholders controlados o excepciones; no se convierten en una promesa de exhaustividad.

Una entrada cuenta como procesada si aparece exactamente una vez con código estable, dimensiones nominales y al menos una fuente respaldada por `source-evidence-v1.json`, o si está registrada en `exceptions-v1.json` con motivo, decisión, owner y fecha. El porcentaje de cobertura se calcula sobre identidades explícitas, dimensiones estructuradas y excepciones declaradas.

## Límites

- No se precargan exhaustivamente cultivares, variedades, razas ni líneas genéticas.
- No se habilitan reglas, KPI, recomendaciones, IA ni cumplimiento regulatorio.
- Fauna, cannabis/cáñamo y otras actividades reguladas permanecen `CATALOGADA` y exigen perfil jurisdiccional antes de elevar soporte.
- Una extensión tenant nunca modifica estos archivos ni sus códigos.
- Una entrada usada se inactiva o sucede; no se elimina ni reutiliza su código.
- El prototipo no persiste, autentica ni publica; usa únicamente el candidato local y debe reemplazarse en una tarea productiva posterior.

## Aprobaciones

La delegación del sponsor resuelve gobierno, cadencia y alcance del baseline. La revisión agronómica/veterinaria nominada sigue siendo necesaria antes de declarar la matriz firmada para publicación productiva. Hasta entonces el artefacto permanece `candidate` y la tarea no puede presentarse como publicación productiva.
