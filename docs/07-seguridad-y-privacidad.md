# Seguridad, privacidad y amenazas

## Objetivos

1. Evitar acceso cruzado entre organizaciones.
2. Proteger identidad, certificados fiscales, documentos y datos patrimoniales.
3. Mantener integridad de la trazabilidad, stock y comprobantes.
4. Permitir recuperación segura y auditoría útil.
5. Cumplir obligaciones de privacidad sin recolectar datos innecesarios.

## Clasificación

| Clase | Ejemplos | Tratamiento |
|---|---|---|
| Público | Contenido comercial público | CDN permitido |
| Interno | Catálogos no sensibles | Acceso autenticado |
| Confidencial | mapas, productividad, stock, contratos | Cifrado y mínimo privilegio |
| Fiscal/personal | CUIT, facturas, pagos, contactos | Retención/legal, auditoría, cifrado |
| Secreto | claves privadas, tokens, recovery codes | KMS/HSM/secret manager; nunca logs |

## Identidad y acceso

- Passkeys WebAuthn preferidas; Google OIDC y email OTP como alternativas.
- TOTP compatible como MFA; códigos de recuperación de un solo uso.
- MFA/passkey obligatorio para owner, administrador, contador y acceso fiscal.
- Step-up para cambiar roles privilegiados, exportar o borrar datos. Los controles sobre comprobantes y certificados fiscales pertenecen a una etapa futura.
- No vincular cuentas solo por email coincidente; reautenticar ambas identidades.
- Sesión en cookie segura; rotación/revocación, expiración y notificación de eventos.
- Rate limiting, anti-enumeración y protección frente a OTP bombing.
- Recuperación sin preguntas de seguridad y con auditoría reforzada.

## Autorización y tenancy

- Denegar por defecto.
- Validar membresía, rol, alcance de campo y permiso de acción en servidor.
- IDs enviados por cliente nunca determinan autorización.
- RLS y claves compuestas con `tenant_id` como defensa en profundidad.
- Object storage, cache, jobs y exportaciones segregados.
- Soporte interno con acceso just-in-time, caducidad, motivo y consentimiento.
- Pruebas BOLA/IDOR para todos los endpoints, workers y enlaces firmados.

## Secretos e integraciones

### Meteorología

- API keys solo en servidor/secret manager; nunca navegador, repositorio ni logs.
- Enviar al proveedor la mínima precisión/coordenada necesaria y registrar subencargado/región.
- Validar esquema, rangos físicos, unidades, timestamps y tamaño antes de persistir.
- No permitir que un payload externo modifique reglas o prompts.
- Firmar/hashear snapshots críticos y conservar procedencia para detectar alteraciones.
- Cuotas, cache, circuit breaker y protección ante 429/abuso.

### ARCA futuro

Los controles siguientes se mantienen como preparación, pero no se implementan en el MVP:

- No solicitar Clave Fiscal.
- Claves privadas X.509 en KMS/HSM o bóveda; no exportables cuando sea posible.
- Alertas de vencimiento, rotación, revocación y offboarding por CUIT.
- Tickets WSAA cacheados por CUIT/servicio, cifrados y de vida limitada.
- Payload fiscal normalizado en logs; respuesta original protegida, con acceso restringido.
- Doble confirmación configurable para notas o importes altos.

## Aplicación y API

- Validación server-side, límites de tamaño, paginación y cuotas por tenant.
- Protección CSRF/CORS, CSP estricta, encabezados seguros y TLS 1.2+.
- Consultas parametrizadas; no SQL generado libremente por IA.
- Archivos: allow-list real de tipo, límites, antivirus, nombre generado y object storage privado.
- SSRF: allow-list de destinos, resolución/control de red y sin fetch arbitrario de URLs del usuario.
- Webhooks firmados, timestamp, replay protection e inbox idempotente.
- Dependencias fijadas, SBOM, escaneo de secretos y actualizaciones de seguridad.

## Amenazas y controles

| Amenaza | Impacto | Controles principales |
|---|---|---|
| Acceso entre tenants/BOLA | Fuga masiva | autorización por objeto, RLS, pruebas negativas |
| Toma de cuenta | Datos/fraude | passkeys, MFA, rate limit, recovery auditado |
| Robo certificado ARCA futuro | Emisión fraudulenta | KMS/HSM, step-up, rotación, mínimo privilegio |
| Doble comprobante | Riesgo fiscal | idempotencia, lock fiscal, conciliación |
| Dato meteorológico alterado/vencido | Mala decisión | validación, snapshots, frescura, multifuente, abstención |
| Catálogo/perfil alterado o fuente comprometida | Reglas incorrectas a escala | snapshots/hash, staging, schema, revisión, firma/publicación, rollback y segregación editorial |
| Exposición de ubicación al proveedor | Confidencialidad | minimización, contrato, proxy backend y retención |
| Polígono inválido/malicioso | Caída/cálculo erróneo | vértices/tamaño, `ST_IsValid`, timeouts |
| Archivo malicioso | Malware | antivirus, sandbox/allow-list, storage privado |
| Prompt injection | Exfiltración/acción | aislamiento, tools allow-list, auth después del modelo |
| IA alucina recomendación | Daño productivo | evidencia, confianza, humano, evals y límites |
| Abuso administrador | Fuga/fraude | segregación, JIT, alertas y auditoría |
| Fuga por telemetría | Privacidad | redacción, no payloads, tenant seudonimizado |
| Ransomware/pérdida | Indisponibilidad | PITR, backups inmutables, restore probado |

## Privacidad por diseño

- Aviso claro: finalidad, categorías, destinatarios, retención y derechos.
- Recolectar solo datos necesarios; desactivar telemetría invasiva por defecto.
- Exportación, acceso, rectificación y supresión desde flujos controlados.
- Registro de bases/responsable ante AAIP cuando corresponda.
- Inventario de subencargados y región de procesamiento.
- Transferencias internacionales evaluadas y contratadas con salvaguardas.
- No entrenar modelos compartidos con datos de clientes sin opt-in contractual explícito.
- Analítica de producto agregada/seudonimizada.
- Retención por categoría; legal hold para documentación que deba conservarse.
- Eliminación lógica inmediata y purga física programada, contemplando backups.

## Auditoría

Separar auditoría de negocio/seguridad de logs técnicos. Debe ser append-only, consultable por permiso y exportable. No almacenar secretos, OTP, tokens, claves ni contenido sensible innecesario.

Eventos mínimos: login/recovery, cambio de rol, acceso soporte, exportación, publicación/rollback de catálogo o perfil, alta/rotación de certificado, emisión/rectificación fiscal futura, cierre/reapertura, ajuste de stock, geometría activada, recomendación IA aprobada/rechazada y borrado.

## Respuesta a incidentes

- Roles, canal y severidad definidos.
- Revocación de sesiones, credenciales y certificados.
- Preservación de evidencia y trazabilidad.
- Evaluación de notificación contractual/regulatoria.
- Comunicación a clientes y post-mortem sin culpabilización.
- Simulacro anual y runbooks probados.

## Checklist antes de producción

- Threat model actualizado y pruebas de aislamiento aprobadas.
- MFA/step-up y recuperación verificados.
- Secretos fuera de código/base/logs.
- SAST, SCA, secret scan, DAST y revisión manual sin altas/críticas abiertas.
- Backup/restore demostrado; revocación de certificado aplica cuando se incorpore ARCA.
- Política de privacidad, términos, contratos con proveedores y registro AAIP revisados.
- Responsable de seguridad/privacidad y canal de incidentes designados.
