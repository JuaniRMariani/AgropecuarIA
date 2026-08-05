# Threat model de archivos y recuperación

Estado: R0, 2026-08-05. Supuestos confirmados por delegación del sponsor: datos sintéticos, sin credenciales/cloud, `Organization` como tenant técnico, RPO 15 min/RTO 2 h hipotéticos y ninguna política legal inferida.

## Activos y fronteras

Activos: contenido documental, metadatos/hash/versiones, vínculos de negocio, geometrías, auditoría, manifests/backups, claves KMS y grants temporales.

```text
Navegador no confiable
  -> API/BFF autenticado y autorización por recurso
     -> DB PostgreSQL/PostGIS (estado y vínculos)
     -> cuarentena privada -> scanner -> objetos disponibles privados
     -> outbox/worker/reconciliador
SRE break-glass
  -> backup inmutable con credencial/cuenta separada
  -> restore aislado -> reconciliación -> promoción controlada
```

Los nombres, MIME, contenido, IDs y eventos externos son no confiables. El prefijo tenant y RLS son defensa adicional; la autorización siempre ocurre antes de emitir un grant.

## Amenazas y controles

| ID | Abuso/impacto | Control exigido | Evidencia R0 / gate pendiente |
|---|---|---|---|
| TM-FILE-01 | BOLA por ID, clave o URL de otro tenant | tenant derivado de sesión, autorización recurso/acción, respuesta neutral y token ligado a archivo+versión; manifest liga `tenant_id`+referencia opaca+tipo/ID de recurso | suite .NET y drill; IdP/RLS productivos pertenecen a R1 |
| TM-FILE-02 | replay, tamper u overwrite de grant | clave única server-side; MAC canónica liga propósito, archivo, versión, tenant, expiración y nonce; registro inmutable liga hash; TTL breve y upload one-shot | suite .NET; método/política cloud debe denegar overwrite |
| TM-FILE-03 | MIME falso/polyglot/archivo hostil | tamaño, allow-list, magic bytes, attachment, cuarentena y AV | MIME/marker sintético; archive bomb/sandbox real pendiente |
| TM-FILE-04 | scanner caído/resultado ambiguo | solo `clean` exacto publica; failed/unsupported/denied quedan en cuarentena | suite fail-closed; proveedor real pendiente |
| TM-FILE-05 | evento falso/duplicado/fuera de orden | firma/origen allow-list futuro, idempotencia, secuencia y match file+version+hash | suite de estado; EventBridge/Defender real pendiente |
| TM-FILE-06 | deduplicación revela existencia cross-tenant | nunca deduplicar ni autorizar por hash; claves físicamente separadas | suite dos tenants/mismo hash |
| TM-FILE-07 | objeto huérfano, metadata rota, corrupción o resultado de delete ambiguo | manifest, checksum y reconciliador privilegiado que aísla/reportan; `PurgeUncertain` nunca vuelve automáticamente a disponible ni se purga otra vez | drill real local y suite concurrente detectan huérfano/corrupción/ambigüedad |
| TM-FILE-08 | restore resucita borrados o rompe hold/auditoría | cutoff + versiones + manifest; restore aislado; hold ortogonal; auditoría append-only | drill verifica hold, hashes, geometría y auditoría; política legal pendiente |
| TM-FILE-09 | ransomware comparte permisos con backup | WORM, cuenta/principal y clave separados, break-glass auditado | diseño/runbook; cloud real pendiente |
| TM-FILE-10 | pérdida/inaccesibilidad KMS vuelve irrecuperable el backup | rotación controlada, protección de borrado, escrow/DR probado según proveedor | gate cloud/Legal/SRE |
| TM-FILE-11 | logs filtran contenido, nombre, token, tenant o ruta | allow-list de atributos, tenant seudonimizado, métricas agregadas | suite .NET/scan; OTel productivo pendiente |
| TM-FILE-12 | región/DPA/subencargados incompatibles | inventario, residencia, transferencia y contrato aprobados por `VAL-LEG` | **bloquea producción** |

## Criterio de cierre

Cualquier archivo `pending`, `scanning`, `quarantined`, `rejected` o `scan_failed` descargable es crítico. También lo son una fuga tenant, un restore con hash/vínculo/geometría/auditoría divergente o backups mutables por el mismo principal. Este spike no reduce esos gates por limitaciones del entorno.
