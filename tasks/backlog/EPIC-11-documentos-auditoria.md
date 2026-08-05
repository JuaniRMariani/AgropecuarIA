# EPIC-11 — Documentos y auditoría

Objetivo: archivos privados, línea de tiempo, rectificación, portabilidad y privacidad verificables entre R1 y R5.

<a id="agro-doc-001"></a>

## AGRO-DOC-001 — Adjuntar y descargar archivos seguros y versionados

- **Release, épica, prioridad y tamaño:** R1/R2 · EPIC-11 · Must · L.
- **Owner y colaboradores:** Documentos; AppSec, Plataforma, Frontend y QA.
- **Resultado/valor esperado:** evidencia privada con hash/versión/autor y ciclo de cuarentena.
- **Historia/JTBD:** Como usuario, quiero adjuntar foto/PDF y recuperarlo solo con permiso.
- **Alcance incluido:** intención de carga, carga, MIME, hash, tamaño, antivirus, estados pendiente/disponible/cuarentena/rechazado, vínculo de negocio, URL breve y versión.
- **Fuera de alcance:** binarios en la base de datos, obtención desde URL arbitraria y deduplicación entre tenants.
- **Requisitos trazados:** RF-DOC-001; RF-FIN-010; RN-CORE-008; RNF-SEC-001/002.
- **Precondiciones y dependencias:** DIS-005, ID-003 y FND-002.
- **Contrato/API/eventos afectados:** iniciar, completar, escanear, descargar y versionar; `DocumentImported`.
- **Datos, índices, migración y compatibilidad:** `FileObject` con tenant, hash, versión, clasificación, estado, retención y vínculo.
- **Autenticación, autorización, tenant y auditoría:** el módulo propietario reautoriza el vínculo y la descarga; URL breve y acceso auditado.
- **Frontend:** progreso, cuarentena, error y vencimiento; responsivo/accesible; sin UUID completo.
- **Reglas e invariantes:** un fallo del antivirus impide publicar el archivo; se valida el MIME real y se conserva la versión original.
- **Criterios de aceptación:** Dado un archivo hostil o de otro tenant, cuando se procesa/descarga, entonces se rechaza o no revela su existencia; un archivo válido queda versionado y verificable por hash.
- **Casos negativos y bordes:** MIME falso, malware de prueba, carga interrumpida, duplicado/colisión de hash y URL vencida.
- **Estrategia de pruebas:** contratos de almacenamiento/antivirus, BOLA, límites de carga, E2E y restauración.
- **Observabilidad:** bytes, estado, latencia/fallos del escaneo y objetos huérfanos, sin registrar contenido.
- **Seguridad y privacidad:** almacenamiento privado cifrado, nombres generados, allow-list y retención.
- **Performance/capacidad y límites:** tamaño, cuota y escaneo asíncrono según Q-020.
- **Feature flag, rollout, migración, rollback y recuperación:** flag del adaptador de almacenamiento; deshabilitar descarga/escaneo conserva objetos y metadatos.
- **Documentación:** política de archivos, límites y runbooks.
- **Comandos/evidencia esperados:** futuras pruebas de integración y seguridad.
- **Definition of Ready:** proveedor, antivirus, tipos y retención definidos.
- **Definition of Done:** ciclo de vida seguro y restauración demostrados.
- **Bloqueos/preguntas:** Q-058/060 y política de retención legal.
- **Paralelizable:** sí con DOC-002.

<a id="agro-doc-002"></a>

## AGRO-DOC-002 — Exponer timeline y auditoría de recurso

- **Release, épica, prioridad y tamaño:** R1/R2 · EPIC-11 · Must · L.
- **Owner y colaboradores:** Auditoría/Documentos; todos los equipos de dominio, AppSec, Frontend y QA.
- **Resultado/valor esperado:** hechos/rectificaciones/acciones sensibles reconstruibles y exportables.
- **Historia/JTBD:** Como auditor, quiero saber quién creó, aprobó o corrigió cada dato.
- **Alcance incluido:** línea de tiempo de negocio; auditoría de seguridad append-only; actor, tenant, recurso, acción, fechas efectiva/de registro, origen, correlación y valores anterior/posterior permitidos.
- **Fuera de alcance:** logs técnicos como auditoría y secretos/payload completos.
- **Requisitos trazados:** RF-DOC-002; RN-CORE-002–004/009; RNF-SEC-003; RNF-OBS-001.
- **Precondiciones y dependencias:** FND-002/003 y política de retención.
- **Contrato/API/eventos afectados:** consultar/exportar línea de tiempo y auditoría.
- **Datos, índices, migración y compatibilidad:** eventos append-only por tenant, recurso y fecha; payload redactado y versionado.
- **Autenticación, autorización, tenant y auditoría:** permiso de auditoría separado; las mutaciones sensibles siguen la política de fallo definida.
- **Frontend:** línea de tiempo, diferencias, fuente, rectificación y estados vacío/carga/error.
- **Reglas e invariantes:** un hecho confirmado se rectifica o revierte; la auditoría nunca almacena tokens, OTP ni secretos.
- **Criterios de aceptación:** Dada una rectificación, cuando un usuario autorizado abre la línea de tiempo, entonces ve original, vínculo, motivo, actor y fechas.
- **Casos negativos y bordes:** actor eliminado, acceso de soporte, importación masiva, desajuste de reloj y auditoría no disponible.
- **Estrategia de pruebas:** integración, append-only, autorización, exportación, redacción y E2E.
- **Observabilidad:** salud/atraso del pipeline de auditoría y accesos denegados.
- **Seguridad y privacidad:** evidencia de manipulación, acceso restringido, retención y retención legal.
- **Performance/capacidad y límites:** paginación, índices y retención según Q-060.
- **Feature flag, rollout, migración, rollback y recuperación:** auditoría obligatoria antes de habilitar funciones sensibles; restauración verificada.
- **Documentación:** catálogo de eventos de auditoría, retención y acceso.
- **Comandos/evidencia esperados:** futuras pruebas de auditoría e informe de inspección.
- **Definition of Ready:** catálogo de eventos, política de fallo y retención definidos.
- **Definition of Done:** eventos críticos completos, redactados y exportables.
- **Bloqueos/preguntas:** política fail-closed y Q-060.
- **Paralelizable:** integración por módulo después de estabilizar el contrato común.

<a id="agro-doc-003"></a>

## AGRO-DOC-003 — Exportar, rectificar y suprimir según política

- **Release, épica, prioridad y tamaño:** R5 · EPIC-11 · Must · L.
- **Owner y colaboradores:** Privacy/Portability; Documents, Data, AppSec, Legal y QA.
- **Resultado/valor esperado:** derechos y portabilidad sin romper retención legal ni auditoría.
- **Historia/JTBD:** Como organización/titular, quiero exportar o solicitar rectificación/supresión controlada.
- **Alcance incluido:** exportación organizacional de datos y originales con manifiesto/hashes; flujos de acceso, rectificación y supresión; eliminación lógica, purga, retención legal y política de backups.
- **Fuera de alcance:** borrar obligaciones/auditoría indiscriminadamente.
- **Requisitos trazados:** RF-ADM-003; RNF-PRI-001; RNF-PORT-001; Q-058/060.
- **Precondiciones y dependencias:** DOC-001/002, SEC-004 y política validada legalmente.
- **Contrato/API/eventos afectados:** solicitud, estado, exportación, descarga y purga.
- **Datos, índices, migración y compatibilidad:** snapshot consistente, esquema, manifiesto, retención legal y programación de purga.
- **Autenticación, autorización, tenant y auditoría:** owner con autenticación reforzada; exportación segregada y todo acceso/acción auditado.
- **Frontend:** solicitud, progreso, errores, vencimiento y explicación accesible de la retención legal.
- **Reglas e invariantes:** no cruza tenants; la retención legal prevalece sobre la purga; el tratamiento de backups es transparente.
- **Criterios de aceptación:** Dada una solicitud autorizada, cuando finaliza, entonces el paquete es verificable y la supresión respeta retención/legal hold sin romper referencias.
- **Casos negativos y bordes:** escrituras concurrentes, organización grande, URL vencida, profesional compartido y restauración de backup.
- **Estrategia de pruebas:** consistencia, BOLA, rendimiento, retención legal/purga y restauración.
- **Observabilidad:** solicitudes, antigüedad, tamaño, fallos y backlog de purga.
- **Seguridad y privacidad:** cifrado, minimización, región, transferencias y respuesta a incidentes.
- **Performance/capacidad y límites:** procesamiento asíncrono, cancelación/reanudación y cuotas.
- **Feature flag, rollout, migración, rollback y recuperación:** piloto del flujo de privacidad; simulación de purga; política de recuperación documentada.
- **Documentación:** aviso de privacidad, inventario de datos, retención y subencargados.
- **Comandos/evidencia esperados:** futuras pruebas de exportación/purga/restauración y aprobación legal.
- **Definition of Ready:** política legal, owner e inventario de datos definidos.
- **Definition of Done:** derechos operables y auditados.
- **Bloqueos/preguntas:** Q-058/060 y revisión legal.
- **Paralelizable:** sí, con FIN-005 después de estabilizar los esquemas.
