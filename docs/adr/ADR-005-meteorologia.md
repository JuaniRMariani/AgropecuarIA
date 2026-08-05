# ADR-005 — Meteorología multifuente y auditable

- Estado: aceptado para contrato base de discovery; WRF postergado
- Fecha: 2026-08-05

## Contexto

Clima y lluvia son casos centrales del MVP. Una API global facilita integración, pero las alertas oficiales argentinas y la validación regional requieren fuentes separadas. Un pronóstico no es una observación del lote.

## Decisión

- `WeatherProvider` interno y consumo únicamente backend.
- Open-Meteo comercial como candidato REST primario, condicionado a plan, DPA/región, cuota, atribución y presupuesto aprobados.
- SMN CAP como autoridad de alertas.
- SMN WRF detrás de un puerto/flag, postergado fuera del camino MVP hasta aprobar presupuesto y operación.
- Pluviómetro/estación propia como lluvia observada prioritaria cuando exista; es opcional y no bloquea el producto.
- Snapshots inmutables con proveedor, modelo, corrida, validez, celda, resolución, unidad y naturaleza del dato.

## Alternativas

- Solo SMN WRF: oficial y detallado, pero no REST, pesado y sin probabilidad/horizonte largo.
- Solo Open-Meteo: simple, pero sin alertas oficiales SMN ni observación local.
- NASA POWER como principal: útil históricamente, resolución/latencia insuficientes para decisión operativa.
- Endpoint interno no documentado del sitio SMN: rechazado por falta de contrato.

## Consecuencias

Hay costo comercial y pipeline adicional para WRF/CAP. A cambio se obtiene integración simple, alertas oficiales, fallback y capacidad de evaluar precisión por zona. Si un dato está vencido, AgropecuarIA lo muestra como tal o se abstiene; nunca completa valores con IA.

`AGRO-DIS-004` confirmó cobertura contractual de Open-Meteo en 24 puntos y parseo NetCDF WRF, no precisión local. Una muestra WRF horaria pesa 14.758.413 bytes: 73 plazos superan 1 GiB por corrida antes de productos adicionales. Registry AWS y documentación WRF difieren en cadencia; cada corrida conserva identidad observada y no se codifican horarios fijos. CAP puede degradar a HTML/no RSS, por lo que content type, schema, frescura y lifecycle se validan antes de publicar una alerta.

## Revisión

Revisar tras un ciclo estacional comparando Open-Meteo/ECMWF, GFS y SMN WRF contra pluviómetros/estaciones disponibles. Si el piloto no posee observación local, usar una red cercana y declarar esa limitación.
