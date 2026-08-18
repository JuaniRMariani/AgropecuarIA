# Gates de seguridad por release

Estado: baseline R0 más gate local R1 de `AGRO-SEC-001`. Es un mecanismo de decisión; separa controles integrados y reproducidos localmente de proveedores, plataforma y decisiones todavía no aprobados.

## Regla transversal

Una frontera o proveedor nuevo entra a la Definition of Ready únicamente cuando registra datos, protocolo, autenticación/autorización, validación, límites, owner, amenazas, controles, pruebas, señales operativas y riesgo residual. Una amenaza crítica abierta sin owner o una mitigación crítica sin prueba bloquea solo la capacidad afectada. Un hallazgo alto/crítico introducido bloquea su release.

Los gates conservan explícitas las preguntas abiertas: Q-054/Q-055 para modelo comercial, controlador, titularidad y delegación; Q-058 para proveedor/región de IA y clima; Q-060 para SLA, soporte y retención. Ningún default técnico R0 responde esas preguntas ni habilita producción.

`ADR-PEND-007` está aceptada para desarrollo R1 y ya se implementa en fronteras runtime acotadas de Identity y Productive Core mediante principals separados, contexto transaccional y `FORCE RLS`. El spike `AGRO-DIS-003` conserva evidencia descartable de discovery, pero no autoriza nuevas rutas: cada módulo debe repetir migraciones, grants y negativos A/B/sin contexto/pool/job sobre PostgreSQL real.

| Gate | Evidencia mínima | Go | No-go | Owner accountable |
|---|---|---|---|---|
| R0 — arquitectura y discovery | Fronteras, activos, proveedores candidatos, clases de datos, abuso, owner, prueba futura y gaps explícitos | Registro válido y ningún crítico sin owner | Supuesto presentado como control, proveedor aprobado o dictamen legal | AppSec/Privacy + Architecture |
| R1 — tenant y fundación | IdP real, authz por recurso, `FORCE RLS`, auditoría, ETag/idempotencia, pipeline y restore base | Suites negativas y recovery reproducidos en entorno aislado | BOLA, ATO, secreto expuesto, restore incompleto o dependencia vulnerable alta/crítica | Identity/AppSec + Data/SRE |
| R2 — GIS, clima, documentos y operación | Parsers/geométricas limitados, canales/proveedores, archivos fail-closed, exactly-once y degradación | Integración real + abuso + observabilidad sin payload sensible | Entrada over-budget, dato stale presentado como vigente, archivo no limpio disponible o doble efecto | GIS/Weather/Documents/Operations |
| R3 — agricultura y escala | Perfiles autorizados, importaciones, límites de carga y trazabilidad completa | Pruebas de aislamiento, performance y reglas firmadas | Regla profesional no aprobada o escala fuera del envelope sin contingencia | Product/Domain + QA/AppSec |
| R4 — ganadería y rotación | Historia temporal, agua/seguridad, abstención y autorización de cada transición | Evals determinísticas y casos de bloqueo profesional aprobados | Ingreso habilitado sin agua/seguridad o recomendación exacta sin evidencia | Livestock/Grazing + Vet/Agronomy |
| R5 — economía, privacidad y portabilidad | Cierre/reapertura, exportes, derechos, hold/purge/restore y paquete canónico | Controles conciliados y `VAL-CON`/`VAL-LEG` aplicables | Borrado rompe obligación/hold, exporte cruza tenant o semántica contable inventada | Finance/Privacy + Legal/Accountant |
| R6 — IA y piloto integral | Retrieval reautorizado, tools read-only, evals/red-team, kill switch, proveedor/DPA | Cero críticos, evidencia/confianza/faltantes y fallback sin LLM | Prompt injection con fuga/acción, cita falsa no detectada o incapacidad de apagar | AI/AppSec + Product/Domain |

### Gate R1 local por capacidad

El gate agregado no declara R1 completa: evita que una capacidad ya integrada siga tratándose como spike y exige evidencia ejecutable proporcional.

| Capacidad local | Evidencia GO local | Condición que sigue NO-GO |
|---|---|---|
| Identity/OIDC y sesiones | API/configuración fail-closed, cookie/CSRF/rate limit/no-store, reauth `max_age/auth_time`, sesión revocada y ausencia de endpoints sintéticos en Production | Auth0 real, edge/TLS, email, DPA/región, logout y provider-down en ambiente compartido |
| Linking y step-up | Binding usuario+sesión+identidad+purpose, TTL, one-shot, replay/concurrencia, ACR/AMR y rotación de sesión contra PostgreSQL real | Lifecycle passkey/TOTP/recovery, notificación y enforcement por roles |
| Discovery de membresías — evidencia R0 previa | PostgreSQL real demuestra principal exacto read-only, `NOINHERIT`/`NOBYPASSRLS`, sin ownership, actor server-side con `SET LOCAL`, policies actor-scoped, 0/1/N, revocación, límite y limpieza de pool; harness local con SCRAM-SHA-256 y ACL owner-only | No es parte del runtime ni autoriza deploy: faltan migración forward-safe R1, migrator separado, secretos administrados, grants por capacidad, roles/alcances definitivos y revalidación A/B/sin contexto/pool/jobs en la solución integrada |
| CreateOrganization tenant bootstrap | Actor y scope server-side, idempotencia HMAC multiversión, transacción negocio+ledger+journal+outbox, roles mínimos, `SET LOCAL`, `FORCE RLS`, A/B/sin contexto/pool/job, rollback/roll-forward y E2E 0/1/N | Credenciales/migrator en ambiente compartido, discovery general, otras mutaciones tenant, retención legal y delivery/consumer |
| Invitación y remoción de co-owner | Owner activo y step-up `manage_organization_owners` para crear/revocar invitaciones y remover a otro owner; token bearer 256-bit visible una vez, sólo digest HMAC versionado; aceptación y remoción actualizan membership/proyección/journal/outbox atómicos; último-owner serializado, errores neutrales, ETag, idempotencia, `FORCE RLS`, A/B/sin contexto/pool/job y carreras terminales | Envío/email, roles no-owner, self-remove/transfer/democión, purge/retención legal, secretos administrados y entorno compartido |
| Persistencia y contratos FND | Migraciones aditivas N/N-1, trigger local que bloquea UPDATE/DELETE del journal, outbox tipado, schema/mapa/fitness y modelo EF sin drift | WORM/principal separado central, dispatcher/inbox, poison/reconciliación operativa, backfill contract y backup/restore compartido |
| Web Identity | pnpm frozen, TypeScript estricto, estados accesibles, loader regional y E2E desktop/mobile | Matriz de navegador/dispositivo aprobada, CSP/edge real y contextos tenant |
| Referencia territorial local | GET search/resolve autenticados, contrato estricto, snapshot oficial 23 provincias+CABA hash/completo/inmutable, roles mínimos+`FORCE RLS`, rate limit/no-store, adapter Georef v2.0 fijo con 5 s/256 KiB/sin redirects, estados fresh/stale/unavailable, fallback manual y coordenadas sin persistencia/journal/browser/log de aplicación | Tráfico Georef real sigue `NO-GO` hasta licencia/atribución, Q-058/Q-060, región/DPA/retención/subencargados, SLA/cuota/capacidad, egress/DNS y `TST-TERRITORY-URL-REDACTION` end-to-end; el caché volátil local no equivale a retención aprobada |
| Productive Core — campos no espaciales | Contrato exacto create/list/detail/rename; actor/tenant server-derived; sesión vigente y owner activo revalidados por puerto Identity `SECURITY DEFINER` antes de lookup/replay; antiforgery e idempotencia HMAC; rename con ETag/If-Match fuerte, 412 neutral y evento sin nombres; roles mínimos, `SET LOCAL`, `FORCE RLS`; unidad+ledger+aliases+journal append-only+outbox atómicos; A/B/sin contexto/pool/job/membership removida, races, N/N-1 y rollback/roll-forward en PostgreSQL real | `GO` solo integrado-local para draft no espacial y rename del nombre. Geometría/área/tiles/history, roles no-owner, otras ediciones, delivery/inbox/worker, DB/backup/secretos administrados, shared hosting/edge/collector y producción siguen `NO-GO` |
| Supply chain local | SDK/tools/dependencias fijados, restore locked/frozen, SCA y secrets scan | CI protegida, workload identity, SBOM, provenance, firma y registro |

Para registrar `GO local`, el validador SEC, las pruebas de abuso afectadas y los gates build/format/SCA deben pasar desde el estado combinado. Un test omitido, un endpoint runtime sin amenaza/control, una declaración que degrade runtime a “futuro” o la atribución de un control R0 descartable al runtime falla el incremento.

## Revisión de una nueva frontera

1. Registrar origen, destino, dirección, protocolo y ambiente.
2. Clasificar cada dato y determinar tenant/resource scope; minimizar antes de transferir.
3. Identificar identidad, credencial, authn, authz, cifrado, validación y límites.
4. Asociar amenaza estable `TM-*`, control existente con evidencia y gap; no confundir diseño con runtime.
5. Definir prueba positiva, negativa, abuso, degradación y señal sin payload sensible.
6. Asignar owner y riesgo residual. Si es crítico sin owner, el gate falla.
7. Registrar rollout, kill/revocación, recovery y fecha de revisión.

## Revisión de un proveedor

1. Registrar servicio, finalidad, datos mínimos, dirección del flujo, credenciales y egress.
2. Confirmar plan/licencia/SLA/cuota, región, DPA, subencargados, retención, entrenamiento y salida; `desconocido` es NO-GO productivo (Q-058/Q-060).
3. Validar schema, tamaño, timeouts, retries selectivos, circuit breaker, idempotencia y modo degradado.
4. Probar breach/replay/schema drift/429/5xx y revocación; documentar evidencia y owner.
5. Obtener `VAL-LEG` cuando haya datos personales, ubicación, transferencia o proveedor internacional.

## Trazabilidad

- Registro ejecutable: `threat-register.json`.
- Modelo humano: `AgropecuarIA-threat-model.md`.
- Privacidad: `data-classification-and-privacy.md`.
- Procesamiento externo: `provider-processing-inventory.md`.
- Requisitos: `docs/07-seguridad-y-privacidad.md`, `tasks/test-strategy.md` y `tasks/traceability-matrix.md`.
