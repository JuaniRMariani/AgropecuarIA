# Reporte de validación — Catálogo Nacional v1

Fecha de ejecución: 2026-08-04  
Versión candidata: `1.0.0-candidate.1`  
Entorno del validador: Windows PowerShell `5.1.26100.8875`  
Resultado técnico integrado: **PASS**  
Estado de aprobación profesional: **PENDIENTE**

## Resultado exacto

La pasada integrada final, ejecutada después de fijar datasets, privacidad del contrato de publicación, prototipo, lockfile, fixture STALE, correcciones de accesibilidad y manifest de 35 artefactos estables, finalizó con código de salida `0`:

```text
Validación Catálogo Nacional v1
  Versión: 1.0.0-candidate.1
  Fuentes: 10
  Entradas vegetales: 154
  Entradas animales: 59
  Entradas reguladas: 31
  Excepciones: 3
  Fixtures de búsqueda: 637
  Fallos: 0
Resultado: PASS
```

El parser de PowerShell informó `PARSER_ERRORS=0`. Inmediatamente después de `npm run build`, el validator finalizó con exit `0`, `PASS` y `0` fallos. El manifest contenía exactamente 35 artefactos estables, con tamaños y SHA-256 válidos; `prototype/next-env.d.ts` quedó fuera de hashes por ser autogenerado mutable y su variante build fue validada semánticamente.

## Comandos y gates ejecutados

Desde la raíz del proyecto:

```powershell
$tokens=$null; $errors=$null; [void][System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path 'tasks/evidence/AGRO-DIS-001/validate-catalog.ps1'),[ref]$tokens,[ref]$errors); "PARSER_ERRORS=$($errors.Count)"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\tasks\evidence\AGRO-DIS-001\validate-catalog.ps1"
```

Resultados finales: parser `0` errores; validator exit `0`, `PASS`, `0` fallos con 35 artefactos.

Desde `tasks/evidence/AGRO-DIS-001/prototype`:

```powershell
npm ci
npm run lint
npm run typecheck
npm test
npm run build
npm audit
Set-Location ..\..\..\..
powershell -NoProfile -ExecutionPolicy Bypass -File ".\tasks\evidence\AGRO-DIS-001\validate-catalog.ps1"
```

El orden de cierre es vinculante: `build` debe ejecutarse antes del validator. Si se ejecuta `dev` después, se debe detener el servidor, repetir `build` y recién entonces ejecutar el validator. `next-env.d.ts` puede existir, pero no forma parte del manifest ni de la cadena de hashes.

Resultados:

- `npm ci`: PASS con lockfile.
- `npm run lint`: PASS.
- `npm run typecheck`: PASS con TypeScript estricto.
- `npm test`: PASS, 9/9 tests.
- `npm run build`: PASS con Next.js `16.3.0`; `/` e `/icon.svg` quedaron estáticos.
- `npm audit`: 0 vulnerabilidades.

## Cobertura del validador

### Catálogo, trazabilidad e integridad

- UTF-8 estricto, JSON válido, campos requeridos/permitidos, versión, dominio y ciclo de vida.
- 213 entradas: 154 vegetales y 59 animales; 31 reguladas y 3 excepciones declaradas.
- Códigos globalmente únicos, jurisdicciones argentinas y referencias a 10 fuentes con evidencia local 1:1.
- `reviewStatus` y `lifecycleStatus` separados; sucesores existentes, del mismo dominio, no autorreferentes y con grafo acíclico.
- 13 familias animales y 205 términos dimensionales comparados exactamente con el oráculo.
- 637 fixtures exhaustivos para código, nombre, alias, dimensión familiar, colisiones y consulta inexistente.

### Contrato conceptual de diff y publicación

- `contractVersion=1.0.0` y versión candidata exacta.
- Campos requeridos del diff y tipos `ADDED`, `UPDATED`, `INACTIVATED` y `SUCCEEDED`.
- Invariantes fail-closed para identidad estable, referencias históricas, sucesión y cambios de soporte respaldados por evidencia.
- Evento conceptual `ProductCatalogPublished` versión `1`, campos obligatorios exactos, idempotencia por `catalogVersion + manifestSha256` y semántica append-only para rollback.
- `publishedBySubject` se clasifica como dato personal seudonimizado: `containsPersonalData=true`, catálogo global `tenantScoped=false`, sin email/nombre directo y con acceso y retención restringidos.
- Ejemplo coherente con códigos y fuentes existentes, fechas, conteos y clases de cambio válidas.

### Prototipo R0

- Inventario exacto de 23 artefactos estables del prototipo. Se excluyen de hashes `node_modules`, `.next` y `next-env.d.ts` por ser salidas autogeneradas mutables.
- Si `next-env.d.ts` existe, el validator exige UTF-8 válido, referencias oficiales de Next/Image, imports coherentes de la misma variante `build` o `dev` y el aviso oficial de archivo autogenerado.
- `package-lock.json` obligatorio, lockfile versión `3`, paquete raíz alineado y dependencias directas fijadas sin rangos.
- Búsqueda determinística por código, nombre y alias, normalización de tildes/mayúsculas y filtros de dominio/soporte.
- Estados `loading`, `empty`, `error`, `ready` y `stale`; fixture local STALE clasificado sin red y publicación bloqueada con `canPublish=false`.

## Evidencia en navegador real

La revisión con Playwright obtuvo HTTP `200` y confirmó:

- carga del catálogo completo con 213 entradas;
- búsqueda de “ponedoras” con una única coincidencia;
- estados stale, error, empty y loading demostrados;
- combinación STALE + 0 resultados con aviso de fuente desactualizada y `0 coincidencias` visibles simultáneamente;
- viewport móvil `390x844`;
- foco de teclado del buscador visible, con outline computado `solid 3px rgb(229, 185, 74)`;
- consola final con 0 errores y 0 warnings.

## Límites y aprobación pendiente

Este entregable es una validación R0. El prototipo está aislado, es descartable y no constituye bootstrap productivo. No existe backend, API, persistencia, autenticación, autorización tenant ni emisión real de `ProductCatalogPublished`. Ningún comando publicó el catálogo, desplegó software o modificó sistemas externos.

`PASS` demuestra integridad, trazabilidad local, contrato conceptual, comportamiento reproducible del prototipo y gates técnicos verdes. No certifica corrección agronómica, veterinaria, taxonómica o legal. La firma de los revisores profesionales definidos por la tarea continúa pendiente y mantiene la tarea en revisión; no debe interpretarse este reporte como autorización de publicación ni como elevación de niveles de soporte.

## Cierre de hashes

Este reporte forma parte de los artefactos hasheados. Después de fijar este contenido debe actualizarse únicamente su tamaño/SHA-256 en `catalog-v1.manifest.json` y repetirse parser y validator, sin ejecutar otro `build` ni `dev` y sin nuevas ediciones en los 35 artefactos. La corrida inmutable de cierre debe conservar exit `0`, `PASS` y `0` fallos; el manifest y `prototype/next-env.d.ts` quedan excluidos para evitar autorreferencia y drift de generación.
