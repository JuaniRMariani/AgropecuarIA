# Matriz de proveedor — storage y antimalware

Fecha de evaluación: 2026-08-05. Alcance: decisión técnica de discovery; no es una compra, DPA ni autorización de transferencia internacional.

## Criterios obligatorios

1. objetos privados, claves generadas por servidor y grants breves;
2. versionado, checksum y protección WORM/legal hold;
3. cifrado con KMS/CMK y mínimo privilegio;
4. resultado antimalware asíncrono, idempotente y fail-closed;
5. inventario, backup/restore y reconciliación verificables;
6. región, DPA, subencargados, residencia, soporte y salida;
7. costo medible de capacidad, requests, scan, KMS, backup y egress.

## Resultado

| Opción | Ajuste técnico | Fortalezas demostradas en documentación primaria | Límites/gates | Decisión R0 |
|---|---|---|---|---|
| AWS S3 + SSE-KMS/Object Lock + GuardDuty Malware Protection; PostgreSQL administrado con PITR | Alto | S3 soporta grants prefirmados, versionado, WORM/hold y SSE-KMS; GuardDuty publica resultados, tags y métricas, soporta Object Lock/SSE-KMS y entrega eventos al menos una vez; RDS restaura a una instancia nueva. | Una URL puede reutilizarse hasta vencer y una clave repetida puede sobrescribirse: la aplicación debe emitir clave única/versionada. `UNSUPPORTED`, `ACCESS_DENIED` y `FAILED` son fail-closed. Falta validar región/DPA/plan/cuotas/costo y drill real. | **Candidato preferido condicionado** para sandbox. No-go productivo hasta gates externos. |
| Azure Blob + CMK/immutability + Defender for Storage | Alto, con adapter no S3 | SAS acota acceso; immutable Blob ofrece WORM y legal hold; Defender escanea on-upload/on-demand y escribe resultado; versioning/soft delete/backup tienen cobertura gestionada. | Immutability y point-in-time restore poseen incompatibilidades; PITR bloquea operaciones durante restore y no restaura contenedores eliminados. Requiere diseñar versiones/backup vaulted, validar región/DPA/costo y aceptar un adapter distinto. | **Alternativa válida**; exige spike cloud específico de combinación WORM/restore. |
| S3-compatible regional/portable + scanner separado | Variable | Reduce acoplamiento de SDK y puede acercar residencia/egress; el contrato básico del spike es portable. | “S3-compatible” no demuestra Object Lock, KMS, eventos, checksum completo, AV, backup ni SLA. Cada feature debe probarse; el scanner propio agrega parchado, aislamiento, escalado y operación. | **Fallback**, solo si supera la misma matriz y `VAL-LEG`. |
| Cloudflare R2 como default | Bajo para este caso | API S3-compatible y lifecycle disponibles; egress puede ser favorable. | La tabla de compatibilidad S3 no implementa Object Lock/legal-hold ni varias operaciones de versioning/tagging requeridas por este diseño. No hay AV gestionado equivalente en el alcance evaluado. | **No-go como default** mientras falte equivalencia demostrada. |

## Fuentes primarias

- AWS S3, URLs prefirmadas: <https://docs.aws.amazon.com/AmazonS3/latest/userguide/using-presigned-url.html>
- AWS S3 Object Lock: <https://docs.aws.amazon.com/AmazonS3/latest/userguide/object-lock.html>
- AWS S3 con KMS: <https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingKMSEncryption.html>
- GuardDuty Malware Protection para S3: <https://docs.aws.amazon.com/guardduty/latest/ug/how-malware-protection-for-s3-gdu-works.html>
- Estados de scan GuardDuty: <https://docs.aws.amazon.com/guardduty/latest/ug/monitoring-malware-protection-s3-scans-gdu.html>
- Compatibilidad de GuardDuty con S3: <https://docs.aws.amazon.com/guardduty/latest/ug/supported-s3-features-malware-protection-s3.html>
- RDS point-in-time restore: <https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_PIT.html>
- Azure SAS: <https://learn.microsoft.com/azure/storage/common/storage-sas-overview>
- Azure Defender for Storage: <https://learn.microsoft.com/azure/defender-for-cloud/introduction-malware-scanning>
- Azure immutable Blob/WORM: <https://learn.microsoft.com/azure/storage/blobs/immutable-storage-overview>
- Azure Blob PITR: <https://learn.microsoft.com/azure/storage/blobs/point-in-time-restore-overview>
- Cloudflare R2 S3 compatibility: <https://developers.cloudflare.com/r2/api/s3/api/>

## Costos y capacidad

Sin Q-020/volumen no existe un total responsable. El próximo sandbox debe medir por tenant: bytes originales/versiones/backups, uploads, GET/HEAD, tags/eventos, GB escaneados, KMS, requests de inventario, egress y duración del restore. Se rechaza cualquier opción que solo cotice almacenamiento y omita scan, versiones, backup, requests o salida.
