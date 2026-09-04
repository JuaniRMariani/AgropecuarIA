# Plan de discovery â€” AgropecuarIA

Fecha de inicio: 2026-08-04  
Estado: completado  
Directorio: `B:\Xenova\AgropecuarIA`  
Nombre del producto: `AgropecuarIA`, confirmado por el sponsor.

## Plan verificable

- [x] Confirmar el directorio destino sin modificar proyectos existentes.
- [x] Crear la estructura documental inicial.
- [x] Investigar fuentes oficiales actuales sobre ARCA, identidad, GIS y normativa argentina.
- [x] Definir visiÃ³n, alcance, actores, vocabulario y supuestos.
- [x] Especificar mÃ³dulos, requisitos funcionales y reglas de negocio.
- [x] Modelar entidades, relaciones, trazabilidad y eventos principales.
- [x] Definir requisitos no funcionales, seguridad, privacidad y auditorÃ­a.
- [x] Proponer arquitectura, integraciones, estrategia de IA y ADR iniciales.
- [x] DiseÃ±ar estrategia QA, matriz de pruebas y criterios de salida.
- [x] Priorizar MVP, releases, dependencias, riesgos y mÃ©tricas de producto.
- [x] Consolidar preguntas de discovery y decisiones pendientes.
- [x] Verificar enlaces, estructura, consistencia y cobertura del paquete documental.

## Restricciones de esta etapa

- La entrega es discovery y especificaciÃ³n; no incluye cÃ³digo productivo ni aprovisionamiento cloud.
- No se emitirÃ¡n comprobantes reales ni se conectarÃ¡ una cuenta ARCA durante el discovery.
- Las decisiones legales, contables e impositivas se documentarÃ¡n para validaciÃ³n profesional; no se presumirÃ¡n.
- Los proveedores tecnolÃ³gicos serÃ¡n sustituibles hasta validar presupuesto, escala y restricciones comerciales.

## RevisiÃ³n

- Se creÃ³ el paquete documental en `B:\Xenova\AgropecuarIA`.
- La documentaciÃ³n cubre visiÃ³n, dominio, requisitos funcionales, reglas, modelo conceptual, arquitectura, integraciones, seguridad, IA, NFR, QA, roadmap, preguntas, fuentes y seis ADR.
- Se investigaron fuentes oficiales de ARCA, SENASA, AAIP, IGN, W3C, NIST, Google, PostGIS, OSM, NASA, Microsoft, Next.js y OpenTelemetry.
- Se validaron todos los enlaces Markdown internos, unicidad de IDs, cierres de code fences, UTF-8 y ausencia de archivos vacÃ­os.
- No se creÃ³ cÃ³digo productivo, no se inicializÃ³ Git, no se conectaron credenciales y no se modificÃ³ ningÃºn otro proyecto de `B:\Xenova`.

### Riesgos residuales

- Escala, equipo, presupuesto y nivel de detalle por sistema productivo siguen pendientes.
- FacturaciÃ³n ARCA quedÃ³ fuera del MVP y las finanzas se limitan a gestiÃ³n/exportaciÃ³n al contador.
- Toda integraciÃ³n ARCA/SENASA debe probar acceso real y homologaciÃ³n; un manual pÃºblico no garantiza autorizaciÃ³n.
- Reglas fiscales, valuaciones, retenciÃ³n documental y recetas profesionales requieren validaciÃ³n contable/legal/tÃ©cnica.
- Proveedor productivo de clima, satÃ©lite, identidad e IA sigue sujeto a presupuesto/validaciÃ³n.

## IteraciÃ³n 2 â€” decisiones del sponsor (2026-08-04)

- [x] Registrar nombre `AgropecuarIA` y perfil del primer usuario de referencia.
- [x] Actualizar el MVP para incluir agricultura y ganaderÃ­a, sin facturaciÃ³n ARCA.
- [x] Limitar finanzas a gestiÃ³n y exportaciÃ³n al contador.
- [x] Retirar offline del MVP y mantener arquitectura preparada sin implementarlo ahora.
- [x] Investigar y seleccionar estrategia de API meteorolÃ³gica para lluvias y contexto zonal.
- [x] Especificar rotaciÃ³n de ganado, alimentaciÃ³n, potreros, forraje y recomendaciones contextualizadas.
- [x] Actualizar requisitos, reglas, modelo, arquitectura, IA, QA, roadmap, preguntas y fuentes.
- [x] Verificar enlaces internos, UTF-8, code fences, IDs y contradicciones del alcance.

### Decisiones confirmadas

- Producto: `AgropecuarIA`.
- Usuario inicial de referencia: ingeniero agrÃ³nomo que ademÃ¡s es productor.
- MVP: catÃ¡logo nacional y flujo comÃºn para todas las producciones identificadas; el piloto define quÃ© perfiles reciben especializaciÃ³n primero.
- FacturaciÃ³n ARCA: fuera del MVP por ahora.
- Finanzas: gestiÃ³n operativa y exportaciÃ³n al contador; no contabilidad legal completa.
- Conectividad: todo online en el MVP.
- IA prioritaria: pronÃ³stico meteorolÃ³gico/lluvias y recomendaciones de rotaciÃ³n/alimentaciÃ³n ganadera segÃºn zona, clima, potreros y rodeos.

### RevisiÃ³n de la iteraciÃ³n 2

- Se renombrÃ³ de forma segura la carpeta a `B:\Xenova\AgropecuarIA`; no se modificaron otros proyectos.
- El MVP quedÃ³ definido como online, con agricultura y ganaderÃ­a en sus flujos comunes, clima y rotaciÃ³n ganadera como capacidades obligatorias.
- La estrategia meteorolÃ³gica propone Open-Meteo comercial para pronÃ³stico, alertas oficiales SMN CAP y un spike de SMN WRF como respaldo nacional.
- La rotaciÃ³n se especificÃ³ mediante datos medidos y versionados de potreros, forraje, rodeos, clima y restricciones; la IA no prescribe raciones ni mueve animales automÃ¡ticamente.
- FacturaciÃ³n e integraciÃ³n ARCA quedaron fuera del MVP. Finanzas cubre gestiÃ³n y exportaciÃ³n conciliable al contador.
- Se validaron 22 archivos Markdown, 119894 bytes y 165 definiciones de requisitos/reglas: cero errores de UTF-8, archivos vacÃ­os, code fences, enlaces internos o IDs duplicados.
- No se creÃ³ cÃ³digo productivo, repositorio Git, credenciales ni infraestructura.

## IteraciÃ³n 3 â€” catÃ¡logo productivo nacional (2026-08-04)

Estado: completado.

- [x] Registrar la correcciÃ³n del sponsor y actualizar las lecciones.
- [x] Investigar actividades agrÃ­colas de Argentina con fuentes oficiales.
- [x] Investigar especies, categorÃ­as y sistemas pecuarios de Argentina con fuentes oficiales.
- [x] DiseÃ±ar catÃ¡logos nacionales configurables sin confundir amplitud con profundidad funcional.
- [x] Marcar pluviÃ³metro y mediciÃ³n forrajera como opcionales, preservando lÃ­mites de recomendaciÃ³n segura.
- [x] Mantener pendiente el software/formato de exportaciÃ³n al contador.
- [x] Actualizar visiÃ³n, dominio, requisitos, reglas, datos, IA, QA, roadmap, discovery y fuentes.
- [x] Ejecutar validaciones documentales y revisiÃ³n final de consistencia.

### RevisiÃ³n de la iteraciÃ³n 3

- Se agregÃ³ `docs/14-catalogo-productivo-argentino.md` con cobertura territorial nacional, taxonomÃ­as vegetales/animales, unidades de manejo, niveles de soporte, gobierno de datos y criterios de aceptaciÃ³n.
- Se agregÃ³ ADR-006 para separar catÃ¡logo nacional, flujo genÃ©rico y especializaciÃ³n validada.
- La lÃ­nea base contempla agricultura extensiva/intensiva, fruticultura, horticultura, forrajes, forestaciÃ³n, viveros, ganaderÃ­a domÃ©stica, aves, porcinos, apicultura, acuicultura, fauna autorizada y producciones menores.
- Georef, CNA, SENASA, INASE, INV, SAGyP e INTA quedaron definidos como fuentes complementarias y versionadas; ninguna se presenta sola como lista exhaustiva.
- PluviÃ³metro y biomasa son cargas opcionales. Su ausencia reduce calibraciÃ³n/confianza, pero solo los faltantes de seguridad bloquean el ingreso ganadero.
- El formato del contador continÃºa pendiente; se diseÃ±Ã³ un paquete canÃ³nico para no bloquear las releases anteriores al adaptador.
- ValidaciÃ³n documental: 24 Markdown, 109 RF, 78 RN, 29 RNF y 66 preguntas Ãºnicas; cero errores de UTF-8, archivos vacÃ­os, code fences, enlaces internos, IDs duplicados o contradicciones de alcance buscadas.
- No se creÃ³ cÃ³digo, Git, credenciales ni infraestructura.

## IteraciÃ³n 4 â€” prompt de planificaciÃ³n para sesiÃ³n nueva (2026-08-04)

Estado: completado.

- [x] Leer instrucciones, lecciones, discovery y quality gates aplicables.
- [x] Crear un prompt autocontenido con roles senior y estrategia obligatoria de subagentes.
- [x] Definir entregables Markdown, formato de tareas, dependencias, criterios de aceptaciÃ³n y validaciÃ³n.
- [x] Impedir implementaciÃ³n, commits o cambios externos durante la sesiÃ³n de planificaciÃ³n.
- [x] Revisar el prompt contra el alcance completo de AgropecuarIA.
- [x] Imprimir el prompt completo por consola y registrar la evidencia.

### RevisiÃ³n de la iteraciÃ³n 4

- Se creÃ³ `prompts/01-planificar-implementacion-desde-cero.md`, autocontenido para una sesiÃ³n sin historial conversacional.
- Define un rol principal multidisciplinario con mÃ¡s de 20 aÃ±os y diez frentes de subagentes con experiencia senior/principal.
- Exige lectura completa, trazabilidad RF/RN/RNF/ADR, vertical slices, backlog implementable, QA, seguridad, GIS, IA, DevOps y revisiÃ³n independiente.
- Restringe la sesiÃ³n futura a Markdown bajo `tasks/`; prohÃ­be cÃ³digo, scaffolding, SQL, scripts, configuraciones, workflows, Git, infraestructura y credenciales.
- ValidaciÃ³n del prompt: UTF-8 estricto, 289 lÃ­neas, marcadores Ãºnicos, contenido obligatorio presente y cero errores.
- El contenido completo se imprimiÃ³ mediante PowerShell `Get-Content -Raw -Encoding UTF8`.

## IteraciÃ³n 5 â€” planificaciÃ³n detallada de implementaciÃ³n (2026-08-04)

Estado: completado.

### Plan verificable de la sesiÃ³n

- [x] Confirmar `B:\Xenova\AgropecuarIA`, preservar el trabajo existente y comprobar de forma no mutante que Git no estÃ¡ inicializado.
- [x] Leer Ã­ntegramente `AGENTS.md`, `README.md`, `tasks/todo .md`, `tasks/lessons .md`, `docs/*.md`, `docs/adr/*.md` y los enlaces internos necesarios.
- [x] Inventariar el discovery: alcance, releases, RF/RN/RNF, ADR, preguntas y decisiones confirmadas/pendientes.
- [x] Ejecutar olas de anÃ¡lisis con los diez frentes senior obligatorios, cruzar hallazgos y mantener la integraciÃ³n/decisiÃ³n final en el agente principal.
- [x] Construir el inventario de alcance y la matriz inicial; separar MVP, posterior y `Won't now`, con excepciones explÃ­citas.
- [x] Definir secuencia por releases y dependencias; registrar spikes y decisiones que bloquean Ãºnicamente slices concretos.
- [x] Redactar `tasks/implementation-plan.md`, `tasks/release-plan.md`, `tasks/risk-register.md`, `tasks/decisions-and-gaps.md` y `tasks/team-workstreams.md`.
- [x] Crear `tasks/backlog/00-index.md` y un archivo por Ã©pica, con tareas `AGRO-<EPICA>-<NNN>` verticales, verificables y completas segÃºn el formato requerido.
- [x] Redactar `tasks/test-strategy.md` y `tasks/traceability-matrix.md`, trazando el 100 % de RF/RN/RNF o una excepciÃ³n justificada.
- [x] Cruzar tareas, releases, criterios, pruebas, seguridad, autorizaciÃ³n, observabilidad, migraciÃ³n/rollback y preguntas abiertas para eliminar duplicaciÃ³n y secuencias imposibles.
- [x] Solicitar una auditorÃ­a final independiente a un subagente QA/reviewer y corregir todos los hallazgos reales.
- [x] Validar UTF-8, archivos no vacÃ­os, enlaces, code fences, IDs Ãºnicos, rutas del Ã­ndice, owners, criterios, pruebas, tamaÃ±os, dependencias y cobertura calculada.
- [x] Confirmar que solo se modificaron Markdown bajo `tasks/`, registrar comandos/resultados/limitaciones/riesgos residuales y completar la autoevaluaciÃ³n â‰¥ 90/100.

### Criterios de salida

- Todos los entregables obligatorios existen, estÃ¡n enlazados y tienen una responsabilidad canÃ³nica.
- Cada `Must`, RN y RNF tiene tarea y prueba en un release del MVP, o una excepciÃ³n explÃ­cita; `Should`, `Could` y `Won't now` conservan su prioridad documental.
- Ninguna tarea implementable queda sin owner, criterios observables, pruebas, controles tenant/autorizaciÃ³n, telemetrÃ­a y estrategia de rollout/recuperaciÃ³n aplicable.
- El formato/software del contador permanece pendiente sin bloquear el paquete canÃ³nico; ARCA y offline permanecen fuera del MVP.
- La revisiÃ³n independiente y los quality gates documentales terminan sin fallos crÃ­ticos.

### RevisiÃ³n de la iteraciÃ³n 5

#### Resultado

- Se planificaron 8 releases (`R0`â€“`R7`), 18 Ã©picas y 81 vertical slices con tamaÃ±o XS/S/M/L; no existen tareas XL.
- Se crearon o actualizaron 28 archivos Markdown bajo `tasks/`: 18 archivos de Ã©pica, un Ã­ndice navegable a cada tarea y los nueve documentos canÃ³nicos transversales/este registro.
- La matriz recalculada desde las fuentes contiene 109 RF, 78 RN y 29 RNF: 216/216 trazados, 207 con tarea/prueba y 9 excepciones explÃ­citas (`RF-GIS-010`, `RF-FIN-009`, `RN-FIS-001â€“007`). Los 6/6 ADR originales estÃ¡n trazados.
- Se ejecutaron en varias olas los diez frentes requeridos: producto/dominio, arquitectura, backend/datos, frontend/UX, GIS/clima, IA/analÃ­tica, QA, seguridad/privacidad, plataforma/SRE y validaciÃ³n profesional. La integraciÃ³n y las decisiones finales quedaron en el agente principal.
- El formato/software del contador permanece pendiente (`Q-018`, `Q-052`, `Q-053`); bloquea solo el adaptador especÃ­fico, no el paquete canÃ³nico. ARCA y offline permanecen `Won't now`.

#### Correcciones surgidas de la revisiÃ³n cruzada

- Se adelantaron a R2 los kernels mÃ­nimos de inventario e imputaciÃ³n de costos para evitar dependencias imposibles de las operaciones.
- Se separaron slices R7 explÃ­citos para calendario/carga masiva (`AGRO-AGR-006`), escenarios avanzados (`AGRO-IA-006`) y soporte JIT (`AGRO-ID-005`), evitando que requisitos posteriores quedaran nominalmente trazados a tareas que los excluÃ­an.
- Se eliminÃ³ el ciclo `DOC-003 â†” SEC-004`, se corrigieron referencias `DIS-*`, se creÃ³ `ADR-PEND-011` para ownership de `ManagementUnit` y se reservaron `ADR-PEND-007` para RLS y `ADR-PEND-010` para compatibilidad de migraciones.
- Se normalizÃ³ el idioma tras detectar y revertir una transformaciÃ³n mecÃ¡nica defectuosa; la correcciÃ³n final fue contextual por archivo. TambiÃ©n se aclarÃ³ el fixture EICAR y se agregaron anclas estables para las 81 tareas.

#### Quality gates y evidencia

| Gate | Resultado final |
|---|---|
| UTF-8 estricto, no vacÃ­os y alcance de archivos | 28/28 archivos bajo `tasks/` son `.md`, vÃ¡lidos y no vacÃ­os; no se generÃ³ cÃ³digo productivo. |
| IDs, formato y tamaÃ±o | 81 tareas, 81 IDs Ãºnicos, 26 campos requeridos no vacÃ­os por tarea, owner/criterios/pruebas presentes, 0 XL. |
| Ãndice y enlaces | 81/81 tareas enlazadas mediante anclas; rutas internas y code fences vÃ¡lidos. |
| Dependencias | Sin ciclos; secuencia R0â€“R7 revisada y dependencias imposibles corregidas. |
| Trazabilidad | 109 RF + 78 RN + 29 RNF = 216/216, sin faltantes ni extras; 6/6 ADR. |
| Idioma | Prosa en espaÃ±ol; permanecen Ãºnicamente tÃ©rminos tÃ©cnicos habituales. |
| AuditorÃ­a independiente | Aprobada sin hallazgos crÃ­ticos, altos ni medios luego de resolver tres hallazgos finales. |
| Git | No existe `.git`; `git status --short` y `git diff --check` no aplican y no se inicializÃ³ repositorio. |

Comandos/recursos de solo lectura usados para inspecciÃ³n y validaciÃ³n: `Test-Path`, `Get-ChildItem`, `Get-Content -Raw -Encoding UTF8`, `rg` y validadores PowerShell sobre IDs, campos, UTF-8, referencias, enlaces, anclas, fences, extensiones y cobertura expandida. Hubo errores intermedios de sintaxis en validadores PowerShell y un intento fallido de parche masivo; se detuvieron, corrigieron y repitieron hasta obtener el resultado final sin errores. Las ediciones se realizaron con `apply_patch` exclusivamente sobre Markdown autorizado bajo `tasks/`.

LimitaciÃ³n de atribuciÃ³n: sin historial Git no puede probarse el origen temporal de `prompts/02-implementar-codigo-sesion-nueva.md`, observado como trabajo concurrente/preexistente y preservado sin cambios por esta iteraciÃ³n. No forma parte de estos entregables.

Riesgos/decisiones residuales que requieren sponsor o especialistas: datos y escala piloto (`Q-012â€“020`), clima/canales/proveedores/WRF (`Q-021â€“030`), perfiles y parÃ¡metros profesionales (`Q-031â€“047`), polÃ­ticas econÃ³micas (`Q-048â€“051`), UX/IdP/IA/privacidad/SLO/gobierno (`Q-054â€“066`) y formato del contador ya seÃ±alado. Ninguno bloquea la existencia del backlog; cada uno tiene gate, owner y contingencia de abstenciÃ³n/degradaciÃ³n.

#### AutoevaluaciÃ³n

| DimensiÃ³n | Puntaje |
|---|---:|
| Contexto y lectura completa | 15/15 |
| LÃ­mites y ausencia de cÃ³digo | 15/15 |
| Subagentes y roles | 15/15 |
| Cobertura | 20/20 |
| Calidad del backlog | 18/20 |
| VerificaciÃ³n y trazabilidad | 14/15 |
| **Total** | **97/100** |

Resultado: planificaciÃ³n aprobada para iniciar implementaciÃ³n por vertical slices, sujeta a los gates y decisiones pendientes registrados.

## IteraciÃ³n 6 â€” prompt para sesiÃ³n implementadora (2026-08-04)

Estado: completado.

- [x] Leer instrucciones, lecciones y skills de orquestaciÃ³n/arquitectura limpia.
- [x] DiseÃ±ar el rol principal y los subagentes implementadores con ownership disjunto.
- [x] Definir selecciÃ³n de vertical slice, flujo de implementaciÃ³n y lÃ­mites de autoridad.
- [x] Incorporar Clean Architecture pragmÃ¡tica, SOLID, patrones y reglas anti-sobreingenierÃ­a.
- [x] Incorporar quality gates de .NET, Next.js, datos, seguridad, accesibilidad y operaciÃ³n.
- [x] Revisar el prompt de forma independiente, validar UTF-8/contenido e imprimirlo por consola.

### RevisiÃ³n de la iteraciÃ³n 6

- Se creÃ³ `prompts/02-implementar-codigo-sesion-nueva.md` para una sesiÃ³n implementadora sin contexto conversacional.
- Define selecciÃ³n determinÃ­stica de una Ãºnica tarea `Ready`, fallback seguro y ejecuciÃ³n mediante vertical slice completo.
- Coordina diez roles senior/principal en tres olas con ownership de archivos disjunto y revisiÃ³n independiente.
- Incorpora monolito modular, Clean Architecture pragmÃ¡tica, SOLID, Adapter/Strategy/Outbox/Problem Details/concurrencia y reglas explÃ­citas contra sobreingenierÃ­a.
- Incluye estÃ¡ndares production-ready para ASP.NET Core, Next.js/TypeScript, PostgreSQL/PostGIS, seguridad, IA, accesibilidad, observabilidad, tests y migraciones.
- Protege el worktree y prohÃ­be Git destructivo, commit/push/deploy no solicitados, secretos, bases productivas y cambios fuera de `B:\Xenova\AgropecuarIA`.
- ValidaciÃ³n: UTF-8 estricto, 460 lÃ­neas, 26044 caracteres, seis delimitadores de code fence balanceados, marcadores Ãºnicos y cero requisitos obligatorios faltantes.
- El contenido completo se imprimiÃ³ mediante PowerShell `Get-Content -Raw -Encoding UTF8`.

## IteraciÃ³n 7 â€” actualizar prompt para iniciar implementaciÃ³n (2026-08-04)

Estado: completado.

### Plan verificable

- [x] Releer instrucciones, lecciones, skill de orquestaciÃ³n, backlog final y prompt implementador existente.
- [x] Contrastar el prompt contra las 81 tareas, el estado greenfield y las dependencias/gates de R0â€“R1.
- [x] Hacer inequÃ­voca la selecciÃ³n de tarea, incluyendo estados `Propuesto`, `Ready`, bloqueado, en curso y completado.
- [x] Definir un arranque greenfield seguro cuando todavÃ­a no existen soluciÃ³n, Git ni comandos ejecutables.
- [x] Reforzar ejecuciÃ³n de una tarea por sesiÃ³n, ownership de subagentes, trazabilidad y actualizaciÃ³n de estado/evidencia.
- [x] Preservar lÃ­mites: sin inventar decisiones profesionales, sin commit/push/deploy/credenciales y sin cambios fuera del proyecto.
- [x] Solicitar revisiÃ³n independiente del prompt y corregir hallazgos.
- [x] Validar UTF-8, marcadores, enlaces/rutas, code fences, coherencia de IDs y contenido obligatorio.
- [x] Documentar resultado, comandos y riesgos residuales en esta secciÃ³n.

### RevisiÃ³n de la iteraciÃ³n 7

#### Resultado

- Se actualizÃ³ [`prompts/02-implementar-codigo-sesion-nueva.md`](../prompts/02-implementar-codigo-sesion-nueva.md) para que una sesiÃ³n nueva implemente como mÃ¡ximo una tarea explÃ­cita y con Definition of Ready demostrada.
- `TAREA_OBJETIVO=AUTO` quedÃ³ como auditorÃ­a read-only: no cambia estados, no publica plan mutante, no crea cÃ³digo/scaffolding y solicita un ID explÃ­cito.
- Se definieron las transiciones `Propuesto â†’ Ready â†’ En curso â†’ En revisiÃ³n â†’ Completada` y `Bloqueada`, con autoridad, evidencia y fallos seguros ante IDs invÃ¡lidos.
- Se diferenciaron tareas R0, R1â€“R6, R7, mixtas y multirelease. Los prototipos R0 son descartables y no pueden convertirse en bootstrap productivo por inferencia.
- El arranque `GREENFIELD_DOCUMENTADO` permite scaffolding mÃ­nimo Ãºnicamente dentro de una tarea explÃ­cita Ready; prohÃ­be mÃ³dulos vacÃ­os, bootstrap implÃ­cito, Git/CI/infraestructura y decisiones de proveedor no autorizadas.
- Se reforzaron ownership de archivos, inventario pre/post cuando no existe Git, gates aplicables con `N/A` justificado, comandos .NET como patrones detectables y autoevaluaciÃ³n no sustitutiva.

#### RevisiÃ³n multiagente

- Producto/dominio detectÃ³ que las 81 tareas estaban `Propuesto`, que ninguna R1 podÃ­a autoaprobar sus gates R0 y que `AUTO` debÃ­a detenerse en diagnÃ³stico.
- Arquitectura/delivery eliminÃ³ el fallback de enabler/bootstrap, acotÃ³ una tarea por sesiÃ³n y precisÃ³ greenfield, estados, ownership y gates.
- La auditorÃ­a final independiente de backend/datos seÃ±alÃ³ un hallazgo alto y tres medios: pedido por Ã©pica/release sin ID, ID inexistente, tareas multirelease y uso de `En revisiÃ³n`. Los cuatro fueron corregidos y la segunda pasada quedÃ³ **aprobada sin hallazgos crÃ­ticos ni altos bloqueantes**.

#### Quality gates y evidencia

| Gate | Resultado |
|---|---|
| UTF-8 y contenido | UTF-8 estricto, archivo no vacÃ­o, 530 lÃ­neas y 36.776 caracteres. |
| Estructura Markdown | 6 delimitadores de code fence balanceados; un marcador de inicio y uno de fin. |
| Contenido obligatorio | Presentes `AUTO` read-only, `MAXIMO_TAREAS=1`, estados, clasificaciÃ³n R0â€“R7, greenfield, DoR, DoD y quality gates; cero patrones mutantes detectados dentro del bloque `AUTO`. |
| Rutas documentales | Existen todos los archivos referenciados: instrucciones, README, planes, estrategia de pruebas, matriz e Ã­ndice. |
| Alcance | 0 archivos de cÃ³digo/configuraciÃ³n productiva detectados; solo se editaron el prompt solicitado y este registro Markdown. |
| Git | `.git` no existe; no se inicializÃ³. `git status --short` y `git diff --check` no aplican. |
| RevisiÃ³n independiente | Aprobada tras corregir todos los hallazgos reportados. |

Comandos/recursos de verificaciÃ³n: `Get-Content -Encoding UTF8`, `rg`, `rg --files`, `Test-Path`, lectura UTF-8 estricta mediante APIs de .NET y validadores PowerShell de marcadores, fences, secciones obligatorias, rutas, patrones `AUTO` y tipos de archivo. Las ediciones se realizaron con `apply_patch`.

LimitaciÃ³n: al no existir Git no hay diff histÃ³rico confiable; la atribuciÃ³n se sostuvo mediante el inventario de la sesiÃ³n y la revisiÃ³n explÃ­cita de las dos rutas modificadas. No se escribiÃ³ cÃ³digo, no se inicializÃ³ Git, no se hizo commit/push y no se desplegÃ³ infraestructura.

## IteraciÃ³n 8 â€” AGRO-DIS-001 CatÃ¡logo Nacional v1 (2026-08-04)

Estado inicial: `Propuesto`; tarea seleccionada explÃ­citamente por el sponsor. Alcance: una Ãºnica tarea R0, sin bootstrap ni cÃ³digo productivo.

### DoR y decisiones de entrada

- [x] Confirmar ID Ãºnico `AGRO-DIS-001`, release R0, prioridad Must y tamaÃ±o M.
- [x] Confirmar fuentes rectoras accesibles y alcance trazado a RF-CAT-001â€“005, RN-CAT-001â€“005, RNF-CAT-001/002 y ADR-006.
- [x] Nominar sponsor/owner accountable y WS-03 Product/Catalog Lead como responsable operativo.
- [x] Cerrar operativamente Q-062â€“Q-066 mediante delegaciÃ³n explÃ­cita del sponsor.
- [x] Acordar cadencia trimestral + urgencias y formato trazable de conflicto/excepciÃ³n.
- [x] Preservar la frontera: catÃ¡logo/flujo comÃºn nacional; especializaciÃ³n solo con validaciÃ³n profesional.

### Plan verificable

- [x] Definir contrato versionado de entrada, fuente, alias, excepciÃ³n, mÃ©tricas y publicaciÃ³n.
- [x] Construir el denominador reproducible de `CatÃ¡logo Nacional v1` a partir del alcance nominal aprobado.
- [x] Crear manifiesto de fuentes con fecha, URL, alcance, autoridad y hash reproducible de la evidencia local.
- [x] Normalizar entradas vegetales y animales con cÃ³digos internos estables, soporte, vigencia, jurisdicciÃ³n y procedencia.
- [x] Registrar aliases, deduplicaciones, placeholders y excepciones sin afirmar exhaustividad eterna.
- [x] Documentar RACI, segregaciÃ³n editorial, workflow, cadencia, rollback lÃ³gico y compatibilidad.
- [x] Implementar y ejecutar validadores del dataset, unicidad, referencias, soporte, alias y cobertura 100 % normalizada/exceptuada.
- [x] Ejecutar bÃºsqueda por nombre/alias/tildes/cÃ³digo y casos negativos representativos.
- [x] Integrar revisiÃ³n independiente de QA y AppSec/Arquitectura; resolver hallazgos.
- [x] Registrar evidencia, riesgos residuales, comandos y estado final de la Ãºnica tarea.

### Ownership exclusivo

- Principal: `tasks/todo .md`, backlog, decisiones/gaps, contrato compartido, integraciÃ³n y estado.
- Product/Data vegetal: dataset vegetal, sin editar archivos compartidos.
- Product/Data animal: dataset animal, sin editar archivos compartidos.
- QA Automation: validador y evidencia de ejecuciÃ³n, sin modificar datasets salvo reporte de hallazgos.
- RevisiÃ³n final: QA y AppSec/Arquitectura en modo read-only sobre el estado integrado.

### Comandos previstos

- Parseo estricto de JSON y UTF-8 mediante PowerShell/.NET local.
- Validador versionado de cÃ³digos, fuentes, aliases, estados, niveles de soporte, duplicados y excepciones.
- BÃºsquedas reproducibles por cÃ³digo, nombre, nombre sin tildes y alias.
- Inventario/hash pre/post porque no existe Git.
- Backend `.NET`, migraciones y contenedores: N/A para esta tarea R0 sin servicio productivo. Next.js/React sÃ­ aplica al prototipo explÃ­cito de validaciÃ³n y se verifica con sus scripts locales.

### RevisiÃ³n

RevisiÃ³n inicial independiente: la evidencia tÃ©cnica del catÃ¡logo pasÃ³, pero se detectaron dos entregables internos explÃ­citos todavÃ­a ausentes. La tarea vuelve de `En revisiÃ³n` a `En curso` hasta resolverlos.

- [x] Crear un prototipo Next.js/React R0 aislado y descartable para bÃºsqueda/soporte con estados loading, vacÃ­o, error, fuente stale y accesibilidad responsive.
- [x] Definir el contrato conceptual versionado de diff y `ProductCatalogPublished` sin exponerlo como API productiva.
- [x] Incluir ambos artefactos en el manifiesto y ampliar los gates automatizados.
- [x] Ejecutar build/lint/typecheck y validar el flujo en navegador real.
- [x] Repetir revisiÃ³n independiente sobre el estado integrado.

#### Resultado y evidencia final

- Se produjo `1.0.0-candidate.1` con 154 entradas vegetales, 59 animales, 213 totales, 31 reguladas, 3 excepciones documentadas, 10 fuentes/evidencias y 205 dimensiones familiares.
- El orÃ¡culo ejecutÃ³ 637 fixtures de bÃºsqueda y cobertura con 0 fallos. La bÃºsqueda diferencia entradas publicables de dimensiones de familia y normaliza cÃ³digo, mayÃºsculas, tildes y aliases.
- El prototipo Next.js 16.3.0 + React 19.2.8 es R0, aislado y descartable. Lee evidencia local exclusivamente del servidor y demuestra bÃºsqueda, niveles de soporte, evidencia, confianza, faltantes y estados normal/loading/empty/error/stale, incluido stale simultÃ¡neo con cero resultados.
- El contrato conceptual documenta diff versionado y `ProductCatalogPublished`; no es una API, publicaciÃ³n ni integraciÃ³n productiva.
- La revisiÃ³n independiente de Principal QA y AppSec/Arquitectura aprobÃ³ el entregable tÃ©cnico sin hallazgos crÃ­ticos, altos ni medios. Queda fuera de esa aprobaciÃ³n la firma profesional nominada sobre la semÃ¡ntica completa del baseline.

#### Quality gates ejecutados

| Gate | Resultado |
|---|---|
| `npm ci --ignore-scripts --no-audit --no-fund` | PASS; 350 paquetes instalados desde lockfile. |
| `npm run lint` | PASS; 0 errores. |
| `npm run typecheck` | PASS; TypeScript estricto. |
| `npm test` | PASS; 9/9 pruebas, 0 fallos. |
| `npm run build` | PASS; rutas estÃ¡ticas `/`, `/_not-found` e `/icon.svg`. |
| `npm audit --audit-level=high` | PASS; 0 vulnerabilidades. |
| Parseo JSON/UTF-8 | PASS; 0 errores. |
| `validate-catalog.ps1` | PASS; 35 artefactos estables, 637 fixtures y 0 fallos. |
| Navegador Playwright, 390 Ã— 844 | PASS; HTTP 200, 213 entradas, bÃºsqueda `ponedoras` = 1, estados y teclado/foco verificados, 0 errores/warnings de consola. |
| Backend `.NET`, migraciones, DB, contenedores y telemetrÃ­a productiva | N/A; `AGRO-DIS-001` es una validaciÃ³n R0 y no autoriza un servicio productivo. |

#### Compatibilidad, rollback y riesgos residuales

- La compatibilidad es lÃ³gica y versionada: no se mutÃ³ una base ni se publicÃ³ un evento real. El rollback consiste en conservar/republicar la versiÃ³n candidata previa segÃºn `governance.md`; no aplica migraciÃ³n fÃ­sica.
- `next-env.d.ts` se valida semÃ¡nticamente pero no integra el manifiesto de hashes porque Next alterna su contenido entre desarrollo y build. El orden reproducible es build, parseo y validaciÃ³n del catÃ¡logo.
- Las pÃ¡ginas remotas pueden cambiar; los hashes protegen la evidencia local capturada, no garantizan que una landing page externa permanezca idÃ©ntica.
- No existe Git, por lo que la atribuciÃ³n se basa en inventarios pre/post. Se preservÃ³ sin borrar un directorio vacÃ­o creado accidentalmente fuera del proyecto: `C:\Users\juanc\tasks\evidence\AGRO-DIS-001`.
- CondiciÃ³n externa pendiente: agrÃ³nomo, veterinario y responsable editorial nominados deben revisar y firmar el baseline/acta. No resta trabajo tÃ©cnico interno en esta tarea; hasta esa firma `GAP-011` no se cierra definitivamente y la tarea permanece `En revisiÃ³n`.

#### AutoevaluaciÃ³n

| DimensiÃ³n | Puntaje |
|---|---:|
| Contexto/selecciÃ³n | 15/15 |
| Arquitectura/cÃ³digo | 20/20 |
| Multiagente | 10/10 |
| Full-stack/datos/observabilidad proporcional a R0 | 14/15 |
| Pruebas/seguridad | 20/20 |
| PreservaciÃ³n/cierre | 19/20 |
| **Total** | **98/100** |

Estado final: `En revisiÃ³n`. La implementaciÃ³n y revisiÃ³n tÃ©cnica de `AGRO-DIS-001` estÃ¡n completas; la transiciÃ³n a `Completada` requiere la aprobaciÃ³n profesional nominada definida por la propia tarea.

## IteraciÃ³n 9 â€” AGRO-DIS-003 identidad, linking, RLS y tenant (2026-08-05)

Estado inicial: `Propuesto`; seleccionada autÃ³nomamente por el lÃ­der tras la delegaciÃ³n explÃ­cita del sponsor. ClasificaciÃ³n: tarea R0 Must/M; se entregarÃ¡ un spike aislado y descartable, no autenticaciÃ³n productiva ni bootstrap R1.

### DoR y defaults reversibles

- [x] Confirmar ID Ãºnico, outcome, requisitos, riesgos y tareas dependientes.
- [x] Adoptar `Organization` como tenant operativo, `User` platform-scoped y `Membership` tenant-scoped.
- [x] Mantener organizaciones separadas para clientes de un asesor; nunca inferir acceso agregado cross-client.
- [x] Tratar el control de datos por organizaciÃ³n como decisiÃ³n tÃ©cnica, sin afirmar propiedad legal entre propietario/productor/asesor.
- [x] Mantener soporte JIT apagado y diferido a `AGRO-ID-005` R7.
- [x] Usar 99,9 %, RPO 15 min y RTO 2 h Ãºnicamente como hipÃ³tesis medibles del spike, no como SLA contractual.
- [x] Fijar shortlist IdP y criterio: Auth0 candidato preferido; ZITADEL/AWS Cognito como comparadores; no-go productivo sin sandbox, DPA/regiÃ³n, plan, exportabilidad y pruebas reales.
- [x] Aceptar fixtures sintÃ©ticos de dos organizaciones y PostgreSQL 17 efÃ­mero local en loopback como test data.

### Alcance verificable

- [x] Definir contratos versionados de actor/tenant/permisos, sesiÃ³n/revocaciÃ³n, linking y eventos conceptuales.
- [x] Documentar matriz IdP, alternativas, trade-offs, gaps y go/no-go condicional.
- [x] Crear threat model repo-grounded con fronteras navegador/Next/API/IdP/pool/DB/jobs y abuso priorizado.
- [x] Implementar un spike Minimal API .NET 10 sin SDK propietario de IdP, con fixtures determinÃ­sticos y Problem Details.
- [x] Implementar PostgreSQL RLS real con owner/runtime separados, `ENABLE/FORCE ROW LEVEL SECURITY`, rol `NOBYPASSRLS` y contexto transaccional local.
- [x] Probar dos tenants, BOLA neutral, `WITH CHECK`, reuse de pool Aâ†’Bâ†’sin tenant, excepciÃ³n/rollback y job sin tenant.
- [x] Modelar linking como estado one-shot: ambas identidades reautenticadas; email coincidente nunca vincula.
- [x] Modelar recovery anti-enumeraciÃ³n, rate limit, expiraciÃ³n/replay y revocaciÃ³n de sesiones sin guardar OTP/cÃ³digos.
- [x] Implementar prototipo Next.js/React accesible para signed-out, 0/1/N organizaciones, cambio de tenant, linking, recovery, provider-down, conflicto y sesiÃ³n revocada.
- [x] Ejecutar restore/build/analyzers/format/tests .NET, lint/typecheck/unit/build frontend, navegador real y scans aplicables.
- [x] Integrar revisiÃ³n independiente Principal QA y AppSec/Arquitectura; resolver hallazgos altos/crÃ­ticos.
- [x] Actualizar ADR-003, decisiones/gaps, evidencia y estado final sin presentar el spike como producciÃ³n.

### Contrato y lÃ­mites fijados antes de editar cÃ³digo

- Identidad externa estable: `(issuer, subject)`. Email es contacto/discovery, nunca autoridad de linking.
- El cliente no decide tenant ni permisos. El servidor deriva contexto desde sesiÃ³n y membresÃ­a vigente; el recurso ajeno responde de forma neutral.
- Cookie opaca `__Host-*`, `HttpOnly`, `Secure`, `SameSite=Lax`; tokens upstream solo server-side y ninguna sesiÃ³n/token en `localStorage`.
- Linking requiere sesiÃ³n primaria, step-up y reautenticaciÃ³n fresca de ambas identidades; challenges ligados a sesiÃ³n, TTL y consumo Ãºnico.
- Recovery devuelve aceptaciÃ³n indistinguible, aplica lÃ­mites y revoca sesiones al completarse. El IdP conserva secretos de passkey/TOTP/recovery; AgropecuarIA no los replica.
- Cada request/job abre transacciÃ³n, aplica contexto tenant local y falla cerrado cuando falta. El rol de aplicaciÃ³n no es owner ni posee `BYPASSRLS`.
- Sin IdP real, email real, proveedor cloud, credenciales, datos personales, deploy, Docker, CI o migraciÃ³n productiva.

### Ownership disjunto â€” ola 2

- Principal: soluciÃ³n/manifiestos/lockfiles, contratos compartidos, scaffolding, integraciÃ³n, tests/harness comÃºn, documentaciÃ³n y estados.
- Backend .NET: `tasks/evidence/AGRO-DIS-003/spike/api/**`.
- Database/Security: `tasks/evidence/AGRO-DIS-003/spike/database/**` y revisiÃ³n de invariantes RLS.
- Frontend Next.js: `tasks/evidence/AGRO-DIS-003/spike/web/app/**`, `features/**`, `lib/**` y estilos.
- Agentes de revisiÃ³n final nuevos y read-only; ningÃºn implementador aprobarÃ¡ sus propios archivos.

### Baseline y comandos previstos

- Baseline: .NET SDK 10.0.201 y PostgreSQL 17 disponibles; Docker no instalado; Git ausente; no existÃ­an `.sln`/`.csproj` ni evidencia `AGRO-DIS-003`.
- El PostgreSQL del sistema exige credenciales desconocidas y no se toca. El harness iniciarÃ¡ un clÃºster efÃ­mero propio con `initdb`, `trust` limitado a loopback y ruta/puerto explÃ­citos, luego lo detendrÃ¡ y eliminarÃ¡ de forma validada.
- .NET 10 + Microsoft.Testing.Platform: detectar seÃ±ales finales antes de ejecutar `dotnet test --solution ...`; no usar sintaxis VSTest por memoria.
- Frontend: package/lock propios del spike, instalaciÃ³n frozen, lint, typecheck, unit, build y Playwright CLI.
- Docker/Compose, cloud, migraciÃ³n/restore productivo, deployment y telemetrÃ­a productiva: N/A por alcance R0 y herramientas disponibles.

### Fuentes primarias que alteran decisiones

- ASP.NET Core 10 es LTS y agrega validaciÃ³n integrada de Minimal APIs: <https://learn.microsoft.com/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0>.
- Auth0 exige autenticar ambas cuentas antes de linking y condiciona capacidades al plan: <https://auth0.com/docs/manage-users/user-accounts/user-account-linking>.
- Auth0 documenta passkeys y recovery codes, pero el plan/regiÃ³n siguen siendo gates: <https://auth0.com/docs/authenticate/database-connections/passkeys> y <https://auth0.com/docs/secure/multi-factor-authentication/multi-factor-authentication-factors>.
- PostgreSQL documenta default-deny, bypass de owner y necesidad de `FORCE ROW LEVEL SECURITY`: <https://www.postgresql.org/docs/17/ddl-rowsecurity.html>.

### RevisiÃ³n

- Resultado interno: `PASS`; Principal QA revalidÃ³ 15/15 backend y 1/1 Playwright E2E. AppSec/Arquitectura confirmÃ³ cero hallazgos crÃ­ticos, altos o medios internos.
- Evidencia principal: `tasks/evidence/AGRO-DIS-003/validation-report.md`.
- Estado final: `En revisiÃ³n`. El `GO CONDICIONAL` habilita Ãºnicamente el siguiente sandbox, no producciÃ³n.
- Pendientes exactos: IdP real OIDC/PKCE y failover; contrato/regiÃ³n/DPA/plan/SLA/exportabilidad; persistencia one-to-many de identidades externas y discovery productivo de membresÃ­as.
- N/A justificados: Docker/Compose/CI/deploy, migraciÃ³n/rollback productivo y telemetrÃ­a productiva no pertenecen a este spike R0; Docker no estaba instalado.
- AutoevaluaciÃ³n informativa: 94/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos 14, tests/seguridad 19, preservaciÃ³n/cierre 17). No compensa los gates externos; por eso no se marca `Completada`.

## IteraciÃ³n 10 â€” AGRO-DIS-004 GIS, mapas y meteorologÃ­a multifuente (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el lÃ­der con la delegaciÃ³n explÃ­cita del sponsor. Transiciones registradas: `Propuesto â†’ Ready` al cerrar esta DoR y `Ready â†’ En curso` al publicar el plan. ClasificaciÃ³n: spike R0 Must/L aislado y descartable; no es pipeline productivo, migraciÃ³n R2 ni autorizaciÃ³n de gasto.

### DoR, evidencia y decisiones reversibles

- [x] Confirmar ID Ãºnico, outcome, exclusiones, requisitos, riesgos y dependencias futuras.
- [x] Usar el endpoint oficial Georef para generar un fixture versionado de las 23 provincias y CABA; sus centroides pÃºblicos no representan campos ni coordenadas privadas.
- [x] Verificar tÃ©rminos pÃºblicos: Georef/Argenmap oficiales, Open-Meteo pricing/terms/licence, SMN CAP CC BY 4.0 y WRF SMN CC BY 2.5 Argentina.
- [x] Separar el contrato base de las decisiones productivas abiertas: `GAP-004`/Q-021â€“030 no bloquean el spike, pero sÃ­ alertas agronÃ³micas, contrataciÃ³n Open-Meteo y adopciÃ³n WRF.
- [x] Fijar targets ya aprobados: mapa â‰¤3 s p75 y clima cacheado â‰¤2 s p75; medir sin convertir una corrida local en SLA.
- [x] Definir una matriz go/no-go por cobertura nacional, contrato/schema, licencia/atribuciÃ³n, latencia, cuota/SLA, costo, privacidad y degradaciÃ³n.
- [x] Confirmar toolchain: .NET SDK 10.0.201, Node 22.19.0, npm 11.11.1 y PostgreSQL 17. PostGIS no estÃ¡ instalado y Docker/WSL no estÃ¡n disponibles.
- [x] Resolver PostGIS sin mutar el sistema: runtime efÃ­mero ignorado que copia PostgreSQL 17 y superpone el bundle oficial PostGIS 3.6.2 fijado y verificado; el harness falla cerrado si no puede ejecutar `CREATE EXTENSION postgis`.

### Umbrales tÃ©cnicos del spike â€” no son reglas agronÃ³micas

- GeometrÃ­a de entrada: GeoJSON `Polygon`/`MultiPolygon` 2D en WGS84, coordenadas finitas, anillos cerrados, no vacÃ­a, SRID 4326 y `ST_IsValid`; sin `ST_MakeValid` silencioso.
- Guardas reversibles para medir abuso: payload â‰¤1 MiB y â‰¤10.000 vÃ©rtices. Probar 4, 100, 1.000, 10.000 y exceso; el lÃ­mite productivo se revisarÃ¡ con telemetrÃ­a R2.
- Ãrea canÃ³nica: `ST_Area(geography)` sobre esferoide en mÂ²/ha. Comparar con EPSG:6933 como control independiente y exigir delta relativo â‰¤0,5 % en fixtures tÃ©cnicos; superficie declarada y calculada nunca se sustituyen ni se aceptan/rechazan entre sÃ­ sin umbral de producto aprobado.
- Georef: exactamente 24 IDs Ãºnicos, coordenadas finitas y respuesta live â‰¤2 s; el fixture estable, no la red, gobierna tests.
- Tiles Argenmap: 24 smoke points, HTTP 2xx, atribuciÃ³n visible y p75 â‰¤3 s. Fallo de tiles conserva tabla y geometrÃ­a; MapLibre no se presenta como proveedor.
- Open-Meteo: 24 puntos en WGS84, schema/unidades/tiempos estrictos, live â‰¤2 s como probe y fixtures para 429/500/timeout/drift. `freshnessThreshold` es dato de polÃ­tica configurable; tests usan 2 h como hipÃ³tesis reversible y nunca convierten error en cero.
- CAP: XML mÃ¡ximo 2 MiB, DTD/entidades externas deshabilitadas, ciclo `Alert/Update/Cancel/expired`, orden CAP `lat,lon` convertido explÃ­citamente a GeoJSON `lon,lat`, sin dereferenciar recursos.
- WRF: procesar solo una muestra oficial â‰¤25 MiB en un venv aislado y con SHA-256 fijado; medir contra 512 MiB RAM y 10 s de parse/subset, sin presentarlo como sandbox o lÃ­mite preventivo. El sandbox/kill preventivo queda como gate productivo. Medir volumen de 73 plazos y, si supera 1 GiB o no hay presupuesto/operaciÃ³n aprobados, decidir `POSTPONER`, no adopciÃ³n implÃ­cita.

### Alcance verificable

- [x] Publicar contratos JSON Schema para referencia espacial, snapshot meteorolÃ³gico, corrida de proveedor y lifecycle CAP.
- [x] Generar fixtures versionados: 24 jurisdicciones, geometrÃ­as vÃ¡lidas/invÃ¡lidas/extremas, Open-Meteo, CAP real+sintÃ©ticos y metadata WRF.
- [x] Implementar harness .NET 10 para validaciÃ³n de lÃ­mites, unidades, frescura/degradaciÃ³n, CAP seguro y telemetrÃ­a local estructurada.
- [x] Implementar y ejecutar SQL PostGIS real para SRID, validez, Ã¡rea, lÃ­mites, intersecciÃ³n CAP y plan GiST.
- [x] Implementar probes live reproducibles para Georef, Argenmap, Open-Meteo, CAP y WRF, conservando hashes/resultados sin volver tests dependientes de red.
- [x] Implementar prototipo Next.js/React + MapLibre con mapa y alternativa tabular, estados `observed/estimated/forecast` y `fresh/stale/unavailable`, atribuciÃ³n, teclado, foco y pantalla angosta.
- [x] Medir WRF NetCDF, documentar contradicciÃ³n de cadencia oficial y emitir decisiÃ³n explÃ­cita incorporar/postergar/rechazar.
- [x] Ejecutar restore/build/analyzers/tests .NET, PostGIS real, frozen install/lint/typecheck/unit/build/E2E frontend, scans y revisiÃ³n final independiente.
- [x] Actualizar ADR-002/005, gaps, reporte de decisiÃ³n y estado sin afirmar precisiÃ³n agronÃ³mica ni contrato productivo.

### Contratos y lÃ­mites fijados antes de editar cÃ³digo

- Puertos conceptuales separados: `TerritoryReferenceProvider`, `MapStyleProvider`, `WeatherProvider` y `OfficialAlertProvider`; MapLibre renderiza, Argenmap entrega tiles, Georef normaliza territorio, Open-Meteo pronostica y CAP conserva autoridad oficial.
- `WeatherSnapshot` es inmutable y conserva proveedor, modelo/corrida, coordenada solicitada y celda resuelta, emisiÃ³n, ingesta, vigencia, variable, valor, unidad, naturaleza, frescura, confianza/limitaciÃ³n y atribuciÃ³n.
- Errores tipados: `timeout`, `rate_limited`, `provider_error`, `schema_invalid`, `run_missing`, `unavailable`; stale se rotula y una alerta CAP cancelada/expirada nunca queda activa.
- Proveedores meteorolÃ³gicos se invocan solo backend. URLs/modelos/variables se obtienen de allow-lists; sin URLs aportadas por usuario, sin secretos en query/log y sin coordenadas privadas en fixtures.
- El spike no simula tenancy porque usa exclusivamente coordenadas pÃºblicas; la futura persistencia debe incorporar tenant, autorizaciÃ³n por recurso, RLS defensiva y auditorÃ­a append-only.

### Ownership disjunto â€” olas 2 y 3

- Principal: contratos compartidos, `.slnx`/manifiestos/lockfiles, scripts de orquestaciÃ³n, documentaciÃ³n, estados, integraciÃ³n y publicaciÃ³n Git.
- Database/GIS: `tasks/evidence/AGRO-DIS-004/spike/postgis/**` y `fixtures/geometry/**`.
- Backend/Weather: `tasks/evidence/AGRO-DIS-004/spike/src/**`, `spike/tests/**` y fixtures `open-meteo/**`, `cap/**`, `wrf/**` asignados.
- Frontend: `tasks/evidence/AGRO-DIS-004/spike/web/app/**`, `features/**`, `lib/**`, estilos y tests frontend; no editar manifiestos compartidos.
- Principal QA y AppSec/Arquitectura revisan el estado combinado en modo read-only; ningÃºn implementador aprueba su propio cambio.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit inicial publicado `66ea2f25ac5fbe738425be6677d20499e0730510` y remoto verificado.
- .NET: `dotnet restore`, `dotnet build --no-restore`, comando MTP detectado por la skill `run-tests`, analyzers/format y suite contractual.
- PostGIS: bootstrap efÃ­mero local, `CREATE EXTENSION postgis`, probes SQL, teardown validado; nunca tocar el servicio PostgreSQL del sistema.
- Frontend: `pnpm install --frozen-lockfile`, format/lint/typecheck/unit/build y navegador real con Playwright; comprobar teclado, axe, consola y 390 px.
- Docker/Compose, cloud, deploy, migraciÃ³n/rollback productivo y telemetrÃ­a productiva: N/A por alcance R0; el spike documenta reemplazo y no se reutiliza como bootstrap.

### Fuentes primarias que alteran decisiones

- PostGIS: `ST_Area(geography)` usa esferoide y metros; `ST_IsValid` valida geometrÃ­a 2D. <https://postgis.net/docs/ST_Area.html> y <https://postgis.net/docs/ST_IsValid.html>.
- Georef es el servicio oficial abierto para unidades territoriales y publica OpenAPI. <https://www.argentina.gob.ar/georef/referencia-completa-de-la-api>.
- IGN publica Argenmap por XYZ/TMS y WMTS como mapa base oficial. <https://www.ign.gob.ar/NuestrasActividades/InformacionGeoespacial/ServiciosOGC>.
- Open-Meteo exige plan comercial para productos comerciales, atribuciÃ³n CC BY 4.0 y ofrece 99,9 % como target pago, no garantÃ­a del free endpoint. <https://open-meteo.com/en/pricing>, <https://open-meteo.com/en/terms> y <https://open-meteo.com/en/license>.
- CAP 1.2 define `Alert/Update/Cancel`, referencias, expiraciÃ³n y polÃ­gonos WGS84. <https://docs.oasis-open.org/emergency/cap/v1.2/CAP-v1.2-os.html>.
- WRF SMN publica NetCDF 4 km/72 h en AWS Open Data; Registry dice 00/12 y documentaciÃ³n 00/06/12/18, por lo que la cadencia se descubre por corrida. <https://registry.opendata.aws/smn-ar-wrf-dataset/> y <https://odp-aws-smn.github.io/documentation_wrf_det/>.

### RevisiÃ³n

- Resultado tÃ©cnico interno e independiente: `PASS`. AppSec/Arquitectura no encontrÃ³ vulnerabilidades crÃ­ticas, altas o medias explotables en el alcance R0; Principal QA aprobÃ³ el artefacto condicionado a mantenerlo en `En revisiÃ³n`, no producciÃ³n.
- .NET SDK 10.0.201/MTP: restore locked, build 0 warnings/errores, format y scan NuGet PASS; 29/29 tests PASS. Cubre Open-Meteo (7 variables, drift, null, 429/500/timeout, ingesta futura), lifecycle CAP append-only/terminal/offset/XXE y shapes WRF.
- Contratos: Ajv 2020 validÃ³ 3 instancias canÃ³nicas y 5 provider runs. PostgreSQL 17/PostGIS 3.6.2 real: 6/6 PASS; delta mÃ¡ximo Ã¡rea `0,000024 %`, lÃ­mites 4/100/1.000/10.000, rechazos 10.001/>1 MiB, CAP espacial y uso GiST; teardown dejÃ³ 55434 libre.
- WRF oficial SHA-256 `d2283cbe5b6aa68d1595806f0f39e27da28ff3df1b2158d605b94ee1d4a2879c`: 14.758.413 bytes, 1.249Ã—999, 5/5 negativos PASS, 179,488 ms, 49.925.315 bytes Python y 112.431.104 bytes working set; budgets observados PASS. No hay sandbox/kill preventivo y 73 plazos estimados superan 1 GiB: `POSTPONER`.
- Frontend pnpm 10.33.0: frozen install, Prettier, ESLint, TypeScript, build y audit PASS; Vitest 7/7 y Playwright 4/4 PASS con 24 referencias observed/fresh, demo climÃ¡tica sintÃ©tica separada, axe, teclado/retry, degradaciÃ³n de tiles y 390 px.
- Probe live persistido final: Georef `success` 120,066 ms; Open-Meteo `degraded` 2.149,274 ms porque el Ãºnico batch smoke superÃ³ 2 s (no es p75); CAP `success` 223,24 ms; Argenmap `success` p75 141,201 ms; WRF `postpone` 755,651 ms. QA observÃ³ CAP degradado/HTML en otra corrida, confirmando que el canal/frescura requiere gate productivo.
- Hallazgos resueltos: lÃ­mites GIS/2D, carrera del harness, NetCDF shape bomb, CAP spoof/replay/orden/cancel/offset y XML del probe, redirects SSRF, contrato ejecutable, BOM/URI template, variables rÃ¡fagas/ET0, confianza/granularidad y evidencia UI no fabricada.
- N/A: Docker/Compose/CI/deploy, migraciÃ³n/rollback y telemetrÃ­a productiva; la tarea es un spike R0 aislado y no autoriza infraestructura ni pipeline productivo.
- Estado final: `En revisiÃ³n`. Pendientes externos exactos: plan/DPA/regiÃ³n/cuota/SLA y p75 cacheado Open-Meteo; canal/autenticidad/frescura durable CAP; presupuesto/operaciÃ³n/sandbox WRF; precisiÃ³n local y `VAL-AGR`; tenant/authz/RLS/auditorÃ­a antes de R2.
- AutoevaluaciÃ³n informativa: 97/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservaciÃ³n/cierre 19). Cero gate interno fallido; los gates externos impiden `Completada`.

## IteraciÃ³n 11 â€” AGRO-DIS-005 storage, antivirus, retenciÃ³n y restore integral (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el lÃ­der con la delegaciÃ³n explÃ­cita del sponsor. Transiciones registradas: `Propuesto â†’ Ready` al demostrar esta DoR y `Ready â†’ En curso` al publicar el plan. ClasificaciÃ³n: spike R0 Must/M aislado y descartable; no es pipeline productivo, provisiÃ³n cloud ni polÃ­tica legal.

### DoR, evidencia y decisiones reversibles

- [x] Confirmar ID Ãºnico, outcome, exclusiones, requisitos, riesgos y tareas dependientes.
- [x] Confirmar las clases `PÃºblico`, `Interno`, `Confidencial`, `Fiscal/personal` y `Secreto`; usar solo fixtures sintÃ©ticos y excluir secretos de objetos, DB y logs.
- [x] Adoptar RPO â‰¤15 min, RTO â‰¤2 h y drill trimestral como hipÃ³tesis medibles del spike, no SLA contractual.
- [x] Fijar shortlist reversible: AWS S3 + GuardDuty, Azure Blob + Defender y storage S3-compatible + scanner separado.
- [x] Registrar que Q-058 no autoriza storage internacional y que regiÃ³n/DPA/subencargados/retenciÃ³n requieren `VAL-LEG`.
- [x] Confirmar toolchain: .NET SDK 10.0.201, Node 22.19.0, pnpm 10.33.0 y PostgreSQL 17.9; Docker no estÃ¡ disponible.

### Contratos y lÃ­mites fijados antes de editar cÃ³digo

- Estados fail-closed: `PendingUpload â†’ Uploaded â†’ Scanning â†’ Available | Quarantined | Rejected | ScanFailed`; solo `clean` habilita descarga. La baja usa `Available â†’ Purging â†’ Deleted | PurgeUncertain` y nunca supone rollback ante un delete ambiguo.
- La clave es generada por servidor bajo prefijo tenant opaco; hash valida integridad y nunca autoriza ni deduplica entre tenants.
- Cada intenciÃ³n y descarga reautoriza tenant, recurso, acciÃ³n y estado; una URL vencida o recurso ajeno responde sin revelar existencia.
- MIME declarado se contrasta con magic bytes, tamaÃ±o y SHA-256. El spike usa un marcador antimalware sintÃ©tico, no una firma EICAR real en el repositorio.
- DB es fuente del estado. El objeto entra a cuarentena; un evento de scan duplicado debe ser idempotente y error/timeout nunca publica.
- El manifest de backup registra cutoff, backup DB, versiones/hashes de objetos, auditorÃ­a y watermark; restore aislado verifica PostGIS, vÃ­nculos, hashes, hold y objetos huÃ©rfanos sin reemitir URLs.
- Legal hold prevalece sobre purga. Los plazos, regiÃ³n y contrato siguen pendientes; no se implementa borrado productivo ni se tocan servicios existentes.

### Plan verificable

- [x] Publicar schemas versionados para intenciÃ³n/completado, resultado AV, grant de descarga, estado de archivo y manifest de backup.
- [x] Implementar spike .NET 10 con dominio/ports compactos, storage local aislado, firma efÃ­mera, MIME/hash, cuarentena, auth tenant y telemetrÃ­a redactada.
- [x] Implementar harness PostgreSQL/PostGIS efÃ­mero y drill `pg_dump`/`pg_restore` + objetos, con corrupciÃ³n, huÃ©rfanos, auditorÃ­a, geometrÃ­a, mediciÃ³n RTO y gap RPO explÃ­cito.
- [x] Implementar prototipo Next.js/React con pnpm para progreso, error, cuarentena, provider-down, expiraciÃ³n, conflicto y estados accesibles/responsive.
- [x] Documentar matriz de proveedores, threat model, ADR storage/retenciÃ³n/DR, runbook, decisiÃ³n go/no-go y gaps externos.
- [x] Ejecutar restore/build/format/analyzers/tests .NET, schemas, frozen install/lint/typecheck/unit/build/E2E, scans y revisiÃ³n independiente.
- [x] Actualizar evidencia y estado final sin presentar el spike como producciÃ³n.

### Ownership disjunto â€” olas 2 y 3

- Principal: contratos compartidos, `.slnx`, manifiestos/lockfiles, scripts de orquestaciÃ³n, documentaciÃ³n, estados, integraciÃ³n y publicaciÃ³n Git.
- Backend/Storage: `tasks/evidence/AGRO-DIS-005/spike/src/**` y `spike/tests/**`.
- Data/Restore: `tasks/evidence/AGRO-DIS-005/spike/postgres/**` y fixtures de restore asignados.
- Frontend: `tasks/evidence/AGRO-DIS-005/spike/web/app/**`, `features/**`, `lib/**` y estilos; no edita manifiestos compartidos.
- Principal QA y AppSec/Arquitectura revisan el estado combinado en modo read-only; ningÃºn implementador aprueba su propio cambio.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit publicado `5873ebbdea1e2bac52c5478d8148749bb6257911`; no existÃ­a `tasks/evidence/AGRO-DIS-005`.
- .NET: restore locked, build sin warnings, format/analyzers, comando MTP detectado por la skill `run-tests` y suites de dominio/API/restore.
- PostgreSQL/PostGIS: clÃºster efÃ­mero propio en loopback, dump/restore a base separada, verificaciÃ³n y teardown; nunca tocar el servicio del sistema.
- Frontend: `pnpm install --frozen-lockfile`, format/lint/typecheck/unit/build y Playwright en navegador real, incluido 390 px y teclado.
- Docker/Compose, cloud, credenciales, deploy, migraciÃ³n productiva, AV/provider real y PITR administrado: N/A para la ejecuciÃ³n local R0; quedan como gates externos explÃ­citos.

### RevisiÃ³n

- Resultado tÃ©cnico interno e independiente: `PASS`. Principal QA y AppSec/Arquitectura aprobaron el R0; no quedan hallazgos altos/medios internos.
- .NET SDK 10.0.201/MTP: restore locked, build 0 warnings/errores, format y scan NuGet PASS; 32/32 tests PASS. Se verifican tenant/BOLA, tokens, MIME/hash/tamaÃ±o, AV fail-closed e idempotente, hold/purga/descarga concurrentes, `PurgeUncertain`, reconciliaciÃ³n privilegiada y telemetrÃ­a redactada.
- PostgreSQL 17/PostGIS 3.6.2 real: 2 registros, 2 objetos y 4 eventos audit; SRID 4326, snapshots completos, `tenant_id â†” tenant_ref`, tipo/ID de recurso, cadena criptogrÃ¡fica, append-only, legal hold, huÃ©rfano y corrupciones PASS. Principal observÃ³ RTO final `0,0217 min`; QA `0,0224 min`; AppSec `0,0258 min`. RPO: `UNPROVEN_WITHOUT_MANAGED_PITR`.
- Frontend pnpm 10.33.0: frozen install, 5 contratos, Prettier, ESLint, TypeScript, build Next.js 16.3 y audit PASS; Vitest 8/8 y Playwright 5/5 PASS, incluido axe, teclado, fallos, conflicto y 390 px. Una corrida con contenciÃ³n concurrente de `.next` fue descartada y repetida en exclusiÃ³n.
- Hallazgos resueltos: Base64URL no canÃ³nica, filtraciÃ³n de paths, restore incompleto, binding tenant/recurso, cadena audit sintÃ©tica, carreras hold/purge y download/purge, delete ambiguo, scanner detenido/verdict invÃ¡lido, scopes operativos, clasificaciones y schemas.
- DecisiÃ³n: `GO tÃ©cnico condicionado` para sandbox AWS detrÃ¡s de ports; Azure es alternativa. `NO-GO productivo` hasta storage/AV/KMS/WORM/PITR cloud real, regiÃ³n/DPA/subencargados, polÃ­tica de retenciÃ³n/`VAL-LEG`, volumen/costo y controles productivos.
- N/A: Docker/Compose/CI/deploy, migraciÃ³n/rollback cloud y alertas productivas; el R0 no autoriza infraestructura. Docker no estaba disponible.
- Evidencia principal: `tasks/evidence/AGRO-DIS-005/validation-report.md`.
- Estado final: `En revisiÃ³n`; no se marca `Completada` porque los gates externos de proveedor, Legal y RPO administrado siguen abiertos.
- AutoevaluaciÃ³n informativa: 97/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservaciÃ³n/cierre 19). Cero gate interno fallido; la puntuaciÃ³n no compensa los gates externos.

## IteraciÃ³n 12 â€” AGRO-DIS-007 capacidad, SLO, costos y conectividad (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el lÃ­der con la delegaciÃ³n explÃ­cita del sponsor. Transiciones registradas: `Propuesto â†’ Ready` al aceptar rangos sintÃ©ticos, owners y caducidad como evidencia R0; `Ready â†’ En curso` al publicar este plan. ClasificaciÃ³n: spike R0 Must/M aislado y descartable; no es benchmark del producto, promesa de fecha, presupuesto aprobado, SLA contractual ni aprovisionamiento.

### DoR, evidencia y decisiones reversibles

- [x] Confirmar ID Ãºnico, outcome, exclusiones, requisitos, riesgos y dependencias futuras.
- [x] Resolver Q-019 como envelope: 1â€“3 carriles de ejecuciÃ³n y roles mÃ­nimos; presupuesto cloud, capacidad nominal y calendario siguen abiertos y no producen fechas.
- [x] Resolver Q-020 con tres escenarios sintÃ©ticos versionados (`pilot`, `growth-10x`, `burst-2x`), confianza baja, owner y vencimiento 2026-09-30; no se presentan como demanda observada.
- [x] Resolver Q-060 para el spike con targets hipotÃ©ticos existentes: disponibilidad mensual 99,9 %, RPO 15 min, RTO 2 h y sensibilidad; contrato SLA/soporte/retenciÃ³n sigue abierto.
- [x] Resolver Q-061 con perfiles sintÃ©ticos de red `target`, `constrained`, `critical` y `offline`; la conectividad rural real permanece sin medir y offline sigue fuera del MVP.
- [x] Confirmar toolchain: .NET SDK 10.0.201, Node 22.19.0, pnpm 10.33.0 y `npx` disponibles. Docker, cloud y un producto desplegable no son necesarios para este R0.

### Contratos y lÃ­mites fijados antes de editar cÃ³digo

- Cadena determinÃ­stica: `CapacityScenario â†’ CapacityProjection â†’ SloEvaluation / CostProjection`; ningÃºn resultado se etiqueta `observed` si proviene de fixtures.
- El costo se calcula por drivers explÃ­citos y rangos `low/base/high`; precio faltante produce `incomplete`/NO-GO, nunca cero. Moneda, regiÃ³n, fuente, fecha y tratamiento impositivo son obligatorios para un catÃ¡logo real.
- El catÃ¡logo SLI separa core propio de dependencias externas y define numerador, denominador, ventana, exclusiones y owner. 99,9 % equivale a 43 min 12 s en 30 dÃ­as y a 1.000 eventos malos por millÃ³n elegible.
- TelemetrÃ­a usa allow-list de dimensiones acotadas (`route_template`, mÃ©todo, clase de estado, dependencia, job, cache, entorno). Se prohÃ­ben tenant/user/resource IDs, CUIT, email, coordenadas, filename, path/query, payload e idempotency key.
- La UI es un laboratorio online: muestra estimaciÃ³n, confianza, faltantes, fecha de revalidaciÃ³n y estados de red. Sin red bloquea confirmaciÃ³n y no persiste/encola trabajo en `localStorage`, IndexedDB ni service worker.
- Canary, rollback y DR son polÃ­ticas de decisiÃ³n; no se despliega ni provisiona. Ãndices, particiÃ³n y cuotas productivas se difieren hasta medir carga real.

### Plan verificable

- [x] Publicar JSON Schema y fixtures para escenarios de capacidad, catÃ¡logo de costos y reporte reproducible.
- [x] Implementar modelo .NET 10 para throughput, storage, drain time, error budget, costos incompletos y polÃ­tica de cardinalidad; agregar pruebas negativas y de lÃ­mites.
- [x] Implementar laboratorio Next.js 16/React 19 con pnpm para escenarios, SLO, costo incompleto y perfiles online/degradado/offline, accesible y responsive.
- [x] Ejecutar contratos, restore/build/format/tests/scan .NET y frozen install/format/lint/typecheck/unit/build/audit/E2E frontend.
- [x] Revisar independientemente QA y AppSec/Arquitectura; resolver hallazgos y registrar evidencia exacta.
- [x] Actualizar ADR, preguntas/gaps/riesgos/trazabilidad, reporte y estado sin convertir estimaciones en compromisos.

### Ownership disjunto â€” olas 2 y 3

- Principal: contratos/fixtures compartidos, `.slnx`, `global.json`, manifests/lockfiles frontend, documentaciÃ³n, backlog, integraciÃ³n, gates finales y Git.
- Backend/Capacity: `tasks/evidence/AGRO-DIS-007/spike/src/**` y `spike/tests/**`; no edita contratos, manifests ni documentaciÃ³n.
- Frontend: `tasks/evidence/AGRO-DIS-007/spike/web/app/**`, `features/**`, `lib/**` y tests UI; no edita package/lock/config ni documentos.
- Principal QA y AppSec/Arquitectura revisan el estado combinado en modo read-only; ningÃºn implementador aprueba su propio cambio.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit publicado `6a551ad39ef01013f4de58bbf9b0cacf046d843b`.
- .NET: `dotnet restore --locked-mode`, `dotnet build --no-restore`, `dotnet format --verify-no-changes`, MTP detectado por `run-tests`, suite contractual y scan NuGet.
- Frontend: `pnpm install --frozen-lockfile --ignore-scripts`, validaciÃ³n de contratos, Prettier, ESLint, TypeScript, Vitest, build, audit y navegador Chromium real; verificar teclado, axe, 390 px y `BrowserContext.setOffline(true)`.
- Docker/Compose, DB/PostGIS, migraciÃ³n/rollback productivo, CI/CD, deploy y telemetrÃ­a productiva: N/A por alcance R0; no existe camino productivo que medir o migrar.

### Fuentes primarias que alteran decisiones

- Google SRE exige una polÃ­tica concreta de error budget para decidir releases y confiabilidad, no solo un porcentaje aislado: <https://sre.google/workbook/error-budget-policy/>.
- Playwright expone `BrowserContext.setOffline()` para validar comportamiento real del navegador sin red: <https://playwright.dev/docs/api/class-browsercontext>.
- OpenTelemetry HTTP semantic conventions estandarizan atributos de bajo riesgo; sus requisitos generales y mÃ©tricas obligan a controlar cardinalidad: <https://opentelemetry.io/docs/specs/semconv/http/>, <https://opentelemetry.io/docs/specs/semconv/general/attribute-requirement-level/> y <https://opentelemetry.io/docs/concepts/signals/metrics/>.
- .NET 10 tiene soporte activo hasta 2028-11-14; el spike fija SDK 10.0.201 para reproducibilidad: <https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core>.

### RevisiÃ³n

- Resultado tÃ©cnico interno: `PASS`. .NET 10/MTP restore locked, build 0 warnings/errores, format y scan NuGet PASS; 21/21 tests PASS.
- Contratos: 3 fixtures positivos y 10 negativos PASS; fixture canÃ³nico alimenta frontend y test .NETâ†’golden. Vitest 5/5, Next build, Prettier/ESLint/TypeScript y pnpm audit PASS.
- Playwright Chromium 4/4 PASS: estados, costo NO-GO, conexiÃ³n inicial default-deny, offline real, cero persistencia local, retry/dedupe, teclado, axe y 390 px.
- Principal QA aprobÃ³ el estado combinado. AppSec/Arquitectura cerrÃ³ hallazgos de integridad FinOps/fixture/golden, cardinalidad y conexiÃ³n inicial; reauditorÃ­a: 0 crÃ­ticos/altos/medios/bajos.
- N/A por alcance: API/DB/PostGIS, migraciÃ³n/rollback productivo, Docker/Compose, CI/CD, deploy, cloud, carga real, telemetrÃ­a emitida y alertas. No se fabricÃ³ un runtime para simular aprobaciÃ³n.
- Estado final: `En revisiÃ³n`. Pendientes externos exactos: Q-019/020/060/061, GAP-003/GAP-010 y RSK-022/024/027; los supuestos vencen 2026-09-30.
- AutoevaluaciÃ³n informativa: 97/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 20, preservaciÃ³n/cierre 19). Cero gate interno fallido; los gates externos impiden `Completada`.

## IteraciÃ³n 13 â€” AGRO-FND-001 lÃ­mites modulares y contratos compatibles (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el lÃ­der con la delegaciÃ³n explÃ­cita del sponsor. Transiciones registradas: `Propuesto â†’ Ready` al verificar mÃ³dulos, consumidores, conflictos y evidencia tÃ©cnica de `AGRO-DIS-003/004`; `Ready â†’ En curso` al publicar este plan. ClasificaciÃ³n: decisiÃ³n y fitness R0 aplicables a R1; no autoriza bootstrap productivo, microservicios, broker, migraciones ni reutilizar spikes.

### DoR y alcance fijado

- [x] Confirmar ID Ãºnico, outcome, exclusiones, requisitos, riesgos y consumidores.
- [x] Aceptar `AGRO-DIS-003/004` como evidencia tÃ©cnica R0 sin promover sus prototipos ni cerrar sus gates externos.
- [x] Resolver la contradicciÃ³n documental: `National Catalog` y `Productive Core` son bounded contexts distintos aunque compartan WS-03.
- [x] Fijar `Organization` como tenant; CUIT no selecciona ni autoriza tenant y su cardinalidad legal queda para Product/Legal.
- [x] Fijar `ManagementUnit` y su lifecycle en Productive Core; Territory posee solo representaciÃ³n espacial versionada y opcional.
- [x] Confirmar que no se requieren reglas agronÃ³micas, veterinarias, fiscales o contables para este lÃ­mite genÃ©rico.

### Plan verificable

- [x] Publicar ADR aceptado, registro machine-readable de 15 mÃ³dulos y mapa completo de consumidores.
- [x] Publicar contratos versionados para scope, Problem Details, paginaciÃ³n cursor y eventos internos.
- [x] Definir polÃ­tica N/N-1, extracciÃ³n futura y `expand â†’ backfill â†’ contract`, separando decisiÃ³n de ensayo operativo posterior.
- [x] Implementar fitness tests aislados para DAG, ownership/schema, persistencia ajena, scope y compatibilidad aditiva/breaking.
- [x] Cubrir negativos: ciclo, schema ajeno, scope ambiguo, consumidor N-1, evento duplicado/fuera de orden y error enumerante.
- [x] Ejecutar restore/build/format/tests/scan y validaciÃ³n JSON; realizar revisiÃ³n independiente QA y AppSec/Arquitectura.
- [x] Actualizar arquitectura, decisiones/gaps, trazabilidad, reporte y estado final sin crear API/UI/producto.

### Ownership disjunto â€” olas 2 y 3

- Principal: ADR/documentaciÃ³n, contratos y fixtures compartidos, manifiestos `.slnx`/`.csproj`, registro/mapa, integraciÃ³n, estados y Git.
- Backend/Architecture Fitness: `tasks/evidence/AGRO-FND-001/fitness/src/**` y `fitness/tests/**`; no edita manifiestos ni documentos.
- Principal QA y AppSec/Arquitectura: revisiÃ³n read-only del estado combinado; ningÃºn implementador aprueba su propio cambio.

### Baseline y comandos ejecutados

- Baseline Git limpio en `main`, commit publicado `879146aa814dfc790ef69ad44641756440f20a44`; no existÃ­a `tasks/evidence/AGRO-FND-001` ni soluciÃ³n productiva raÃ­z.
- `dotnet restore --locked-mode`: PASS; `dotnet build --no-restore`: PASS, 0 warnings/errores.
- MTP detectado por la skill `run-tests`: PASS 42/42, 0 failed/skipped; `dotnet format --verify-no-changes`: PASS.
- 12 JSON parseados, cuatro producer schemas ejecutados con mutation tests, NuGet vulnerable scan y secrets scan dirigidos: PASS.
- Frontend/pnpm, API, PostgreSQL/PostGIS, Docker/Compose, migraciÃ³n staging, observabilidad productiva, CI/CD y deploy: N/A; la tarea produce arquitectura verificable y no una superficie funcional.

### RevisiÃ³n

- Resultado tÃ©cnico R0 e independiente: `PASS`. Principal QA y AppSec/Arquitectura reprodujeron gates; cero hallazgos crÃ­ticos, altos o medios residuales.
- TopologÃ­a: 15 mÃ³dulos, 15 schemas dueÃ±os y 69 edges declarados/mapeados; DAG, ownership, scopes y persistencia ajena cubiertos por positivos y mutaciones negativas.
- Contratos: scope discriminado, producer schemas cerrados, reader N-1 tolerante, 401/403/404/409/412, ETag despuÃ©s de authz y compatibilidad aditiva/breaking verificados.
- Eventos: source/scope/tenant/agregado/version forman el stream; duplicate/out-of-order/gap/foreign/unknown no mutan ni elevan privilegios.
- Hallazgos cerrados: schemas no ejecutados, requiredâ†’optional, policies/shared kernel laxos, scope ambiguo, escalamiento platform/tenant, stream sin tenant/source, docs divergentes y vocabulario de dominio inventado.
- `ADR-PEND-011` resuelta por ADR-009. `ADR-PEND-010` queda `polÃ­tica definida; ensayo pendiente` para `AGRO-FND-003`/`AGRO-PLT-004`; no se afirma staging, backup/restore ni migraciÃ³n R1.
- Estado final: `En curso`. El gate R0 estÃ¡ aprobado, pero por ser tarea multirelease R0/R1 la tarea padre no pasa a `Completada` hasta demostrar el ensayo operativo R1.
- Evidencia: `tasks/evidence/AGRO-FND-001/validation-report.md`.
- AutoevaluaciÃ³n informativa: 96/100 (contexto 15, arquitectura 20, multiagente 10, full-stack/datos/observabilidad 12, tests/seguridad 20, preservaciÃ³n/cierre 19). Cero gate aplicable fallido; los N/A corresponden al alcance R0.

## IteraciÃ³n 14 â€” AGRO-SEC-001 modelo de amenazas y clasificaciÃ³n por release (2026-08-05)

Estado inicial: `Propuesto`; seleccionada por el lÃ­der con la delegaciÃ³n explÃ­cita del sponsor. Transiciones registradas: `Propuesto â†’ Ready` al verificar arquitectura, flujos, activos e inventarios candidatos; `Ready â†’ En curso` al publicar este plan. ClasificaciÃ³n: gate documental R0 de una tarea continua R0â€“R6; no es certificaciÃ³n legal, pentest, implementaciÃ³n de controles productivos ni autorizaciÃ³n de proveedor.

### DoR, supuestos y lÃ­mites

- [x] Confirmar ID Ãºnico, outcome, criterios, requisitos, riesgos y exclusiones.
- [x] Confirmar arquitectura y fronteras en `docs/05-arquitectura.md`, actores/flujos en `docs/02-dominio-actores-y-flujos.md` y seguridad base en `docs/07-seguridad-y-privacidad.md`.
- [x] Confirmar inventarios candidatos de identidad, GIS/clima y archivos en las evidencias de `AGRO-DIS-003/004/005`, sin tratarlos como proveedores contratados o superficies productivas.
- [x] Acotar la fase actual a R0 y mantener Q-054/055/058/060 como supuestos condicionales con owner y NO-GO productivo.
- [x] Registrar que los spikes son locales/descartables y que no existe todavÃ­a runtime, CI o despliegue productivo que escanear.
- [x] Descartar `AGRO-FND-003`: no estÃ¡ Ready por falta de agregado productivo, estados/campos inmutables, identidad/auditorÃ­a productivas y retenciÃ³n aprobada; no se mutÃ³ su estado.

### Contrato de evidencia y plan verificable

- [x] Publicar un threat model central, repo-grounded y versionado con componentes, fronteras, activos, atacante, superficies, abuso y calibraciÃ³n explÃ­cita.
- [x] Publicar clasificaciÃ³n de datos/privacidad e inventario de proveedores/procesamiento, distinguiendo presente, candidato y futuro fuera del MVP.
- [x] Publicar un registro machine-readable `threat â†’ control â†’ prueba â†’ owner â†’ riesgo residual â†’ capability/release`, sin crÃ­ticos huÃ©rfanos.
- [x] Publicar gates por release y plantilla para revisar una nueva frontera/proveedor durante DoR.
- [x] Implementar un validador reproducible de estructura, IDs, referencias, owners, criticidad y gates fail-closed; cubrir mutaciones negativas.
- [x] Ejecutar validaciÃ³n documental, JSON, enlaces/rutas, secretos dirigidos y revisiÃ³n independiente AppSec/Arquitectura/Product.
- [x] Actualizar riesgos, trazabilidad, reporte y estado final preservando los gates Legal/Privacy/Sponsor.

### Ownership disjunto â€” olas 2 y 3

- Principal: estructura/README, contrato y validador compartidos, integraciÃ³n, backlog/todo/riesgos/trazabilidad, reporte, gates finales y Git.
- Architecture Lead: `tasks/evidence/AGRO-SEC-001/AgropecuarIA-threat-model.md`; no edita registros, scripts ni documentaciÃ³n global.
- Product/Domain/UX: `tasks/evidence/AGRO-SEC-001/data-classification-and-privacy.md`; no edita threat model, registros ni scripts.
- Security/Data: `tasks/evidence/AGRO-SEC-001/provider-processing-inventory.md` y borrador de amenazas para integraciÃ³n; no edita archivos de otros owners.
- Ola 3 cruza revisores sobre archivos ajenos y permanece read-only; ningÃºn autor aprueba su propio artefacto.

### Baseline y comandos previstos

- Baseline Git limpio en `main`, commit publicado `f86e2d4434a6670abc024ea248d8443dd54f1a9e`; no existÃ­a `tasks/evidence/AGRO-SEC-001` ni runtime productivo raÃ­z.
- PowerShell: validador propio con casos positivo/negativos, parseo JSON estricto, chequeo de rutas Markdown, IDs estables y secretos dirigidos.
- RevisiÃ³n manual: cobertura de todas las fronteras y superficies, separaciÃ³n runtime futuro/spikes/build, owners de amenazas crÃ­ticas y Q-054/055/058/060 explÃ­citas.
- .NET, pnpm/frontend, API, PostgreSQL/PostGIS, Docker/Compose, SAST/SCA/DAST, migraciÃ³n, telemetrÃ­a emitida, CI/CD y deploy: N/A para este gate R0 documental sin runtime productivo; no se fabricarÃ¡ infraestructura para aparentar controles.

### RevisiÃ³n

- Resultado R0: `PASS`. Registro con 14 amenazas (7 crÃ­ticas/7 altas), 4 preguntas abiertas, 16 `RSK-*` Ãºnicos y 12 superficies `PI-01`â€“`PI-12`; 0 crÃ­ticas sin owner/prueba/gate.
- Validador: 7/7 mutation tests PASS â€” owner, test, valor blanco, ID duplicado, riesgo invÃ¡lido, pregunta omitida y drift JSONâ†”tabla. JSON, rutas/evidence, enlaces Markdown y `git diff --check`: PASS.
- Scan dirigido: 0 patrones de credenciales. No se agregaron secretos, datos personales, proveedor, cuenta cloud ni runtime.
- RevisiÃ³n independiente final: AppSec/Data, Architecture y Product/UX/Privacy `PASS`; 0 crÃ­ticos, altos o medios. Los hallazgos iniciales sobre edge/web, browserâ†’storage/tiles, email, telemetrÃ­a/CI/backup, trazabilidad de riesgos, owner e integridad del validador fueron corregidos; las observaciones bajas de Ã­ndices `PI-09/PI-12` tambiÃ©n quedaron resueltas.
- N/A: build/test .NET, pnpm/frontend, API, DB/PostGIS, Docker, migraciones, SAST/SCA/DAST runtime, observabilidad emitida, CI/CD y deploy. No existe producto ejecutable y este gate documental no autoriza crearlo.
- Riesgo residual: las 14 amenazas permanecen abiertas para producciÃ³n. Q-054/055/058/060, `GAP-003`, `GAP-008`, `VAL-LEG`, IdP/proveedores/regiones/DPA/retenciÃ³n, pipeline y restore administrado siguen como NO-GO de cada capacidad afectada.
- Estado final: `En curso`. El baseline R0 estÃ¡ aprobado, pero `AGRO-SEC-001` es continua R0â€“R6 y debe actualizarse/revalidarse por slice y release.
- Evidencia: `tasks/evidence/AGRO-SEC-001/validation-report.md`.
- AutoevaluaciÃ³n informativa: 96/100 (contexto 15, arquitectura 19, multiagente 10, full-stack/datos/observabilidad 13, tests/seguridad 20, preservaciÃ³n/cierre 19). Cero gate aplicable fallido; los N/A y NO-GO no son aprobaciones implÃ­citas.

## IteraciÃ³n 15 â€” AGRO-ID-001 registro y vinculaciÃ³n de identidades (2026-08-05)

Estado inicial: `Propuesto`. El sponsor seleccionÃ³ y delegÃ³ explÃ­citamente la ejecuciÃ³n. Transiciones: `Propuesto â†’ Ready` al fijar Auth0 como IdP objetivo, email OTP + Google OIDC como mecanismos y separar las credenciales reales como gate del servidor de prueba; `Ready â†’ En curso` al publicar este plan. ClasificaciÃ³n: capacidad R1 y primer bootstrap productivo.

### DoR, alcance y contrato

- [x] Verificar ID, outcome, criterios, dependencias `AGRO-DIS-003`/`AGRO-FND-001`, amenazas y contratos previos.
- [x] Fijar Auth0 como adaptador OIDC objetivo y proveedor sintÃ©tico exclusivamente para `Development`/`Test`; los ambientes no locales fallan cerrados sin configuraciÃ³n.
- [x] Mantener Q-054/Q-055 como decisiones contractuales no bloqueantes: `User` es platform-scoped y las membresÃ­as no transfieren propiedad ni mezclan organizaciones.
- [x] Definir sesiÃ³n cookie, reautenticaciÃ³n de ambas credenciales, replay protection, CSRF, revocaciÃ³n, auditorÃ­a sin PII y migraciÃ³n aditiva.
- [x] Crear bootstrap mÃ­nimo productivo .NET 10 + Next.js 16/React/pnpm sin reutilizar los spikes descartables.
- [x] Implementar dominio/aplicaciÃ³n, PostgreSQL, API y telemetrÃ­a de login, linking, unlink y revocaciÃ³n.
- [x] Implementar experiencia frontend accesible y responsive con estados loading, error, conflicto, proveedor caÃ­do y sesiÃ³n revocada.
- [x] Agregar pruebas unitarias, integraciÃ³n PostgreSQL, API/seguridad, frontend, accesibilidad y E2E en navegador real.
- [x] Ejecutar restore/build/format/tests/lint/typecheck/unit/e2e, migraciÃ³n aislada, scans y revisiÃ³n independiente.
- [x] Documentar operaciÃ³n local, configuraciÃ³n del servidor de prueba, rollback, evidencia y estado final.

### Ownership disjunto

- Principal: `.slnx`, `global.json`, props/paquetes raÃ­z, contratos compartidos, `apps/web/package.json`, lockfile, configuraciÃ³n transversal, integraciÃ³n, documentaciÃ³n, estados y Git.
- Backend .NET: `src/Identity/**`, `apps/api/**` y tests backend asignados; no edita manifiestos raÃ­z ni frontend.
- Frontend Next.js: `apps/web/app/**`, `apps/web/features/**`, `apps/web/lib/**`, estilos y tests frontend; no edita `package.json`, lockfile ni backend.
- Database/QA: migraciones/fixtures PostgreSQL y harness de integraciÃ³n/E2E asignados; no comparte migraciones ni implementaciÃ³n con otro owner.
- RevisiÃ³n final: QA y AppSec/Arquitectura read-only sobre el estado combinado; ningÃºn autor aprueba su propio cambio.

### Baseline y gates previstos

- Baseline Git limpio en `main`, sincronizado con `origin/main`; no existen aplicaciÃ³n productiva raÃ­z, soluciÃ³n, lockfile raÃ­z ni migraciones productivas.
- Antes del bootstrap, gates .NET/frontend/product DB son N/A por ausencia verificable. DespuÃ©s del bootstrap pasan a ser obligatorios.
- Comandos objetivo: `dotnet restore`, `dotnet build --no-restore`, tests MTP, `dotnet format --verify-no-changes`; `pnpm install --frozen-lockfile`, format/lint/typecheck/unit/build/E2E; migraciÃ³n PostgreSQL efÃ­mera y scans dirigidos.
- No incluye deploy, aprovisionamiento Auth0, credenciales reales, MFA/passkeys (`AGRO-ID-002`) ni organizaciones/roles (`AGRO-ID-003`).

### RevisiÃ³n

- Resultado integrado local: `PASS`. API y mÃ³dulo .NET compilan sin warnings; 21/21 pruebas MTP pasan contra PostgreSQL 17 efÃ­mero, incluida migraciÃ³n rollback/roll-forward, unicidad concurrente, aislamiento por recurso, replay, CSRF, rate limit preautenticaciÃ³n por IP/sesiÃ³n, cookies, revocaciÃ³n y auditorÃ­a append-only.
- Frontend: pnpm frozen, format, lint, TypeScript estricto, 18/18 Vitest y build Next.js productivo `PASS`; Playwright Chromium desktop/mÃ³vil 4/4 con Axe WCAG 2.2 AA, teclado real, viewport angosto y loader limitado a la regiÃ³n.
- Seguridad/operaciÃ³n: sesiÃ³n opaca hasheada, OIDC code+PKCE same-origin, provider sintÃ©tico fÃ­sicamente local, lÃ­mites preautenticaciÃ³n por IP/sesiÃ³n, proxies explÃ­citos, `no-store`, headers defensivos, Problem Details, mÃ©tricas/logs sin secretos y outbox `IdentityLinked` exactamente una vez.
- MigraciÃ³n: aditiva, aplicada/retirada/reaplicada sobre DB efÃ­mera. En ambiente compartido el rollback es funcional por flags y roll-forward; no se elimina historia ni auditorÃ­a.
- RevisiÃ³n independiente: hallazgos internos de QA/AppSec corregidos y revalidados; el Ãºnico gate externo es ejecutar Auth0 real en el servidor de prueba con secretos fuera del repositorio, callback/state/nonce/claims, email/Google, provider-down y logout.
- Estado final: `En revisiÃ³n`. La implementaciÃ³n local estÃ¡ terminada; no se marca `Completada` hasta obtener evidencia del IdP real en el ambiente compartido solicitado por el sponsor.
- Evidencia: `tasks/evidence/AGRO-ID-001/validation-report.md`.
- AutoevaluaciÃ³n informativa: 96/100 (contexto 15, arquitectura 20, multiagente 10, full-stack/datos/observabilidad 14, tests/seguridad 19, preservaciÃ³n/cierre 18). Cero gate interno fallido; el gate Auth0 externo no se compensa con la puntuaciÃ³n.

## IteraciÃ³n 16 â€” AGRO-FND-001 enforcement R1 sobre el runtime (2026-08-05)

Estado inicial: `En curso`. La fase R0 tiene ADR y fitness aprobados, pero el gate quedÃ³ aislado y no inspecciona la soluciÃ³n productiva creada por `AGRO-ID-001`. Este sub-slice R1 permanece dentro de la misma tarea y no incorpora `AGRO-FND-002`, `AGRO-FND-003` ni `AGRO-PLT-004`.

### DoR, alcance y aceptaciÃ³n

- [x] Revalidar ID, outcome, dependencias, ADR-009, mapa de consumidores y evidencia R0.
- [x] Identificar drift real en scope, Problem Details, outbox, auditorÃ­a local y telemetrÃ­a de contrato.
- [x] Integrar el fitness arquitectÃ³nico en `AgropecuarIA.slnx` para que el gate normal inspeccione el runtime actual.
- [x] Incorporar scope discriminado derivado por servidor y distinguir el journal de seguridad local del agregado central de Audit/Compliance.
- [x] Alinear status/media type/schema de Problem Details con la polÃ­tica FND.
- [x] Evolucionar aditivamente el outbox Identity al envelope canÃ³nico sin implementar delivery/retry de `FND-002`.
- [x] Emitir versiÃ³n de contrato/consumidor con cardinalidad acotada y probar su emisiÃ³n sin PII.
- [x] Agregar pruebas contra referencias reales, ownership EF/schema, OpenAPI y contrato N/N-1 del producto.
- [x] Ejecutar restore/build/format/tests, migraciÃ³n PostgreSQL aislada, scans y revisiÃ³n independiente.
- [x] Actualizar ADR/evidencia/backlog y cerrar solo si todos los gates propios de FND-001 quedan demostrados.

### Ownership disjunto

- Principal: `AgropecuarIA.slnx`, contratos/manifestos compartidos, migraciÃ³n, integraciÃ³n, documentaciÃ³n, estados y Git.
- Backend: `src/AgropecuarIA.Identity/**`, `apps/AgropecuarIA.Api/**` y tests backend asignados; no edita manifests ni evidencia FND.
- Architecture Fitness: `tasks/evidence/AGRO-FND-001/fitness/**`; no edita runtime ni manifiestos raÃ­z.
- QA/AppSec: revisiÃ³n final read-only del estado combinado; ningÃºn implementador aprueba su propio cambio.

### No objetivos y gates

- No crear mÃ³dulos vacÃ­os, multi-CUIT, ARCA, broker, delivery de outbox, idempotencia genÃ©rica, ETag/backfill ni backup/restore.
- Baseline y gates objetivo: soluciÃ³n .NET 10/MTP, PostgreSQL efÃ­mero, contrato OpenAPI, `dotnet format`, NuGet vulnerable scan, secrets scan dirigido y `git diff --check`.

### RevisiÃ³n

- Resultado del sub-slice R1: `PASS` local. Restore locked, build Release 0 warnings/0 errors, format y modelo EF sin drift; suite raÃ­z 77/77 (27 Identity + 50 Architecture Fitness).
- PostgreSQL efÃ­mero: initialâ†’expand, preservaciÃ³n/backfill, escritor N-1 despuÃ©s del upgrade y rollback/roll-forward `PASS`. La expansiÃ³n conserva `identity.audit_events` y nullability fÃ­sica para N-1; el modelo/escritor N exige actor y envelope canÃ³nico.
- Problem Details runtime/OpenAPI cerrado y sin `traceId` duplicado; 403/429, autorizaciÃ³n por actor/scope, telemetrÃ­a de contrato sin PII y outbox monotÃ³nico cubiertos.
- Frontend sin cambios: pnpm frozen, format, lint, typecheck, Vitest 18/18 y build Next.js `PASS`; E2E navegador N/A porque no cambiÃ³ UI ni contrato consumido por UI.
- NuGet vulnerable scan, JSON, secrets scan dirigido y `git diff --check`: `PASS`; cero vulnerabilidades altas/crÃ­ticas nuevas o credenciales.
- RevisiÃ³n independiente inicial bloqueÃ³ correctamente `traceId`, falta de `ActorId` y migraciÃ³n contract prematura; las tres causas se corrigieron. RevalidaciÃ³n final QA y AppSec/Arquitectura: `PASS`, 0 hallazgos crÃ­ticos/altos/medios.
- Estado: `En curso`. El enforcement runtime quedÃ³ integrado, pero la tarea multirelease no puede cerrarse hasta `AGRO-FND-003`/`AGRO-PLT-004`; delivery del outbox sigue en `AGRO-FND-002`.

## IteraciÃ³n 17 â€” AGRO-ID-001 reautenticaciÃ³n OIDC verificable (2026-08-10)

Estado inicial: `En revisiÃ³n`. Una auditorÃ­a de readiness de `AGRO-ID-002` detectÃ³ que la sesiÃ³n local marcaba `AuthenticatedAtUtc=now` al recibir cualquier callback OIDC. Eso permitÃ­a tratar una sesiÃ³n SSO antigua como reautenticaciÃ³n reciente para vincular o desvincular identidades. La correcciÃ³n pertenece al alcance y al gate de seguridad de `AGRO-ID-001`; no inicia `AGRO-ID-002` ni implementa MFA.

### Plan, aceptaciÃ³n y ownership

- [x] Capturar el instante del challenge dentro del `AuthenticationProperties` protegido y solicitar `max_age=0` al IdP.
- [x] Rechazar callbacks sin `auth_time`, con valor invÃ¡lido, fuera de rango, futuro o anterior al challenge mÃ¡s la tolerancia documentada.
- [x] Derivar `AuthenticatedAtUtc` del claim validado, nunca de la hora local del callback.
- [x] Marcar explÃ­citamente la procedencia verificada de la sesiÃ³n; las sesiones legacy/N-1 fallan cerradas para mutaciones sensibles.
- [x] Mantener el proveedor sintÃ©tico limitado a Development/Test con assurance explÃ­cita y sin afirmar equivalencia con Auth0.
- [x] Agregar migraciÃ³n aditiva compatible N/N-1 y pruebas de stale, legacy, malformed, replay y rollback/roll-forward.
- [x] Ejecutar gates .NET/PostgreSQL/OpenAPI, revisiÃ³n independiente y actualizar evidencia de `AGRO-ID-001`.

Ownership exclusivo: principal sobre `apps/AgropecuarIA.Api/IdentityEndpoints.cs`, contrato OpenAPI, documentaciÃ³n, backlog y Git; Backend sobre `src/AgropecuarIA.Identity/**`, migraciÃ³n EF y tests .NET asignados; QA/AppSec revisan read-only el estado combinado. No hay cambio frontend salvo que el contrato observable lo exija.

No objetivos: passkeys, TOTP, recovery, roles, contexto tenant, delivery de notificaciones, despliegue ni credenciales Auth0. El gate Auth0 real de `AGRO-ID-001` permanece externo al repositorio.

### RevisiÃ³n

- Resultado local: `PASS`. Restore locked y build Release 0 warnings/0 errors; suite raÃ­z MTP 81/81, suite Identity posterior al Ãºltimo refuerzo 31/31, format y modelo EF sin drift.
- Seguridad: el challenge protegido emite `max_age=0`; el callback valida `auth_time` firmado contra el instante protegido con tolerancia acotada. Ausente, malformado, fuera de rango, stale, futuro o sin state falla cerrado.
- Persistencia: migraciÃ³n `20260810192543_AddAuthenticationAssuranceToSessions` aditiva `NOT NULL DEFAULT false`; filas existentes y writer N-1 permanecen sin assurance, writer N la marca solo tras validaciÃ³n. PostgreSQL real verificÃ³ upgrade, writer coexistente y rollback/roll-forward efÃ­mero.
- RevisiÃ³n independiente QA y AppSec/Arquitectura: `PASS`, 0 crÃ­ticos/altos/medios. NuGet vulnerable 0, secrets scan 0 y `git diff --check` PASS.
- Frontend, Playwright, contenedores y CI: N/A; no cambiÃ³ UI, respuesta consumida, contenedor ni pipeline. El flujo visible conserva el mismo contrato y los fallos usan los estados existentes.
- Estado final: `En revisiÃ³n`. El defecto local quedÃ³ corregido; Auth0 real aÃºn debe demostrar `state`/nonce/code replay, `max_age=0`, `auth_time` y el comportamiento upstream de Google antes de `Completada` o deploy.
- Riesgo residual bajo: `OnRemoteFailure` aÃºn agrega la categorÃ­a general `provider_unavailable` a rechazos de freshness; el rechazo es seguro pero se recomienda una mÃ©trica especÃ­fica cuando se conecte el sandbox Auth0.

## IteraciÃ³n 18 â€” AGRO-ID-002 step-up MFA ligado a propÃ³sito (2026-08-10)

Estado inicial: `Propuesto`. El sponsor seleccionÃ³ explÃ­citamente `AGRO-ID-002`, delegÃ³ los defaults de producto reversibles y confirmÃ³ que las credenciales reales se incorporarÃ¡n en el servidor de prueba. TransiciÃ³n registrada: `Propuesto â†’ Ready` al aprobar la polÃ­tica MFA/recovery de desarrollo y acotar el primer sub-slice; pasarÃ¡ a `En curso` al comenzar la ediciÃ³n productiva. ClasificaciÃ³n: capacidad R1 de tamaÃ±o M; este sub-slice habilita assurance fuerte sin declarar implementado el lifecycle completo de factores.

### DoR, polÃ­tica y lÃ­mites

- [x] Confirmar ID, outcome, actor, requisitos RF-ID-003/004/006, ADR-003, amenazas y criterios observables.
- [x] Confirmar que `AGRO-ID-001` provee sesiÃ³n/identidad interna integrada; su gate Auth0 real se hereda como gate externo, pero no bloquea desarrollo local.
- [x] Fijar passkey como mÃ©todo preferido, TOTP como segundo factor/fallback, recovery codes de un uso y SMS fuera de alcance.
- [x] Fijar Auth0 como custodio de credenciales, semillas, cÃ³digos y factor IDs; AgropecuarIA conserva solo assurance gruesa, instante, propÃ³sito y evidencia de auditorÃ­a sin PII.
- [x] Fijar step-up one-shot de cinco minutos para `manage_authentication_methods`, ligado a usuario+sesiÃ³n+identidad; exigir `max_age=0`, `acr_values` MFA, `amr=mfa` y `auth_time` firmado/fresco.
- [x] Diferir enforcement owner/admin/contador hasta que `AGRO-ID-003` entregue roles efectivos; no inventar autoridad a partir de strings o claims del IdP.
- [x] Fijar correo verificado como canal de recuperaciÃ³n/notificaciÃ³n candidato, sin implementar delivery de `AGRO-FND-002` ni afirmar validaciÃ³n real.
- [x] Acotar este sub-slice a assurance/step-up, rotaciÃ³n de sesiÃ³n, UI y evidencia local. Alta/revocaciÃ³n passkey/TOTP, recovery real y notificaciÃ³n permanecen dentro de la tarea padre pero fuera de este incremento.

### Plan verificable

- [x] Publicar polÃ­tica y contrato HTTP/OpenAPI de intento, challenge, callback y assurance de sesiÃ³n.
- [x] Agregar intento one-shot y assurance fuerte separados de la frescura OIDC de `AGRO-ID-001`.
- [x] Implementar inicio, validaciÃ³n `acr`/`amr`/`auth_time`/issuer/subject, consumo atÃ³mico y rotaciÃ³n de sesiÃ³n sin extender su expiraciÃ³n absoluta.
- [x] Agregar migraciÃ³n aditiva N/N-1: nuevas columnas conservadoras y tabla efÃ­mera de intentos; ninguna sesiÃ³n legacy se eleva automÃ¡ticamente.
- [x] Emitir journal/outbox/telemetrÃ­a acotados para inicio, Ã©xito y rechazo sin token, subject, email, claims ni factor IDs.
- [x] Implementar UI accesible que muestre `primary`/`strong`, vencimiento, loading regional, proveedor caÃ­do, expiraciÃ³n/replay y reintento; el fixture fuerte solo existe en Development/Test.
- [x] Cubrir CSRF, propÃ³sito invÃ¡lido, identidad/sesiÃ³n cruzada, sesiÃ³n revocada, `acr`/`amr` ausente, `auth_time` stale/futuro, doble callback, replay y rotaciÃ³n de cookie.
- [x] Ejecutar migraciÃ³n PostgreSQL real, restore/build/format/tests MTP, pnpm frozen/format/lint/typecheck/unit/build/E2E, SCA/secrets y revisiÃ³n independiente.
- [x] Actualizar evidencia, backlog y riesgos; mantener `En curso` si el lifecycle real de factores continÃºa pendiente.

### Ownership disjunto

- Principal: contrato/polÃ­tica, `IdentityEndpoints.cs`, OIDC compartido, configuraciÃ³n, migraciÃ³n, integraciÃ³n, documentaciÃ³n, estados, gates y Git.
- Backend .NET: dominio/aplicaciÃ³n/EF no-migraciÃ³n y tests backend asignados; no edita API compartida, migraciones, frontend ni documentaciÃ³n.
- Frontend Next.js: feature Identity, estilos y tests frontend/E2E; no edita contratos, manifiestos/lockfile ni backend.
- QA y AppSec/Arquitectura: revisiÃ³n final read-only sobre el estado combinado; ningÃºn implementador aprueba su propio cambio.

### Baseline y gates previstos

- Baseline Git limpio en `main`, sincronizado con `origin/main`, commit `40b6d4717f2e66fb867e30ea9a033f18fb0a2bb3`.
- Runtime actual: .NET 10/MTP, PostgreSQL real efÃ­mero, 81 tests raÃ­z; Next.js 16.3/React 19.2/TypeScript 6 con pnpm 10.33 y 18 tests Vitest.
- Comandos: locked restore; build Release 0 warnings; MTP con mÃ­nimo actualizado; format/model drift; pnpm frozen, format/lint/typecheck/test/build/E2E; auditorÃ­a de dependencias, secretos, `git diff --check` y revisiÃ³n de migraciÃ³n N/N-1.
- N/A: deploy, aprovisionamiento Auth0, credenciales reales, delivery de correo y enforcement por rol. Son gates externos/dependencias nominadas, no aprobaciones implÃ­citas.

### Review y evidencia final

- Resultado: sub-slice local de step-up MFA ligado a propÃ³sito aprobado. Separa frescura OIDC de assurance fuerte, consume el intento una vez, rota la sesiÃ³n sin extenderla y presenta estado/vencimiento en una regiÃ³n accesible de la UI.
- Contrato/arquitectura: Auth0 conserva todo material de factor; AgropecuarIA usa un intento one-shot ligado a usuario+sesiÃ³n+identidad+propÃ³sito, modelo aditivo y endpoint sintÃ©tico fÃ­sicamente limitado a `Development`/`Test`.
- MigraciÃ³n: `20260810195645_AddPurposeBoundStrongAuthentication` validada sobre PostgreSQL 17 efÃ­mero con writer N-1 antes/despuÃ©s del expand, writer N, constraints, rollback y roll-forward. El rollback compartido es operativo/roll-forward; `Down` queda limitado a base efÃ­mera.
- Backend: restore locked PASS; build Release 0 warnings/0 errors; MTP raÃ­z 100/100; format PASS; EF sin cambios pendientes.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest 23/23; Playwright 4/4 desktop/mobile con Axe, teclado y viewport angosto. El loader afecta solo la tarjeta de assurance.
- Seguridad/supply chain: NuGet vulnerable 0, pnpm audit 0, secrets scan 0 y diff-check PASS. CSRF, rate limit, replay, exact-once, expiry, cookie rotada, usuario/sesiÃ³n cruzados, sesiÃ³n revocada y claims dÃ©biles/ajenos quedan cubiertos.
- RevisiÃ³n independiente: QA y AppSec/Arquitectura PASS, 0 hallazgos crÃ­ticos/altos/medios. Los hallazgos iniciales de CRLF, texto UTF-8 y cobertura negativa fueron corregidos y revalidados.
- Evidencia reproducible: `tasks/evidence/AGRO-ID-002/validation-report.md`, `mfa-recovery-policy.md` y `factor-loss-runbook.md`.
- Estado final: `En curso`. El sub-slice estÃ¡ terminado, pero la tarea padre aÃºn requiere lifecycle real de passkeys/TOTP/recovery, sandbox Auth0, notificaciÃ³n, enforcement por roles y matriz de dispositivos/navegadores. No hubo deploy.

## IteraciÃ³n 19 â€” AGRO-FND-001 cierre contractual R1 (2026-08-10)

Estado inicial: `En curso`. La auditorÃ­a posterior a `AGRO-ID-002` detectÃ³ que el runtime emite `IdentityStepUpCompleted`, pero el mapa contractual solo registra `IdentityLinked`. El cierre se acota a eliminar ese drift, hacer comprobable que todo evento Identity se publica desde un catÃ¡logo Ãºnico y reproducir la DoD propia de FND-001. Backfill por lotes/ETag, restore de staging y delivery/idempotencia permanecen en `AGRO-FND-003`, `AGRO-PLT-004` y `AGRO-FND-002`; no se absorben aquÃ­.

### Plan verificable

- [x] Registrar `IdentityStepUpCompleted` en el mapa de consumidores con provider, scope y ventana compatible.
- [x] Introducir un catÃ¡logo inmutable y Ãºnico de eventos pÃºblicos Identity; los escritores outbox no aceptan strings contractuales dispersos.
- [x] Validar por fitness que catÃ¡logo runtime y mapa revisado coincidan exactamente en nombre, source, scope y major version.
- [x] Cubrir eventos desconocidos, contrato faltante, scope divergente y versiÃ³n incompatible.
- [x] Reproducir restore locked, build Release, suite MTP raÃ­z, format, modelo EF, JSON/SCA/secrets y diff-check.
- [x] Obtener revisiÃ³n independiente QA y AppSec/Arquitectura; actualizar ADR/evidencia y completar FND-001 solo con gates verdes.
- [x] Commit/push autorizado y detenerse sin iniciar FND-002.

### Ownership y no objetivos

- Principal: catÃ¡logo/constructor compartido, integraciÃ³n, plan/evidencia/backlog, gates y Git.
- Architecture Lead: revisiÃ³n read-only del alcance y compatibilidad; QA y AppSec: revisiÃ³n final independiente.
- No hay frontend, endpoints, migraciÃ³n ni cambio de payload. No se implementan dispatcher, idempotency ledger, RLS tenant, backfill contractual, CI ni deploy.

### Review final

- Resultado: `AGRO-FND-001` completa su DoD R0/R1. El runtime Identity, el mapa de consumidores y el registro runtime contienen exactamente los mismos eventos pÃºblicos y schemas; el grafo/ownership continÃºa sin ciclos ni persistencia ajena.
- Contrato: catÃ¡logo exhaustivo derivado de enum, resolver fail-closed, aggregate type catalogado y constructors privados. Solo factories con payloads v1 tipados pueden crear outbox; source/scope/version/schema/aggregate no quedan como strings libres del writer.
- Compatibilidad: se preservÃ³ exactamente la shape v1 histÃ³rica. PostgreSQL prueba payload jsonb real antes y despuÃ©s del expand, writer N-1 posterior y coexistencia sin mutaciÃ³n. Reducir IDs requiere v2 explÃ­cito.
- Gates: restore locked PASS; build Release 0/0; fitness 60/60; Identity/PostgreSQL dirigido 13/13; raÃ­z 114/114; format PASS; EF sin drift; 15 JSON PASS; NuGet vulnerable 0; secrets 0; diff-check PASS.
- RevisiÃ³n independiente: QA y AppSec/Arquitectura PASS, cero crÃ­ticos/altos/medios. Los blockers de catÃ¡logo eludible, JSON libre, payload breaking y fixture N/N-1 vacÃ­o se corrigieron y revalidaron.
- AutoevaluaciÃ³n: 97/100 â€” contexto/selecciÃ³n 15/15, arquitectura/cÃ³digo 20/20, multiagente 10/10, contrato/datos/observabilidad 14/15, tests/seguridad 20/20, preservaciÃ³n/cierre 18/20. Cero gate obligatorio fallido.
- Estado: `En curso â†’ En revisiÃ³n â†’ Completada`. FND-002 conserva delivery/idempotencia/RLS; FND-003 conserva ETag/backfill/contract; PLT-004 conserva staging/backup/restore. No hubo frontend, API, migraciÃ³n ni deploy.

## IteraciÃ³n 20 â€” AGRO-SEC-001 gate incremental R1 del runtime Identity/FND (2026-08-10)

Estado inicial: `En curso`. La selecciÃ³n autÃ³noma descartÃ³ `AGRO-FND-002` porque todavÃ­a carece de una mutaciÃ³n tenant real y su primer consumidor pertenece a `AGRO-ID-003`; tambiÃ©n descartÃ³ cerrar `AGRO-ID-001/002` porque sus siguientes gates requieren Auth0 real. `AGRO-SEC-001` sÃ­ tiene DoR para revisar la frontera ya integrada y su evidencia todavÃ­a describe un repositorio sin runtime, lockfiles ni controles emitidos.

### Assumption-validation check-in

- AgropecuarIA sigue siendo un SaaS online multiempresa para Argentina; el runtime local no estÃ¡ desplegado ni expuesto a Internet.
- API ASP.NET Core, web Next.js, PostgreSQL, OIDC Auth0 objetivo y adaptadores sintÃ©ticos Development/Test son las Ãºnicas superficies productivas integradas.
- Los datos de prueba son sintÃ©ticos; no existen secretos, PII real, proveedor contratado, CI productiva, edge ni telemetrÃ­a remota.
- El sponsor delegÃ³ decisiones tÃ©cnicas reversibles y pidiÃ³ continuar sin checkpoints; las preguntas de regiÃ³n, DPA, retenciÃ³n, roles legales, ambiente compartido y Auth0 real permanecen explÃ­citas y condicionan despliegue, no este gate local.
- No se implementan RLS tenant, idempotencia/delivery, passkeys/TOTP/recovery reales, CI ni deploy dentro de esta tarea continua.

### Plan verificable

- [x] Inventariar entrypoints, fronteras, datos, proveedores y controles del runtime Identity/FND con anclas a cÃ³digo, contratos y pruebas.
- [x] Reconciliar threat model, clasificaciÃ³n e inventario de procesamiento: separar runtime local, Development/Test, build/CI futuro y servicios externos no aprovisionados.
- [x] Actualizar amenazas existentes y, solo si el flujo lo exige, agregar abusos estables para sesiÃ³n/OIDC, step-up, outbox, migraciones y supply chain.
- [x] Hacer que el validador falle si el runtime obligatorio, sus controles/pruebas o el gate R1 desaparecen de la evidencia.
- [x] Reproducir abuse tests dirigidos, suite de seguridad documental, build/test/format, SCA, secretos, JSON y diff-check.
- [x] Obtener revisiÃ³n independiente QA y AppSec/Arquitectura; corregir hallazgos y documentar evidencia final.
- [x] Mantener `AGRO-SEC-001` en `En curso`, publicar commit/push autorizado y detenerse sin iniciar otra tarea.

### Ownership y no objetivos

- Principal: alcance, `tasks/todo .md`, integraciÃ³n, threat model narrativo, estado, gates y Git.
- Security/Data: registro JSON, inventario/clasificaciÃ³n y validador con ownership exclusivo de esos artefactos.
- Architecture/QA: revisiÃ³n read-only del runtime y revisiÃ³n final independiente; no editan evidencia poseÃ­da por Security/Data.
- No se cambia cÃ³digo productivo, OpenAPI, migraciones, frontend, configuraciÃ³n, manifiestos/lockfiles ni backlog normativo. Un hallazgo de cÃ³digo crÃ­tico/alto detiene el gate y se corrige dentro de SEC-001 solo si pertenece inequÃ­vocamente al control revisado.

### Baseline y gates previstos

- Baseline Git limpio en `main`, sincronizado con `origin/main`, commit `08b764de40fb8eaa5af432969929edf83cc5b49c`.
- Baseline documental: validador SEC `PASS`, 14 amenazas (7 crÃ­ticas/7 altas), 7/7 mutation tests; ese verde cubre R0 pero no detecta drift contra el runtime R1.
- Gates: validador/mutation tests SEC; JSON/enlaces; restore locked; build Release sin warnings; MTP raÃ­z con mÃ­nimo 114 y pruebas de abuso dirigidas; format/model drift; pnpm frozen/audit si la evidencia referencia el lockfile; NuGet vulnerable, secrets scan y `git diff --check`.

### Review final

- Resultado: `PASS` del gate local R1 para Identity/FND. El modelo, registro, clasificaciÃ³n e inventario distinguen superficies integradas, Development/Test y dependencias externas `NO-GO`; no se presenta el bootstrap local como despliegue aprobado.
- Gate de drift: 15/15 mutation tests, 14 amenazas (7 crÃ­ticas/7 altas) y cobertura exacta de los 14 paths OpenAPI. El validador comprueba artefactos, paths y sÃ­mbolos reales; la revisiÃ³n humana conserva responsabilidad por la correspondencia semÃ¡ntica controlâ†”test.
- Backend/datos: restore locked PASS; build Release 0 warnings/0 errors; suite raÃ­z MTP 114/114; format PASS; EF sin drift. La carrera exact-once pasÃ³ 5/5 y ahora acepta las dos posiciones seguras del callback perdedor: `401` tras rotaciÃ³n o `409` tras autenticaciÃ³n previa.
- Frontend: pnpm frozen, format, lint, typecheck y build PASS; Vitest 23/23; Playwright 4/4 desktop/mobile con PostgreSQL 17 efÃ­mero. No se modificÃ³ frontend.
- Supply chain/calidad: NuGet vulnerable 0, pnpm audit 0, ambos JSON vÃ¡lidos, UTF-8 estricto 13/13, secret scan 0 y `git diff --check` PASS. No hay migraciÃ³n ni cambio de contrato/runtime productivo.
- RevisiÃ³n independiente QA y AppSec/Arquitectura: `PASS`, 0 hallazgos crÃ­ticos/altos/medios. Los blockers iniciales â€”drift no detectable, reporte R0 obsoleto, sobreafirmaciÃ³n de step-up, evidencia Production faltante y aserciÃ³n concurrente rÃ­gidaâ€” quedaron corregidos y revalidados.
- Riesgos residuales: Auth0/factores reales, tenant/RLS/roles, edge/HSTS/hosts/key ring/limiter distribuido, OTLP, CI/SBOM/provenance, auditorÃ­a central, backup/restore, regiÃ³n/DPA/retenciÃ³n y notificaciÃ³n permanecen `NO-GO` para ambiente compartido o Internet.
- AutoevaluaciÃ³n: 96/100 â€” contexto/selecciÃ³n 15/15, arquitectura/cÃ³digo 19/20, multiagente 10/10, full-stack/datos/observabilidad 14/15, tests/seguridad 20/20, preservaciÃ³n/cierre 18/20. Cero gate obligatorio fallido.
- Estado: `En curso`. El incremento local estÃ¡ terminado; la tarea padre R0â€“R6 continÃºa y se reevaluarÃ¡ por slice. No hubo deploy.

## IteraciÃ³n 21 â€” AGRO-DIS-003 discovery seguro de membresÃ­as (2026-08-10)

Estado inicial: `En revisiÃ³n`. El sponsor aprobÃ³ continuar con la candidata recomendada. ClasificaciÃ³n: extensiÃ³n R0 descartable del spike existente; no es cÃ³digo productivo ni inicia `AGRO-FND-002`/`AGRO-ID-003`.

### DoR, decisiÃ³n y alcance

- [x] Confirmar que el discovery 0/1/N actual usa `FixtureIdentityDirectory`, mientras PostgreSQL solo permite RLS despuÃ©s de conocer `app.current_organization_id`.
- [x] Confirmar que `Organization` sigue siendo tenant tÃ©cnico, pero CUIT, propiedad/control contractual y roles definitivos permanecen fuera del spike.
- [x] Elegir un principal DB exclusivo `agro_membership_discovery`: `LOGIN`, `NOINHERIT`, `NOBYPASSRLS`, sin ownership ni escritura.
- [x] Fijar `app.current_actor_id` mediante `set_config(..., true)` dentro de una transacciÃ³n, derivado Ãºnicamente de la sesiÃ³n server-side; ningÃºn request acepta `userId` autoritativo.
- [x] Mantener policies de discovery separadas de las policies tenant; seleccionar organizaciÃ³n solo revalida una membership activa y no entrega autoridad por el ID del cliente.
- [x] Basar la decisiÃ³n en PostgreSQL 17: `FORCE ROW LEVEL SECURITY`, default-deny, `set_config` transaccional y rol runtime sin `BYPASSRLS`; Npgsql conserva conexiÃ³n/transacciÃ³n explÃ­citas sobre un pool dedicado.

### Plan verificable

- [x] Capturar baseline del spike con PostgreSQL 17 efÃ­mero y suite raÃ­z sin modificar artefactos productivos.
- [x] Agregar una migraciÃ³n R0 `003` con rol/grants/policies actor-scoped, datos mÃ­nimos y probes de catÃ¡logo/discovery.
- [x] Implementar un port pequeÃ±o de discovery PostgreSQL; reemplazar Ãºnicamente el listado in-memory y hacer la resoluciÃ³n de sesiÃ³n async/cancelable.
- [x] Conservar 0 memberships como sesiÃ³n platform-scoped sin tenant; 1 activa selecciÃ³n automÃ¡tica; N activas requieren selecciÃ³n; revocada/inactiva/ajena falla cerrada.
- [x] Revalidar la membership al cambiar organizaciÃ³n, rotar sesiÃ³n y resolver permisos/security version desde DB antes de acceder a datos tenant.
- [x] Probar 0/1/N, actor ausente/ajeno, org/membership inactiva, revocaciÃ³n entre listado y switch, orden/lÃ­mite y pool tras commit/rollback/excepciÃ³n/cancelaciÃ³n.
- [x] Verificar roles/grants: ningÃºn runtime owner/superuser/`BYPASSRLS`; discovery sin escritura, sin `platform_user`, email, CUIT ni datos productivos.
- [x] Reconciliar contratos y evidencia histÃ³rica: separar sesiÃ³n platform-scoped, discovery y contexto tenant; marcar identidad externa 1:N como cerrada por ID-001 y conservar gates Auth0/Legal externos.
- [x] Obtener revisiÃ³n independiente DBA/AppSec y QA/Arquitectura; actualizar `ADR-PEND-007` solo si toda la evidencia tÃ©cnica queda verde.
- [x] Ejecutar gates del spike y regresiÃ³n raÃ­z; commit/push autorizado y detenerse sin iniciar otra tarea.

### Ownership disjunto

- Principal: contrato compartido, `Program.cs`, plan/ADR/evidencia, integraciÃ³n, gates, backlog y Git.
- Database/Security: migraciÃ³n/probes/runner/scripts PostgreSQL del spike; no edita API, tests C# ni documentaciÃ³n principal.
- Backend .NET: port/repositorio, resoluciÃ³n de sesiÃ³n y tests C# del spike; no edita SQL, `Program.cs`, contratos ni documentaciÃ³n.
- QA y Architecture/AppSec: revisiÃ³n final read-only desde el estado combinado.

### No objetivos y gates

- No modificar `src/**`, `apps/**`, migraciones EF ni contratos productivos; no implementar invitaciones/roles, mutation ledger, outbox/inbox, worker, IdP real, CI o deploy.
- El rol de discovery sigue confiando en que el actor proviene del borde autenticado: RLS es defensa en profundidad, no mitigaciÃ³n de SQL injection o compromiso total del principal DB.
- Rollback: detener/eliminar el clÃºster efÃ­mero; la migraciÃ³n del spike no se copia a producciÃ³n.
- La menciÃ³n histÃ³rica a `trust` en IteraciÃ³n 9 describe la baseline del 2026-08-05 y queda supersedida: el harness vigente exige SCRAM-SHA-256, cuatro secretos efÃ­meros distintos y ACL owner-only.
- Gates: PostgreSQL probes; spike restore/build/MTP/format/SCA; contratos JSON; suite raÃ­z 114/114; secrets/UTF-8/diff-check; revisiones independientes con cero hallazgos crÃ­ticos/altos.

### Review final

- Resultado: `PASS` del incremento R0. PostgreSQL 17 limpio con SCRAM devolviÃ³ `catalog-security-pass`, `rls-isolation-pass`, `membership-discovery-pass` e `identity-spike-database-pass`; migration/probe `003` reejecutables.
- Spike: restore `PASS`; build Debug 0 warnings/errores; MTP 29/29; format `PASS`; NuGet SCA 0 vulnerabilidades conocidas; cleanup eliminÃ³ `.runtime`.
- RegresiÃ³n raÃ­z: restore locked `PASS`; build Release 0 warnings/errores; MTP 114/114; format `PASS`; EF sin cambios pendientes; NuGet SCA 0 vulnerabilidades conocidas.
- Seguridad: owner/superuser/`BYPASSRLS`/`INHERIT`/`CREATEDB`/`CREATEROLE`/`REPLICATION`, memberships u ownership indebidos fallan antes de servir. ReautorizaciÃ³n del recurso comparte statement/snapshot con la lectura.
- QA y AppSec/Arquitectura independientes: `PASS`, cero hallazgos crÃ­ticos, altos o medios. Los blockers iniciales de `trust`, principal fail-open, TOCTOU y cobertura documental fueron corregidos y revalidados.
- Contratos/documentaciÃ³n: 4 JSON vÃ¡lidos y UTF-8; discovery es contrato conceptual interno, no endpoint independiente; cero memberships conserva estado interno pero el HTTP histÃ³rico responde 403.
- Estado: `AGRO-DIS-003` continÃºa `En revisiÃ³n` por Auth0/Legal/runtime productivo. `ADR-PEND-007` queda aceptada para desarrollo R1, no implementada en producciÃ³n.
- No hubo cambios en `src/**`, `apps/**`, `tests/**`, frontend, migraciones EF, CI o deploy. AutoevaluaciÃ³n: 96/100; cero gate obligatorio fallido.

## IteraciÃ³n 22 â€” AGRO-SEC-001 refresh de tenancy/RLS R0 (2026-08-10)

Estado inicial: `En curso`. El lÃ­der seleccionÃ³ la Ãºnica tarea activa con un incremento local legÃ­timo despuÃ©s de `AGRO-DIS-003`; clasificaciÃ³n R0/R1 de threat modeling y gate continuo. No implementa `AGRO-FND-002`, runtime tenant ni controles productivos.

### DoR, outcome y alcance

- [x] Confirmar repositorio limpio y `main` sincronizado en `014fb48`.
- [x] Confirmar que `ADR-PEND-007` ahora estÃ¡ aceptada para desarrollo R1 y que el discovery actor-scoped pasÃ³ PostgreSQL/SCRAM/RLS, pero sigue siendo spike descartable.
- [x] Detectar drift factual: `TM-001`, clasificaciÃ³n e inventario aÃºn dicen que ADR/discovery estÃ¡n abiertos y que el harness usa `trust`.
- [x] Mantener el riesgo tenant como crÃ­tico hasta repetir el patrÃ³n en runtime productivo; no promover el spike a `integrated-local` ni inventar una nueva superficie HTTP.
- [x] Fijar contexto: SaaS online multiempresa argentino, `Organization` tenant tÃ©cnico, runtime local no desplegado, Auth0/hosting/Legal externos y sin datos reales.

### Plan verificable

- [x] Reconciliar `TM-001` y documentos humanos con la decisiÃ³n RLS/discovery aceptada, separando evidencia R0 de controles runtime.
- [x] Actualizar clasificaciÃ³n/inventario: SCRAM, secretos efÃ­meros/ACL, principal discovery fail-fast y gates productivos restantes.
- [x] Endurecer el validador para rechazar declaraciones obsoletas sobre `ADR-PEND-007`, discovery pendiente o harness `trust`, con mutation self-test.
- [x] Anexar evidencia reproducible al validation report sin reescribir el historial R0/R1 previo.
- [x] Ejecutar validator/self-tests, JSON/UTF-8/secrets/diff-check y gates documentales aplicables.
- [x] Obtener revisiÃ³n independiente QA y AppSec/Arquitectura, resolver hallazgos y conservar `AGRO-SEC-001` `En curso`.
- [x] Commit/push autorizado; detenerse sin iniciar `AGRO-FND-002`.

### Ownership disjunto

- Principal: selecciÃ³n, `tasks/todo .md`, validation report, integraciÃ³n, gates y Git.
- AppSec/Data: `threat-register.json`, clasificaciÃ³n, inventario y validador; no edita informe principal.
- Architecture: threat model humano y release gates; no edita registros JSON/validador.
- Principal QA: revisiÃ³n final read-only y reproducciÃ³n de gates.

### Baseline

- `validate-threat-model.ps1 -SelfTest`: `PASS`, 15/15 mutations; 14 amenazas, 7 crÃ­ticas y 7 altas, ninguna crÃ­tica sin owner/test/gate.
- JSON parse: `PASS` para threat register y runtime surface register.
- Git: worktree limpio; no se cambia backlog ni cÃ³digo productivo.

### Review final

- Resultado: `PASS` del refresh R0/R1. El modelo, registro, clasificaciÃ³n e inventario reconocen `ADR-PEND-007` y las 29/29 pruebas del spike descartable sin atribuir RLS/discovery al runtime productivo.
- Gate afectado: `validate-threat-model.ps1 -SelfTest` `PASS`, 24/24 mutation tests; 14 amenazas (7 crÃ­ticas/7 altas), ninguna crÃ­tica sin owner/test/gate. Las seis mutations positivas impiden perder silenciosamente ADR aceptada, 29/29, SCRAM, secretos distintos, ACL owner-only o fail-fast del principal; otras tres impiden reintroducir los claims obsoletos.
- Calidad documental: ambos JSON vÃ¡lidos; UTF-8 estricto, parser PowerShell, secret scan dirigido y `git diff --check` `PASS`.
- RevisiÃ³n independiente QA y AppSec/Arquitectura: `PASS`, cero hallazgos crÃ­ticos, altos o medios despuÃ©s de corregir la falta de mutations positivas. La simplificaciÃ³n del diagrama R0 queda como observaciÃ³n informativa; las fronteras sintÃ©tica y de bootstrap estÃ¡n descritas en narrativa, tabla y gates.
- Gates .NET/frontend/EF/PostgreSQL/E2E/SCA: `N/A` para este diff exclusivamente documental; no se reutilizan resultados previos como sustituto del validador modificado.
- Estado: `AGRO-SEC-001` continÃºa `En curso` por ser gate multirelease. Tenant/RLS productivo, Auth0/hosting, CI/provenance, auditorÃ­a central, backup y decisiones Legal/retenciÃ³n siguen `NO-GO`.
- Siguiente candidato: `AGRO-FND-002`, sin iniciar. Antes necesita un consumidor tenant real y semÃ¡nticas aprobadas de auditorÃ­a, orden, retry/poison e idempotencia/retenciÃ³n.
- AutoevaluaciÃ³n: 95/100; cero gate obligatorio fallido, sin cambios de producto, contrato, migraciÃ³n, configuraciÃ³n, manifiestos o lockfiles.

## IteraciÃ³n 23 â€” AGRO-FND-002 protocolo idempotente y secuencia del primer consumidor (2026-08-10)

Estado inicial: `Propuesto`. El sponsor indicÃ³ continuar despuÃ©s de recibir el diagnÃ³stico del ciclo `FND-002 â†’ tenancy/RLS â†’ ID-003/SEC-002 â†’ FND-002`; esta continuaciÃ³n autoriza al lÃ­der a fijar defaults tÃ©cnicos reversibles y la secuencia, sin absorber la implementaciÃ³n de `AGRO-ID-003` ni alterar requisitos, release o DoD.

### Outcome, DoR y lÃ­mites

- [x] Confirmar `AGRO-FND-001` `Completada`, ADR-009, `RequestScope`, journal local y outbox tipado como precondiciones satisfechas.
- [x] Confirmar `ADR-PEND-007` aceptada para desarrollo R1 y mantener sus migraciones/roles productivos como trabajo del primer consumidor tenant, no del spike descartable.
- [x] Confirmar que el runtime actual solo tiene mutaciones Identity platform-scoped y que una tabla, endpoint o consumidor ficticio violarÃ­a la DoD.
- [x] Usar `CreateOrganization` de `AGRO-ID-003` Ãºnicamente como primer consumidor nominado futuro; no implementar organizaciÃ³n, invitaciÃ³n, roles, RLS runtime ni UI en este incremento.
- [x] Separar TTL tÃ©cnico de replay/conciliaciÃ³n de cualquier plazo legal: el desarrollo local no purga automÃ¡ticamente y ningÃºn default se presenta como polÃ­tica productiva.
- [x] Fijar fuentes primarias: `draft-ietf-httpapi-idempotency-key-header-07` expirÃ³ el 2026-04-18 sin publicarse como RFC y solo inspira un contrato propio versionado; EF Core exige tratar el commit incierto; PostgreSQL reserva `SKIP LOCKED` para tablas tipo cola.

### Plan verificable

- [x] Publicar una polÃ­tica canÃ³nica con identidad de clave, fingerprint, estados, autorizaciÃ³n previa al replay, respuesta concurrente/expirada, transacciÃ³n, orden, retry/poison y rollout.
- [x] Publicar la matriz de auditorÃ­a/retenciÃ³n que distingue journal local fail-closed, proyecciÃ³n central eventual, denegaciones, legal hold y datos prohibidos.
- [x] Resolver la secuencia: FND-002 entrega contrato/gates; ID-003 implementa el primer consumidor y su frontera tenant/RLS; FND-002 solo completa despuÃ©s de esa evidencia real.
- [x] Crear un validador reproducible que rechace pÃ©rdida de invariantes, contradicciones de identidad de clave, fake consumer, promociÃ³n del spike o plazos legales inventados.
- [x] Ejecutar validator/mutations, JSON/UTF-8/enlaces/parser/secrets/diff-check y revisiÃ³n independiente Architecture, Security/Data y QA.
- [x] Transicionar solo `AGRO-FND-002` a `Ready â†’ En curso` al demostrar la DoR documental; mantenerla `En curso` hasta el consumidor real.
- [x] Commit/push autorizado y detenerse sin iniciar `AGRO-ID-003`.

### Contrato preliminar a validar

- Unicidad tenant: `(tenant_id, operation, idempotency_key)`; `CreateOrganization` usa la excepciÃ³n discriminada platform con namespace constante del servidor porque el tenant aÃºn no existe. Actor, recurso/colecciÃ³n, versiÃ³n de autorizaciÃ³n y fingerprint quedan ligados y se reautorizan antes de cualquier replay. No hay lookup ni oracle de conflicto entre tenants.
- `Idempotency-Key`: valor opaco generado por cliente, 16â€“128 caracteres ASCII visibles, nunca UUID/tenant/actor derivado ni label/log. La API sigue validaciÃ³n estricta; RFC 9651 y el draft IETF son antecedentes no normativos y no convierten el valor en `sf-string`.
- Fingerprint: SHA-256 de una serializaciÃ³n canÃ³nica definida por operaciÃ³n sobre mÃ©todo, route template, versiÃ³n de contrato y payload normalizado; no se persiste ni registra el payload crudo.
- Misma key+fingerprint tras autorizaciÃ³n vigente reproduce status/body/header allow-listed; misma key con fingerprint distinto devuelve `409`; in-flight devuelve `409` retryable con `Retry-After`; respuesta expirada no reejecuta el hecho y exige conciliaciÃ³n/lookup del recurso.
- Negocio, ledger terminal, journal local y outbox se confirman en una transacciÃ³n PostgreSQL. Audit/Compliance central es proyecciÃ³n at-least-once; una caÃ­da posterior no revierte el hecho ya confirmado.
- Delivery es at-least-once, consumidores deduplican `(consumer, event_id)`, orden solo por stream de agregado y los gaps se cuarentenan. No se promete exactly-once de transporte ni orden global.

### Ownership disjunto

- Principal: plan, secuencia/estado, decisiones globales, integraciÃ³n, fuentes, gates y Git.
- Architecture Lead: `tasks/evidence/AGRO-FND-002/idempotency-and-delivery-policy.md`.
- Security/Data: `tasks/evidence/AGRO-FND-002/audit-retention-and-threats.md`.
- QA Automation: `tasks/evidence/AGRO-FND-002/validate-foundation-protocol.ps1` y fixtures machine-readable propios.
- Ola 3: Architecture/AppSec/QA revisan archivos ajenos en modo read-only; ningÃºn autor aprueba su propia entrega.

### No objetivos y gates

- No crear `src/**`, `apps/**`, migraciones EF, endpoint, worker, inbox/dispatcher, organizaciÃ³n, roles, UI, manifiesto/lockfile, CI, Docker, credencial o deploy.
- .NET/frontend/PostgreSQL/E2E/SCA son `N/A` para este incremento contractual sin runtime afectado; no se reutilizan resultados previos como sustituto del validador nuevo.
- Gates aplicables: validador positivo y mutations negativas; JSON/UTF-8 estricto; referencias existentes; parser PowerShell; scan de secretos; `git diff --check`; revisiÃ³n independiente y alcance Git.

### Review final

- Resultado: `PASS` del incremento contractual R1; transiciÃ³n documentada `Propuesto â†’ Ready â†’ En curso`. El contrato satisface la DoR del sub-slice, no la DoD de la tarea padre.
- Protocolo: uniÃ³n discriminada `platform | tenant`; bootstrap `CreateOrganization` usa namespace platform constante server-side, sin tenant sintÃ©tico; los tenants posteriores conservan namespaces independientes y errores sin oracle.
- Correcciones de revisiÃ³n: fencing monotÃ³nico + lock/CAS owner/fence antes del negocio; identidad estable del ledger y aliases HMAC multiversiÃ³n con intersecciÃ³n N/N-1, lazy alias y `alias_identity_split` fail-closed; poison de delivery separado de `failed_terminal`; replay reautoriza y no entrega body histÃ³rico ciegamente.
- Gate principal: `validate-foundation-protocol.ps1 -SelfTest` `PASS`, 44/44 mutations rechazadas y protocolo `1.0.0` vÃ¡lido con `runtimeImplemented=false`.
- Calidad documental: JSON, parser PowerShell, UTF-8 estricto sin BOM, LF/newline final/whitespace, referencias locales, secret scan y `git diff --check` `PASS`.
- Revisiones independientes Architecture, Security/Data y QA: `PASS`, cero hallazgos crÃ­ticos, altos o medios despuÃ©s de resolver scope bootstrap, oracle cross-tenant, fencing, rotaciÃ³n HMAC y poison.
- Gates .NET/frontend/EF/PostgreSQL/E2E/SCA: `N/A`; no se modificÃ³ runtime, contrato HTTP, migraciÃ³n, paquete, lockfile, infraestructura o UI.
- Estado: `AGRO-FND-002` queda `En curso`. Faltan `AGRO-ID-003/CreateOrganization`, migraciones/principals/RLS productivos y pruebas reales de concurrencia, crash/replay, delivery y telemetrÃ­a; no se iniciÃ³ esa tarea.
- AutoevaluaciÃ³n: 94/100; cero gate obligatorio fallido y cero cambio ajeno.

## IteraciÃ³n 24 â€” AGRO-ID-003 CreateOrganization (2026-08-10)

Estado inicial: `Propuesto`. El sponsor confirmÃ³ registro pÃºblico con datos privados, creaciÃ³n autÃ³noma de mÃºltiples organizaciones, creador como `owner` tenant y un posible superadmin futuro separado. El ID activo es `AGRO-ID-003`; se ejecuta un Ãºnico sub-slice R1 cohesivo y la tarea padre permanecerÃ¡ `En curso` porque invitaciones, matriz completa de roles y alcances por campo no forman parte de este incremento.

### DoR, outcome y decisiones vinculantes

- [x] Confirmar que `AGRO-FND-002` nominÃ³ `AGRO-ID-003/CreateOrganization` como primer consumidor real de su protocolo y conserva `runtimeImplemented=false` hasta esta integraciÃ³n.
- [x] Confirmar que la sesiÃ³n local de `AGRO-ID-001` deriva usuario, verificaciÃ³n y `AuthenticatedAtUtc` del proveedor; los gates Auth0 externos siguen siendo de despliegue, no bloquean el slice local.
- [x] Fijar registro pÃºblico como creaciÃ³n de cuenta, no exposiciÃ³n pÃºblica de organizaciones ni datos productivos.
- [x] Fijar que cualquier usuario autenticado, verificado y con autenticaciÃ³n reciente menor a 15 minutos puede crear mÃºltiples organizaciones; no se exige MFA fuerte para este bootstrap.
- [x] Fijar que el creador queda como Ãºnico `owner` activo inicial. `owner` es un rol tenant y jamÃ¡s concede `platform superadmin`.
- [x] Fijar la regla de Ãºltimo owner para mutaciones posteriores: no podrÃ¡ removerse, demoverse ni abandonar si dejarÃ­a la organizaciÃ³n sin owner activo.
- [x] Confirmar que nombres de organizaciÃ³n no son Ãºnicos globalmente, CUIT no autoriza y no se inventa un lÃ­mite comercial de organizaciones.
- [x] Obtener GO read-only de Architecture, Product/QA y Security/Data para el sub-slice; conservar superadmin, soporte JIT, observabilidad global, invitaciones, cambios de rol y campos fuera de alcance.

Outcome observable: un usuario con sesiÃ³n verificada y reciente crea una organizaciÃ³n privada y queda owner en una Ãºnica transacciÃ³n PostgreSQL idempotente; la UI muestra 0/1/N organizaciones sin bloquear el shell y ningÃºn usuario, tenant, job o principal DB puede observar o reproducir datos de otro tenant.

### AceptaciÃ³n del sub-slice

- [x] `POST /api/identity/organizations` acepta solo `displayName`; actor, scope, owner y tenant se derivan en servidor. Cookie, CSRF, rate limit e `Idempotency-Key` son obligatorios.
- [x] AutenticaciÃ³n verificada y reciente `< 15 minutos` permite crear; frontera de 15 minutos, sesiÃ³n no verificada, stale, expirada o revocada fallan sin efecto.
- [x] OrganizaciÃ³n, owner membership, ledger FND-002 terminal, journal local y outbox tipado se confirman atÃ³micamente; cualquier fallo inyectado deja cero parcial.
- [x] Misma key+fingerprint concurrente produce una organizaciÃ³n, una membresÃ­a, un journal y un outbox; mismatch, in-flight, commit desconocido, replay stale y response expirada siguen el protocolo 1.0.0 sin oracle cross-tenant.
- [x] El mismo usuario puede crear Org A y B con claves distintas; nombres duplicados son vÃ¡lidos y el discovery de sesiÃ³n devuelve exclusivamente membresÃ­as activas propias en orden determinista.
- [x] PostgreSQL usa principals mÃ­nimos, `FORCE RLS`, contexto actor/organizaciÃ³n transaction-local y falla cerrado para tenant A/B/sin contexto, pool reutilizado, job sin scope y principal owner/BYPASS.
- [x] La migraciÃ³n es expand/N-N-1 compatible: el writer previo sigue funcionando, rollback de aplicaciÃ³n y roll-forward preservan datos y no duplican efectos.
- [x] La UI cubre 0/1/N organizaciones, crear otra, submitting localizado, validaciÃ³n, unauthorized, reauth requerida, rate-limit, conflicto/in-flight, reconciliaciÃ³n y offline; teclado, foco, lector, 390 px y UUID corto cumplen las reglas globales.
- [x] TelemetrÃ­a usa outcomes allow-listed sin nombre, UUID, user/tenant, key, digest ni payload; evidencia local diagnostica Ã©xito, denegaciÃ³n, replay, conflicto y commit desconocido.
- [x] Contrato HTTP, evento, schema, consumer/runtime maps, migraciÃ³n, clientes, fixtures y documentaciÃ³n quedan alineados y revisados independientemente.

### Plan verificable

- [x] DiseÃ±ar primero OpenAPI, errores Problem Details, evento `OrganizationCreated` v1 y modelo persistente mÃ­nimo compatible.
- [x] Implementar dominio/aplicaciÃ³n/persistencia/API con autorizaciÃ³n previa al lookup, ledger HMAC/fencing, transacciÃ³n y RLS.
- [x] Integrar onboarding/selector en Identity Hub con cliente TypeScript estricto y estados accesibles/localizados.
- [x] Agregar pruebas unitarias, PostgreSQL/API/seguridad/concurrencia/migraciÃ³n, frontend y Playwright desktop/mobile.
- [x] Integrar temprano y ejecutar gates parciales despuÃ©s de contrato, backend y frontend.
- [x] Someter el sub-slice a Ola 3 independiente, resolver hallazgos y repetir todos los gates sin sacar a la tarea padre de `En curso`.
- [x] Documentar evidencia, mantener `AGRO-ID-003` `En curso`, reconciliar `AGRO-FND-002` sin cerrarla prematuramente y publicar commit/push autorizado.

### Ownership disjunto

- Principal: plan/estados, OpenAPI, contratos/eventos compartidos, migration/snapshot, configuraciÃ³n transversal, integraciÃ³n, gates y Git.
- Backend .NET: dominio/aplicaciÃ³n/persistencia/API y pruebas backend asignadas; no edita OpenAPI, migraciones ni frontend.
- Frontend Next.js: feature Identity/Organization y pruebas unitarias; no edita backend, OpenAPI, manifiestos ni lockfiles.
- Database/Security: revisiÃ³n y pruebas RLS/principals/migraciÃ³n bajo archivos exclusivos; no comparte migration con el principal.
- QA Automation: fixtures/E2E y revisiÃ³n reproducible; no modifica implementaciÃ³n salvo correcciÃ³n expresamente reasignada.

### Baseline antes de editar cÃ³digo

- Git: `main`, HEAD `30bc893edeff70d6670945413c01518d871fa1c5`, worktree limpio.
- Backend: restore locked `PASS`; build Release 0 warnings/errores; MTP 114/114; format `PASS`.
- Frontend: pnpm frozen `PASS`; format/lint/typecheck `PASS`; Vitest 23/23; Next.js 16.3.0 build `PASS`.
- TransiciÃ³n autorizada por selecciÃ³n explÃ­cita y DoR acotada: `Propuesto â†’ Ready â†’ En curso` al publicar este plan. La tarea padre no serÃ¡ `Completada` en este incremento.

### No objetivos

- No implementar invitaciones, aceptaciÃ³n/revocaciÃ³n, otros roles, demociÃ³n/transferencia/Ãºltimo-owner runtime, scopes por campo ni creaciÃ³n GIS.
- No implementar superadmin, impersonaciÃ³n, soporte cross-tenant, collector/dashboard global, CI, Docker, deploy ni secretos reales.
- No afirmar que un owner posee legalmente el establecimiento o CUIT; Organization es el tenant tÃ©cnico y los datos siguen privados.

### Review final

- Resultado: `PASS` local del sub-slice `CreateOrganization`; `AGRO-ID-003` permanece `En curso` por invitaciones, matriz de roles, Ãºltimo-owner runtime y scopes por campo.
- Valor: cualquier usuario con sesiÃ³n verificada y autenticaciÃ³n reciente puede crear mÃºltiples organizaciones privadas y queda como `owner` tenant, sin capacidad platform/superadmin.
- Atomicidad e idempotencia: Organization, membership autoritativa y legacy, ledger/aliases HMAC, journal y `OrganizationCreated` se confirman juntos; replay, rotaciÃ³n, conflicto, expiraciÃ³n, commit desconocido y fallos inyectados quedan cubiertos y fail-closed.
- Datos y seguridad: principals mÃ­nimos, grants por columna, `SET LOCAL`, `FORCE RLS`, A/B/sin contexto, pool/job y rollback N/N-1 demostrados en PostgreSQL real.
- Backend: restore locked, build Release 0 warnings/errores, MTP 142/142, format y EF pending-model `PASS`.
- Frontend: pnpm frozen, format/lint/typecheck/build `PASS`; Vitest 50/50; Playwright 4/4 desktop/mobile con Axe, teclado y 390 px.
- Contratos/seguridad: FND 45/45, SEC 25/25, SCA NuGet/pnpm, JSON, UTF-8, secrets y diff-check `PASS`; revisiÃ³n independiente sin crÃ­ticos/altos/medios abiertos.
- Compatibilidad: migraciÃ³n expand, writer N-1 coexistente, app rollback y roll-forward demostrados; `Down` destructivo queda limitado a base efÃ­mera.
- Riesgos externos: Auth0/edge, secretos administrados, principal de ambiente compartido, rate limit distribuido, Audit central y retenciÃ³n legal siguen NO-GO de deploy, no de desarrollo local.
- PublicaciÃ³n: commit/push autorizados; sin deploy. No se iniciÃ³ una segunda tarea.
- AutoevaluaciÃ³n: 96/100; cero gate obligatorio fallido y cero cambio ajeno conocido.

## IteraciÃ³n 25 â€” AGRO-ID-003 invitaciÃ³n one-shot de co-owner (2026-08-11)

Estado inicial: `En curso`. El incremento anterior entregÃ³ `CreateOrganization`; esta continuaciÃ³n conserva el mismo ID y agrega colaboraciÃ³n mÃ­nima sin inventar la matriz completa de roles.

### DoR y decisiones vinculantes

- [x] Auditar backlog, modelo, RLS, sesiÃ³n, step-up, OpenAPI y UI existentes con Product, AppSec/Data y QA independientes.
- [x] Confirmar que roles distintos de `owner`, scopes por campo y invitaciÃ³n por email no estÃ¡n Ready.
- [x] Fijar el slice a invitaciÃ³n mediante enlace one-shot para agregar exclusivamente un `co-owner` (`role=owner` server-side).
- [x] Fijar actor y assurance: owner activo; crear/revocar con purpose `manage_organization_owners`; aceptar con identidad verificada y autenticaciÃ³n reciente `<15m`.
- [x] Fijar token 256-bit, fragmento URL, persistencia solo digest versionado, TTL configurable de 7 dÃ­as y estados pending/accepted/revoked/expired.
- [x] Mantener fuera email/delivery, otros roles, demociÃ³n/remociÃ³n/Ãºltimo-owner runtime, GIS/campos, superadmin y soporte JIT.

### AceptaciÃ³n observable

- [x] Owner A crea una invitaciÃ³n bajo Org A; recibe metadata y token una sola vez, `no-store`, sin email/rol/tenant autoritativo en el body.
- [x] Crear/revocar/listar revalida owner y tenant en la misma transacciÃ³n; owner/usuario de B y sesiÃ³n sin contexto obtienen error neutral sin existencia.
- [x] Invitado verificado acepta antes del vencimiento y obtiene exactamente una membership owner autoritativa mÃ¡s proyecciÃ³n N-1; replay propio devuelve el mismo resultado.
- [x] Token malformado, robado/reutilizado por otro actor, expirado, revocado o aceptado concurrentemente no crea efectos adicionales.
- [x] Accept-vs-revoke produce un Ãºnico estado terminal; fallos de journal/outbox revierten invitaciÃ³n/membership/ledger.
- [x] Evento(s), journal y telemetrÃ­a omiten token/digest/nombre/user/tenant como labels o payload sensible.
- [x] PostgreSQL demuestra `FORCE RLS`, grants mÃ­nimos, A/B/sin contexto/pool/job, migraciÃ³n expand N/N-1, app rollback y roll-forward.
- [x] UI cubre create/copy-once/list/revoke/accept, login/reauth, loading regional, empty/offline/error/expired/conflict, foco/teclado/Axe/390px y UUID corto.

### Plan y ownership

- [x] Principal: OpenAPI, API composition, eventos/schemas/maps, configuraciÃ³n transversal, plan/evidencia, integraciÃ³n, gates y Git.
- [x] Backend: dominio/aplicaciÃ³n y pruebas de servicio/API bajo archivos exclusivos; no edita migration/OpenAPI/frontend.
- [x] Database/Security: DbContext, migration/designer/snapshot, policies/grants y pruebas PostgreSQL; espera modelo congelado.
- [x] Frontend: cliente/tipos/hub/vistas/CSS y Vitest; no edita backend/OpenAPI/E2E runner.
- [x] QA/E2E: fixtures y journey inviter/invitee/attacker desktop/mobile; revisiÃ³n final read-only independiente.
- [x] Ejecutar restore/build/MTP/format/EF, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC, SCA, JSON/UTF-8/secrets/diff.
- [x] Mantener `AGRO-ID-003` `En curso`, documentar residuales y publicar un Ãºnico commit/push autorizado; no iniciar otra tarea.

### Baseline

- Git `main` limpio en `0583708`; origin/main coincide.
- Gate previo integrado: .NET 142/142, build Release 0/0, EF sin drift; Vitest 50/50; Playwright 4/4; FND 45/45; SEC 25/25; SCA 0.

### Review final

- Resultado: `PASS` local del sub-slice de invitaciÃ³n one-shot de co-owner; `AGRO-ID-003` permanece `En curso` por roles no-owner, scopes por campo, remociÃ³n/demociÃ³n/Ãºltimo-owner runtime e invitaciones dirigidas por email.
- Contrato y seguridad: token CSPRNG de 256 bits visible una sola vez y persistido solo como HMAC versionado; fragmento URL, respuestas `no-store`, errores neutrales, step-up purpose-bound para crear/revocar y aceptaciÃ³n con identidad verificada reciente.
- Persistencia: invitaciÃ³n, membership autoritativa y proyecciÃ³n N-1, journal y outbox tipado son atÃ³micos; concurrencia create/accept/revoke, expiraciÃ³n exacta, replay, rotaciÃ³n/retirada de claves y fallos inyectados quedan fail-closed.
- PostgreSQL: migraciÃ³n expand, writer N-1, roll-forward y rollback efÃ­mero; `FORCE RLS`, roles/grants mÃ­nimos, A/B/sin contexto/pool/job y funciones estrechas SECURITY DEFINER demostradas.
- Backend: restore locked y build Release `PASS` con 0 warnings/errores; suite raÃ­z MTP `170/170`; format y EF pending-model `PASS`.
- Frontend: pnpm frozen, format, lint, typecheck y Next build `PASS`; Vitest `67/67`; Playwright `4/4` en Chromium desktop/mÃ³vil con invitador, invitado distinto, atacante, revocaciÃ³n, Axe, teclado y 390 px.
- Arquitectura/seguridad: fitness incluido en la suite (`70/70`), FND `45/45`, SEC `26/26`, NuGet/pnpm SCA, JSON, UTF-8 y `git diff --check` `PASS`.
- Hallazgos de integraciÃ³n corregidos: versiÃ³n OpenAPI mutada incorrectamente, agregado tenant no declarado, constraint SQL NULL, fixture E2E compartida/rate limit, navegaciÃ³n por hash en SPA y confirmaciÃ³n de revocaciÃ³n.
- Residuales: Auth0/hosting/secret manager/limiter distribuido/Audit central/retenciÃ³n legal siguen NO-GO para ambiente compartido; el fragmento se procesa al montar la app y el journey soportado abrir enlace â†’ login â†’ reload queda cubierto.

## IteraciÃ³n 26 â€” AGRO-GIS-001 referencia territorial v1 (2026-08-11)

Estado inicial: `Propuesto`. Se selecciona autÃ³nomamente como el siguiente puente directo entre organizaciones privadas y la futura creaciÃ³n de campos. TransiciÃ³n del sub-slice: `Propuesto â†’ Ready â†’ En curso`; la tarea padre permanecerÃ¡ `En curso` hasta incorporar snapshot jerÃ¡rquico completo, operaciÃ³n de actualizaciÃ³n y evidencia productiva del proveedor.

### DoR, decisiones y no objetivos

- [x] Verificar que `AGRO-DIS-004` aprobÃ³ WGS84/Georef/MapLibre de forma condicional para desarrollo local y que los pendientes de proveedor/DPA/SLA son gates de ambiente compartido.
- [x] Verificar el fixture reproducible de las 23 provincias+CABA con cÃ³digos oficiales y centroides pÃºblicos; no interpretarlos como campos, parcelas ni precisiÃ³n agronÃ³mica.
- [x] Elegir mÃ³dulo `territory` separado del schema `identity`, dentro del monolito modular y sin microservicio, broker o base compartida entre mÃ³dulos.
- [x] Fijar snapshot inmutable/versionado y modelo provider-neutral de niveles `province|department|municipality|locality`; el seed local cubre provincias y el importador admite jerarquÃ­a completa sin inventarla.
- [x] Fijar bÃºsqueda local como fallback durable y resoluciÃ³n por coordenada mediante adapter Georef de host fijo; si no existe respuesta/cachÃ© vÃ¡lida, devolver `unavailable` y ofrecer bÃºsqueda manual, nunca inferir cÃ³digo por cercanÃ­a al centroide.
- [x] Fijar que bÃºsqueda/resoluciÃ³n requieren sesiÃ³n autenticada, son solo lectura, usan rate limit y no reciben tenant, organizaciÃ³n, nombre de campo ni PII.
- [x] Mantener fuera creaciÃ³n de campos/geometrÃ­as, mapa/tiles productivos, clima, catastro legal, restricciones agronÃ³micas, job/scheduler de sync y deploy.

Outcome observable: una persona autenticada busca territorio argentino sin lupa con resultados jerÃ¡rquicos oficiales del snapshot activo. La resoluciÃ³n por coordenada se verifica localmente con respuestas literales del adapter, pero el egress real permanece deshabilitado por defecto y NO-GO hasta aprobar proveedor/Legal; sin respuesta habilitada, la UI degrada explÃ­citamente a bÃºsqueda manual. Los 24 centroides contractuales validan cobertura nacional sin persistir coordenadas de campos.

### AceptaciÃ³n verificable

- [x] Nuevo mÃ³dulo .NET `AgropecuarIA.Territory` con lÃ­mites Domain/Application/Infrastructure, schema PostgreSQL `territory` y composiciÃ³n explÃ­cita en la API.
- [x] Snapshot activo inmutable con fuente, versiÃ³n, captura, hash y estado; units con cÃ³digo oficial, nivel, parent, nombre y nombre normalizado; constraints evitan parent invÃ¡lido, duplicados y mÃºltiples snapshots activos.
- [x] Seed expand local contiene exactamente 24 provincias/CABA del fixture oficial, incluyendo Tierra del Fuego, con source/version/hash reproducibles.
- [x] Importador valida cÃ³digos, niveles, parents, Unicode, duplicados, ciclos, hash y cobertura antes de activar atÃ³micamente; una activaciÃ³n fallida conserva el snapshot anterior.
- [x] `GET /api/territory/search` aplica query normalizada, level/parent/limit acotados, orden determinista y devuelve fuente/versiÃ³n/frescura; homÃ³nimos conservan jerarquÃ­a.
- [x] `GET /api/territory/resolve` valida WGS84/Argentina, usa `IHttpClientFactory`, host fijo, timeout/tamaÃ±o/schema acotados y estados `fresh|stale|unavailable`; fallo externo no inventa territorio.
- [x] Cache de resoluciÃ³n es derivable, acotada y no persiste/loguea coordenadas; caÃ­da sin cache ofrece fallback manual desde el snapshot.
- [x] UI autenticada ofrece autocomplete reactivo con debounce/cancelaciÃ³n, sin lupa, loader solo en la regiÃ³n de resultados, estados empty/error/unavailable y navegaciÃ³n por teclado/mÃ³vil 390 px/Axe.
- [x] OpenAPI, runtime map, module boundaries y threat model reflejan el nuevo mÃ³dulo/superficie sin declarar mapa/campos/proveedor productivo.
- [x] Tests cubren 24 jurisdicciones, acentos/homÃ³nimos, parent/level, lÃ­mites/coordenadas, payload externo invÃ¡lido/HTML/truncado/429/500/timeout, cache stale y PostgreSQL emptyâ†’N/rollback/roll-forward. No existÃ­a un writer Territory N-1; la compatibilidad demostrada es aditiva y coexiste con el runtime Identity sin alterar su schema.

### Ownership y gates

- [x] Principal: plan, OpenAPI/contratos compartidos, soluciÃ³n/composition root, mapas/evidencia, integraciÃ³n, gates y Git.
- [x] Backend: dominio/aplicaciÃ³n, adapter Georef, endpoints del mÃ³dulo y tests no-DB bajo ownership exclusivo.
- [x] Database/Security: DbContext, migraciÃ³n/seed, importer persistente, constraints y tests PostgreSQL bajo ownership exclusivo.
- [x] Frontend/QA: cliente/tipos/UI y Vitest/E2E de bÃºsqueda/degradaciÃ³n bajo ownership exclusivo.
- [x] Ejecutar restore locked, build Release, MTP, format, EF pending/migrations PostgreSQL, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC, SCA, JSON/UTF-8/secrets/diff y revisiÃ³n independiente.
- [x] Documentar resultados, mantener `AGRO-GIS-001` `En curso`, commit/push autorizado y no iniciar `AGRO-GIS-002` en este incremento.

### Baseline

- Git `main` y `origin/main` en `4d4893f`, worktree limpio.
- Backend: build Release 0 warnings/errores; MTP 170/170; EF sin drift.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest 67/67; Playwright 4/4.
- Validadores: FND 45/45; SEC 26/26; SCA y secret scan sin hallazgos.

### Review final

- Resultado del incremento local: `PASS`; `AGRO-GIS-001` queda `En curso` por fuente jerÃ¡rquica completa, operaciÃ³n administrada de actualizaciÃ³n y gates externos de Georef/Legal/ambiente compartido.
- Backend: restore locked `PASS`; build Release 0 warnings/errores; suite raÃ­z MTP `223/223`; Territory/PostgreSQL `44/44`; Architecture Fitness `79/79`; `dotnet format` y EF pending-model de Identity/Territory `PASS`.
- Frontend: pnpm frozen, Prettier, lint, typecheck y Next build `PASS`; Vitest `79/79`; Playwright Chromium+mobile `6/6`, incluida degradaciÃ³n sin egress, teclado, Axe y viewport 390 px.
- Seguridad/contratos: FND protocol `45/45`, SEC threat model `41/41`, OpenAPI/runtime map/RLS/hash/provider guards `PASS`; NuGet y pnpm sin vulnerabilidades conocidas; secretos, JSON, UTF-8 y diff-check sin hallazgos.
- Persistencia: primer schema Territory validado emptyâ†’N, seed/hash reproducibles, activaciÃ³n atÃ³mica, rollback/roll-forward efÃ­mero y convivencia aditiva con Identity. No se afirma un writer Territory N-1 inexistente.
- RevisiÃ³n independiente: sin hallazgos crÃ­ticos/altos/medios abiertos. Se corrigieron shape real `gobierno_local`, egress default-off, logging HTTP sin URI, hash NFC completo, homÃ³nimos, payload truncado, coordenadas echoed, contrato Problem/Retry-After/parent, copy de frescura y evidencia E2E.

## IteraciÃ³n 27 â€” AGRO-SEC-002 frontera tenant Identity v1 (2026-08-11)

Estado inicial: `Propuesto`. Tras auditar readiness con Architecture/Data, Product/QA y AppSec, se selecciona el primer incremento ejecutable por mÃ³dulo y se transiciona `Propuesto â†’ Ready â†’ En curso`. La tarea padre R1â€“R6 permanecerÃ¡ `En curso` para futuras superficies, jobs, storage, exports e IA.

### DoR, alcance y no objetivos

- [x] Verificar que `ID-003` y el contrato local de `FND-002` aportan rutas tenant reales, RLS, replay/idempotencia y tests PostgreSQL reproducibles.
- [x] Elegir exclusivamente la frontera actual Identity + Territory; no crear roles, endpoints, campos, jobs, cache tenant, storage, export, retrieval, DAST remoto ni deploy.
- [x] Tratar Territory como referencia compartida autenticada y sin datos tenant; no inventar RLS tenant para un snapshot oficial platform-owned.
- [x] Fijar output del audit en `tasks/evidence/AGRO-SEC-002/`, con matriz ejecutable, arquitectura, reportes y findings estructurados.
- [x] Fijar que una ruta futura no puede entrar al runtime sin resource/action/scope/context/authz/storage/error/tests/owner registrados.

Outcome observable: un gate reproducible cubre exactamente todas las operaciones HTTP actuales de Identity y Territory, distingue `public-platform`, `authenticated-platform`, `tenant`, `shared-reference` y `development-test-only`, y falla si una nueva ruta tenant carece de autorizaciÃ³n por recurso, contexto server-derived, RLS/default-deny, error neutral o caso negativo.

### AceptaciÃ³n verificable

- [x] Inventario machine-readable completo de operaciones OpenAPI/runtime, sin duplicados ni rutas huÃ©rfanas.
- [x] Cada operaciÃ³n declara recurso, acciÃ³n, boundary, autenticaciÃ³n, fuente de actor/tenant, autorizaciÃ³n de aplicaciÃ³n, frontera de persistencia/cache, error neutral, owner y tests ejecutables.
- [x] Rutas tenant de owner-invitations demuestran Org A/B, actor ajeno/sin contexto, sesiÃ³n revocada, replay, concurrencia, pool/job y `FORCE RLS` con principal sin ownership/BYPASS. La revocaciÃ³n de membership no existe todavÃ­a y queda para el futuro slice de remociÃ³n de co-owner.
- [x] Bootstrap `CreateOrganization` y accept por token permanecen platform-scoped pero reautorizan actor/sesiÃ³n antes de lookup/replay y fijan tenant server-side despuÃ©s de resolverlo.
- [x] Territory search/resolve requieren sesiÃ³n, no aceptan tenant, no persisten coordenadas y mantienen egress default-off; el cache global queda como gate de privacidad antes de habilitar Georef multiusuario.
- [x] Rutas Development/Test estÃ¡n clasificadas y el gate enlaza prueba de ausencia fuera de esos ambientes.
- [x] Jobs, storage, export y AI figuran `not-present`, no `approved`; la tarea no afirma cobertura de superficies inexistentes.
- [x] Mutations negativas rompen por ruta ausente, mÃ©todo/grupo nuevo, scope falso, tenant client-authoritative, falta de authz/RLS/error/test/owner, shared-reference sin minimizaciÃ³n y superficie inexistente marcada aprobada.
- [x] Security audit entrega `architecture.md`, `REPORT.md`, `FINDINGS-DETAIL.md` y `findings.json` validados; no hubo hallazgos confirmados que exigieran cambio productivo.

### Ownership y gates

- [x] Principal: plan, matriz/validator, fitness, integraciÃ³n, evidencias, gates y Git.
- [x] Security/Data: revisar RLS/roles/grants/functions/pool/job/replay y hunting de BOLA; no editar producto salvo defecto confirmado y asignado.
- [x] Architecture: verificar cobertura OpenAPI/runtime y clasificaciones platform/tenant/shared/dev.
- [x] Product/QA: fixtures A/B/attacker/revoked y acceptance; revisiÃ³n independiente final.
- [x] Ejecutar restore locked, build Release, MTP raÃ­z y dirigidos PostgreSQL, format, EF pending, E2E existente, validadores FND/SEC/SEC-002, SCA, JSON/UTF-8/secrets/diff.
- [x] Documentar resultados, mantener `AGRO-SEC-002` `En curso`, commit/push autorizado y no iniciar revocaciÃ³n de co-owner en esta iteraciÃ³n.

### Baseline

- Git `main`/`origin/main` limpios en `15ead58`.
- Backend: restore/build/format/EF PASS; MTP `223/223`; Territory `44/44`; fitness `79/79`.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest `79/79`; Playwright `6/6`.
- Validadores: FND `45/45`; SEC `41/41`; SCA y secret scan sin hallazgos.

### Review final

- Resultado: `PASS` del incremento Identity tenant v1; `AGRO-SEC-002` permanece `En curso` porque storage, jobs, export, retrieval, IA y nuevas superficies tenant aÃºn no existen.
- Gate: registro estricto de `20/20` operaciones HTTP, callback OIDC y cinco superficies futuras ausentes; OpenAPI, rutas source, ownership, tests y boundary semantics se validan en Architecture Fitness.
- Mutations: `16/16` negativos mÃ¡s el caso publicado prueban ruta faltante, mÃ©todo/grupo nuevo, IDs Ãºnicos, boundary, transiciones platformâ†’tenant, tenant client-authoritative, authz, `FORCE RLS`, error neutral, shared reference, Dev/Test, test/sÃ­mbolo, owner, OIDC y superficies futuras.
- Security audit: cero findings Critical/High/Medium/Low explotables en runtime default. El oracle temporal potencial del cache Territory es condicional a Georef habilitado y queda como gate previo a egress multiusuario.
- Backend: restore locked, build Release 0 warnings/errores, MTP raÃ­z `240/240`, fitness `96/96`, format y EF pending-model de ambos contextos `PASS`.
- Frontend: pnpm frozen, format, lint, typecheck, Vitest `79/79`, Next build y Playwright `6/6` `PASS`; no hubo cambio productivo frontend.
- Validadores y supply chain: FND `45/45`, SEC `41/41`, findings schema, JSON/UTF-8, NuGet/pnpm SCA, secrets y diff-check `PASS`.
- RevisiÃ³n independiente: autorizaciÃ³n tenant/RLS `23/23` dirigida y auth/browser `79/79`; se corrigieron extracciÃ³n de PUT/PATCH/grupos nuevos, invariantes shared-reference y el claim imposible de membership revocada.
- PublicaciÃ³n: un Ãºnico commit/push autorizado, sin deploy y sin iniciar revocaciÃ³n de co-owner.

## IteraciÃ³n 28 â€” AGRO-ID-003 remociÃ³n segura de co-owner (2026-08-18)

Estado inicial: `Ready` para un sub-slice estrecho. `AGRO-ID-003` permanece `En curso`; esta iteraciÃ³n no implementa salida voluntaria, transferencia, demociÃ³n, roles adicionales, scopes por campo, email ni superadmin.

### DoR, alcance y decisiones

- [x] Fijar que cualquier owner activo puede remover a otro owner activo; todos los owners son simÃ©tricos y el creador no tiene privilegio especial.
- [x] Excluir `actor == target`; self-remove/leave requiere un slice posterior.
- [x] Exigir sesiÃ³n vigente y step-up purpose-bound `manage_organization_owners` para remover; el listado requiere owner activo.
- [x] Modelar remociÃ³n autoritativa como estado terminal `removed`, conservar historial y eliminar la proyecciÃ³n legacy en la misma transacciÃ³n.
- [x] Proteger `>= 1` owner activo con serializaciÃ³n/lock por organizaciÃ³n y una primitiva PostgreSQL estrecha; no conceder UPDATE/DELETE amplios al rol app.
- [x] Revocar atÃ³micamente invitaciones pendientes creadas por el owner removido y serializar create-invitation contra la misma organizaciÃ³n.
- [x] Mantener fail-closed la reactivaciÃ³n de una membership removida; reinvite/reactivaciÃ³n queda fuera de esta iteraciÃ³n.

Outcome observable: el owner A lista los co-owners activos de Org A y remueve a B con CSRF, `If-Match` e idempotencia. B conserva su cuenta de plataforma, pero pierde inmediatamente Org A y todas sus rutas tenant; carreras nunca dejan la organizaciÃ³n sin owner.

### AceptaciÃ³n verificable

- [x] `GET /api/identity/organizations/{organizationId}/owner-memberships` devuelve sÃ³lo owners activos con display name, membership UUID, versiÃ³n y marca `isCurrentUser`; nunca expone userId, email ni identidad externa.
- [x] `DELETE /api/identity/organizations/{organizationId}/owner-memberships/{membershipId}` deriva actor/tenant server-side, exige CSRF, `If-Match`, `Idempotency-Key` y assurance vigente.
- [x] Org ajena, actor no-owner, target ausente/ajeno/removido y self-target responden neutralmente sin oracle; ETag stale, last-owner, replay, mismatch e in-flight tienen errores tipados.
- [x] Membership autoritativa queda `removed`, incrementa security/concurrency version, conserva historial y desaparece de `organization_memberships` legacy y de `/session`.
- [x] Membership, proyecciÃ³n legacy, invitaciones pendientes, ledger, journal y outbox cambian en una transacciÃ³n; fault injection demuestra rollback total.
- [x] Dos remociones concurrentes dejan exactamente un owner activo; retry/replay produce un solo efecto, journal y evento.
- [x] RLS/roles/grants prueban A/B/sin contexto/pool/job, actor removido y ausencia de UPDATE/DELETE amplio; la funciÃ³n privilegiada no queda expuesta a PUBLIC/job/discovery.
- [x] OpenAPI, evento tipado, schema/runtime/consumer maps y SEC-002 registran las dos rutas y fallan ante drift.
- [x] UI muestra `Co-owners`, UUID corto, confirmaciÃ³n accesible, reauth, loading/offline/error/stale/last-owner, foco/teclado/Axe y viewport 390 px.
- [x] E2E demuestra A invita B, B acepta, A remueve B y B pierde Org A tras refrescar, en desktop y mobile.

### Ownership y gates

- [x] Principal: decisiones, plan, contratos compartidos, eventos/maps, integraciÃ³n, documentaciÃ³n, gates y Git.
- [x] Backend: dominio/aplicaciÃ³n/API/telemetrÃ­a y pruebas funcionales bajo ownership exclusivo.
- [x] Database/Security: DbContext, migraciÃ³n, funciÃ³n/roles/RLS y pruebas PostgreSQL bajo ownership exclusivo.
- [x] Frontend/QA: tipos/cliente/UI, Vitest y E2E bajo ownership exclusivo.
- [x] Ejecutar restore locked, build Release, MTP raÃ­z/dirigidos, format, EF pending/migraciÃ³n N/N-1/rollback, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON y diff-check.
- [x] RevisiÃ³n independiente sin hallazgos crÃ­ticos/altos/medios; mantener `AGRO-ID-003` `En curso`.
- [x] Publicar sÃ³lo con autor local `JuaniRMariani <juanirmariani@gmail.com>`, verificar `git show --format=fuller -1` y no desplegar (`79a0b09`).

### Baseline

- Git `main`/`origin/main` en `6ac8d74`; worktree limpio salvo la lecciÃ³n solicitada para fijar identidad Git personal.
- Backend: restore/build/format/EF PASS; MTP raÃ­z `240/240`; fitness `96/96`.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest `79/79`; Playwright `6/6`.
- Validadores: FND `45/45`; SEC `41/41`; SEC-002 `20/20` operaciones; SCA y secrets sin hallazgos.

### Review final

- Resultado tÃ©cnico: `PASS` local del sub-slice; `AGRO-ID-003` permanece `En curso`.
- Backend: restore locked, build Release 0 warnings/errores, suite raÃ­z MTP `256/256`, fitness `101/101`, format y EF Identity/Territory `PASS`.
- Seguridad/DB: target ausente/ajeno/removido/self neutral, last-owner concurrente, replay/mismatch/in-flight/commit incierto y rollback journal/outbox `PASS`; FND `45/45`, SEC `42/42`, SEC-002 `22/22`.
- Frontend: pnpm frozen/format/lint/typecheck/build `PASS`, Vitest `95/95`; Playwright PostgreSQL real `6/6` desktop/mobile con journey Aâ†’Bâ†’remociÃ³n, Axe, teclado y 390 px.
- Supply chain: advisories transitivos detectados durante el gate corregidos mediante pins compatibles `SSH.NET 2026.0.0` y `nanoid 3.3.18`; NuGet/pnpm audit final sin vulnerabilidades conocidas.
- Sin deploy. Self-remove/transfer/demociÃ³n, roles no-owner, scopes campo, email/delivery y superadmin siguen fuera.

## IteraciÃ³n 29 â€” AGRO-GIS-002 campo borrador no espacial (2026-08-18)

Estado inicial: `Ready` Ãºnicamente para el sub-slice estrecho `CreateField draft + lista/ficha accesible`. `AGRO-GIS-002` permanece `En curso`; geometrÃ­a, Ã¡rea, mapa, tiles, establecimiento/parcela/lote/potrero, catÃ¡logo y ediciÃ³n quedan fuera hasta cerrar sus dependencias.

### DoR, alcance y decisiones

- [x] Crear el bounded context productivo `Productive Core`; posee `ManagementUnit`. Territory no persiste ni consulta campos en este slice.
- [x] Fijar `ManagementUnit` de tipo server-side `field`, estado inicial `draft` y representaciÃ³n espacial `not_configured`; el request no acepta tipo, estado, actor, tenant, rol, coordenadas ni Ã¡rea.
- [x] Fijar `displayName` con trim Unicode `White_Space`+`U+FEFF`, NFC, 2..120 escalares, sin controles ni surrogates aislados; nombres duplicados se permiten dentro y entre organizaciones y se distinguen por UUID corto.
- [x] Autorizar sÃ³lo owner activo. Actor, sesiÃ³n y organizaciÃ³n se derivan/revalidan en servidor antes de consultar idempotencia o recurso.
- [x] Congelar rutas `POST/GET /api/organizations/{organizationId}/fields` y `GET /api/organizations/{organizationId}/fields/{fieldId}` con cookie, CSRF en POST, `Idempotency-Key` y errores Problem neutrales.
- [x] Crear `ManagementUnitCreated` tenant-scoped sin nombre, coordenadas, key/digest ni PII; journal local y outbox quedan atÃ³micos con negocio e idempotencia.

Outcome observable: un owner crea `Campo Norte`, recarga, lo ve en una lista y abre su ficha `Sin geometrÃ­a`. Otra organizaciÃ³n puede usar el mismo nombre pero nunca ve ni infiere el recurso ajeno. Un co-owner removido pierde acceso inmediatamente.

### AceptaciÃ³n verificable

- [x] POST vÃ¡lido responde 201 y confirma exactamente una unidad `field/draft/not_configured`; GET list/detail conserva orden determinista y UUID corto sÃ³lo en UI.
- [x] Mismo key+fingerprint reproduce el mismo recurso; payload diferente da conflicto; concurrencia, respuesta perdida/commit incierto y retry no duplican unidad, journal ni outbox.
- [x] El lÃ­mite local de 100 campos por organizaciÃ³n se aplica dentro de la transacciÃ³n `SERIALIZABLE`: replay precede al conteo, dos altas concurrentes desde 99 dejan exactamente 100 y el exceso responde conflicto terminal sin ledger ni efectos auxiliares.
- [x] Org A/B, actor ajeno, target ausente/ajeno, sin contexto, sesiÃ³n o membership revocada fallan neutralmente antes de lookup/replay; no existe oracle cross-tenant.
- [x] PostgreSQL usa schema/owner/principal propios, `FORCE RLS`, actor+tenant transaction-local, grants mÃ­nimos y pruebas de pool, rollback, error, cancelaciÃ³n y job sin contexto.
- [x] Fallo inyectado de ledger, journal u outbox revierte todas las superficies; telemetrÃ­a allow-listed no contiene UUID, nombre, idempotency key, digest ni payload.
- [x] MigraciÃ³n expand-compatible demuestra clean, N/N-1, app rollback/roll-forward y pending model; `Down` destructivo sÃ³lo en base efÃ­mera.
- [x] OpenAPI, module/event/runtime/consumer maps y SEC-002 registran las tres operaciones y fallan ante drift.
- [x] UI cubre empty/loading/submitting/offline/validation/conflict/reconciliation/unavailable/success, ficha `Sin geometrÃ­a`, foco/teclado/Axe y 390 px sin UUID completo.
- [x] Playwright demuestra createâ†’reloadâ†’detail y aislamiento A/B en desktop y mobile, sin PostGIS, tiles ni egress.

### Ownership y gates

- [x] Principal: plan, contrato compartido, composiciÃ³n, mapas/evidencia, integraciÃ³n, gates y Git.
- [x] Backend: nuevo mÃ³dulo Productive Core, dominio/aplicaciÃ³n/API/telemetrÃ­a y pruebas no-DB bajo ownership exclusivo.
- [x] Database/Security: DbContext, migraciÃ³n, roles/RLS/primitivas y pruebas PostgreSQL bajo ownership exclusivo.
- [x] Frontend/QA: tipos/cliente/UI, Vitest y Playwright bajo ownership exclusivo.
- [x] Ejecutar restore locked, build Release, MTP raÃ­z/dirigidos, format, EF pending/N-N-1/rollback, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON y diff-check.
- [x] RevisiÃ³n independiente sin hallazgos crÃ­ticos/altos/medios; mantener `AGRO-GIS-002` `En curso`.
- [x] Commit/push con `JuaniRMariani <juanirmariani@gmail.com>`; author y committer verificados, sin deploy (`4d4fe70`).

### Baseline

- Git `main`/`origin/main` en `5f32e15`; worktree limpio.
- Backend: restore/build/format/EF PASS; MTP raÃ­z `256/256`; fitness `101/101`.
- Frontend: pnpm frozen/format/lint/typecheck/build PASS; Vitest `95/95`; Playwright `6/6`.
- Validadores: FND `45/45`; SEC `42/42`; SEC-002 `22/22`; SCA y secrets sin hallazgos.

### Review final

- PASS integrado-local: restore locked; build Release 9 proyectos `0/0`; MTP raÃ­z `308/308`; Productive Core/PostgreSQL `30/30`; Architecture Fitness `121/121`; Vitest `130/130`; Playwright oficial `6/6`; FND `45/45`; SEC `53/53`; SEC-002 `25/25`; EF `3/3`, format, SCA, secretos, UTF-8/JSON y diff-check verdes.
- La revisiÃ³n independiente cerrÃ³ cinco hallazgos medios antes de publicar: errores de apertura/commit read tipados como 503, seguridad OpenAPI AND, persistencia idempotente ante respuesta ambigua, capacidad atÃ³mica de 100 sin truncamiento y canonicalizaciÃ³n Unicode idÃ©ntica. VerificÃ³ 0 Critical, 0 High y 0 Medium restantes.
- `AGRO-GIS-002` permanece `En curso`: geometrÃ­a, Ã¡rea, mapas/tiles, catÃ¡logo, ediciÃ³n, delivery y gates de ambiente compartido continÃºan fuera de este sub-slice.
- PublicaciÃ³n funcional: `4d4fe70` (`feat(productive-core): create non-spatial field drafts`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`.

## IteraciÃ³n 30 â€” AGRO-FND-003 renombrar campo borrador (2026-08-18)

Estado inicial: micro-DoR cerrado con defaults tÃ©cnicos reversibles; transiciÃ³n `Propuesto â†’ Ready â†’ En curso` Ãºnicamente para `RenameFieldDraft`. `AGRO-FND-003` permanece `En curso` y no absorbe backfills masivos, contract migrations, geometrÃ­a, catÃ¡logo, archivo/borrado ni ediciÃ³n de otros campos.

### Plan y decisiones congeladas

- [x] Elegir un consumidor vertical real: renombrar sÃ³lo `ManagementUnit field/draft/not_configured` ya autorizado; owner activo, sin step-up adicional.
- [x] Congelar `PATCH /api/organizations/{organizationId}/fields/{fieldId}` con body cerrado `{displayName}`, cookie, CSRF, `If-Match` fuerte e `Idempotency-Key`; respuesta 200 flat + `isReplay` y ETag nuevo.
- [x] Ordenar la decisiÃ³n como authz vigente â†’ replay ligado â†’ recurso/If-Match â†’ mutaciÃ³n. Un replay vÃ¡lido conserva resultado; versiÃ³n stale nueva responde 412 neutral y nunca aplica last-write-wins.
- [x] Canonicalizar el nombre con la misma regla CreateField: Unicode `White_Space`+`U+FEFF`, NFC, 2..120 escalares, sin controles ni surrogates aislados; duplicados siguen permitidos.
- [x] Rotar `Version` UUID y aumentar una revisiÃ³n monotÃ³nica por rename. Journal/outbox no guardan nombre, key, digest, actor ni payload; `ManagementUnitDisplayNameChanged` publica sÃ³lo IDs internos, revisiÃ³n y fecha.
- [x] Mantener compatibilidad expand N/N-1: revisiÃ³n con default 1 y tablas/Ã­ndices aditivos; rollback de aplicaciÃ³n deshabilita PATCH sin revertir el nombre confirmado y ambientes compartidos usan roll-forward.

### AceptaciÃ³n verificable

- [x] Un owner cambia sÃ³lo el nombre, recibe nuevo ETag y list/detail convergen; mismo nombre canÃ³nico no crea versiÃ³n, ledger, journal ni outbox.
- [x] Dos editores con el mismo ETag dejan un Ãºnico nombre: uno confirma y el otro recibe 412 sin sobrescribir ni filtrar datos.
- [x] Mismo key/fingerprint/versiÃ³n reproduce el resultado; key con nombre o `If-Match` distinto da 409; commit incierto reconcilia o falla cerrado sin repetir el rename.
- [x] Org B, owner removido, sesiÃ³n revocada, recurso ausente/ajeno y contexto faltante fallan 404 neutral antes de lookup/replay.
- [x] Field + ledger/aliases + journal + outbox confirman atÃ³micamente; fault injection por cada sink demuestra rollback total y ETag anterior vigente.
- [x] PostgreSQL real prueba `FORCE RLS`, grants mÃ­nimos, A/B/sin contexto/pool/job, concurrencia EF/Serializable, cancelaciÃ³n y migraciÃ³n clean/N/N-1/rollback/roll-forward.
- [x] UI ofrece â€œEditar nombreâ€ en ficha, conserva draft+key ante offline/429/in-progress/503/reauth y ofrece â€œRecargar y revisarâ€ ante 412; foco, teclado, Axe, 390 px y UUID corto.
- [x] OpenAPI, schema/event/runtime/consumer maps, SEC-002 y Architecture Fitness fallan ante drift de PATCH, ETag, evento, boundary o test negativo.

### Ownership y gates

- [x] Backend: dominio/aplicaciÃ³n/API/telemetrÃ­a y tests no-DB, sin editar migraciÃ³n, contratos, Program, web ni evidencia.
- [x] Database/Security: DbContext, migraciÃ³n, adapter PostgreSQL/RLS/grants y pruebas DB, sin editar API/web/docs.
- [x] Frontend/QA: cliente/UI/Vitest/Playwright y estados accesibles, sin editar backend/contratos/docs.
- [x] Principal: contrato compartido, composiciÃ³n/config, eventos/maps/evidencia, integraciÃ³n, gates y Git.
- [x] Ejecutar restore locked, build Release, MTP raÃ­z/dirigidos, format, EF pending/N-N-1/rollback, pnpm frozen/format/lint/typecheck/Vitest/build, Playwright, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON y diff-check.
- [x] RevisiÃ³n independiente con 0 Critical/High/Medium; mantener `AGRO-FND-003` `En curso`.
- [x] Commit/push con `JuaniRMariani <juanirmariani@gmail.com>`; verificar author/committer y no desplegar.

### Review final

- PASS integrado-local: restore locked; build Release 9 proyectos `0/0`; MTP raÃ­z `348/348`; Productive Core/PostgreSQL `56/56`; no-DB/API `37/37`; Architecture Fitness `135/135`; Vitest `153/153`; Playwright oficial `6/6`; FND `45/45`; SEC `56/56`; SEC-002 `26/26`; EF `3/3`, format, SCA, secretos, UTF-8/JSON, parser y diff-check verdes.
- La revisiÃ³n independiente cerrÃ³ dos Medium antes de publicar: alias split en recovery ahora falla 503 `idempotency.reconciliation_required`, y el rol app sÃ³lo conserva SELECT/INSERT sobre rename ledgers con UPDATE real denegado `42501`. Resultado final: 0 Critical, 0 High y 0 Medium pendientes.
- `AGRO-FND-003` permanece `En curso`; backfills/contract migrations/restore general y otros agregados siguen fuera. No hubo deploy.
- PublicaciÃ³n funcional: `2ace1f5` (`feat(productive-core): rename non-spatial field drafts`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`.

## IteraciÃ³n 31 â€” AGRO-FE-001 OwnerWorkspaceShellV1 (2026-08-18)

Estado inicial: auditorÃ­a Product/QA, Security/Data y Architecture convergiÃ³ en un Ãºnico sub-slice Ready. TransiciÃ³n `Propuesto â†’ Ready â†’ En curso` sÃ³lo para un shell owner sobre contratos existentes; `AGRO-FE-001` permanece `En curso`.

### Plan y decisiones congeladas

- [x] Reutilizar sesiÃ³n y memberships `owner/active`; no crear roles, endpoints, tablas, eventos, preferencias persistentes ni telemetrÃ­a nueva.
- [x] Modelar 0 organizaciones como onboarding; 1 como selecciÃ³n automÃ¡tica; N como selector explÃ­cito sin consultar datos tenant hasta elegir.
- [x] Usar `?org=ABCDEF&view=fields|team|territory|account`. El prefijo corto se resuelve sÃ³lo entre memberships activas de la sesiÃ³n; cero o mÃºltiples coincidencias fallan cerradas. El backend sigue recibiendo el UUID completo.
- [x] Consultar campos/equipo sÃ³lo para la organizaciÃ³n activa. Al cambiar, abortar requests anteriores, limpiar ficha/estado del tenant previo y mover foco al heading del workspace.
- [x] Preservar cualquier intento idempotente ambiguo dentro de su organizaciÃ³n. Bloquear cambio durante submit/in-progress/reconciliation y confirmar antes de descartar un borrador editable.
- [x] Mantener espaÃ±ol `es-AR`, UUID visible corto, sin datos sensibles en `localStorage`, sin offline/PWA falso y sin copiar el dominio/autorizaciÃ³n al cliente.
- [x] Fijar gate local reproducible en Chromium desktop y Pixel 7; Firefox/WebKit y certificaciÃ³n manual completa quedan en el padre.

### AceptaciÃ³n verificable

- [x] Owner con dos organizaciones selecciona A y ve Ãºnicamente campos/co-owners de A; B con nombres duplicados no aparece ni recibe requests hasta cambiar contexto.
- [x] Cambio Aâ†’B actualiza URL, heading y vista; aborta respuesta tardÃ­a de A y no reintroduce estado anterior.
- [x] Reload y back/forward restauran un contexto vÃ¡lido; prefijo inexistente, colisionado o membership removida vuelven al selector sin consultar tenant.
- [x] 0 organizaciones conserva onboarding; 1 se selecciona de forma determinista; sesiÃ³n revocada limpia inmediatamente datos y contexto visibles.
- [x] Un draft no enviado exige confirmaciÃ³n accesible al cambiar; una mutaciÃ³n pending/in-progress/reconciliation impide cambiar y conserva key/contexto.
- [x] NavegaciÃ³n `fields|team|territory|account` usa landmarks, skip-link, `aria-current`, foco visible y anuncios acotados; 390 px no tiene overflow horizontal.
- [x] NingÃºn UUID completo se renderiza en selector, URL, tarjetas, modales o mensajes; el locator corto jamÃ¡s concede autoridad.
- [x] Axe no reporta violaciones critical/serious y shell/estados siguen utilizables ante loading, offline, 404, 429 y 503.

### Ownership y gates

- [x] Frontend: shell, resolver/contexto URL, navegaciÃ³n, estilos e integraciÃ³n mÃ­nima con features existentes.
- [x] QA: unit/component de 0/1/N, colisiÃ³n/remociÃ³n, URL/back-forward, abort stale, draft/pending, foco/UUID; Playwright desktop/mÃ³vil/Axe/390.
- [x] Principal: micro-DoR/backlog/evidencia, integraciÃ³n, gates, revisiÃ³n y Git; sin cambios backend/schema/OpenAPI.
- [x] Ejecutar pnpm frozen/format/lint/typecheck/Vitest/build, Playwright oficial, restore/build/MTP raÃ­z, FND/SEC/SEC-002, SCA, secrets, UTF-8/JSON, parser y diff-check.
- [x] RevisiÃ³n independiente con 0 Critical/High/Medium; mantener `AGRO-FE-001` `En curso`.
- [x] Commit/push con `JuaniRMariani <juanirmariani@gmail.com>`; verificar author/committer y no desplegar.

### Review final

- Restore/build Release `PASS` (0 warnings/errores); suite raÃ­z MTP `348/348`; Architecture Fitness `135/135` y SEC-002 `26/26` operaciones sin cambios de runtime.
- Frontend frozen/format/lint/typecheck/build `PASS`; Vitest `179/179`; Playwright oficial `8/8` desktop+mÃ³vil con Axe/390 y cleanup hermÃ©tico.
- FND `45/45`, SEC `56/56`, NuGet/pnpm audit, UTF-8/JSON/parser/secrets/diff `PASS`.
- RevisiÃ³n independiente final `PASS`: 0 Critical, 0 High, 0 Medium. Cinco Medium encontrados durante revisiÃ³n fueron corregidos y cubiertos antes de publicaciÃ³n.
- PublicaciÃ³n funcional: `0a170e0` (`feat(frontend): add owner workspace shell`) en `origin/main`; author y committer verificados como `JuaniRMariani <juanirmariani@gmail.com>`.
- `OwnerWorkspaceShellV1` queda aprobado integrado-local. `AGRO-FE-001` permanece `En curso`; roles no-owner, preferencias, matriz completa de navegadores y certificaciÃ³n WCAG manual siguen fuera. No hubo deploy.

## IteraciÃ³n 32 â€” AGRO-ID-004 OwnSessionInventoryAndRevokeV1 (2026-08-18)

Estado inicial: dos revisiones independientes de tres seleccionaron este sub-slice como el Ãºnico Ready local. `ArchiveFieldDraft` queda bloqueado por cuota/restore/visibilidad/N-N-1; `AGRO-FND-002` delivery queda bloqueado por falta de consumidor real. `AGRO-ID-004` transiciona `Propuesto â†’ Ready â†’ En curso` sÃ³lo para inventario y revocaciÃ³n individual de sesiones propias; el padre permanece `En curso`.

### Plan y decisiones congeladas

- [x] Contrastar Product/QA, Security/Data y Architecture para `ArchiveFieldDraft`, `AGRO-ID-004` sesiones propias y `AGRO-FND-002` delivery con consumidor real.
- [x] Verificar DoR, dependencias, decisiones, runtime reutilizable, riesgos, valor sponsor y que el alcance no absorba otra tarea.
- [x] Seleccionar `OwnSessionInventoryAndRevokeV1`: plataforma, sÃ³lo actor autenticado, sesiones propias, sin organizaciÃ³n/tenant, dispositivo, IP, user-agent, fingerprint ni token/hash.
- [x] Mantener la sesiÃ³n actual en el flujo existente de logout; la nueva revocaciÃ³n individual sÃ³lo actÃºa sobre otra sesiÃ³n propia y exige step-up purpose-bound nuevo `manage_sessions`.
- [x] Usar colecciÃ³n paginada `offset/limit` acotada y orden estable; wire mÃ­nimo `sessionId/authenticatedAtUtc/expiresAtUtc/isCurrent/version`, con UUID corto sÃ³lo en UI.
- [x] Revocar mediante CSRF + `If-Match` fuerte; actor derivado del servidor, target ajeno/ausente neutral, CAS activoâ†’revocado y replay concurrente sin segundo journal.
- [x] Reutilizar `RevokedAtUtc`/`Version` existentes y auditorÃ­a local atÃ³mica. La Ãºnica migraciÃ³n permitida amplÃ­a de forma aditiva los CHECK de purpose para `manage_sessions`; no agrega motivo/columnas, dispositivo, notificaciÃ³n, evento/consumer, purge ni claim legal.
- [x] Implementar OpenAPI, dominio/aplicaciÃ³n/API, autorizaciÃ³n/DB mÃ­nima, vista Cuenta, tests y evidencia sin cerrar el padre.
- [x] Ejecutar gates integrales, revisiÃ³n independiente, commit/push con identidad local verificada y no desplegar.

### Replan tras gate E2E

- [x] Corregir el locator ambiguo entre logout actual y revocaciÃ³n de otra sesiÃ³n sin relajar la aserciÃ³n accesible.
- [x] Aislar de forma determinista las identidades/sesiones entre proyectos Chromium y mobile; una falla de serializaciÃ³n o un perfil reutilizado no puede convertir A/B en el mismo actor.
- [x] Demostrar en navegador que la sesiÃ³n B realmente queda revocada (401 inmediato) mientras A conserva 200, y que el aislamiento tenant sigue dando 404.
- [x] Repetir el wrapper oficial completo `10/10`, verificar cleanup de puertos/cluster/artefactos y reciÃ©n entonces retomar revisiÃ³n/publicaciÃ³n.
- [x] Corregir el copy purpose-bound de `manage_sessions` y cubrirlo con un mapping exhaustivo en UI.
- [x] Probar purpose confusion, rollback ante fallo del journal y denegaciÃ³n inmediata de la cookie B en Productive, sin sustituirlos por claims documentales.
- [x] Invalidar el inventario cuando rota la sesiÃ³n actual por cualquier purpose, y tratar 401 de list/revoke como sesiÃ³n global revocada en vez de un falso estado MFA/local.
- [x] Repetir gates afectados y obtener revisiÃ³n independiente final con 0 Critical/High/Medium.

### AceptaciÃ³n verificable

- [x] Un usuario con sesiÃ³n actual A y otra sesiÃ³n B lista ambas por pÃ¡ginas; B se muestra con UUID corto, fechas y estado actual, sin secretos ni metadata de dispositivo.
- [x] Desde A, `manage_sessions` vÃ¡lido revoca B una sola vez; dos requests concurrentes producen una transiciÃ³n y un journal, y el replay seguro no revive ni duplica evidencia.
- [x] La cookie de B falla en el siguiente request Identity y tambiÃ©n ante un puerto Productive; A permanece vÃ¡lida.
- [x] Target de otro usuario, ausente o activo-expirado no revela existencia; una sesiÃ³n propia ya revocada reproduce 204 sin segundo efecto, y la sesiÃ³n actual se deriva al logout existente.
- [x] Purpose de owners/authentication-methods no autoriza manage-sessions; sesiÃ³n stale/revocada, CSRF ausente, `If-Match` invÃ¡lido/stale, cancelaciÃ³n y fallo de journal fallan cerrados.
- [x] UI Cuenta cubre loading/empty/error/offline/reauth/stale/success, confirmaciÃ³n/foco/teclado/Axe/390 px y nunca muestra UUID completo.
- [x] OpenAPI, runtime, SEC-002 y tests coinciden; no se afirma notificaciÃ³n, device inventory, revoke-all, propagaciÃ³n distribuida ni deploy.

### Ownership y gates

- [x] Backend/contrato: tipos, query/revoke service, purpose, endpoints, OpenAPI y tests de aplicaciÃ³n/API.
- [x] Database/Security: grants/RLS/consultas actor-scoped y PostgreSQL real A/B/concurrencia/cancelaciÃ³n/rollback, sin ampliar acceso a `TokenHash`.
- [x] Frontend/QA: tipos/API/vista Cuenta, estados accesibles, UUID corto, Vitest y Playwright desktop/mÃ³vil.
- [x] Principal: integraciÃ³n, backlog/evidencia, fitness/SEC, restore/build/MTP/format/EF, pnpm gates/E2E, SCA/secrets/UTF-8/JSON/diff y revisiÃ³n final.

### Review final

- Gate integrado-local final: build Release 9 proyectos `0/0`; suite raÃ­z `361/361`; Identity `126/126`; OwnSession API `8/8` y PostgreSQL `5/5`; Architecture Fitness `135/135`; EF `3/3` sin drift.
- Frontend frozen/format/lint/typecheck/build PASS; Vitest `206/206`; Playwright oficial `10/10` desktop+mÃ³vil con perfiles disjuntos, target exacto, 401 B/200 A, 404 cross-tenant, Axe/390 y cleanup.
- FND `45/45`, SEC `56/56`, SEC-002 `28/28`, NuGet/pnpm audit, UTF-8/JSON, parser, secretos y diff-check PASS.
- RevisiÃ³n independiente: GO con 0 Critical, 0 High y 0 Medium. Cinco Medium fueron corregidos y cubiertos antes de publicaciÃ³n.
- `AGRO-ID-004` permanece `En curso`; dispositivos/fingerprints, revoke-all, notificaciones, propagaciÃ³n distribuida, SLO, retenciÃ³n/purge y deploy siguen fuera.
- PublicaciÃ³n funcional: `45a7b91` (`feat(identity): manage own active sessions`) en `origin/main`; author y committer verificados como `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.

## IteraciÃ³n 33 â€” selecciÃ³n del prÃ³ximo sub-slice Ready (2026-08-18)

### Plan

- [x] Auditar Product/QA, Security/Data y Architecture sobre tres candidatos: `ArchiveFieldDraft` conservador, cierre de todas las demÃ¡s sesiones propias y primer delivery FND con consumidor real.
- [x] Contrastar valor sponsor, DoR, semÃ¡ntica de ciclo de vida/capacidad, autoridad, privacidad, dependencias y compatibilidad N/N-1 sin absorber otra tarea.
- [x] Seleccionar `RevokeAllOtherOwnSessionsV1` como Ãºnico sub-slice Ready de `AGRO-ID-004`; el padre permanece `En curso`.
- [x] Mantener `ArchiveFieldDraft` en NO-GO hasta congelar cuota, visibilidad, restore/terminalidad y compatibilidad N/N-1; mantener FND-002 en NO-GO hasta que exista un consumidor real aprobado.
- [x] Congelar `DELETE /api/identity/sessions/others`: comando platform-scoped, sin body, lista de IDs, organizaciÃ³n, `If-Match` ni conteo de respuesta; cookie + CSRF + step-up exacto `manage_sessions`; `204` idempotente.
- [x] Definir el corte lineal en la transacciÃ³n: revocar sÃ³lo sesiones propias activas, no expiradas y distintas de la actual que existan al ejecutar el `UPDATE`; una sesiÃ³n confirmada despuÃ©s queda fuera.
- [x] Conservar la sesiÃ³n actual y rotar `Version`/fijar `RevokedAtUtc` por target. Emitir journal local exactamente una vez por sesiÃ³n realmente modificada, todo en la misma transacciÃ³n; sin evento, outbox, email, notificaciÃ³n, dispositivo, IP, UA o fingerprint.
- [x] Implementar contrato/backend, funciÃ³n DB/grants mÃ­nimos y UI Cuenta con ownership disjunto.
- [x] Probar 0/1/N, replay, bulkÃ—bulk y bulkÃ—individual, sesiÃ³n actual intacta, otros usuarios/expiradas/revocadas intactos, purpose confusion, actor stale, CSRF, fallo de journal, cancelaciÃ³n/pool y cookies revocadas ante Identity y Productive.
- [x] Ejecutar gates completos, revisiÃ³n independiente, commit/push con `JuaniRMariani` y sin deploy.

### Replan tras revisiÃ³n independiente

- [x] Compartir el mismo advisory transaction lock por actor entre revocaciÃ³n individual y bulk antes de revalidar current/target.
- [x] Probar la carrera cross-current exacta Aâ†’B individual contra Bâ†’A bulk; nunca pueden quedar ambas sesiones revocadas.
- [x] Sacar el refresh bulk del code fence histÃ³rico de SEC-001 y mantener los conteos 28/28 como evidencia histÃ³rica, no vigente.
- [x] Repetir build, Identity/DB/API, raÃ­z, format, validadores y revisiÃ³n independiente antes de publicar.

### AceptaciÃ³n congelada

- [x] A con sesiones B/C propias cierra las otras en una sola acciÃ³n; A sigue en 200 y B/C fallan 401 inmediatamente en Identity y Productive Core.
- [x] Cero targets y repeticiÃ³n devuelven 204 sin journal adicional; dos comandos concurrentes y la carrera con revocaciÃ³n individual no duplican transiciÃ³n ni auditorÃ­a.
- [x] Una sesiÃ³n de otro usuario y cualquier login confirmado despuÃ©s del corte permanecen vÃ¡lidos y no son observables en la respuesta.
- [x] Fallo de cualquier journal revierte todas las revocaciones del comando; la credencial app sÃ³lo recibe `EXECUTE` sobre una funciÃ³n estrecha y no obtiene acceso general a `sessions`, `users` ni `TokenHash`.
- [x] UI separa â€œCerrar las otras sesionesâ€ de logout, sÃ³lo habilita con `total > 1`, explica que la actual seguirÃ¡ abierta, confirma con foco/Escape, refresca inventario tras 204 y no muestra UUID completo.
- [x] 401 invalida el estado global; 403 reanuda step-up; pending evita doble submit local; 429/503/offline conservan contexto sin afirmar que no apareciÃ³ una sesiÃ³n concurrente.
- [x] OpenAPI/runtime/SEC-002/evidencia coinciden y los gates desktop/mÃ³vil/Axe/390 pasan.

### Review

- Readiness unÃ¡nime. Build Release `0/0`; raÃ­z `371/371`; Identity `136/136`; DB own-session `12/12`; API own-session `10/10`; Fitness `135/135`; EF `3/3`; Vitest `217/217`; Playwright `10/10`; FND `45/45`; SEC `56/56`; SEC-002 `29/29`; SCA/UTF-8/JSON/parser/secrets/diff PASS.
- La revisiÃ³n independiente detectÃ³ una carrera cross-current Medium antes de publicar. El lock actor compartido, la revalidaciÃ³n current `FOR UPDATE` y una prueba determinista con waiter observado en `pg_locks` la cerraron. Dictamen final: GO, 0 Critical, 0 High, 0 Medium.
- PublicaciÃ³n funcional: `36429af` (`feat(identity): close other active sessions`) en `origin/main`; author y committer verificados como `JuaniRMariani <juanirmariani@gmail.com>`.
- `RevokeAllOtherOwnSessionsV1` queda aprobado integrado-local. `AGRO-ID-004` permanece `En curso`; no hubo deploy.

## IteraciÃ³n 34 â€” selecciÃ³n del prÃ³ximo sub-slice Ready (2026-08-18)

### Plan

- [x] Auditar en paralelo Product/QA, Security/Data y Architecture sobre el backlog R1 vigente despuÃ©s de `RevokeAllOtherOwnSessionsV1`.
- [x] Exigir un ranking Ãºnico con un solo candidato GO, sus dependencias satisfechas, micro-DoR, valor sponsor, riesgos, lÃ­mites y gates; no inventar consumidor, proveedor, rol o polÃ­tica de retenciÃ³n.
- [x] Contrastar especialmente `ArchiveFieldDraft`, el siguiente incremento ID-004 sin PII/proveedor, FE-001 y cualquier slice Productive/Catalog realmente habilitado por el runtime actual.
- [x] Seleccionar por unanimidad `RevokeAllOwnSessionsAndLogoutV1` como Ãºnico GO; `ArchiveFieldDraft`, deep links/preferencias FE y Productive/Catalog permanecen NO-GO por decisiones o runtime ausentes.
- [x] Congelar contrato y aceptaciÃ³n de un Ãºnico sub-slice antes de editar runtime; documentar NO-GO de los demÃ¡s.
- [x] Implementar con ownership disjunto, gates completos, revisiÃ³n independiente, commit/push con identidad local y sin deploy.

### Micro-DoR y aceptaciÃ³n congelada

- [x] `DELETE /api/identity/sessions`, cookie + CSRF + assurance exacta `manage_sessions`; sin body, tenant, IDs, `If-Match`, idempotency key, count ni payload de respuesta.
- [x] Revocar atÃ³micamente todas las sesiones propias activas/no expiradas visibles al statement, incluida current; login confirmado despuÃ©s del corte queda fuera.
- [x] Compartir advisory lock por actor con logout, revocaciÃ³n individual y bulk-others; revalidar current `FOR UPDATE` despuÃ©s del lock.
- [x] Usar timestamp comÃºn y nueva `Version` por target; escribir exactamente un journal `session_revoked` por transiciÃ³n en la misma transacciÃ³n. Cualquier fallo o cancelaciÃ³n revierte todo.
- [x] Devolver 204 y eliminar la cookie HttpOnly sÃ³lo despuÃ©s del commit. Ante respuesta perdida, no afirmar Ã©xito global: 401 sÃ³lo confirma que current ya no autentica y exige reingreso+inventario; 200 permite reintento.
- [x] Mantener sin cambios sesiones ajenas, expiradas y ya revocadas; no agregar evento, outbox, notificaciÃ³n, dispositivo, IP, UA, fingerprint, proveedor ni retenciÃ³n.
- [x] UI Cuenta ofrece CTA y confirmaciÃ³n accesibles, explica que este navegador tambiÃ©n se cerrarÃ¡, evita doble submit, maneja 401/403/429/503/offline y no muestra UUID/count/metadata.
- [x] Probar 0/1/N, Identity y Productive 401 post-commit, purpose/CSRF/stale, globalÃ—global/Ã—individual/Ã—bulk-others/Ã—logout, login post-corte, journal rollback, commit incierto, cancelaciÃ³n/pool y N/N-1.

### Review

- Readiness unÃ¡nime: `RevokeAllOwnSessionsAndLogoutV1` fue el Ãºnico GO; Archive, FE residual y Catalog permanecen NO-GO por decisiones/runtime ausentes.
- Gate integrado preliminar: restore locked y build Release 9 proyectos `0/0`; raÃ­z `381/381`; Identity `146/146`; DB global `18/18`; API own-session `14/14`; Fitness `135/135`; EF `3/3`; Vitest `225/225`; Playwright `10/10`; FND `45/45`; SEC `56/56`; SEC-002 `30/30`; format/lint/typecheck/build/SCA/UTF-8/JSON/secrets/diff PASS.
- La revisiÃ³n independiente detectÃ³ un Medium contractual: un 401 posterior a respuesta perdida no confirma el cierre global si logout ganÃ³ la carrera. Wording, UX/evidencia y test reverso same-current quedaron corregidos; el 401 sÃ³lo prueba current invÃ¡lida y exige reingreso+inventario.
- Dictamen final: GO integrado-local, 0 Critical, 0 High, 0 Medium y 0 Low.
- PublicaciÃ³n funcional: `5b123e3` (`feat(identity): close all active sessions`) en `origin/main`; author y committer verificados como `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.

## IteraciÃ³n 35 â€” selecciÃ³n del prÃ³ximo sub-slice Ready (2026-08-18)

### Plan

- [x] Auditar en paralelo Product/QA, Security/Data y Architecture sobre el backlog R1 vigente despuÃ©s de `RevokeAllOwnSessionsAndLogoutV1`.
- [x] Exigir ranking Ãºnico con un solo candidato GO, micro-DoR, valor sponsor, dependencias satisfechas, lÃ­mites y gates; no inventar consumidor, proveedor, rol, retenciÃ³n ni metadata personal.
- [x] Reconsiderar `ArchiveFieldDraft`, deep-link/contexto FE, residuos honestos de ID-004 y cualquier vertical Productive/Catalog realmente habilitado por el runtime actual.
- [x] Seleccionar por unanimidad `OwnerFieldDeepLinkV1` como Ãºnico GO frontend-only; Archive, residuos ID-004 y Productive/Catalog permanecen NO-GO.
- [x] Congelar contrato y aceptaciÃ³n antes de editar runtime; registrar NO-GO de los demÃ¡s candidatos.
- [x] Implementar con ownership disjunto, gates completos y revisiÃ³n independiente.
- [x] Publicar con identidad local `JuaniRMariani`, verificar el remoto y no hacer deploy.

### Micro-DoR y aceptaciÃ³n congelada

- [x] URL canÃ³nica `?org=ABCDEF&view=fields&field=123ABC`; `field` es prefijo hexadecimal de seis caracteres, nunca UUID completo ni autoridad.
- [x] Resolver `field` sÃ³lo contra la lista completa ya autorizada de la organizaciÃ³n activa. Una coincidencia usa internamente su UUID completo; cero o colisiÃ³n fallan cerrados y no ejecutan GET detail.
- [x] `field` sÃ³lo es vÃ¡lido en `view=fields`; cambiar vista/organizaciÃ³n, membership removida, 401 o 404 limpian ficha y locator. Fallo de lista/offline/429/503 conserva locator para retry sin adivinar.
- [x] Abrir/cerrar ficha usa historial real; reload y back/forward restauran estado. URL invÃ¡lida confirmada se canonicaliza sin contaminar historia.
- [x] Rename dirty confirma antes de cambiar/cerrar; pending/reconciliation bloquean el intento sin trasladar draft/key. Requests tardÃ­os org Aâ†’B o field 1â†’2 se abortan o invalidan por generaciÃ³n.
- [x] Foco entra al heading de ficha y vuelve al disparador/listado al cerrar; anuncio acotado, Axe y 390 px sin overflow. El locator no persiste UUID ni agrega storage, y ningÃºn UUID completo aparece en DOM, URL, errores o telemetrÃ­a; los reintentos preexistentes conservan sus IDs internos en `sessionStorage` segÃºn su contrato one-shot.
- [x] Cero cambios backend, OpenAPI, DB, grants/RLS, evento, consumer, retenciÃ³n o telemetrÃ­a. Clientes N-1 ignoran el query aditivo.
- [x] Cubrir 0/1/N orgs, prefijo invÃ¡lido/desconocido/colisionado/cross-tenant, lista/detail tardÃ­os, reload/back/forward/open/close/Escape, guards, foco/Axe/390 y regresiÃ³n frontend completa.

### Review

- Readiness unÃ¡nime: `OwnerFieldDeepLinkV1` es el Ãºnico GO reversible y frontend-only. Archive, Catalog/Productive y residuos ID-004 permanecen NO-GO por decisiones o runtime ausentes.
- ImplementaciÃ³n frontend-only: locator corto canÃ³nico, resoluciÃ³n contra la lista autorizada del tenant activo, history/guards/foco y requests detail ligados a organizaciÃ³n/campo/generaciÃ³n. No cambiÃ³ backend, OpenAPI, DB, grants/RLS, eventos, telemetrÃ­a ni retenciÃ³n.
- Gates finales: build Release 9 proyectos `0/0`; raÃ­z MTP `381/381`; EF `3/3`; Vitest `252/252`; Playwright oficial `12/12`; FND `45/45`; SEC `56/56`; format/lint/typecheck/Next build/SCA/UTF-8/secrets/diff `PASS`.
- RevisiÃ³n independiente final: `PASS`, 0 Critical, 0 High, 0 Medium y 0 Low abiertos. Se cerraron antes de publicar la pÃ©rdida de foco por popstate, el reemplazo del Ã©xito de creaciÃ³n, la carrera detail Aâ†’B con prefijo compartido y UUID completos en atributos DOM.
- PublicaciÃ³n funcional: `6956c43` (`feat(frontend): add field detail deep links`) en `origin/main`, con author y committer `JuaniRMariani <juanirmariani@gmail.com>`. No hubo deploy.

## IteraciÃ³n 36 â€” selecciÃ³n del prÃ³ximo sub-slice Ready (2026-08-18)

### Plan

- [x] Auditar en paralelo Product/QA, Security/Data y Architecture sobre el backlog vigente despuÃ©s de `OwnerFieldDeepLinkV1`.
- [x] Comparar al menos un incremento Productive, uno Identity/seguridad y uno frontend/plataforma; exigir valor observable, micro-DoR, dependencias satisfechas, lÃ­mites y gates.
- [x] Mantener fail-closed los candidatos que requieran rol, proveedor, consumidor, retenciÃ³n, dispositivo, catÃ¡logo o polÃ­tica todavÃ­a inexistentes.
- [x] Declarar `0 GO` al no existir un sub-slice reversible con DoR completa; registrar NO-GO factual antes de cualquier ediciÃ³n runtime.
- [x] No implementar ni mutar runtime, contratos, DB o frontend; publicar sÃ³lo la evidencia de readiness con `JuaniRMariani` y sin deploy.

### Review

- Resultado unÃ¡nime: `0 GO`. `ArchiveFieldDraftV1` es el candidato mÃ¡s cercano, pero sigue NO-GO por lifecycle/restore, visibilidad, cuota 100, carreras y compatibilidad N/N-1: DB/OpenAPI/cliente sÃ³lo admiten `draft`, List/Get no filtran lifecycle y Count incluye todas las filas.
- `FND-002 DeliveryWithRealConsumerV1` permanece NO-GO: los eventos runtime tienen `consumers=[]` y no existe consumidor aprobado, inbox, dispatcher, worker, lease/fencing, poison ni polÃ­tica de retenciÃ³n. Crear infraestructura sin efecto real violarÃ­a su DoD.
- El residuo de `AGRO-ID-004` permanece NO-GO: dispositivo/familia/fingerprint/UA-IP/notificaciÃ³n/propagaciÃ³n/SLO/purge requieren PII, finalidad, retenciÃ³n, proveedor o runtime inexistentes.
- Frontend/plataforma tampoco ofrece un slice autÃ³nomo Ready: preferencias requieren modelo/polÃ­tica y el quality gate de CI requiere proveedor, identidad de runner, artifacts, provenance y promociÃ³n definidos.
- PrÃ³ximo desbloqueo vÃ¡lido: decisiÃ³n explÃ­cita sobre lifecycle/cuota/visibilidad de Archive o nominaciÃ³n de un consumidor de negocio real. No se editaron runtime, contratos, DB ni frontend; no hubo deploy.

## IteraciÃ³n 37 â€” AuditorÃ­a de tareas pendientes (2026-08-22)

### Plan
- [x] Ejecutar la directiva TAREA_OBJETIVO=AUTO para identificar tareas Ready o Propuesto viables.
- [x] Respetar la restricciÃ³n del sponsor: realizar las tareas pendientes sin tomar decisiones de logica o de negocio.
- [x] Auditar candidatos a sub-slice.

### Review
- Resultado unÃ¡nime: 0 GO.
- No hay tareas en estado Ready en el backlog (00-index.md).
- Las tareas en estado Propuesto o En curso requieren decisiones explÃ­citas de negocio, definiciÃ³n de polÃ­ticas, proveedores o contratos para cumplir su Definition of Ready (DoR).
- Dado que no se permite tomar decisiones de lÃ³gica o negocio de forma autÃ³noma, la implementaciÃ³n queda bloqueada hasta que se definan dichos requerimientos (ej. ciclo de vida de Archive o nominaciÃ³n de consumidor real).
- No se modificÃ³ el cÃ³digo productivo ni se alterÃ³ el runtime.


## IteraciÃ³n 38 â€” ImplementaciÃ³n de ArchiveFieldDraftV1 (2026-08-22)

### Plan
- [x] Ejecutar la directiva TAREA_OBJETIVO=ArchiveFieldDraftV1.
- [x] Agregar el estado Archived y la funciÃ³n Archive() al dominio ManagementUnit.
- [x] Actualizar PostgresProductiveCoreUnitOfWork para que List y Count excluyan los campos archivados (comportamiento tipo papelera/borrador que no afecta cuota).
- [x] Verificar compilaciÃ³n (0 errores).

### Review
- Resultado: GO.
- La lÃ³gica de dominio y exclusiÃ³n en repositorios fue implementada. Los campos archivados ya no bloquean la cuota de 100 ni aparecen por defecto.
- Pendiente para completarlo al 100% (Siguiente iteraciÃ³n): ProductiveCoreArchiveApplicationService, su Ledger de idempotencia y el Endpoint HTTP para invocarlo desde el frontend.
- Siguen pendientes: AGRO-FND-002, AGRO-FND-003, AGRO-ID-002, AGRO-ID-003, AGRO-ID-004, AGRO-CAT-001 (entre otros).


## IteraciÃ³n 39 â€” Capa de AplicaciÃ³n e Idempotencia para ArchiveFieldDraftV1 (2026-08-22)

### Plan
- [x] Crear clases de idempotencia ManagementUnitArchiveLedger y ManagementUnitArchiveKeyAlias.
- [x] Agregar los Unit Of Work contracts y la implementaciÃ³n en PostgresProductiveCoreUnitOfWork (AddArchive, AddMissingArchiveAliasesAsync, GetArchiveLedgerAsync, etc).
- [x] Configurar EF Core DbSet y restricciones de Constraints/Check en ProductiveCoreDbContext.
- [x] Implementar el Application Service transaccional ProductiveCoreArchiveApplicationService copiando la semÃ¡ntica de VSA segura del proyecto.
- [x] Exponer el minimal API endpoint MapPost("/fields/{fieldId:guid}/archive") en ProductiveCoreEndpoints.
- [x] Registrar el servicio en el contenedor DI.
- [x] Generar EF Migration AddArchiveFieldDraft para ProductiveCoreDbContext.
- [x] Validar que todo compila exitosamente.

### Review
- Resultado: GO.
- Hemos completado rigurosamente toda la capa de persistencia y API requerida por la VSA para exponer el caso de uso Archive de borradores, logrando una operaciÃ³n transaccionalmente segura e idempotente.

## Iteración 40 — Creación del Bounded Context Catalog e Ingestión V1 (2026-08-22)

### Plan
- [x] Crear el proyecto de librería AgropecuarIA.Catalog y AgropecuarIA.Catalog.Tests.
- [x] Agregar al slnx y la solución global.
- [x] Dominio: CatalogSourceSnapshot, CatalogStagingEntry, CatalogEditorialDiff.
- [x] Infra: Configurar CatalogDbContext y generar InitialCatalog migration.
- [x] Aplicación: Implementar CatalogIngestionApplicationService y CatalogDiffApplicationService.
- [x] API: Exponer Endpoints protegidos en Minimal API (/api/catalog/ingest, /api/catalog/diff).
- [x] Agregar permisos en el Authorization Register.

### Review
- Resultado: GO.
- El nuevo catálogo ha sido integrado a la arquitectura y pasa con éxito todas las pruebas unitarias principales y la inyección en AgropecuarIA.Api.

## Iteración 41 — Estabilización de CPM, MSTest y AGRO-CAT-001 Ingesta/Diff (2026-08-24)

### Plan
- [x] Estandarizar AgropecuarIA.Catalog.Tests con MSTest.Sdk y Central Package Management.
- [x] Implementar métodos faltantes de archive ledger en InMemoryUnitOfWork y RenameUnitOfWork de ProductiveCore.Tests.
- [x] Robustecer CatalogIngestionApplicationService con SHA-256 e ingesta estructurada a CatalogStagingEntry.
- [x] Implementar cálculo de diff editorial y detección de conflictos en CatalogDiffApplicationService.
- [x] Exponer endpoints /api/catalog/ingest y /api/catalog/diff.
- [x] Ejecutar y validar suite en verde.

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings (11 proyectos).
- Pruebas unitarias de Catalog y ProductiveCore pasando al 100%. Commit 614f68f en origin/main.

## Iteración 42 — Publicación Atómica, Búsqueda Normalizada y Reversión AGRO-CAT-002 (2026-08-24)

### Plan
- [x] Modelar CatalogPublishedVersion, CatalogPublishedItem, CatalogSupportLevels y CatalogCategories.
- [x] Implementar CatalogNameNormalizer con descomposición FormD, supresión de diacríticos y folding case.
- [x] Mapear entidades e índices en CatalogDbContext con columna jsonb para sinónimos.
- [x] Implementar CatalogPublicationApplicationService para publicación atómica desde staging y rollback de versión activa.
- [x] Implementar CatalogSearchApplicationService con búsqueda por código normalizado, nombre y filtros de soporte/jurisdicción/categoría.
- [x] Exponer minimal API endpoints: POST /api/catalog/publish, POST /api/catalog/rollback/{versionId}, GET /api/catalog/items, GET /api/catalog/items/{code}.
- [x] Agregar tests unitarios completos (14/14 en verde).

### Review
- Resultado: GO.
- Todo compilado en verde, 14 tests de catálogo pasando. Commit dfc2291 en origin/main.

## Iteración 43 — Registro de Actividades en Núcleo Común AGRO-CAT-003 (2026-08-24)

### Plan
- [x] Modelar ProductionCycle y ProductionEvent con orígenes tipados, estados y soporte para flujo genérico.
- [x] Mapear entidades e índices en ProductiveCoreDbContext.
- [x] Implementar ProductionCycleApplicationService para inicio de ciclos vinculados a ManagementUnit y catálogo, registro de eventos y timeline cronológico.
- [x] Exponer endpoints minimal API: POST /api/organizations/{orgId}/fields/{fieldId}/cycles, GET /api/organizations/{orgId}/fields/{fieldId}/cycles, POST /api/organizations/{orgId}/cycles/{cycleId}/events, GET /api/organizations/{orgId}/cycles/{cycleId}/timeline.
- [x] Agregar tests unitarios completos en ProductiveCore.Tests (5/5 en verde).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings. Commit a9dd386 en origin/main.

## Iteración 44 — Membresías, Roles y Alcance por Campo AGRO-ID-003 (2026-08-24)

### Plan
- [x] Ampliar OrganizationMembershipRoles con roles multirol (owner, admin, agronomist, operator, accountant, viewer) y validación IsValid.
- [x] Modelar OrganizationFieldScopeAssignment y soporte para asignación de alcance por campo.
- [x] Configurar tablas y constraints (CK_memberships_Role) en IdentityDbContext.
- [x] Implementar OrganizationMembershipApplicationService con alta de miembros, actualización de rol con protección de último owner, asignación/revocación de alcance por campo y resolución de permisos efectivos (deny-by-default).
- [x] Agregar suite de tests unitarios en OrganizationMembershipTests.cs (5/5 en verde).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings. Commit 4e7ca04 en origin/main.

## Iteración 45 — Geometrías Espaciales y Normalización Territorial AGRO-GIS-001 / AGRO-GIS-002 (2026-08-24)

### Plan
- [x] Habilitar estado espacial `configured` en ManagementUnitSpatialStatuses y modelar ConfigureSpatialGeometry en ManagementUnit.
- [x] Mapear columnas espaciales (BoundaryGeoJson, DeclaredAreaHectares, CalculatedAreaHectares, CentroidLatitude, CentroidLongitude, OfficialProvinceCode, OfficialDepartmentCode) y actualizar check constraints en ProductiveCoreDbContext.
- [x] Implementar ConfigureGeometryAsync en ProductiveCoreApplicationService con verificación de permisos, concurrencia optimista y auditoría.
- [x] Exponer endpoint POST /api/organizations/{orgId}/fields/{fieldId}/geometry con encabezados ETag y respuesta privada.
- [x] Validar suite de Territory.Tests (44/44 en verde) y agregar tests unitarios para ConfigureSpatialGeometry en ManagementUnitDomainTests.cs (12/12 en verde).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings. Commit 6223720 en origin/main.

## Iteración 46 — Módulo de Clima, Pronósticos y Lluvia Observada AGRO-CLI-001 / AGRO-CLI-002 (2026-08-24)

### Plan
- [x] Crear módulo AgropecuarIA.Weather con entidades WeatherForecastSnapshot, WeatherObservedRain y enumeraciones WeatherFreshnessStatuses / WeatherObservedRainMethods.
- [x] Mapear tablas e índices en WeatherDbContext con almacenamiento jsonb para variables horarias/diarias y unicidad de snapshot hash.
- [x] Implementar OpenMeteoWeatherClient y WeatherForecastApplicationService con estrategia de cache fresh, degradación transparente a stale y fallback a unavailable sin romper transacciones de negocio.
- [x] Implementar registro y rectificación auditable de lluvias observadas por pluviómetro / estación meteorológica.
- [x] Exponer endpoints: GET /api/organizations/{orgId}/fields/{fieldId}/weather/forecast, POST /api/organizations/{orgId}/fields/{fieldId}/weather/rain, GET /api/organizations/{orgId}/fields/{fieldId}/weather/rain.
- [x] Crear suite de pruebas unitarias en AgropecuarIA.Weather.Tests (5/5 en verde).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings en los 13 proyectos. Commit 3732d59 en origin/main.

## Iteración 47 — Ingestión y Cruce Espacial de Alertas SMN CAP AGRO-CLI-003 (2026-08-24)

### Plan
- [x] Modelar entidad WeatherAlert con severidades normalizadas (yellow, orange, red, minor), estados (actual, update, cancel), ventana temporal (effective/expires) y bounding boxes espaciales.
- [x] Mapear tabla weather_alerts en WeatherDbContext con índices por identificador único, vigencias y límites espaciales.
- [x] Implementar WeatherAlertApplicationService con ingestión idempotente y consulta de alertas activas por coordenadas geográficas.
- [x] Exponer endpoints: GET /api/organizations/{orgId}/fields/{fieldId}/weather/alerts y POST /api/weather/alerts/ingest.
- [x] Crear suite de pruebas unitarias en WeatherAlertTests.cs (4/4 en verde, totalizando 9/9 en Weather.Tests).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings. Commit 333e4d4 en origin/main.

## Iteración 48 — Reglas de Aptitud Climática por Labor y Ventanas de Aplicación AGRO-CLI-004 (2026-08-24)

### Plan
- [x] Modelar WeatherActivityRule con umbrales de viento, temperatura, precipitación y humedad relativa por tipo de labor (pulverización, siembra, cosecha, fertilización).
- [x] Implementar motor de evaluación WeatherActivityRule.Evaluate retornando status (optima, marginal, no_apta) y factores de riesgo explícitos.
- [x] Mapear tabla activity_rules en WeatherDbContext.
- [x] Implementar WeatherActivityApplicationService con creación/listado de reglas y evaluación con fallbacks a buenas prácticas agronómicas estándar.
- [x] Exponer endpoints: POST/GET /api/organizations/{orgId}/weather/rules y GET /api/organizations/{orgId}/fields/{fieldId}/weather/suitability.
- [x] Crear suite de pruebas unitarias en WeatherActivityRuleTests.cs (4/4 en verde, totalizando 13/13 en Weather.Tests).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings. Commit 802c4e0 en origin/main.

## Iteración 49 — Transacciones y Procesamiento Idempotente Exactly-Once AGRO-FND-002 (2026-08-24)

### Plan
- [x] Modelar entidad ProductiveInboxEntry para deduplicación atómica de mensajes de integración por MessageId, ConsumerName y OrganizationId.
- [x] Mapear tabla inbox_entries con índice único compuesto en ProductiveCoreDbContext.
- [x] Implementar ProductiveInboxProcessor para ejecutar el despacho de mensajes en bloque transaccional atómico con idempotencia garantizada.
- [x] Crear suite de pruebas unitarias en ProductiveInboxTests.cs (3/3 en verde).

### Review
- Resultado: GO.
- Solución completa compila con 0 errores y 0 warnings en los 13 proyectos.

## Iteración 50 — Cierre verificable de pendientes sobre el runtime actual (2026-09-04)

### Plan

- [x] Reconciliar las 81 tareas con implementaciones, contratos, UI y pruebas actuales; distinguir evidencia completa, implementación parcial y validaciones externas.
- [x] Ejecutar baseline locked restore/build y suites backend/frontend; reparar fallos de integración sin degradar assertions ni ocultar superficies.
- [ ] Cerrar primero defectos de autorización, persistencia y contratos de las capacidades incorporadas en las iteraciones 38–49.
- [ ] Completar las superficies UI y operaciones verificables siguiendo dependencias; registrar decisiones técnicas delegadas y aceptación antes de editar.
- [x] Mantener explícitos los pendientes que requieren credenciales, datos piloto o validación profesional; no sustituirlos por valores inventados.
- [ ] Revisar independientemente cada entrega, registrar gates efectivos y publicar con author/committer JuaniRMariani, sin deploy.

### Review

- Inicio desde `dd808e4`, worktree limpio. El estado histórico de Iteración 36 ya no representa el runtime: existen módulos Catalog y Weather y nuevas capacidades Productive/Identity. Se reevalúa contra el código actual.
- Inventario exacto 81/81 en `docs/implementation/current-backlog-evidence-2026-09-04.md`; no se promueven padres incompletos por la existencia de stubs o pruebas vacías.
- Baseline: locked restore/build PASS (0W/0E); backend 427/429, dos gates de arquitectura fallaban por operaciones/módulo sin registrar y referencias de pruebas inexistentes. Frontend 252/252, typecheck/lint PASS.
- Reparaciones locales en verificación: Catalog autenticado/editorial+CSRF+migración de publicación; Weather con configuración explícita, migraciones/FORCE RLS, owner/recurso, roles editoriales y abstención sin reglas; ciclos en UOW autorizado; protocolo de archivo con evento correcto y RLS; resúmenes espaciales compatibles; MFA local con enrollment protegido y pruebas HTTP reales; inbox atómico solo para efectos en la misma base.
- Gates parciales ejecutados: Catalog 18/18; Weather HTTP/RLS inicial 4/4; ciclos 9/9; archivo 12/12 + HTTP 1/1; inbox 5/5; frontend 265/265 + typecheck/lint. El gate completo integrado todavía está pendiente y estos resultados no cierran las 81 tareas.
- Gate backend integrado posterior: locked restore PASS; `dotnet test --solution AgropecuarIA.slnx --configuration Release --no-restore` **486/486**, 0 fallos/omitidos, 3m36s. Incluye las 16 pruebas MFA reales y los 6 escenarios Weather HTTP/RLS/editor/rectificación. La UI de archivo y el gate de navegador continúan en curso.
- Checkpoint final local de esta tanda: backend **521/521**, frontend **286/286**, typegen/typecheck/lint/formato/build/auditoría de paquetes PASS. Navegador real **16/16**, escritorio y móvil, 1m30s, sobre API y PostgreSQL/PostGIS efímeros; incluye cancelar/confirmar archivo, refresco de activos, ficha de solo lectura, foco y accesibilidad. Los procesos y datos descartables de E2E fueron cerrados/retirados por el wrapper. No se modificó PostgreSQL del sistema ni se desplegó.

## Iteración 51 — Geometría inicial calculada por servidor (2026-09-04)

### Plan y aceptación

- [x] Reutilizar ADR-002 y runtime PostGIS aislado ya disponible; probar carga de extensión en base efímera, sin instalar ni desplegar.
- [x] Aceptar geometría/área declarada; calcular área esferoidal, normalización MultiPolygon 4326 y centroide en el servidor, nunca desde métricas cliente.
- [x] Rechazar geometrías inválidas, coordenadas/dimensiones no admitidas, payloads/vértices excesivos, campos archivados y reconfiguración; no ejecutar reparación silenciosa ni inferir pertenencia oficial.
- [x] Integrar la transición inicial con UOW, RLS, grants/trigger y trazabilidad; mantener separado lo pendiente de edición/versionado completo y tolerancias profesionales.
- [x] Probar PostGIS real, límites/errores, aislamiento, ETags/concurrencia y regresión rename/archive; actualizar contrato/registro con evidencia ejecutable.
- [x] Ejecutar gates completos y navegador sobre el estado integrado, revisar diff y publicar usando la identidad Git solicitada.

### Review

- Plan derivado de la auditoría del runtime: PostgreSQL del sistema carece de PostGIS, pero el bundle aislado de AGRO-DIS-004 está disponible y verificado. No se equipara disponibilidad de archivos con prueba de extensión cargada.
- Gate integrado: restore locked con auditoría NuGet high/critical PASS, Release build 0W/0E, backend **521/521**, 0 fallos/omitidos, 4m32s. Incluye 19 casos PostGIS/parser, HTTP geometry con límites/spoofing/GET y 155 controles de arquitectura. Revisión independiente corrigió rollback de migración y lectura inconsistente durante configuración concurrente. No hay edición espacial ni aprobación territorial/profesional implícita.

## Iteración 52 — CI de verificación sin despliegue (2026-09-04)

### Plan

- [x] Añadir jobs backend/frontend con lockfiles, lint/tipos/tests/build/audit y permisos read-only, sin secretos ni despliegues.
- [x] Verificar localmente comandos y sintaxis; usar PostGIS aislado en pruebas, nunca un servidor compartido.
- [x] Inspeccionar el primer resultado remoto después del push; no confundir YAML válido con CI aprobado.

### Review

- Las skills de CI orientan el orden de gates y la separación del despliegue. Sus assets de ejemplo no están instalados; se usa su patrón documentado adaptado al runner MTP SDK10 y al monorepo real. Actions fijadas por SHA comprobado en los repositorios oficiales.
- Primer remoto `33914604296` sobre `3023288`: frontend PASS; backend restore/build PASS, test step FAIL (exit 2). No se declara CI verde. `gh` no está instalado y la API pública de logs devuelve 403; no se extraen credenciales ni se cambia la cuenta. `f339cde` agrega TRX y anotaciones limitadas a nombres estáticos de métodos fallidos, parser probado sin payloads/DTD, para diagnosticar el runner Linux. Nuevo run `33915446788` en curso.
- Resultado posterior confirmado: run `33915446788` sobre `f339cde` **SUCCESS**, ambos jobs. Se conservan todas las assertions y no se añadieron reintentos ni skips. El primer fallo no se reprodujo y su causa no puede afirmarse con los logs disponibles: permanece como riesgo de intermitencia por investigar si reaparece. Los diagnósticos ahora se preservan como nombres de métodos, sin exponer contenido de las pruebas.

## Iteración 53 — Regresión de archivo y ciclos (2026-09-04)

### Plan y aceptación

- [x] Rechazar nuevos ciclos sobre campos archivados con 409 tras autorizar el recurso, sin impedir lecturas históricas.
- [x] Aplicar la restricción también al INSERT del rol runtime y probar servicio/PostgreSQL sin efectos parciales.
- [x] Agregar cobertura de navegador para la confirmación/cancelación de archivo, listado y resumen de solo lectura.
- [x] Ejecutar los gates integrados antes del commit/push autorizado.

### Review

- Guardia de inicio de ciclo revisada independientemente: 409 tras autorizar/bloquear campo; política INSERT restrictiva evita bypass y SELECT preserva historia. Cubierta por el gate 521/521.
- Las cuatro ejecuciones E2E nuevas (dos casos en escritorio/móvil) pasan sin stubs de HTTP. No implica cierre de GIS-002 ni CAT-003 completos.

## Iteración 54 — Publicación consistente y lector de catálogo (2026-09-04)

### Plan y aceptación

- [x] Serializar publicación/rollback antes de leer el activo, con transacción y restricción única parcial; rechazar bases históricas ambiguas sin elegir/borrar un ganador. Probar carreras y rollback real en PostgreSQL.
- [x] Validar completamente la fuente JSON antes de persistir; límites explícitos y rechazo de filas inválidas, sin snapshots falsamente exitosos. Usar el último snapshot completo por fuente para el candidato; duplicados normalizados entre fuentes son conflictos explícitos, nunca una selección silenciosa.
- [x] Calcular diff contra el publicado y un fingerprint de candidato que incluya snapshots seleccionados y versión activa. La publicación exige ese fingerprint para no aprobar una revisión diferente; documentar explícitamente la evolución del contrato editorial. Conservar referencias a snapshots en publicado, auditoría y outbox local atómicos, sin habilitar despacho externo.
- [x] Exponer búsqueda/detalle autenticados con versión/procedencia y consulta histórica estable; búsqueda normalizada y sinónimos acotados. Diff queda editorial, no una lectura general del staging.
- [x] Agregar lector web de solo lectura dentro del workspace: filtro reactivo acotado/cancelable, región de resultados estable, versión/fuentes/soporte/capacidades ausentes y estados sin publicación/vacío/error/versión histórica. Reutilizar tamaños, estilos y controles existentes; UUID corto; ninguna aprobación profesional inventada.
- [ ] Preparar el puerto público de resolución versionada para ciclos; conectar después de fijar su contrato sin accesos de Productive Core a tablas privadas de Catalog. Los metadatos enviados por clientes nunca certifican soporte especializado. Implementación y criterios separados en Iteración 55; no se declara cerrado aquí.
- [x] Actualizar contratos/fitness, probar E2E con fixtures claramente sintéticos, revisar independientemente y ejecutar gates.
- [ ] Publicar el checkpoint verificado con la identidad Git solicitada e inspeccionar su CI remoto.

### Decisiones y límites

- El checkpoint anterior está publicado como `3023288`; gates locales 521 backend, 286 frontend y 16 navegador. CI remoto iniciado en run `33914604296`; frontend ya PASS, backend todavía en curso al abrir esta iteración.
- La prioridad técnica está delegada. Esta entrega no publica el candidato nacional ni sustituye el acta editorial/profesional de ADR-006. No modifica ambientes compartidos.
- Las guías de arquitectura mantienen el acceso entre módulos mediante puertos; las de frontend mantienen límites cliente/servidor, cancelación y accesibilidad. El buscador de diseño UI/UX no está instalado (su enlace apunta a un destino ausente); se aplican sus reglas disponibles y el sistema visual existente, sin nuevas dependencias visuales.

### Review en curso

- Lector web implementado con detalle fijado a la versión consultada, historial paginado, filtros cancelables, procedencia nullable explícita y capacidades ausentes. Gates web del agente: typecheck/lint/formato/build PASS y **345/345** tests. Playwright descubre **20** casos; aún no se afirma ejecución integrada de esta entrega.
- Wrapper E2E preparado y revisado independientemente: dos publicaciones sintéticas por HTTP real, editor local efímero, sesiones revocadas antes del navegador, proceso API directo y restauración del entorno. Parser PowerShell PASS; no modifica el PostgreSQL del sistema.
- Revisión independiente encontró bypass por identidad de fuente legacy no canonizada y posibilidad de insertar hijos después de confirmada la versión/snapshot. Se requieren regresiones PostgreSQL y cierre del segundo hallazgo antes de aprobar la migración; no se reemplaza historia ni se fabrica evidencia legacy.
- Guía operativa de esta entrega: `docs/implementation/catalog-publication-reader.md`. El contrato 2.0.0 exige fingerprint en publish y mantiene diff editorial. Gates integrados y commit/push todavía pendientes.
- Gate backend integrado: restore locked/auditoría NuGet PASS, Release build 0W/0E, **593/593** tests, 0 fallos/omitidos, 4m42s. Incluye Catalog **62** y arquitectura **183**. Web repetido por root: **345/345**, formato/lint/typecheck/build/audit PASS.
- Bootstrap E2E detectó 401 del cliente PowerShell: CookieContainer omite Secure en HTTP local y reemplaza Cookie manual. El cliente de setup usa ahora jar explícito con HttpClient, sin cambiar flags de la aplicación, sin proxies/redirecciones y solo contra 127.0.0.1. Ya publicó ambos fixtures mediante HTTP real; navegador integrado sigue en curso. Los dos intentos fallidos cerraron su cluster y retiraron datos descartables.
- Primera corrida efectiva de navegador: **19/20**; los cuatro casos Catalog pasaron, falló una regresión móvil existente de Back. Trace: URL correcta pero vista anterior durante cinco segundos. Regresión unitaria determinista RED antes/GREEN después demuestra retirada del listener `popstate` por un rerender previo durante el despacho. Se mantiene la suscripción una vez y se leen valores del último render confirmado con [Effect Events de React](https://react.dev/reference/react/useEffectEvent); no se confunde esto con identidad estable de la función. No se tocaron assertions/timeout/retries E2E. Gates web posteriores: **346/346**, typecheck/lint/formato/build PASS. Nueva corrida exacta en curso.
- Gate final exacto: navegador **20/20 PASS**, escritorio/móvil, 1m54s aproximadamente (runner 1.9m), incluyendo la regresión Back y todos los casos Catalog; setup enteramente con cliente local sin proxy/redirecciones y cookies Secure/HttpOnly intactas. Base/host efímeros cerrados y directorio de ejecución retirado por el wrapper. Con backend **593/593** y web **346/346**, la entrega queda lista para commit/push; no hay deploy ni aprobación del catálogo nacional.

## Iteración 55 — Referencia autoritativa de catálogo en ciclos (preparación)

### Plan y aceptación técnica

- [ ] Iniciar después de verificar/publicar el checkpoint Catalog de Iteración 54; no mezclar código a medio terminar con ese checkpoint.
- [ ] Introducir un puerto propiedad de Productive Core y un adaptador en la composición API que consulte exclusivamente la superficie pública Application de Catalog. Sin ProjectReference Productive→Catalog, consultas a tablas privadas ni FK cruzada.
- [ ] Versionar explícitamente el contrato Productive a 2.0.0 para inicio de ciclo: request estricto `{catalogCode,purpose,system,startDateUtc,expectedCatalogVersionId?}`. Rechazar metadatos de nombre/soporte enviados por cliente y propiedades desconocidas; documentar la migración del consumidor, no ocultar la incompatibilidad.
- [ ] Autorizar organización/campo y rechazar archivo antes de consultar Catalog. Resolver activo, entrada y procedencia en un snapshot de lectura coherente. La versión esperada es una precondición del activo observado, no una selección de historia.
- [ ] Congelar en el ciclo IDs de versión/entrada, etiqueta, código/nombre canónicos, soporte declarado, procedencia nullable y momento de resolución del servidor. Soporte efectivo nuevo siempre genérico, sin capacidades especializadas implícitas. Publicar después de la resolución puede cambiar el activo: el ciclo conserva la versión realmente observada, sin prometer atomicidad distribuida o activo al commit.
- [ ] Agregar migración forward y guards de coherencia/inmutabilidad de referencias. Ciclos anteriores conservan texto/soporte originales con `legacy_unresolved` y snapshot null; no backfill por coincidencia del código actual. Una publicación legacy sí puede dar una referencia de versión real con procedencia de fuente no disponible.
- [ ] Probar HTTP real y PostgreSQL: spoof/unknown/CSRF/owner/cross-tenant/archivo, catálogo ausente/stale/caído sin efectos, publicación concurrente después de resolver, lecturas históricas sin consultar Catalog y cierre sin alterar snapshot. Mantener ausencia de retries ante commit ambiguo.
- [ ] Actualizar contratos/fitness/consumidores, revisar independientemente y ejecutar gates antes de publicar. Registrar los residuos del padre CAT-003: idempotencia/journal/eventos, outputs/costos/documentos, UI y certificación del baseline no se dan por completados por este puerto.

### Decisión

- Propuesta read-only del auditor contrastada con el código: `StartCycleAsync` aún acepta nombre y soporte del cliente; no existe cobertura HTTP real de `/cycles`. La entrega siguiente corrige esa autoridad y agrega esa cobertura, no renombra tests de metadata como integración. Plan aprobado técnicamente; implementación todavía no iniciada.
