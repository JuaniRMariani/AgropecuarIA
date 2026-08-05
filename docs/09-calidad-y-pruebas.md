# Requisitos no funcionales y estrategia QA

Los valores son objetivos iniciales; deben ajustarse con escala, presupuesto y pilotos.

## Requisitos no funcionales

### Disponibilidad y resiliencia

- `RNF-REL-001`: núcleo mensual ≥ 99,9 %; dependencias externas quedan fuera pero el modo degradado debe ser visible.
- `RNF-REL-002`: RPO ≤ 15 minutos y RTO ≤ 2 horas.
- `RNF-REL-003`: restauración trimestral demostrada y documentada.
- `RNF-REL-004`: clima/SMN/IA caídos no impiden usar módulos transaccionales; se muestra degradación y los reintentos del servidor son durables.

### Rendimiento

- `RNF-PER-001`: API propia p95 lectura ≤ 400 ms y escritura ≤ 800 ms, excluyendo externos.
- `RNF-PER-002`: mapa interactivo inicial ≤ 3 s p75 en perfil 4G objetivo.
- `RNF-PER-003`: procesar 1.000 movimientos/importaciones en ≤ 2 min p95 en el entorno objetivo.
- `RNF-PER-004`: importaciones grandes son asíncronas, con progreso y reporte de errores.
- `RNF-PER-005`: clima del campo visible ≤ 2 s p75 cuando existe cache vigente; la actualización externa continúa en servidor.

### Conectividad y compatibilidad

- `RNF-CON-001`: el MVP requiere conexión; sin red se informa antes de confirmar y no se promete persistencia/sincronización local.
- `RNF-CON-002`: reintentos del navegador no duplican operaciones gracias a idempotencia server-side.
- `RNF-CON-003`: últimas dos versiones estables de navegadores principales; web responsive instalable como PWA online.
- `RNF-CON-004`: uso móvil con controles táctiles y feedback claro de conexión/proveedor degradado.

### Seguridad y privacidad

- `RNF-SEC-001`: cifrado en tránsito y reposo; secretos en vault/KMS.
- `RNF-SEC-002`: cero fallos conocidos de aislamiento y cero vulnerabilidades altas/críticas abiertas al release.
- `RNF-SEC-003`: auditoría de acciones sensibles y retención definida.
- `RNF-PRI-001`: exportación, rectificación y supresión operables según política/legal hold.

### Usabilidad y accesibilidad

- `RNF-UX-001`: WCAG 2.2 AA para web aplicable.
- `RNF-UX-002`: español inicial, unidades configurables, zonas IANA, UTC interno/presentación local.
- `RNF-UX-003`: estados de carga, vacío, sin conexión, dato obsoleto, proveedor caído, error y conflicto diseñados explícitamente.
- `RNF-UX-004`: UUID visibles abreviados a 6 caracteres mayúsculos sin guiones.

### Observabilidad y portabilidad

- `RNF-OBS-001`: trazas, métricas y logs correlacionados sin secretos/PII innecesaria.
- `RNF-OBS-002`: cada integración expone salud, latencia, errores, backlog y último éxito.
- `RNF-PORT-001`: exportación integral en formatos documentados y archivos originales.
- `RNF-PORT-002`: proveedores de IA/mapas/identidad detrás de contratos para reducir lock-in.

### Catálogo y cobertura nacional

- `RNF-CAT-001`: una publicación del catálogo procesa el 100 % del baseline fuente o produce excepciones explícitas y aprobadas.
- `RNF-CAT-002`: códigos usados permanecen estables; actualizar/inactivar una entrada no modifica históricos.
- `RNF-CAT-003`: el flujo genérico se prueba parametrizadamente contra todas las entradas publicadas.
- `RNF-GEO-001`: smoke geográfico al menos en un punto de cada provincia y CABA para alta, mapa, unidad territorial y clima/degradación.

## Estrategia de pruebas

| Capa | Cobertura mínima |
|---|---|
| Unitarias | invariantes, estados, permisos, fórmulas, conversiones |
| Property-based | asientos balanceados, stock por movimientos, área no negativa, idempotencia |
| Integración | PostgreSQL/PostGIS real, object storage, outbox/inbox |
| Contrato | OIDC, Open-Meteo, SMN CAP/WRF, mapas e IA |
| Catálogo | ingesta, normalización, alias, deduplicación, diff, rollback y recorrido del baseline completo |
| Perfiles | schema versionado, jurisdicción, unidades y ausencia de contaminación entre actividades |
| API | validación, auth positiva/negativa, concurrencia, errores |
| E2E | onboarding, lote, agricultura, ganadería, clima, rotación, stock, gasto y exporte contable |
| GIS | polígonos inválidos, huecos, multi, solapamiento, SRID, precisión |
| Meteorología | corridas, unidades, UTC/local, CAP, frescura, 429/500, fallback y desempeño histórico |
| Pastoreo | oferta/demanda, límites, especies, abstención, concurrencia y versionado |
| IA evals | evidencia, abstención, clima sin inventar, permisos, inyección, costo, regresión |
| Seguridad | SAST/SCA/SBOM/secrets/DAST y revisión manual BOLA |
| Rendimiento | mapa, dashboard, importación y sync masivo |
| Operación | backup/restore, rollback, rotación/revocación |

## Escenarios de aceptación críticos

### Aislamiento

```gherkin
Dado un usuario de la organización A
Cuando solicita un lote, archivo o exportación de la organización B
Entonces el servidor no devuelve datos ni confirma la existencia del recurso
Y registra el intento según política sin filtrar información sensible
```

### Historia espacial

```gherkin
Dado un lote que fue subdividido el 1 de julio
Y una labor confirmada el 20 de junio
Cuando consulto el historial de campaña
Entonces la labor se muestra sobre la geometría vigente el 20 de junio
Y los lotes sucesores conservan el vínculo histórico
```

### Clima degradado

```gherkin
Dado que el proveedor meteorológico no responde
Cuando el usuario abre un lote
Entonces el sistema muestra el último snapshot como obsoleto o “no disponible”
Y conserva proveedor, corrida, emisión y vigencia
Y la IA no inventa lluvia ni presenta una alerta propia como oficial
```

### Cobertura del catálogo nacional

```gherkin
Dado Catálogo Nacional v1 publicado
Cuando la suite parametrizada recorre cada entrada
Entonces puede crear unidad, ciclo, evento, cantidad/unidad, costo, documento, producto y timeline
Y toda excepción tiene fuente, motivo y aprobación
Y una entrada genérica nunca ofrece reglas ni KPI de otro perfil
```

### Cobertura territorial argentina

```gherkin
Dado un punto de prueba por cada provincia y CABA
Cuando se crea un establecimiento y se consulta territorio, mapa y clima
Entonces se conservan códigos oficiales, coordenadas y fuente
Y una falta de cobertura se muestra como degradación sin inventar datos
```

### Rotación ganadera

```gherkin
Dado un rodeo con cantidad, peso y consumo configurado
Y un potrero con superficie efectiva, agua, biomasa, remanente y clima vigentes
Cuando se solicita una recomendación
Entonces se muestran oferta, demanda, días máximos, descanso/revisión y alternativas
Y aceptar la recomendación no mueve animales hasta confirmar un evento separado
```

### Abstención ganadera

```gherkin
Dado un potrero sin medición vigente de forraje pero con agua y seguridad confirmadas
Cuando se solicita una fecha de ingreso
Entonces AgropecuarIA muestra escenarios bajo/base/alto rotulados como estimados y prioriza inspección
Y no declara una fecha exacta ni el potrero “listo”
Pero si falta agua o existe carencia, toxicidad o riesgo sanitario/climático, bloquea el ingreso
```

### Pluviómetro opcional

```gherkin
Dado un campo sin pluviómetro ni estación propia
Cuando el usuario abre clima
Entonces ve pronóstico/modelo con fuente y frescura
Y la calibración local figura “no disponible” sin bloquear la operación
```

### Exportación al contador

```gherkin
Dado un período cerrado con gastos, ingresos, imputaciones y monedas
Cuando un usuario autorizado exporta para el contador
Entonces el paquete canónico contiene totales de control conciliados con la interfaz
Y referencias estables a los documentos
Y la exportación queda auditada sin afirmar compatibilidad con un software ni liquidación fiscal
```

### IA

```gherkin
Dado un usuario sin permiso financiero
Cuando pide a la IA el margen de otro campo
Entonces la respuesta no recupera ni infiere esos datos
Y explica que no posee acceso suficiente
```

## Matriz de trazabilidad mínima

| Riesgo/objetivo | Requisitos | Prueba obligatoria |
|---|---|---|
| Acceso cruzado | RF-ID-005, RNF-SEC-002 | suite tenant negativa |
| Historial de lote | RF-GIS-006, RN-GIS-005 | integración PostGIS + E2E |
| Catálogo nacional | RF-CAT-001–005, RN-CAT-001–005 | import/diff + suite parametrizada completa |
| Flujo común | RF-PRD-001–005, RN-PRD-001–005 | E2E por familia + property de unidades |
| Cobertura argentina | RF-GIS-011, RNF-GEO-001 | matriz de 24 jurisdicciones |
| Stock/costo único | RF-OPS-005, RN-CORE-005 | idempotencia/concurrencia |
| Trazabilidad animal | RF-GAN-002/003/004 | importación + inventario a fecha |
| Clima trazable | RF-CLI-001/002/003, RN-CLI-001/003 | contrato + frescura + fixtures |
| Rotación segura | RF-GAN-011–017, RN-GAN-008–014 | unit/property + E2E + especialista |
| Export contador | RF-FIN-012, RN-FIN-005 | conciliación + importación piloto |
| IA fundamentada | RF-IA-001/002/004/005, RN-IA-003 | eval de citas/abstención |
| Recuperación | RNF-REL-002/003 | restore drill |

## Gates por release

- Requisito → criterios → prueba → evidencia enlazados.
- Lint, build, typecheck y tests configurados aprobados.
- Cero fallos tenant y altas/críticas.
- Migraciones compatibles/rollback o plan de roll-forward.
- Pruebas de accesibilidad y pantallas estrechas.
- Contratos externos simulados y prueba real contra clima/SMN; homologación ARCA solo en una fase futura.
- Evals IA aprobadas por especialista para el caso habilitado.
- Backup/restore y runbook de rollback demostrados en releases mayores.

## Datos de prueba

Fixtures sintéticas por organización, sin PII real. Conjuntos: baseline nacional versionado; al menos una entrada por familia vegetal/animal y por tipo de unidad de manejo; 24 puntos jurisdiccionales; campo mixto; lotes fusionados/subdivididos; campaña agrícola; rodeos individuales/grupales; apiario, galpón y estanque genéricos; potreros con/sin biomasa y agua/restricciones; pronósticos por varias corridas; CAP actualizado/cancelado; campo con/sin pluviómetro; partidas vencidas; multimoneda y documentos con prompt injection.
