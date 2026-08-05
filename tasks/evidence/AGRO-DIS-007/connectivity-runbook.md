# Runbook de conectividad, rollout y recuperación

Estado: procedimiento R0 para un MVP online. Los perfiles son sintéticos y no demuestran conectividad rural. Offline, sincronización local y mapas descargables permanecen fuera del MVP.  
Owner operativo propuesto: Product/UX + Frontend/SRE. Owner de medición de campo: Product.  
Revalidar: 2026-09-30 o ante cambio de dispositivo, carrier, zona o flujo crítico.

## Perfiles sintéticos

| Perfil | RTT/latencia | Descarga | Subida | Pérdida | Estado esperado |
|---|---:|---:|---:|---:|---|
| `target` | 150 ms | 9.000 Kbps | 1.500 Kbps | 0 % | flujo online dentro de targets p75/p95 aplicables |
| `constrained` | 300 ms | 1.500 Kbps | 750 Kbps | 1 % | loading visible, timeout acotado, reintento seguro y datos stale rotulados |
| `critical` | 600 ms | 400 Kbps | 100 Kbps | 2 % | evitar confirmación dudosa, conservar lectura útil si existe cache de servidor, mensaje accionable |
| `offline` | sin red | 0 | 0 | 100 % | bloquear escritura antes de confirmar; explicar que se necesita conexión; no persistir ni encolar localmente |

La selección visual de un perfil solo presenta el escenario. `setOffline(true)` de Playwright sí reproduce ausencia de red en el contexto del navegador; las cifras de latencia/ancho de banda/pérdida no se consideran medidas salvo que un harness de red independiente las imponga y registre. Referencia primaria: [Playwright BrowserContext.setOffline](https://playwright.dev/docs/api/class-browsercontext#set-offline).

## Ensayo reproducible en navegador

Precondiciones: build inmutable, navegador/dispositivo registrados, datos sintéticos, endpoint/harness aislado y contador de requests. No usar credenciales ni datos personales reales.

1. Abrir el flujo crítico online y verificar estados `loading`, éxito, error y stale/degraded.
2. Preparar un comando con idempotency key generada para el intento lógico; la clave nunca se imprime ni se usa como atributo de telemetría.
3. Activar `browserContext.setOffline(true)` después de cargar la pantalla.
4. Confirmar que el indicador cambia a “Sin conexión”, que la acción irreversible queda deshabilitada antes del submit y que no sale ningún request.
5. Verificar que la aplicación no registra el borrador/comando en Service Worker, Cache Storage, IndexedDB, localStorage ni una cola propia. El navegador puede conservar assets de su cache HTTP normal; eso no convierte el producto en offline.
6. Reactivar con `setOffline(false)` y verificar recuperación de lectura sin recargar cuando corresponda.
7. Para un resultado previo incierto, reintentar el mismo intento lógico con la misma idempotency key y demostrar una sola mutación/auditoría en el servidor. Si no existe backend idempotente, el escenario queda pendiente: una simulación frontend no lo aprueba.
8. Ejecutar el camino bajo `target`, `constrained` y `critical` con el harness declarado; conservar configuración, timestamps, percentiles, tamaño de muestra, requests duplicados y capturas accesibles.

Criterio de fallo: pérdida de input sin aviso, doble efecto, confirmación falsa, spinner infinito, dato viejo sin fecha/fuente, secreto/PII en logs o cualquier persistencia offline encubierta.

## Protocolo de medición de campo para Q-061

El Sponsor/Product debe seleccionar sitios, dispositivos y carriers representativos del piloto; el spike no inventa esa muestra. En cada combinación aprobada:

1. Asignar un código de sitio pseudónimo y región amplia; no registrar productor, CUIT, coordenada exacta ni SSID.
2. Registrar modelo/SO/navegador del dispositivo, carrier, tipo de conexión, fecha/hora/zona IANA y condición meteorológica si afecta la prueba.
3. Medir DNS/conexión/TLS, RTT, descarga, subida, pérdida, cortes y tiempo de los flujos mapa, campo, clima y escritura idempotente.
4. Repetir en ventanas horarias y ubicaciones operativas acordadas hasta cubrir la variación del piloto; publicar tamaño de muestra y ausencias, no solo promedios.
5. Calcular p50/p75/p95, proporción offline, duración de cortes y tasa de éxito/reintento por perfil; comparar contra los perfiles sintéticos sin forzar coincidencia.
6. Product/UX registra impacto observado y SRE conserva herramienta/versión/configuración/hash. Privacy revisa que el dataset anonimizado no reidentifique un establecimiento.

Q-061 solo se cierra cuando Product acepta cobertura del piloto y QA puede reproducir el dataset/procedimiento. Si el resultado hace inviable el MVP online, se escala decisión de producto; no se introduce sincronización offline dentro de esta tarea.

## Canary y rollback propuestos

No se ejecutó despliegue. Para una futura release:

- comenzar en entorno interno con datos sintéticos; luego un tenant piloto explícitamente autorizado y después cohortes acotadas;
- aislar métricas por cohorte sin etiquetas de tenant: usar un identificador de cohorte enumerado y no reversible;
- promover solo si disponibilidad/latencia, duplicados, backlog, costo y errores permanecen dentro de budgets aprobados con tamaño de muestra suficiente;
- pausar automáticamente ante fallo de tenant isolation, corrupción, doble efecto, migración incompatible o hallazgo alto/crítico;
- rollback de aplicación a artefacto anterior firmado; cambios de esquema deben ser expand/contract y forward-safe. Nunca ejecutar downgrade destructivo automático;
- mantener lectura/degradación segura mientras se detienen nuevos comandos si su resultado es incierto.

Sin SLO, presupuesto y piso de tráfico aprobados no se inventan porcentajes/duración de canary. El owner registra versión, decisión, señal y hora UTC de cada promoción/rollback.

## Incidente de conectividad

1. Clasificar alcance: dispositivo, carrier/zona, DNS/TLS/CDN, core o dependencia.
2. Preservar correlación técnica sin PII y verificar desde otra red/región.
3. Si la escritura pudo llegar, no pedir “intentar de nuevo” con una clave nueva: consultar estado o reutilizar el mismo intento idempotente.
4. Mostrar último dato confirmado con fecha/fuente/confianza; nunca inventar éxito ni dato fresco.
5. Si core está sano y la dependencia falla, activar solo el fallback aprobado y medir su antigüedad.
6. Si se agotó el error budget o hay integridad en riesgo, detener rollout y seguir rollback/incident response.

## DR y continuidad

Los objetivos RPO ≤15 min y RTO ≤2 h son hipótesis. El runbook de `AGRO-DIS-005` gobierna restore de DB/PostGIS/objetos/auditoría; falta PITR administrado, dataset representativo y región/proveedor. Un fallo de carrier no habilita copia local de datos sensibles: la continuidad del MVP online es degradación visible, espera segura y recuperación idempotente.

## Telemetría permitida

Emitir: ambiente, plantilla de ruta, método, clase de status, dependencia enumerada, tipo de job, resultado cache y resultado acotado; además latency/error/backlog/último éxito sin dimensiones de negocio. Aplican los límites y prohibiciones de `sli-slo-catalog.md`.

Nunca emitir tenant/usuario/CUIT/email, UUID de recurso, coordenadas, IP precisa persistida como atributo, SSID, path/query/filename, payload, token, cookie o idempotency key. Traces y logs se muestrean/retienen según política aprobada, todavía pendiente.

## Gates de salida

- E2E Chromium reproduce offline real y confirma cero request/cero persistencia local del comando.
- API demuestra idempotencia y autorización tenant frente a respuesta perdida/reintento.
- Perfiles limitados usan harness versionado; targets mapa/clima/API se miden con tamaño de muestra publicado.
- Q-061 dispone de medición de campo aceptada; accesibilidad de estados se valida con teclado/lector/pantalla angosta.
- Canary/rollback y restore se ensayan con artefacto, esquema y proveedor candidatos.
- SRE/AppSec verifica allow-list, cardinalidad y ausencia de PII/secrets.

Mientras falte cualquiera de esos gates, el resultado es `NO-GO` para afirmar resiliencia rural productiva; RSK-024 permanece abierto.
