# EPIC-09 — Inventario y activos

Objetivo: núcleo de partidas, movimientos y costos en R2; inventario completo y activos entre R3 y R5.

<a id="agro-inv-001"></a>

## AGRO-INV-001 — Gestionar partidas, movimientos y stock a fecha

- **Release, épica, prioridad y tamaño:** R2/R3 · EPIC-09 · Must · L.
- **Owner y colaboradores:** Inventario; Operaciones, Agricultura, Ganadería, Datos y QA.
- **Resultado/valor esperado:** stock reconstruible por partida/ubicación/unidad sin contador editable.
- **Historia/JTBD:** Como depósito, quiero recibir/consumir/transferir/ajustar existencias con trazabilidad.
- **Alcance incluido:** ítems/depósitos/partidas/presentación/unidad/vencimiento, recepción/consumo/transferencia/devolución/pérdida/ajuste/conteo.
- **Fuera de alcance:** integración proveedores y stock negativo implícito.
- **Requisitos trazados:** RF-INV-001–003/005; RN-INV-001–004; RN-CORE-005/007.
- **Precondiciones y dependencias:** FND-002/003 y catálogos comunes.
- **Contrato/API/eventos afectados:** movement/adjust/count/stock-at-date; `InventoryConsumed`.
- **Datos, índices, migración y compatibilidad:** libro de movimientos; índices por tenant, ítem, partida y ubicación; cantidad/unidad original, conversión y vencimiento.
- **Autenticación, autorización, tenant y auditoría:** depósito/destino, ajuste con motivo/aprobación.
- **Frontend:** movimientos/saldo/partida, responsive, estados/conflicto y UUID corto.
- **Reglas e invariantes:** saldo=sum movimientos; negativo solo política explícita/alerta; consumo crítico conserva destino.
- **Criterios de aceptación:** Dada una secuencia con reintentos, cuando se consulta el stock a una fecha, entonces el saldo coincide y un ajuste no borra el movimiento original.
- **Casos negativos y bordes:** concurrencia, unidad incompatible, vencimiento/carencia, fecha retroactiva y partida duplicada.
- **Estrategia de pruebas:** propiedades de conservación, integración, concurrencia, idempotencia, aislamiento tenant y E2E.
- **Observabilidad:** stock negativo/intentos/ajustes/conflictos/latencia.
- **Seguridad y privacidad:** BOLA/RLS y aprobaciones según política.
- **Performance/capacidad y límites:** 1.000 movimientos ≤2 min p95 en import objetivo; consultas indexadas.
- **Feature flag, rollout, migración, rollback y recuperación:** núcleo en R2 y expansión en R3; rectificación y reconstrucción desde movimientos.
- **Documentación:** movimiento/unidades/políticas.
- **Comandos/evidencia esperados:** futuras pruebas de propiedades, integración y rendimiento.
- **Definition of Ready:** movimientos, unidades y política de stock negativo definidos.
- **Definition of Done:** saldo reconstruible y procesamiento exactamente una vez demostrados.
- **Bloqueos/preguntas:** políticas por categoría.
- **Paralelizable:** sí con AGR-001; integración final AGR-002 no.

<a id="agro-inv-002"></a>

## AGRO-INV-002 — Reservar stock y alertar mínimos/vencimientos

- **Release, épica, prioridad y tamaño:** R3 · EPIC-09 · Must · M.
- **Owner y colaboradores:** Inventory; Operations, Analytics, Frontend y QA.
- **Resultado/valor esperado:** órdenes conocen disponibilidad y evitan doble asignación.
- **Historia/JTBD:** Como planificador, quiero reservar partidas y anticipar mínimos/vencimientos.
- **Alcance incluido:** reserva, liberación y consumo; políticas de mínimo y vencimiento; alertas y conflictos concurrentes.
- **Fuera de alcance:** compras automáticas.
- **Requisitos trazados:** RF-INV-004; RN-INV-002/004; RF-ANA-003; RF-OPS-002.
- **Precondiciones y dependencias:** INV-001 y AGR-001.
- **Contrato/API/eventos afectados:** disponibilidad, reserva, liberación y alertas.
- **Datos, índices, migración y compatibilidad:** reservas, estado, vencimiento y referencia a la orden; control de exclusión y concurrencia.
- **Autenticación, autorización, tenant y auditoría:** depósito/orden y liberación/override auditados.
- **Frontend:** disponibilidad, reservas, alertas y estados de dato obsoleto/conflicto.
- **Reglas e invariantes:** reservas no exceden política; carencia/vencimiento bloquea/advierte por perfil.
- **Criterios de aceptación:** Dadas dos órdenes, cuando compiten, entonces una reserva segura o conflicto, nunca sobreasignación silenciosa.
- **Casos negativos y bordes:** orden cancelada, lote vence, reserva expira y stock ajustado.
- **Estrategia de pruebas:** concurrencia, propiedades, integración con Operaciones y E2E.
- **Observabilidad:** reservas activas/vencidas, conflictos y stock bajo.
- **Seguridad y privacidad:** aislamiento tenant y alcance por depósito.
- **Performance/capacidad y límites:** objetivo transaccional e índices por definir y medir.
- **Feature flag, rollout, migración, rollback y recuperación:** flag de reservas; runbook de liberación y conciliación.
- **Documentación:** políticas y ciclo de vida de estados.
- **Comandos/evidencia esperados:** futuras pruebas de concurrencia.
- **Definition of Ready:** políticas y ciclo de vida de las reservas definidos.
- **Definition of Done:** ausencia de sobreasignación y alertas trazables.
- **Bloqueos/preguntas:** umbrales del piloto.
- **Paralelizable:** sí con INV-003.

<a id="agro-inv-003"></a>

## AGRO-INV-003 — Registrar activos y valuaciones separadas

- **Release, épica, prioridad y tamaño:** R3/R5 · EPIC-09 · Must · M.
- **Owner y colaboradores:** Assets; Finance, Operations, Contador y QA.
- **Resultado/valor esperado:** activos propios/alquilados/leasing/contratista y valores no confundidos.
- **Historia/JTBD:** Como administrador, quiero asignar activos y conservar costo/valor contable/estimado por separado.
- **Alcance incluido:** activo, propiedad, disponibilidad, costo histórico, valor contable y valuación de gestión con método, fuente, fecha y autor.
- **Fuera de alcance:** método contable inventado y mantenimiento Should.
- **Requisitos trazados:** RF-ACT-001/002; RF-FIN-007; RN-ACT-001/002; Q-050.
- **Precondiciones y dependencias:** DIS-006 y política contador.
- **Contrato/API/eventos afectados:** activo, valuación y referencia de uso.
- **Datos, índices, migración y compatibilidad:** series de valuación tipadas/versionadas, con moneda y fuente.
- **Autenticación, autorización, tenant y auditoría:** alcance financiero y por campo; cambios sensibles auditados.
- **Frontend:** ficha, series y uso; método, frescura y estados explícitos.
- **Reglas e invariantes:** tres series separadas; estimado siempre muestra método/fuente/autor.
- **Criterios de aceptación:** Dado activo con tres valores, cuando consulta, entonces no se sustituyen y cada uno conserva fecha/origen.
- **Casos negativos y bordes:** moneda/tasa faltante, activo contratista, revaluación retroactiva y cierre.
- **Estrategia de pruebas:** unitarias de dinero/tiempo, autorización, E2E y revisión del contador.
- **Observabilidad:** valuaciones obsoletas, errores y uso.
- **Seguridad y privacidad:** patrimonio confidencial.
- **Performance/capacidad y límites:** historial paginado.
- **Feature flag, rollout, migración, rollback y recuperación:** política de valuación versionada y rectificación auditable.
- **Documentación:** semántica y advertencia de que no constituye valuación fiscal.
- **Comandos/evidencia esperados:** futuras pruebas y conciliación.
- **Definition of Ready:** política Q-050.
- **Definition of Done:** separación aprobada por contador.
- **Bloqueos/preguntas:** Q-050.
- **Paralelizable:** sí con FIN-002.

<a id="agro-inv-004"></a>

## AGRO-INV-004 — Mantener activos y lecturas operativas

- **Release, épica, prioridad y tamaño:** R7 · EPIC-09 · Should · M.
- **Owner y colaboradores:** Assets; Operations, Inventory, Frontend y QA.
- **Resultado/valor esperado:** mantenimiento preventivo/fallas/repuestos/seguros priorizado por uso.
- **Historia/JTBD:** Como encargado, quiero registrar horómetro/falla/mantenimiento para reducir indisponibilidad.
- **Alcance incluido:** lecturas, plan y orden de mantenimiento, fallas, repuestos y seguros.
- **Fuera de alcance:** telemetría IoT y mantenimiento predictivo IA.
- **Requisitos trazados:** RF-ACT-003; Q-047.
- **Precondiciones y dependencias:** INV-003, Operaciones e inventario de repuestos.
- **Contrato/API/eventos afectados:** usage/maintenance/failure.
- **Datos, índices, migración y compatibilidad:** lecturas temporales, órdenes y repuestos.
- **Autenticación, autorización, tenant y auditoría:** alcance por activo y campo.
- **Frontend:** calendario, lista y formulario móvil con todos los estados y accesibilidad.
- **Reglas e invariantes:** la lectura debe ser monotónica o incluir motivo; el mantenimiento no modifica la valuación.
- **Criterios de aceptación:** Dado umbral, cuando lectura lo supera, entonces tarea se alerta y ejecución traza repuesto/costo.
- **Casos negativos y bordes:** retroceso del medidor, activo alquilado/de contratista y trabajo duplicado.
- **Estrategia de pruebas:** estados, propiedades, integración y E2E.
- **Observabilidad:** mantenimientos próximos/vencidos y fallas.
- **Seguridad y privacidad:** alcance por recurso y documentos.
- **Performance/capacidad y límites:** volumen de flota por medir.
- **Feature flag, rollout, migración, rollback y recuperación:** flag para R7; deshabilitar la programación conserva el historial.
- **Documentación:** políticas de mantenimiento.
- **Comandos/evidencia esperados:** pruebas futuras.
- **Definition of Ready:** prioridad de negocio y datos definidos.
- **Definition of Done:** capacidad Should aceptada sin crear dependencia para el MVP.
- **Bloqueos/preguntas:** Q-047/prioridad.
- **Paralelizable:** sí R7.
