# ADR-007 — Storage privado, cuarentena y recuperación integral

- **Estado:** aceptado para contrato de discovery; pendiente proveedor/Legal y sandbox cloud.
- **Fecha:** 2026-08-05.
- **Drivers:** RF-DOC-001/002, RN-CORE-008/009, RNF-REL-002/003, RNF-SEC-001/003, RNF-PRI-001 y RSK-016/020.

## Contexto

Los archivos deben permanecer privados, tenant-safe, trazables y recuperables junto con PostgreSQL/PostGIS y auditoría. Una transacción distribuida DB↔storage no es viable; antivirus, URLs firmadas, retención y restore agregan estados y fallos propios. La política legal, región, proveedor y volumen no están aprobados.

## Decisión

- Mantener un port de storage y otro de scanner; ninguna regla de dominio depende del SDK cloud.
- DB es fuente del ciclo de vida. La carga entra a una clave única generada por servidor bajo cuarentena tenant-partitioned.
- Validar tamaño, SHA-256, MIME declarado y magic bytes. Solo un resultado `clean` para la misma versión/hash publica; todo resultado ambiguo falla cerrado.
- Descargar solo después de reautorizar tenant, recurso y acción, mediante grant breve ligado a versión. No deduplicar entre tenants.
- Conservar versiones y auditoría append-only. Legal hold es ortogonal y prevalece sobre purga; plazos quedan fuera de esta ADR hasta `VAL-LEG`.
- Serializar hold, descargas y purga por versión. Un timeout/cancelación de delete es resultado ambiguo: permanecer fail-closed como `PurgeUncertain` hasta que un reconciliador privilegiado compruebe la existencia del objeto.
- Recuperar con PITR/dump + versiones de objetos + manifest de cutoff/hashes/auditoría, siempre primero en entorno aislado y con reconciliación.
- Usar AWS S3 + GuardDuty como candidato técnico preferido, Azure como alternativa y S3-compatible solo tras demostrar equivalencia. Ninguno está autorizado para producción por esta ADR.

## Consecuencias

Se acepta consistencia eventual explícita mediante estados/outbox/reconciliación en tareas R1; no se simula atomicidad. El proveedor debe soportar KMS/CMK, inmutabilidad/hold, versionado, eventos idempotentes, inventario y restore. Backups requieren principal/cuenta independiente. Cloudflare R2 no es default mientras su compatibilidad no cubra Object Lock/legal hold/AV requeridos.

## Evidencia y gates pendientes

`tasks/evidence/AGRO-DIS-005` contiene schemas, spike, threat model, matriz y restore real local. Antes de R1/producción faltan sandbox storage+AV real, región/DPA/subencargados, política de retención, Q-020/060, KMS/backup separado, dataset representativo y aprobación Privacy/Legal/SRE/AppSec.
