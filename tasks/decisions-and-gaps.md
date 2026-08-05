# Decisiones, gaps y validaciones pendientes

Estado inicial: planificación de implementación, 2026-08-04. Este documento es el registro canónico de decisiones y bloqueos; el backlog solo enlaza los IDs de esta página.

## Criterio de precedencia

Se aplica: instrucciones de sesión → `AGENTS.md` → decisiones explícitas del sponsor → documentación vigente → supuestos declarados. Una contradicción no se resuelve por conveniencia: se registra y se somete al owner indicado.

## Decisiones confirmadas

| ID | Decisión | Fuente | Consecuencia de implementación |
|---|---|---|---|
| DEC-001 | Producto B2B SaaS multiempresa, multicampo y de cobertura argentina completa. | Q-001–Q-004 | `Organization` es tenant; cobertura territorial nacional y catálogo versionado. |
| DEC-002 | MVP web/PWA online; offline no se implementa ahora. | Q-009, RF-GIS-010, RNF-CON-001 | Sin colas locales ni mapas descargables; la UI evita prometer confirmación sin red. |
| DEC-003 | Monolito modular .NET, web Next.js/React/TypeScript, PostgreSQL/PostGIS, storage privado y worker. | ADR-001, ADR-002 | Contratos internos explícitos; no microservicios, Kubernetes ni event sourcing completo. |
| DEC-004 | Email verificado, Google OIDC, passkeys, TOTP/MFA y recuperación segura. | RF-ID-001–006, ADR-003 | IdP sustituible, sesión segura y step-up para acciones sensibles. |
| DEC-005 | Aislamiento estricto por organización y autorización por recurso. | RN-CORE-001, RNF-SEC-002 | Control de aplicación más RLS `default deny`; pruebas BOLA negativas en toda vía de acceso. |
| DEC-006 | Catálogo nacional versionado y niveles `CATALOGADA`, `FLUJO_GENERICO`, `ESPECIALIZADA_VALIDADA`. | Q-003–Q-004, ADR-006 | Todo baseline recorre el núcleo común; reglas específicas exigen perfil, versión, jurisdicción y aprobador. |
| DEC-007 | Pluviómetro y biomasa son opcionales; su ausencia reduce evidencia. | Q-007, RF-CLI-004, RF-GAN-012–013 | Clima y registro siguen; sin biomasa solo escenarios rotulados y pedido de inspección. |
| DEC-008 | Agua, carencia, toxicidad y riesgos sanitarios/climáticos bloquean recomendaciones de ingreso ganadero. | RF-GAN-017, RN-GAN-008, docs/13 | Abstención `SEGURIDAD_INSUFICIENTE`; no existe override automático. |
| DEC-009 | Open-Meteo comercial es primario propuesto, SMN CAP es autoritativo y SMN WRF requiere spike. | ADR-005 | Contratos/fixtures/fallback preceden dependencia crítica; proveedor final sujeto a viabilidad. |
| DEC-010 | IA consultiva, fundamentada, read-only inicialmente y bajo aprobación humana. | ADR-004, RN-IA-001–006 | Cálculos críticos determinísticos; ninguna mutación ni función crítica depende del LLM. |
| DEC-011 | Gestión económica operativa y paquete contable canónico; no contabilidad legal completa. | Q-005–Q-008 | El modelo/exporte canónico entra al MVP; el adaptador específico queda bloqueado por muestra real. |
| DEC-012 | ARCA/facturación electrónica queda fuera del MVP. | RF-FIN-009, Q-005 | No habrá tareas encubiertas de emisión, credenciales, homologación ni UI fiscal. |
| DEC-013 | Los UUID visibles en tablas/tarjetas son los primeros seis caracteres, mayúsculas y sin guiones. | RNF-UX-004, `AGENTS.md` | El UUID completo no aparece en modales, cards o columnas salvo pedido explícito. |

## ADR y decisiones de arquitectura

| ID | Estado documental | Acción requerida | Gate |
|---|---|---|---|
| ADR-001 | Propuesto | Ratificar monolito modular y reglas de dependencia antes de R1. | Revisión de arquitectura + prueba de límites. |
| ADR-002 | Aceptado para contrato de discovery; pendiente R2 | WGS84/SRID 4326, `ST_Area(geography)`, superficie declarada separada, validez y GiST demostrados por AGRO-DIS-004. | Revisar límites y tolerancias con telemetría/casos productivos. |
| ADR-003 | Propuesto | Elegir IdP y validar account linking/recovery antes del slice de identidad. | Spike contra tenant de prueba, sin credenciales reales. |
| ADR-004 | Propuesto | Ratificar gateway, control humano, retención y proveedor luego de threat model/evals. | Dataset y kill switch aprobados. |
| ADR-005 | Aceptado para contrato base; WRF postergado | Open-Meteo queda candidato condicionado, CAP autoridad fail-closed y WRF fuera del camino MVP hasta aprobar presupuesto/operación. | Plan/DPA/región/cuota y validación local siguen abiertos. |
| ADR-006 | Aceptado para discovery | Elevar a aceptado de implementación al aprobar baseline, gobierno y perfiles iniciales. | Comité editorial y especialistas firman matriz de soporte. |
| ADR-PEND-007 | Pendiente | RLS: política `default deny`, `FORCE RLS`, rol sin `BYPASSRLS`, contexto transaccional y operación de migraciones. | Suite tenant negativa y revisión de DBA/AppSec. |
| ADR-PEND-008 | Contrato R0 validado; proveedor/Legal pendientes | ADR-007 fija port, cuarentena fail-closed, grants breves, manifest y restore aislado; AWS es candidato condicionado y Azure alternativa. | Sandbox storage/AV real, región/DPA/subencargados, política de retención y aprobación `VAL-LEG`. |
| ADR-PEND-009 | Pendiente | Observabilidad y SLO: backend OTLP, backend de telemetría, cardinalidad/costos y seudonimización. | Prueba de carga y presupuesto operativo. |
| ADR-PEND-010 | Pendiente | Estrategia de compatibilidad de migraciones y rollback/roll-forward del monolito. | Ensayo en staging con backup y restauración. |
| ADR-PEND-011 | Pendiente | Ownership y ciclo de vida de `ManagementUnit` entre núcleo productivo y representación espacial, junto con dependencias permitidas entre módulos. | Revisión de arquitectura, mapa de consumidores y prueba de ausencia de ciclos/acceso a tablas ajenas. |

## Gaps, preguntas y decisiones pendientes

| ID | Preguntas relacionadas | Gap / decisión requerida | Bloquea | Owner / evidencia de cierre |
|---|---|---|---|---|
| GAP-001 | Q-012–Q-017 | Ubicación, producción, escala, lluvia/forraje y dataset anonimizados del piloto. | Selección/aceptación de perfiles especializados; no bloquea catálogo ni flujo genérico. | Sponsor + productor + agrónomo/veterinario; acta y dataset aprobado. |
| GAP-002 | Q-018, Q-052–Q-053 | Software, formato, columnas y totales que recibe el contador. | Solo adaptador específico; **no** bloquea paquete canónico ni R1–R4. | Contador; muestra importada y conciliada. |
| GAP-003 | Q-019–Q-020, Q-060 | Equipo, presupuesto, calendario, volúmenes, SLA y retención. | Estimación temporal, dimensionamiento y contrato de proveedores. | Sponsor/Delivery; supuestos de capacidad firmados. |
| GAP-004 | Q-021–Q-030 | AGRO-DIS-004 cerró contrato base y matriz técnica; siguen abiertos horizontes, umbrales, canales, plan Open-Meteo, tolerancia y autorización/presupuesto WRF. | Configuración productiva de alertas y WRF; no bloquea contrato base. | Sponsor/productor + Weather Lead; decidir con `tasks/evidence/AGRO-DIS-004/validation-report.md`. |
| GAP-005 | Q-031–Q-042 | Parámetros, recursos, restricciones, perfiles y aprobadores ganaderos. | `ESPECIALIZADA_VALIDADA` de pastoreo y recomendaciones; sin cierre solo flujo genérico/abstención. | Agrónomo + veterinario; perfiles versionados firmados. |
| GAP-006 | Q-043–Q-047 | Perfiles agrícolas y jurisdicciones iniciales; receta oficial, precisión y almacenaje avanzado. | Especialización agrícola; no bloquea ciclo/labor genéricos. | Agrónomo + sponsor; matriz perfil/jurisdicción. |
| GAP-007 | Q-048–Q-051 | Caja/devengado, moneda funcional, cotización, valuación, cierre y prorrateos. | KPI/valuaciones y cierre económico definitivo. | Contador + sponsor; política contable de gestión aprobada. |
| GAP-008 | Q-054–Q-055 | ICP comercial y propiedad/roles cuando propietario, productor y asesor difieren. | Packaging, contratos y algunos alcances de autorización. | Sponsor/Product/Legal; modelo contractual y permisos aprobados. |
| GAP-009 | Q-056–Q-059 | Tercer caso IA, acciones borrador, región/proveedor y definición de éxito. | Casos IA posteriores y proveedor productivo; no bloquea línea base transaccional. | Sponsor + AI/Product + privacidad; ficha de caso y evals. |
| GAP-010 | Q-061 | Conectividad real del piloto no medida. | Riesgo de adopción; offline sigue fuera del MVP. | Product/UX; medición y prueba de degradación online. |
| GAP-011 | Q-062–Q-066 | Owner editorial, cadencia, separación visible y profundidad de variedades/razas. | Publicación del Catálogo Nacional v1. | Sponsor + comité editorial; charter y baseline firmados. |
| GAP-012 | Q-010, Q-029 | Satélite, catastro y capas agronómicas no confirmados. | Solo capacidades `Should`; no bloquea GIS/clima MVP. | Sponsor/Product; decisión de R7. |

## Validaciones profesionales obligatorias

| Gate | Alcance | Regla de abstención | Evidencia |
|---|---|---|---|
| VAL-AGR | Perfiles de cultivo, fórmulas, umbrales, receta y jurisdicción. | Sin firma del agrónomo, solo flujo genérico; no se muestran prescripciones/KPI técnicos. | Perfil, versión, fuente, jurisdicción, casos y aprobación nominada. |
| VAL-VET | Especies/categorías, sanidad, carencias, bienestar y bloqueos. | Sin firma veterinaria no se automatizan indicaciones sanitarias ni tratamientos. | Matriz de riesgos, casos de abstención y aprobación nominada. |
| VAL-FOR | Oferta/demanda, biomasa, remanente, consumo, descanso y seguridad. | Sin perfil compatible o datos de seguridad se bloquea recomendación; sin medición no hay cifra exacta. | Dataset, cálculo reproducible y evaluación conjunta agrónomo/veterinario. |
| VAL-CON | Caja/devengado, cotización, valuación, cierres y exporte. | No se declara validez fiscal/contable ni compatibilidad de adaptador sin conciliación real. | Política de gestión y muestra importada aprobadas por contador. |
| VAL-LEG | Privacidad, contratos de proveedores, transferencias, retención y registro AAIP aplicable. | No producción con altas/críticas legales o de privacidad sin tratamiento aceptado. | Dictamen/registro y DPA/contratos. |

## Excepciones explícitas de trazabilidad

| ID | Requisitos | Tratamiento |
|---|---|---|
| EXC-001 | RF-GIS-010 | `Won't now`: offline, mapas descargables y sincronización de dispositivos no tienen tarea MVP. Reconsiderar solo por decisión del sponsor. |
| EXC-002 | RF-FIN-009 | `Won't now`: emisión/sincronización ARCA no tiene tarea MVP. |
| EXC-003 | RN-FIS-001–007 | Reglas futuras conservadas como restricciones de diseño; no se implementan mientras ARCA esté fuera de alcance. |

## Contradicciones y aclaraciones registradas

- `README.md` dice que el sistema debe funcionar con conectividad rural inestable, mientras Q-009 y RF-GIS-010 excluyen offline. No es contradicción funcional: el MVP debe comunicar fallas y evitar duplicados, pero no conservar mutaciones locales ni sincronizarlas.
- ADR-003 menciona email OTP alternativo y RF-ID-001 habla de email verificado; el mecanismo exacto de login por email depende del IdP y se valida en el spike, sin degradar los requisitos de verificación/recuperación.
- El roadmap ubica el núcleo de catálogo en R1 y exige en R3 probar el 100 % del baseline. El plan mantiene publicación/flujo mínimo en R1 y reserva certificación paramétrica completa e integración económica para R3.
- R2 pretende registrar una labor sin duplicar inventario/costo antes del desarrollo completo de inventario/economía. Se planifica un ledger mínimo transaccional en R2 y se amplía en R3/R5; no se posterga idempotencia.
- SMN WRF aparece en la matriz como “MVP tras spike”, pero su viabilidad está pendiente. R0 decide si entra al MVP; Open-Meteo + CAP y degradación cubren el contrato obligatorio mientras tanto.

## Política de supuestos

Los supuestos de capacidad, proveedor, perfil o regulación deben incluir owner, fecha de vencimiento y tarea de validación. Un supuesto vencido convierte el slice afectado en no listo; nunca habilita reglas especializadas por defecto.

## Cierre operativo de Q-062–Q-066 para AGRO-DIS-001 — 2026-08-04

El sponsor/owner del producto se identificó como usuario solicitante y delegó al equipo ejecutor las decisiones operativas seguras de `AGRO-DIS-001`. El cierre aplica al baseline y su gobierno; no sustituye validaciones agronómicas, veterinarias o regulatorias de perfiles especializados.

| Pregunta | Decisión | Accountable / responsable | Revisión |
|---|---|---|---|
| Q-062 | El sponsor/owner es accountable; WS-03 Product/Catalog Lead opera el catálogo. Conflictos regulados o especializados requieren especialista competente antes de elevar soporte. | Sponsor/owner · WS-03 | Por publicación. |
| Q-063 | Publicación ordinaria trimestral y actualización extraordinaria ante cambio oficial crítico, corrección de seguridad o error material. | WS-03 | Trimestral; evento crítico inmediato. |
| Q-064 | La UI y los contratos separan visiblemente `CATALOGADA`, `FLUJO_GENERICO` y `ESPECIALIZADA_VALIDADA`. | Sponsor/owner · WS-03 | Gate de publicación y QA. |
| Q-065 | Cobertura nacional significa registro y flujo común; la automatización técnica/regulatoria se habilita solo mediante perfil versionado y validado. | Sponsor/owner · WS-18 cuando aplique | Por perfil/jurisdicción. |
| Q-066 | Variedades, razas y líneas se sincronizan o crean bajo demanda desde fuentes oficiales; v1 no promete precarga exhaustiva. | WS-03 | Según demanda y fuente. |

Cada conflicto o excepción registra identificador, fuente y versión, entradas afectadas, tipo, motivo, decisión, aprobador, fecha y estado. Una excepción no eleva el nivel de soporte ni habilita reglas especializadas. Con estas decisiones, `GAP-011` deja de bloquear el inicio de `AGRO-DIS-001`; su cierre definitivo exige el baseline reproducible y la revisión registrada en la propia tarea.

### Estado de cierre técnico de GAP-011 — 2026-08-04

El baseline reproducible `1.0.0-candidate.1`, su contrato, gobierno, prototipo R0 y gates técnicos quedaron implementados y aprobados por revisión independiente. `GAP-011` permanece abierto únicamente para la firma nominada del agrónomo, veterinario y responsable editorial sobre la semántica del baseline y su acta; esa autoridad externa mantiene `AGRO-DIS-001` en `En revisión` y no habilita perfiles especializados ni una publicación productiva.

## Defaults operativos para AGRO-DIS-003 — 2026-08-05

El sponsor delegó al líder la selección de tarea y los defaults técnicos reversibles. Para ejecutar el spike R0 sin convertir supuestos en política productiva se adoptan estas decisiones:

| Entrada | Default del spike | Revisión pendiente no bloqueante para R0 |
|---|---|---|
| Q-054 | `Organization` es tenant operativo; productor, empresa o estudio usan el mismo contrato técnico. Cada cliente de un asesor conserva una organización separada. | Packaging/ICP comercial. |
| Q-055 | La organización controla operativamente recursos y acceso; membresías delegan alcance sin transferir datos. | Propiedad/controlador contractual, Organization↔CUIT y derechos entre partes: Product/Legal. |
| Q-060 | Entorno descartable, datos sintéticos y objetivos hipotéticos 99,9 %, RPO 15 min/RTO 2 h; sin SLA ni retención contractual. | SLA, soporte, retención, región y DPA: Sponsor/Delivery/Privacy/Legal. |
| Soporte JIT | Apagado y fuera de alcance; no existe acceso implícito de soporte. | `AGRO-ID-005` R7 con consentimiento, step-up, scope, caducidad y auditoría. |
| IdP | Auth0 es candidato preferido condicionado; ZITADEL y AWS Cognito quedan comparadores. | Tenant sandbox, precio/plan, DPA/región, exportabilidad, failover y contrato real. |

Estos defaults cierran la Definition of Ready del spike, no ADR-003 ni ADR-PEND-007. La tarea debe producir evidencia de linking/recovery/contexto tenant/RLS y puede terminar `En revisión` o `Bloqueada` si el test real del proveedor o una aceptación externa siguen pendientes.

### Resultado técnico de AGRO-DIS-003 — 2026-08-05

La implementación aislada confirmó contexto tenant server-side, autorización default-deny, `FORCE RLS`, pool/job fail-closed, linking con doble reautenticación/step-up y recovery con challenge one-shot. Principal QA y AppSec/Arquitectura aprobaron los gates internos sin hallazgos altos/críticos.

ADR-003 y ADR-PEND-007 no se cierran: persisten sandbox OIDC/PKCE y failover reales, contrato/región/DPA/plan/SLA/exportabilidad, identidad externa one-to-many persistida con unicidad concurrente y discovery productivo seguro de membresías. Estas condiciones mantienen `AGRO-DIS-003` en `En revisión` y no impiden usar el resultado para diseñar el siguiente sandbox; sí impiden producción.

## Defaults operativos para AGRO-DIS-005 — 2026-08-05

El sponsor delegó al líder la selección y los defaults técnicos reversibles. Para ejecutar el spike R0 sin convertir supuestos en política legal o contractual se adoptan estas decisiones:

| Entrada | Default del spike | Revisión pendiente no bloqueante para R0 |
|---|---|---|
| Clasificación | El contrato evalúa `Público`, `Interno`, `Confidencial`, `Fiscal/personal` y `Secreto`; solo las cuatro primeras pueden persistirse y `Secreto` se prueba como rechazo. Todo fixture es sintético. | Clasificación definitiva por tipo documental: Privacy/Legal/Product. |
| Q-060 | RPO ≤15 min, RTO ≤2 h y drill trimestral se tratan como hipótesis medibles; no son SLA ni retención contractual. | Volumen, SLA, soporte, retención y presupuesto: Sponsor/Delivery/Privacy/Legal. |
| Proveedor | Comparar AWS S3 + GuardDuty, Azure Blob + Defender y una opción S3-compatible con scanner separado; el port permanece independiente. | Región, DPA, subencargados, residencia, costo, soporte y contrato real. |
| Retención | Legal hold prevalece sobre purga; no se fijan plazos legales. El spike prueba únicamente estados y controles con datos sintéticos. | Política y plazos nominados: Privacy/Legal. |
| Antivirus | `clean` es el único resultado que habilita descarga; amenaza, unsupported, acceso denegado, timeout o fallo permanecen en cuarentena. | Scanner/plan/SLA/límites y prueba sandbox real. |

`Q-058` está redactada para proveedores internacionales de IA/clima, no para object storage. Su referencia desde `AGRO-DIS-005` se registra como gap de trazabilidad y no como autorización para transferencias internacionales de archivos. Estos defaults cierran la Definition of Ready del spike aislado, no `ADR-PEND-008`, `GAP-003` ni `VAL-LEG`.

### Resultado técnico de AGRO-DIS-005 — 2026-08-05

El spike aislado validó el contrato fail-closed de archivo, aislamiento tenant/recurso, purga ambigua reconciliable, manifest integral y restore local conjunto PostgreSQL/PostGIS + objetos + auditoría. Principal QA y AppSec/Arquitectura aprobaron los gates internos sin hallazgos altos/medios. AWS S3 + GuardDuty queda como candidato preferido condicionado para sandbox y Azure Blob + Defender como alternativa; esto no constituye selección contractual.

`ADR-PEND-008`, `GAP-003` y `VAL-LEG` permanecen abiertos: faltan storage/AV/KMS/WORM/PITR cloud real, región/DPA/subencargados, retención, volumen/costo/SLA y RPO administrado. Estas condiciones mantienen `AGRO-DIS-005` en `En revisión` y bloquean producción, no la conservación del contrato R0 como evidencia.

## Defaults operativos para AGRO-DIS-007 — 2026-08-05

La delegación del sponsor permite fijar rangos reversibles para el spike R0, no sustituir medición, contratación ni aprobación profesional. Cada hipótesis vence el 2026-09-30 y debe revalidarse antes de comprometer una release.

| Entrada | Rango/default del spike | Revisión pendiente no bloqueante para el laboratorio R0 |
|---|---|---|
| Q-019 | 1–3 carriles de ejecución; roles mínimos Product/Delivery, Backend, Frontend, QA, SRE/AppSec y especialistas on-demand. Costo externo de este laboratorio: USD 0. Sin fecha de release. | Personas nominadas, disponibilidad, throughput, presupuesto total y calendario: Sponsor/Delivery. |
| Q-020 | `pilot`: 10 tenants/100 usuarios/20 concurrentes; `growth-10x`: 10×; `burst-2x`: 2× sobre growth con pico 10×. Documentos, jobs y requests se versionan en fixtures sintéticos. | Distribución y crecimiento observados, tenant caliente, documentos/retención y concurrencia reales: Product/SRE. |
| Q-060 | 99,9 % mensual, RPO 15 min y RTO 2 h como targets de ingeniería; precios unitarios faltantes devuelven NO-GO. | SLA, soporte, retención, región, precios/impuestos/egress y presupuesto: Sponsor/Delivery/FinOps/Privacy/Legal. |
| Q-061 | Perfiles sintéticos 150/300/600 ms y offline; el laboratorio bloquea confirmación sin red y no persiste trabajo offline. | RTT, throughput, pérdida, operador, dispositivo y cortes medidos en campo: Product/UX. |
| Telemetría | Allow-list de dimensiones acotadas y prueba contra cardinalidad/PII. | SDK/exporter/backend y presupuestos observados en el slice productivo: SRE/AppSec. |

Estos rangos satisfacen la DoR del artefacto R0 porque tienen owner, fuente sintética y vencimiento. No cierran `GAP-003`, `GAP-010`, RSK-022/024/027 ni autorizan fechas, gasto, infraestructura, offline o un SLA productivo. La tarea termina como máximo `En revisión` hasta que esos inputs reales y su aceptación nominada reemplacen las hipótesis.

### Resultado técnico de AGRO-DIS-007 — 2026-08-05

El spike aislado validó schemas y semántica fail-closed, proyecciones exactas fixture→motor→golden, error budget, costos incompletos, cardinalidad acotada y experiencia online/default-deny. Principal QA reprodujo todos los gates y AppSec/Arquitectura cerró sus hallazgos con cero severidades residuales internas.

ADR-008 queda aceptada para discovery, no para producción. `GAP-003`, `GAP-010` y Q-019/020/060/061 permanecen abiertos hasta reemplazar estimaciones por equipo/volumen/precio/red reales y aceptación nominada. Estas condiciones mantienen `AGRO-DIS-007` en `En revisión` y prohíben fechas o presupuesto falsos.
