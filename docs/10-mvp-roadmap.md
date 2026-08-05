# MVP, backlog y roadmap

## Estrategia

El MVP de AgropecuarIA incluye una línea base nacional de actividades, cultivos y especies sobre un núcleo común de unidades de manejo, ciclos, eventos, inventario y gestión económica. Clima y rotación ganadera son capacidades centrales. La primera versión es online y no emite comprobantes ARCA.

Toda entrada del `Catálogo Nacional v1` tendrá flujo genérico de punta a punta. La profundidad se declara por nivel: `CATALOGADA`, `FLUJO_GENERICO` o `ESPECIALIZADA_VALIDADA`. El piloto prioriza perfiles especializados; no limita qué producciones pueden registrarse.

## MVP operativo confirmado

### Must

- Organización, usuarios, roles, Google/email, passkey/MFA y auditoría.
- Establecimientos/campos/lotes/potreros con mapa, hectáreas, historial y selección gráfica.
- Cobertura territorial de las 23 provincias y CABA mediante códigos oficiales y smoke tests nacionales.
- Catálogo nacional versionado de actividades, cultivos, especies, sistemas, categorías y productos.
- Núcleo productivo para lotes, potreros, invernaderos, rodales, apiarios, galpones, corrales, estanques, viveros y otras unidades configurables.
- Web responsive/PWA que requiere conexión.
- Campañas, planes, órdenes y partes agrícolas.
- Registro de todas las especies productivas nacionales con modo de seguimiento apropiado; perfiles especializados iniciales para herbívoros de campo y pastoreo.
- Potreros, mediciones de forraje, agua, oferta/demanda, ocupación, descanso y rotación.
- API meteorológica, alertas oficiales, carga opcional de lluvia manual y snapshots de pronóstico.
- Recomendaciones explicables de lluvia/riesgo y rotación con abstención cuando falten datos.
- Inventario por partida, consumos, activos e imputación de costos.
- Compras, gastos, ingresos, cuentas por pagar/cobrar y multimoneda básica.
- Documentos de respaldo y exportación conciliada al contador.
- Tableros y asistente read-only con evidencia.

### Should

- Mantenimiento de activos.
- Satélite/NDVI y capas agronómicas.
- RFID por archivo/dispositivo priorizado.
- Conciliación bancaria y compra–recepción–documento.
- Estaciones meteorológicas/IoT.
- Integraciones SENASA/SISA/DTV-e/CPE según mecanismo oficial y sistema productivo.

### Won't now

- Emisión o sincronización ARCA.
- Contabilidad legal/impositiva integral.
- Modo offline, mapas descargables o sincronización de dispositivos.
- Microservicios/Kubernetes.
- Automatización de portales por scraping.
- Formulación automática de raciones, dosis veterinarias o decisiones autónomas.
- Reglas/KPI/IA especializados para entradas sin perfil técnico y jurisdiccional validado; su flujo genérico sí forma parte del MVP.

## Releases que componen el MVP

### R0 — Discovery y spikes

- **R0A Catálogo nacional:** inventariar fuentes CNA/SENASA/INASE/INV/SAGyP/Georef, congelar baseline v1, definir taxonomía, soporte, excepciones, owner editorial y matriz de cobertura.
- **R0B Piloto:** elegir, con el productor de referencia y un segundo productor, qué perfiles recibirán profundidad especializada primero.
- Validar flujos con el ingeniero agrónomo/productor de referencia y un segundo productor.
- Registrar la ubicación/cultivos/sistemas reales del piloto para seleccionar perfiles y datos, no para limitar el catálogo.
- Prototipo GIS de lotes/potreros/versiones/área.
- Spike Open-Meteo comercial, SMN CAP y procesamiento SMN WRF.
- Relevar opcionalmente pluviómetro/estación, pasturas, medición de biomasa y reglas actuales de rotación.
- Diseñar paquete contable canónico; formato/software del contador permanece pendiente y solo bloquea el adaptador final.
- Spike OIDC/passkey/recovery.

Criterio de salida: baseline y matriz de soporte aprobados, perfiles iniciales elegidos, proveedor meteorológico viable, contrato contable canónico y dataset de prueba. El formato específico del contador puede seguir pendiente.

### R1 — Fundación

- Identidad, tenancy, roles y auditoría.
- CI/CD, entornos, observabilidad, backups y secretos.
- Catálogos, archivos, importación/exportación base.
- Motor de catálogo/versionado, núcleo productivo común y publicación con diff/rollback.
- Pruebas automatizadas de aislamiento.

Criterio de salida: dos organizaciones operan sin acceso cruzado y la restauración está probada.

### R2 — GIS, clima y trazabilidad

- Establecimientos, campos, lotes, potreros y geometrías versionadas.
- Georef y matriz de smoke tests para las 24 jurisdicciones.
- WeatherProvider, pronóstico, lluvia manual, CAP oficial y frescura.
- Campañas, órdenes, partes, documentos y timeline.

Criterio de salida: el piloto dibuja/subdivide lotes, consulta clima trazable y registra una labor sin duplicar inventario/costo.

### R3 — Agricultura

- Flujo productivo genérico probado contra el 100 % de `Catálogo Nacional v1`.
- Plan de campaña y cultivos.
- Siembra, aplicaciones, monitoreos, cosecha y producción almacenada.
- Inventario e imputación económica por lote/campaña.
- Alertas meteorológicas para ventanas de trabajo.

Criterio de salida: todas las entradas completan el flujo común; los perfiles agrícolas especializados completan plan/labor hasta producción, costo e historial sin contaminación entre cultivos.

### R4 — Ganadería y rotación

- Rodeos/animales, ubicaciones, movimientos, pesadas y sanidad básica.
- Modos genéricos para especies/lotes/colonias declarados; ninguna producción se fuerza al modelo de rodeo.
- Recursos forrajeros, mediciones, perfiles y restricciones.
- Motor determinístico de oferta/demanda, días máximos, descanso y déficit.
- Recomendaciones con clima, alternativas, confianza y decisión humana.

Criterio de salida: flujo pecuario común completo; perfiles pastoriles iniciales con recomendación reconstruible, escenarios estimados sin biomasa y bloqueo por riesgos de seguridad.

### R5 — Gestión económica y contador

- Compras/ventas/gastos/ingresos y documentos.
- Presupuesto, caja/devengado, multimoneda, cierres y KPI.
- Paquete contable canónico con totales de control, schemas y auditoría; perfil/mapeo específico cuando el contador entregue muestra.

Criterio de salida: paquete canónico conciliado contra la UI. La compatibilidad con software externo solo se declara después de una importación real aprobada por el contador.

### R6 — IA y piloto integral

- Explicaciones meteorológicas y de rotación sobre datos estructurados.
- Informes de campaña y alertas por excepción.
- Evals de clima, groundedness, abstención y seguridad.
- Piloto estacional, feedback y kill switch.

Criterio de salida: especialistas aprueban las evaluaciones, los usuarios comprenden límites y el sistema funciona sin depender del LLM.

## Épicas

| Épica | Valor | Dependencias |
|---|---|---|
| E01 Identidad segura | acceso y colaboración | IdP/tenancy |
| E02 Organización y campos | propiedad/alcance | E01 |
| E02A Catálogo/núcleo productivo | cobertura nacional | fuentes oficiales/gobierno editorial |
| E03 GIS versionado | mapa e historia | E02/PostGIS/tiles |
| E04 Clima y alertas | anticipación | E03/proveedores |
| E05 Agricultura | trazabilidad de campaña | E03/E04 |
| E06 Ganadería | stock e historia | E03 |
| E07 Forraje y rotación | alimentación/pastoreo | E04/E06 |
| E08 Inventario/activos | recursos/costos | E05–E07 |
| E09 Gestión económica | resultado/caja | E08 |
| E10 Exportación contable | colaboración contador | E09/formato acordado |
| E11 Analítica/IA | explicación y escenarios | E04–E10 |
| E12 Integraciones agro | menos duplicación | factibilidad externa |

## Primeras historias verticales

1. Como owner, creo AgropecuarIA para mi organización, activo passkey e invito a un colaborador.
2. Como productor, dibujo un campo y lotes/potreros, veo área calculada y justifico la declarada.
3. Como productor, busco cualquier producción nacional, veo su soporte y registro un ciclo/evento/costo mediante el flujo común.
4. Como agrónomo, planifico y confirmo una labor; se consume una partida y se imputa costo una sola vez.
5. Como productor, veo lluvia, probabilidad, temperatura, viento y alertas con fuente/corrida/frescura aunque no tenga pluviómetro.
6. Como productor, si dispongo de un pluviómetro registro su lluvia sin mezclarla con pronóstico/modelo.
7. Como responsable ganadero, con medición recibo alternativas cuantitativas; sin biomasa veo escenarios estimados y pedido de inspección.
8. Como productor, rechazo o ajusto una sugerencia; luego confirmo aparte el movimiento real.
9. Como administración, cargo un gasto/documento, lo imputa a campaña/actividad y registra el pago.
10. Como contador, recibo un paquete canónico con totales conciliados; el adaptador se configura al conocer mi software.
11. Como usuario, pregunto por una recomendación y la IA cita perfil, clima, mediciones y fórmulas sin inventar faltantes.

## Definition of Ready

- Actor, problema, valor y alcance definidos.
- Reglas, errores, roles y estados aclarados.
- Contrato/datos/migración considerados.
- Criterios observables y dataset de prueba.
- Riesgos de seguridad, clima, bienestar e integración identificados.
- Diseño móvil y estados degradados validados.
- Fuente/proveedor, perfil técnico y responsable de aprobación disponibles.

## Definition of Done

- Slice completo frontend/API/datos/telemetría.
- Pruebas proporcionales al riesgo y trazabilidad actualizada.
- Accesibilidad, autorización y aislamiento revisados.
- Migración/rollback y documentación actualizados.
- Métricas/runbook cuando corresponda.
- Sin vulnerabilidades altas/críticas nuevas.
- Aprobación funcional del referente y evidencia del quality gate.

## Estimación

No se fija calendario hasta confirmar equipo, escala, perfiles especializados prioritarios y presupuesto. Provincia, cultivos y mediciones del piloto afinan esos perfiles pero no limitan el catálogo. El formato del contador afecta el adaptador final, no R1–R4.

## Backlog posterior

Offline, ARCA, fiscalidad sectorial, app nativa, sensores/IoT avanzados, agricultura de precisión y nuevos perfiles especializados se priorizan con evidencia de uso. Las producciones sin especialización permanecen disponibles mediante el flujo genérico.
