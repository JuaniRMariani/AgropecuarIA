# Fuentes, atribución y decisión

Revisión: 2026-08-05. Los términos pueden cambiar y deben revalidarse antes de producción.

| Componente | Fuente primaria | Términos/licencia observados | Evidencia local | Decisión R0 | Condición productiva |
|---|---|---|---|---|---|
| PostGIS 3.6.2 | [PostGIS Windows](https://postgis.net/documentation/getting_started/install_windows/released_versions/) | Bundle oficial para PostgreSQL 17; el runtime se usa solo en pruebas | harness SQL + versión de extensión | `GO` técnico | fijar imagen/runtime soportado y ensayar migración/restore |
| Georef | [API oficial](https://www.argentina.gob.ar/georef/referencia-completa-de-la-api) | Servicio oficial, gratuito, abierto; código MIT. No se evidenció SLA | 24 IDs/centroides y hash de respuesta | `GO CONDICIONAL` | snapshot versionado, rate limit, atribución y fallback manual |
| Argenmap | [Geoservicios IGN](https://www.ign.gob.ar/NuestrasActividades/InformacionGeoespacial/ServiciosOGC) | Servicio libre y gratuito; el ejemplo oficial pide atribuir IGN + OpenStreetMap y comunicar demanda de uso | 24 tiles, p75 y hash agregado | `GO CONDICIONAL` | confirmar capacidad/SLA con IGN y conservar atribución/fallback tabular |
| MapLibre GL JS | [Documentación oficial](https://maplibre.org/maplibre-gl-js/docs/) | Librería renderer; no provee tiles | build/bundle y browser real | `GO` renderer | CSP/worker y proveedor de estilo/tiles allow-listed |
| Open-Meteo | [Pricing](https://open-meteo.com/en/pricing), [Terms](https://open-meteo.com/en/terms), [Licence](https://open-meteo.com/en/license) | Free solo evaluación/no comercial; datos CC BY 4.0 con atribución; plan pago brinda endpoint/key y target 99,9 % | smoke batch único de 24 respuestas/7 variables, contrato y fallos fixture; no es p75 ni mide cache | `GO CONDICIONAL` contrato | sponsor/procurement/legal eligen plan, DPA/región/cuota y presupuesto; medir p75 cacheado en R2 |
| SMN CAP | [Feed oficial](https://ssl.smn.gob.ar/CAP/AR.php), [CAP 1.2 OASIS](https://docs.oasis-open.org/emergency/cap/v1.2/CAP-v1.2-os.html) | Feed declara SMN CC BY 4.0; CAP define lifecycle y geometría | fixture real/sintéticos; probe puede observar HTML degradado | `GO CONDICIONAL` autoridad | monitorear frescura/schema, canal oficial alternativo y no mantener alertas stale |
| SMN WRF | [AWS Open Data](https://registry.opendata.aws/smn-ar-wrf-dataset/), [documentación SMN](https://odp-aws-smn.github.io/documentation_wrf_det/) | CC BY 2.5 Argentina; bucket público sin credencial | NetCDF 14.758.413 bytes, SHA-256 fijado, parser y límites | `POSTPONER` | presupuesto, storage/cómputo/operación y validación local; no asumir cadencia fija |

## Atribución visible del prototipo

- “Mapa base: Instituto Geográfico Nacional + OpenStreetMap”.
- “Weather data by Open-Meteo.com” junto a cualquier dato Open-Meteo mostrado.
- “Alertas oficiales: Servicio Meteorológico Nacional”, conservando fuente y vigencia.
- WRF: “SMN Hi-Res Weather Forecast over Argentina; acceso y fecha”, si se visualiza evidencia del dataset.

## No-go explícitos

- Tiles comunitarios directos de OpenStreetMap como proveedor productivo.
- Free API Open-Meteo para el SaaS comercial.
- Endpoint interno/no documentado del sitio SMN.
- WRF como fallback productivo antes de presupuesto, operación y evaluación contra observación.
