# ADR-005 — Meteorología multifuente y auditable

- Estado: propuesto
- Fecha: 2026-08-04

## Contexto

Clima y lluvia son casos centrales del MVP. Una API global facilita integración, pero las alertas oficiales argentinas y la validación regional requieren fuentes separadas. Un pronóstico no es una observación del lote.

## Decisión

- `WeatherProvider` interno y consumo únicamente backend.
- Open-Meteo comercial como REST primario propuesto.
- SMN CAP como autoridad de alertas.
- SMN WRF como fallback 0–72 h después de un spike NetCDF.
- Pluviómetro/estación propia como lluvia observada prioritaria cuando exista; es opcional y no bloquea el producto.
- Snapshots inmutables con proveedor, modelo, corrida, validez, celda, resolución, unidad y naturaleza del dato.

## Alternativas

- Solo SMN WRF: oficial y detallado, pero no REST, pesado y sin probabilidad/horizonte largo.
- Solo Open-Meteo: simple, pero sin alertas oficiales SMN ni observación local.
- NASA POWER como principal: útil históricamente, resolución/latencia insuficientes para decisión operativa.
- Endpoint interno no documentado del sitio SMN: rechazado por falta de contrato.

## Consecuencias

Hay costo comercial y pipeline adicional para WRF/CAP. A cambio se obtiene integración simple, alertas oficiales, fallback y capacidad de evaluar precisión por zona. Si un dato está vencido, AgropecuarIA lo muestra como tal o se abstiene; nunca completa valores con IA.

## Revisión

Revisar tras un ciclo estacional comparando Open-Meteo/ECMWF, GFS y SMN WRF contra pluviómetros/estaciones disponibles. Si el piloto no posee observación local, usar una red cercana y declarar esa limitación.
