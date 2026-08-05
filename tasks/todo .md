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
