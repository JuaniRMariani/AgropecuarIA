# Clasificación de datos y evaluación de privacidad R0/R1

**Tarea:** `AGRO-SEC-001`  
**Fecha de revisión:** 2026-08-11  
**Alcance:** baseline de decisión reconciliada con el runtime R1 local de Identity y Territory sobre API/web/PostgreSQL; no es dictamen legal, aviso de privacidad, aprobación de proveedor ni autorización de despliegue compartido/productivo.

## Niveles de evidencia

- **Runtime R1 local integrado:** `apps/AgropecuarIA.Api`, `src/AgropecuarIA.Identity`, `apps/web` y sus pruebas ejecutan autenticación/sesión, linking y step-up contra PostgreSQL local. Es código de producto ejecutable, no evidencia de un entorno compartido.
- **Solo Development/Test:** el proveedor OIDC sintético, endpoints de autenticación sintética y configuración local existen únicamente para desarrollo/pruebas; no constituyen un IdP externo ni pueden habilitarse en otro ambiente.
- **Evidencia R0 descartable:** `tasks/evidence/AGRO-DIS-*` demuestra contratos o mecánica con fixtures sintéticos. En particular, `AGRO-DIS-003` aprobó internamente la decisión RLS/discovery con SCRAM-SHA-256 local/TCP, cuatro secretos efímeros distintos, archivos con ACL exclusiva del usuario actual y validación fail-fast del principal discovery exacto. No es runtime integrado ni se promueve como scaffold.
- **Externo/compartido/productivo:** Auth0, hosting, edge, collector OTLP, PostgreSQL administrado, regiones, DPA, retención y credenciales reales siguen ausentes o sin aprobación.
- **CI/release:** existen lockfiles de NuGet y pnpm para el producto, pero no pipeline, identidad de workload, SBOM, provenance, firma ni registro de artefactos.

## Reglas de uso

La clasificación de referencia conserva las cinco clases de `docs/07-seguridad-y-privacidad.md`. Una superficie adopta la clase más restrictiva presente; cifrado no reduce la clase. `Organization` es el tenant técnico y controla operativamente acceso, pero no resuelve propiedad, responsable legal ni relación propietario/productor/asesor: esas decisiones continúan condicionadas por Q-054/Q-055 y GAP-008 en `docs/11-preguntas-discovery.md` y `tasks/decisions-and-gaps.md`.

Escala C/I/A: `A`, impacto alto; `M`, medio; `B`, bajo. Es una valoración cualitativa iniciada en R0 y reconciliada con el runtime local R1; cada slice debe revisarla contra sus datos y operación reales.

| Clase | Ejemplos del producto | C/I/A | Scope permitido | Regla mínima |
|---|---|---|---|---|
| Público | contenido comercial aprobado; fuentes y catálogo nacional **publicados** | B/M/M | `platform` | Publicación explícita y owner; un candidato o borrador no es público. |
| Interno | arquitectura, inventario de proveedores, catálogo no sensible aún no publicado, fixtures sintéticos | M/M/M | `platform` o entorno de ingeniería | Autenticado; sin datos reales copiados a fixtures o tickets. |
| Confidencial | geometrías y ubicación rural, productividad, stock, contratos, costos, documentos, observaciones y recomendaciones | A/A/M-A | `tenant`; excepcionalmente `platform` solo si el contrato lo declara | Autorización por recurso, aislamiento tenant, minimización y cifrado. |
| Fiscal/personal | identidad/contacto, membresía, CUIT, pagos/facturas y actor/IP/dispositivo de auditoría | A/A/M-A | `platform` o `tenant` discriminado, nunca ambiguo | Finalidad nominada, acceso mínimo, step-up en exportes/acciones sensibles y tratamiento legal pendiente. |
| Secreto | tokens de sesión/recuperación, OTP, claves privadas, credenciales de proveedor y claves de firma | A/A/A | sin scope de negocio; custodia técnica | Solo secret manager/KMS/HSM o memoria estrictamente necesaria; nunca payload de aplicación. |

El scope discriminado `platform | tenant` y la prohibición de usar CUIT, IDs del cliente o correlación como autoridad provienen de `tasks/evidence/AGRO-FND-001/contract-policy.md`. Mapas, productividad y datos patrimoniales ya están clasificados como confidenciales en `docs/07-seguridad-y-privacidad.md`; no se infiere una categoría legal adicional.

## Finalidad y minimización

- **Identidad y tenancy:** el runtime R1 conserva issuer/subject externos, label mínimo, hash de sesión opaca/revocable, `AuthenticatedAtUtc`, assurance gruesa, `StrongAuthenticatedAtUtc`, purpose, organizaciones privadas, memberships owner, aliases/ledger HMAC sin clave cruda, journal y outbox tipado. `acr`/`amr`/`auth_time` se validan en callback pero no se persisten como claims crudos. No persiste contraseña, OTP, recovery code, token ni idempotency key en claro; email, nombres de organización, claims, tokens y UUID no se copian a telemetría. Los payloads outbox v1 sí incluyen UUID internos mínimos como contrato compatible, no como autoridad ni label. Q-054/Q-055 no autorizan fusionar clientes de un asesor en un tenant; el default mantiene una `Organization` separada por cliente (`tasks/decisions-and-gaps.md`).
- **Operación agropecuaria:** recolectar solo identidad del recurso, fecha efectiva/registro, origen, unidad, evidencia y campos requeridos por el caso de uso. Mantener ubicación legal, operación y geometría separadas (`tasks/lessons .md`; `docs/04-reglas-y-modelo-de-datos.md`).
- **GIS y clima:** el snapshot Territory local contiene códigos/nombres/jerarquía y centroides públicos oficiales, más hash, versión, origen y vigencia; no contiene parcela, productor ni tenant. Search no sale del backend. Resolve requiere que la persona escriba latitud/longitud y advierte que el resultado no delimita parcela ni ofrece precisión agronómica. La coordenada exacta se envía únicamente al Georef v2.0 fijo, vive temporalmente en un caché volátil acotado y no se persiste en PostgreSQL/journal, browser storage ni logs de aplicación. Es ubicación `Confidential` mientras está en tránsito/caché. Georef externo continúa `NO-GO` hasta aprobar región/DPA/retención/subencargados/licencia/SLA/cuota y probar redacción de query strings en proxy/collector (`src/AgropecuarIA.Territory/Application/TerritoryResolutionCache.cs`; `tasks/evidence/AGRO-SEC-001/provider-processing-inventory.md`, `PI-04`).
- **Archivos:** almacenar binario privado y metadatos/hash mínimos, con vínculo opaco al recurso. No deduplicar entre tenants ni habilitar descarga sin veredicto `clean` (`tasks/evidence/AGRO-DIS-005/README.md`).
- **Telemetría y auditoría:** el runtime Identity emite localmente actividades/métricas OpenTelemetry con allow-list y tests de baja cardinalidad. Territory registra solamente código de error y correlation ID; no query, coordenada ni payload provider. No hay exporter, collector, backend ni proxy compartido validado, por lo que `TST-OTEL-REDACTION` y `TST-TERRITORY-URL-REDACTION` siguen pendientes y bloquean egress real. Sin payload, CUIT, email, coordenadas, UUID de negocio, token u OTP (`src/AgropecuarIA.Territory/Delivery/TerritoryExceptionHandler.cs`; `tests/AgropecuarIA.Identity.Tests/IdentityTelemetryTests.cs`).
- **IA:** solo paquetes de evidencia autorizados y mínimos; sin claves, payload fiscal completo ni coordenadas/identidades innecesarias. No entrenamiento compartido por defecto; proveedor, región, retención y subencargados deben aprobarse antes de enviar datos (`docs/08-estrategia-ia.md`).
- **Soporte:** acceso implícito deshabilitado. El acceso JIT pertenece a `AGRO-ID-005` R7 y requiere consentimiento, motivo, alcance, caducidad, step-up y auditoría (`tasks/decisions-and-gaps.md`).

## Canales, ownership y gates

| Clase | Canales permitidos | Canales prohibidos | Owner de decisión | Gate/test antes de habilitar |
|---|---|---|---|---|
| Público | CDN/web y exporte público después de publicación aprobada | publicar candidato, extensión tenant o dato derivado sin revisión | Product + owner del módulo | aprobación/version/hash; test que candidato/tenant no sea público |
| Interno | repositorio privado, herramienta autenticada, CI con fixture sintético | CDN público, issue/ticket con dato real, fixture tomado de producción | owner del módulo + QA | inventario y scan de secretos/datos reales; permisos mínimos |
| Confidencial | API server-side, PostgreSQL/RLS, worker con contexto, storage privado, backup cifrado | CDN, bucket público, localStorage, logs/metrics, proveedor/IA sin gate, cache sin tenant | owner del módulo + AppSec/Data | authz por recurso y tenant negativa en API/DB/cache/job/storage/export; redacción |
| Fiscal/personal | servicios autorizados, DB/storage privado, exporte controlado con step-up | métricas, trazas, prompts, URL pública, nombre de archivo/objeto, soporte implícito | Product/Privacy/Legal + owner del módulo | finalidad/campos/roles aprobados; rights/export/rectification tests; DPA/región si sale del control propio |
| Secreto | vault/KMS/HSM, inyección efímera y memoria acotada | repo, DB de negocio, backup general, navegador, log, error, métrica, prompt o exporte | Platform/AppSec | secret scan, rotación/revocación, acceso auditado y prueba de ausencia en artefactos/logs |

Para toda clase no pública, los exports, backups, caches, jobs y telemetría heredan la clase y el scope del dato fuente. Un hash, UUID opaco o seudónimo reduce exposición pero no convierte el dato en público.

## Actores y responsabilidad todavía condicional

| Actor técnico | Autoridad R0 | Límite pendiente |
|---|---|---|
| Usuario miembro | acciones explícitas según rol, campo/recurso y estado | no es dueño legal por estar autenticado |
| Admin/owner de `Organization` | membresías y operación del tenant, con step-up para acciones sensibles | controlador/responsable, titularidad y relación con terceros requieren Q-054/Q-055 |
| Asesor multi-cliente | acceso delegado a organizaciones separadas | no replica ni transfiere datos entre clientes |
| Operador de plataforma | operación nominada y mínima; sin acceso implícito tenant | soporte JIT no está habilitado en MVP |
| Proveedor/subencargado | solo contrato, finalidad, región y campos aprobados | IdP, clima, storage, telemetría e IA continúan candidatos, no autoridades productivas |
| Especialista/Legal | aprobación dentro de su competencia | el software y este assessment no sustituyen su firma |

Hasta cerrar GAP-008, la UI y los contratos deben decir “acceso” o “control operativo”, no “propietario de los datos”.

## Retención, legal hold y purga: `NO-GO` productivo

No existe una tabla de plazos aprobada. Q-060 mantiene abiertos retención, SLA y soporte; Q-058 mantiene abiertos proveedor/región para IA y clima, y no autoriza almacenamiento internacional. `LegalHold` prevalece sobre purga solo como invariante técnica; no define causal ni duración legal (`tasks/decisions-and-gaps.md`; `tasks/evidence/AGRO-DIS-005/README.md`).

Quedan bloqueados hasta aprobación de Privacy/Legal/Sponsor:

1. activar retención o purga automática con datos reales;
2. contratar proveedor, región o transferencia internacional;
3. enviar datos tenant a IA, telemetría o soporte externo;
4. afirmar supresión completa sin probar primario, índices/proyecciones, objetos, auditoría permitida y restauración desde backup;
5. definir que un restore no reactiva datos previamente suprimidos o bajo hold contrario.

El drill sintético de `AGRO-DIS-005` demuestra mecánica local de hold/purga/restore, no plazos, PITR administrado ni cumplimiento legal. El RPO/RTO de `AGRO-DIS-007` es una hipótesis de ingeniería, no contrato.

## Consentimiento, estados y errores sin dark patterns

- Explicar finalidad, datos, destinatarios, vigencia/revocación y efecto real antes de solicitar una decisión; no presentar consentimiento como única base legal sin revisión.
- Opt-in opcional sin preselección, granular y revocable; aceptar y rechazar con igual claridad/prominencia. Rechazar no degrada el núcleo salvo que el dato sea estrictamente necesario y se explique.
- Separar consentimiento de términos obligatorios y de aprobación humana de una recomendación. No reutilizar una aceptación para entrenamiento, marketing o acceso de soporte.
- Mantener estados explícitos `loading`, `empty`, `offline`, `stale`, `provider-down`, `error`, `forbidden` y `conflict` según `RNF-UX-003` en `docs/09-calidad-y-pruebas.md`; conservar la entrada del usuario cuando sea seguro.
- Errores de recurso ajeno/inexistente/no autorizado no revelan existencia ni diff. La UI no muestra payload, secreto, coordenada, tenant o identificador completo; aplica la política 401/403/404/409/412 de `tasks/evidence/AGRO-FND-001/contract-policy.md`.
- Registrar versión y timestamp de la decisión, finalidad y mecanismo de retiro sin copiar texto libre o contenido sensible a telemetría.

## Evaluación R0/R1 por superficie

| Superficie | Datos/riesgo principal | Evidencia actual | Decisión R0 / gate pendiente |
|---|---|---|---|
| Identidad/tenancy | personal, sesión, account linking, invitaciones bearer y acceso cruzado | runtime R1 local API/web/PostgreSQL con OIDC/PKCE, CSRF, rate limit, step-up, `CreateOrganization`, invitación one-shot y remoción de otro co-owner; el token crudo se muestra una vez y sólo persiste un digest HMAC versionado; las fronteras tenant integran roles mínimos, `SET LOCAL`, `FORCE RLS` y negativos A/B/sin contexto/pool/job; la remoción muestra display name y UUID corto, nunca userId/email/identidad externa; el spike `AGRO-DIS-003` conserva evidencia separada de discovery 0/1/N | GO local para bootstrap, invitación y remoción de otro co-owner con último-owner protegido; NO-GO para email/delivery, roles no-owner, self-remove/transfer/democión, discovery general y nuevas mutaciones hasta repetir migraciones/principals/grants/negativos, y NO-GO IdP/DB/entorno compartido hasta credenciales administradas, sandbox, DPA/región/retención/subprocesadores/exportabilidad |
| Catálogo | baseline global, fuentes, aprobadores; contaminación por extensión tenant | candidato trazable en `tasks/evidence/AGRO-DIS-001/README.md` | GO para revisión; NO-GO publicación hasta acta y firmas nominadas |
| GIS/clima/mapa | ubicación exacta, historial espacial y envío a terceros | Territory integra snapshot público local de 23 provincias+CABA, búsqueda autenticada y resolución explícita. Coordenada exacta `Confidential`: solo Georef v2.0, caché volátil acotado, sin persistencia/journal/browser/log de aplicación; tests estrictos de contrato, provider, degradación y storage frontend | GO local para snapshot/search y UI; adaptador verificable localmente. NO-GO tráfico Georef real hasta Q-058/060, licencia/atribución, DPA/región/retención/subencargados/SLA/cuota, egress y redacción URL end-to-end |
| Archivos/backups/exports | documentos, malware, URL, restauración y fuga masiva | storage local/restore sintético en `tasks/evidence/AGRO-DIS-005/validation-report.md` | GO para contrato; NO-GO cloud/AV/retención/exporte real hasta sandbox, IAM/KMS, región/DPA/hold/purge |
| Telemetría | fuga por payload/IDs y cardinalidad | SDK local y tests de allow-list en `src/AgropecuarIA.Identity/IdentityTelemetry.cs`; política sintética en `AGRO-DIS-007` | GO para emisión local; NO-GO exporter/backend hasta redacción end-to-end, región/DPA, acceso, presupuesto y retención |
| IA/analítica | prompt injection, exfiltración, inferencias y entrenamiento | estrategia read-only en `docs/08-estrategia-ia.md`; sin proveedor/runtime | NO-GO envío de datos hasta caso, paquete autorizado, eval/threat model, Q-058, no-training y kill switch |
| Exporte/privacidad | descarga masiva, destinatario erróneo, rectificación/supresión | requisitos en `docs/07-seguridad-y-privacidad.md`; sin slice productivo | NO-GO hasta política, step-up/authz, manifest/auditoría, retención/hold y E2E de derechos |

Los artefactos bajo `tasks/evidence/AGRO-DIS-*` son spikes descartables y usan fixtures sintéticos. Sus controles prueban contratos o mecánica local; no son la aplicación, no almacenan datos productivos y no autorizan reutilizar el scaffold. El runtime R1 integrado debe repetir cualquier gate que todavía dependa del spike, especialmente tenant/RLS. Los lockfiles del producto habilitan restore reproducible local, no prueban CI, provenance ni artefactos confiables.

## Checklist de privacidad para toda nueva superficie

- [ ] Outcome, owner del módulo y actor nominados; no hay regla profesional/legal inventada.
- [ ] Inventario campo por campo con clase, C/I/A, `platform|tenant`, finalidad, origen y obligatoriedad.
- [ ] Q-054/Q-055 resueltas si la superficie afirma propiedad, controlador/responsable o comparte con terceros.
- [ ] Diagrama de flujo incluye navegador, API, DB, cache, job, archivo, backup, exporte, telemetría, IA y cada proveedor/región.
- [ ] Minimización demostrada; secretos y payload sensible excluidos de cliente, logs, errores, métricas y prompts.
- [ ] Authn, autorización por recurso/acción/estado y aislamiento tenant probados positiva y negativamente en cada canal.
- [ ] Proveedor tiene finalidad, campos, región, DPA/subencargados, retención, borrado, exportabilidad y kill/revocación aprobados.
- [ ] Retención, hold, purga y restore aprobados y probados; si no, la capacidad permanece `NO-GO`.
- [ ] Aviso/consentimiento/errores son accesibles, reversibles y sin dark patterns; rechazo y provider-down tienen comportamiento explícito.
- [ ] Exportes aplican step-up, alcance mínimo, auditoría y pruebas cross-tenant; ningún UUID completo se muestra sin necesidad explícita.
- [ ] Abuse cases incluyen BOLA, admin/insider, third-party breach, backup/restore, exporte, prompt injection y datos obsoletos.
- [ ] Tests, señales/alertas redactadas, rollback/kill switch, riesgo residual y owner están enlazados a la tarea antes de `Ready`.
- [ ] Legal/Privacy firma cualquier interpretación normativa; este assessment solo informa el gate técnico.

Una amenaza crítica sin control y owner, un canal con scope ambiguo o cualquiera de los `NO-GO` anteriores bloquea la superficie afectada.
