# Plan de discovery — AgropecuarIA

Fecha de inicio: 2026-08-04  
Estado: completado  
Directorio: `B:\Xenova\AgropecuarIA`  
Nombre del producto: `AgropecuarIA`, confirmado por el sponsor.

## Plan verificable

- [x] Confirmar el directorio destino sin modificar proyectos existentes.
- [x] Crear la estructura documental inicial.
- [x] Investigar fuentes oficiales actuales sobre ARCA, identidad, GIS y normativa argentina.
- [x] Definir visión, alcance, actores, vocabulario y supuestos.
- [x] Especificar módulos, requisitos funcionales y reglas de negocio.
- [x] Modelar entidades, relaciones, trazabilidad y eventos principales.
- [x] Definir requisitos no funcionales, seguridad, privacidad y auditoría.
- [x] Proponer arquitectura, integraciones, estrategia de IA y ADR iniciales.
- [x] Diseñar estrategia QA, matriz de pruebas y criterios de salida.
- [x] Priorizar MVP, releases, dependencias, riesgos y métricas de producto.
- [x] Consolidar preguntas de discovery y decisiones pendientes.
- [x] Verificar enlaces, estructura, consistencia y cobertura del paquete documental.

## Restricciones de esta etapa

- La entrega es discovery y especificación; no incluye código productivo ni aprovisionamiento cloud.
- No se emitirán comprobantes reales ni se conectará una cuenta ARCA durante el discovery.
- Las decisiones legales, contables e impositivas se documentarán para validación profesional; no se presumirán.
- Los proveedores tecnológicos serán sustituibles hasta validar presupuesto, escala y restricciones comerciales.

## Revisión

- Se creó el paquete documental en `B:\Xenova\AgropecuarIA`.
- La documentación cubre visión, dominio, requisitos funcionales, reglas, modelo conceptual, arquitectura, integraciones, seguridad, IA, NFR, QA, roadmap, preguntas, fuentes y seis ADR.
- Se investigaron fuentes oficiales de ARCA, SENASA, AAIP, IGN, W3C, NIST, Google, PostGIS, OSM, NASA, Microsoft, Next.js y OpenTelemetry.
- Se validaron todos los enlaces Markdown internos, unicidad de IDs, cierres de code fences, UTF-8 y ausencia de archivos vacíos.
- No se creó código productivo, no se inicializó Git, no se conectaron credenciales y no se modificó ningún otro proyecto de `B:\Xenova`.

### Riesgos residuales

- Escala, equipo, presupuesto y nivel de detalle por sistema productivo siguen pendientes.
- Facturación ARCA quedó fuera del MVP y las finanzas se limitan a gestión/exportación al contador.
- Toda integración ARCA/SENASA debe probar acceso real y homologación; un manual público no garantiza autorización.
- Reglas fiscales, valuaciones, retención documental y recetas profesionales requieren validación contable/legal/técnica.
- Proveedor productivo de clima, satélite, identidad e IA sigue sujeto a presupuesto/validación.

## Iteración 2 — decisiones del sponsor (2026-08-04)

- [x] Registrar nombre `AgropecuarIA` y perfil del primer usuario de referencia.
- [x] Actualizar el MVP para incluir agricultura y ganadería, sin facturación ARCA.
- [x] Limitar finanzas a gestión y exportación al contador.
- [x] Retirar offline del MVP y mantener arquitectura preparada sin implementarlo ahora.
- [x] Investigar y seleccionar estrategia de API meteorológica para lluvias y contexto zonal.
- [x] Especificar rotación de ganado, alimentación, potreros, forraje y recomendaciones contextualizadas.
- [x] Actualizar requisitos, reglas, modelo, arquitectura, IA, QA, roadmap, preguntas y fuentes.
- [x] Verificar enlaces internos, UTF-8, code fences, IDs y contradicciones del alcance.

### Decisiones confirmadas

- Producto: `AgropecuarIA`.
- Usuario inicial de referencia: ingeniero agrónomo que además es productor.
- MVP: catálogo nacional y flujo común para todas las producciones identificadas; el piloto define qué perfiles reciben especialización primero.
- Facturación ARCA: fuera del MVP por ahora.
- Finanzas: gestión operativa y exportación al contador; no contabilidad legal completa.
- Conectividad: todo online en el MVP.
- IA prioritaria: pronóstico meteorológico/lluvias y recomendaciones de rotación/alimentación ganadera según zona, clima, potreros y rodeos.

### Revisión de la iteración 2

- Se renombró de forma segura la carpeta a `B:\Xenova\AgropecuarIA`; no se modificaron otros proyectos.
- El MVP quedó definido como online, con agricultura y ganadería en sus flujos comunes, clima y rotación ganadera como capacidades obligatorias.
- La estrategia meteorológica propone Open-Meteo comercial para pronóstico, alertas oficiales SMN CAP y un spike de SMN WRF como respaldo nacional.
- La rotación se especificó mediante datos medidos y versionados de potreros, forraje, rodeos, clima y restricciones; la IA no prescribe raciones ni mueve animales automáticamente.
- Facturación e integración ARCA quedaron fuera del MVP. Finanzas cubre gestión y exportación conciliable al contador.
- Se validaron 22 archivos Markdown, 119894 bytes y 165 definiciones de requisitos/reglas: cero errores de UTF-8, archivos vacíos, code fences, enlaces internos o IDs duplicados.
- No se creó código productivo, repositorio Git, credenciales ni infraestructura.

## Iteración 3 — catálogo productivo nacional (2026-08-04)

Estado: completado.

- [x] Registrar la corrección del sponsor y actualizar las lecciones.
- [x] Investigar actividades agrícolas de Argentina con fuentes oficiales.
- [x] Investigar especies, categorías y sistemas pecuarios de Argentina con fuentes oficiales.
- [x] Diseñar catálogos nacionales configurables sin confundir amplitud con profundidad funcional.
- [x] Marcar pluviómetro y medición forrajera como opcionales, preservando límites de recomendación segura.
- [x] Mantener pendiente el software/formato de exportación al contador.
- [x] Actualizar visión, dominio, requisitos, reglas, datos, IA, QA, roadmap, discovery y fuentes.
- [x] Ejecutar validaciones documentales y revisión final de consistencia.

### Revisión de la iteración 3

- Se agregó `docs/14-catalogo-productivo-argentino.md` con cobertura territorial nacional, taxonomías vegetales/animales, unidades de manejo, niveles de soporte, gobierno de datos y criterios de aceptación.
- Se agregó ADR-006 para separar catálogo nacional, flujo genérico y especialización validada.
- La línea base contempla agricultura extensiva/intensiva, fruticultura, horticultura, forrajes, forestación, viveros, ganadería doméstica, aves, porcinos, apicultura, acuicultura, fauna autorizada y producciones menores.
- Georef, CNA, SENASA, INASE, INV, SAGyP e INTA quedaron definidos como fuentes complementarias y versionadas; ninguna se presenta sola como lista exhaustiva.
- Pluviómetro y biomasa son cargas opcionales. Su ausencia reduce calibración/confianza, pero solo los faltantes de seguridad bloquean el ingreso ganadero.
- El formato del contador continúa pendiente; se diseñó un paquete canónico para no bloquear las releases anteriores al adaptador.
- Validación documental: 24 Markdown, 109 RF, 78 RN, 29 RNF y 66 preguntas únicas; cero errores de UTF-8, archivos vacíos, code fences, enlaces internos, IDs duplicados o contradicciones de alcance buscadas.
- No se creó código, Git, credenciales ni infraestructura.

## Iteración 4 — prompt de planificación para sesión nueva (2026-08-04)

Estado: completado.

- [x] Leer instrucciones, lecciones, discovery y quality gates aplicables.
- [x] Crear un prompt autocontenido con roles senior y estrategia obligatoria de subagentes.
- [x] Definir entregables Markdown, formato de tareas, dependencias, criterios de aceptación y validación.
- [x] Impedir implementación, commits o cambios externos durante la sesión de planificación.
- [x] Revisar el prompt contra el alcance completo de AgropecuarIA.
- [x] Imprimir el prompt completo por consola y registrar la evidencia.

### Revisión de la iteración 4

- Se creó `prompts/01-planificar-implementacion-desde-cero.md`, autocontenido para una sesión sin historial conversacional.
- Define un rol principal multidisciplinario con más de 20 años y diez frentes de subagentes con experiencia senior/principal.
- Exige lectura completa, trazabilidad RF/RN/RNF/ADR, vertical slices, backlog implementable, QA, seguridad, GIS, IA, DevOps y revisión independiente.
- Restringe la sesión futura a Markdown bajo `tasks/`; prohíbe código, scaffolding, SQL, scripts, configuraciones, workflows, Git, infraestructura y credenciales.
- Validación del prompt: UTF-8 estricto, 289 líneas, marcadores únicos, contenido obligatorio presente y cero errores.
- El contenido completo se imprimió mediante PowerShell `Get-Content -Raw -Encoding UTF8`.

## Iteración 5 — planificación detallada de implementación (2026-08-04)

Estado: completado.

### Plan verificable de la sesión

- [x] Confirmar `B:\Xenova\AgropecuarIA`, preservar el trabajo existente y comprobar de forma no mutante que Git no está inicializado.
- [x] Leer íntegramente `AGENTS.md`, `README.md`, `tasks/todo .md`, `tasks/lessons .md`, `docs/*.md`, `docs/adr/*.md` y los enlaces internos necesarios.
- [x] Inventariar el discovery: alcance, releases, RF/RN/RNF, ADR, preguntas y decisiones confirmadas/pendientes.
- [x] Ejecutar olas de análisis con los diez frentes senior obligatorios, cruzar hallazgos y mantener la integración/decisión final en el agente principal.
- [x] Construir el inventario de alcance y la matriz inicial; separar MVP, posterior y `Won't now`, con excepciones explícitas.
- [x] Definir secuencia por releases y dependencias; registrar spikes y decisiones que bloquean únicamente slices concretos.
- [x] Redactar `tasks/implementation-plan.md`, `tasks/release-plan.md`, `tasks/risk-register.md`, `tasks/decisions-and-gaps.md` y `tasks/team-workstreams.md`.
- [x] Crear `tasks/backlog/00-index.md` y un archivo por épica, con tareas `AGRO-<EPICA>-<NNN>` verticales, verificables y completas según el formato requerido.
- [x] Redactar `tasks/test-strategy.md` y `tasks/traceability-matrix.md`, trazando el 100 % de RF/RN/RNF o una excepción justificada.
- [x] Cruzar tareas, releases, criterios, pruebas, seguridad, autorización, observabilidad, migración/rollback y preguntas abiertas para eliminar duplicación y secuencias imposibles.
- [x] Solicitar una auditoría final independiente a un subagente QA/reviewer y corregir todos los hallazgos reales.
- [x] Validar UTF-8, archivos no vacíos, enlaces, code fences, IDs únicos, rutas del índice, owners, criterios, pruebas, tamaños, dependencias y cobertura calculada.
- [x] Confirmar que solo se modificaron Markdown bajo `tasks/`, registrar comandos/resultados/limitaciones/riesgos residuales y completar la autoevaluación ≥ 90/100.

### Criterios de salida

- Todos los entregables obligatorios existen, están enlazados y tienen una responsabilidad canónica.
- Cada `Must`, RN y RNF tiene tarea y prueba en un release del MVP, o una excepción explícita; `Should`, `Could` y `Won't now` conservan su prioridad documental.
- Ninguna tarea implementable queda sin owner, criterios observables, pruebas, controles tenant/autorización, telemetría y estrategia de rollout/recuperación aplicable.
- El formato/software del contador permanece pendiente sin bloquear el paquete canónico; ARCA y offline permanecen fuera del MVP.
- La revisión independiente y los quality gates documentales terminan sin fallos críticos.

### Revisión de la iteración 5

#### Resultado

- Se planificaron 8 releases (`R0`–`R7`), 18 épicas y 81 vertical slices con tamaño XS/S/M/L; no existen tareas XL.
- Se crearon o actualizaron 28 archivos Markdown bajo `tasks/`: 18 archivos de épica, un índice navegable a cada tarea y los nueve documentos canónicos transversales/este registro.
- La matriz recalculada desde las fuentes contiene 109 RF, 78 RN y 29 RNF: 216/216 trazados, 207 con tarea/prueba y 9 excepciones explícitas (`RF-GIS-010`, `RF-FIN-009`, `RN-FIS-001–007`). Los 6/6 ADR originales están trazados.
- Se ejecutaron en varias olas los diez frentes requeridos: producto/dominio, arquitectura, backend/datos, frontend/UX, GIS/clima, IA/analítica, QA, seguridad/privacidad, plataforma/SRE y validación profesional. La integración y las decisiones finales quedaron en el agente principal.
- El formato/software del contador permanece pendiente (`Q-018`, `Q-052`, `Q-053`); bloquea solo el adaptador específico, no el paquete canónico. ARCA y offline permanecen `Won't now`.

#### Correcciones surgidas de la revisión cruzada

- Se adelantaron a R2 los kernels mínimos de inventario e imputación de costos para evitar dependencias imposibles de las operaciones.
- Se separaron slices R7 explícitos para calendario/carga masiva (`AGRO-AGR-006`), escenarios avanzados (`AGRO-IA-006`) y soporte JIT (`AGRO-ID-005`), evitando que requisitos posteriores quedaran nominalmente trazados a tareas que los excluían.
- Se eliminó el ciclo `DOC-003 ↔ SEC-004`, se corrigieron referencias `DIS-*`, se creó `ADR-PEND-011` para ownership de `ManagementUnit` y se reservaron `ADR-PEND-007` para RLS y `ADR-PEND-010` para compatibilidad de migraciones.
- Se normalizó el idioma tras detectar y revertir una transformación mecánica defectuosa; la corrección final fue contextual por archivo. También se aclaró el fixture EICAR y se agregaron anclas estables para las 81 tareas.

#### Quality gates y evidencia

| Gate | Resultado final |
|---|---|
| UTF-8 estricto, no vacíos y alcance de archivos | 28/28 archivos bajo `tasks/` son `.md`, válidos y no vacíos; no se generó código productivo. |
| IDs, formato y tamaño | 81 tareas, 81 IDs únicos, 26 campos requeridos no vacíos por tarea, owner/criterios/pruebas presentes, 0 XL. |
| Índice y enlaces | 81/81 tareas enlazadas mediante anclas; rutas internas y code fences válidos. |
| Dependencias | Sin ciclos; secuencia R0–R7 revisada y dependencias imposibles corregidas. |
| Trazabilidad | 109 RF + 78 RN + 29 RNF = 216/216, sin faltantes ni extras; 6/6 ADR. |
| Idioma | Prosa en español; permanecen únicamente términos técnicos habituales. |
| Auditoría independiente | Aprobada sin hallazgos críticos, altos ni medios luego de resolver tres hallazgos finales. |
| Git | No existe `.git`; `git status --short` y `git diff --check` no aplican y no se inicializó repositorio. |

Comandos/recursos de solo lectura usados para inspección y validación: `Test-Path`, `Get-ChildItem`, `Get-Content -Raw -Encoding UTF8`, `rg` y validadores PowerShell sobre IDs, campos, UTF-8, referencias, enlaces, anclas, fences, extensiones y cobertura expandida. Hubo errores intermedios de sintaxis en validadores PowerShell y un intento fallido de parche masivo; se detuvieron, corrigieron y repitieron hasta obtener el resultado final sin errores. Las ediciones se realizaron con `apply_patch` exclusivamente sobre Markdown autorizado bajo `tasks/`.

Limitación de atribución: sin historial Git no puede probarse el origen temporal de `prompts/02-implementar-codigo-sesion-nueva.md`, observado como trabajo concurrente/preexistente y preservado sin cambios por esta iteración. No forma parte de estos entregables.

Riesgos/decisiones residuales que requieren sponsor o especialistas: datos y escala piloto (`Q-012–020`), clima/canales/proveedores/WRF (`Q-021–030`), perfiles y parámetros profesionales (`Q-031–047`), políticas económicas (`Q-048–051`), UX/IdP/IA/privacidad/SLO/gobierno (`Q-054–066`) y formato del contador ya señalado. Ninguno bloquea la existencia del backlog; cada uno tiene gate, owner y contingencia de abstención/degradación.

#### Autoevaluación

| Dimensión | Puntaje |
|---|---:|
| Contexto y lectura completa | 15/15 |
| Límites y ausencia de código | 15/15 |
| Subagentes y roles | 15/15 |
| Cobertura | 20/20 |
| Calidad del backlog | 18/20 |
| Verificación y trazabilidad | 14/15 |
| **Total** | **97/100** |

Resultado: planificación aprobada para iniciar implementación por vertical slices, sujeta a los gates y decisiones pendientes registrados.

## Iteración 6 — prompt para sesión implementadora (2026-08-04)

Estado: completado.

- [x] Leer instrucciones, lecciones y skills de orquestación/arquitectura limpia.
- [x] Diseñar el rol principal y los subagentes implementadores con ownership disjunto.
- [x] Definir selección de vertical slice, flujo de implementación y límites de autoridad.
- [x] Incorporar Clean Architecture pragmática, SOLID, patrones y reglas anti-sobreingeniería.
- [x] Incorporar quality gates de .NET, Next.js, datos, seguridad, accesibilidad y operación.
- [x] Revisar el prompt de forma independiente, validar UTF-8/contenido e imprimirlo por consola.

### Revisión de la iteración 6

- Se creó `prompts/02-implementar-codigo-sesion-nueva.md` para una sesión implementadora sin contexto conversacional.
- Define selección determinística de una única tarea `Ready`, fallback seguro y ejecución mediante vertical slice completo.
- Coordina diez roles senior/principal en tres olas con ownership de archivos disjunto y revisión independiente.
- Incorpora monolito modular, Clean Architecture pragmática, SOLID, Adapter/Strategy/Outbox/Problem Details/concurrencia y reglas explícitas contra sobreingeniería.
- Incluye estándares production-ready para ASP.NET Core, Next.js/TypeScript, PostgreSQL/PostGIS, seguridad, IA, accesibilidad, observabilidad, tests y migraciones.
- Protege el worktree y prohíbe Git destructivo, commit/push/deploy no solicitados, secretos, bases productivas y cambios fuera de `B:\Xenova\AgropecuarIA`.
- Validación: UTF-8 estricto, 460 líneas, 26044 caracteres, seis delimitadores de code fence balanceados, marcadores únicos y cero requisitos obligatorios faltantes.
- El contenido completo se imprimió mediante PowerShell `Get-Content -Raw -Encoding UTF8`.

## Iteración 7 — actualizar prompt para iniciar implementación (2026-08-04)

Estado: completado.

### Plan verificable

- [x] Releer instrucciones, lecciones, skill de orquestación, backlog final y prompt implementador existente.
- [x] Contrastar el prompt contra las 81 tareas, el estado greenfield y las dependencias/gates de R0–R1.
- [x] Hacer inequívoca la selección de tarea, incluyendo estados `Propuesto`, `Ready`, bloqueado, en curso y completado.
- [x] Definir un arranque greenfield seguro cuando todavía no existen solución, Git ni comandos ejecutables.
- [x] Reforzar ejecución de una tarea por sesión, ownership de subagentes, trazabilidad y actualización de estado/evidencia.
- [x] Preservar límites: sin inventar decisiones profesionales, sin commit/push/deploy/credenciales y sin cambios fuera del proyecto.
- [x] Solicitar revisión independiente del prompt y corregir hallazgos.
- [x] Validar UTF-8, marcadores, enlaces/rutas, code fences, coherencia de IDs y contenido obligatorio.
- [x] Documentar resultado, comandos y riesgos residuales en esta sección.

### Revisión de la iteración 7

#### Resultado

- Se actualizó [`prompts/02-implementar-codigo-sesion-nueva.md`](../prompts/02-implementar-codigo-sesion-nueva.md) para que una sesión nueva implemente como máximo una tarea explícita y con Definition of Ready demostrada.
- `TAREA_OBJETIVO=AUTO` quedó como auditoría read-only: no cambia estados, no publica plan mutante, no crea código/scaffolding y solicita un ID explícito.
- Se definieron las transiciones `Propuesto → Ready → En curso → En revisión → Completada` y `Bloqueada`, con autoridad, evidencia y fallos seguros ante IDs inválidos.
- Se diferenciaron tareas R0, R1–R6, R7, mixtas y multirelease. Los prototipos R0 son descartables y no pueden convertirse en bootstrap productivo por inferencia.
- El arranque `GREENFIELD_DOCUMENTADO` permite scaffolding mínimo únicamente dentro de una tarea explícita Ready; prohíbe módulos vacíos, bootstrap implícito, Git/CI/infraestructura y decisiones de proveedor no autorizadas.
- Se reforzaron ownership de archivos, inventario pre/post cuando no existe Git, gates aplicables con `N/A` justificado, comandos .NET como patrones detectables y autoevaluación no sustitutiva.

#### Revisión multiagente

- Producto/dominio detectó que las 81 tareas estaban `Propuesto`, que ninguna R1 podía autoaprobar sus gates R0 y que `AUTO` debía detenerse en diagnóstico.
- Arquitectura/delivery eliminó el fallback de enabler/bootstrap, acotó una tarea por sesión y precisó greenfield, estados, ownership y gates.
- La auditoría final independiente de backend/datos señaló un hallazgo alto y tres medios: pedido por épica/release sin ID, ID inexistente, tareas multirelease y uso de `En revisión`. Los cuatro fueron corregidos y la segunda pasada quedó **aprobada sin hallazgos críticos ni altos bloqueantes**.

#### Quality gates y evidencia

| Gate | Resultado |
|---|---|
| UTF-8 y contenido | UTF-8 estricto, archivo no vacío, 530 líneas y 36.776 caracteres. |
| Estructura Markdown | 6 delimitadores de code fence balanceados; un marcador de inicio y uno de fin. |
| Contenido obligatorio | Presentes `AUTO` read-only, `MAXIMO_TAREAS=1`, estados, clasificación R0–R7, greenfield, DoR, DoD y quality gates; cero patrones mutantes detectados dentro del bloque `AUTO`. |
| Rutas documentales | Existen todos los archivos referenciados: instrucciones, README, planes, estrategia de pruebas, matriz e índice. |
| Alcance | 0 archivos de código/configuración productiva detectados; solo se editaron el prompt solicitado y este registro Markdown. |
| Git | `.git` no existe; no se inicializó. `git status --short` y `git diff --check` no aplican. |
| Revisión independiente | Aprobada tras corregir todos los hallazgos reportados. |

Comandos/recursos de verificación: `Get-Content -Encoding UTF8`, `rg`, `rg --files`, `Test-Path`, lectura UTF-8 estricta mediante APIs de .NET y validadores PowerShell de marcadores, fences, secciones obligatorias, rutas, patrones `AUTO` y tipos de archivo. Las ediciones se realizaron con `apply_patch`.

Limitación: al no existir Git no hay diff histórico confiable; la atribución se sostuvo mediante el inventario de la sesión y la revisión explícita de las dos rutas modificadas. No se escribió código, no se inicializó Git, no se hizo commit/push y no se desplegó infraestructura.

## Iteración 8 — AGRO-DIS-001 Catálogo Nacional v1 (2026-08-04)

Estado inicial: `Propuesto`; tarea seleccionada explícitamente por el sponsor. Alcance: una única tarea R0, sin bootstrap ni código productivo.

### DoR y decisiones de entrada

- [x] Confirmar ID único `AGRO-DIS-001`, release R0, prioridad Must y tamaño M.
- [x] Confirmar fuentes rectoras accesibles y alcance trazado a RF-CAT-001–005, RN-CAT-001–005, RNF-CAT-001/002 y ADR-006.
- [x] Nominar sponsor/owner accountable y WS-03 Product/Catalog Lead como responsable operativo.
- [x] Cerrar operativamente Q-062–Q-066 mediante delegación explícita del sponsor.
- [x] Acordar cadencia trimestral + urgencias y formato trazable de conflicto/excepción.
- [x] Preservar la frontera: catálogo/flujo común nacional; especialización solo con validación profesional.

### Plan verificable

- [x] Definir contrato versionado de entrada, fuente, alias, excepción, métricas y publicación.
- [x] Construir el denominador reproducible de `Catálogo Nacional v1` a partir del alcance nominal aprobado.
- [x] Crear manifiesto de fuentes con fecha, URL, alcance, autoridad y hash reproducible de la evidencia local.
- [x] Normalizar entradas vegetales y animales con códigos internos estables, soporte, vigencia, jurisdicción y procedencia.
- [x] Registrar aliases, deduplicaciones, placeholders y excepciones sin afirmar exhaustividad eterna.
- [x] Documentar RACI, segregación editorial, workflow, cadencia, rollback lógico y compatibilidad.
- [x] Implementar y ejecutar validadores del dataset, unicidad, referencias, soporte, alias y cobertura 100 % normalizada/exceptuada.
- [x] Ejecutar búsqueda por nombre/alias/tildes/código y casos negativos representativos.
- [x] Integrar revisión independiente de QA y AppSec/Arquitectura; resolver hallazgos.
- [x] Registrar evidencia, riesgos residuales, comandos y estado final de la única tarea.

### Ownership exclusivo

- Principal: `tasks/todo .md`, backlog, decisiones/gaps, contrato compartido, integración y estado.
- Product/Data vegetal: dataset vegetal, sin editar archivos compartidos.
- Product/Data animal: dataset animal, sin editar archivos compartidos.
- QA Automation: validador y evidencia de ejecución, sin modificar datasets salvo reporte de hallazgos.
- Revisión final: QA y AppSec/Arquitectura en modo read-only sobre el estado integrado.

### Comandos previstos

- Parseo estricto de JSON y UTF-8 mediante PowerShell/.NET local.
- Validador versionado de códigos, fuentes, aliases, estados, niveles de soporte, duplicados y excepciones.
- Búsquedas reproducibles por código, nombre, nombre sin tildes y alias.
- Inventario/hash pre/post porque no existe Git.
- Backend `.NET`, migraciones y contenedores: N/A para esta tarea R0 sin servicio productivo. Next.js/React sí aplica al prototipo explícito de validación y se verifica con sus scripts locales.

### Revisión

Revisión inicial independiente: la evidencia técnica del catálogo pasó, pero se detectaron dos entregables internos explícitos todavía ausentes. La tarea vuelve de `En revisión` a `En curso` hasta resolverlos.

- [x] Crear un prototipo Next.js/React R0 aislado y descartable para búsqueda/soporte con estados loading, vacío, error, fuente stale y accesibilidad responsive.
- [x] Definir el contrato conceptual versionado de diff y `ProductCatalogPublished` sin exponerlo como API productiva.
- [x] Incluir ambos artefactos en el manifiesto y ampliar los gates automatizados.
- [x] Ejecutar build/lint/typecheck y validar el flujo en navegador real.
- [x] Repetir revisión independiente sobre el estado integrado.

#### Resultado y evidencia final

- Se produjo `1.0.0-candidate.1` con 154 entradas vegetales, 59 animales, 213 totales, 31 reguladas, 3 excepciones documentadas, 10 fuentes/evidencias y 205 dimensiones familiares.
- El oráculo ejecutó 637 fixtures de búsqueda y cobertura con 0 fallos. La búsqueda diferencia entradas publicables de dimensiones de familia y normaliza código, mayúsculas, tildes y aliases.
- El prototipo Next.js 16.3.0 + React 19.2.8 es R0, aislado y descartable. Lee evidencia local exclusivamente del servidor y demuestra búsqueda, niveles de soporte, evidencia, confianza, faltantes y estados normal/loading/empty/error/stale, incluido stale simultáneo con cero resultados.
- El contrato conceptual documenta diff versionado y `ProductCatalogPublished`; no es una API, publicación ni integración productiva.
- La revisión independiente de Principal QA y AppSec/Arquitectura aprobó el entregable técnico sin hallazgos críticos, altos ni medios. Queda fuera de esa aprobación la firma profesional nominada sobre la semántica completa del baseline.

#### Quality gates ejecutados

| Gate | Resultado |
|---|---|
| `npm ci --ignore-scripts --no-audit --no-fund` | PASS; 350 paquetes instalados desde lockfile. |
| `npm run lint` | PASS; 0 errores. |
| `npm run typecheck` | PASS; TypeScript estricto. |
| `npm test` | PASS; 9/9 pruebas, 0 fallos. |
| `npm run build` | PASS; rutas estáticas `/`, `/_not-found` e `/icon.svg`. |
| `npm audit --audit-level=high` | PASS; 0 vulnerabilidades. |
| Parseo JSON/UTF-8 | PASS; 0 errores. |
| `validate-catalog.ps1` | PASS; 35 artefactos estables, 637 fixtures y 0 fallos. |
| Navegador Playwright, 390 × 844 | PASS; HTTP 200, 213 entradas, búsqueda `ponedoras` = 1, estados y teclado/foco verificados, 0 errores/warnings de consola. |
| Backend `.NET`, migraciones, DB, contenedores y telemetría productiva | N/A; `AGRO-DIS-001` es una validación R0 y no autoriza un servicio productivo. |

#### Compatibilidad, rollback y riesgos residuales

- La compatibilidad es lógica y versionada: no se mutó una base ni se publicó un evento real. El rollback consiste en conservar/republicar la versión candidata previa según `governance.md`; no aplica migración física.
- `next-env.d.ts` se valida semánticamente pero no integra el manifiesto de hashes porque Next alterna su contenido entre desarrollo y build. El orden reproducible es build, parseo y validación del catálogo.
- Las páginas remotas pueden cambiar; los hashes protegen la evidencia local capturada, no garantizan que una landing page externa permanezca idéntica.
- No existe Git, por lo que la atribución se basa en inventarios pre/post. Se preservó sin borrar un directorio vacío creado accidentalmente fuera del proyecto: `C:\Users\juanc\tasks\evidence\AGRO-DIS-001`.
- Condición externa pendiente: agrónomo, veterinario y responsable editorial nominados deben revisar y firmar el baseline/acta. No resta trabajo técnico interno en esta tarea; hasta esa firma `GAP-011` no se cierra definitivamente y la tarea permanece `En revisión`.

#### Autoevaluación

| Dimensión | Puntaje |
|---|---:|
| Contexto/selección | 15/15 |
| Arquitectura/código | 20/20 |
| Multiagente | 10/10 |
| Full-stack/datos/observabilidad proporcional a R0 | 14/15 |
| Pruebas/seguridad | 20/20 |
| Preservación/cierre | 19/20 |
| **Total** | **98/100** |

Estado final: `En revisión`. La implementación y revisión técnica de `AGRO-DIS-001` están completas; la transición a `Completada` requiere la aprobación profesional nominada definida por la propia tarea.

## Iteración 9 — AGRO-DIS-003 identidad, linking, RLS y tenant (2026-08-05)

Estado inicial: `Propuesto`; seleccionada autónomamente por el líder tras la delegación explícita del sponsor. Clasificación: tarea R0 Must/M; se entregará un spike aislado y descartable, no autenticación productiva ni bootstrap R1.

### DoR y defaults reversibles

- [x] Confirmar ID único, outcome, requisitos, riesgos y tareas dependientes.
- [x] Adoptar `Organization` como tenant operativo, `User` platform-scoped y `Membership` tenant-scoped.
- [x] Mantener organizaciones separadas para clientes de un asesor; nunca inferir acceso agregado cross-client.
- [x] Tratar el control de datos por organización como decisión técnica, sin afirmar propiedad legal entre propietario/productor/asesor.
- [x] Mantener soporte JIT apagado y diferido a `AGRO-ID-005` R7.
- [x] Usar 99,9 %, RPO 15 min y RTO 2 h únicamente como hipótesis medibles del spike, no como SLA contractual.
- [x] Fijar shortlist IdP y criterio: Auth0 candidato preferido; ZITADEL/AWS Cognito como comparadores; no-go productivo sin sandbox, DPA/región, plan, exportabilidad y pruebas reales.
- [x] Aceptar fixtures sintéticos de dos organizaciones y PostgreSQL 17 efímero local en loopback como test data.

### Alcance verificable

- [x] Definir contratos versionados de actor/tenant/permisos, sesión/revocación, linking y eventos conceptuales.
- [x] Documentar matriz IdP, alternativas, trade-offs, gaps y go/no-go condicional.
- [x] Crear threat model repo-grounded con fronteras navegador/Next/API/IdP/pool/DB/jobs y abuso priorizado.
- [x] Implementar un spike Minimal API .NET 10 sin SDK propietario de IdP, con fixtures determinísticos y Problem Details.
- [x] Implementar PostgreSQL RLS real con owner/runtime separados, `ENABLE/FORCE ROW LEVEL SECURITY`, rol `NOBYPASSRLS` y contexto transaccional local.
- [x] Probar dos tenants, BOLA neutral, `WITH CHECK`, reuse de pool A→B→sin tenant, excepción/rollback y job sin tenant.
- [x] Modelar linking como estado one-shot: ambas identidades reautenticadas; email coincidente nunca vincula.
- [x] Modelar recovery anti-enumeración, rate limit, expiración/replay y revocación de sesiones sin guardar OTP/códigos.
- [x] Implementar prototipo Next.js/React accesible para signed-out, 0/1/N organizaciones, cambio de tenant, linking, recovery, provider-down, conflicto y sesión revocada.
- [x] Ejecutar restore/build/analyzers/format/tests .NET, lint/typecheck/unit/build frontend, navegador real y scans aplicables.
- [x] Integrar revisión independiente Principal QA y AppSec/Arquitectura; resolver hallazgos altos/críticos.
- [x] Actualizar ADR-003, decisiones/gaps, evidencia y estado final sin presentar el spike como producción.

### Contrato y límites fijados antes de editar código

- Identidad externa estable: `(issuer, subject)`. Email es contacto/discovery, nunca autoridad de linking.
- El cliente no decide tenant ni permisos. El servidor deriva contexto desde sesión y membresía vigente; el recurso ajeno responde de forma neutral.
- Cookie opaca `__Host-*`, `HttpOnly`, `Secure`, `SameSite=Lax`; tokens upstream solo server-side y ninguna sesión/token en `localStorage`.
- Linking requiere sesión primaria, step-up y reautenticación fresca de ambas identidades; challenges ligados a sesión, TTL y consumo único.
- Recovery devuelve aceptación indistinguible, aplica límites y revoca sesiones al completarse. El IdP conserva secretos de passkey/TOTP/recovery; AgropecuarIA no los replica.
- Cada request/job abre transacción, aplica contexto tenant local y falla cerrado cuando falta. El rol de aplicación no es owner ni posee `BYPASSRLS`.
- Sin IdP real, email real, proveedor cloud, credenciales, datos personales, deploy, Docker, CI o migración productiva.

### Ownership disjunto — ola 2

- Principal: solución/manifiestos/lockfiles, contratos compartidos, scaffolding, integración, tests/harness común, documentación y estados.
- Backend .NET: `tasks/evidence/AGRO-DIS-003/spike/api/**`.
- Database/Security: `tasks/evidence/AGRO-DIS-003/spike/database/**` y revisión de invariantes RLS.
- Frontend Next.js: `tasks/evidence/AGRO-DIS-003/spike/web/app/**`, `features/**`, `lib/**` y estilos.
- Agentes de revisión final nuevos y read-only; ningún implementador aprobará sus propios archivos.

### Baseline y comandos previstos

- Baseline: .NET SDK 10.0.201 y PostgreSQL 17 disponibles; Docker no instalado; Git ausente; no existían `.sln`/`.csproj` ni evidencia `AGRO-DIS-003`.
- El PostgreSQL del sistema exige credenciales desconocidas y no se toca. El harness iniciará un clúster efímero propio con `initdb`, `trust` limitado a loopback y ruta/puerto explícitos, luego lo detendrá y eliminará de forma validada.
- .NET 10 + Microsoft.Testing.Platform: detectar señales finales antes de ejecutar `dotnet test --solution ...`; no usar sintaxis VSTest por memoria.
- Frontend: package/lock propios del spike, instalación frozen, lint, typecheck, unit, build y Playwright CLI.
- Docker/Compose, cloud, migración/restore productivo, deployment y telemetría productiva: N/A por alcance R0 y herramientas disponibles.

### Fuentes primarias que alteran decisiones

- ASP.NET Core 10 es LTS y agrega validación integrada de Minimal APIs: <https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0>.
- Auth0 exige autenticar ambas cuentas antes de linking y condiciona capacidades al plan: <https://auth0.com/docs/manage-users/user-accounts/user-account-linking>.
- Auth0 documenta passkeys y recovery codes, pero el plan/región siguen siendo gates: <https://auth0.com/docs/authenticate/database-connections/passkeys> y <https://auth0.com/docs/secure/multi-factor-authentication/multi-factor-authentication-factors>.
- PostgreSQL documenta default-deny, bypass de owner y necesidad de `FORCE ROW LEVEL SECURITY`: <https://www.postgresql.org/docs/17/ddl-rowsecurity.html>.

### Revisión

- Resultado interno: `PASS`; Principal QA revalidó 15/15 backend y 1/1 Playwright E2E. AppSec/Arquitectura confirmó cero hallazgos críticos, altos o medios internos.
- Evidencia principal: `tasks/evidence/AGRO-DIS-003/validation-report.md`.
- Estado final: `En revisión`. El `GO CONDICIONAL` habilita únicamente el siguiente sandbox, no producción.
- Pendientes exactos: IdP real OIDC/PKCE y failover; contrato/región/DPA/plan/SLA/exportabilidad; persistencia one-to-many de identidades externas y discovery productivo de membresías.
- N/A justificados: Docker/Compose/CI/deploy, migración/rollback productivo y telemetría productiva no pertenecen a este spike R0; Docker no estaba instalado.
- Autoevaluación informativa: 94/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos 14, tests/seguridad 19, preservación/cierre 17). No compensa los gates externos; por eso no se marca `Completada`.

## Iteración 10 — AGRO-DIS-004 GIS, mapas y meteorología multifuente (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el líder con la delegación explícita del sponsor. Transiciones registradas: `Propuesto → Ready` al cerrar esta DoR y `Ready → En curso` al publicar el plan. Clasificación: spike R0 Must/L aislado y descartable; no es pipeline productivo, migración R2 ni autorización de gasto.

### DoR, evidencia y decisiones reversibles

- [x] Confirmar ID único, outcome, exclusiones, requisitos, riesgos y dependencias futuras.
- [x] Usar el endpoint oficial Georef para generar un fixture versionado de las 23 provincias y CABA; sus centroides públicos no representan campos ni coordenadas privadas.
- [x] Verificar términos públicos: Georef/Argenmap oficiales, Open-Meteo pricing/terms/licence, SMN CAP CC BY 4.0 y WRF SMN CC BY 2.5 Argentina.
- [x] Separar el contrato base de las decisiones productivas abiertas: `GAP-004`/Q-021–030 no bloquean el spike, pero sí alertas agronómicas, contratación Open-Meteo y adopción WRF.
- [x] Fijar targets ya aprobados: mapa ≤3 s p75 y clima cacheado ≤2 s p75; medir sin convertir una corrida local en SLA.
- [x] Definir una matriz go/no-go por cobertura nacional, contrato/schema, licencia/atribución, latencia, cuota/SLA, costo, privacidad y degradación.
- [x] Confirmar toolchain: .NET SDK 10.0.201, Node 22.19.0, npm 11.11.1 y PostgreSQL 17. PostGIS no está instalado y Docker/WSL no están disponibles.
- [x] Resolver PostGIS sin mutar el sistema: runtime efímero ignorado que copia PostgreSQL 17 y superpone el bundle oficial PostGIS 3.6.2 fijado y verificado; el harness falla cerrado si no puede ejecutar `CREATE EXTENSION postgis`.

### Umbrales técnicos del spike — no son reglas agronómicas

- Geometría de entrada: GeoJSON `Polygon`/`MultiPolygon` 2D en WGS84, coordenadas finitas, anillos cerrados, no vacía, SRID 4326 y `ST_IsValid`; sin `ST_MakeValid` silencioso.
- Guardas reversibles para medir abuso: payload ≤1 MiB y ≤10.000 vértices. Probar 4, 100, 1.000, 10.000 y exceso; el límite productivo se revisará con telemetría R2.
- Área canónica: `ST_Area(geography)` sobre esferoide en m²/ha. Comparar con EPSG:6933 como control independiente y exigir delta relativo ≤0,5 % en fixtures técnicos; superficie declarada y calculada nunca se sustituyen ni se aceptan/rechazan entre sí sin umbral de producto aprobado.
- Georef: exactamente 24 IDs únicos, coordenadas finitas y respuesta live ≤2 s; el fixture estable, no la red, gobierna tests.
- Tiles Argenmap: 24 smoke points, HTTP 2xx, atribución visible y p75 ≤3 s. Fallo de tiles conserva tabla y geometría; MapLibre no se presenta como proveedor.
- Open-Meteo: 24 puntos en WGS84, schema/unidades/tiempos estrictos, live ≤2 s como probe y fixtures para 429/500/timeout/drift. `freshnessThreshold` es dato de política configurable; tests usan 2 h como hipótesis reversible y nunca convierten error en cero.
- CAP: XML máximo 2 MiB, DTD/entidades externas deshabilitadas, ciclo `Alert/Update/Cancel/expired`, orden CAP `lat,lon` convertido explícitamente a GeoJSON `lon,lat`, sin dereferenciar recursos.
- WRF: procesar solo una muestra oficial ≤25 MiB en un venv aislado y con SHA-256 fijado; medir contra 512 MiB RAM y 10 s de parse/subset, sin presentarlo como sandbox o límite preventivo. El sandbox/kill preventivo queda como gate productivo. Medir volumen de 73 plazos y, si supera 1 GiB o no hay presupuesto/operación aprobados, decidir `POSTPONER`, no adopción implícita.

### Alcance verificable

- [x] Publicar contratos JSON Schema para referencia espacial, snapshot meteorológico, corrida de proveedor y lifecycle CAP.
- [x] Generar fixtures versionados: 24 jurisdicciones, geometrías válidas/inválidas/extremas, Open-Meteo, CAP real+sintéticos y metadata WRF.
- [x] Implementar harness .NET 10 para validación de límites, unidades, frescura/degradación, CAP seguro y telemetría local estructurada.
- [x] Implementar y ejecutar SQL PostGIS real para SRID, validez, área, límites, intersección CAP y plan GiST.
- [x] Implementar probes live reproducibles para Georef, Argenmap, Open-Meteo, CAP y WRF, conservando hashes/resultados sin volver tests dependientes de red.
- [x] Implementar prototipo Next.js/React + MapLibre con mapa y alternativa tabular, estados `observed/estimated/forecast` y `fresh/stale/unavailable`, atribución, teclado, foco y pantalla angosta.
- [x] Medir WRF NetCDF, documentar contradicción de cadencia oficial y emitir decisión explícita incorporar/postergar/rechazar.
- [x] Ejecutar restore/build/analyzers/tests .NET, PostGIS real, frozen install/lint/typecheck/unit/build/E2E frontend, scans y revisión final independiente.
- [x] Actualizar ADR-002/005, gaps, reporte de decisión y estado sin afirmar precisión agronómica ni contrato productivo.

### Contratos y límites fijados antes de editar código

- Puertos conceptuales separados: `TerritoryReferenceProvider`, `MapStyleProvider`, `WeatherProvider` y `OfficialAlertProvider`; MapLibre renderiza, Argenmap entrega tiles, Georef normaliza territorio, Open-Meteo pronostica y CAP conserva autoridad oficial.
- `WeatherSnapshot` es inmutable y conserva proveedor, modelo/corrida, coordenada solicitada y celda resuelta, emisión, ingesta, vigencia, variable, valor, unidad, naturaleza, frescura, confianza/limitación y atribución.
- Errores tipados: `timeout`, `rate_limited`, `provider_error`, `schema_invalid`, `run_missing`, `unavailable`; stale se rotula y una alerta CAP cancelada/expirada nunca queda activa.
- Proveedores meteorológicos se invocan solo backend. URLs/modelos/variables se obtienen de allow-lists; sin URLs aportadas por usuario, sin secretos en query/log y sin coordenadas privadas en fixtures.
- El spike no simula tenancy porque usa exclusivamente coordenadas públicas; la futura persistencia debe incorporar tenant, autorización por recurso, RLS defensiva y auditoría append-only.

### Ownership disjunto — olas 2 y 3

- Principal: contratos compartidos, `.slnx`/manifiestos/lockfiles, scripts de orquestación, documentación, estados, integración y publicación Git.
- Database/GIS: `tasks/evidence/AGRO-DIS-004/spike/postgis/**` y `fixtures/geometry/**`.
- Backend/Weather: `tasks/evidence/AGRO-DIS-004/spike/src/**`, `spike/tests/**` y fixtures `open-meteo/**`, `cap/**`, `wrf/**` asignados.
- Frontend: `tasks/evidence/AGRO-DIS-004/spike/web/app/**`, `features/**`, `lib/**`, estilos y tests frontend; no editar manifiestos compartidos.
- Principal QA y AppSec/Arquitectura revisan el estado combinado en modo read-only; ningún implementador aprueba su propio cambio.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit inicial publicado `66ea2f25ac5fbe738425be6677d20499e0730510` y remoto verificado.
- .NET: `dotnet restore`, `dotnet build --no-restore`, comando MTP detectado por la skill `run-tests`, analyzers/format y suite contractual.
- PostGIS: bootstrap efímero local, `CREATE EXTENSION postgis`, probes SQL, teardown validado; nunca tocar el servicio PostgreSQL del sistema.
- Frontend: `pnpm install --frozen-lockfile`, format/lint/typecheck/unit/build y navegador real con Playwright; comprobar teclado, axe, consola y 390 px.
- Docker/Compose, cloud, deploy, migración/rollback productivo y telemetría productiva: N/A por alcance R0; el spike documenta reemplazo y no se reutiliza como bootstrap.

### Fuentes primarias que alteran decisiones

- PostGIS: `ST_Area(geography)` usa esferoide y metros; `ST_IsValid` valida geometría 2D. <https://postgis.net/docs/ST_Area.html> y <https://postgis.net/docs/ST_IsValid.html>.
- Georef es el servicio oficial abierto para unidades territoriales y publica OpenAPI. <https://www.argentina.gob.ar/georef/referencia-completa-de-la-api>.
- IGN publica Argenmap por XYZ/TMS y WMTS como mapa base oficial. <https://www.ign.gob.ar/NuestrasActividades/InformacionGeoespacial/ServiciosOGC>.
- Open-Meteo exige plan comercial para productos comerciales, atribución CC BY 4.0 y ofrece 99,9 % como target pago, no garantía del free endpoint. <https://open-meteo.com/en/pricing>, <https://open-meteo.com/en/terms> y <https://open-meteo.com/en/license>.
- CAP 1.2 define `Alert/Update/Cancel`, referencias, expiración y polígonos WGS84. <https://docs.oasis-open.org/emergency/cap/v1.2/CAP-v1.2-os.html>.
- WRF SMN publica NetCDF 4 km/72 h en AWS Open Data; Registry dice 00/12 y documentación 00/06/12/18, por lo que la cadencia se descubre por corrida. <https://registry.opendata.aws/smn-ar-wrf-dataset/> y <https://odp-aws-smn.github.io/documentation_wrf_det/>.

### Revisión

- Resultado técnico interno e independiente: `PASS`. AppSec/Arquitectura no encontró vulnerabilidades críticas, altas o medias explotables en el alcance R0; Principal QA aprobó el artefacto condicionado a mantenerlo en `En revisión`, no producción.
- .NET SDK 10.0.201/MTP: restore locked, build 0 warnings/errores, format y scan NuGet PASS; 29/29 tests PASS. Cubre Open-Meteo (7 variables, drift, null, 429/500/timeout, ingesta futura), lifecycle CAP append-only/terminal/offset/XXE y shapes WRF.
- Contratos: Ajv 2020 validó 3 instancias canónicas y 5 provider runs. PostgreSQL 17/PostGIS 3.6.2 real: 6/6 PASS; delta máximo área `0,000024 %`, límites 4/100/1.000/10.000, rechazos 10.001/>1 MiB, CAP espacial y uso GiST; teardown dejó 55434 libre.
- WRF oficial SHA-256 `d2283cbe5b6aa68d1595806f0f39e27da28ff3df1b2158d605b94ee1d4a2879c`: 14.758.413 bytes, 1.249×999, 5/5 negativos PASS, 179,488 ms, 49.925.315 bytes Python y 112.431.104 bytes working set; budgets observados PASS. No hay sandbox/kill preventivo y 73 plazos estimados superan 1 GiB: `POSTPONER`.
- Frontend pnpm 10.33.0: frozen install, Prettier, ESLint, TypeScript, build y audit PASS; Vitest 7/7 y Playwright 4/4 PASS con 24 referencias observed/fresh, demo climática sintética separada, axe, teclado/retry, degradación de tiles y 390 px.
- Probe live persistido final: Georef `success` 120,066 ms; Open-Meteo `degraded` 2.149,274 ms porque el único batch smoke superó 2 s (no es p75); CAP `success` 223,24 ms; Argenmap `success` p75 141,201 ms; WRF `postpone` 755,651 ms. QA observó CAP degradado/HTML en otra corrida, confirmando que el canal/frescura requiere gate productivo.
- Hallazgos resueltos: límites GIS/2D, carrera del harness, NetCDF shape bomb, CAP spoof/replay/orden/cancel/offset y XML del probe, redirects SSRF, contrato ejecutable, BOM/URI template, variables ráfagas/ET0, confianza/granularidad y evidencia UI no fabricada.
- N/A: Docker/Compose/CI/deploy, migración/rollback y telemetría productiva; la tarea es un spike R0 aislado y no autoriza infraestructura ni pipeline productivo.
- Estado final: `En revisión`. Pendientes externos exactos: plan/DPA/región/cuota/SLA y p75 cacheado Open-Meteo; canal/autenticidad/frescura durable CAP; presupuesto/operación/sandbox WRF; precisión local y `VAL-AGR`; tenant/authz/RLS/auditoría antes de R2.
- Autoevaluación informativa: 97/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservación/cierre 19). Cero gate interno fallido; los gates externos impiden `Completada`.

## Iteración 11 — AGRO-DIS-005 storage, antivirus, retención y restore integral (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el líder con la delegación explícita del sponsor. Transiciones registradas: `Propuesto → Ready` al demostrar esta DoR y `Ready → En curso` al publicar el plan. Clasificación: spike R0 Must/M aislado y descartable; no es pipeline productivo, provisión cloud ni política legal.

### DoR, evidencia y decisiones reversibles

- [x] Confirmar ID único, outcome, exclusiones, requisitos, riesgos y tareas dependientes.
- [x] Confirmar las clases `Público`, `Interno`, `Confidencial`, `Fiscal/personal` y `Secreto`; usar solo fixtures sintéticos y excluir secretos de objetos, DB y logs.
- [x] Adoptar RPO ≤15 min, RTO ≤2 h y drill trimestral como hipótesis medibles del spike, no SLA contractual.
- [x] Fijar shortlist reversible: AWS S3 + GuardDuty, Azure Blob + Defender y storage S3-compatible + scanner separado.
- [x] Registrar que Q-058 no autoriza storage internacional y que región/DPA/subencargados/retención requieren `VAL-LEG`.
- [x] Confirmar toolchain: .NET SDK 10.0.201, Node 22.19.0, pnpm 10.33.0 y PostgreSQL 17.9; Docker no está disponible.

### Contratos y límites fijados antes de editar código

- Estados fail-closed: `PendingUpload → Uploaded → Scanning → Available | Quarantined | Rejected | ScanFailed`; solo `clean` habilita descarga. La baja usa `Available → Purging → Deleted | PurgeUncertain` y nunca supone rollback ante un delete ambiguo.
- La clave es generada por servidor bajo prefijo tenant opaco; hash valida integridad y nunca autoriza ni deduplica entre tenants.
- Cada intención y descarga reautoriza tenant, recurso, acción y estado; una URL vencida o recurso ajeno responde sin revelar existencia.
- MIME declarado se contrasta con magic bytes, tamaño y SHA-256. El spike usa un marcador antimalware sintético, no una firma EICAR real en el repositorio.
- DB es fuente del estado. El objeto entra a cuarentena; un evento de scan duplicado debe ser idempotente y error/timeout nunca publica.
- El manifest de backup registra cutoff, backup DB, versiones/hashes de objetos, auditoría y watermark; restore aislado verifica PostGIS, vínculos, hashes, hold y objetos huérfanos sin reemitir URLs.
- Legal hold prevalece sobre purga. Los plazos, región y contrato siguen pendientes; no se implementa borrado productivo ni se tocan servicios existentes.

### Plan verificable

- [x] Publicar schemas versionados para intención/completado, resultado AV, grant de descarga, estado de archivo y manifest de backup.
- [x] Implementar spike .NET 10 con dominio/ports compactos, storage local aislado, firma efímera, MIME/hash, cuarentena, auth tenant y telemetría redactada.
- [x] Implementar harness PostgreSQL/PostGIS efímero y drill `pg_dump`/`pg_restore` + objetos, con corrupción, huérfanos, auditoría, geometría, medición RTO y gap RPO explícito.
- [x] Implementar prototipo Next.js/React con pnpm para progreso, error, cuarentena, provider-down, expiración, conflicto y estados accesibles/responsive.
- [x] Documentar matriz de proveedores, threat model, ADR storage/retención/DR, runbook, decisión go/no-go y gaps externos.
- [x] Ejecutar restore/build/format/analyzers/tests .NET, schemas, frozen install/lint/typecheck/unit/build/E2E, scans y revisión independiente.
- [x] Actualizar evidencia y estado final sin presentar el spike como producción.

### Ownership disjunto — olas 2 y 3

- Principal: contratos compartidos, `.slnx`, manifiestos/lockfiles, scripts de orquestación, documentación, estados, integración y publicación Git.
- Backend/Storage: `tasks/evidence/AGRO-DIS-005/spike/src/**` y `spike/tests/**`.
- Data/Restore: `tasks/evidence/AGRO-DIS-005/spike/postgres/**` y fixtures de restore asignados.
- Frontend: `tasks/evidence/AGRO-DIS-005/spike/web/app/**`, `features/**`, `lib/**` y estilos; no edita manifiestos compartidos.
- Principal QA y AppSec/Arquitectura revisan el estado combinado en modo read-only; ningún implementador aprueba su propio cambio.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit publicado `5873ebbdea1e2bac52c5478d8148749bb6257911`; no existía `tasks/evidence/AGRO-DIS-005`.
- .NET: restore locked, build sin warnings, format/analyzers, comando MTP detectado por la skill `run-tests` y suites de dominio/API/restore.
- PostgreSQL/PostGIS: clúster efímero propio en loopback, dump/restore a base separada, verificación y teardown; nunca tocar el servicio del sistema.
- Frontend: `pnpm install --frozen-lockfile`, format/lint/typecheck/unit/build y Playwright en navegador real, incluido 390 px y teclado.
- Docker/Compose, cloud, credenciales, deploy, migración productiva, AV/provider real y PITR administrado: N/A para la ejecución local R0; quedan como gates externos explícitos.

### Revisión

- Resultado técnico interno e independiente: `PASS`. Principal QA y AppSec/Arquitectura aprobaron el R0; no quedan hallazgos altos/medios internos.
- .NET SDK 10.0.201/MTP: restore locked, build 0 warnings/errores, format y scan NuGet PASS; 32/32 tests PASS. Se verifican tenant/BOLA, tokens, MIME/hash/tamaño, AV fail-closed e idempotente, hold/purga/descarga concurrentes, `PurgeUncertain`, reconciliación privilegiada y telemetría redactada.
- PostgreSQL 17/PostGIS 3.6.2 real: 2 registros, 2 objetos y 4 eventos audit; SRID 4326, snapshots completos, `tenant_id ↔ tenant_ref`, tipo/ID de recurso, cadena criptográfica, append-only, legal hold, huérfano y corrupciones PASS. Principal observó RTO final `0,0217 min`; QA `0,0224 min`; AppSec `0,0258 min`. RPO: `UNPROVEN_WITHOUT_MANAGED_PITR`.
- Frontend pnpm 10.33.0: frozen install, 5 contratos, Prettier, ESLint, TypeScript, build Next.js 16.3 y audit PASS; Vitest 8/8 y Playwright 5/5 PASS, incluido axe, teclado, fallos, conflicto y 390 px. Una corrida con contención concurrente de `.next` fue descartada y repetida en exclusión.
- Hallazgos resueltos: Base64URL no canónica, filtración de paths, restore incompleto, binding tenant/recurso, cadena audit sintética, carreras hold/purge y download/purge, delete ambiguo, scanner detenido/verdict inválido, scopes operativos, clasificaciones y schemas.
- Decisión: `GO técnico condicionado` para sandbox AWS detrás de ports; Azure es alternativa. `NO-GO productivo` hasta storage/AV/KMS/WORM/PITR cloud real, región/DPA/subencargados, política de retención/`VAL-LEG`, volumen/costo y controles productivos.
- N/A: Docker/Compose/CI/deploy, migración/rollback cloud y alertas productivas; el R0 no autoriza infraestructura. Docker no estaba disponible.
- Evidencia principal: `tasks/evidence/AGRO-DIS-005/validation-report.md`.
- Estado final: `En revisión`; no se marca `Completada` porque los gates externos de proveedor, Legal y RPO administrado siguen abiertos.
- Autoevaluación informativa: 97/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservación/cierre 19). Cero gate interno fallido; la puntuación no compensa los gates externos.

## Iteración 12 — AGRO-DIS-007 capacidad, SLO, costos y conectividad (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el líder con la delegación explícita del sponsor. Transiciones registradas: `Propuesto → Ready` al aceptar rangos sintéticos, owners y caducidad como evidencia R0; `Ready → En curso` al publicar este plan. Clasificación: spike R0 Must/M aislado y descartable; no es benchmark del producto, promesa de fecha, presupuesto aprobado, SLA contractual ni aprovisionamiento.

### DoR, evidencia y decisiones reversibles

- [x] Confirmar ID único, outcome, exclusiones, requisitos, riesgos y dependencias futuras.
- [x] Resolver Q-019 como envelope: 1–3 carriles de ejecución y roles mínimos; presupuesto cloud, capacidad nominal y calendario siguen abiertos y no producen fechas.
- [x] Resolver Q-020 con tres escenarios sintéticos versionados (`pilot`, `growth-10x`, `burst-2x`), confianza baja, owner y vencimiento 2026-09-30; no se presentan como demanda observada.
- [x] Resolver Q-060 para el spike con targets hipotéticos existentes: disponibilidad mensual 99,9 %, RPO 15 min, RTO 2 h y sensibilidad; contrato SLA/soporte/retención sigue abierto.
- [x] Resolver Q-061 con perfiles sintéticos de red `target`, `constrained`, `critical` y `offline`; la conectividad rural real permanece sin medir y offline sigue fuera del MVP.
- [x] Confirmar toolchain: .NET SDK 10.0.201, Node 22.19.0, pnpm 10.33.0 y `npx` disponibles. Docker, cloud y un producto desplegable no son necesarios para este R0.

### Contratos y límites fijados antes de editar código

- Cadena determinística: `CapacityScenario → CapacityProjection → SloEvaluation / CostProjection`; ningún resultado se etiqueta `observed` si proviene de fixtures.
- El costo se calcula por drivers explícitos y rangos `low/base/high`; precio faltante produce `incomplete`/NO-GO, nunca cero. Moneda, región, fuente, fecha y tratamiento impositivo son obligatorios para un catálogo real.
- El catálogo SLI separa core propio de dependencias externas y define numerador, denominador, ventana, exclusiones y owner. 99,9 % equivale a 43 min 12 s en 30 días y a 1.000 eventos malos por millón elegible.
- Telemetría usa allow-list de dimensiones acotadas (`route_template`, método, clase de estado, dependencia, job, cache, entorno). Se prohíben tenant/user/resource IDs, CUIT, email, coordenadas, filename, path/query, payload e idempotency key.
- La UI es un laboratorio online: muestra estimación, confianza, faltantes, fecha de revalidación y estados de red. Sin red bloquea confirmación y no persiste/encola trabajo en `localStorage`, IndexedDB ni service worker.
- Canary, rollback y DR son políticas de decisión; no se despliega ni provisiona. Índices, partición y cuotas productivas se difieren hasta medir carga real.

### Plan verificable

- [x] Publicar JSON Schema y fixtures para escenarios de capacidad, catálogo de costos y reporte reproducible.
- [x] Implementar modelo .NET 10 para throughput, storage, drain time, error budget, costos incompletos y política de cardinalidad; agregar pruebas negativas y de límites.
- [x] Implementar laboratorio Next.js 16/React 19 con pnpm para escenarios, SLO, costo incompleto y perfiles online/degradado/offline, accesible y responsive.
- [x] Ejecutar contratos, restore/build/format/tests/scan .NET y frozen install/format/lint/typecheck/unit/build/audit/E2E frontend.
- [x] Revisar independientemente QA y AppSec/Arquitectura; resolver hallazgos y registrar evidencia exacta.
- [x] Actualizar ADR, preguntas/gaps/riesgos/trazabilidad, reporte y estado sin convertir estimaciones en compromisos.

### Ownership disjunto — olas 2 y 3

- Principal: contratos/fixtures compartidos, `.slnx`, `global.json`, manifests/lockfiles frontend, documentación, backlog, integración, gates finales y Git.
- Backend/Capacity: `tasks/evidence/AGRO-DIS-007/spike/src/**` y `spike/tests/**`; no edita contratos, manifests ni documentación.
- Frontend: `tasks/evidence/AGRO-DIS-007/spike/web/app/**`, `features/**`, `lib/**` y tests UI; no edita package/lock/config ni documentos.
- Principal QA y AppSec/Arquitectura revisan el estado combinado en modo read-only; ningún implementador aprueba su propio cambio.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit publicado `6a551ad39ef01013f4de58bbf9b0cacf046d843b`.
- .NET: `dotnet restore --locked-mode`, `dotnet build --no-restore`, `dotnet format --verify-no-changes`, MTP detectado por `run-tests`, suite contractual y scan NuGet.
- Frontend: `pnpm install --frozen-lockfile --ignore-scripts`, validación de contratos, Prettier, ESLint, TypeScript, Vitest, build, audit y navegador Chromium real; verificar teclado, axe, 390 px y `BrowserContext.setOffline(true)`.
- Docker/Compose, DB/PostGIS, migración/rollback productivo, CI/CD, deploy y telemetría productiva: N/A por alcance R0; no existe camino productivo que medir o migrar.

### Fuentes primarias que alteran decisiones

- Google SRE exige una política concreta de error budget para decidir releases y confiabilidad, no solo un porcentaje aislado: <https://sre.google/workbook/error-budget-policy/>.
- Playwright expone `BrowserContext.setOffline()` para validar comportamiento real del navegador sin red: <https://playwright.dev/docs/api/class-browsercontext>.
- OpenTelemetry HTTP semantic conventions estandarizan atributos de bajo riesgo; sus requisitos generales y métricas obligan a controlar cardinalidad: <https://opentelemetry.io/docs/specs/semconv/http/>, <https://opentelemetry.io/docs/specs/semconv/general/attribute-requirement-level/> y <https://opentelemetry.io/docs/concepts/signals/metrics/>.
- .NET 10 tiene soporte activo hasta 2028-11-14; el spike fija SDK 10.0.201 para reproducibilidad: <https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core>.

### Revisión

- Resultado técnico interno: `PASS`. .NET 10/MTP restore locked, build 0 warnings/errores, format y scan NuGet PASS; 21/21 tests PASS.
- Contratos: 3 fixtures positivos y 10 negativos PASS; fixture canónico alimenta frontend y test .NET→golden. Vitest 5/5, Next build, Prettier/ESLint/TypeScript y pnpm audit PASS.
- Playwright Chromium 4/4 PASS: estados, costo NO-GO, conexión inicial default-deny, offline real, cero persistencia local, retry/dedupe, teclado, axe y 390 px.
- Principal QA aprobó el estado combinado. AppSec/Arquitectura cerró hallazgos de integridad FinOps/fixture/golden, cardinalidad y conexión inicial; reauditoría: 0 críticos/altos/medios/bajos.
- N/A por alcance: API/DB/PostGIS, migración/rollback productivo, Docker/Compose, CI/CD, deploy, cloud, carga real, telemetría emitida y alertas. No se fabricó un runtime para simular aprobación.
- Estado final: `En revisión`. Pendientes externos exactos: Q-019/020/060/061, GAP-003/GAP-010 y RSK-022/024/027; los supuestos vencen 2026-09-30.
- Autoevaluación informativa: 97/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservación/cierre 19). Cero gate interno fallido; los gates externos impiden `Completada`.

## Iteración 13 — AGRO-FND-001 límites modulares y contratos compatibles (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el líder con la delegación explícita del sponsor. Transiciones registradas: `Propuesto → Ready` al verificar módulos, consumidores, conflictos y evidencia técnica de `AGRO-DIS-003/004`; `Ready → En curso` al publicar este plan. Clasificación: decisión y fitness R0 aplicables a R1; no autoriza bootstrap productivo, microservicios, broker, migraciones ni reutilizar spikes.

### DoR y alcance fijado

- [x] Confirmar ID único, outcome, exclusiones, requisitos, riesgos y consumidores.
- [x] Aceptar `AGRO-DIS-003/004` como evidencia técnica R0 sin promover sus prototipos ni cerrar sus gates externos.
- [x] Resolver la contradicción documental: `National Catalog` y `Productive Core` son bounded contexts distintos aunque compartan WS-03.
- [x] Fijar `Organization` como tenant; CUIT no selecciona ni autoriza tenant y su cardinalidad legal queda para Product/Legal.
- [x] Fijar `ManagementUnit` y su lifecycle en Productive Core; Territory posee solo representación espacial versionada y opcional.
- [x] Confirmar que no se requieren reglas agronómicas, veterinarias, fiscales o contables para este límite genérico.

### Plan verificable

- [x] Publicar ADR aceptado, registro machine-readable de 15 módulos y mapa completo de consumidores.
- [x] Publicar contratos versionados para scope, Problem Details, paginación cursor y eventos internos.
- [x] Definir política N/N-1, extracción futura y `expand → backfill → contract`, separando decisión de ensayo operativo posterior.
- [x] Implementar fitness tests aislados para DAG, ownership/schema, persistencia ajena, scope y compatibilidad aditiva/breaking.
- [x] Cubrir negativos: ciclo, schema ajeno, scope ambiguo, consumidor N-1, evento duplicado/fuera de orden y error enumerante.
- [x] Ejecutar restore/build/format/tests/scan y validación JSON; realizar revisión independiente QA y AppSec/Arquitectura.
- [x] Actualizar arquitectura, decisiones/gaps, trazabilidad, reporte y estado final sin crear API/UI/producto.

### Ownership disjunto — olas 2 y 3

- Principal: ADR/documentación, contratos y fixtures compartidos, manifiestos `.slnx`/`.csproj`, registro/mapa, integración, estados y Git.
- Backend/Architecture Fitness: `tasks/evidence/AGRO-FND-001/fitness/src/**` y `fitness/tests/**`; no edita manifiestos ni documentos.
- Principal QA y AppSec/Arquitectura: revisión read-only del estado combinado; ningún implementador aprueba su propio cambio.

### Baseline y comandos ejecutados

- Baseline Git limpio en `main`, commit publicado `879146aa814dfc790ef69ad44641756440f20a44`; no existía `tasks/evidence/AGRO-FND-001` ni solución productiva raíz.
- `dotnet restore --locked-mode`: PASS; `dotnet build --no-restore`: PASS, 0 warnings/errores.
- MTP detectado por la skill `run-tests`: PASS 42/42, 0 failed/skipped; `dotnet format --verify-no-changes`: PASS.
- 12 JSON parseados, cuatro producer schemas ejecutados con mutation tests, NuGet vulnerable scan y secrets scan dirigidos: PASS.
- Frontend/pnpm, API, PostgreSQL/PostGIS, Docker/Compose, migración staging, observabilidad productiva, CI/CD y deploy: N/A; la tarea produce arquitectura verificable y no una superficie funcional.

### Revisión

- Resultado técnico R0 e independiente: `PASS`. Principal QA y AppSec/Arquitectura reprodujeron gates; cero hallazgos críticos, altos o medios residuales.
- Topología: 15 módulos, 15 schemas dueños y 69 edges declarados/mapeados; DAG, ownership, scopes y persistencia ajena cubiertos por positivos y mutaciones negativas.
- Contratos: scope discriminado, producer schemas cerrados, reader N-1 tolerante, 401/403/404/409/412, ETag después de authz y compatibilidad aditiva/breaking verificados.
- Eventos: source/scope/tenant/agregado/version forman el stream; duplicate/out-of-order/gap/foreign/unknown no mutan ni elevan privilegios.
- Hallazgos cerrados: schemas no ejecutados, required→optional, policies/shared kernel laxos, scope ambiguo, escalamiento platform/tenant, stream sin tenant/source, docs divergentes y vocabulario de dominio inventado.
- `ADR-PEND-011` resuelta por ADR-009. `ADR-PEND-010` queda `política definida; ensayo pendiente` para `AGRO-FND-003`/`AGRO-PLT-004`; no se afirma staging, backup/restore ni migración R1.
- Estado final: `En curso`. El gate R0 está aprobado, pero por ser tarea multirelease R0/R1 la tarea padre no pasa a `Completada` hasta demostrar el ensayo operativo R1.
- Evidencia: `tasks/evidence/AGRO-FND-001/validation-report.md`.
- Autoevaluación informativa: 96/100 (contexto 15, arquitectura 20, multiagente 10, full-stack/datos/observabilidad 12, tests/seguridad 20, preservación/cierre 19). Cero gate aplicable fallido; los N/A corresponden al alcance R0.

## Iteración 14 — AGRO-SEC-001 modelo de amenazas y clasificación por release (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el líder con la delegación explícita del sponsor. Transiciones registradas: `Propuesto → Ready` al verificar arquitectura, flujos, activos e inventarios candidatos; `Ready → En curso` al publicar este plan. Clasificación: gate documental R0 de una tarea continua R0–R6; no es certificación legal, pentest, implementación de controles productivos ni autorización de proveedor.

### DoR, supuestos y límites

- [x] Confirmar ID único, outcome, criterios, requisitos, riesgos y exclusiones.
- [x] Confirmar arquitectura y fronteras en `docs/05-arquitectura.md`, actores/flujos en `docs/02-dominio-actores-y-flujos.md` y seguridad base en `docs/07-seguridad-y-privacidad.md`.
- [x] Confirmar inventarios candidatos de identidad, GIS/clima y archivos en las evidencias de `AGRO-DIS-003/004/005`, sin tratarlos como proveedores contratados o superficies productivas.
- [x] Acotar la fase actual a R0 y mantener Q-054/055/058/060 como supuestos condicionales con owner y NO-GO productivo.
- [x] Registrar que los spikes son locales/descartables y que no existe todavía runtime, CI o despliegue productivo que escanear.
- [x] Descartar `AGRO-FND-003`: no está Ready por falta de agregado productivo, estados/campos inmutables, identidad/auditoría productivas y retención aprobada; no se mutó su estado.

### Contrato de evidencia y plan verificable

- [x] Publicar un threat model central, repo-grounded y versionado con componentes, fronteras, activos, atacante, superficies, abuso y calibración explícita.
- [x] Publicar clasificación de datos/privacidad e inventario de proveedores/procesamiento, distinguiendo presente, candidato y futuro fuera del MVP.
- [x] Publicar un registro machine-readable `threat → control → prueba → owner → riesgo residual → capability/release`, sin críticos huérfanos.
- [x] Publicar gates por release y plantilla para revisar una nueva frontera/proveedor durante DoR.
- [x] Implementar un validador reproducible de estructura, IDs, referencias, owners, criticidad y gates fail-closed; cubrir mutaciones negativas.
- [x] Ejecutar validación documental, JSON, enlaces/rutas, secretos dirigidos y revisión independiente AppSec/Arquitectura/Product.
- [x] Actualizar riesgos, trazabilidad, reporte y estado final preservando los gates Legal/Privacy/Sponsor.

### Ownership disjunto — olas 2 y 3

- Principal: estructura/README, contrato y validador compartidos, integración, backlog/todo/riesgos/trazabilidad, reporte, gates finales y Git.
- Architecture Lead: `tasks/evidence/AGRO-SEC-001/AgropecuarIA-threat-model.md`; no edita registros, scripts ni documentación global.
- Product/Domain/UX: `tasks/evidence/AGRO-SEC-001/data-classification-and-privacy.md`; no edita threat model, registros ni scripts.
- Security/Data: `tasks/evidence/AGRO-SEC-001/provider-processing-inventory.md` y borrador de amenazas para integración; no edita archivos de otros owners.
- Ola 3 cruza revisores sobre archivos ajenos y permanece read-only; ningún autor aprueba su propio artefacto.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit publicado `f86e2d4434a6670abc024ea248d8443dd54f1a9e`; no existía `tasks/evidence/AGRO-SEC-001` ni runtime productivo raíz.
- PowerShell: validador propio con casos positivo/negativos, parseo JSON estricto, chequeo de rutas Markdown, IDs estables y secretos dirigidos.
- Revisión manual: cobertura de todas las fronteras y superficies, separación runtime futuro/spikes/build, owners de amenazas críticas y Q-054/055/058/060 explícitas.
- .NET, pnpm/frontend, API, PostgreSQL/PostGIS, Docker/Compose, SAST/SCA/DAST, migración, telemetría emitida, CI/CD y deploy: N/A para este gate R0 documental sin runtime productivo; no se fabricará infraestructura para aparentar controles.

### Revisión

- Resultado R0: `PASS`. Registro con 14 amenazas (7 críticas/7 altas), 4 preguntas abiertas, 16 `RSK-*` únicos y 12 superficies `PI-01`–`PI-12`; 0 críticas sin owner/prueba/gate.
- Validador: 7/7 mutation tests PASS — owner, test, valor blanco, ID duplicado, riesgo inválido, pregunta omitida y drift JSON↔tabla. JSON, rutas/evidence, enlaces Markdown y `git diff --check`: PASS.
- Scan dirigido: 0 patrones de credenciales. No se agregaron secretos, datos personales, proveedor, cuenta cloud ni runtime.
- Revisión independiente final: AppSec/Data, Architecture y Product/UX/Privacy `PASS`; 0 críticos, altos o medios. Los hallazgos iniciales sobre edge/web, browser→storage/tiles, email, telemetría/CI/backup, trazabilidad de riesgos, owner e integridad del validador fueron corregidos; las observaciones bajas de índices `PI-09/PI-12` también quedaron resueltas.
- N/A: build/test .NET, pnpm/frontend, API, DB/PostGIS, Docker, migraciones, SAST/SCA/DAST runtime, observabilidad emitida, CI/CD y deploy. No existe producto ejecutable y este gate documental no autoriza crearlo.
- Riesgo residual: las 14 amenazas permanecen abiertas para producción. Q-054/055/058/060, `GAP-003`, `GAP-008`, `VAL-LEG`, IdP/proveedores/regiones/DPA/retención, pipeline y restore administrado siguen como NO-GO de cada capacidad afectada.
- Estado final: `En curso`. El baseline R0 está aprobado, pero `AGRO-SEC-001` es continua R0–R6 y debe actualizarse/revalidarse por slice y release.
- Evidencia: `tasks/evidence/AGRO-SEC-001/validation-report.md`.
- Autoevaluación informativa: 96/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 13, tests/seguridad 20, preservación/cierre 19). Cero gate aplicable fallido; los N/A y NO-GO no son aprobaciones implícitas.

## Iteración 15 — AGRO-ID-001 registro y vinculación de identidades (2026-08-05)

Estado inicial: `Propuesto`. El sponsor seleccionó y delegó explícitamente la ejecución. Transiciones: `Propuesto → Ready` al fijar Auth0 como IdP objetivo, email OTP + Google OIDC como mecanismos y separar las credenciales reales como gate del servidor de prueba; `Ready → En curso` al publicar este plan. Clasificación: capacidad R1 y primer bootstrap productivo.

### DoR, alcance y contrato

- [x] Verificar ID, outcome, criterios, dependencias `AGRO-DIS-003`/`AGRO-FND-001`, amenazas y contratos previos.
- [x] Fijar Auth0 como adaptador OIDC objetivo y proveedor sintético exclusivamente para `Development`/`Test`; los ambientes no locales fallan cerrados sin configuración.
- [x] Mantener Q-054/Q-055 como decisiones contractuales no bloqueantes: `User` es platform-scoped y las membresías no transfieren propiedad ni mezclan organizaciones.
- [x] Definir sesión cookie, reautenticación de ambas credenciales, replay protection, CSRF, revocación, auditoría sin PII y migración aditiva.
- [x] Crear bootstrap mínimo productivo .NET 10 + Next.js 16/React/pnpm sin reutilizar los spikes descartables.
- [x] Implementar dominio/aplicación, PostgreSQL, API y telemetría de login, linking, unlink y revocación.
- [x] Implementar experiencia frontend accesible y responsive con estados loading, error, conflicto, proveedor caído y sesión revocada.
- [x] Agregar pruebas unitarias, integración PostgreSQL, API/seguridad, frontend, accesibilidad y E2E en navegador real.
- [x] Ejecutar restore/build/format/tests/lint/typecheck/unit/e2e, migración aislada, scans y revisión independiente.
- [x] Documentar operación local, configuración del servidor de prueba, rollback, evidencia y estado final.

### Ownership disjunto

- Principal: `.slnx`, `global.json`, props/paquetes raíz, contratos compartidos, `apps/web/package.json`, lockfile, configuración transversal, integración, documentación, estados y Git.
- Backend .NET: `src/Identity/**`, `apps/api/**` y tests backend asignados; no edita manifiestos raíz ni frontend.
- Frontend Next.js: `apps/web/app/**`, `apps/web/features/**`, `apps/web/lib/**`, estilos y tests frontend; no edita `package.json`, lockfile ni backend.
- Database/QA: migraciones/fixtures PostgreSQL y harness de integración/E2E asignados; no comparte migraciones ni implementación con otro owner.
- Revisión final: QA y AppSec/Arquitectura read-only sobre el estado combinado; ningún autor aprueba su propio cambio.

### Baseline y gates previstos

- Baseline Git limpio en `main`, sincronizado con `origin/main`; no existen aplicación productiva raíz, solución, lockfile raíz ni migraciones productivas.
- Antes del bootstrap, gates .NET/frontend/product DB son N/A por ausencia verificable. Después del bootstrap pasan a ser obligatorios.
- Comandos objetivo: `dotnet restore`, `dotnet build --no-restore`, tests MTP, `dotnet format --verify-no-changes`; `pnpm install --frozen-lockfile`, format/lint/typecheck/unit/build/E2E; migración PostgreSQL efímera y scans dirigidos.
- No incluye deploy, aprovisionamiento Auth0, credenciales reales, MFA/passkeys (`AGRO-ID-002`) ni organizaciones/roles (`AGRO-ID-003`).

### Revisión

- Resultado integrado local: `PASS`. API y módulo .NET compilan sin warnings; 21/21 pruebas MTP pasan contra PostgreSQL 17 efímero, incluida migración rollback/roll-forward, unicidad concurrente, aislamiento por recurso, replay, CSRF, rate limit preautenticación por IP/sesión, cookies, revocación y auditoría append-only.
- Frontend: pnpm frozen, format, lint, TypeScript estricto, 18/18 Vitest y build Next.js productivo `PASS`; Playwright Chromium desktop/móvil 4/4 con Axe WCAG 2.2 AA, teclado real, viewport angosto y loader limitado a la región.
- Seguridad/operación: sesión opaca hasheada, OIDC code+PKCE same-origin, provider sintético físicamente local, límites preautenticación por IP/sesión, proxies explícitos, `no-store`, headers defensivos, Problem Details, métricas/logs sin secretos y outbox `IdentityLinked` exactamente una vez.
- Migración: aditiva, aplicada/retirada/reaplicada sobre DB efímera. En ambiente compartido el rollback es funcional por flags y roll-forward; no se elimina historia ni auditoría.
- Revisión independiente: hallazgos internos de QA/AppSec corregidos y revalidados; el único gate externo es ejecutar Auth0 real en el servidor de prueba con secretos fuera del repositorio, callback/state/nonce/claims, email/Google, provider-down y logout.
- Estado final: `En revisión`. La implementación local está terminada; no se marca `Completada` hasta obtener evidencia del IdP real en el ambiente compartido solicitado por el sponsor.
- Evidencia: `tasks/evidence/AGRO-ID-001/validation-report.md`.
- Autoevaluación informativa: 96/100 (contexto 15, arquitectura 20, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 19, preservación/cierre 18). Cero gate interno fallido; el gate Auth0 externo no se compensa con la puntuación.

## Iteración 16 — AGRO-FND-001 enforcement R1 sobre el runtime (2026-08-05)

Estado inicial: `En curso`. La fase R0 tiene ADR y fitness aprobados, pero el gate quedó aislado y no inspecciona la solución productiva creada por `AGRO-ID-001`. Este sub-slice R1 permanece dentro de la misma tarea y no incorpora `AGRO-FND-002`, `AGRO-FND-003` ni `AGRO-PLT-004`.

### DoR, alcance y aceptación

- [x] Revalidar ID, outcome, dependencias, ADR-009, mapa de consumidores y evidencia R0.
- [x] Identificar drift real en scope, Problem Details, outbox, auditoría local y telemetría de contrato.
- [x] Integrar el fitness arquitectónico en `AgropecuarIA.slnx` para que el gate normal inspeccione el runtime actual.
- [x] Incorporar scope discriminado derivado por servidor y distinguir el journal de seguridad local del agregado central de Audit/Compliance.
- [x] Alinear status/media type/schema de Problem Details con la política FND.
- [x] Evolucionar aditivamente el outbox Identity al envelope canónico sin implementar delivery/retry de `FND-002`.
- [x] Emitir versión de contrato/consumidor con cardinalidad acotada y probar su emisión sin PII.
- [x] Agregar pruebas contra referencias reales, ownership EF/schema, OpenAPI y contrato N/N-1 del producto.
- [x] Ejecutar restore/build/format/tests, migración PostgreSQL aislada, scans y revisión independiente.
- [x] Actualizar ADR/evidencia/backlog y cerrar solo si todos los gates propios de FND-001 quedan demostrados.

### Ownership disjunto

- Principal: `AgropecuarIA.slnx`, contratos/manifestos compartidos, migración, integración, documentación, estados y Git.
- Backend: `src/AgropecuarIA.Identity/**`, `apps/AgropecuarIA.Api/**` y tests backend asignados; no edita manifests ni evidencia FND.
- Architecture Fitness: `tasks/evidence/AGRO-FND-001/fitness/**`; no edita runtime ni manifiestos raíz.
- QA/AppSec: revisión final read-only del estado combinado; ningún implementador aprueba su propio cambio.

### No objetivos y gates

- No crear módulos vacíos, multi-CUIT, ARCA, broker, delivery de outbox, idempotencia genérica, ETag/backfill ni backup/restore.
- Baseline y gates objetivo: solución .NET 10/MTP, PostgreSQL efímero, contrato OpenAPI, `dotnet format`, NuGet vulnerable scan, secrets scan dirigido y `git diff --check`.

### Revisión

- Resultado del sub-slice R1: `PASS` local. Restore locked, build Release 0 warnings/0 errors, format y modelo EF sin drift; suite raíz 77/77 (27 Identity + 50 Architecture Fitness).
- PostgreSQL efímero: initial→expand, preservación/backfill, escritor N-1 después del upgrade y rollback/roll-forward `PASS`. La expansión conserva `identity.audit_events` y nullability física para N-1; el modelo/escritor N exige actor y envelope canónico.
- Problem Details runtime/OpenAPI cerrado y sin `traceId` duplicado; 403/429, autorización por actor/scope, telemetría de contrato sin PII y outbox monotónico cubiertos.
- Frontend sin cambios: pnpm frozen, format, lint, typecheck, Vitest 18/18 y build Next.js `PASS`; E2E navegador N/A porque no cambió UI ni contrato consumido por UI.
- NuGet vulnerable scan, JSON, secrets scan dirigido y `git diff --check`: `PASS`; cero vulnerabilidades altas/críticas nuevas o credenciales.
- Revisión independiente inicial bloqueó correctamente `traceId`, falta de `ActorId` y migración contract prematura; las tres causas se corrigieron. Revalidación final QA y AppSec/Arquitectura: `PASS`, 0 hallazgos críticos/altos/medios.
- Estado: `En curso`. El enforcement runtime quedó integrado, pero la tarea multirelease no puede cerrarse hasta `AGRO-FND-003`/`AGRO-PLT-004`; delivery del outbox sigue en `AGRO-FND-002`.

## Iteración 17 — AGRO-ID-001 reautenticación OIDC verificable (2026-08-10)

Estado inicial: `En revisión`. Una auditoría de readiness de `AGRO-ID-002` detectó que la sesión local marcaba `AuthenticatedAtUtc=now` al recibir cualquier callback OIDC. Eso permitía tratar una sesión SSO antigua como reautenticación reciente para vincular o desvincular identidades. La corrección pertenece al alcance y al gate de seguridad de `AGRO-ID-001`; no inicia `AGRO-ID-002` ni implementa MFA.

### Plan, aceptación y ownership

- [x] Capturar el instante del challenge dentro del `AuthenticationProperties` protegido y solicitar `max_age=0` al IdP.
- [x] Rechazar callbacks sin `auth_time`, con valor inválido, fuera de rango, futuro o anterior al challenge más la tolerancia documentada.
- [x] Derivar `AuthenticatedAtUtc` del claim validado, nunca de la hora local del callback.
- [x] Marcar explícitamente la procedencia verificada de la sesión; las sesiones legacy/N-1 fallan cerradas para mutaciones sensibles.
- [x] Mantener el proveedor sintético limitado a Development/Test con assurance explícita y sin afirmar equivalencia con Auth0.
- [x] Agregar migración aditiva compatible N/N-1 y pruebas de stale, legacy, malformed, replay y rollback/roll-forward.
- [x] Ejecutar gates .NET/PostgreSQL/OpenAPI, revisión independiente y actualizar evidencia de `AGRO-ID-001`.

Ownership exclusivo: principal sobre `apps/AgropecuarIA.Api/IdentityEndpoints.cs`, contrato OpenAPI, documentación, backlog y Git; Backend sobre `src/AgropecuarIA.Identity/**`, migración EF y tests .NET asignados; QA/AppSec revisan read-only el estado combinado. No hay cambio frontend salvo que el contrato observable lo exija.

No objetivos: passkeys, TOTP, recovery, roles, contexto tenant, delivery de notificaciones, despliegue ni credenciales Auth0. El gate Auth0 real de `AGRO-ID-001` permanece externo al repositorio.

### Revisión

- Resultado local: `PASS`. Restore locked y build Release 0 warnings/0 errors; suite raíz MTP 81/81, suite Identity posterior al último refuerzo 31/31, format y modelo EF sin drift.
- Seguridad: el challenge protegido emite `max_age=0`; el callback valida `auth_time` firmado contra el instante protegido con tolerancia acotada. Ausente, malformado, fuera de rango, stale, futuro o sin state falla cerrado.
- Persistencia: migración `20260810192543_AddAuthenticationAssuranceToSessions` aditiva `NOT NULL DEFAULT false`; filas existentes y writer N-1 permanecen sin assurance, writer N la marca solo tras validación. PostgreSQL real verificó upgrade, writer coexistente y rollback/roll-forward efímero.
- Revisión independiente QA y AppSec/Arquitectura: `PASS`, 0 críticos/altos/medios. NuGet vulnerable 0, secrets scan 0 y `git diff --check` PASS.
- Frontend, Playwright, contenedores y CI: N/A; no cambió UI, respuesta consumida, contenedor ni pipeline. El flujo visible conserva el mismo contrato y los fallos usan los estados existentes.
- Estado final: `En revisión`. El defecto local quedó corregido; Auth0 real aún debe demostrar `state`/nonce/code replay, `max_age=0`, `auth_time` y el comportamiento upstream de Google antes de `Completada` o deploy.
- Riesgo residual bajo: `OnRemoteFailure` aún agrega la categoría general `provider_unavailable` a rechazos de freshness; el rechazo es seguro pero se recomienda una métrica específica cuando se conecte el sandbox Auth0.

## Iteración 18 — AGRO-ID-002 step-up MFA ligado a propósito (2026-08-10)

Estado inicial: `Propuesto`. El sponsor seleccionó explícitamente `AGRO-ID-002`, delegó los defaults de producto reversibles y confirmó que las credenciales reales se incorporarán en el servidor de prueba. Transición registrada: `Propuesto → Ready` al aprobar la política MFA/recovery de desarrollo y acotar el primer sub-slice; pasará a `En curso` al comenzar la edición productiva. Clasificación: capacidad R1 de tamaño M; este sub-slice habilita assurance fuerte sin declarar implementado el lifecycle completo de factores.

### DoR, política y límites

- [x] Confirmar ID, outcome, actor, requisitos RF-ID-003/004/006, ADR-003, amenazas y criterios observables.
- [x] Confirmar que `AGRO-ID-001` provee sesión/identidad interna integrada; su gate Auth0 real se hereda como gate externo, pero no bloquea desarrollo local.
- [x] Fijar passkey como método preferido, TOTP como segundo factor/fallback, recovery codes de un uso y SMS fuera de alcance.
- [x] Fijar Auth0 como custodio de credenciales, semillas, códigos y factor IDs; AgropecuarIA conserva solo assurance gruesa, instante, propósito y evidencia de auditoría sin PII.
- [x] Fijar step-up one-shot de cinco minutos para `manage_authentication_methods`, ligado a usuario+sesión+identidad; exigir `max_age=0`, `acr_values` MFA, `amr=mfa` y `auth_time` firmado/fresco.
- [x] Diferir enforcement owner/admin/contador hasta que `AGRO-ID-003` entregue roles efectivos; no inventar autoridad a partir de strings o claims del IdP.
- [x] Fijar correo verificado como canal de recuperación/notificación candidato, sin implementar delivery de `AGRO-FND-002` ni afirmar validación real.
- [x] Acotar este sub-slice a assurance/step-up, rotación de sesión, UI y evidencia local. Alta/revocación passkey/TOTP, recovery real y notificación permanecen dentro de la tarea padre pero fuera de este incremento.

### Plan verificable

- [x] Publicar política y contrato HTTP/OpenAPI de intento, challenge, callback y assurance de sesión.
- [x] Agregar intento one-shot y assurance fuerte separados de la frescura OIDC de `AGRO-ID-001`.
- [x] Implementar inicio, validación `acr`/`amr`/`auth_time`/issuer/subject, consumo atómico y rotación de sesión sin extender su expiración absoluta.
- [x] Agregar migración aditiva N/N-1: nuevas columnas conservadoras y tabla efímera de intentos; ninguna sesión legacy se eleva automáticamente.
- [x] Emitir journal/outbox/telemetría acotados para inicio, éxito y rechazo sin token, subject, email, claims ni factor IDs.
- [x] Implementar UI accesible que muestre `primary`/`strong`, vencimiento, loading regional, proveedor caído, expiración/replay y reintento; el fixture fuerte solo existe en Development/Test.
- [x] Cubrir CSRF, propósito inválido, identidad/sesión cruzada, sesión revocada, `acr`/`amr` ausente, `auth_time` stale/futuro, doble callback, replay y rotación de cookie.
- [x] Ejecutar migración PostgreSQL real, restore/build/format/tests MTP, pnpm frozen/format/lint/typecheck/unit/build/E2E, SCA/secrets y revisión independiente.
- [x] Actualizar evidencia, backlog y riesgos; mantener `En curso` si el lifecycle real de factores continúa pendiente.

### Ownership disjunto

- Principal: contrato/política, `IdentityEndpoints.cs`, OIDC compartido, configuración, migración, integración, documentación, estados, gates y Git.
- Backend .NET: dominio/aplicación/EF no-migración y tests backend asignados; no edita API compartida, migraciones, frontend ni documentación.
- Frontend Next.js: feature Identity, estilos y tests frontend/E2E; no edita contratos, manifiestos/lockfile ni backend.
- QA y AppSec/Arquitectura: revisión final read-only sobre el estado combinado; ningún implementador aprueba su propio cambio.

### Baseline y gates previstos

- Baseline Git limpio en `main`, sincronizado con `origin/main`, commit `40b6d4717f2e66fb867e30ea9a033f18fb0a2bb3`.
- Runtime actual: .NET 10/MTP, PostgreSQL real efímero, 81 tests raíz; Next.js 16.3/React 19.2/TypeScript 6 con pnpm 10.33 y 18 tests Vitest.
- Comandos: locked restore; build Release 0 warnings; MTP con mínimo actualizado; format/model drift; pnpm frozen, format/lint/typecheck/test/build/E2E; auditoría de dependencias, secretos, `git diff --check` y revisión de migración N/N-1.
- N/A: deploy, aprovisionamiento Auth0, credenciales reales, delivery de correo y enforcement por rol. Son gates externos/dependencias nominadas, no aprobaciones implícitas.

### Review y evidencia final

- Resultado: sub-slice local de step-up MFA ligado a propósito aprobado. Separa frescura OIDC de assurance fuerte, consume el intento una vez, rota la sesión sin extenderla y presenta estado/vencimiento en una región accesible de la UI.
- Contrato/arquitectura: Auth0 conserva todo material de factor; AgropecuarIA usa un intento one-shot ligado a usuario+sesión+identidad+propósito, modelo aditivo y endpoint sintético físicamente limitado a `Development`/`Test`.
- Migración: `20260810195645_AddPurposeBoundStrongAuthentication` validada sobre PostgreSQL 17 efímero con writer N-1 antes/después del expand, writer N, constraints, rollback y roll-forward. El rollback compartido es operativo/roll-forward; `Down` queda limitado a base efímera.
- Backend: restore locked PASS; build Release 0 warnings/0 errors; MTP raíz 100/100; format PASS; EF sin cambios pendientes.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest 23/23; Playwright 4/4 desktop/mobile con Axe, teclado y viewport angosto. El loader afecta solo la tarjeta de assurance.
- Seguridad/supply chain: NuGet vulnerable 0, pnpm audit 0, secrets scan 0 y diff-check PASS. CSRF, rate limit, replay, exact-once, expiry, cookie rotada, usuario/sesión cruzados, sesión revocada y claims débiles/ajenos quedan cubiertos.
- Revisión independiente: QA y AppSec/Arquitectura PASS, 0 hallazgos críticos/altos/medios. Los hallazgos iniciales de CRLF, texto UTF-8 y cobertura negativa fueron corregidos y revalidados.
- Evidencia reproducible: `tasks/evidence/AGRO-ID-002/validation-report.md`, `mfa-recovery-policy.md` y `factor-loss-runbook.md`.
- Estado final: `En curso`. El sub-slice está terminado, pero la tarea padre aún requiere lifecycle real de passkeys/TOTP/recovery, sandbox Auth0, notificación, enforcement por roles y matriz de dispositivos/navegadores. No hubo deploy.

## Iteración 19 — AGRO-FND-001 cierre contractual R1 (2026-08-10)

Estado inicial: `En curso`. La auditoría posterior a `AGRO-ID-002` detectó que el runtime emite `IdentityStepUpCompleted`, pero el mapa contractual solo registra `IdentityLinked`. El cierre se acota a eliminar ese drift, hacer comprobable que todo evento Identity se publica desde un catálogo único y reproducir la DoD propia de FND-001. Backfill por lotes/ETag, restore de staging y delivery/idempotencia permanecen en `AGRO-FND-003`, `AGRO-PLT-004` y `AGRO-FND-002`; no se absorben aquí.

### Plan verificable

- [x] Registrar `IdentityStepUpCompleted` en el mapa de consumidores con provider, scope y ventana compatible.
- [x] Introducir un catálogo inmutable y único de eventos públicos Identity; los escritores outbox no aceptan strings contractuales dispersos.
- [x] Validar por fitness que catálogo runtime y mapa revisado coincidan exactamente en nombre, source, scope y major version.
- [x] Cubrir eventos desconocidos, contrato faltante, scope divergente y versión incompatible.
- [x] Reproducir restore locked, build Release, suite MTP raíz, format, modelo EF, JSON/SCA/secrets y diff-check.
- [x] Obtener revisión independiente QA y AppSec/Arquitectura; actualizar ADR/evidencia y completar FND-001 solo con gates verdes.
- [x] Commit/push autorizado y detenerse sin iniciar FND-002.

### Ownership y no objetivos

- Principal: catálogo/constructor compartido, integración, plan/evidencia/backlog, gates y Git.
- Architecture Lead: revisión read-only del alcance y compatibilidad; QA y AppSec: revisión final independiente.
- No hay frontend, endpoints, migración ni cambio de payload. No se implementan dispatcher, idempotency ledger, RLS tenant, backfill contractual, CI ni deploy.

### Review final

- Resultado: `AGRO-FND-001` completa su DoD R0/R1. El runtime Identity, el mapa de consumidores y el registro runtime contienen exactamente los mismos eventos públicos y schemas; el grafo/ownership continúa sin ciclos ni persistencia ajena.
- Contrato: catálogo exhaustivo derivado de enum, resolver fail-closed, aggregate type catalogado y constructors privados. Solo factories con payloads v1 tipados pueden crear outbox; source/scope/version/schema/aggregate no quedan como strings libres del writer.
- Compatibilidad: se preservó exactamente la shape v1 histórica. PostgreSQL prueba payload jsonb real antes y después del expand, writer N-1 posterior y coexistencia sin mutación. Reducir IDs requiere v2 explícito.
- Gates: restore locked PASS; build Release 0/0; fitness 60/60; Identity/PostgreSQL dirigido 13/13; raíz 114/114; format PASS; EF sin drift; 15 JSON PASS; NuGet vulnerable 0; secrets 0; diff-check PASS.
- Revisión independiente: QA y AppSec/Arquitectura PASS, cero críticos/altos/medios. Los blockers de catálogo eludible, JSON libre, payload breaking y fixture N/N-1 vacío se corrigieron y revalidaron.
- Autoevaluación: 97/100 — contexto/selección 15/15, arquitectura/código 20/20, multiagente 10/10, contrato/datos/observabilidad 14/15, tests/seguridad 20/20, preservación/cierre 18/20. Cero gate obligatorio fallido.
- Estado: `En curso → En revisión → Completada`. FND-002 conserva delivery/idempotencia/RLS; FND-003 conserva ETag/backfill/contract; PLT-004 conserva staging/backup/restore. No hubo frontend, API, migración ni deploy.

## Iteración 20 — AGRO-SEC-001 gate incremental R1 del runtime Identity/FND (2026-08-10)

Estado inicial: `En curso`. La selección autónoma descartó `AGRO-FND-002` porque todavía carece de una mutación tenant real y su primer consumidor pertenece a `AGRO-ID-003`; también descartó cerrar `AGRO-ID-001/002` porque sus siguientes gates requieren Auth0 real. `AGRO-SEC-001` sí tiene DoR para revisar la frontera ya integrada y su evidencia todavía describe un repositorio sin runtime, lockfiles ni controles emitidos.

### Assumption-validation check-in

- AgropecuarIA sigue siendo un SaaS online multiempresa para Argentina; el runtime local no está desplegado ni expuesto a Internet.
- API ASP.NET Core, web Next.js, PostgreSQL, OIDC Auth0 objetivo y adaptadores sintéticos Development/Test son las únicas superficies productivas integradas.
- Los datos de prueba son sintéticos; no existen secretos, PII real, proveedor contratado, CI productiva, edge ni telemetría remota.
- El sponsor delegó decisiones técnicas reversibles y pidió continuar sin checkpoints; las preguntas de región, DPA, retención, roles legales, ambiente compartido y Auth0 real permanecen explícitas y condicionan despliegue, no este gate local.
- No se implementan RLS tenant, idempotencia/delivery, passkeys/TOTP/recovery reales, CI ni deploy dentro de esta tarea continua.

### Plan verificable

- [x] Inventariar entrypoints, fronteras, datos, proveedores y controles del runtime Identity/FND con anclas a código, contratos y pruebas.
- [x] Reconciliar threat model, clasificación e inventario de procesamiento: separar runtime local, Development/Test, build/CI futuro y servicios externos no aprovisionados.
- [x] Actualizar amenazas existentes y, solo si el flujo lo exige, agregar abusos estables para sesión/OIDC, step-up, outbox, migraciones y supply chain.
- [x] Hacer que el validador falle si el runtime obligatorio, sus controles/pruebas o el gate R1 desaparecen de la evidencia.
- [x] Reproducir abuse tests dirigidos, suite de seguridad documental, build/test/format, SCA, secretos, JSON y diff-check.
- [x] Obtener revisión independiente QA y AppSec/Arquitectura; corregir hallazgos y documentar evidencia final.
- [x] Mantener `AGRO-SEC-001` en `En curso`, publicar commit/push autorizado y detenerse sin iniciar otra tarea.

### Ownership y no objetivos

- Principal: alcance, `tasks/todo .md`, integración, threat model narrativo, estado, gates y Git.
- Security/Data: registro JSON, inventario/clasificación y validador con ownership exclusivo de esos artefactos.
- Architecture/QA: revisión read-only del runtime y revisión final independiente; no editan evidencia poseída por Security/Data.
- No se cambia código productivo, OpenAPI, migraciones, frontend, configuración, manifiestos/lockfiles ni backlog normativo. Un hallazgo de código crítico/alto detiene el gate y se corrige dentro de SEC-001 solo si pertenece inequívocamente al control revisado.

### Baseline y gates previstos

- Baseline Git limpio en `main`, sincronizado con `origin/main`, commit `08b764de40fb8eaa5af432969929edf83cc5b49c`.
- Baseline documental: validador SEC `PASS`, 14 amenazas (7 críticas/7 altas), 7/7 mutation tests; ese verde cubre R0 pero no detecta drift contra el runtime R1.
- Gates: validador/mutation tests SEC; JSON/enlaces; restore locked; build Release sin warnings; MTP raíz con mínimo 114 y pruebas de abuso dirigidas; format/model drift; pnpm frozen/audit si la evidencia referencia el lockfile; NuGet vulnerable, secrets scan y `git diff --check`.

### Review final

- Resultado: `PASS` del gate local R1 para Identity/FND. El modelo, registro, clasificación e inventario distinguen superficies integradas, Development/Test y dependencias externas `NO-GO`; no se presenta el bootstrap local como despliegue aprobado.
- Gate de drift: 15/15 mutation tests, 14 amenazas (7 críticas/7 altas) y cobertura exacta de los 14 paths OpenAPI. El validador comprueba artefactos, paths y símbolos reales; la revisión humana conserva responsabilidad por la correspondencia semántica control↔test.
- Backend/datos: restore locked PASS; build Release 0 warnings/0 errors; suite raíz MTP 114/114; format PASS; EF sin drift. La carrera exact-once pasó 5/5 y ahora acepta las dos posiciones seguras del callback perdedor: `401` tras rotación o `409` tras autenticación previa.
- Frontend: pnpm frozen, format, lint, typecheck y build PASS; Vitest 23/23; Playwright 4/4 desktop/mobile con PostgreSQL 17 efímero. No se modificó frontend.
- Supply chain/calidad: NuGet vulnerable 0, pnpm audit 0, ambos JSON válidos, UTF-8 estricto 13/13, secret scan 0 y `git diff --check` PASS. No hay migración ni cambio de contrato/runtime productivo.
- Revisión independiente QA y AppSec/Arquitectura: `PASS`, 0 hallazgos críticos/altos/medios. Los blockers iniciales —drift no detectable, reporte R0 obsoleto, sobreafirmación de step-up, evidencia Production faltante y aserción concurrente rígida— quedaron corregidos y revalidados.
- Riesgos residuales: Auth0/factores reales, tenant/RLS/roles, edge/HSTS/hosts/key ring/limiter distribuido, OTLP, CI/SBOM/provenance, auditoría central, backup/restore, región/DPA/retención y notificación permanecen `NO-GO` para ambiente compartido o Internet.
- Autoevaluación: 96/100 — contexto/selección 15/15, arquitectura/código 19/20, multiagente 10/10, full-stack/datos/observabilidad 14/15, tests/seguridad 20/20, preservación/cierre 18/20. Cero gate obligatorio fallido.
- Estado: `En curso`. El incremento local está terminado; la tarea padre R0–R6 continúa y se reevaluará por slice. No hubo deploy.

## Iteración 21 — AGRO-DIS-003 discovery seguro de membresías (2026-08-10)

Estado inicial: `En revisión`. El sponsor aprobó continuar con la candidata recomendada. Clasificación: extensión R0 descartable del spike existente; no es código productivo ni inicia `AGRO-FND-002`/`AGRO-ID-003`.

### DoR, decisión y alcance

- [x] Confirmar que el discovery 0/1/N actual usa `FixtureIdentityDirectory`, mientras PostgreSQL solo permite RLS después de conocer `app.current_organization_id`.
- [x] Confirmar que `Organization` sigue siendo tenant técnico, pero CUIT, propiedad/control contractual y roles definitivos permanecen fuera del spike.
- [x] Elegir un principal DB exclusivo `agro_membership_discovery`: `LOGIN`, `NOINHERIT`, `NOBYPASSRLS`, sin ownership ni escritura.
- [x] Fijar `app.current_actor_id` mediante `set_config(..., true)` dentro de una transacción, derivado únicamente de la sesión server-side; ningún request acepta `userId` autoritativo.
- [x] Mantener policies de discovery separadas de las policies tenant; seleccionar organización solo revalida una membership activa y no entrega autoridad por el ID del cliente.
- [x] Basar la decisión en PostgreSQL 17: `FORCE ROW LEVEL SECURITY`, default-deny, `set_config` transaccional y rol runtime sin `BYPASSRLS`; Npgsql conserva conexión/transacción explícitas sobre un pool dedicado.

### Plan verificable

- [x] Capturar baseline del spike con PostgreSQL 17 efímero y suite raíz sin modificar artefactos productivos.
- [x] Agregar una migración R0 `003` con rol/grants/policies actor-scoped, datos mínimos y probes de catálogo/discovery.
- [x] Implementar un port pequeño de discovery PostgreSQL; reemplazar únicamente el listado in-memory y hacer la resolución de sesión async/cancelable.
- [x] Conservar 0 memberships como sesión platform-scoped sin tenant; 1 activa selección automática; N activas requieren selección; revocada/inactiva/ajena falla cerrada.
- [x] Revalidar la membership al cambiar organización, rotar sesión y resolver permisos/security version desde DB antes de acceder a datos tenant.
- [x] Probar 0/1/N, actor ausente/ajeno, org/membership inactiva, revocación entre listado y switch, orden/límite y pool tras commit/rollback/excepción/cancelación.
- [x] Verificar roles/grants: ningún runtime owner/superuser/`BYPASSRLS`; discovery sin escritura, sin `platform_user`, email, CUIT ni datos productivos.
- [x] Reconciliar contratos y evidencia histórica: separar sesión platform-scoped, discovery y contexto tenant; marcar identidad externa 1:N como cerrada por ID-001 y conservar gates Auth0/Legal externos.
- [x] Obtener revisión independiente DBA/AppSec y QA/Arquitectura; actualizar `ADR-PEND-007` solo si toda la evidencia técnica queda verde.
- [x] Ejecutar gates del spike y regresión raíz; commit/push autorizado y detenerse sin iniciar otra tarea.

### Ownership disjunto

- Principal: contrato compartido, `Program.cs`, plan/ADR/evidencia, integración, gates, backlog y Git.
- Database/Security: migración/probes/runner/scripts PostgreSQL del spike; no edita API, tests C# ni documentación principal.
- Backend .NET: port/repositorio, resolución de sesión y tests C# del spike; no edita SQL, `Program.cs`, contratos ni documentación.
- QA y Architecture/AppSec: revisión final read-only desde el estado combinado.

### No objetivos y gates

- No modificar `src/**`, `apps/**`, migraciones EF ni contratos productivos; no implementar invitaciones/roles, mutation ledger, outbox/inbox, worker, IdP real, CI o deploy.
- El rol de discovery sigue confiando en que el actor proviene del borde autenticado: RLS es defensa en profundidad, no mitigación de SQL injection o compromiso total del principal DB.
- Rollback: detener/eliminar el clúster efímero; la migración del spike no se copia a producción.
- La mención histórica a `trust` en Iteración 9 describe la baseline del 2026-08-05 y queda supersedida: el harness vigente exige SCRAM-SHA-256, cuatro secretos efímeros distintos y ACL owner-only.
- Gates: PostgreSQL probes; spike restore/build/MTP/format/SCA; contratos JSON; suite raíz 114/114; secrets/UTF-8/diff-check; revisiones independientes con cero hallazgos críticos/altos.

### Review final

- Resultado: `PASS` del incremento R0. PostgreSQL 17 limpio con SCRAM devolvió `catalog-security-pass`, `rls-isolation-pass`, `membership-discovery-pass` e `identity-spike-database-pass`; migration/probe `003` reejecutables.
- Spike: restore `PASS`; build Debug 0 warnings/errores; MTP 29/29; format `PASS`; NuGet SCA 0 vulnerabilidades conocidas; cleanup eliminó `.runtime`.
- Regresión raíz: restore locked `PASS`; build Release 0 warnings/errores; MTP 114/114; format `PASS`; EF sin cambios pendientes; NuGet SCA 0 vulnerabilidades conocidas.
- Seguridad: owner/superuser/`BYPASSRLS`/`INHERIT`/`CREATEDB`/`CREATEROLE`/`REPLICATION`, memberships u ownership indebidos fallan antes de servir. Reautorización del recurso comparte statement/snapshot con la lectura.
- QA y AppSec/Arquitectura independientes: `PASS`, cero hallazgos críticos, altos o medios. Los blockers iniciales de `trust`, principal fail-open, TOCTOU y cobertura documental fueron corregidos y revalidados.
- Contratos/documentación: 4 JSON válidos y UTF-8; discovery es contrato conceptual interno, no endpoint independiente; cero memberships conserva estado interno pero el HTTP histórico responde 403.
- Estado: `AGRO-DIS-003` continúa `En revisión` por Auth0/Legal/runtime productivo. `ADR-PEND-007` queda aceptada para desarrollo R1, no implementada en producción.
- No hubo cambios en `src/**`, `apps/**`, `tests/**`, frontend, migraciones EF, CI o deploy. Autoevaluación: 96/100; cero gate obligatorio fallido.

## Iteración 22 — AGRO-SEC-001 refresh de tenancy/RLS R0 (2026-08-10)

Estado inicial: `En curso`. El líder seleccionó la única tarea activa con un incremento local legítimo después de `AGRO-DIS-003`; clasificación R0/R1 de threat modeling y gate continuo. No implementa `AGRO-FND-002`, runtime tenant ni controles productivos.

### DoR, outcome y alcance

- [x] Confirmar repositorio limpio y `main` sincronizado en `014fb48`.
- [x] Confirmar que `ADR-PEND-007` ahora está aceptada para desarrollo R1 y que el discovery actor-scoped pasó PostgreSQL/SCRAM/RLS, pero sigue siendo spike descartable.
- [x] Detectar drift factual: `TM-001`, clasificación e inventario aún dicen que ADR/discovery están abiertos y que el harness usa `trust`.
- [x] Mantener el riesgo tenant como crítico hasta repetir el patrón en runtime productivo; no promover el spike a `integrated-local` ni inventar una nueva superficie HTTP.
- [x] Fijar contexto: SaaS online multiempresa argentino, `Organization` tenant técnico, runtime local no desplegado, Auth0/hosting/Legal externos y sin datos reales.

### Plan verificable

- [x] Reconciliar `TM-001` y documentos humanos con la decisión RLS/discovery aceptada, separando evidencia R0 de controles runtime.
- [x] Actualizar clasificación/inventario: SCRAM, secretos efímeros/ACL, principal discovery fail-fast y gates productivos restantes.
- [x] Endurecer el validador para rechazar declaraciones obsoletas sobre `ADR-PEND-007`, discovery pendiente o harness `trust`, con mutation self-test.
- [x] Anexar evidencia reproducible al validation report sin reescribir el historial R0/R1 previo.
- [x] Ejecutar validator/self-tests, JSON/UTF-8/secrets/diff-check y gates documentales aplicables.
- [x] Obtener revisión independiente QA y AppSec/Arquitectura, resolver hallazgos y conservar `AGRO-SEC-001` `En curso`.
- [x] Commit/push autorizado; detenerse sin iniciar `AGRO-FND-002`.

### Ownership disjunto

- Principal: selección, `tasks/todo .md`, validation report, integración, gates y Git.
- AppSec/Data: `threat-register.json`, clasificación, inventario y validador; no edita informe principal.
- Architecture: threat model humano y release gates; no edita registros JSON/validador.
- Principal QA: revisión final read-only y reproducción de gates.

### Baseline

- `validate-threat-model.ps1 -SelfTest`: `PASS`, 15/15 mutations; 14 amenazas, 7 críticas y 7 altas, ninguna crítica sin owner/test/gate.
- JSON parse: `PASS` para threat register y runtime surface register.
- Git: worktree limpio; no se cambia backlog ni código productivo.

### Review final

- Resultado: `PASS` del refresh R0/R1. El modelo, registro, clasificación e inventario reconocen `ADR-PEND-007` y las 29/29 pruebas del spike descartable sin atribuir RLS/discovery al runtime productivo.
- Gate afectado: `validate-threat-model.ps1 -SelfTest` `PASS`, 24/24 mutation tests; 14 amenazas (7 críticas/7 altas), ninguna crítica sin owner/test/gate. Las seis mutations positivas impiden perder silenciosamente ADR aceptada, 29/29, SCRAM, secretos distintos, ACL owner-only o fail-fast del principal; otras tres impiden reintroducir los claims obsoletos.
- Calidad documental: ambos JSON válidos; UTF-8 estricto, parser PowerShell, secret scan dirigido y `git diff --check` `PASS`.
- Revisión independiente QA y AppSec/Arquitectura: `PASS`, cero hallazgos críticos, altos o medios después de corregir la falta de mutations positivas. La simplificación del diagrama R0 queda como observación informativa; las fronteras sintética y de bootstrap están descritas en narrativa, tabla y gates.
- Gates .NET/frontend/EF/PostgreSQL/E2E/SCA: `N/A` para este diff exclusivamente documental; no se reutilizan resultados previos como sustituto del validador modificado.
- Estado: `AGRO-SEC-001` continúa `En curso` por ser gate multirelease. Tenant/RLS productivo, Auth0/hosting, CI/provenance, auditoría central, backup y decisiones Legal/retención siguen `NO-GO`.
- Siguiente candidato: `AGRO-FND-002`, sin iniciar. Antes necesita un consumidor tenant real y semánticas aprobadas de auditoría, orden, retry/poison e idempotencia/retención.
- Autoevaluación: 95/100; cero gate obligatorio fallido, sin cambios de producto, contrato, migración, configuración, manifiestos o lockfiles.

## Iteración 23 — AGRO-FND-002 protocolo idempotente y secuencia del primer consumidor (2026-08-10)

Estado inicial: `Propuesto`. El sponsor indicó continuar después de recibir el diagnóstico del ciclo `FND-002 → tenancy/RLS → ID-003/SEC-002 → FND-002`; esta continuación autoriza al líder a fijar defaults técnicos reversibles y la secuencia, sin absorber la implementación de `AGRO-ID-003` ni alterar requisitos, release o DoD.

### Outcome, DoR y límites

- [x] Confirmar `AGRO-FND-001` `Completada`, ADR-009, `RequestScope`, journal local y outbox tipado como precondiciones satisfechas.
- [x] Confirmar `ADR-PEND-007` aceptada para desarrollo R1 y mantener sus migraciones/roles productivos como trabajo del primer consumidor tenant, no del spike descartable.
- [x] Confirmar que el runtime actual solo tiene mutaciones Identity platform-scoped y que una tabla, endpoint o consumidor ficticio violaría la DoD.
- [x] Usar `CreateOrganization` de `AGRO-ID-003` únicamente como primer consumidor nominado futuro; no implementar organización, invitación, roles, RLS runtime ni UI en este incremento.
- [x] Separar TTL técnico de replay/conciliación de cualquier plazo legal: el desarrollo local no purga automáticamente y ningún default se presenta como política productiva.
- [x] Fijar fuentes primarias: `draft-ietf-httpapi-idempotency-key-header-07` expiró el 2026-04-18 sin publicarse como RFC y solo inspira un contrato propio versionado; EF Core exige tratar el commit incierto; PostgreSQL reserva `SKIP LOCKED` para tablas tipo cola.

### Plan verificable

- [x] Publicar una política canónica con identidad de clave, fingerprint, estados, autorización previa al replay, respuesta concurrente/expirada, transacción, orden, retry/poison y rollout.
- [x] Publicar la matriz de auditoría/retención que distingue journal local fail-closed, proyección central eventual, denegaciones, legal hold y datos prohibidos.
- [x] Resolver la secuencia: FND-002 entrega contrato/gates; ID-003 implementa el primer consumidor y su frontera tenant/RLS; FND-002 solo completa después de esa evidencia real.
- [x] Crear un validador reproducible que rechace pérdida de invariantes, contradicciones de identidad de clave, fake consumer, promoción del spike o plazos legales inventados.
- [x] Ejecutar validator/mutations, JSON/UTF-8/enlaces/parser/secrets/diff-check y revisión independiente Architecture, Security/Data y QA.
- [x] Transicionar solo `AGRO-FND-002` a `Ready → En curso` al demostrar la DoR documental; mantenerla `En curso` hasta el consumidor real.
- [x] Commit/push autorizado y detenerse sin iniciar `AGRO-ID-003`.

### Contrato preliminar a validar

- Unicidad tenant: `(tenant_id, operation, idempotency_key)`; `CreateOrganization` usa la excepción discriminada platform con namespace constante del servidor porque el tenant aún no existe. Actor, recurso/colección, versión de autorización y fingerprint quedan ligados y se reautorizan antes de cualquier replay. No hay lookup ni oracle de conflicto entre tenants.
- `Idempotency-Key`: valor opaco generado por cliente, 16–128 caracteres ASCII visibles, nunca UUID/tenant/actor derivado ni label/log. La API sigue validación estricta; RFC 9651 y el draft IETF son antecedentes no normativos y no convierten el valor en `sf-string`.
- Fingerprint: SHA-256 de una serialización canónica definida por operación sobre método, route template, versión de contrato y payload normalizado; no se persiste ni registra el payload crudo.
- Misma key+fingerprint tras autorización vigente reproduce status/body/header allow-listed; misma key con fingerprint distinto devuelve `409`; in-flight devuelve `409` retryable con `Retry-After`; respuesta expirada no reejecuta el hecho y exige conciliación/lookup del recurso.
- Negocio, ledger terminal, journal local y outbox se confirman en una transacción PostgreSQL. Audit/Compliance central es proyección at-least-once; una caída posterior no revierte el hecho ya confirmado.
- Delivery es at-least-once, consumidores deduplican `(consumer, event_id)`, orden solo por stream de agregado y los gaps se cuarentenan. No se promete exactly-once de transporte ni orden global.

### Ownership disjunto

- Principal: plan, secuencia/estado, decisiones globales, integración, fuentes, gates y Git.
- Architecture Lead: `tasks/evidence/AGRO-FND-002/idempotency-and-delivery-policy.md`.
- Security/Data: `tasks/evidence/AGRO-FND-002/audit-retention-and-threats.md`.
- QA Automation: `tasks/evidence/AGRO-FND-002/validate-foundation-protocol.ps1` y fixtures machine-readable propios.
- Ola 3: Architecture/AppSec/QA revisan archivos ajenos en modo read-only; ningún autor aprueba su propia entrega.

### No objetivos y gates

- No crear `src/**`, `apps/**`, migraciones EF, endpoint, worker, inbox/dispatcher, organización, roles, UI, manifiesto/lockfile, CI, Docker, credencial o deploy.
- .NET/frontend/PostgreSQL/E2E/SCA son `N/A` para este incremento contractual sin runtime afectado; no se reutilizan resultados previos como sustituto del validador nuevo.
- Gates aplicables: validador positivo y mutations negativas; JSON/UTF-8 estricto; referencias existentes; parser PowerShell; scan de secretos; `git diff --check`; revisión independiente y alcance Git.

### Review final

- Resultado: `PASS` del incremento contractual R1; transición documentada `Propuesto → Ready → En curso`. El contrato satisface la DoR del sub-slice, no la DoD de la tarea padre.
- Protocolo: unión discriminada `platform | tenant`; bootstrap `CreateOrganization` usa namespace platform constante server-side, sin tenant sintético; los tenants posteriores conservan namespaces independientes y errores sin oracle.
- Correcciones de revisión: fencing monotónico + lock/CAS owner/fence antes del negocio; identidad estable del ledger y aliases HMAC multiversión con intersección N/N-1, lazy alias y `alias_identity_split` fail-closed; poison de delivery separado de `failed_terminal`; replay reautoriza y no entrega body histórico ciegamente.
- Gate principal: `validate-foundation-protocol.ps1 -SelfTest` `PASS`, 44/44 mutations rechazadas y protocolo `1.0.0` válido con `runtimeImplemented=false`.
- Calidad documental: JSON, parser PowerShell, UTF-8 estricto sin BOM, LF/newline final/whitespace, referencias locales, secret scan y `git diff --check` `PASS`.
- Revisiones independientes Architecture, Security/Data y QA: `PASS`, cero hallazgos críticos, altos o medios después de resolver scope bootstrap, oracle cross-tenant, fencing, rotación HMAC y poison.
- Gates .NET/frontend/EF/PostgreSQL/E2E/SCA: `N/A`; no se modificó runtime, contrato HTTP, migración, paquete, lockfile, infraestructura o UI.
- Estado: `AGRO-FND-002` queda `En curso`. Faltan `AGRO-ID-003/CreateOrganization`, migraciones/principals/RLS productivos y pruebas reales de concurrencia, crash/replay, delivery y telemetría; no se inició esa tarea.
- Autoevaluación: 94/100; cero gate obligatorio fallido y cero cambio ajeno.

## Iteración 24 — AGRO-ID-003 CreateOrganization (2026-08-10)

Estado inicial: `Propuesto`. El sponsor confirmó registro público con datos privados, creación autónoma de múltiples organizaciones, creador como `owner` tenant y un posible superadmin futuro separado. El ID activo es `AGRO-ID-003`; se ejecuta un único sub-slice R1 cohesivo y la tarea padre permanecerá `En curso` porque invitaciones, matriz completa de roles y alcances por campo no forman parte de este incremento.

### DoR, outcome y decisiones vinculantes

- [x] Confirmar que `AGRO-FND-002` nominó `AGRO-ID-003/CreateOrganization` como primer consumidor real de su protocolo y conserva `runtimeImplemented=false` hasta esta integración.
- [x] Confirmar que la sesión local de `AGRO-ID-001` deriva usuario, verificación y `AuthenticatedAtUtc` del proveedor; los gates Auth0 externos siguen siendo de despliegue, no bloquean el slice local.
- [x] Fijar registro público como creación de cuenta, no exposición pública de organizaciones ni datos productivos.
- [x] Fijar que cualquier usuario autenticado, verificado y con autenticación reciente menor a 15 minutos puede crear múltiples organizaciones; no se exige MFA fuerte para este bootstrap.
- [x] Fijar que el creador queda como único `owner` activo inicial. `owner` es un rol tenant y jamás concede `platform superadmin`.
- [x] Fijar la regla de último owner para mutaciones posteriores: no podrá removerse, demoverse ni abandonar si dejaría la organización sin owner activo.
- [x] Confirmar que nombres de organización no son únicos globalmente, CUIT no autoriza y no se inventa un límite comercial de organizaciones.
- [x] Obtener GO read-only de Architecture, Product/QA y Security/Data para el sub-slice; conservar superadmin, soporte JIT, observabilidad global, invitaciones, cambios de rol y campos fuera de alcance.

Outcome observable: un usuario con sesión verificada y reciente crea una organización privada y queda owner en una única transacción PostgreSQL idempotente; la UI muestra 0/1/N organizaciones sin bloquear el shell y ningún usuario, tenant, job o principal DB puede observar o reproducir datos de otro tenant.

### Aceptación del sub-slice

- [x] `POST /api/identity/organizations` acepta solo `displayName`; actor, scope, owner y tenant se derivan en servidor. Cookie, CSRF, rate limit e `Idempotency-Key` son obligatorios.
- [x] Autenticación verificada y reciente `< 15 minutos` permite crear; frontera de 15 minutos, sesión no verificada, stale, expirada o revocada fallan sin efecto.
- [x] Organización, owner membership, ledger FND-002 terminal, journal local y outbox tipado se confirman atómicamente; cualquier fallo inyectado deja cero parcial.
- [x] Misma key+fingerprint concurrente produce una organización, una membresía, un journal y un outbox; mismatch, in-flight, commit desconocido, replay stale y response expirada siguen el protocolo 1.0.0 sin oracle cross-tenant.
- [x] El mismo usuario puede crear Org A y B con claves distintas; nombres duplicados son válidos y el discovery de sesión devuelve exclusivamente membresías activas propias en orden determinista.
- [x] PostgreSQL usa principals mínimos, `FORCE RLS`, contexto actor/organización transaction-local y falla cerrado para tenant A/B/sin contexto, pool reutilizado, job sin scope y principal owner/BYPASS.
- [x] La migración es expand/N-N-1 compatible: el writer previo sigue funcionando, rollback de aplicación y roll-forward preservan datos y no duplican efectos.
- [x] La UI cubre 0/1/N organizaciones, crear otra, submitting localizado, validación, unauthorized, reauth requerida, rate-limit, conflicto/in-flight, reconciliación y offline; teclado, foco, lector, 390 px y UUID corto cumplen las reglas globales.
- [x] Telemetría usa outcomes allow-listed sin nombre, UUID, user/tenant, key, digest ni payload; evidencia local diagnostica éxito, denegación, replay, conflicto y commit desconocido.
- [x] Contrato HTTP, evento, schema, consumer/runtime maps, migración, clientes, fixtures y documentación quedan alineados y revisados independientemente.

### Plan verificable

- [x] Diseñar primero OpenAPI, errores Problem Details, evento `OrganizationCreated` v1 y modelo persistente mínimo compatible.
- [x] Implementar dominio/aplicación/persistencia/API con autorización previa al lookup, ledger HMAC/fencing, transacción y RLS.
- [x] Integrar onboarding/selector en Identity Hub con cliente TypeScript estricto y estados accesibles/localizados.
- [x] Agregar pruebas unitarias, PostgreSQL/API/seguridad/concurrencia/migración, frontend y Playwright desktop/mobile.
- [x] Integrar temprano y ejecutar gates parciales después de contrato, backend y frontend.
- [x] Someter el sub-slice a Ola 3 independiente, resolver hallazgos y repetir todos los gates sin sacar a la tarea padre de `En curso`.
- [x] Documentar evidencia, mantener `AGRO-ID-003` `En curso`, reconciliar `AGRO-FND-002` sin cerrarla prematuramente y publicar commit/push autorizado.

### Ownership disjunto

- Principal: plan/estados, OpenAPI, contratos/eventos compartidos, migration/snapshot, configuración transversal, integración, gates y Git.
- Backend .NET: dominio/aplicación/persistencia/API y pruebas backend asignadas; no edita OpenAPI, migraciones ni frontend.
- Frontend Next.js: feature Identity/Organization y pruebas unitarias; no edita backend, OpenAPI, manifiestos ni lockfiles.
- Database/Security: revisión y pruebas RLS/principals/migración bajo archivos exclusivos; no comparte migration con el principal.
- QA Automation: fixtures/E2E y revisión reproducible; no modifica implementación salvo corrección expresamente reasignada.

### Baseline antes de editar código

- Git: `main`, HEAD `30bc893edeff70d6670945413c01518d871fa1c5`, worktree limpio.
- Backend: restore locked `PASS`; build Release 0 warnings/errores; MTP 114/114; format `PASS`.
- Frontend: pnpm frozen `PASS`; format/lint/typecheck `PASS`; Vitest 23/23; Next.js 16.3.0 build `PASS`.
- Transición autorizada por selección explícita y DoR acotada: `Propuesto → Ready → En curso` al publicar este plan. La tarea padre no será `Completada` en este incremento.

### No objetivos

- No implementar invitaciones, aceptación/revocación, otros roles, democión/transferencia/último-owner runtime, scopes por campo ni creación GIS.
- No implementar superadmin, impersonación, soporte cross-tenant, collector/dashboard global, CI, Docker, deploy ni secretos reales.
- No afirmar que un owner posee legalmente el establecimiento o CUIT; Organization es el tenant técnico y los datos siguen privados.

### Review final

- Resultado: `PASS` local del sub-slice `CreateOrganization`; `AGRO-ID-003` permanece `En curso` por invitaciones, matriz de roles, último-owner runtime y scopes por campo.
- Valor: cualquier usuario con sesión verificada y autenticación reciente puede crear múltiples organizaciones privadas y queda como `owner` tenant, sin capacidad platform/superadmin.
- Atomicidad e idempotencia: Organization, membership autoritativa y legacy, ledger/aliases HMAC, journal y `OrganizationCreated` se confirman juntos; replay, rotación, conflicto, expiración, commit desconocido y fallos inyectados quedan cubiertos y fail-closed.
- Datos y seguridad: principals mínimos, grants por columna, `SET LOCAL`, `FORCE RLS`, A/B/sin contexto, pool/job y rollback N/N-1 demostrados en PostgreSQL real.
- Backend: restore locked, build Release 0 warnings/errores, MTP 142/142, format y EF pending-model `PASS`.
- Frontend: pnpm frozen, format/lint/typecheck/build `PASS`; Vitest 50/50; Playwright 4/4 desktop/mobile con Axe, teclado y 390 px.
- Contratos/seguridad: FND 45/45, SEC 25/25, SCA NuGet/pnpm, JSON, UTF-8, secrets y diff-check `PASS`; revisión independiente sin críticos/altos/medios abiertos.
- Compatibilidad: migración expand, writer N-1 coexistente, app rollback y roll-forward demostrados; `Down` destructivo queda limitado a base efímera.
- Riesgos externos: Auth0/edge, secretos administrados, principal de ambiente compartido, rate limit distribuido, Audit central y retención legal siguen NO-GO de deploy, no de desarrollo local.
- Publicación: commit/push autorizados; sin deploy. No se inició una segunda tarea.
- Autoevaluación: 96/100; cero gate obligatorio fallido y cero cambio ajeno conocido.

## Iteración 25 — AGRO-ID-003 invitación one-shot de co-owner (2026-08-11)

Estado inicial: `En curso`. El incremento anterior entregó `CreateOrganization`; esta continuación conserva el mismo ID y agrega colaboración mínima sin inventar la matriz completa de roles.

### DoR y decisiones vinculantes

- [x] Auditar backlog, modelo, RLS, sesión, step-up, OpenAPI y UI existentes con Product, AppSec/Data y QA independientes.
- [x] Confirmar que roles distintos de `owner`, scopes por campo y invitación por email no están Ready.
- [x] Fijar el slice a invitación mediante enlace one-shot para agregar exclusivamente un `co-owner` (`role=owner` server-side).
- [x] Fijar actor y assurance: owner activo; crear/revocar con purpose `manage_organization_owners`; aceptar con identidad verificada y autenticación reciente `<15m`.
- [x] Fijar token 256-bit, fragmento URL, persistencia solo digest versionado, TTL configurable de 7 días y estados pending/accepted/revoked/expired.
- [x] Mantener fuera email/delivery, otros roles, democión/remoción/último-owner runtime, GIS/campos, superadmin y soporte JIT.

### Aceptación observable

- [x] Owner A crea una invitación bajo Org A; recibe metadata y token una sola vez, `no-store`, sin email/rol/tenant autoritativo en el body.
- [x] Crear/revocar/listar revalida owner y tenant en la misma transacción; owner/usuario de B y sesión sin contexto obtienen error neutral sin existencia.
- [x] Invitado verificado acepta antes del vencimiento y obtiene exactamente una membership owner autoritativa más proyección N-1; replay propio devuelve el mismo resultado.
- [x] Token malformado, robado/reutilizado por otro actor, expirado, revocado o aceptado concurrentemente no crea efectos adicionales.
- [x] Accept-vs-revoke produce un único estado terminal; fallos de journal/outbox revierten invitación/membership/ledger.
- [x] Evento(s), journal y telemetría omiten token/digest/nombre/user/tenant como labels o payload sensible.
- [x] PostgreSQL demuestra `FORCE RLS`, grants mínimos, A/B/sin contexto/pool/job, migración expand N/N-1, app rollback y roll-forward.
- [x] UI cubre create/copy-once/list/revoke/accept, login/reauth, loading regional, empty/offline/error/expired/conflict, foco/teclado/Axe/390px y UUID corto.

### Plan y ownership

- [x] Principal: OpenAPI, API composition, eventos/schemas/maps, configuración transversal, plan/evidencia, integración, gates y Git.
- [x] Backend: dominio/aplicación y pruebas de servicio/API bajo archivos exclusivos; no edita migration/OpenAPI/frontend.
- [x] Database/Security: DbContext, migration/designer/snapshot, policies/grants y pruebas PostgreSQL; espera modelo congelado.
- [x] Frontend: cliente/tipos/hub/vistas/CSS y Vitest; no edita backend/OpenAPI/E2E runner.
- [x] QA/E2E: fixtures y journey inviter/invitee/attacker desktop/mobile; revisión final read-only independiente.
- [x] Ejecutar restore/build/MTP/format/EF, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC, SCA, JSON/UTF-8/secrets/diff.
- [x] Mantener `AGRO-ID-003` `En curso`, documentar residuales y publicar un único commit/push autorizado; no iniciar otra tarea.

### Baseline

- Git `main` limpio en `0583708`; origin/main coincide.
- Gate previo integrado: .NET 142/142, build Release 0/0, EF sin drift; Vitest 50/50; Playwright 4/4; FND 45/45; SEC 25/25; SCA 0.

### Review final

- Resultado: `PASS` local del sub-slice de invitación one-shot de co-owner; `AGRO-ID-003` permanece `En curso` por roles no-owner, scopes por campo, remoción/democión/último-owner runtime e invitaciones dirigidas por email.
- Contrato y seguridad: token CSPRNG de 256 bits visible una sola vez y persistido solo como HMAC versionado; fragmento URL, respuestas `no-store`, errores neutrales, step-up purpose-bound para crear/revocar y aceptación con identidad verificada reciente.
- Persistencia: invitación, membership autoritativa y proyección N-1, journal y outbox tipado son atómicos; concurrencia create/accept/revoke, expiración exacta, replay, rotación/retirada de claves y fallos inyectados quedan fail-closed.
- PostgreSQL: migración expand, writer N-1, roll-forward y rollback efímero; `FORCE RLS`, roles/grants mínimos, A/B/sin contexto/pool/job y funciones estrechas SECURITY DEFINER demostradas.
- Backend: restore locked y build Release `PASS` con 0 warnings/errores; suite raíz MTP `170/170`; format y EF pending-model `PASS`.
- Frontend: pnpm frozen, format, lint, typecheck y Next build `PASS`; Vitest `67/67`; Playwright `4/4` en Chromium desktop/móvil con invitador, invitado distinto, atacante, revocación, Axe, teclado y 390 px.
- Arquitectura/seguridad: fitness incluido en la suite (`70/70`), FND `45/45`, SEC `26/26`, NuGet/pnpm SCA, JSON, UTF-8 y `git diff --check` `PASS`.
- Hallazgos de integración corregidos: versión OpenAPI mutada incorrectamente, agregado tenant no declarado, constraint SQL NULL, fixture E2E compartida/rate limit, navegación por hash en SPA y confirmación de revocación.
- Residuales: Auth0/hosting/secret manager/limiter distribuido/Audit central/retención legal siguen NO-GO para ambiente compartido; el fragmento se procesa al montar la app y el journey soportado abrir enlace → login → reload queda cubierto.

## Iteración 26 — AGRO-GIS-001 referencia territorial v1 (2026-08-11)

Estado inicial: `Propuesto`. Se selecciona autónomamente como el siguiente puente directo entre organizaciones privadas y la futura creación de campos. Transición del sub-slice: `Propuesto → Ready → En curso`; la tarea padre permanecerá `En curso` hasta incorporar snapshot jerárquico completo, operación de actualización y evidencia productiva del proveedor.

### DoR, decisiones y no objetivos

- [x] Verificar que `AGRO-DIS-004` aprobó WGS84/Georef/MapLibre de forma condicional para desarrollo local y que los pendientes de proveedor/DPA/SLA son gates de ambiente compartido.
- [x] Verificar el fixture reproducible de las 23 provincias+CABA con códigos oficiales y centroides públicos; no interpretarlos como campos, parcelas ni precisión agronómica.
- [x] Elegir módulo `territory` separado del schema `identity`, dentro del monolito modular y sin microservicio, broker o base compartida entre módulos.
- [x] Fijar snapshot inmutable/versionado y modelo provider-neutral de niveles `province|department|municipality|locality`; el seed local cubre provincias y el importador admite jerarquía completa sin inventarla.
- [x] Fijar búsqueda local como fallback durable y resolución por coordenada mediante adapter Georef de host fijo; si no existe respuesta/caché válida, devolver `unavailable` y ofrecer búsqueda manual, nunca inferir código por cercanía al centroide.
- [x] Fijar que búsqueda/resolución requieren sesión autenticada, son solo lectura, usan rate limit y no reciben tenant, organización, nombre de campo ni PII.
- [x] Mantener fuera creación de campos/geometrías, mapa/tiles productivos, clima, catastro legal, restricciones agronómicas, job/scheduler de sync y deploy.

Outcome observable: una persona autenticada busca territorio argentino sin lupa con resultados jerárquicos oficiales del snapshot activo. La resolución por coordenada se verifica localmente con respuestas literales del adapter, pero el egress real permanece deshabilitado por defecto y NO-GO hasta aprobar proveedor/Legal; sin respuesta habilitada, la UI degrada explícitamente a búsqueda manual. Los 24 centroides contractuales validan cobertura nacional sin persistir coordenadas de campos.

### Aceptación verificable

- [x] Nuevo módulo .NET `AgropecuarIA.Territory` con límites Domain/Application/Infrastructure, schema PostgreSQL `territory` y composición explícita en la API.
- [x] Snapshot activo inmutable con fuente, versión, captura, hash y estado; units con código oficial, nivel, parent, nombre y nombre normalizado; constraints evitan parent inválido, duplicados y múltiples snapshots activos.
- [x] Seed expand local contiene exactamente 24 provincias/CABA del fixture oficial, incluyendo Tierra del Fuego, con source/version/hash reproducibles.
- [x] Importador valida códigos, niveles, parents, Unicode, duplicados, ciclos, hash y cobertura antes de activar atómicamente; una activación fallida conserva el snapshot anterior.
- [x] `GET /api/territory/search` aplica query normalizada, level/parent/limit acotados, orden determinista y devuelve fuente/versión/frescura; homónimos conservan jerarquía.
- [x] `GET /api/territory/resolve` valida WGS84/Argentina, usa `IHttpClientFactory`, host fijo, timeout/tamaño/schema acotados y estados `fresh|stale|unavailable`; fallo externo no inventa territorio.
- [x] Cache de resolución es derivable, acotada y no persiste/loguea coordenadas; caída sin cache ofrece fallback manual desde el snapshot.
- [x] UI autenticada ofrece autocomplete reactivo con debounce/cancelación, sin lupa, loader solo en la región de resultados, estados empty/error/unavailable y navegación por teclado/móvil 390 px/Axe.
- [x] OpenAPI, runtime map, module boundaries y threat model reflejan el nuevo módulo/superficie sin declarar mapa/campos/proveedor productivo.
- [x] Tests cubren 24 jurisdicciones, acentos/homónimos, parent/level, límites/coordenadas, payload externo inválido/HTML/truncado/429/500/timeout, cache stale y PostgreSQL empty→N/rollback/roll-forward. No existía un writer Territory N-1; la compatibilidad demostrada es aditiva y coexiste con el runtime Identity sin alterar su schema.

### Ownership y gates

- [x] Principal: plan, OpenAPI/contratos compartidos, solución/composition root, mapas/evidencia, integración, gates y Git.
- [x] Backend: dominio/aplicación, adapter Georef, endpoints del módulo y tests no-DB bajo ownership exclusivo.
- [x] Database/Security: DbContext, migración/seed, importer persistente, constraints y tests PostgreSQL bajo ownership exclusivo.
- [x] Frontend/QA: cliente/tipos/UI y Vitest/E2E de búsqueda/degradación bajo ownership exclusivo.
- [x] Ejecutar restore locked, build Release, MTP, format, EF pending/migrations PostgreSQL, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC, SCA, JSON/UTF-8/secrets/diff y revisión independiente.
- [x] Documentar resultados, mantener `AGRO-GIS-001` `En curso`, commit/push autorizado y no iniciar `AGRO-GIS-002` en este incremento.

### Baseline

- Git `main` y `origin/main` en `4d4893f`, worktree limpio.
- Backend: build Release 0 warnings/errores; MTP 170/170; EF sin drift.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest 67/67; Playwright 4/4.
- Validadores: FND 45/45; SEC 26/26; SCA y secret scan sin hallazgos.

### Review final

- Resultado del incremento local: `PASS`; `AGRO-GIS-001` queda `En curso` por fuente jerárquica completa, operación administrada de actualización y gates externos de Georef/Legal/ambiente compartido.
- Backend: restore locked `PASS`; build Release 0 warnings/errores; suite raíz MTP `223/223`; Territory/PostgreSQL `44/44`; Architecture Fitness `79/79`; `dotnet format` y EF pending-model de Identity/Territory `PASS`.
- Frontend: pnpm frozen, Prettier, lint, typecheck y Next build `PASS`; Vitest `79/79`; Playwright Chromium+mobile `6/6`, incluida degradación sin egress, teclado, Axe y viewport 390 px.
- Seguridad/contratos: FND protocol `45/45`, SEC threat model `41/41`, OpenAPI/runtime map/RLS/hash/provider guards `PASS`; NuGet y pnpm sin vulnerabilidades conocidas; secretos, JSON, UTF-8 y diff-check sin hallazgos.
- Persistencia: primer schema Territory validado empty→N, seed/hash reproducibles, activación atómica, rollback/roll-forward efímero y convivencia aditiva con Identity. No se afirma un writer Territory N-1 inexistente.
- Revisión independiente: sin hallazgos críticos/altos/medios abiertos. Se corrigieron shape real `gobierno_local`, egress default-off, logging HTTP sin URI, hash NFC completo, homónimos, payload truncado, coordenadas echoed, contrato Problem/Retry-After/parent, copy de frescura y evidencia E2E.

## Iteración 27 — AGRO-SEC-002 frontera tenant Identity v1 (2026-08-11)

Estado inicial: `Propuesto`. Tras auditar readiness con Architecture/Data, Product/QA y AppSec, se selecciona el primer incremento ejecutable por módulo y se transiciona `Propuesto → Ready → En curso`. La tarea padre R1–R6 permanecerá `En curso` para futuras superficies, jobs, storage, exports e IA.

### DoR, alcance y no objetivos

- [x] Verificar que `ID-003` y el contrato local de `FND-002` aportan rutas tenant reales, RLS, replay/idempotencia y tests PostgreSQL reproducibles.
- [x] Elegir exclusivamente la frontera actual Identity + Territory; no crear roles, endpoints, campos, jobs, cache tenant, storage, export, retrieval, DAST remoto ni deploy.
- [x] Tratar Territory como referencia compartida autenticada y sin datos tenant; no inventar RLS tenant para un snapshot oficial platform-owned.
- [x] Fijar output del audit en `tasks/evidence/AGRO-SEC-002/`, con matriz ejecutable, arquitectura, reportes y findings estructurados.
- [x] Fijar que una ruta futura no puede entrar al runtime sin resource/action/scope/context/authz/storage/error/tests/owner registrados.

Outcome observable: un gate reproducible cubre exactamente todas las operaciones HTTP actuales de Identity y Territory, distingue `public-platform`, `authenticated-platform`, `tenant`, `shared-reference` y `development-test-only`, y falla si una nueva ruta tenant carece de autorización por recurso, contexto server-derived, RLS/default-deny, error neutral o caso negativo.

### Aceptación verificable

- [x] Inventario machine-readable completo de operaciones OpenAPI/runtime, sin duplicados ni rutas huérfanas.
- [x] Cada operación declara recurso, acción, boundary, autenticación, fuente de actor/tenant, autorización de aplicación, frontera de persistencia/cache, error neutral, owner y tests ejecutables.
- [x] Rutas tenant de owner-invitations demuestran Org A/B, actor ajeno/sin contexto, sesión revocada, replay, concurrencia, pool/job y `FORCE RLS` con principal sin ownership/BYPASS. La revocación de membership no existe todavía y queda para el futuro slice de remoción de co-owner.
- [x] Bootstrap `CreateOrganization` y accept por token permanecen platform-scoped pero reautorizan actor/sesión antes de lookup/replay y fijan tenant server-side después de resolverlo.
- [x] Territory search/resolve requieren sesión, no aceptan tenant, no persisten coordenadas y mantienen egress default-off; el cache global queda como gate de privacidad antes de habilitar Georef multiusuario.
- [x] Rutas Development/Test están clasificadas y el gate enlaza prueba de ausencia fuera de esos ambientes.
- [x] Jobs, storage, export y AI figuran `not-present`, no `approved`; la tarea no afirma cobertura de superficies inexistentes.
- [x] Mutations negativas rompen por ruta ausente, método/grupo nuevo, scope falso, tenant client-authoritative, falta de authz/RLS/error/test/owner, shared-reference sin minimización y superficie inexistente marcada aprobada.
- [x] Security audit entrega `architecture.md`, `REPORT.md`, `FINDINGS-DETAIL.md` y `findings.json` validados; no hubo hallazgos confirmados que exigieran cambio productivo.

### Ownership y gates

- [x] Principal: plan, matriz/validator, fitness, integración, evidencias, gates y Git.
- [x] Security/Data: revisar RLS/roles/grants/functions/pool/job/replay y hunting de BOLA; no editar producto salvo defecto confirmado y asignado.
- [x] Architecture: verificar cobertura OpenAPI/runtime y clasificaciones platform/tenant/shared/dev.
- [x] Product/QA: fixtures A/B/attacker/revoked y acceptance; revisión independiente final.
- [x] Ejecutar restore locked, build Release, MTP raíz y dirigidos PostgreSQL, format, EF pending, E2E existente, validadores FND/SEC/SEC-002, SCA, JSON/UTF-8/secrets/diff.
- [x] Documentar resultados, mantener `AGRO-SEC-002` `En curso`, commit/push autorizado y no iniciar revocación de co-owner en esta iteración.

### Baseline

- Git `main`/`origin/main` limpios en `15ead58`.
- Backend: restore/build/format/EF PASS; MTP `223/223`; Territory `44/44`; fitness `79/79`.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest `79/79`; Playwright `6/6`.
- Validadores: FND `45/45`; SEC `41/41`; SCA y secret scan sin hallazgos.

### Review final

- Resultado: `PASS` del incremento Identity tenant v1; `AGRO-SEC-002` permanece `En curso` porque storage, jobs, export, retrieval, IA y nuevas superficies tenant aún no existen.
- Gate: registro estricto de `20/20` operaciones HTTP, callback OIDC y cinco superficies futuras ausentes; OpenAPI, rutas source, ownership, tests y boundary semantics se validan en Architecture Fitness.
- Mutations: `16/16` negativos más el caso publicado prueban ruta faltante, método/grupo nuevo, IDs únicos, boundary, transiciones platform→tenant, tenant client-authoritative, authz, `FORCE RLS`, error neutral, shared reference, Dev/Test, test/símbolo, owner, OIDC y superficies futuras.
- Security audit: cero findings Critical/High/Medium/Low explotables en runtime default. El oracle temporal potencial del cache Territory es condicional a Georef habilitado y queda como gate previo a egress multiusuario.
- Backend: restore locked, build Release 0 warnings/errores, MTP raíz `240/240`, fitness `96/96`, format y EF pending-model de ambos contextos `PASS`.
- Frontend: pnpm frozen, format, lint, typecheck, Vitest `79/79`, Next build y Playwright `6/6` `PASS`; no hubo cambio productivo frontend.
- Validadores y supply chain: FND `45/45`, SEC `41/41`, findings schema, JSON/UTF-8, NuGet/pnpm SCA, secrets y diff-check `PASS`.
- Revisión independiente: autorización tenant/RLS `23/23` dirigida y auth/browser `79/79`; se corrigieron extracción de PUT/PATCH/grupos nuevos, invariantes shared-reference y el claim imposible de membership revocada.
- Publicación: un único commit/push autorizado, sin deploy y sin iniciar revocación de co-owner.

## Iteración 28 — AGRO-ID-003 remoción segura de co-owner (2026-08-18)

Estado inicial: `Ready` para un sub-slice estrecho. `AGRO-ID-003` permanece `En curso`; esta iteración no implementa salida voluntaria, transferencia, democión, roles adicionales, scopes por campo, email ni superadmin.

### DoR, alcance y decisiones

- [x] Fijar que cualquier owner activo puede remover a otro owner activo; todos los owners son simétricos y el creador no tiene privilegio especial.
- [x] Excluir `actor == target`; self-remove/leave requiere un slice posterior.
- [x] Exigir sesión vigente y step-up purpose-bound `manage_organization_owners` para remover; el listado requiere owner activo.
- [x] Modelar remoción autoritativa como estado terminal `removed`, conservar historial y eliminar la proyección legacy en la misma transacción.
- [x] Proteger `>= 1` owner activo con serialización/lock por organización y una primitiva PostgreSQL estrecha; no conceder UPDATE/DELETE amplios al rol app.
- [x] Revocar atómicamente invitaciones pendientes creadas por el owner removido y serializar create-invitation contra la misma organización.
- [x] Mantener fail-closed la reactivación de una membership removida; reinvite/reactivación queda fuera de esta iteración.

Outcome observable: el owner A lista los co-owners activos de Org A y remueve a B con CSRF, `If-Match` e idempotencia. B conserva su cuenta de plataforma, pero pierde inmediatamente Org A y todas sus rutas tenant; carreras nunca dejan la organización sin owner.

### Aceptación verificable

- [x] `GET /api/identity/organizations/{organizationId}/owner-memberships` devuelve sólo owners activos con display name, membership UUID, versión y marca `isCurrentUser`; nunca expone userId, email ni identidad externa.
- [x] `DELETE /api/identity/organizations/{organizationId}/owner-memberships/{membershipId}` deriva actor/tenant server-side, exige CSRF, `If-Match`, `Idempotency-Key` y assurance vigente.
- [x] Org ajena, actor no-owner, target ausente/ajeno/removido y self-target responden neutralmente sin oracle; ETag stale, last-owner, replay, mismatch e in-flight tienen errores tipados.
- [x] Membership autoritativa queda `removed`, incrementa security/concurrency version, conserva historial y desaparece de `organization_memberships` legacy y de `/session`.
- [x] Membership, proyección legacy, invitaciones pendientes, ledger, journal y outbox cambian en una transacción; fault injection demuestra rollback total.
- [x] Dos remociones concurrentes dejan exactamente un owner activo; retry/replay produce un solo efecto, journal y evento.
- [x] RLS/roles/grants prueban A/B/sin contexto/pool/job, actor removido y ausencia de UPDATE/DELETE amplio; la función privilegiada no queda expuesta a PUBLIC/job/discovery.
- [x] OpenAPI, evento tipado, schema/runtime/consumer maps y SEC-002 registran las dos rutas y fallan ante drift.
- [x] UI muestra `Co-owners`, UUID corto, confirmación accesible, reauth, loading/offline/error/stale/last-owner, foco/teclado/Axe y viewport 390 px.
- [x] E2E demuestra A invita B, B acepta, A remueve B y B pierde Org A tras refrescar, en desktop y mobile.

### Ownership y gates

- [x] Principal: decisiones, plan, contratos compartidos, eventos/maps, integración, documentación, gates y Git.
- [x] Backend: dominio/aplicación/API/telemetría y pruebas funcionales bajo ownership exclusivo.
- [x] Database/Security: DbContext, migración, función/roles/RLS y pruebas PostgreSQL bajo ownership exclusivo.
- [x] Frontend/QA: tipos/cliente/UI, Vitest y E2E bajo ownership exclusivo.
- [x] Ejecutar restore locked, build Release, MTP raíz/dirigidos, format, EF pending/migración N/N-1/rollback, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON y diff-check.
- [x] Revisión independiente sin hallazgos críticos/altos/medios; mantener `AGRO-ID-003` `En curso`.
- [x] Publicar sólo con autor local `JuaniRMariani <juanirmariani@gmail.com>`, verificar `git show --format=fuller -1` y no desplegar (`79a0b09`).

### Baseline

- Git `main`/`origin/main` en `6ac8d74`; worktree limpio salvo la lección solicitada para fijar identidad Git personal.
- Backend: restore/build/format/EF PASS; MTP raíz `240/240`; fitness `96/96`.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest `79/79`; Playwright `6/6`.
- Validadores: FND `45/45`; SEC `41/41`; SEC-002 `20/20` operaciones; SCA y secrets sin hallazgos.

### Review final

- Resultado técnico: `PASS` local del sub-slice; `AGRO-ID-003` permanece `En curso`.
- Backend: restore locked, build Release 0 warnings/errores, suite raíz MTP `256/256`, fitness `101/101`, format y EF Identity/Territory `PASS`.
- Seguridad/DB: target ausente/ajeno/removido/self neutral, last-owner concurrente, replay/mismatch/in-flight/commit incierto y rollback journal/outbox `PASS`; FND `45/45`, SEC `42/42`, SEC-002 `22/22`.
- Frontend: pnpm frozen/format/lint/typecheck/build `PASS`, Vitest `95/95`; Playwright PostgreSQL real `6/6` desktop/mobile con journey A→B→remoción, Axe, teclado y 390 px.
- Supply chain: advisories transitivos detectados durante el gate corregidos mediante pins compatibles `SSH.NET 2026.0.0` y `nanoid 3.3.18`; NuGet/pnpm audit final sin vulnerabilidades conocidas.
- Sin deploy. Self-remove/transfer/democión, roles no-owner, scopes campo, email/delivery y superadmin siguen fuera.

## Iteración 29 — AGRO-GIS-002 campo borrador no espacial (2026-08-18)

Estado inicial: `Ready` únicamente para el sub-slice estrecho `CreateField draft + lista/ficha accesible`. `AGRO-GIS-002` permanece `En curso`; geometría, área, mapa, tiles, establecimiento/parcela/lote/potrero, catálogo y edición quedan fuera hasta cerrar sus dependencias.

### DoR, alcance y decisiones

- [x] Crear el bounded context productivo `Productive Core`; posee `ManagementUnit`. Territory no persiste ni consulta campos en este slice.
- [x] Fijar `ManagementUnit` de tipo server-side `field`, estado inicial `draft` y representación espacial `not_configured`; el request no acepta tipo, estado, actor, tenant, rol, coordenadas ni área.
- [x] Fijar `displayName` con trim Unicode `White_Space`+`U+FEFF`, NFC, 2..120 escalares, sin controles ni surrogates aislados; nombres duplicados se permiten dentro y entre organizaciones y se distinguen por UUID corto.
- [x] Autorizar sólo owner activo. Actor, sesión y organización se derivan/revalidan en servidor antes de consultar idempotencia o recurso.
- [x] Congelar rutas `POST/GET /api/organizations/{organizationId}/fields` y `GET /api/organizations/{organizationId}/fields/{fieldId}` con cookie, CSRF en POST, `Idempotency-Key` y errores Problem neutrales.
- [x] Crear `ManagementUnitCreated` tenant-scoped sin nombre, coordenadas, key/digest ni PII; journal local y outbox quedan atómicos con negocio e idempotencia.

Outcome observable: un owner crea `Campo Norte`, recarga, lo ve en una lista y abre su ficha `Sin geometría`. Otra organización puede usar el mismo nombre pero nunca ve ni infiere el recurso ajeno. Un co-owner removido pierde acceso inmediatamente.

### Aceptación verificable

- [x] POST válido responde 201 y confirma exactamente una unidad `field/draft/not_configured`; GET list/detail conserva orden determinista y UUID corto sólo en UI.
- [x] Mismo key+fingerprint reproduce el mismo recurso; payload diferente da conflicto; concurrencia, respuesta perdida/commit incierto y retry no duplican unidad, journal ni outbox.
- [x] El límite local de 100 campos por organización se aplica dentro de la transacción `SERIALIZABLE`: replay precede al conteo, dos altas concurrentes desde 99 dejan exactamente 100 y el exceso responde conflicto terminal sin ledger ni efectos auxiliares.
- [x] Org A/B, actor ajeno, target ausente/ajeno, sin contexto, sesión o membership revocada fallan neutralmente antes de lookup/replay; no existe oracle cross-tenant.
- [x] PostgreSQL usa schema/owner/principal propios, `FORCE RLS`, actor+tenant transaction-local, grants mínimos y pruebas de pool, rollback, error, cancelación y job sin contexto.
- [x] Fallo inyectado de ledger, journal u outbox revierte todas las superficies; telemetría allow-listed no contiene UUID, nombre, idempotency key, digest ni payload.
- [x] Migración expand-compatible demuestra clean, N/N-1, app rollback/roll-forward y pending model; `Down` destructivo sólo en base efímera.
- [x] OpenAPI, module/event/runtime/consumer maps y SEC-002 registran las tres operaciones y fallan ante drift.
- [x] UI cubre empty/loading/submitting/offline/validation/conflict/reconciliation/unavailable/success, ficha `Sin geometría`, foco/teclado/Axe y 390 px sin UUID completo.
- [x] Playwright demuestra create→reload→detail y aislamiento A/B en desktop y mobile, sin PostGIS, tiles ni egress.

### Ownership y gates

- [x] Principal: plan, contrato compartido, composición, mapas/evidencia, integración, gates y Git.
- [x] Backend: nuevo módulo Productive Core, dominio/aplicación/API/telemetría y pruebas no-DB bajo ownership exclusivo.
- [x] Database/Security: DbContext, migración, roles/RLS/primitivas y pruebas PostgreSQL bajo ownership exclusivo.
- [x] Frontend/QA: tipos/cliente/UI, Vitest y Playwright bajo ownership exclusivo.
- [x] Ejecutar restore locked, build Release, MTP raíz/dirigidos, format, EF pending/N-N-1/rollback, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON y diff-check.
- [x] Revisión independiente sin hallazgos críticos/altos/medios; mantener `AGRO-GIS-002` `En curso`.
- [x] Commit/push con `JuaniRMariani <juanirmariani@gmail.com>`; author y committer verificados, sin deploy (`4d4fe70`).

### Baseline

- Git `main`/`origin/main` en `5f32e15`; worktree limpio.
- Backend: restore/build/format/EF PASS; MTP raíz `256/256`; fitness `101/101`.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest `95/95`; Playwright `6/6`.
- Validadores: FND `45/45`; SEC `42/42`; SEC-002 `22/22`; SCA y secrets sin hallazgos.

### Review final

- PASS integrado-local: restore locked; build Release 9 proyectos `0/0`; MTP raíz `308/308`; Productive Core/PostgreSQL `30/30`; Architecture Fitness `121/121`; Vitest `130/130`; Playwright oficial `6/6`; FND `45/45`; SEC `53/53`; SEC-002 `25/25`; EF `3/3`, format, SCA, secretos, UTF-8/JSON y diff-check verdes.
- La revisión independiente cerró cinco hallazgos medios antes de publicar: errores de apertura/commit read tipados como 503, seguridad OpenAPI AND, persistencia idempotente ante respuesta ambigua, capacidad atómica de 100 sin truncamiento y canonicalización Unicode idéntica. Verificó 0 Critical, 0 High y 0 Medium restantes.
- `AGRO-GIS-002` permanece `En curso`: geometría, área, mapas/tiles, catálogo, edición, delivery y gates de ambiente compartido continúan fuera de este sub-slice.
- Publicación funcional: `4d4fe70` (`feat(productive-core): create non-spatial field drafts`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`.

## Iteración 30 — AGRO-FND-003 renombrar campo borrador (2026-08-18)

Estado inicial: micro-DoR cerrado con defaults técnicos reversibles; transición `Propuesto → Ready → En curso` únicamente para `RenameFieldDraft`. `AGRO-FND-003` permanece `En curso` y no absorbe backfills masivos, contract migrations, geometría, catálogo, archivo/borrado ni edición de otros campos.

### Plan y decisiones congeladas

- [x] Elegir un consumidor vertical real: renombrar sólo `ManagementUnit field/draft/not_configured` ya autorizado; owner activo, sin step-up adicional.
- [x] Congelar `PATCH /api/organizations/{organizationId}/fields/{fieldId}` con body cerrado `{displayName}`, cookie, CSRF, `If-Match` fuerte e `Idempotency-Key`; respuesta 200 flat + `isReplay` y ETag nuevo.
- [x] Ordenar la decisión como authz vigente → replay ligado → recurso/If-Match → mutación. Un replay válido conserva resultado; versión stale nueva responde 412 neutral y nunca aplica last-write-wins.
- [x] Canonicalizar el nombre con la misma regla CreateField: Unicode `White_Space`+`U+FEFF`, NFC, 2..120 escalares, sin controles ni surrogates aislados; duplicados siguen permitidos.
- [x] Rotar `Version` UUID y aumentar una revisión monotónica por rename. Journal/outbox no guardan nombre, key, digest, actor ni payload; `ManagementUnitDisplayNameChanged` publica sólo IDs internos, revisión y fecha.
- [x] Mantener compatibilidad expand N/N-1: revisión con default 1 y tablas/índices aditivos; rollback de aplicación deshabilita PATCH sin revertir el nombre confirmado y ambientes compartidos usan roll-forward.

### Aceptación verificable

- [x] Un owner cambia sólo el nombre, recibe nuevo ETag y list/detail convergen; mismo nombre canónico no crea versión, ledger, journal ni outbox.
- [x] Dos editores con el mismo ETag dejan un único nombre: uno confirma y el otro recibe 412 sin sobrescribir ni filtrar datos.
- [x] Mismo key/fingerprint/versión reproduce el resultado; key con nombre o `If-Match` distinto da 409; commit incierto reconcilia o falla cerrado sin repetir el rename.
- [x] Org B, owner removido, sesión revocada, recurso ausente/ajeno y contexto faltante fallan 404 neutral antes de lookup/replay.
- [x] Field + ledger/aliases + journal + outbox confirman atómicamente; fault injection por cada sink demuestra rollback total y ETag anterior vigente.
- [x] PostgreSQL real prueba `FORCE RLS`, grants mínimos, A/B/sin contexto/pool/job, concurrencia EF/Serializable, cancelación y migración clean/N/N-1/rollback/roll-forward.
- [x] UI ofrece “Editar nombre” en ficha, conserva draft+key ante offline/429/in-progress/503/reauth y ofrece “Recargar y revisar” ante 412; foco, teclado, Axe, 390 px y UUID corto.
- [x] OpenAPI, schema/event/runtime/consumer maps, SEC-002 y Architecture Fitness fallan ante drift de PATCH, ETag, evento, boundary o test negativo.

### Ownership y gates

- [x] Backend: dominio/aplicación/API/telemetría y tests no-DB, sin editar migración, contratos, Program, web ni evidencia.
- [x] Database/Security: DbContext, migración, adapter PostgreSQL/RLS/grants y pruebas DB, sin editar API/web/docs.
- [x] Frontend/QA: cliente/UI/Vitest/Playwright y estados accesibles, sin editar backend/contratos/docs.
- [x] Principal: contrato compartido, composición/config, eventos/maps/evidencia, integración, gates y Git.
- [x] Ejecutar restore locked, build Release, MTP raíz/dirigidos, format, EF pending/N-N-1/rollback, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON y diff-check.
- [x] Revisión independiente con 0 Critical/High/Medium; mantener `AGRO-FND-003` `En curso`.
- [x] Commit/push con `JuaniRMariani <juanirmariani@gmail.com>`; verificar author/committer y no desplegar.

### Review final

- PASS integrado-local: restore locked; build Release 9 proyectos `0/0`; MTP raíz `348/348`; Productive Core/PostgreSQL `56/56`; no-DB/API `37/37`; Architecture Fitness `135/135`; Vitest `153/153`; Playwright oficial `6/6`; FND `45/45`; SEC `56/56`; SEC-002 `26/26`; EF `3/3`, format, SCA, secretos, UTF-8/JSON, parser y diff-check verdes.
- La revisión independiente cerró dos Medium antes de publicar: alias split en recovery ahora falla 503 `idempotency.reconciliation_required`, y el rol app sólo conserva SELECT/INSERT sobre rename ledgers con UPDATE real denegado `42501`. Resultado final: 0 Critical, 0 High y 0 Medium pendientes.
- `AGRO-FND-003` permanece `En curso`; backfills/contract migrations/restore general y otros agregados siguen fuera. No hubo deploy.
- Publicación funcional: `2ace1f5` (`feat(productive-core): rename non-spatial field drafts`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`.

## Iteración 31 — AGRO-FE-001 OwnerWorkspaceShellV1 (2026-08-18)

Estado inicial: auditoría Product/QA, Security/Data y Architecture convergió en un único sub-slice Ready. Transición `Propuesto → Ready → En curso` sólo para un shell owner sobre contratos existentes; `AGRO-FE-001` permanece `En curso`.

### Plan y decisiones congeladas

- [x] Reutilizar sesión y memberships `owner/active`; no crear roles, endpoints, tablas, eventos, preferencias persistentes ni telemetría nueva.
- [x] Modelar 0 organizaciones como onboarding; 1 como selección automática; N como selector explícito sin consultar datos tenant hasta elegir.
- [x] Usar `?org=ABCDEF&view=fields|team|territory|account`. El prefijo corto se resuelve sólo entre memberships activas de la sesión; cero o múltiples coincidencias fallan cerradas. El backend sigue recibiendo el UUID completo.
- [x] Consultar campos/equipo sólo para la organización activa. Al cambiar, abortar requests anteriores, limpiar ficha/estado del tenant previo y mover foco al heading del workspace.
- [x] Preservar cualquier intento idempotente ambiguo dentro de su organización. Bloquear cambio durante submit/in-progress/reconciliation y confirmar antes de descartar un borrador editable.
- [x] Mantener español `es-AR`, UUID visible corto, sin datos sensibles en `localStorage`, sin offline/PWA falso y sin copiar el dominio/autorización al cliente.
- [x] Fijar gate local reproducible en Chromium desktop y Pixel 7; Firefox/WebKit y certificación manual completa quedan en el padre.

### Aceptación verificable

- [x] Owner con dos organizaciones selecciona A y ve únicamente campos/co-owners de A; B con nombres duplicados no aparece ni recibe requests hasta cambiar contexto.
- [x] Cambio A→B actualiza URL, heading y vista; aborta respuesta tardía de A y no reintroduce estado anterior.
- [x] Reload y back/forward restauran un contexto válido; prefijo inexistente, colisionado o membership removida vuelven al selector sin consultar tenant.
- [x] 0 organizaciones conserva onboarding; 1 se selecciona de forma determinista; sesión revocada limpia inmediatamente datos y contexto visibles.
- [x] Un draft no enviado exige confirmación accesible al cambiar; una mutación pending/in-progress/reconciliation impide cambiar y conserva key/contexto.
- [x] Navegación `fields|team|territory|account` usa landmarks, skip-link, `aria-current`, foco visible y anuncios acotados; 390 px no tiene overflow horizontal.
- [x] Ningún UUID completo se renderiza en selector, URL, tarjetas, modales o mensajes; el locator corto jamás concede autoridad.
- [x] Axe no reporta violaciones critical/serious y shell/estados siguen utilizables ante loading, offline, 404, 429 y 503.

### Ownership y gates

- [x] Frontend: shell, resolver/contexto URL, navegación, estilos e integración mínima con features existentes.
- [x] QA: unit/component de 0/1/N, colisión/remoción, URL/back-forward, abort stale, draft/pending, foco/UUID; Playwright desktop/móvil/Axe/390.
- [x] Principal: micro-DoR/backlog/evidencia, integración, gates, revisión y Git; sin cambios backend/schema/OpenAPI.
- [x] Ejecutar pnpm frozen/format/lint/typecheck/Vitest/build, Playwright oficial, restore/build/MTP raíz, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON, parser y diff-check.
- [x] Revisión independiente con 0 Critical/High/Medium; mantener `AGRO-FE-001` `En curso`.
- [x] Commit/push con `JuaniRMariani <juanirmariani@gmail.com>`; verificar author/committer y no desplegar.

### Review final

- Restore/build Release `PASS` (0 warnings/errores); suite raíz MTP `348/348`; Architecture Fitness `135/135` y SEC-002 `26/26` operaciones sin cambios de runtime.
- Frontend frozen/format/lint/typecheck/build `PASS`; Vitest `179/179`; Playwright oficial `8/8` desktop+móvil con Axe/390 y cleanup hermético.
- FND `45/45`, SEC `56/56`, NuGet/pnpm audit, UTF-8/JSON/parser/secrets/diff `PASS`.
- Revisión independiente final `PASS`: 0 Critical, 0 High, 0 Medium. Cinco Medium encontrados durante revisión fueron corregidos y cubiertos antes de publicación.
- Publicación funcional: `0a170e0` (`feat(frontend): add owner workspace shell`) en `origin/main`; author y committer verificados como `JuaniRMariani <juanirmariani@gmail.com>`.
- `OwnerWorkspaceShellV1` queda aprobado integrado-local. `AGRO-FE-001` permanece `En curso`; roles no-owner, preferencias, matriz completa de navegadores y certificación WCAG manual siguen fuera. No hubo deploy.
