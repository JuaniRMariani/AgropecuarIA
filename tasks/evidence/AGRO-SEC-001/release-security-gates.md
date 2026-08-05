# Gates de seguridad por release

Estado: baseline R0 de `AGRO-SEC-001`. Es un mecanismo de decisión, no una aprobación legal ni evidencia de controles que todavía no tienen runtime.

## Regla transversal

Una frontera o proveedor nuevo entra a la Definition of Ready únicamente cuando registra datos, protocolo, autenticación/autorización, validación, límites, owner, amenazas, controles, pruebas, señales operativas y riesgo residual. Una amenaza crítica abierta sin owner o una mitigación crítica sin prueba bloquea solo la capacidad afectada. Un hallazgo alto/crítico introducido bloquea su release.

Los gates conservan explícitas las preguntas abiertas: Q-054/Q-055 para modelo comercial, controlador, titularidad y delegación; Q-058 para proveedor/región de IA y clima; Q-060 para SLA, soporte y retención. Ningún default técnico R0 responde esas preguntas ni habilita producción.

| Gate | Evidencia mínima | Go | No-go | Owner accountable |
|---|---|---|---|---|
| R0 — arquitectura y discovery | Fronteras, activos, proveedores candidatos, clases de datos, abuso, owner, prueba futura y gaps explícitos | Registro válido y ningún crítico sin owner | Supuesto presentado como control, proveedor aprobado o dictamen legal | AppSec/Privacy + Architecture |
| R1 — tenant y fundación | IdP real, authz por recurso, `FORCE RLS`, auditoría, ETag/idempotencia, pipeline y restore base | Suites negativas y recovery reproducidos en entorno aislado | BOLA, ATO, secreto expuesto, restore incompleto o dependencia vulnerable alta/crítica | Identity/AppSec + Data/SRE |
| R2 — GIS, clima, documentos y operación | Parsers/geométricas limitados, canales/proveedores, archivos fail-closed, exactly-once y degradación | Integración real + abuso + observabilidad sin payload sensible | Entrada over-budget, dato stale presentado como vigente, archivo no limpio disponible o doble efecto | GIS/Weather/Documents/Operations |
| R3 — agricultura y escala | Perfiles autorizados, importaciones, límites de carga y trazabilidad completa | Pruebas de aislamiento, performance y reglas firmadas | Regla profesional no aprobada o escala fuera del envelope sin contingencia | Product/Domain + QA/AppSec |
| R4 — ganadería y rotación | Historia temporal, agua/seguridad, abstención y autorización de cada transición | Evals determinísticas y casos de bloqueo profesional aprobados | Ingreso habilitado sin agua/seguridad o recomendación exacta sin evidencia | Livestock/Grazing + Vet/Agronomy |
| R5 — economía, privacidad y portabilidad | Cierre/reapertura, exportes, derechos, hold/purge/restore y paquete canónico | Controles conciliados y `VAL-CON`/`VAL-LEG` aplicables | Borrado rompe obligación/hold, exporte cruza tenant o semántica contable inventada | Finance/Privacy + Legal/Accountant |
| R6 — IA y piloto integral | Retrieval reautorizado, tools read-only, evals/red-team, kill switch, proveedor/DPA | Cero críticos, evidencia/confianza/faltantes y fallback sin LLM | Prompt injection con fuga/acción, cita falsa no detectada o incapacidad de apagar | AI/AppSec + Product/Domain |

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
