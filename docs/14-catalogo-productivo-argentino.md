# Catálogo productivo argentino

## Decisión de alcance

AgropecuarIA tendrá cobertura para **todo el territorio argentino** y una línea base nacional versionada de actividades, cultivos y especies productivas. La cobertura se implementa en dos ejes independientes:

1. **Cobertura horizontal:** cualquier entrada del catálogo puede seleccionarse, asignarse a una unidad de manejo y registrar ciclos, eventos, cantidades, productos, inventario, costos, documentos e historial mediante el núcleo productivo común.
2. **Profundidad vertical:** formularios, KPI, algoritmos, IA, normativa e integraciones específicas solo se habilitan cuando existe un perfil técnico versionado y aprobado para la actividad y jurisdicción.

Por lo tanto, “incluida” nunca significará falsamente “automatizada en todos sus detalles”. La interfaz mostrará el nivel máximo disponible:

| Nivel | Capacidad |
|---|---|
| `CATALOGADA` | entrada reconocida, fuente, sinónimos, vigencia y jurisdicción |
| `FLUJO_GENERICO` | unidad/ciclo, eventos, cantidades, producción, costos, documentos y trazabilidad |
| `ESPECIALIZADA_VALIDADA` | reglas, formularios, KPI, alertas o IA aprobados para perfil y jurisdicción |

La completitud se mide contra una versión congelada, por ejemplo `Catálogo Nacional v1`, con fuentes, denominador, altas, deduplicaciones y excepciones. No se promete una lista eterna: aparecen producciones, cultivares y obligaciones nuevas.

## Cobertura territorial

- Alta de campos y unidades productivas en las 23 provincias y la Ciudad Autónoma de Buenos Aires, sin restringir una producción únicamente por provincia.
- Provincias, departamentos/partidos/comunas, municipios, gobiernos locales, localidades y asentamientos se normalizan con **Georef Argentina** y códigos oficiales.
- Las coordenadas y el polígono real prevalecen para clima, mapa y análisis espacial.
- Región/ecorregión, antecedentes productivos y jurisdicción sirven para ordenar opciones y seleccionar perfiles; no prueban aptitud agronómica ni bloquean por sí solos una actividad.
- Cada regla regulatoria declara jurisdicción, fuente, vigencia y responsable de validación. Si no hay perfil validado, se permite registrar la operación y se informa “normativa no automatizada”.

## Taxonomía vegetal

No se usará un único `tipo_cultivo`. Una asignación combina dimensiones versionadas:

```text
dominio → grupo estadístico/sanitario → especie o mezcla
        → propósito → cultivar/variedad → sistema → ciclo
```

Una especie puede tener varios propósitos. Maíz puede ser grano, choclo, pastoreo, silaje o semilla; vid puede destinarse a vinificación, mosto, pasa o consumo fresco. Los grupos son relaciones/etiquetas, no casilleros excluyentes.

### Línea base agrícola v1

| Familia | Entradas iniciales y cobertura |
|---|---|
| Cereales y pseudocereales | maíz, trigo pan, trigo candeal, arroz, cebada cervecera/forrajera, avena, sorgo granífero/forrajero, centeno, mijo, alpiste, triticale, quinoa y amaranto |
| Oleaginosas | soja, girasol, maní, colza/canola, lino, cártamo, sésamo y chía |
| Legumbres | porotos alubia/negro/de color y otros, garbanzo, arveja seca/fresca, lenteja, haba y lupino |
| Industriales y economías regionales | caña de azúcar, algodón, tabaco, yerba mate, té, mandioca, stevia, jojoba, lúpulo, tung, cáñamo/cannabis bajo perfil regulatorio y otros regionales |
| Forrajeras anuales | avena, centeno, cebada y triticale forrajeros; maíz/sorgo silero; mijo, moha, raigrás anual y mezclas/verdeos configurables |
| Pasturas y recursos perennes | alfalfa, festuca, raigrás perenne, agropiro, trébol blanco/rojo, lotus, pasto ovillo, Panicum/Gatton panic, buffel grass, pasturas consociadas y campo natural/pastizal |
| Hortalizas | papa, batata, mandioca hortícola, cebolla, ajo, tomate, pimiento/pimentón, zapallos, zapallito, pepino, lechuga, acelga, espinaca, repollo, brócoli, coliflor, zanahoria, remolacha, choclo, chaucha, arveja fresca, espárrago, alcaucil, melón, sandía, frutilla y otras de hoja/fruto/raíz/bulbo/flor |
| Frutales de pepita | manzana, pera y membrillo |
| Cítricos | limón, naranja, mandarina y pomelo |
| Frutales de carozo | durazno, nectarina, ciruela, cereza, damasco y guindo |
| Frutos secos | nogal, almendro, pistacho, pecán, avellano, castaño y otros |
| Frutas finas | arándano, frambuesa, mora y otras berries |
| Subtropicales y otros frutales | banana, palta, mango, papaya, ananá, kiwi, higo y otros declarados |
| Viticultura y olivicultura | vid por aptitud/variedad; olivo para aceite y/o mesa |
| Aromáticas, medicinales y condimentarias | orégano, coriandro, comino, anís, hinojo, manzanilla, lavanda, menta, romero, tomillo, albahaca y otras |
| Forestación/agroforestería | pinos, eucaliptos, álamos, sauces, araucarias y otras especies implantadas; rodales y sistemas silvopastoriles |
| Viveros, floricultura y propagación | plantines hortícolas, frutales, forestales, ornamentales, flores de corte y material de propagación |
| Producciones controladas/emergentes | hongos comestibles, hidroponía, acuaponía vegetal y otras producciones verificadas por el administrador del catálogo |

`Producción de semillas` es un propósito transversal de la especie, no un cultivo independiente. `Orgánico`, `agroecológico`, `biodinámico`, `siembra directa`, `secano`, `riego`, `campo` y `protegido` son sistemas o atributos, no especies.

### Datos vegetales configurables

- código interno estable, fuente/código externo, nombre común/científico y sinónimos regionales;
- grupos CNA/SENASA y etiquetas secundarias;
- ciclo anual/perenne, propósitos, ambientes y unidades admitidas;
- cultivar declarado y referencia INASE/INV cuando corresponda;
- perfil geográfico orientativo y perfiles regulatorios versionados;
- esquema fenológico, atributos especializados y nivel de soporte;
- estado, vigencia, fecha de actualización y motivo de inactivación.

Para perennes se agregan año de implantación, edad, marco, portainjerto/conducción y superficie productiva. Para forestales: rodal, densidad, poda, raleo, turno y destino. Para mezclas/consociaciones se conserva cada componente y proporción sin inventar un cultivo único.

## Taxonomía animal y otras producciones biológicas

Se separan taxón, orientación, sistema, raza/línea, categoría etaria/sexual, estado fisiológico, unidad de seguimiento y productos. Ninguna lista será un `enum` rígido.

### Línea base pecuaria v1

| Familia | Especies/sistemas | Unidad de seguimiento y productos |
|---|---|---|
| Bovinos | cría, recría, invernada/terminación, ciclo completo, feedlot, tambo, cabaña | individuo/rodeo; carne, leche, genética, cuero/subproductos |
| Bubalinos | carne, leche, mixta, cría, recría, terminación y cabaña | individuo/rodeo; carne, leche, genética |
| Ovinos | carne, lana, doble propósito, leche, cabaña y peletera | individuo/majada; carne, lana, leche, cuero, genética |
| Caprinos | carne, leche, fibra/pelo, cuero, cabaña y mixta | individuo/hato; carne, leche, fibra, cuero, genética |
| Porcinos | lechones, crecimiento/engorde, ciclo completo, genética, aire libre, mixto e intensivo | lote/individuo reproductor; animal, carne y genética |
| Equinos y otros équidos | caballos de trabajo, deporte, recreación, haras/cabaña y exportación; asnos, mulas y burdéganos | preferentemente individuo; trabajo, deporte, reproducción, animal/productos |
| Camélidos domésticos | llamas y alpacas para carne, fibra, cuero, cabaña y servicios | individuo/tropa; carne, fibra, cuero, genética |
| Aves | pollos parrilleros, ponedoras, reproductores/incubación; pavos, patos, gansos, codornices, faisanes, palomas, ñandú/choique y otras autorizadas | lote/galpón; carne, huevo, material vivo, plumas |
| Conejos y pilíferos | conejos; chinchillas, visones, zorros y otros sujetos a validación | lote/jaula/reproductor; carne, pelo, piel, genética |
| Apicultura | abeja melífera; apiarios fijos/trashumantes y polinización | apiario, colmena, núcleo, reina; miel, polen, propóleos, jalea, cera, apitoxina y material vivo |
| Acuicultura | pacú, trucha arcoíris, surubí, carpas, tilapia, boga, sábalo, pejerrey, tararira, dorado, bagres, esturión, ostras, mejillones, crustáceos, rana, yacaré, algas y otras autorizadas | lote acuático/estanque/jaula; biomasa, juveniles, consumo o repoblamiento |
| Cérvidos y fauna productiva | ciervo colorado/otros cérvidos; guanaco, vicuña, jabalí, liebre, carpincho, ñandú/choique, yacaré y otros solo con perfil legal | individuo/lote/cupo; carne, cuero, fibra, reproductores u otros permitidos |
| Producciones menores | caracoles, lombrices, sericicultura y otros animales/invertebrados declarados en fuentes oficiales o habilitados | unidad configurable: biomasa, camas, núcleos, lotes o individuos |

Guanaco, vicuña y demás fauna silvestre no se presentarán como ganado doméstico. Requieren origen legal, permiso, cupo, jurisdicción y perfil específico.

### Categorías

Las categorías son plantillas versionadas y mapeables, no texto libre como única fuente:

- bovinos/bubalinos: ternero/a, vaquillona, vaca, novillito, novillo, torito, toro, buey/toruno; lactancia, preñez, servicio, reposición y descarte como estados;
- ovinos: cordero/a, borrego/a, oveja, capón y carnero;
- caprinos: cabrito/a, chivito/a, cabrilla, cabra, capón y chivo/chivato;
- porcinos: lechón, cachorro/a, reposición, capón, cerda y padrillo; categorías de res comercial separadas;
- equinos: potrillo/a, potro/potra, yegua, caballo castrado y padrillo;
- aves, acuicultura y producciones de alta rotación: fase/lote en lugar de forzar categorías de rodeo.

Alias regionales se vinculan a una categoría canónica. El usuario puede crear una categoría privada y mapearla sin sobrescribir la fuente nacional.

## Núcleo productivo común

Para no forzar apiarios, galpones, invernaderos o estanques dentro de `Field`/`Herd`, se adopta:

- `ProductCatalogVersion`, `ProductCatalogSource`, `TaxonomyNode`, `CatalogAlias`, `JurisdictionApplicability`;
- `SupportLevel`, `ActivityProfile`, `ActivityProfileVersion`;
- `ProductiveActivity`: qué se produce;
- `ProductionSystem`: cómo se produce;
- `ManagementUnit`: lote, potrero, invernadero, rodal, apiario, estanque, jaula, galpón, corral, vivero u otra; geometría opcional según tipo;
- `ProductionCycle`, `ProductionEvent`, `ProductionOutput` y `ProducedBatch`;
- extensiones `CropCycle`, `Animal/Herd`, `Apiary`, `AquaticLot`, etc., únicamente donde su perfil lo exige.

Los atributos variables usan un esquema JSON versionado y validado por perfil. Fecha, unidad, cantidad, fuente, estado, costo, ubicación y evidencia permanecen tipados.

## Mediciones locales opcionales

- El alta, el mapa, el clima y el flujo productivo no requieren pluviómetro ni estación propia.
- La carga manual de lluvia es una capacidad opcional. Si no existe, se muestran pronóstico/modelo/estimación con procedencia y “calibración local no disponible”.
- La medición de altura o biomasa es opcional para registrar potreros, animales y pastoreos.
- Sin biomasa puede mostrarse un escenario bajo/base/alto desde un perfil regional o supuesto profesional, rotulado `ESTIMADO`, con menor confianza y pedido de inspección. No se declara un potrero “listo” ni una capacidad/fecha exacta.
- Con medición vigente, la recomendación pasa a `OBSERVADO`, recalcula y conserva ambas versiones.
- Agua no confirmada, carencia, toxicidad, riesgo sanitario o meteorológico extremo siguen siendo bloqueos de seguridad.

## Exportación contable pendiente

El formato/software del contador continúa pendiente. El MVP puede construir un paquete canónico versionado con movimientos, terceros, centros de costo, monedas, impuestos informados, documentos y totales de control. `AccountingExportProfile`, `FieldMapping`, `ExportSchemaVersion` y `ExportRun` permitirán agregar el adaptador real sin cambiar los datos fuente. No se declarará compatibilidad con un software hasta que el contador importe y concilie una muestra.

## Actualización del catálogo

1. Ingerir fuentes a un área de staging y conservar archivo/hash/fecha.
2. Normalizar nombres, códigos y sinónimos sin perder el valor de origen.
3. Detectar altas, cambios, duplicados e inactivaciones.
4. Someter conflictos a revisión editorial.
5. Publicar una versión inmutable con changelog y posibilidad de rollback.
6. Nunca borrar una entrada usada: inactivarla conservando el código.
7. Permitir `otra actividad/especie/cultivo` privada con nombre científico, unidad y habilitación cuando aplique; puede proponerse para la siguiente versión nacional.

## Criterios de aceptación

1. Cada entrada del baseline v1 completa el flujo genérico: asignación → ciclo → evento → cantidad/unidad → costo/documento → producto → timeline/exportación.
2. Toda entrada muestra fuente, vigencia, jurisdicción y nivel de soporte.
3. Búsqueda tolera tildes, nombre científico, nombre común, sinónimo regional y código.
4. Agregar o inactivar una entrada no requiere desplegar código ni rompe históricos.
5. Una actividad catalogada nunca muestra reglas o KPI de otra especie/cultivo.
6. El sistema registra una unidad de manejo adecuada para lote, potrero, invernadero, rodal, apiario, galpón y estanque.
7. Se ejecuta un smoke test geográfico al menos en un punto de cada provincia y CABA; cualquier falta de cobertura se informa sin inventar datos.
8. Sin pluviómetro, clima permanece operativo y distingue pronóstico/estimación de observación.
9. Sin biomasa, se permiten escenarios claramente estimados, pero no una fecha exacta ni estado “listo”.
10. El paquete contable canónico concilia con la UI; el adaptador específico permanece pendiente hasta la muestra real.

## Fuentes rectoras

- [INDEC — Censo Nacional Agropecuario 2018](https://www.indec.gob.ar/indec/web/Nivel4-Tema-3-8-87)
- [INDEC — resultados definitivos CNA 2018](https://www.indec.gob.ar/ftp/cuadros/economia/cna2018_resultados_definitivos.pdf)
- [SAGyP — Estimaciones Agrícolas](https://www.magyp.gob.ar/sitio/areas/estimaciones/index.php)
- [SENASA — Cadena Vegetal](https://www.argentina.gob.ar/senasa/programas-sanitarios/cadenavegetal)
- [SENASA — Cadena Animal](https://www.argentina.gob.ar/senasa/programas-sanitarios/cadenaanimal)
- [SENASA — RENSPA](https://www.argentina.gob.ar/senasa/micrositios/renspa)
- [INASE — Registro Nacional de Cultivares](https://www.argentina.gob.ar/node/104464)
- [INV — estadísticas de superficie y variedades](https://www.argentina.gob.ar/inv/vinos/estadisticas/superficie/anuarios)
- [INTA — programas nacionales](https://www.argentina.gob.ar/inta/programas-nacionales)
- [Georef Argentina](https://www.argentina.gob.ar/georef/georef-servicio-de-normalizacion-de-direcciones-y-unidades-territoriales-de-argentina)
- [SENASA — producción acuícola](https://www.argentina.gob.ar/senasa/programas-sanitarios/cadenaanimal/animales-acuaticos-produccion-primaria)
- [SENASA — producción apícola](https://www.argentina.gob.ar/senasa/programas-sanitarios/cadenaanimal/abejas/produccion-primaria)

