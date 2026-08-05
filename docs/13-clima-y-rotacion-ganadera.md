# Clima y rotación ganadera

## Objetivo

Ayudar al ingeniero agrónomo/productor a responder:

- ¿Cuándo puede llover y con qué incertidumbre?
- ¿Qué labores tienen riesgo por lluvia, helada, calor o viento?
- ¿Qué potrero está en condiciones de recibir el rodeo/majada/tropilla?
- ¿Cuántos días puede permanecer allí sin bajar del remanente objetivo?
- ¿Cuándo debe revisarse o podría volver a utilizarse?
- ¿Existe déficit de materia seca que requiere cambiar el plan o consultar suplementación?

La decisión cuantitativa debe ser determinística, reproducible y auditada. La IA explica y compara; no inventa variables ni mueve animales.

## Meteorología del MVP

### Fuentes

| Fuente | Uso | Resolución/horizonte orientativo | Decisión |
|---|---|---|---|
| Open-Meteo comercial | REST JSON operativo | ECMWF ~9 km/15 días; GFS hasta 16 | Primaria propuesta |
| SMN CAP | Alertas oficiales argentinas y polígonos | vigencia del mensaje | Autoritativa para alertas |
| SMN WRF | Modelo regional oficial | 4 km, horario, 72 h | Fallback tras spike NetCDF |
| Pluviómetro/estación propia | Lluvia observada local opcional | punto del campo | Prioridad si existe; no bloquea clima |
| NASA POWER/IMERG | Histórico/estimación complementaria | ~10–50 km según producto | Contexto, no medición |
| INA/INTA/estaciones | Observación complementaria | cobertura variable | Según disponibilidad/licencia |

Precedencia para lluvia observada:

`pluviómetro del campo > estación cercana con distancia > satélite/INA > análisis de modelo > pronóstico`

La cadena comienza en la primera fuente disponible. Sin observación propia se informa “calibración local no disponible”.

### Variables mínimas

- precipitación total, lluvia y probabilidad de precipitación;
- temperatura mínima/máxima/horaria;
- humedad relativa;
- viento y ráfagas;
- ET₀ y radiación cuando estén disponibles;
- alertas oficiales SMN;
- opcional modelado: humedad/temperatura de suelo, rotulado como estimado.

### Contrato de dato

Cada valor guarda:

- proveedor/modelo/corrida;
- `issued_at`, `valid_at`, `ingested_at` y zona horaria;
- coordenada solicitada y punto/celda resuelta;
- resolución espacial/temporal;
- valor, unidad y acumulación/intervalo;
- naturaleza `observed | estimated | forecast`;
- estado `fresh | stale | unavailable`;
- hash/referencia del payload origen.

Una recomendación referencia el snapshot exacto usado. Los pronósticos nuevos no reescriben los anteriores.

### Operación y degradación

- Consumir proveedores solo desde backend mediante `WeatherProvider`.
- Cachear por ubicación, variables y horizonte.
- CAP se cruza con el polígono del campo en PostGIS, no solo por nombre de localidad.
- Si el proveedor falla: mostrar último dato con antigüedad o “no disponible”.
- Nunca producir alertas oficiales rojas/amarillas/naranjas a partir de inferencias propias.
- Campos grandes usan puntos representativos; lotes menores que una celda no muestran falsa precisión.
- Comparar pronósticos contra pluviómetro y registrar error por horizonte/estación cuando exista observación local.

## Modelo de pastoreo

### Potrero

- geometría, superficie total y efectiva aprovechable;
- estado: disponible, ocupado, descanso, reservado o clausurado;
- recurso: pastizal natural, pastura implantada, verdeo, rastrojo u otro;
- biomasa/altura de entrada, actual y remanente objetivo;
- método, muestras, fecha y confiabilidad de medición;
- tasa de crecimiento observada/configurada;
- factor de accesibilidad y eficiencia de cosecha;
- última salida, descanso mínimo y ventana óptima;
- agua/caudal, sombra, alambrados y distancia;
- anegamiento/piso, toxicidad, carencia, sanidad y otras restricciones.

### Grupo animal

- taxón/especie del catálogo nacional; el pastoreo especializado inicial admite bovinos, bubalinos, ovinos, caprinos, equinos, asininos/mulares y camélidos domésticos únicamente con perfil propio validado;
- categoría/objetivo/estado fisiológico;
- cantidad, peso promedio y fecha de pesada;
- condición corporal;
- tasa de consumo de materia seca, fuente y versión;
- suplementos/reservas actuales;
- requerimiento de agua y restricciones.

No aplicar una tasa bovina universal a otras especies ni convertirlas automáticamente a equivalente vaca. Aves, porcinos, colmenas y lotes acuáticos no usan este motor por el solo hecho de estar catalogados.

### Perfil forrajero

Plantilla versionada por recurso, región y estación:

- método de medición;
- biomasa/altura de entrada y remanente;
- tasa/curva de crecimiento inicial;
- descanso mínimo/ventana y máximo de ocupación;
- eficiencia de cosecha/utilización;
- estado fenológico y riesgos;
- fuente técnica y profesional aprobador.

El perfil regional inicial nunca prevalece sobre una medición vigente del potrero.

## Datos y niveles de evidencia

Para una recomendación `OBSERVADO` con fecha/capacidad cuantitativa son necesarios:

1. superficie efectiva y estado del potrero;
2. agua confirmada;
3. biomasa/altura vigente y remanente objetivo, con método aprobado;
4. cantidad, especie, categoría y peso promedio;
5. tasa de consumo de materia seca aplicable;
6. fecha de último pastoreo y restricciones;
7. pronóstico vigente y alertas.

Si falta biomasa/altura puede emitirse `ESTIMADO`: escenarios bajo/base/alto desde un perfil regional o supuesto profesional, con fecha de revisión e inspección requerida, pero sin declarar capacidad exacta ni potrero “listo”. Si falta agua, peso/consumo, perfil compatible, seguridad o clima requerido, el nivel es `SEGURIDAD_INSUFICIENTE` y no se propone ingreso. Ubicación/ecorregión por sí sola no prueba el estado del pasto.

## Fórmulas determinísticas

### Demanda diaria

```text
PV_total_grupo = cantidad × peso_promedio_kg
demanda_MS_grupo = PV_total_grupo × tasa_consumo_MS
demanda_total_D = suma(demanda_MS_grupo)
demanda_pastoril = máximo(0, demanda_total_D − MS_suplemento_efectivo)
```

La tasa es una fracción versionada (por ejemplo `0,025`), definida/aceptada por profesional para especie, categoría y objetivo.

### Oferta aprovechable

```text
oferta_S0_kg_MS =
  superficie_efectiva_ha
  × máximo(0, biomasa_ingreso − biomasa_remanente)
  × factor_accesibilidad
```

La remoción supera al consumo por pisoteo, selección y rechazo:

```text
remoción_diaria = demanda_pastoril / eficiencia_cosecha
crecimiento_diario_G = superficie_efectiva × tasa_crecimiento × factor_accesibilidad
balance(t) = oferta_S0 + crecimiento_diario_G × t − remoción_diaria × t
```

Si `remoción_diaria > G`:

```text
días_por_oferta = piso(oferta_S0 / (remoción_diaria − G))
```

Siempre:

```text
días_recomendados = mínimo(
  días_por_oferta,
  máximo_ocupación_perfil,
  días_hasta_riesgo_climático,
  límite_operativo
)
```

Si el crecimiento iguala/supera la remoción, nunca se informa permanencia ilimitada: domina el máximo de ocupación para proteger rebrotes.

### Déficit

```text
déficit_MS = máximo(
  0,
  remoción_diaria × días_planificados
  − (oferta_S0 + G × días_planificados)
)
```

Se muestra en kg MS. Convertirlo en suplemento/ración requiere calidad, sustitución, energía, proteína, minerales y revisión profesional; queda fuera del MVP.

### Próximo ingreso

```text
días_hasta_biomasa = techo(
  máximo(0, biomasa_objetivo − biomasa_actual) / tasa_crecimiento_proyectada
)

próxima_revisión_estimativa = máximo(
  salida + descanso_mínimo,
  hoy + días_hasta_biomasa
)
```

Solo declarar “listo” después de una medición que confirme biomasa/altura, descanso y ausencia de bloqueos.

## Algoritmo MVP

1. Validar integridad y frescura.
2. Calcular demanda total/pastoril por grupo.
3. Obtener clima, CAP y riesgos.
4. Excluir potreros ocupados/clausurados/sin agua/con carencia/riesgo.
5. Verificar descanso, biomasa/altura y estado fenológico; si no hay medición, seleccionar únicamente un perfil estimativo aprobado.
6. Con evidencia observada calcular oferta, balance, remanente, días y déficit; con evidencia estimada calcular rangos bajo/base/alto sin estado “listo”.
7. Excluir candidatos que violan remanente o no soportan un mínimo seguro.
8. Ordenar transparentemente por ventana óptima, capacidad, riesgo, calidad por perder, distancia y prioridad manual.
9. Mostrar hasta tres alternativas con cálculos, fuentes, confianza y faltantes.
10. Productor confirma/modifica/rechaza.
11. Crear plan; el movimiento real se confirma aparte.
12. Recalcular ante nueva corrida, lluvia/medición, cambio de animales o restricción.
13. Registrar salida/biomasa real y evaluar el resultado.

## Bloqueos de seguridad

- agua no confirmada/insuficiente;
- potrero anegado, clausurado o inseguro;
- carencia fitosanitaria, toxicidad o restricción sanitaria;
- dato crítico de seguridad, agua, animales o perfil ausente/vencido;
- riesgo meteorológico extremo;
- remanente proyectado inferior al mínimo;
- denominadores, tasas o factores inválidos.

La recomendación se rotula orientativa y nunca prescribe medicamentos, minerales, aditivos, raciones ni tratamientos.

## QA y evaluación

- Contract tests por proveedor y fixtures CAP reales.
- Timeouts, 429/500, corrida faltante, NetCDF/XML inválido, actualización/cancelación/vencimiento.
- Acumulados alrededor de UTC/medianoche local sin duplicación.
- Cuando exista pluviómetro, comparar ECMWF/GFS/WRF: sesgo/RMSE de lluvia, Brier para probabilidad, MAE para temperatura/viento; sin él mostrar que no hay validación local.
- Property tests: demanda aditiva, oferta no negativa, remanente protegido, no infinito, cero/negativos rechazados.
- Especies separadas y perfiles versionados.
- Concurrencia: dos planes no reservan el mismo potrero por encima de capacidad.
- Toda recomendación reconstruible y su cambio por nueva corrida conserva ambas versiones.

## Fuentes clave

- [SMN WRF Open Data](https://registry.opendata.aws/smn-ar-wrf-dataset/)
- [SMN CAP oficial](https://ssl.smn.gob.ar/CAP/AR.php)
- [Open-Meteo Forecast API](https://open-meteo.com/en/docs)
- [Open-Meteo precios/licencia comercial](https://open-meteo.com/en/pricing)
- [NASA POWER API](https://power.larc.nasa.gov/docs/services/api/)
- [INTA: guía de planificación forrajera 2025](https://repositorio.inta.gob.ar/bitstream/handle/20.500.12123/24584/INTA_CREntreR%C3%ADos_EEAConcordia_Chiossone_J_Gu%C3%ADa_planificaci%C3%B3n_forrajera.pdf?isAllowed=y&sequence=1)
- [INTA: asignación forrajera](https://repositorio.inta.gob.ar/xmlui/handle/20.500.12123/18986)
- [INTA: forraje y carga](https://intainforma.inta.gob.ar/forrajes-la-clave-para-incrementar-la-productividad-del-rodeo/)

Los rangos técnicos se almacenan como perfiles editables con fuente/versión. No deben copiarse como constantes universales al código.
