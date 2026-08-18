# Inventario R0/R1 de proveedores y tratamiento de datos

Fecha de corte: 2026-08-18  
Tarea: `AGRO-SEC-001`  
Estado: evidencia de discovery; no constituye selección, contratación, DPA ni autorización productiva.

## Alcance y regla de decisión

Este inventario describe superficies previstas, candidatas y el runtime R1 local integrado. `apps/AgropecuarIA.Api`, `src/AgropecuarIA.Identity`, `src/AgropecuarIA.Territory`, `src/AgropecuarIA.ProductiveCore` y `apps/web` son código de producto ejecutable; el proveedor OIDC sintético y su configuración pertenecen solo a Development/Test. Productive Core usa PostgreSQL local y no introduce proveedor ni egress externo. `AGRO-DIS-003/004/005/007` continúan siendo pruebas descartables o sintéticas. Ninguna de estas evidencias demuestra región, residencia, retención, subencargados, SLA ni controles de una cuenta contratada o un despliegue compartido.

La evidencia se rotula así: `R1 local` para runtime integrado verificable en el workspace; `Development/Test` para adaptadores sintéticos que deben fallar cerrados fuera de esos ambientes; `R0 descartable` para spikes; `externo pendiente` para proveedores/cuentas no seleccionados; y `CI ausente` para controles que requieren pipeline o artefactos publicados.

Un estado tiene esta semántica:

- `CANDIDATO CONDICIONADO`: existe ajuste técnico o contrato R0, pero no autorización para producción.
- `FUTURO`: la superficie pertenece a una release posterior y no debe incorporarse todavía al runtime.
- `FUERA DE ALCANCE/NO-GO`: el uso indicado está prohibido por el alcance o por evidencia insuficiente.

`Q-058`, `Q-060`, `GAP-003` y `VAL-LEG` permanecen abiertos. La delegación técnica del sponsor no reemplaza la aprobación de Privacy/Legal sobre región, transferencias, DPA, subencargados o retención. Ante un campo obligatorio sin evidencia, el resultado es `NO-GO`, nunca una suposición favorable.

## Inventario

### PI-01 — Proveedor de identidad OIDC

- **Estado/fase:** el relying party OIDC R1 local está integrado con Authorization Code + PKCE y un proveedor sintético limitado a Development/Test. Auth0 es candidato condicionado para sandbox; ZITADEL Cloud es alternativa y AWS Cognito comparador. Ninguno está seleccionado, configurado ni aprobado para un entorno compartido/productivo.
- **Datos y dirección:** navegador ↔ IdP para autenticación; IdP → backend mediante callback OIDC; backend conserva `(issuer, subject)`, usuario, membresías, permisos y sesión propios. Pueden procesarse email verificado, factores, recovery, IP/dispositivo y eventos de autenticación. El IdP no recibe autoridad sobre tenant ni recurso.
- **Región, credenciales, retención y subencargados:** plan, región, DPA, retención, logs, subencargados, exportabilidad y SLA no están aprobados. Client secrets/keys y credenciales administrativas deben residir en secret manager, con scopes mínimos y rotación; tokens no van a `localStorage` ni logs.
- **Egress/SSRF:** endpoints de issuer, authorization, token, JWKS y logout se fijan por configuración aprobada; HTTPS, issuer/audience/nonce/state/PKCE S256 y redirects exactos. No se descubre ni consulta una URL aportada por usuario.
- **Degradación:** sesión existente puede conservar solo las capacidades que su política autorice; login, linking, recovery y step-up fallan cerrados y muestran proveedor no disponible. Nunca se vinculan cuentas solo por email.
- **Owner:** Identity/Tenancy + AppSec; Privacy/Legal/Procurement aprueban tratamiento y contrato; Sponsor aprueba costo/plan.
- **Evidencia R1 local + R0 descartable:** [`apps/AgropecuarIA.Api/Program.cs`](../../../apps/AgropecuarIA.Api/Program.cs), [`apps/AgropecuarIA.Api/IdentityEndpoints.cs`](../../../apps/AgropecuarIA.Api/IdentityEndpoints.cs), [`apps/AgropecuarIA.Api/OidcReauthentication.cs`](../../../apps/AgropecuarIA.Api/OidcReauthentication.cs), [`tests/AgropecuarIA.Identity.Tests/OidcConfigurationContractTests.cs`](../../../tests/AgropecuarIA.Identity.Tests/OidcConfigurationContractTests.cs), [`tests/AgropecuarIA.Identity.Tests/StepUpApiIntegrationTests.cs`](../../../tests/AgropecuarIA.Identity.Tests/StepUpApiIntegrationTests.cs), [`AGRO-DIS-003/idp-decision-matrix.md`](../AGRO-DIS-003/idp-decision-matrix.md).
- **Gate:** sandbox real con OIDC Authorization Code + PKCE, linking con doble reautenticación, recovery/factor perdido/revocación/failover, claims y callback; DPA/región/retención/subencargados/plan/SLA/exportación aprobados. Hasta entonces: `NO-GO productivo`.

### PI-02 — PostgreSQL/PostGIS administrado

- **Estado/fase:** PostgreSQL/PostGIS es baseline arquitectónico; proveedor, servicio administrado, región y topología siguen sin seleccionar. Identity posee persistencia, migraciones, concurrencia y outbox R1 integrados contra PostgreSQL local real. `CreateOrganization` implementa el primer conjunto acotado de datos tenant, roles de privilegio, `SET LOCAL` y `FORCE RLS`; el discovery actor-scoped general permanece en `AGRO-DIS-003` y el migrator/credenciales compartidos siguen pendientes.
- **Datos y dirección:** API/worker ↔ base transaccional. Procesará datos tenant confidenciales —ubicación, productividad, stock, finanzas, documentos metadata y auditoría— y datos platform separados. Backups/replicas recibirán el mismo nivel de clasificación.
- **Región, credenciales, retención y subencargados:** sin región, DPA, PITR, retención, réplica, soporte ni subencargados aprobados. El harness R0 usa SCRAM-SHA-256 local/TCP, cuatro passwords aleatorios efímeros distintos (`postgres`, app, job y discovery), `pwfile` transitorio y JSON de conexiones con ACL exclusiva del usuario actual, sin imprimir secretos y con cleanup. El adapter falla antes de servir si `current_user`/`session_user` no son exactamente `agro_membership_discovery` o si el rol posee ownership, membresías o atributos privilegiados. Para runtime siguen requeridos roles sin ownership, superuser o `BYPASSRLS`, credenciales separadas en secret manager y rotación.
- **Egress/SSRF:** no acepta URLs de conexión del cliente. Red privada/allow-list entre workloads y base; migraciones son una ruta privilegiada separada. Extensiones, FDW y network egress no se habilitan sin revisión.
- **Degradación:** una pérdida de conexión rechaza mutaciones; no se confirma ni encola negocio offline. Lecturas stale solo cuando el contrato del slice lo permita y lo rotule; corrupción/pérdida activa incidente y restore/roll-forward.
- **Owner:** Data/DBA + Platform/SRE; cada módulo posee schema/migraciones; AppSec revisa roles/RLS y Privacy/Legal región/retención.
- **Evidencia R1 local + R0 descartable:** [`src/AgropecuarIA.Identity/Infrastructure/IdentityDbContext.cs`](../../../src/AgropecuarIA.Identity/Infrastructure/IdentityDbContext.cs), [`tests/AgropecuarIA.Identity.Tests/IdentityDatabaseMigrationTests.cs`](../../../tests/AgropecuarIA.Identity.Tests/IdentityDatabaseMigrationTests.cs), [`tests/AgropecuarIA.Identity.Tests/OrganizationDatabaseSecurityTests.cs`](../../../tests/AgropecuarIA.Identity.Tests/OrganizationDatabaseSecurityTests.cs), [`AGRO-ID-003/validation-report.md`](../AGRO-ID-003/validation-report.md), [`AGRO-DIS-003/validation-report.md`](../AGRO-DIS-003/validation-report.md), [`docs/adr/ADR-002-postgis.md`](../../../docs/adr/ADR-002-postgis.md).
- **Gate:** proveedor/región/DPA/retención aprobados; conexión cifrada y roles productivos; RLS `default deny`/`FORCE RLS` con pool/jobs negativos; migración N/N-1; PITR/backup inmutable y restore representativo con RPO/RTO aceptados. El probe SCRAM loopback y su principal discovery least-privilege prueban la mecánica R0, pero no se promueven a runtime ni satisfacen estos gates productivos.

### PI-03 — Object storage, antimalware y KMS

- **Estado/fase:** AWS S3 + SSE-KMS/Object Lock + GuardDuty es candidato condicionado de sandbox; Azure Blob + CMK/immutability + Defender es alternativa; S3-compatible con scanner separado es fallback. Cloudflare R2 es `NO-GO como default` según el alcance evaluado. No hay proveedor seleccionado.
- **Datos y dirección:** navegador → API para iniciar carga; cliente → grant breve del storage; storage/evento → scanner; scanner → backend con verdict; backend/worker ↔ storage/KMS. Incluye documentos confidenciales o personales, MIME/hash/tamaño, object key, tenant binding, estado AV, auditoría y backups. Nunca se persisten secretos como documentos.
- **Región, credenciales, retención y subencargados:** región, residencia, DPA, subencargados, retención/purga, legal hold, WORM, soporte y costos abiertos. IAM workload identity y grants mínimos; CMK/KMS con separación de duties y rotación. Una URL firmada no equivale a autorización durable.
- **Egress/SSRF:** claves de objeto generadas por servidor; descarga/upload solo mediante endpoints y grants emitidos después de authz. No se importa una URL arbitraria. Scanner y callbacks usan origen allow-listed, autenticación, replay protection y payload acotado.
- **Degradación:** únicamente verdict `clean` publica el objeto. Timeout, `unsupported`, `access_denied`, fallo, evento duplicado/desordenado o proveedor caído permanecen en cuarentena fail-closed. Borrado ambiguo se reconcilia; jamás se informa éxito irreversible sin confirmación.
- **Owner:** Documents + Platform/SRE; AppSec controla uploads/IAM/eventos; Privacy/Legal controla clasificación, retención, residencia y hold.
- **Evidencia R0:** [`AGRO-DIS-005/provider-matrix.md`](../AGRO-DIS-005/provider-matrix.md), [`AGRO-DIS-005/threat-model.md`](../AGRO-DIS-005/threat-model.md), [`AGRO-DIS-005/validation-report.md`](../AGRO-DIS-005/validation-report.md), [`docs/adr/ADR-007-storage-retencion-recuperacion.md`](../../../docs/adr/ADR-007-storage-retencion-recuperacion.md).
- **Gate:** sandbox real storage+AV+KMS/WORM, eventos al menos una vez, IAM/BOLA y URLs firmadas negativas, MIME/polyglot/zip-bomb/tamaño, restore/PITR, inventario y purga/hold; DPA/región/subencargados/retención aprobados. Hasta entonces: `NO-GO productivo`.

### PI-04 — Mapas, Georef e IGN/Argenmap

- **Estado/fase:** Territory ya integra localmente un snapshot oficial inmutable de 23 provincias más CABA y un adapter fijado exactamente a Georef `v2.0`; ese código/test local no aprueba tráfico al proveedor. Georef real, Argenmap/IGN y tiles siguen `externo pendiente/NO-GO`. MapLibre es renderer, no proveedor; tiles comunitarios directos de OpenStreetMap son `FUERA DE ALCANCE/NO-GO` productivo.
- **Datos y dirección:** búsqueda autenticada → API → snapshot PostgreSQL local no sale a terceros. Solo el formulario explícito resolve envía `latitude`/`longitude` desde backend a `https://apis.datos.gob.ar/georef/api/v2.0/ubicacion`; no envía tenant, productor, campo ni parcela. La respuesta estricta devuelve código/nombre/jerarquía y fuente/frescura. La coordenada exacta es `Confidential`, existe temporalmente en caché volátil acotado y no se persiste en DB/journal, browser storage ni logs de aplicación.
- **Región, credenciales, retención y subencargados:** el endpoint público sin key no prueba región, access logs/query retention, DPA, subencargados, SLA, cuota o capacidad. Licencia/atribución y términos deben aprobarse. Q-058/Q-060 y `VAL-LEG` permanecen abiertos; por eso Georef externo es `NO-GO` aun cuando el adapter local pase.
- **Egress/SSRF:** el adapter solo acepta la base HTTPS compilada de Georef v2.0 y una ruta relativa tipada; deshabilita redirects, cookies y descompresión automática, usa timeout 5 s, máximo 256 KiB, media type JSON exacto, profundidad 12 y schema/códigos/nombres acotados. No hay proxy/fetch de URL libre. Un entorno compartido todavía debe bloquear DNS rebinding/IP reservada y demostrar egress allow-listed.
- **Degradación:** búsqueda local rotula snapshot `fresh|stale`. Resolve sirve caché volátil fresh/stale cuando corresponde; si el provider falla sin valor utilizable responde `unavailable` y ofrece búsqueda manual local. Nunca inventa unidad ni presenta la resolución como parcela o precisión agronómica.
- **Privacidad/observabilidad:** `TerritoryExceptionHandler` registra solamente código de error y correlation ID. La URL de salida contiene la coordenada, de modo que proxy, collector y provider deben demostrar redacción/retención antes de habilitarse; `TST-TERRITORY-URL-REDACTION` es obligatorio.
- **Owner:** Territory/GIS + Frontend; Integrations/SRE opera adapter, snapshot, egress y cuota; AppSec revisa schema/redacción; Privacy/Legal/Procurement aprueban finalidad, términos, licencia, región y DPA.
- **Evidencia R1 local + R0:** [`GeorefTerritoryClient.cs`](../../../src/AgropecuarIA.Territory/Providers/Georef/GeorefTerritoryClient.cs), [`TerritoryResolutionCache.cs`](../../../src/AgropecuarIA.Territory/Application/TerritoryResolutionCache.cs), [`TerritoryDatabaseSecurityTests.cs`](../../../tests/AgropecuarIA.Territory.Tests/TerritoryDatabaseSecurityTests.cs), [`GeorefTerritoryClientTests.cs`](../../../tests/AgropecuarIA.Territory.Tests/GeorefTerritoryClientTests.cs), [`AGRO-DIS-004/source-and-license-matrix.md`](../AGRO-DIS-004/source-and-license-matrix.md).
- **Gate:** GO local solamente para snapshot/search/UI y verificación del adapter. `NO-GO` a Georef externo hasta aprobar términos/licencia/atribución, región/DPA/retención/subencargados, SLA/cuota/capacidad, egress/DNS y redacción end-to-end; luego probar cobertura 23 provincias+CABA, schema drift, timeout/redirect/oversize, degradación y minimización. No se afirma SLA por el snapshot ni por tests locales.

### PI-05 — Open-Meteo

- **Estado/fase:** candidato condicionado para contrato meteorológico R2. La API gratuita es `NO-GO` para el SaaS comercial; no hay plan productivo contratado.
- **Datos y dirección:** backend/worker → Open-Meteo con coordenada/resolución mínima, variables y horizonte; respuesta → validación/staging → snapshot inmutable. Se conservan proveedor, modelo/corrida, emisión/vigencia, punto/celda, unidad, ingesta, hash y `fresh|stale|unavailable`. No se envían tenant, productor, campo ni reglas de negocio.
- **Región, credenciales, retención y subencargados:** región/DPA/subencargados/retención/plan/cuota/SLA no aprobados. API key server-side en secret manager; nunca navegador, URL registrada, métrica o error público.
- **Egress/SSRF:** adapter backend a base URL y rutas allow-listed; parámetros tipados/acotados, timeout, redirect policy, límite de respuesta y schema estricto. Sin URL aportada por usuario.
- **Degradación:** servir último snapshot con fuente/corrida/edad visible cuando aún sea utilizable; de lo contrario `unavailable`. Nunca completar lluvia ni pronóstico faltante con IA; caída no bloquea el núcleo transaccional.
- **Owner:** Weather/Agroclimate + Integrations; SRE controla cuota/frescura/circuit breaker; Privacy/Legal/Procurement Q-058 y contrato.
- **Evidencia R0:** [`AGRO-DIS-004/source-and-license-matrix.md`](../AGRO-DIS-004/source-and-license-matrix.md), [`AGRO-DIS-004/validation-report.md`](../AGRO-DIS-004/validation-report.md), [`docs/adr/ADR-005-meteorologia.md`](../../../docs/adr/ADR-005-meteorologia.md).
- **Gate:** plan comercial, región/DPA/subencargados/cuota/costo/SLA; validación contra observaciones locales; contract/schema drift, 429/5xx/timeout/redirect/body oversized, cache/frescura y atribución. Hasta entonces: `NO-GO productivo`.

### PI-06 — SMN CAP y SMN WRF

- **Estado/fase:** SMN CAP es fuente autoritativa candidata condicionada para R2. SMN WRF está `POSTERGADO/FUTURO`; no entra al camino MVP antes de presupuesto, operación y validación. Endpoints internos/no documentados del SMN son `NO-GO`.
- **Datos y dirección:** worker → feed CAP público; CAP → parser/staging → alertas versionadas con lifecycle, geometría, emisión, vigencia y fuente. Worker → dataset público WRF S3/NetCDF; archivo → parser limitado → grilla/snapshot. Las asociaciones con tenants/geometrías autorizadas ocurren internamente, no se envían al SMN.
- **Región, credenciales, retención y subencargados:** feeds/datasets observados son públicos y sin credencial, pero eso no demuestra hosting, logs, SLA o condiciones futuras. Retención interna de payload/hash/snapshot y costos WRF siguen sin aprobarse; licencias/atribución deben revalidarse.
- **Egress/SSRF:** URLs/buckets/keys y redirects allow-listed; CAP XML sin entidades externas/DTD, límites de bytes/elementos/coordenadas y schema; WRF con límite de tamaño, hash, variables/dimensiones/grilla y recursos de parser. Sin fetch arbitrario.
- **Degradación:** CAP inválido, HTML inesperado, stale o ciclo inconsistente se retira como señal oficial y muestra canal SMN directo; no se mantiene alerta vencida. Sin WRF se conserva Open-Meteo+CAP; WRF no es dependencia crítica.
- **Owner:** Weather + Integrations; GIS para intersección; SRE para frescura/costo; AppSec para parsers/egress.
- **Evidencia R0:** [`AGRO-DIS-004/source-and-license-matrix.md`](../AGRO-DIS-004/source-and-license-matrix.md), [`AGRO-DIS-004/validation-report.md`](../AGRO-DIS-004/validation-report.md), [`AGRO-DIS-004/AGRO-DIS-004-threat-model.md`](../AGRO-DIS-004/AGRO-DIS-004-threat-model.md).
- **Gate:** CAP: canal/autenticidad operativa, schema/lifecycle/frescura/XXE/oversize y fallback ensayados. WRF: autorización/presupuesto, storage/cómputo/cadencia, parser sandbox/limits y evaluación local. WRF permanece apagado hasta cerrar todos sus gates.

### PI-07 — Entrega de email

- **Estado/fase:** `FUTURO/NO SELECCIONADO`. Email verificado/OTP/recovery es requisito, pero no hay mecanismo ni proveedor de entrega aprobado. Email/WhatsApp/push de alertas no entra sin resolver los canales correspondientes.
- **Datos y dirección:** backend o IdP → proveedor de entrega con destinatario, plantilla, locale, token/link opaco y metadata mínima; webhooks de entrega/rebote → backend. No incluir tenant, CUIT, coordenadas, productividad, documentos ni motivo sensible en asunto/cuerpo/logs.
- **Región, credenciales, retención y subencargados:** proveedor, región, DPA, subencargados, retención de destinatarios/contenido/eventos y supresión no definidos. API/SMTP keys y webhook secrets en secret manager, separados por ambiente y rotados.
- **Egress/SSRF:** endpoint fijo/allow-listed; plantillas versionadas sin URLs o headers aportados por usuario. Webhooks autenticados, timestamped y replay-safe; links usan tokens one-shot/TTL y destino propio allow-listed.
- **Degradación:** respuestas de login/recovery permanecen anti-enumeración; reintentos acotados y conciliables. No se informa si una identidad existe; provider-down no habilita bypass de MFA/recovery ni duplica mensajes ilimitadamente.
- **Owner:** Identity para auth/recovery y contenido del mensaje; Integrations para el adapter de entrega; AppSec/Privacy/Legal/Procurement antes de seleccionar. No se crea un módulo `Notifications` sin una decisión arquitectónica posterior.
- **Evidencia R0:** [`docs/05-arquitectura.md`](../../../docs/05-arquitectura.md), [`docs/07-seguridad-y-privacidad.md`](../../../docs/07-seguridad-y-privacidad.md), [`AGRO-DIS-003/validation-report.md`](../AGRO-DIS-003/validation-report.md).
- **Gate:** decisión de mecanismo/proveedor; DPA/región/retención/subencargados; sandbox con anti-enumeración, OTP/link one-shot/TTL, rate limit/OTP bombing, header/template injection, bounce/webhook spoof/replay y outage. Hasta entonces: `NO-GO productivo`.

### PI-08 — Proveedor/modelo de IA

- **Estado/fase:** `FUTURO R6/NO SELECCIONADO`. El AI Gateway, proveedor y modelos siguen pendientes. Mutaciones autónomas, cálculo crítico por LLM, navegación/SQL/fetch libre y entrenamiento compartido sin consentimiento explícito son `FUERA DE ALCANCE/NO-GO`.
- **Datos y dirección:** backend AI Gateway → proveedor con un `EvidencePack` mínimo ya autorizado; proveedor → explicación/citas estructuradas que el backend valida. Pueden incluir hechos de clima/rotación autorizados, fechas, supuestos y evidencia; no se envían credenciales, documentos completos innecesarios, tenant IDs, datos de otros tenants ni herramientas de mutación.
- **Región, credenciales, retención y subencargados:** proveedor/modelo/región/DPA/subencargados/no-training/retención/abuse monitoring no aprobados; Q-058 y Q-060 son `NO-GO`. Keys server-side en secret manager; una key por ambiente/servicio con cuota y rotación.
- **Egress/SSRF:** solo gateway y modelos allow-listed. Herramientas determinísticas se invocan internamente y reautorizan cada recurso; el modelo no elige hosts ni ejecuta URL, SQL, filesystem o red libre.
- **Degradación:** kill switch por tenant/caso/modelo; el producto transaccional y los cálculos determinísticos siguen disponibles. Provider-down, output inválido, evidencia insuficiente o perfil incompatible producen abstención visible, no una respuesta fabricada.
- **Owner:** Analytics/AI + AppSec; módulos de dominio poseen cálculos/evidencia; QA y especialistas aprueban evals; Privacy/Legal/Sponsor aprueban proveedor/caso.
- **Evidencia R0:** [`docs/08-estrategia-ia.md`](../../../docs/08-estrategia-ia.md), [`docs/adr/ADR-004-ia-control-humano.md`](../../../docs/adr/ADR-004-ia-control-humano.md), [`tasks/backlog/EPIC-12-ia-analitica.md`](../../backlog/EPIC-12-ia-analitica.md).
- **Gate:** caso/dataset/evals/retención aprobados; DPA/región/subencargados/no-training; threat model de prompt injection y tool authorization; tenant isolation/RAG, permiso revocado mid-flow, exfiltración, cita falsa, schema inválido, costo/cuota, drift y kill switch. Hasta entonces: `NO-GO productivo`.

### PI-09 — Hosting, edge, CDN y WAF

- **Estado/fase:** `EXTERNO PENDIENTE/NO SELECCIONADO`. API y web existen como runtime R1 local, pero no existe cuenta de hosting/edge, región, contrato, origen compartido ni despliegue productivo.
- **Datos y dirección:** Internet ↔ edge/CDN/WAF ↔ web/API. Cruzan IP, headers, cookies de sesión, rutas, status, tamaños y contenido público; el edge no debe cachear respuestas tenant/privadas ni registrar query, cookie, body, coordenadas o identificadores de recurso.
- **Región, credenciales, retención y subencargados:** hosting, regiones, DPA, logs, retención, subencargados, soporte y SLA desconocidos. Certificados, DNS, WAF/API tokens y credenciales de origen requieren workload identity/secret manager, rotación y mínimo privilegio.
- **Egress/SSRF:** origen inaccesible salvo desde edge/red aprobada; headers de forwarding se aceptan solo del proxy confiable. No hay proxy de URL libre. CSP/CORS/origin/redirects y cache keys se fijan por configuración versionada.
- **Degradación:** fallo de edge no habilita bypass al origen ni contenido privado stale. Rate limit/WAF degradado activa deny o capacidad acotada según runbook; el núcleo no confirma mutaciones con respuesta incierta.
- **Owner:** Platform/SRE + AppSec; Frontend/API poseen CSP, CORS, CSRF, cookies y headers; Privacy/Legal/Procurement aprueban tratamiento.
- **Evidencia R0:** [`docs/05-arquitectura.md`](../../../docs/05-arquitectura.md), [`docs/07-seguridad-y-privacidad.md`](../../../docs/07-seguridad-y-privacidad.md), [`AGRO-DIS-007/validation-report.md`](../AGRO-DIS-007/validation-report.md).
- **Gate:** región/DPA/subencargados/retención/SLA; TLS/origin lockdown; CSP/headers/cookies; XSS/CSRF/CORS/clickjacking; cache poisoning y dos tenants sin cache cross-leak; rate-limit/DoS, failover, logs redactados y revocación. Hasta entonces: `NO-GO productivo`.

### PI-10 — Observabilidad y product analytics

- **Estado/fase:** el SDK OpenTelemetry está integrado localmente en Identity y emite señales allow-listed verificadas por tests. Collector/exporter/backend y analítica de producto son `EXTERNO PENDIENTE/NO SELECCIONADO`.
- **Datos y dirección:** browser/runtime/worker → collector/backend con ruta template, método, clase de status, dependencia, latencia, correlation y tenant seudonimizado. Payload, query, CUIT, email, coordenadas, filename, UUID de recurso, tokens y secretos están prohibidos.
- **Región, credenciales, retención y subencargados:** región, DPA, subencargados, acceso, sampling, retención, borrado y costo desconocidos. API keys/certificados OTLP son server-side, separados por ambiente y rotados.
- **Egress/SSRF:** exporter a endpoint fijo/mTLS o autenticado; no acepta destino por tenant/request. El navegador no carga analytics de tercero sin decisión de privacidad, CSP y minimización aprobadas.
- **Degradación:** exporter caído descarta/bufferiza solo dentro de límites sin bloquear transacciones ni volcar payload a disco/log. Alta cardinalidad o presupuesto excedido activa sampling/disable, no remueve redacción.
- **Owner:** Platform/SRE + Privacy/AppSec; cada módulo define señales de bajo riesgo; Product decide si existe analytics opcional sin dark patterns.
- **Evidencia R1 local + R0 descartable:** [`src/AgropecuarIA.Identity/IdentityTelemetry.cs`](../../../src/AgropecuarIA.Identity/IdentityTelemetry.cs), [`tests/AgropecuarIA.Identity.Tests/IdentityTelemetryTests.cs`](../../../tests/AgropecuarIA.Identity.Tests/IdentityTelemetryTests.cs), [`apps/AgropecuarIA.Api/Program.cs`](../../../apps/AgropecuarIA.Api/Program.cs), [`AGRO-DIS-007/sli-slo-catalog.md`](../AGRO-DIS-007/sli-slo-catalog.md).
- **Gate:** backend/región/DPA/retención/acceso/costo; allow-list schema, secret/PII canaries, cardinalidad, sampling y outage end-to-end; consentimiento/base aplicable para analytics. Hasta entonces: `NO-GO productivo`.

### PI-11 — CI, registros de paquetes y artefactos

- **Estado/fase:** `CI AUSENTE/NO SELECCIONADO`. El producto fija dependencias con `packages.lock.json` para API/Identity/tests/fitness y `apps/web/pnpm-lock.yaml`; no existe pipeline, identidad CI, registro OCI, firma, SBOM publicada ni provenance.
- **Datos y dirección:** developer/repository → CI; package registries → build; CI → artifact registry/deployment. Cruzan código, manifests, dependencias, SBOM, resultados de scan, artefactos y credenciales de promoción; nunca datos tenant reales.
- **Región, credenciales, retención y subencargados:** proveedor/región/retención de logs/artefactos, runners y subencargados pendientes. Preferir OIDC/workload identity efímera, scopes por ambiente, branches/protections y ningún secreto de larga vida en repo.
- **Egress/SSRF:** runners con egress mínimo a registries/repositorios allow-listed; builds no descargan scripts no fijados ni acceden a metadata/cloud innecesaria. Instalación frozen/locked y fuentes aprobadas.
- **Degradación:** registry/scanner/provenance caído bloquea promoción; no se omite el gate. Artefacto vulnerable o sin firma/SBOM permanece no promovible.
- **Owner:** Platform + AppSec; owners de módulo mantienen dependencias; QA consume artefactos inmutables.
- **Evidencia R1 local:** [`apps/AgropecuarIA.Api/packages.lock.json`](../../../apps/AgropecuarIA.Api/packages.lock.json), [`src/AgropecuarIA.Identity/packages.lock.json`](../../../src/AgropecuarIA.Identity/packages.lock.json), [`tests/AgropecuarIA.Identity.Tests/packages.lock.json`](../../../tests/AgropecuarIA.Identity.Tests/packages.lock.json), [`apps/web/pnpm-lock.yaml`](../../../apps/web/pnpm-lock.yaml), [`AGRO-FND-001/validation-report.md`](../AGRO-FND-001/validation-report.md).
- **Gate:** proveedor/runner/IAM aprobados; protected review, frozen install, SAST/SCA/secrets/SBOM/licencias, firma/provenance, reproducibilidad, permisos mínimos y revocación probados. Alta/crítica o secreto expuesto bloquea release.

### PI-12 — Backup, archivo inmutable y recuperación administrada

- **Estado/fase:** `FUTURO/NO SELECCIONADO`. El drill local prueba mecánica sintética; no demuestra un servicio de backup, PITR, WORM, región ni RPO/RTO administrados.
- **Datos y dirección:** PostgreSQL/PostGIS/object storage/auditoría → servicio de backup/archivo; operador break-glass → restore aislado → reconciliación/promoción. Hereda todas las clasificaciones, tenants, holds, versiones y obligaciones del origen.
- **Región, credenciales, retención y subencargados:** región primaria/copia, DPA, subencargados, retención, eliminación, key escrow, soporte y salida desconocidos. Principal/CMK separados del runtime, MFA/step-up y protección de borrado.
- **Egress/SSRF:** solo agentes/endpoints aprobados y red privada; manifests y checksums autenticados. El operador no restaura sobre producción ni elige una URL/objeto arbitrario sin aprobación.
- **Degradación:** backup atrasado, mutable, sin key o manifest incompleto alerta y bloquea gate de release afectado. Restore permanece aislado hasta reconciliar DB, PostGIS, objetos, auditoría, holds y supresiones.
- **Owner:** SRE + Data; Privacy/Legal define retención/hold/purge; AppSec revisa IAM/keys/break-glass.
- **Evidencia R0:** [`AGRO-DIS-005/runbook.md`](../AGRO-DIS-005/runbook.md), [`AGRO-DIS-005/validation-report.md`](../AGRO-DIS-005/validation-report.md), [`docs/adr/ADR-007-storage-retencion-recuperacion.md`](../../../docs/adr/ADR-007-storage-retencion-recuperacion.md).
- **Gate:** proveedor/región/DPA/retención/keys; backup inmutable y separado; PITR; restore representativo DB+GIS+objects+audit; rights/hold/purge y roll-forward; RPO/RTO medidos y aceptados. Hasta entonces: `NO-GO productivo`.

### PI-13 — Productive Core PostgreSQL local

- **Estado/fase:** `INTEGRADO LOCAL/SIN PROVEEDOR EXTERNO`. Cubre únicamente crear, listar y ver campos draft no espaciales.
- **Datos y dirección:** navegador same-origin → API autenticada → puerto Identity SQL → schema PostgreSQL `productive_core`. Cruzan actor/tenant/session derivados por servidor, nombre de campo `Confidential`, UUID internos, fingerprint/aliases HMAC versionados, journal y outbox tipado. No cruzan coordenadas, área, geometría, key idempotente cruda, token ni identidad externa.
- **Región, credenciales, retención y subencargados:** PostgreSQL efímero local; no acredita DB administrada, región, backup, DPA, subencargados, retención ni secret manager. Roles owner/app/job son `NOLOGIN`, `NOINHERIT`, `NOBYPASSRLS`; el principal de prueba es efímero.
- **Egress/SSRF:** ninguno en este slice. No recibe URL, host, callback ni destino por tenant/request. El outbox se persiste localmente y no tiene worker de delivery.
- **Degradación:** sesión vencida/revocada, membership removida, tenant ajeno o contexto ausente niegan antes de lookup/replay; error de DB/serialización revierte la unidad, ledger, journal y outbox. La ausencia del puerto Identity o un orden de migración incorrecto falla cerrado.
- **Owner:** Productive Core + Identity + Data + AppSec.
- **Evidencia R1 local:** [`contracts/productive-core.openapi.yaml`](../../../contracts/productive-core.openapi.yaml), [`ProductiveCoreDbContext.cs`](../../../src/AgropecuarIA.ProductiveCore/Infrastructure/ProductiveCoreDbContext.cs), [`ProductiveCoreDatabaseSecurityTests.cs`](../../../tests/AgropecuarIA.ProductiveCore.Tests/ProductiveCoreDatabaseSecurityTests.cs), [`ProductiveCoreAuthorizationPortDatabaseTests.cs`](../../../tests/AgropecuarIA.Identity.Tests/ProductiveCoreAuthorizationPortDatabaseTests.cs).
- **Gate:** `GO integrado-local` solo para campos draft no espaciales. Geometría/área/tiles/historial, roles no-owner, delivery externo, DB/backup/credenciales administradas, hosting compartido y producción permanecen `NO-GO`.

## Mapeo amenaza → control → prueba

| Amenaza/impacto | Superficies | Controles mínimos antes de runtime | Evidencia/prueba exigida |
|---|---|---|---|
| Toma de cuenta, linking indebido, sesión web o recovery enumerable | PI-01, PI-07, PI-09 | OIDC/PKCE, CSP/CSRF/CORS/cookie segura, doble reautenticación, tokens one-shot, rate limit, sesión revocable, respuesta neutral | PASS local para contrato OIDC, step-up, replay/concurrencia y aislamiento de endpoint sintético; PENDIENTE sandbox externo para callback/factor/recovery/revocación, CSP/edge y outage/failover |
| Fuga cross-tenant/BOLA o privilegio platform↔tenant | PI-01, PI-02, PI-03, PI-08, PI-09, PI-13 | authz por recurso antes de acceso/ETag/tool, RLS, claves compuestas, binding tenant, scope discriminado y cache privada deshabilitada o tenant-safe | dos tenants por API/DB/job/cache/edge/storage/AI; rol sin contexto; pool reutilizado; membership removida; cache poisoning/cross-leak; grants y tools ajenos; error neutral |
| Robo/abuso de credencial o clave | PI-01–PI-13 | secret manager/KMS, workload identity, scope mínimo, rotación, no secretos en cliente/log/URL | secret scan; permisos efectivos; rotación/revocación; token/key ausente de errores, trazas, bundles y snapshots |
| SSRF/egress no controlado y exfiltración | PI-04–PI-11 | hosts/esquemas/redirects allow-listed, resolución segura, sin URL libre, payload/response limits | loopback, link-local/metadata, IPv6 local, DNS rebinding, redirect externo, body oversized, timeout |
| Proveedor/fuente comprometido o schema drift | PI-04–PI-06, PI-08 | schema estricto, hash/snapshot, staging, revisión/publicación, citas/evals, fuente/fecha visibles | fixture válido/adverso, tipo/enum desconocido, hash cambiado, CAP lifecycle, cita falsa, model/schema drift |
| Malware, parser exploit o agotamiento de recursos | PI-03, PI-06 | cuarentena fail-closed, MIME/hash/tamaño, parser sin XXE, límites de compresión/geometría/grilla | EICAR sintético, polyglot/zip bomb, XML DTD/entity, CAP/NetCDF oversized, timeout/memoria/cancelación |
| Pérdida, corrupción, retención o borrado ilegal | PI-02, PI-03, PI-08, PI-12, PI-13 | política por clase, hold, versionado/WORM, PITR, manifest/hash, roll-forward, borrado conciliable | backup/restore aislado y proveedor real; hold/purga; objeto huérfano/corrupto; referencias/auditoría consistentes |
| Indisponibilidad o datos stale presentados como actuales | PI-01–PI-13 | timeout/circuit breaker, cache/snapshot con edad, retry acotado, kill switch/fallback, núcleo independiente | provider-down/429/5xx; retry storm; CAP vencido; cache stale; IdP/DB/storage/email/edge/telemetría/registry/backup/IA caídos y recovery probado |
| Transferencia, retención o subencargado no autorizado | PI-01–PI-13 | inventario completo, minimización, DPA/región/subencargados/retención, `VAL-LEG`, no-training cuando aplica | revisión documental nominada; configuración/región contrastada; export/delete verificados; evidencia de no payload sensible |

## Plantilla obligatoria para incorporar o cambiar un proveedor

No se integra código, credencial ni dato real hasta completar y aprobar esta ficha:

```text
ProviderChangeId:
Owner técnico:
Owner de negocio:
Servicio/proveedor/plan/versión:
Fase y feature flag/kill switch:
Propósito y alternativa/degradación:
Dirección de cada flujo y protocolo:
Datos/categoría/sujetos; campos minimizados:
Tenant/platform y reglas de autorización:
Región primaria, réplicas, backups y soporte:
DPA/base contractual/transferencia/subencargados:
Retención, borrado, legal hold, exportabilidad y exit plan:
Credenciales, IAM/KMS, rotación y break-glass:
Hosts/puertos/redirects/DNS/egress permitidos:
SLA/cuota/rate limit/costo/alertas:
Schemas, versiones, idempotencia y compatibilidad N/N-1:
Threats enlazadas y riesgo residual:
Pruebas sandbox/contrato/negativas/fault injection/restore:
Evidencia primaria y fecha de revalidación:
Aprobaciones AppSec, Privacy/Legal, SRE, owner y Sponsor:
Decisión GO/NO-GO y condiciones:
```

Gate automático: cualquier campo de región, tratamiento, retención, subencargados, credencial, egress, degradación, prueba o owner vacío produce `NO-GO`. Un probe público o fixture confirma únicamente el comportamiento observado; nunca reemplaza sandbox de la cuenta/plan, contrato, revisión legal ni operación productiva.
