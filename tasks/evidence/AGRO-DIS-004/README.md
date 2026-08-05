# AGRO-DIS-004 — GIS, mapas y meteorología multifuente

Evidencia reproducible de un spike R0 aislado. Valida contratos, degradación, cobertura nacional y costos técnicos antes de construir GIS/clima productivo. No contiene coordenadas de campos, credenciales, migraciones productivas ni una aprobación agronómica.

## Resultado resumido

- PostGIS: harness efímero real para SRID, validez, área, intersección y GiST, sin tocar el PostgreSQL del sistema.
- Cobertura: fixture oficial Georef con las 23 provincias y CABA. El centroide de Tierra del Fuego incluye el territorio antártico y se conserva como borde explícito, no se corrige silenciosamente.
- Mapa: MapLibre como renderer y Argenmap/IGN como proveedor de tiles, con atribución y alternativa tabular equivalente.
- Clima: contrato inmutable que separa `observed`, `estimated` y `forecast` de `fresh`, `stale` y `unavailable`.
- CAP: parser seguro y lifecycle `Alert/Update/Cancel/expired`; un feed que devuelve HTML o queda stale se degrada a no disponible.
- WRF: muestra NetCDF oficial válida dentro de los límites del spike, pero decisión `POSTPONER`: 73 archivos horarios del tamaño medido superan 1 GiB por corrida y no existe presupuesto/operación aprobada.

La evidencia técnica no afirma precisión meteorológica local. Q-012/Q-024/Q-027/Q-028 y `VAL-AGR` siguen requiriendo campo piloto, observación y aprobación profesional.

## Estructura

- `contracts/`: contratos JSON Schema versionados.
- `fixtures/`: datos públicos y sintéticos; los tests no dependen de red.
- `scripts/`: probes live y lector WRF aislado.
- `spike/src` y `spike/tests`: contratos/parsers .NET 10 y pruebas MTP/MSTest.
- `spike/postgis`: runtime PostgreSQL/PostGIS efímero y probes SQL.
- `spike/web`: prototipo Next.js/React/MapLibre administrado con pnpm.
- `results/`: resultados medidos y hashes de contenido.

## Verificación

Desde `tasks/evidence/AGRO-DIS-004`:

```powershell
./scripts/probe-providers.ps1
./scripts/inspect-wrf.ps1
./.runtime/wrf-venv/Scripts/python.exe -m unittest discover -s scripts -p 'test_*.py' -v
```

Desde `spike`:

```powershell
dotnet restore AgropecuarIA.GisWeatherSpike.slnx
dotnet build AgropecuarIA.GisWeatherSpike.slnx --no-restore
dotnet test --solution AgropecuarIA.GisWeatherSpike.slnx --no-build
```

Los comandos PostGIS se documentan dentro de `spike/postgis`. Desde `spike/web`:

```powershell
pnpm install --frozen-lockfile
pnpm run format:check
pnpm run lint
pnpm run typecheck
pnpm run test
pnpm run build
pnpm run test:e2e
```

## Límites

- Los probes live son diagnóstico de disponibilidad, no tests determinísticos ni un SLA.
- El free endpoint de Open-Meteo se usa solo para evaluación; producción exige plan comercial, revisión legal/privacidad y clave solo backend.
- Argenmap es libre y gratuito, pero no se evidenció SLA contractual; la tabla y las coordenadas siguen disponibles si fallan tiles.
- El runtime `.runtime/` es descargable/recreable e ignorado por Git. Ningún artefacto del spike debe promoverse automáticamente a R1/R2.
