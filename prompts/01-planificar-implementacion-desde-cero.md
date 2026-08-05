# Prompt para una sesión nueva: generar el plan completo de implementación

Copiá y pegá desde la línea `INICIO DEL PROMPT` hasta `FIN DEL PROMPT` en una sesión nueva.

---

## INICIO DEL PROMPT

Trabajá como líder principal de un equipo senior encargado de transformar el discovery de **AgropecuarIA** en un backlog de implementación completo, trazable y ejecutable.

No contás con contexto de conversaciones anteriores. La única fuente de verdad inicial es el proyecto local:

`B:\Xenova\AgropecuarIA`

Respetá esta precedencia: instrucciones de sistema/desarrollador de la sesión → `AGENTS.md` aplicable → decisiones explícitas del sponsor registradas en el proyecto → documentación vigente → supuestos declarados. Nunca intentes anular una instrucción de mayor prioridad.

### Rol principal obligatorio

Asumí simultáneamente las responsabilidades de:

- Ingeniero/a Principal de Sistemas y Software con más de 20 años diseñando plataformas SaaS empresariales, sistemas distribuidos, GIS y soluciones de misión crítica.
- Arquitecto/a de Soluciones con más de 20 años en .NET, aplicaciones web modernas, PostgreSQL/PostGIS, seguridad, integraciones y observabilidad.
- Product Owner/Product Manager con más de 20 años convirtiendo necesidades ambiguas en alcance, épicas, historias, criterios de aceptación y releases medibles.
- Líder de QA con más de 20 años en estrategia de pruebas, automatización, riesgo, trazabilidad, pruebas de contrato, integración, E2E, seguridad y performance.
- Analista funcional senior con experiencia en sistemas agropecuarios, gestión económica, inventarios, trazabilidad y procesos regulados argentinos.
- Delivery/Program Manager con experiencia en dependencias, riesgos, planificación incremental y coordinación de equipos multidisciplinarios.

Tu responsabilidad es integrar el trabajo; no delegues la decisión final ni copies resultados de subagentes sin contrastarlos contra la documentación.

### Objetivo de esta sesión

Generar exclusivamente la **planificación detallada de implementación** de AgropecuarIA. Debés producir todos los archivos Markdown necesarios para que distintos equipos puedan comenzar a desarrollar mediante vertical slices, sin depender de esta conversación.

Audiencia: equipos senior de producto, arquitectura, desarrollo, QA, seguridad y plataforma que implementarán el sistema, y sponsor/especialistas que aprobarán alcance y reglas. Todos los entregables se escriben en español y UTF-8.

En esta sesión:

- No escribas código productivo.
- No generes scaffolding, pseudocódigo extenso, especificaciones ejecutables, SQL, migraciones, scripts, configuraciones, workflows, paquetes, Dockerfiles, YAML ni infraestructura.
- No inicialices Git, no hagas commit/push y no despliegues nada.
- No solicites ni conectes credenciales.
- No modifiques los requisitos originales para hacerlos más sencillos.
- Si detectás una contradicción, registrala como gap/pregunta/decisión pendiente; no la resuelvas silenciosamente.
- Solo podés crear o actualizar archivos `.md` de planificación bajo `tasks/`, salvo que una instrucción explícita del proyecto indique otro destino.

### Lectura obligatoria antes de planificar

1. Confirmá que existe `B:\Xenova\AgropecuarIA` y trabajá desde allí.
2. Inspeccioná de forma no mutante si existe Git y qué archivos/cambios previos hay; preservá todo trabajo del usuario.
3. Leé completamente `AGENTS.md`.
4. Leé completamente `README.md`.
5. Leé completamente `tasks/todo .md` y `tasks/lessons .md`.
6. Leé todos los archivos `docs/*.md` y `docs/adr/*.md`, incluyendo enlaces internos relevantes.
7. Inventariá archivos, requisitos `RF-*`, reglas `RN-*`, no funcionales `RNF-*`, preguntas `Q-*`, ADR, decisiones confirmadas y pendientes.
8. No empieces a escribir el backlog hasta terminar esa lectura y publicar en `tasks/todo .md` un plan verificable de esta sesión.

Si falta el directorio o no podés leer la documentación, detenete y reportá exactamente qué recurso falta. No reconstruyas requisitos desde memoria o conocimiento general.

### Contexto que debés confirmar desde los archivos

No lo tomes como reemplazo de la lectura; usalo como checklist:

- Producto SaaS multiempresa para producción agropecuaria argentina.
- Monolito modular ASP.NET Core/.NET, frontend Next.js/React/TypeScript y PostgreSQL/PostGIS.
- Web/PWA online; offline fuera del MVP.
- Identidad mediante email, Google OIDC, passkeys, TOTP/MFA y recuperación segura.
- Aislamiento estricto por organización y autorización por recurso.
- GIS, campos, lotes, unidades de manejo y geometrías versionadas.
- Catálogo productivo nacional versionado y niveles `CATALOGADA`, `FLUJO_GENERICO`, `ESPECIALIZADA_VALIDADA`.
- Agricultura, ganadería, inventario, activos, gestión económica, documentos, clima, forraje, rotación, analítica e IA explicable.
- Open-Meteo comercial propuesto, SMN CAP autoritativo y spike SMN WRF.
- Pluviómetro y biomasa opcionales; su ausencia reduce evidencia, no bloquea el registro. Los riesgos de agua/sanidad/toxicidad sí bloquean recomendaciones ganaderas.
- IA consultiva, fundamentada y bajo aprobación humana; ninguna función crítica depende del LLM.
- ARCA/facturación electrónica y modo offline fuera del MVP.
- Exportación contable mediante paquete canónico; software/formato del contador pendiente.
- Cobertura territorial argentina completa y perfiles especializados limitados a lo técnicamente validado.
- En tablas/tarjetas, UUID visibles abreviados a 6 caracteres mayúsculos sin guiones; no exponer el UUID completo salvo pedido explícito.

Si un punto difiere de los archivos, prevalecen los archivos y documentás la diferencia.

## Uso obligatorio de subagentes

Usá subagentes reales en varias olas, respetando el límite de concurrencia disponible. Una tarea concreta por subagente. No asignes dos agentes a editar el mismo archivo. Para evitar colisiones, los subagentes deben investigar, analizar y devolverte reportes; el agente principal integra y escribe los entregables finales.

Creá como mínimo estos frentes especializados:

1. **Producto y dominio agropecuario**  
   Profesional: Product Owner/Business Analyst AgroTech con al menos 15 años de experiencia.  
   Misión: mapear personas, journeys, catálogo nacional, procesos agrícolas/pecuarios, valor, alcance MVP, preguntas y reglas de negocio.

2. **Arquitectura y modelo de dominio**  
   Profesional: Principal Software/Solution Architect con al menos 20 años en SaaS y sistemas empresariales.  
   Misión: módulos, bounded contexts, contratos, dependencias, decisiones ADR, consistencia, concurrencia, idempotencia y secuencia de vertical slices.

3. **Backend, datos e integraciones**  
   Profesional: Staff .NET Backend/Data Engineer con al menos 15 años en ASP.NET Core, PostgreSQL/PostGIS, APIs e integraciones.  
   Misión: tareas de API, dominio, persistencia, migrations, outbox/inbox, jobs, archivos, proveedores externos, importación/exportación y pruebas de integración.

4. **Frontend, UX y accesibilidad**  
   Profesional: Staff Frontend/Product Designer con al menos 12 años en React/Next.js, sistemas complejos, responsive, mapas y WCAG.  
   Misión: arquitectura frontend, navegación, design system, formularios, tablas, mapas, estados vacíos/degradados, accesibilidad, performance y E2E.

5. **GIS, meteorología y datos geoespaciales**  
   Profesional: Ingeniero/a GIS/Geospatial y Weather Integration Lead con al menos 15 años.  
   Misión: PostGIS, geometrías versionadas, MapLibre, Georef, tiles, CAP, WRF, Open-Meteo, unidades, caché, precisión, degradación y QA geográfico nacional.

6. **IA, analítica y ciencia de datos**  
   Profesional: ML/AI Architect y Data Scientist con al menos 12 años en sistemas explicables y evaluaciones.  
   Misión: AI Gateway, RAG autorizado, herramientas determinísticas, datasets/evals, abstención, trazabilidad, monitoreo, drift, seguridad y control humano.

7. **QA y estrategia de pruebas**  
   Profesional: Principal QA/Test Architect con al menos 20 años.  
   Misión: pirámide de pruebas, matriz requisito-prueba, fixtures, property tests, contratos, integración, E2E, performance, resiliencia, accesibilidad, seguridad y gates.

8. **Seguridad, privacidad y cumplimiento**  
   Profesional: Application Security/Privacy Architect con al menos 15 años en SaaS multi-tenant.  
   Misión: threat model, authn/authz, BOLA, RLS, secretos, archivos, SSRF, prompt injection, auditoría, privacidad argentina, supply chain y tareas de mitigación.

9. **DevOps, plataforma y observabilidad**  
   Profesional: Platform/SRE/DevOps Lead con al menos 15 años.  
   Misión: entornos, CI/CD, contenedores, configuración, secretos, backups, restore, OpenTelemetry, SLO, runbooks, release/rollback y costos operativos.

10. **Validación profesional de dominio**  
    Profesionales representados: ingeniero/a agrónomo/a, médico/a veterinario/a y contador/a con al menos 15 años en producción/gestión agropecuaria argentina.  
    Misión: identificar qué reglas necesitan validación humana real, formular preguntas y gates; no inventar prescripciones agronómicas, veterinarias, sanitarias, fiscales o contables.

Instrucciones para cada subagente:

- Recibir alcance, archivos que debe leer, preguntas concretas y formato de respuesta.
- No editar archivos salvo asignación exclusiva explícita.
- Entregar: hallazgos, tareas propuestas, dependencias, criterios de aceptación, pruebas, riesgos, decisiones pendientes y referencias a `RF/RN/RNF/ADR`.
- Distinguir hechos documentados, inferencias y preguntas.
- Informar comandos/recursos consultados y limitaciones.

Después de cada ola, cruzá resultados entre agentes. Pedí una revisión final independiente de cobertura para detectar requisitos sin tarea, duplicaciones, secuencias imposibles o contradicciones.

Si la plataforma no permite crear subagentes reales, no simules que los utilizaste ni declares la orquestación completa: registrá el bloqueo y pedí dirección al usuario. Si la concurrencia es limitada, ejecutá las especialidades en tandas.

## Investigación externa

Buscá en Internet únicamente cuando un dato actual, integración, estándar o normativa deba verificarse. Usá fuentes primarias/oficiales, registrá URL y fecha de consulta, y no sustituyas una decisión pendiente del sponsor con una suposición.

Para tareas técnicas, priorizá documentación oficial de Microsoft/.NET, Next.js/React, PostgreSQL/PostGIS, OGC/MapLibre, OpenTelemetry, proveedores meteorológicos y organismos argentinos correspondientes.

## Estrategia obligatoria de planificación

1. Construí un inventario de alcance y una matriz de trazabilidad inicial.
2. Separá capacidades del MVP, posteriores y explícitamente fuera de alcance.
3. Identificá dependencias fundacionales y riesgos que requieren spike.
4. Organizá el trabajo por releases y épicas, siguiendo el roadmap documentado, pero corregí en el plan cualquier dependencia imposible y explicá por qué.
5. Dividí cada épica en vertical slices demostrables; evitá planes por capas aisladas que posterguen valor y pruebas hasta el final.
6. Para cada slice incluí contrato, backend, datos, frontend, autorización, tests, telemetría, documentación y migración/rollback cuando aplique.
7. Priorizá simplicidad: monolito modular y contratos explícitos; no agregues microservicios, Kubernetes ni event sourcing completo sin evidencia.
8. No crees una tarea por cada cultivo o especie. Diseñá tareas parametrizadas para catálogo, perfiles y pruebas sobre el baseline completo.
9. Toda capacidad especializada debe declarar actividad, perfil, versión, jurisdicción, especialista aprobador y comportamiento de abstención.
10. Cerrá con una auditoría de cobertura: cada `Must`, `RN` y `RNF` debe apuntar a una o más tareas y pruebas, o a una excepción explícita.

## Entregables obligatorios

Creá como mínimo:

1. `tasks/implementation-plan.md`  
   Objetivo, alcance/no alcance, supuestos, estrategia, secuencia, waves, dependencias maestras, hitos y criterios de finalización.

2. `tasks/backlog/00-index.md`  
   Índice navegable de releases, épicas y tareas, estado inicial, prioridad, tamaño relativo, dependencias y enlaces.

3. Un archivo por épica en `tasks/backlog/`, con nombres estables como `EPIC-01-identidad-tenancy.md`. Cubrí al menos:

   - fundación/arquitectura;
   - identidad, tenancy y autorización;
   - catálogo nacional y núcleo productivo;
   - GIS/territorio/unidades de manejo;
   - clima y alertas;
   - agricultura;
   - ganadería común;
   - forraje y rotación;
   - inventario y activos;
   - gestión económica y exportación contable;
   - documentos y auditoría;
   - IA/analítica;
   - frontend/design system/accesibilidad;
   - integraciones/importaciones;
   - seguridad y privacidad;
   - plataforma, CI/CD, observabilidad y operación;
   - QA transversal y release readiness.

4. `tasks/traceability-matrix.md`  
   Matriz `RF/RN/RNF/ADR → épica → tarea → prueba → release`. Incluí conteos de cubiertos, excepciones y faltantes.

5. `tasks/test-strategy.md`  
   Estrategia unitaria, property-based, integración, contrato, API, E2E, GIS, clima, IA evals, seguridad, accesibilidad, performance, resiliencia y restore.

6. `tasks/release-plan.md`  
   Releases, objetivos demostrables, dependencias, entrada/salida, riesgos, feature flags, migración, rollback y evidencia requerida.

7. `tasks/risk-register.md`  
   Riesgo, probabilidad, impacto, severidad, disparador, mitigación, contingencia, owner y tarea relacionada.

8. `tasks/decisions-and-gaps.md`  
   Decisiones confirmadas, ADR pendientes, contradicciones, spikes, preguntas al sponsor y bloqueos reales. Conservá explícitamente pendiente el formato del contador.

9. `tasks/team-workstreams.md`  
   Roles, responsabilidades, ownership de módulos/archivos, dependencias entre equipos y oportunidades seguras de paralelismo.

10. Una sección nueva en `tasks/todo .md` con checklist de esta sesión y revisión final basada en evidencia.

No reemplaces documentos existentes ni borres trabajo anterior. Si ya existe un archivo destino, integrá de manera mínima y preservá el contenido del usuario.

Evitá duplicar el mismo contenido en varios archivos: cada documento debe tener una responsabilidad canónica y enlazar al resto.

## Formato obligatorio de cada tarea

Cada tarea implementable debe contener:

- **ID estable:** `AGRO-<EPICA>-<NNN>`.
- **Título orientado a resultado.**
- **Release, épica, prioridad MoSCoW y tamaño relativo** (`XS/S/M/L/XL`; dividir `XL`).
- **Rol owner y colaboradores.**
- **Resultado/valor esperado.**
- **Historia de usuario o job-to-be-done.**
- **Alcance incluido.**
- **Fuera de alcance.**
- **Requisitos trazados:** `RF`, `RN`, `RNF`, ADR y preguntas relacionadas.
- **Precondiciones y dependencias.**
- **Contrato/API/eventos afectados.**
- **Modelo de datos, índices, migración y compatibilidad.**
- **Autenticación, autorización, tenant y auditoría.**
- **Comportamiento frontend:** responsive, loading, empty, error, stale/degraded, conflicto y accesibilidad.
- **Reglas de negocio e invariantes.**
- **Criterios de aceptación observables**, preferentemente Given/When/Then.
- **Casos negativos y bordes.**
- **Estrategia de pruebas:** unitarias, integración, contrato, E2E y no funcionales aplicables.
- **Observabilidad:** logs, métricas, trazas, alertas y dashboard/runbook cuando corresponda.
- **Seguridad y privacidad.**
- **Performance/capacidad y límites.**
- **Feature flag, rollout, migración, rollback y recuperación.**
- **Documentación que debe actualizarse.**
- **Comandos/evidencia de verificación esperados**, sin inventar scripts que el proyecto todavía no posee.
- **Definition of Ready específica.**
- **Definition of Done específica.**
- **Bloqueos/preguntas abiertas.**
- **Paralelizable:** sí/no y con qué tareas.

Una tarea no está lista si dice solamente “crear backend”, “hacer pantalla” o “agregar tests”. Debe ser verificable por un tercero sin reconstruir la intención.

## Reglas de priorización y dependencias

- `Must` documentado debe quedar en un release del MVP o tener excepción explícita.
- Fundaciones técnicas solo se adelantan si desbloquean vertical slices concretos.
- Autorización por recurso, aislamiento tenant, auditoría e idempotencia forman parte de cada slice; no son una fase final.
- Accesibilidad, observabilidad, seguridad y pruebas forman parte del DoD.
- Un proveedor externo requiere spike/contrato/fixtures/fallback antes de ser dependencia crítica.
- El LLM nunca realiza aritmética crítica ni mutaciones autónomas.
- Sin perfil especializado validado, usar flujo genérico y abstención; no copiar reglas entre producciones.
- El formato contable pendiente bloquea el adaptador específico, no el modelo canónico ni releases anteriores.
- Offline y ARCA deben figurar como `Won't now`, sin tareas encubiertas dentro del MVP.

## Quality gates de la planificación

Antes de finalizar:

1. Validá UTF-8, archivos no vacíos, enlaces Markdown internos y code fences.
2. Validá IDs únicos de épicas/tareas.
3. Validá que todas las rutas del índice existan.
4. Calculá cobertura de `RF`, `RN` y `RNF`; el objetivo es 100 % trazado o excepción justificada.
5. Detectá tareas `XL`, ciclos de dependencia, tareas sin criterios, tareas sin pruebas y tareas sin owner.
6. Verificá que no se haya generado código ni modificado discovery fuera del alcance.
7. Confirmá que todos los archivos creados/modificados autorizados son Markdown dentro de `tasks/`; cualquier desviación invalida la sesión.
8. Si existe Git, revisá `git status --short` y `git diff --check`; si no existe, registralo como no aplicable, no inicialices repositorio.
9. Pedí a un subagente QA/reviewer una auditoría final independiente y corregí hallazgos reales.
10. Registrá comandos, resultados, limitaciones y riesgos residuales en la revisión de `tasks/todo .md`.
11. Autoevaluá el resultado sobre 100 puntos: contexto 15, límites/no código 15, subagentes/roles 15, cobertura 20, calidad del backlog 20 y verificación/trazabilidad 15. No cierres con menos de 90/100 ni con un fallo crítico.

## Salida final de la sesión

Respondé en español y de forma ejecutiva con:

- resultado logrado;
- archivos creados/actualizados con enlaces locales;
- cantidad de releases, épicas, tareas, requisitos trazados y excepciones;
- quality gates ejecutados y resultados;
- decisiones pendientes y bloqueos que realmente necesitan al sponsor;
- confirmación explícita de que no se escribió código, no se hizo commit/push y no se desplegó infraestructura.

No finalices hasta que los entregables existan, estén enlazados, la matriz esté calculada y la revisión independiente esté resuelta.

## FIN DEL PROMPT
