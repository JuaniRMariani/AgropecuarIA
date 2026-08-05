# Visión y alcance

Producto: **AgropecuarIA**.

## Problema

La información de un establecimiento rural suele quedar fragmentada entre planillas, WhatsApp, cuadernos, sistemas fiscales, mapas, dispositivos, asesores y memoria de las personas. Esto dificulta responder preguntas básicas: qué hay en cada lote, qué se hizo, cuánto costó, qué stock existe, qué vencimientos se aproximan y qué resultado económico produce cada actividad.

## Visión

Crear una plataforma única, confiable y usable desde el campo que conecte el mapa, la operación agrícola y ganadera, el inventario, los activos, la gestión económica y la documentación. Sobre esa base trazable, una IA explicable ayuda a anticipar clima, estimar lluvias, planificar pastoreo y rotación, detectar desvíos y comparar escenarios.

## Resultado esperado

Un responsable debería poder seleccionar un lote en el mapa y entender, con permisos adecuados:

- su geometría vigente y superficie declarada/calculada;
- actividad actual y planificada;
- historial completo de labores, cultivos, rodeos y eventos;
- insumos y activos utilizados;
- costos, ingresos, documentos y margen;
- alertas y recomendaciones con evidencia;
- pronóstico de lluvia, temperatura, viento y riesgos meteorológicos con fuente, actualización e incertidumbre;
- estado de potreros, oferta/demanda forrajera y propuesta de próxima rotación con supuestos;
- quién creó, aprobó o corrigió cada dato.

## Objetivos de producto

1. Reducir la duplicación y pérdida de registros operativos.
2. Lograr trazabilidad desde una compra o partida de insumo hasta el resultado productivo y económico.
3. Mostrar rentabilidad y flujo de caja por organización, campo, lote, campaña y actividad.
4. Mejorar la oportunidad de decisiones mediante alertas, comparaciones y escenarios.
5. Simplificar la colaboración entre productor, profesionales, operarios y administración.
6. Ayudar al ingeniero agrónomo/productor a decidir cuándo realizar labores y cuánto tiempo ocupar/descansar potreros.
7. Preparar integraciones oficiales futuras sin incorporarlas al MVP ni automatizar portales de forma no admitida.

## Métricas de éxito propuestas

Estas metas deben validarse tras obtener una línea base:

- ≥ 90 % de lotes activos con geometría, uso y responsable vigentes.
- ≥ 85 % de labores/partes cargados dentro de 48 horas.
- ≥ 95 % de movimientos de stock críticos conciliados al cierre mensual.
- ≥ 90 % de eventos confirmados con evidencia y responsable.
- Reducción ≥ 50 % del tiempo para preparar un informe de campaña.
- ≥ 70 % de usuarios activos semanales entre roles operativos objetivo.
- ≥ 80 % de recomendaciones IA evaluadas por un humano; medir precisión/impacto por caso, no una métrica genérica.
- 100 % de entradas de `Catálogo Nacional v1` con flujo genérico aprobado o excepción explícita.
- Smoke territorial aprobado en las 23 provincias y CABA, informando degradaciones sin inventar cobertura.

## Alcance funcional objetivo

- Identidad, organizaciones, roles e invitaciones.
- Establecimientos, campos, lotes y capas GIS.
- Campañas, planificación y órdenes de trabajo.
- Agricultura y ganadería con historial temporal.
- Catálogo productivo nacional versionado y núcleo común para cultivos, animales, forestación, apicultura, acuicultura y otras actividades reconocidas.
- Depósitos, inventario, insumos y producción.
- Activos, uso y mantenimiento.
- Compras, ventas, comprobantes, tesorería y gestión económica.
- Meteorología por API, registro local de lluvias y contexto agroclimático por campo.
- Planificación de pastoreo, balance forrajero y recomendaciones de rotación ganadera.
- Exportaciones operativas/económicas para el contador.
- Tableros, alertas, documentos, auditoría y exportaciones.
- Asistente IA y modelos analíticos bajo control humano.

## Fuera del MVP salvo decisión explícita

- Contabilidad legal completa y liquidación impositiva sustituyendo al contador.
- Nómina y liquidación de sueldos.
- Marketplace de insumos o hacienda.
- Trading de granos, derivados, seguros o crédito automatizado.
- Control autónomo de maquinaria, riego, aplicaciones o tratamientos.
- Automatización por scraping de portales estatales.
- Predicciones agronómicas presentadas como certeza o sin procedencia.
- Automatización especializada no validada para cada tambo, feedlot, fruticultura, horticultura, forestación, apicultura, acuicultura o sistema de precisión; todos permanecen registrables mediante el flujo genérico.
- Emisión de comprobantes ARCA, liquidación fiscal y sincronización fiscal automática.
- Operación sin conexión; el MVP requiere conectividad activa.

## Supuestos iniciales

- Mercado y cobertura territorial: toda la República Argentina, normalizada mediante fuentes geográficas oficiales.
- Primer usuario de referencia: ingeniero agrónomo y productor.
- Producto B2B multiempresa y multicampo.
- El MVP incluye el catálogo nacional de actividades, cultivos y especies con flujo productivo común. Cada especialización se habilita mediante un perfil técnico/regulatorio versionado y validado.
- Web responsive/PWA online; no se implementa sincronización offline en el MVP.
- ARS y USD como monedas iniciales, preservando moneda original.
- La plataforma es sistema de gestión y evidencia; no reemplaza obligaciones ni criterio profesional.

## Dependencias de negocio

- Definir el segmento comercial inicial y los perfiles productivos que recibirán profundidad especializada primero; esto no limita el catálogo nacional.
- Conseguir datos reales anonimizados para validar flujos y migración.
- Designar referentes de agricultura, ganadería y administración.
- Relevar, cuando existan, pluviómetros/estaciones y mediciones de biomasa; su ausencia no bloquea el alta, clima ni registro productivo, pero reduce la precisión de la recomendación ganadera.
- Establecer presupuesto de mapas, clima, identidad e IA.
