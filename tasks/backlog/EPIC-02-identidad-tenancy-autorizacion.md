# EPIC-02 — Identidad, tenancy y autorización

Objetivo: acceso moderno, recuperación segura y colaboración por organización/campo sin fugas. R1; delegación temporal en R7.

<a id="agro-id-001"></a>

## AGRO-ID-001 — Registrar y vincular identidades sin duplicar usuarios

- **Release, épica, prioridad y tamaño:** R1 · EPIC-02 · Must · M.
- **Owner y colaboradores:** Identity; Frontend, AppSec, QA y SRE.
- **Resultado/valor esperado:** email verificado y Google OIDC convergen en una identidad segura.
- **Historia/JTBD:** Como usuario, quiero entrar por email o Google sin crear cuentas duplicadas.
- **Alcance incluido:** registro/login, verificación, OIDC PKCE, linking con reautenticación, anti-enumeración y sesión cookie.
- **Fuera de alcance:** SMS, password propio si IdP no lo exige y soporte JIT.
- **Requisitos trazados:** RF-ID-001/002/006; ADR-003; RNF-SEC-001/002; Q-054/055.
- **Precondiciones y dependencias:** AGRO-DIS-003 y FND-001.
- **Contrato/API/eventos afectados:** login callback, link/unlink, session/revocation y `IdentityLinked`.
- **Datos, índices, migración y compatibilidad:** User platform-scoped e identidades externas únicas; migración aditiva.
- **Autenticación, autorización, tenant y auditoría:** sesión HttpOnly/Secure/SameSite; linking exige ambas identidades; eventos sensibles auditados.
- **Frontend:** login/link responsive, teclado/foco, loading/error/conflicto y no enumeración.
- **Reglas e invariantes:** email coincidente no alcanza; sesión revocada no se reutiliza.
- **Criterios de aceptación:** Dadas dos credenciales verificadas, cuando se vinculan, entonces existe un User y ambas rutas conservan membresías.
- **Casos negativos y bordes:** email no verificado, identidad ya vinculada, callback replay, IdP caído y sesión robada.
- **Estrategia de pruebas:** contrato OIDC, E2E, CSRF, linking/replay, revocación y accesibilidad.
- **Observabilidad:** éxito/fallo/latencia sin tokens/PII; alertas de anomalía.
- **Seguridad y privacidad:** mínimos claims, PKCE, rate limit y cookie segura.
- **Performance/capacidad y límites:** límites por IP/cuenta calibrados y disponibilidad IdP monitorizada.
- **Feature flag, rollout, migración, rollback y recuperación:** Google/email flags; fallback de login y revocación masiva.
- **Documentación:** onboarding, privacidad y runbook IdP.
- **Comandos/evidencia esperados:** tests auth/contrato configurados por la futura solución.
- **Definition of Ready:** mecanismo email e IdP elegidos.
- **Definition of Done:** E2E y amenaza linking/recovery sin críticas.
- **Bloqueos/preguntas:** mecanismo exacto email; Q-054/055.
- **Paralelizable:** sí, con ID-002/003 sobre contratos acordados.

<a id="agro-id-002"></a>

## AGRO-ID-002 — Activar passkeys, TOTP y recuperación resistente

- **Release, épica, prioridad y tamaño:** R1 · EPIC-02 · Must · M.
- **Owner y colaboradores:** Identity/AppSec; Frontend, QA y Support.
- **Resultado/valor esperado:** autenticación fuerte y recuperación sin dependencia de preguntas inseguras.
- **Historia/JTBD:** Como owner, quiero passkey/MFA y códigos para proteger y recuperar mi cuenta.
- **Alcance incluido:** registro/uso/revocación passkey, TOTP, recovery codes un solo uso, step-up y notificación.
- **Fuera de alcance:** SMS y passkey como sinónimo de TOTP.
- **Requisitos trazados:** RF-ID-003/004/006; ADR-003; RNF-SEC-002; Q-060.
- **Precondiciones y dependencias:** ID-001 y política MFA/roles.
- **Contrato/API/eventos afectados:** authenticators, challenges, recovery y step-up.
- **Datos, índices, migración y compatibilidad:** credenciales públicas/metadatos y secretos TOTP/recovery protegidos; no logs.
- **Autenticación, autorización, tenant y auditoría:** reautenticación para cambios; recovery invalida código y puede revocar sesiones.
- **Frontend:** compatibilidad/navegadores, fallback claro, foco/lector y códigos mostrados una vez.
- **Reglas e invariantes:** códigos no reutilizables; owner/admin/contador requieren MFA/passkey según política.
- **Criterios de aceptación:** Dado un factor perdido, cuando usa recovery válido, entonces recupera con auditoría/notificación y el código no vuelve a funcionar.
- **Casos negativos y bordes:** reloj TOTP, dispositivo duplicado, challenge replay, todos factores perdidos y OTP bombing.
- **Estrategia de pruebas:** WebAuthn/TOTP/recovery E2E, replay/rate limit y navegadores objetivo.
- **Observabilidad:** altas/revocaciones/fallos y fraude sin secretos.
- **Seguridad y privacidad:** cifrado, hashing apropiado, anti-enumeración y step-up.
- **Performance/capacidad y límites:** challenge TTL y rate limits medidos.
- **Feature flag, rollout, migración, rollback y recuperación:** flags por factor; revocación/rollback sin bloquear email recovery aprobado.
- **Documentación:** guía usuario/support y runbook pérdida de factor.
- **Comandos/evidencia esperados:** suites de autenticador de la futura implementación.
- **Definition of Ready:** política MFA/recovery aprobada.
- **Definition of Done:** casos positivos/abuso y accesibilidad aprobados.
- **Bloqueos/preguntas:** obligatoriedad por rol y canal de notificación.
- **Paralelizable:** sí, con ID-003.

<a id="agro-id-003"></a>

## AGRO-ID-003 — Crear organización, invitar y asignar alcance por campo

- **Release, épica, prioridad y tamaño:** R1 · EPIC-02 · Must · L.
- **Owner y colaboradores:** Identity/Tenancy; Product, Frontend, AppSec, GIS y QA.
- **Resultado/valor esperado:** colaboración multirol con autorización por recurso y separación carga/aprobación.
- **Historia/JTBD:** Como owner, quiero invitar personas y limitar qué campos/módulos/acciones pueden usar.
- **Alcance incluido:** organización, membresía, invitación/caducidad, roles, alcance campo/módulo/acción/estado y cambio de contexto.
- **Fuera de alcance:** permisos del cliente como control, soporte JIT y delegación temporal R7.
- **Requisitos trazados:** RF-ID-005; RN-CORE-001/009; RNF-SEC-002; Q-054/055.
- **Precondiciones y dependencias:** ID-001, FND-002 y matriz de roles.
- **Contrato/API/eventos afectados:** organization/membership/invitation/effective permissions.
- **Datos, índices, migración y compatibilidad:** unicidad tenant+membresía/rol/alcance; RLS y relaciones compuestas.
- **Autenticación, autorización, tenant y auditoría:** deny-by-default; cambio privilegiado con step-up y auditoría.
- **Frontend:** selector org/campo, invitaciones/roles responsive y estados vacío/caducado/conflicto.
- **Reglas e invariantes:** cliente no envía tenant autoritativo; recurso fuera de alcance no revela existencia.
- **Criterios de aceptación:** Dadas org A/B, cuando usuario de A solicita recurso B, entonces no recibe dato/existencia y se registra según política.
- **Casos negativos y bordes:** invitación reenviada/caducada, último owner, alcance retirado en sesión y multi-CUIT pendiente.
- **Estrategia de pruebas:** matriz roles/recursos, BOLA/RLS, cache/jobs, concurrencia y E2E.
- **Observabilidad:** cambios/denegaciones con tenant seudonimizado; alertas privilegiadas.
- **Seguridad y privacidad:** mínimo privilegio y separación de funciones.
- **Performance/capacidad y límites:** permisos cacheables con invalidación segura; volúmenes Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** roles iniciales versionados; rollback revoca permisos inmediatamente.
- **Documentación:** matriz de acceso y onboarding admin.
- **Comandos/evidencia esperados:** suite tenant negativa y tests E2E del repositorio futuro.
- **Definition of Ready:** roles/alcances/estados y owner policy definidos.
- **Definition of Done:** dos tenants operan sin fuga por todas las vías.
- **Bloqueos/preguntas:** Q-054/055 y relación organización-CUIT.
- **Paralelizable:** sí, con catálogo/archivos tras contrato tenant.

<a id="agro-id-004"></a>

## AGRO-ID-004 — Revocar sesiones y proteger acciones sensibles

- **Release, épica, prioridad y tamaño:** R1 · EPIC-02 · Must · M.
- **Owner y colaboradores:** Identidad/AppSec; Frontend, Auditoría, Soporte y QA.
- **Resultado/valor esperado:** contener una toma de cuenta y confirmar de nuevo la identidad antes de acciones sensibles.
- **Historia/JTBD:** Como usuario o administrador, quiero ver y revocar sesiones y recibir aviso de cambios sensibles.
- **Alcance incluido:** lista/revocación de dispositivos, cierre global de sesión, notificación y autenticación reforzada para exportes/cambios sensibles.
- **Fuera de alcance:** soporte delegado JIT, acceso permanente de soporte, acceso sin consentimiento y certificados ARCA.
- **Requisitos trazados:** RF-ID-006; RN-CORE-009; RNF-SEC-003; Q-060.
- **Precondiciones y dependencias:** ID-001/002/003 y contrato de auditoría.
- **Contrato/API/eventos afectados:** sesiones, dispositivos, revocación y notificación de cambio sensible.
- **Datos, índices, migración y compatibilidad:** sesiones/familias con expiración, motivo de revocación e índices por actor/tenant/vencimiento.
- **Autenticación, autorización, tenant y auditoría:** autenticación reforzada y eventos append-only; revocación revalidada en API, caché y trabajos.
- **Frontend:** sesiones/dispositivos/alertas, estados caducado/error y confirmación accesible.
- **Reglas e invariantes:** una sesión revocada no vuelve a operar; la UI no expone huellas excesivas del dispositivo.
- **Criterios de aceptación:** Dada una sesión revocada, cuando intenta operar, entonces falla sin ventana indebida, las demás sesiones válidas conservan su estado según la elección y el usuario recibe aviso.
- **Casos negativos y bordes:** revocación concurrente, dispositivo desconocido, último factor, caché atrasada y cierre global repetido.
- **Estrategia de pruebas:** unitarias de familia de sesión, integración API/caché/trabajos, E2E de autenticación reforzada, auditoría y reloj/vencimiento.
- **Observabilidad:** revocaciones, propagación, uso anómalo y runbook de toma de cuenta.
- **Seguridad y privacidad:** fingerprint mínimo, anti-enumeración, protección CSRF y sesiones/avisos sin secretos.
- **Performance/capacidad y límites:** propagación de revocación con SLO definido en AGRO-DIS-007.
- **Feature flag, rollout, migración, rollback y recuperación:** rollout por tenant; revocación global como contingencia; rollback no reactiva sesiones revocadas.
- **Documentación:** guía de sesiones, acciones sensibles e incidentes.
- **Comandos/evidencia esperados:** futuras pruebas del repositorio de revocación, endpoints, caché, trabajos y E2E.
- **Definition of Ready:** política de sesiones/notificación, acciones sensibles y SLO de propagación definidos.
- **Definition of Done:** revocación y autenticación reforzada demostradas en todas las vías, con auditoría y aviso accesible.
- **Bloqueos/preguntas:** Q-060 fija SLO/retención, pero no bloquea el comportamiento seguro base.
- **Paralelizable:** sí, después de ID-001/003; la verificación transversal coordina con SEC-002 y QA-002.

<a id="agro-id-005"></a>

## AGRO-ID-005 — Delegar soporte JIT con consentimiento y caducidad

- **Release, épica, prioridad y tamaño:** R7 · EPIC-02 · Should · M.
- **Owner y colaboradores:** Identidad/AppSec; Soporte, Legal/Privacidad, Frontend, Auditoría y QA.
- **Resultado/valor esperado:** resolver incidentes con acceso excepcional mínimo, consentido, temporal y completamente trazable.
- **Historia/JTBD:** Como administrador de organización, quiero conceder soporte acotado para recibir ayuda sin entregar credenciales ni acceso permanente.
- **Alcance incluido:** solicitud, motivo, consentimiento explícito, alcance por recurso/acción, autenticación reforzada, caducidad, revocación inmediata, sesión distinguible y aviso al titular.
- **Fuera de alcance:** impersonación silenciosa, soporte permanente, acceso global por defecto, credenciales compartidas y operaciones ARCA.
- **Requisitos trazados:** RF-ID-007; RN-CORE-001/009; RNF-SEC-002/003; Q-060.
- **Precondiciones y dependencias:** AGRO-ID-004, AGRO-SEC-002/004, contrato de auditoría y política de soporte aprobada.
- **Contrato/API/eventos afectados:** solicitud/concesión/revocación/consulta de grant JIT y eventos de acceso de soporte.
- **Datos, índices, migración y compatibilidad:** grant con tenant, solicitante, aprobador, motivo, recursos, acciones, inicio/vencimiento/revocación y correlación; migración aditiva.
- **Autenticación, autorización, tenant y auditoría:** autenticación reforzada de ambas partes; deny-by-default; cada operación reautoriza grant/recurso y queda auditada.
- **Frontend:** flujo accesible de solicitud/aprobación/revocación, banner persistente de sesión de soporte y estados de carga/vacío/error/caducado/conflicto; UUID corto.
- **Reglas e invariantes:** sin consentimiento o al vencer/revocar no existe acceso; el grant no amplía el rol base ni habilita exportes salvo permiso explícito.
- **Criterios de aceptación:** Dado un grant acotado, cuando soporte intenta un recurso/acción fuera del alcance o después del vencimiento, entonces recibe denegación neutral; dentro del alcance el titular puede observar y revocar la sesión en tiempo efectivo.
- **Casos negativos y bordes:** reloj desalineado, titular revocado, grant solapado, organización suspendida, consentimiento retirado y caché atrasada.
- **Estrategia de pruebas:** unitarias de estado/vencimiento, integración autorización/auditoría/caché, BOLA, E2E de consentimiento/revocación y simulacro de incidente.
- **Observabilidad:** grants solicitados/aprobados/rechazados/vencidos/revocados, accesos denegados y uso anómalo, sin datos sensibles.
- **Seguridad y privacidad:** mínimo privilegio, propósito y retención; aviso visible; protección contra abuso interno y revisión periódica.
- **Performance/capacidad y límites:** validación del grant dentro del presupuesto de autorización; duración y concurrencia máximas configuradas por política.
- **Feature flag, rollout, migración, rollback y recuperación:** flag `support-jit` apagado por defecto; piloto limitado; interruptor global y revocación masiva; rollback preserva evidencia.
- **Documentación:** política/RACI de soporte, consentimiento, matriz de acciones, playbook y runbook de abuso/revocación.
- **Comandos/evidencia esperados:** futuras suites del repositorio de autorización, BOLA, E2E, auditoría y expiración; acta de Privacidad/AppSec.
- **Definition of Ready específica:** política, duración, alcance, consentimiento, owners, SLO de revocación y retención aprobados.
- **Definition of Done específica:** grant JIT se concede, distingue, limita, vence y revoca con evidencia; AppSec/Privacidad/QA aprueban y el flag permanece controlable.
- **Bloqueos/preguntas abiertas:** Q-060 y decisión del sponsor sobre operación/soporte; si no se cierran, el flag permanece apagado sin afectar R1–R6.
- **Paralelizable:** sí con tareas R7, después de ID-004 y SEC-004; integración final con QA-002/003.
