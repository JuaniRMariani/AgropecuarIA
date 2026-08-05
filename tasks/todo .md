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
