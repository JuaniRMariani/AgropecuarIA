# Validación del protocolo AGRO-FND-002

Fecha: 2026-08-10
Resultado del incremento: **PASS contractual R1**
Estado de la tarea padre: **En curso**

## Resultado demostrado

La Definition of Ready del sub-slice contractual quedó demostrada: outcome, invariantes, riesgos, ownership, primer consumidor y comandos de verificación son explícitos. El backlog transitó `Propuesto → Ready → En curso` por la selección del sponsor y la evidencia de este gate.

El resultado no implementa runtime ni satisface la Definition of Done de `AGRO-FND-002`. `runtimeImplemented=false` permanece vinculante. El primer consumidor futuro es `AGRO-ID-003/CreateOrganization`, un bootstrap platform con namespace constante del servidor; la organización creada habilitará la frontera tenant y sus pruebas posteriores. No se inició ni absorbió `AGRO-ID-003`.

## Decisiones validadas

- Scope discriminado `platform | tenant`, sin tenant sintético, lookup global u oracle cross-tenant.
- Identidad tenant `(tenant, operation, key)` y bootstrap `(platform namespace, operation, key)`; actor y bindings se comparan sin ampliar unicidad.
- Key opaca con identidad estable de ledger y aliases `HMAC-SHA-256` multiversión; intersección N/N-1, lazy alias y split de identidad fallan a conciliación sin efecto.
- Autorización antes de lookup y replay; resultados históricos solo se reproducen de forma allow-listed o se reconstruyen bajo autorización vigente.
- Fencing monotónico, lock y CAS de owner/fence antes del negocio; owner stale produce cero efecto.
- Negocio, ledger terminal, journal local y outbox en una transacción PostgreSQL; commit incierto requiere verificación en conexión nueva, no retry ciego.
- Delivery at-least-once, inbox por consumidor/evento, orden solo por agregado, retry acotado y poison separado del estado terminal del ledger.
- Sin auto-purge local ni plazo legal inventado; `Q-060`, `GAP-003` y `VAL-LEG` siguen abiertos.

## Gates ejecutados

| Gate | Resultado exacto |
|---|---|
| `powershell -NoProfile -ExecutionPolicy Bypass -File .\tasks\evidence\AGRO-FND-002\validate-foundation-protocol.ps1 -SelfTest` | PASS; `44/44 mutations rejected`; `VALIDATION PASS` protocolo `1.0.0`, scope `tenant|platform`, cuatro estados persistidos, `runtimeImplemented=false`, consumidor futuro y padre `En curso`. |
| Parse de `foundation-protocol.json` | PASS. |
| Parser AST de `validate-foundation-protocol.ps1` | PASS, cero errores. |
| UTF-8 estricto, sin BOM, LF, newline final y trailing whitespace | PASS para los artefactos del incremento. |
| Referencias y tokens normativos policy/audit/backlog/ADR | PASS mediante el validador. |
| Secret assignment y datos sensibles | PASS, cero valores concretos detectados. |
| `git diff --check` | PASS; repetir sobre staged antes del commit por tratarse de archivos nuevos. |

.NET, frontend, EF Core, PostgreSQL runtime, E2E y SCA son `N/A` para este diff exclusivamente documental. No se declaran verdes por resultados históricos y pasan a ser obligatorios al implementar el primer consumidor.

## Revisión independiente

Architecture, Security/Data y Principal QA aprobaron el estado combinado con cero hallazgos críticos, altos o medios. Durante la revisión se corrigieron:

1. el falso supuesto de que `CreateOrganization` ya tenía tenant;
2. el conflicto/oracle global entre namespaces tenant;
3. la falta de fencing ante un lease recuperado;
4. el bypass de unicidad durante rotación HMAC;
5. la mezcla entre poison de delivery y `failed_terminal` del ledger;
6. el replay de body histórico sin reautorización.

## Compatibilidad, rollback y riesgos residuales

No hay migración en este incremento. El contrato futuro exige `expand`, coexistencia N/N-1, rollback de aplicación que preserve evidencia y roll-forward; no autoriza un `Down` destructivo compartido.

Riesgos residuales no bloqueantes para este gate, pero bloqueantes para la DoD:

- no existen ledger, aliases, dispatcher, inbox, poison store ni conciliación productivos;
- `ADR-PEND-007` aún no está implementada en runtime mediante roles, migrator, `FORCE RLS` y contexto transaction-local;
- faltan PostgreSQL real A/B/sin contexto/pool/job, concurrencia/fencing, commit unknown, N/N-1 y delivery efecto/ack;
- faltan volumen Q-020, retención/legal hold Q-060/VAL-LEG y operación de Audit/Compliance central;
- roles y reglas de `CreateOrganization` pertenecen a `AGRO-ID-003` y no se inventaron aquí.

La siguiente tarea recomendada es `AGRO-ID-003`, mediante una instrucción independiente. No fue iniciada en este incremento.
