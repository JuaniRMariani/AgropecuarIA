# Requisitos funcionales

Prioridad base: `Must` forma el producto operativo inicial; `Should` se planifica después de validar el piloto; `Could` no debe condicionar la arquitectura innecesariamente.

## Identidad y organizaciones

- `RF-ID-001` `Must`: registrar e iniciar sesión mediante email verificado.
- `RF-ID-002` `Must`: iniciar sesión con Google usando OpenID Connect y vincular cuentas sin duplicar usuarios.
- `RF-ID-003` `Must`: ofrecer passkeys WebAuthn; mantener email de recuperación y códigos de recuperación.
- `RF-ID-004` `Must`: permitir MFA TOTP compatible con aplicaciones como Google Authenticator o Authy. TOTP es segundo factor, no sinónimo de passkey.
- `RF-ID-005` `Must`: crear organizaciones, invitar miembros y asignar roles/alcances por campo.
- `RF-ID-006` `Must`: cerrar sesiones, revocar dispositivos y notificar cambios sensibles.
- `RF-ID-007` `Should`: delegaciones temporales y soporte consentido con caducidad.

## GIS, establecimientos y lotes

- `RF-GIS-001` `Must`: crear establecimientos y campos con dirección, coordenadas, titularidad y registros.
- `RF-GIS-002` `Must`: buscar ubicación, centrar por GPS y dibujar/editar polígonos en un mapa real.
- `RF-GIS-003` `Must`: calcular hectáreas y conservar por separado superficie declarada/calculada.
- `RF-GIS-004` `Must`: crear lotes/potreros con nombre, color, uso y vigencia.
- `RF-GIS-005` `Must`: seleccionar gráficamente un lote y abrir su ficha e historial.
- `RF-GIS-006` `Must`: versionar geometrías; subdividir/fusionar sin perder predecesores.
- `RF-GIS-007` `Must`: detectar geometrías inválidas y solapamientos según reglas configuradas.
- `RF-GIS-008` `Should`: importar/exportar GeoJSON y KML; Shapefile/ISOXML se validan por demanda.
- `RF-GIS-009` `Should`: capas de caminos, aguadas, corrales, silos, ambientes, suelo y vegetación.
- `RF-GIS-010` `Won't now`: captura y mapas offline quedan preparados como evolución futura; el MVP requiere conexión.
- `RF-GIS-011` `Must`: normalizar provincias, departamentos/partidos/comunas, municipios y localidades de todo el país mediante Georef u otra fuente oficial versionada.

## Catálogo y núcleo productivo nacional

- `RF-CAT-001` `Must`: consultar un catálogo nacional versionado de actividades, cultivos, especies, propósitos, sistemas y categorías productivas.
- `RF-CAT-002` `Must`: buscar por nombre oficial/científico, sinónimo regional y código, tolerando tildes y variaciones controladas.
- `RF-CAT-003` `Must`: mostrar fuente, versión, vigencia, jurisdicción y nivel `CATALOGADA`, `FLUJO_GENERICO` o `ESPECIALIZADA_VALIDADA`.
- `RF-CAT-004` `Must`: permitir una entrada privada/local y propuesta editorial sin modificar silenciosamente la línea base nacional.
- `RF-CAT-005` `Must`: publicar actualizaciones con altas, cambios, alias, deduplicaciones e inactivaciones, conservando códigos e históricos.
- `RF-PRD-001` `Must`: asignar cualquier entrada nacional a una unidad de manejo y ciclo productivo genérico.
- `RF-PRD-002` `Must`: registrar para toda actividad eventos, cantidades/unidades, insumos, productos, pérdidas, costos, documentos, responsables y timeline.
- `RF-PRD-003` `Must`: soportar unidades de manejo tipo lote, potrero, invernadero, rodal, apiario, galpón, corral, estanque, jaula, vivero y otras configurables.
- `RF-PRD-004` `Must`: habilitar formularios, KPI, normativa, algoritmos e IA específicos solo mediante un perfil versionado y aprobado.
- `RF-PRD-005` `Must`: informar capacidades especializadas ausentes sin bloquear el flujo común ni inferir cumplimiento regulatorio.

## Planificación y trabajo

- `RF-OPS-001` `Must`: crear campañas, planes, presupuestos y órdenes de trabajo.
- `RF-OPS-002` `Must`: asignar responsable, contratista, fecha, lote/rodeo, insumos y activos.
- `RF-OPS-003` `Must`: flujo borrador → aprobación → programada → ejecución → completada/cancelada.
- `RF-OPS-004` `Must`: cargar parte real con cantidades, horas, área, ubicación, clima observado/manual, fotos e incidencias; el clima API se vincula como snapshot separado con su origen.
- `RF-OPS-005` `Must`: al confirmar, generar movimientos de stock e imputaciones económicas idempotentes.
- `RF-OPS-006` `Should`: calendario, recordatorios, dependencias y cargas masivas.

## Agricultura

- `RF-AGR-001` `Must`: asignar cualquier cultivo/especie/mezcla del catálogo nacional, variedad, destino, antecesor y objetivo a unidad/campaña o ciclo.
- `RF-AGR-002` `Must`: registrar barbecho, labranza, siembra, fertilización, riego, pulverización y cosecha.
- `RF-AGR-003` `Must`: registrar semilla, densidad, dosis, producto/partida, maquinaria, operador y profesional.
- `RF-AGR-004` `Must`: monitoreos georreferenciados de fenología, emergencia, malezas, plagas, enfermedades y humedad.
- `RF-AGR-005` `Must`: registrar recomendación/prescripción y aprobación profesional cuando corresponda.
- `RF-AGR-006` `Must`: registrar cosecha, humedad, merma, rendimiento, calidad, pesaje y destino.
- `RF-AGR-007` `Must`: controlar producción almacenada por ubicación, partida y calidad.
- `RF-AGR-008` `Must`: comparar plan vs. real y calcular costos/margen por lote y campaña.
- `RF-AGR-009` `Should`: recetas agronómicas jurisdiccionales, mapas de rendimiento y dosis variable.
- `RF-AGR-010` `Could`: contratos forward, canjes, fijaciones y carta de porte.
- `RF-AGR-011` `Must`: admitir ciclos anuales/perennes, cultivos sucesivos/consociados y destinos múltiples sin reutilizar reglas especializadas entre perfiles.

## Ganadería

- `RF-GAN-001` `Must`: administrar todas las especies productivas de la línea base nacional y las extensiones autorizadas, con taxón, propósito, sistema, categoría y estado separados.
- `RF-GAN-002` `Must`: soportar el modo de seguimiento definido por perfil: individuo, rodeo/majada/hato, lote, galpón, colmena/apiario, lote acuático, biomasa u otra unidad validada.
- `RF-GAN-003` `Must`: registrar caravana visual/RFID, CUIG u otros identificadores sin reutilización cuando la especie/perfil los requiera.
- `RF-GAN-004` `Must`: altas, bajas, compras, ventas, nacimientos, muertes y movimientos internos/externos.
- `RF-GAN-005` `Must`: historial temporal de ubicación en lote/potrero.
- `RF-GAN-006` `Must`: pesadas/biomasa, condición corporal y cambio de categoría cuando apliquen al perfil.
- `RF-GAN-007` `Must`: servicios, inseminación, diagnóstico, parto, aborto, incubación u otros eventos reproductivos definidos por perfil.
- `RF-GAN-008` `Must`: plan sanitario, vacunación, enfermedad, tratamiento, dosis, partida y carencia cuando correspondan, sin prescripción automática.
- `RF-GAN-009` `Must`: alimentación, suplemento, pastoreo y fechas/días de ocupación y descanso planificados y reales.
- `RF-GAN-010` `Should`: importar lecturas RFID y conciliar con existencias/DT-e.
- `RF-GAN-011` `Must`: registrar potreros/pasturas, superficie aprovechable, recurso forrajero, agua e infraestructura.
- `RF-GAN-012` `Must`: permitir registrar opcionalmente mediciones de altura/biomasa, fecha, método, muestras y confiabilidad; su ausencia no bloquea el registro productivo.
- `RF-GAN-013` `Must`: con medición vigente, calcular oferta utilizable en materia seca; sin ella, mostrar únicamente rangos bajo/base/alto desde un perfil validado y rotulados como estimados.
- `RF-GAN-014` `Must`: calcular demanda diaria desde especie, categoría, cantidad, peso vivo, objetivo y consumo de materia seca definido por profesional.
- `RF-GAN-015` `Must`: planificar entrada, salida, días de ocupación y descanso de cada potrero, conservando el historial.
- `RF-GAN-016` `Must`: recomendar próximos potreros y permanencia mostrando nivel de evidencia, clima, supuestos, límites y alternativas; una fecha/capacidad exacta exige medición vigente.
- `RF-GAN-017` `Must`: sin forraje medido, priorizar inspección o escenarios estimados y abstenerse de declarar “listo”; sin agua confirmada o ante carencia, toxicidad o riesgo sanitario/climático, bloquear el ingreso.
- `RF-GAN-018` `Should`: registrar suplementos/reservas y descontar su materia seca de la demanda atribuible al pastoreo.

## Clima y contexto zonal

- `RF-CLI-001` `Must`: consultar por coordenadas pronóstico horario y diario de precipitación, probabilidad de lluvia, temperatura, humedad, viento/ráfagas y evapotranspiración de referencia cuando esté disponible.
- `RF-CLI-002` `Must`: mostrar proveedor, modelo/fuente, punto de grilla, fecha de emisión, última actualización, horizonte, unidades e incertidumbre.
- `RF-CLI-003` `Must`: guardar snapshots del pronóstico usado por cada alerta/recomendación para poder auditarlo y evaluar su precisión.
- `RF-CLI-004` `Must`: permitir registrar opcionalmente lluvia observada por pluviómetro o estación, separada de valores pronosticados/modelados; su ausencia no bloquea alta, clima ni operación.
- `RF-CLI-005` `Must`: generar alertas configurables por lluvia, helada, calor, viento fuerte, tormenta y déficit hídrico, sin presentar el pronóstico como certeza.
- `RF-CLI-006` `Must`: usar el centroide/representación del campo y permitir seleccionar otro punto cuando la extensión o relieve lo requiera.
- `RF-CLI-007` `Must`: cachear por ubicación/horizonte, respetar cuotas y mostrar estado degradado/último dato válido si el proveedor falla.
- `RF-CLI-008` `Must`: incorporar alertas oficiales del SMN mediante un mecanismo público/contractual confirmado.
- `RF-CLI-009` `Should`: cuando existan observaciones locales, comparar pronóstico y calcular error por campo/horizonte; de lo contrario mostrar evaluación regional disponible y “calibración local no disponible”.
- `RF-CLI-010` `Could`: integrar estación meteorológica/pluviómetro IoT y fuentes satelitales.

## Inventario y activos

- `RF-INV-001` `Must`: catálogo y depósitos para semillas, fertilizantes, fitosanitarios, medicamentos, alimento, combustible, repuestos y producción.
- `RF-INV-002` `Must`: existencias por partida, presentación, unidad, vencimiento y ubicación.
- `RF-INV-003` `Must`: recepción, consumo, transferencia, devolución, pérdida, ajuste y conteo.
- `RF-INV-004` `Must`: reservas contra órdenes y alertas de mínimo/vencimiento.
- `RF-INV-005` `Must`: conversión de unidades preservando cantidad/unidad original.
- `RF-ACT-001` `Must`: activos propios, alquilados, en leasing o provistos por contratista.
- `RF-ACT-002` `Must`: costo histórico, valor contable y valuaciones estimadas separadas.
- `RF-ACT-003` `Should`: horómetro/kilometraje, mantenimiento preventivo, fallas, repuestos y seguros.

## Gestión económica y exportación contable

- `RF-FIN-001` `Must`: terceros, compras, ventas, gastos, ingresos, recepciones/entregas y documentos de respaldo.
- `RF-FIN-002` `Must`: gastos e ingresos por caja y devengado, sujeto a decisión contable.
- `RF-FIN-003` `Must`: cuentas por pagar/cobrar, vencimientos, pagos/cobros y flujo proyectado básico.
- `RF-FIN-004` `Must`: imputar a campo, lote, campaña, actividad, rodeo, activo o administración.
- `RF-FIN-005` `Must`: moneda original, tipo de cambio, moneda de reporte y fuente de cotización.
- `RF-FIN-006` `Must`: presupuesto, comprometido, devengado y pagado/cobrado.
- `RF-FIN-007` `Must`: valorar activos/inventario con método, fecha, fuente y responsable visibles.
- `RF-FIN-008` `Should`: conciliación compra–recepción–documento y conciliación bancaria.
- `RF-FIN-009` `Won't now`: emisión de comprobantes electrónicos ARCA fuera del MVP.
- `RF-FIN-010` `Must`: adjuntar/importar documentos de respaldo por archivo/foto y revisión humana, sin pretender validez fiscal automática.
- `RF-FIN-011` `Must`: cierre mensual/campaña, bloqueo y reapertura auditada.
- `RF-FIN-012` `Must`: generar un paquete contable canónico, versionado y conciliable con movimientos, terceros, categorías, centros de costo, impuestos informados, monedas, cotizaciones, documentos y totales de control; el adaptador CSV/XLSX/software continúa pendiente hasta validar una muestra del contador.

## Documentos, analítica e IA

- `RF-DOC-001` `Must`: adjuntar fotos/PDF/archivos con hash, versión, autor y vínculo de negocio.
- `RF-DOC-002` `Must`: línea de tiempo y auditoría exportable por recurso.
- `RF-ANA-001` `Must`: tableros por rol y filtros por campo/lote/campaña/rodeo/período.
- `RF-ANA-002` `Must`: KPI con fórmula, unidad, período, fuente y datos faltantes visibles.
- `RF-ANA-003` `Must`: alertas de vencimiento, stock, desvío, atraso, sanidad, lluvia/tormenta y fecha sugerida de revisión/movimiento del rodeo.
- `RF-IA-001` `Must`: asistente de consulta sobre datos/documentos autorizados con citas internas.
- `RF-IA-002` `Must`: toda recomendación declara datos, fecha, supuestos, confianza y límites.
- `RF-IA-003` `Must`: aprobación humana antes de convertir una recomendación en acción.
- `RF-IA-004` `Must`: explicar pronóstico y riesgos meteorológicos relevantes para tareas agrícolas/ganaderas sin modificar el dato del proveedor.
- `RF-IA-005` `Must`: proponer rotación/pastoreo desde cálculos determinísticos de oferta/demanda y contexto meteorológico; el LLM solo explica y compara alternativas.
- `RF-IA-006` `Should`: escenarios avanzados de rendimiento, costos, caja y optimización forrajera multimes.
- `RF-IA-007` `Must`: feedback y evaluación posterior de utilidad/corrección de recomendaciones climáticas y ganaderas.

## Administración y portabilidad

- `RF-ADM-001` `Must`: catálogos configurables sin perder histórico.
- `RF-ADM-002` `Must`: importación inicial mediante plantillas validadas y reporte de errores.
- `RF-ADM-003` `Must`: exportación integral de datos y documentos por organización.
- `RF-ADM-004` `Must`: bandeja de integraciones servidoras con estados, reintentos y conciliación; no implica sincronización offline de dispositivos.
