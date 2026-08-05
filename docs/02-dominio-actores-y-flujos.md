# Dominio, actores y flujos

## Jerarquía de negocio

`Organización → establecimiento legal/sanitario → campo operativo → unidad de manejo → actividad/ciclo productivo → eventos`

Conceptos que no deben fusionarse:

- **Organización**: empresa, productor o grupo que posee datos y usuarios.
- **Establecimiento**: unidad legal/sanitaria asociada a titular, CUIT, domicilio y registros como RENSPA.
- **Campo**: agrupación operativa administrada como una unidad.
- **Parcela catastral**: referencia legal; puede no coincidir con un lote productivo.
- **Lote/potrero**: geometría de manejo con vigencia temporal.
- **Unidad de manejo**: lote, potrero, invernadero, rodal, apiario, galpón, estanque, corral, vivero u otra unidad apropiada para la producción; su geometría puede ser polígono, punto o no aplicar.
- **Actividad productiva**: entrada versionada del catálogo nacional que indica qué se produce, mediante qué sistema y con qué nivel de soporte.
- **Asignación productiva**: vínculo temporal entre actividad, sistema, unidad de manejo, perfil y responsable.
- **Campaña/ciclo**: período que agrupa plan, ejecución, producción, costos e ingresos.

## Actores

El actor primario del piloto es el **ingeniero agrónomo que también es productor**: planifica, ejecuta o supervisa, administra recursos y necesita decidir con datos técnicos y económicos sin alternar entre múltiples herramientas.

| Actor | Responsabilidad principal | Alcance típico |
|---|---|---|
| Propietario/representante | Titularidad, integraciones y decisiones críticas | Organización completa |
| Administrador | Usuarios, roles, catálogos y configuración | Organización/campos |
| Editor técnico de catálogo | Fuentes, taxonomía, perfiles, revisión y publicación | Catálogo nacional/perfiles asignados |
| Gerente/productor | Plan, presupuesto, aprobaciones y resultados | Campos asignados |
| Ingeniero agrónomo | Plan técnico, monitoreo, prescripciones y cosecha | Agricultura |
| Veterinario | Sanidad, tratamientos y reproducción | Ganadería |
| Responsable ganadero | Rodeos, movimientos, pesadas y pastoreo | Ganadería/campos |
| Encargado/capataz | Ejecución y partes desde campo | Campos asignados |
| Depósito/compras | Stock, recepciones, partidas y órdenes | Depósitos/campos |
| Administración/contador | Comprobantes, pagos, cierres y exportes | Finanzas/CUIT |
| Contratista | Órdenes y evidencia limitada | Trabajo asignado |
| Auditor/consultor | Consulta y exportación | Solo lectura |
| Soporte plataforma | Diagnóstico excepcional consentido | Temporal y auditado |

Los permisos se evalúan por organización, establecimiento/campo, módulo, acción y estado del registro. Las funciones sensibles deben permitir separación entre carga y aprobación.

## Flujos críticos

### Alta inicial

1. Crear organización y verificar identidad.
2. Configurar CUIT, monedas, unidades y zona horaria.
3. Invitar miembros y asignar alcance.
4. Crear establecimiento y registros oficiales.
5. Dibujar/importar campo y lotes.
6. Cargar saldos, inventario, activos y hacienda inicial.
7. Vincular integraciones.
8. Resolver inconsistencias y aprobar apertura.

### Flujo productivo común nacional

1. Buscar actividad, cultivo o especie por nombre oficial, científico, regional o código.
2. Ver fuente, vigencia y nivel `CATALOGADA`, `FLUJO_GENERICO` o `ESPECIALIZADA_VALIDADA`.
3. Asignarla a una unidad de manejo y seleccionar sistema, propósito y perfil disponible.
4. Registrar ciclo, eventos, cantidades/unidades, insumos, productos, costos, documentos y responsables.
5. Aplicar formularios/KPI/reglas específicos solo si el perfil elegido los valida.
6. Mantener timeline y exportación común aunque no exista automatización especializada.

### Campaña agrícola

1. Definir campaña, cultivo y objetivo por lote.
2. Preparar plan técnico y presupuesto.
3. Aprobar y generar órdenes.
4. Ejecutar partes con insumos, maquinaria, clima y evidencia.
5. Descontar stock e imputar costos.
6. Registrar monitoreos y decisiones.
7. Cosechar y almacenar/entregar producción.
8. Registrar venta, documento externo y cobro; la emisión ARCA queda fuera del MVP.
9. Cerrar campaña y comparar plan, ejecución y resultado.

### Rodeo/animal

1. Alta por nacimiento, compra, transferencia o inventario.
2. Identificar individualmente y/o agrupar en rodeo.
3. Confirmar agua/restricciones y, opcionalmente, medir superficie aprovechable y biomasa/altura; registrar clima vigente.
4. Con medición, calcular oferta/demanda observada; sin ella, mostrar escenarios estimados y solicitar inspección sin declarar capacidad o fecha exactas.
5. El productor confirma o modifica el plan; la recomendación por sí sola no mueve hacienda.
6. Registrar entrada/salida real, ocupación, descanso y remanente.
7. Registrar pesadas, reproducción, alimentación y sanidad.
8. Trasladar conservando historia y resultado de la recomendación.
9. Preparar venta/movimiento y documentación.
10. Conciliar salida, existencias, ingreso y cobro.

### Clima y decisión diaria

1. Resolver puntos representativos del campo/lote.
2. Ingerir pronóstico, alertas oficiales y corrida/modelo con timestamp.
3. Mostrar lluvia probable/esperada, temperatura, humedad, viento, ET₀ y alertas sin mezclar unidades ni fuentes.
4. Contrastar con lluvia manual/pluviómetro y estado observado del lote cuando existan; su ausencia deja la calibración local como no disponible.
5. Evaluar ventanas para labores y pastoreo mediante reglas determinísticas.
6. Explicar riesgos, alternativas y faltantes; el productor decide.
7. Conservar el snapshot y evaluar luego pronóstico vs. observado.

### Compra y consumo de insumo

1. Solicitud y aprobación.
2. Orden de compra.
3. Recepción por partida y vencimiento.
4. Conciliación con factura.
5. Ingreso a depósito.
6. Reserva y consumo desde una orden de trabajo.
7. Imputación a lote/campaña/rodeo/activo.
8. Pago y conciliación.

### Rectificación

Un registro confirmado no se elimina ni sobrescribe en silencio. Se crea una reversión o rectificación con motivo, usuario, fecha y vínculo al original. Los documentos fiscales siguen sus reglas específicas.

## Cadena de trazabilidad objetivo

`compra → partida → depósito → orden → aplicación/tratamiento → lote/animal → producción → almacenamiento → entrega → venta/comprobante → cobro`

Todo evento almacena fecha efectiva y fecha de carga, actor, origen, ubicación, cantidades/unidades, costo, documentos, estado de aprobación y versión.

## Glosario mínimo

- **Superficie declarada**: valor informado por el usuario o documento.
- **Superficie calculada**: área obtenida de la geometría y proyección definidas.
- **Rodeo**: agrupación operativa de animales que cambia en el tiempo.
- **Partida**: unidad trazable de un producto recibido o producido.
- **Centro de costo**: destino de imputación económica.
- **Valor estimado**: valuación de gestión con fecha, fuente y responsable; no equivale a valor contable.
- **Dato observado**: medido o cargado con evidencia.
- **Dato inferido**: calculado o predicho; siempre debe identificarse como tal.
