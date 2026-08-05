# Decisiones y preguntas de discovery

## Respondidas por el sponsor

| ID | Decisión | Estado |
|---|---|---|
| Q-001 | Nombre comercial: **AgropecuarIA**; carpeta `B:\Xenova\AgropecuarIA`. | Resuelta |
| Q-002 | Primer usuario de referencia: padre del sponsor, ingeniero agrónomo y productor. | Resuelta para piloto; ICP comercial pendiente |
| Q-003 | Cobertura de toda Argentina y todas las actividades/cultivos/especies productivas identificadas en la línea base nacional. | Resuelta como catálogo + flujo común |
| Q-004 | El piloto elige profundidad especializada, pero no limita qué producciones pueden registrarse. | Resuelta |
| Q-005 | No habrá emisión/integración ARCA por ahora. | Resuelta |
| Q-006 | Gestión económica con exportación al contador; no contabilidad legal completa. | Resuelta en alcance; falta formato |
| Q-007 | Pluviómetro/estación y medición de forraje no son obligatorios; deben poder cargarse opcionalmente. | Resuelta |
| Q-008 | El formato/software del contador se deja pendiente. | Pendiente explícito; no bloquea R1–R4 |
| Q-009 | El MVP funciona completamente online. | Resuelta |
| Q-010 | Clima/lluvia es obligatorio; satélite, catastro y mapas offline no fueron confirmados. | Parcial |
| Q-011 | IA prioritaria: clima/lluvias y rotación/alimentación ganadera contextual. | Resuelta para dos casos |

## Datos pendientes para ejecutar el piloto

Estas respuestas eligen perfiles, escala y validaciones; no reducen el catálogo nacional.

| ID | Pregunta | Impacto | Recomendación inicial |
|---|---|---|---|
| Q-012 | ¿Provincia/localidad y coordenadas aproximadas del campo piloto? | clima, suelo, perfiles y pruebas | usar el establecimiento real de referencia |
| Q-013 | ¿Qué cultivos y labores del catálogo usa el piloto? | elegir especializaciones iniciales | cubrir primero su campaña real sin ocultar las demás |
| Q-014 | ¿Ganadería de cría, recría, invernada, tambo, feedlot u otra en el piloto? | elegir perfil especializado | priorizar su sistema real |
| Q-015 | ¿Qué especies/categorías y escala maneja el piloto? | parámetros, fixtures y volumen | relevar su operación; catálogo nacional ya confirmado |
| Q-016 | ¿Puede aportar alguna medición puntual de pasto/biomasa para validar escenarios? | calibración de rotación | opcional; sin medición usar rangos y pedir inspección |
| Q-017 | ¿Puede aportar lluvia observada por algún medio? | calibración meteorológica | opcional; no bloquear clima ni operación |
| Q-018 | ¿Qué software/formato recibe el contador? | adaptador final utilizable | pendiente por decisión del sponsor |
| Q-019 | ¿Equipo, presupuesto y fecha objetivo? | alcance técnico y proveedor clima | resolver antes de estimar |
| Q-020 | ¿Qué volumen de campos, lotes, usuarios y documentos espera? | SLO/costos | diseñar con datos del piloto |

## Clima

- `Q-021`: ¿horizonte principal: próximas 24/72 horas, 7 días o 15 días?
- `Q-022`: ¿qué alertas necesita primero: lluvia, helada, calor, viento, tormenta, granizo o anegamiento?
- `Q-023`: ¿cada cuánto desea notificaciones y por qué canal?
- `Q-024`: ¿hay estaciones cercanas cuyos datos confíe el productor?
- `Q-025`: ¿se contratará Open-Meteo comercial para producción o se requiere otra alternativa/SLA?
- `Q-026`: ¿se autoriza procesar SMN WRF NetCDF como fallback, con su costo de almacenamiento/cómputo?
- `Q-027`: ¿qué tolerancia de error de lluvia considera útil para una decisión?
- `Q-028`: ¿qué labores deben usar ventanas climáticas en el MVP?
- `Q-029`: ¿satélite/NDVI entra en el MVP o queda posterior?
- `Q-030`: ¿las alertas se mostrarán solo en app o también email/WhatsApp/push?

## Ganadería, forraje y rotación

- `Q-031`: tipos de recurso: pastizal natural, pastura implantada, verdeo, rastrojo u otros.
- `Q-032`: ¿superficie efectiva, aguadas, sombra, alambrados y restricciones están relevados?
- `Q-033`: ¿se usa altura, corte/pesaje, plato medidor, estimación visual o NDVI para biomasa?
- `Q-034`: ¿biomasa/altura objetivo de entrada y remanente por recurso?
- `Q-035`: ¿factor de accesibilidad y eficiencia de cosecha usados por el asesor?
- `Q-036`: ¿peso vivo promedio y tasa de consumo por especie/categoría están disponibles?
- `Q-037`: ¿qué suplementos/reservas se registran y en qué unidad/materia seca?
- `Q-038`: ¿máximo de ocupación y mínimo/ventana de descanso por recurso/estación?
- `Q-039`: ¿qué riesgos bloquean un potrero: anegamiento, toxicidad, carencia, agua, alambrado, sanidad?
- `Q-040`: ¿se permitirán varios grupos/especies simultáneos en un potrero?
- `Q-041`: ¿qué síntomas/condiciones deben disparar consulta profesional urgente?
- `Q-042`: ¿quién aprueba los perfiles: agrónomo, veterinario o ambos?

## Agricultura

- `Q-043`: ¿qué perfiles agrícolas del catálogo nacional se especializan primero?
- `Q-044`: ¿qué combinación del piloto validará agricultura extensiva, intensiva, fruticultura, horticultura u otra?
- `Q-045`: ¿receta agronómica oficial? ¿En qué provincias?
- `Q-046`: ¿granos almacenados, silo bolsa, calidad, canjes, forwards y liquidaciones?
- `Q-047`: ¿riego/agricultura de precisión/archivos de maquinaria desde qué etapa?

## Gestión económica y contador

- `Q-048`: criterio caja, devengado o ambos; calendario de cierre.
- `Q-049`: moneda funcional/reporte, fuente de cotización e inflación.
- `Q-050`: métodos de valuación para hacienda, granos, insumos, tierra y maquinaria.
- `Q-051`: reglas de costos indirectos y centros de costo.
- `Q-052`: campos/columnas, plan de cuentas y totales de control requeridos por el contador.
- `Q-053`: ¿CSV, XLSX o importador específico? ¿Cómo se entregan documentos originales?

## Producto, IA y operación

- `Q-054`: ¿el cliente comercial inicial será productor individual, empresa o estudio agronómico multi-cliente?
- `Q-055`: ¿quién es dueño de datos cuando propietario, productor y asesor son distintos?
- `Q-056`: ¿qué tercera decisión concreta debería asistir la IA después de clima y rotación?
- `Q-057`: ¿qué acciones puede preparar como borrador y cuáles solo explicar?
- `Q-058`: ¿se autoriza proveedor internacional de IA/clima y en qué región?
- `Q-059`: ¿cómo se evaluará si una recomendación fue correcta o útil?
- `Q-060`: ¿SLA, RPO/RTO, soporte y retención requeridos?
- `Q-061`: ¿la conectividad del campo fue medida? Offline queda fuera, pero debe registrarse el riesgo.
- `Q-062`: ¿quién será owner editorial y aprobará conflictos/excepciones de `Catálogo Nacional v1`?
- `Q-063`: ¿qué cadencia de actualización tendrán catálogo y perfiles?
- `Q-064`: ¿se aprueba la separación visible `CATALOGADA` / `FLUJO_GENERICO` / `ESPECIALIZADA_VALIDADA`?
- `Q-065`: ¿la cobertura nacional exige solo registro/operación o automatización regulatoria provincial? Recomendación: automatizar únicamente perfiles validados.
- `Q-066`: ¿también se exige precargar exhaustivamente variedades, razas y líneas genéticas, o basta sincronizarlas/crearlas bajo demanda desde fuentes oficiales?

## Taller recomendado

Participantes: sponsor, ingeniero agrónomo/productor de referencia, referente ganadero o veterinario y contador. Material real anonimizado:

- mapa y lotes/potreros;
- última campaña agrícola;
- rodeos/categorías/pesos;
- mediciones de forraje, si existen, y calendario de pastoreo;
- lluvia observada, si existe, y decisiones tomadas después de eventos climáticos;
- gastos/ingresos; el archivo del contador se incorpora cuando esté disponible.

Resultado: respuestas operativas Q-012–Q-020, perfiles iniciales versionados, dataset piloto y criterios de éxito. El formato contable puede continuar pendiente sin bloquear las primeras releases. Después validar con un segundo productor para reducir sesgo del caso familiar.
