# Runbook inicial de backup, restore y reconciliación

Este procedimiento es evidencia R0. Un restore productivo requiere change ticket, dos personas, credenciales break-glass, canal de incidente y aprobación de Privacy/Legal cuando afecte retención o residencia.

## Preparación

1. Declarar incidente, owner, cutoff UTC deseado, objetivo RPO/RTO y alcance tenant/región.
2. Congelar escrituras o fijar un fence/watermark; preservar outbox y auditoría.
3. Restaurar DB/PITR **a una instancia aislada nueva**, nunca sobre el origen.
4. Restaurar versiones exactas de objetos desde backup inmutable usando credencial distinta.
5. No reusar tokens, URLs firmadas, sesiones, secretos ni claves temporales del entorno afectado.

## Verificación antes de promover

- extensión PostGIS y SRID/geometrías coinciden;
- conteos y vínculos DB↔objetos coinciden con el manifest;
- SHA-256 y tamaño coinciden por versión;
- ningún estado no limpio queda disponible;
- legal holds permanecen y las bajas lógicas no recuperan visibilidad normal;
- auditoría append-only conserva secuencia/cadena y el outbox parte del watermark correcto;
- huérfanos, metadata sin objeto y corrupción están listados, aislados y no borrados automáticamente;
- pruebas tenant/BOLA y telemetría redactada pasan;
- RPO/RTO se calculan con timestamps del incidente, no con estimaciones.
- `PurgeUncertain` se resuelve solo con scope operativo de reconciliación y una comprobación actual del objeto; nunca se supone que un timeout implica rollback del delete.

## Decisión

- **Promover:** solo con cero divergencias y aprobación SRE/Data/AppSec.
- **Conciliar:** si hay huérfanos o referencias faltantes; mantener sistema aislado.
- **Abortar:** hash, PostGIS, auditoría, tenant, hold o KMS no verificables. Conservar evidencia y elegir otro punto.

## Ejecución local reproducible

```powershell
& '.\tasks\evidence\AGRO-DIS-005\spike\postgres\run-restore-drill.ps1' -Port 55435
```

El harness usa un clúster efímero loopback con `trust` solo en un host de desarrollo confiable. Reutiliza como herramienta el runtime PostgreSQL 17/PostGIS 3.6.2 fijado y validado por `AGRO-DIS-004`; no depende de su aplicación ni modifica su estado. Crea fuente, backup y destino aislados; ejecuta `pg_dump`/`pg_restore`, corrompe una copia, agrega un huérfano y prueba legal hold. El `finally` detiene el servidor y valida el puerto antes de limpiar.

## RPO/RTO y frecuencia

Los targets RPO ≤15 min, RTO ≤2 h y drill trimestral son hipótesis del discovery. La frecuencia, retención, región y soporte no son contractuales hasta cerrar Q-060/GAP-003/VAL-LEG. El ensayo local mínimo prueba el procedimiento; un dataset representativo y un proveedor real siguen siendo gates.
