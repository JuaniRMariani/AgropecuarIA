# EPIC-10 — Gestión económica y exportación contable

Objetivo: hechos económicos de gestión, cierres, KPI y paquete canónico, sin ARCA ni contabilidad legal. Núcleo mínimo en R2 y capacidad completa en R5.

<a id="agro-fin-001"></a>

## AGRO-FIN-001 — Imputar costo operativo mediante contrato mínimo

- **Release, épica, prioridad y tamaño:** R2 · EPIC-10 · Must · M.
- **Owner y colaboradores:** Finanzas; Operaciones, Inventario, Datos y QA.
- **Resultado/valor esperado:** labor/tratamiento imputa costo exactamente una vez sin esperar finanzas completas.
- **Historia/JTBD:** Como productor, quiero ver costo por destino desde el primer parte confirmado.
- **Alcance incluido:** borrador, confirmación y reversión de imputaciones; destinos campo/lote/campaña/actividad/rodeo/activo/administración y referencias a la fuente.
- **Fuera de alcance:** tesorería, cierre, impuestos y prorrateos complejos.
- **Requisitos trazados:** RF-FIN-004/006; RF-OPS-005; RN-FIN-001; RN-CORE-005/006.
- **Precondiciones y dependencias:** FND-002 y DIS-006 mínimo.
- **Contrato/API/eventos afectados:** imputar, revertir y consultar costos.
- **Datos, índices, migración y compatibilidad:** decimal, moneda, fuente, destino y clave de idempotencia.
- **Autenticación, autorización, tenant y auditoría:** autorización sobre el destino y auditoría.
- **Frontend:** costo en parte/resultado con estados pendiente, error y conflicto.
- **Reglas e invariantes:** una ejecución genera una única imputación; la reversión es explícita; se conserva la moneda original.
- **Criterios de aceptación:** Dado un reintento, cuando se confirma una labor, entonces existe una única imputación conciliada con la ejecución y el inventario.
- **Casos negativos y bordes:** moneda faltante, destino cerrado y fallo parcial.
- **Estrategia de pruebas:** integración, procesamiento exactamente una vez, concurrencia y BOLA.
- **Observabilidad:** imputaciones, fallos y conciliación.
- **Seguridad y privacidad:** rol financiero y autorización por recurso.
- **Performance/capacidad y límites:** objetivo de escritura RNF-PER-001.
- **Feature flag, rollout, migración, rollback y recuperación:** flag del núcleo; reversión y roll-forward.
- **Documentación:** contrato y semántica.
- **Comandos/evidencia esperados:** futuras pruebas de integración.
- **Definition of Ready:** destinos y semántica monetaria definidos.
- **Definition of Done:** criterio de costo por labor de R2 demostrado.
- **Bloqueos/preguntas:** Q-048 solo afecta la política contable más amplia.
- **Paralelizable:** sí, con el diseño de FIN-002.

<a id="agro-fin-002"></a>

## AGRO-FIN-002 — Registrar operaciones, documentos y tesorería separadas

- **Release, épica, prioridad y tamaño:** R5 · EPIC-10 · Must · L.
- **Owner y colaboradores:** Finanzas; Documentos, Inventario, Frontend, Contador y QA.
- **Resultado/valor esperado:** compras/ventas/gastos/ingresos, recepciones/entregas, pagos/cobros y vencimientos conciliables.
- **Historia/JTBD:** Como administración, quiero registrar un gasto/documento/pago sin confundir sus estados.
- **Alcance incluido:** terceros, operación, documento comercial, cuentas por pagar/cobrar, pago/cobro, política de caja/devengado y flujo de caja proyectado.
- **Fuera de alcance:** emisión ARCA y validación fiscal automática.
- **Requisitos trazados:** RF-FIN-001–003/008/010; RN-FIN-001; RF-FIN-009 excepción; Q-048.
- **Precondiciones y dependencias:** FIN-001, DOC-001, DIS-006.
- **Contrato/API/eventos afectados:** operación, documento, deuda, pago y conciliación; `PaymentRegistered`.
- **Datos, índices, migración y compatibilidad:** agregados y vínculos separados, fechas efectiva/de registro, estado y relaciones.
- **Autenticación, autorización, tenant y auditoría:** roles financieros, step-up para exportar y auditoría de acciones sensibles.
- **Frontend:** flujos, tablas adaptables a móvil, revisión documental y todos los estados.
- **Reglas e invariantes:** las capas son independientes; adjuntar un documento no implica validación fiscal; la versión de la política es explícita.
- **Criterios de aceptación:** Dada compra, cuando recibe documento/paga, entonces estados concilian sin fusionar hechos.
- **Casos negativos y bordes:** pago parcial, documento faltante, tercero duplicado y ambigüedad entre caja/devengado.
- **Estrategia de pruebas:** estados, integración, documentos, aislamiento multitenant y E2E.
- **Observabilidad:** vencimientos, conciliación y errores.
- **Seguridad y privacidad:** clasificación confidencial y fiscal/personal.
- **Performance/capacidad y límites:** importaciones asíncronas y paginación.
- **Feature flag, rollout, migración, rollback y recuperación:** piloto del núcleo financiero; rectificación/reversión.
- **Documentación:** flujos y advertencia de que no constituye validación fiscal.
- **Comandos/evidencia esperados:** futuras suites de pruebas.
- **Definition of Ready:** política Q-048 aprobada.
- **Definition of Done:** operación, documento y pago trazables.
- **Bloqueos/preguntas:** Q-048 y conciliación bancaria Should.
- **Paralelizable:** sí con FIN-003.

<a id="agro-fin-003"></a>

## AGRO-FIN-003 — Gestionar multimoneda, presupuesto y valuación visible

- **Release, épica, prioridad y tamaño:** R5 · EPIC-10 · Must · L.
- **Owner y colaboradores:** Finanzas; Activos, Analítica, Contador y QA.
- **Resultado/valor esperado:** presupuesto/comprometido/devengado/pagado y valores con moneda/método/fuente.
- **Historia/JTBD:** Como gerente, quiero comparar plan-real en ARS/USD sin perder moneda original.
- **Alcance incluido:** moneda original/de reporte, cotización, fecha y fuente; estados presupuestarios, valuaciones y regla/base versionada de costos indirectos.
- **Fuera de alcance:** método de inflación, fiscal o contable no aprobado.
- **Requisitos trazados:** RF-FIN-002/004–007; RN-CORE-006; RN-FIN-003/004; RN-ACT-001/002; Q-049–051.
- **Precondiciones y dependencias:** DIS-006, FIN-001/002, INV-003.
- **Contrato/API/eventos afectados:** presupuesto, cotización, valuación, imputación y KPI.
- **Datos, índices, migración y compatibilidad:** decimales, monedas ISO, fuente/versión de cotización y regla/base.
- **Autenticación, autorización, tenant y auditoría:** alcance financiero; cambios de política/cotización auditados.
- **Frontend:** fórmula, moneda, fuente, frescura y faltantes visibles; diseño responsivo.
- **Reglas e invariantes:** no usar punto flotante; valores estimado, contable e histórico separados; un faltante no equivale a cero.
- **Criterios de aceptación:** Dada una operación multimoneda, cuando se informa, entonces conserva el valor original y muestra cotización, fuente, fecha y fórmula.
- **Casos negativos y bordes:** cotización faltante, cero/negativo, política retroactiva, redondeo y períodos mixtos.
- **Estrategia de pruebas:** propiedades de decimales/redondeo, conciliación, autorización y revisión del contador.
- **Observabilidad:** cotizaciones faltantes/obsoletas y fallos de cálculo.
- **Seguridad y privacidad:** confidencialidad financiera.
- **Performance/capacidad y límites:** KPI y proyecciones dentro del objetivo definido.
- **Feature flag, rollout, migración, rollback y recuperación:** flags por versión de política; recalcular proyecciones sin alterar hechos.
- **Documentación:** fichas de fórmulas y políticas.
- **Comandos/evidencia esperados:** futuras pruebas de propiedades y conciliación.
- **Definition of Ready:** Q-049–051 aprobado.
- **Definition of Done:** cálculos reproducibles y aprobados.
- **Bloqueos/preguntas:** Q-049–051.
- **Paralelizable:** sí con FIN-002.

<a id="agro-fin-004"></a>

## AGRO-FIN-004 — Cerrar y reabrir períodos de forma auditada

- **Release, épica, prioridad y tamaño:** R5 · EPIC-10 · Must · M.
- **Owner y colaboradores:** Finance; Identity, Audit, Frontend y QA.
- **Resultado/valor esperado:** estabilidad de informes/exportes con reapertura controlada.
- **Historia/JTBD:** Como contador, quiero cerrar y que cualquier reapertura tenga permiso/motivo.
- **Alcance incluido:** cierre mensual/de campaña, validación, bloqueo, reapertura, versión y proyecciones afectadas.
- **Fuera de alcance:** cierre contable legal.
- **Requisitos trazados:** RF-FIN-011; RN-FIN-002; RN-CORE-004/009; Q-048.
- **Precondiciones y dependencias:** FIN-002/003 y step-up.
- **Contrato/API/eventos afectados:** close/reopen/estado; `PeriodClosed`.
- **Datos, índices, migración y compatibilidad:** estado/versión del período, motivo, actor y restricción de exclusión.
- **Autenticación, autorización, tenant y auditoría:** permiso privilegiado y step-up; evento append-only.
- **Frontend:** checklist, confirmación, estado bloqueado y conflictos.
- **Reglas e invariantes:** el cierre bloquea mutaciones; la reapertura exige un motivo visible.
- **Criterios de aceptación:** Dado un período cerrado, cuando se intenta modificarlo, entonces se bloquea; una reapertura autorizada lo habilita y queda auditada.
- **Casos negativos y bordes:** cierre/pago concurrentes, validación parcial y límite de zona horaria.
- **Estrategia de pruebas:** concurrencia, estados, autorización y E2E.
- **Observabilidad:** cierres, reaperturas e intentos bloqueados.
- **Seguridad y privacidad:** acceso privilegiado.
- **Performance/capacidad y límites:** validación de cierre asíncrona si fuera necesario.
- **Feature flag, rollout, migración, rollback y recuperación:** flag; la reapertura no es un rollback destructivo.
- **Documentación:** política de cierre y runbook.
- **Comandos/evidencia esperados:** futuras pruebas.
- **Definition of Ready:** calendario y política definidos.
- **Definition of Done:** bloqueo, reapertura y recuperación demostrados.
- **Bloqueos/preguntas:** Q-048.
- **Paralelizable:** no sobre el mismo período; sí entre tenants.

<a id="agro-fin-005"></a>

## AGRO-FIN-005 — Generar paquete contable canónico conciliable

- **Release, épica, prioridad y tamaño:** R5 · EPIC-10 · Must · L.
- **Owner y colaboradores:** Finanzas/Integraciones; Contador, Documentos, Frontend, QA y AppSec.
- **Resultado/valor esperado:** exporte versionado/totales/referencias sin falsa compatibilidad.
- **Historia/JTBD:** Como contador, quiero recibir paquete con hechos/totales/documentos para conciliar.
- **Alcance incluido:** movimientos, terceros, categorías, centros de costo, impuestos informados, monedas/cotizaciones, documentos, manifiesto, hashes, totales de control y auditoría de la ejecución.
- **Fuera de alcance:** adaptador específico hasta recibir una muestra; ARCA y liquidación fiscal.
- **Requisitos trazados:** RF-FIN-012; RN-FIN-005; RNF-PORT-001; Q-006/008/018/052/053.
- **Precondiciones y dependencias:** FIN-002–004, DOC-001/003, DIS-006.
- **Contrato/API/eventos afectados:** generar, consultar estado, descargar y verificar una ejecución de exportación.
- **Datos, índices, migración y compatibilidad:** versión de esquema, ejecución, versiones de entrada, totales de control e identificadores estables.
- **Autenticación, autorización, tenant y auditoría:** permiso financiero/de exportación y step-up; URL firmada de corta duración.
- **Frontend:** vista previa, totales, progreso, error, descarga y advertencia explícita de compatibilidad.
- **Reglas e invariantes:** los totales coinciden con la misma versión visible en UI; nunca se afirma validez fiscal ni compatibilidad con software.
- **Criterios de aceptación:** Dado un período cerrado, cuando se exporta, entonces los totales y referencias documentales concilian y el acceso queda registrado en auditoría.
- **Casos negativos y bordes:** documento/cotización faltante, reapertura concurrente, exportación grande y formato desconocido.
- **Estrategia de pruebas:** conciliación, multimoneda, asincronía/rendimiento, BOLA e importación real solo posteriormente.
- **Observabilidad:** ejecuciones, duración, errores, tamaño y descargas.
- **Seguridad y privacidad:** minimización, cifrado, vencimiento y ausencia de acceso cross-tenant.
- **Performance/capacidad y límites:** procesamiento asíncrono, RNF-PER-004 y cuotas de tamaño.
- **Feature flag, rollout, migración, rollback y recuperación:** `canonical-export`; flag separado para el adaptador; una nueva ejecución/versión no sobrescribe la anterior.
- **Documentación:** esquema, diccionario de datos, controles y advertencias.
- **Comandos/evidencia esperados:** futuras pruebas de conciliación y aprobación del contador.
- **Definition of Ready:** contrato canónico y políticas aprobados.
- **Definition of Done:** paquete conciliado; el formato permanece como gap explícito mientras no se defina.
- **Bloqueos/preguntas:** Q-018/052/053 bloquean únicamente el adaptador.
- **Paralelizable:** sí, con portabilidad después de estabilizar el esquema.
