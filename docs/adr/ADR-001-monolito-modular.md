# ADR-001 — Monolito modular antes que microservicios

- Estado: aceptado
- Fecha: 2026-08-04

## Contexto

El producto combina GIS, clima, agricultura, ganadería, rotación, inventario, gestión económica e IA. Separarlo de inicio aumenta transacciones distribuidas, despliegues y observabilidad sin evidencia de escala independiente.

## Decisión

Backend único desplegable con módulos de dominio, contratos internos explícitos, worker y outbox. Frontend desplegable por separado.

## Alternativas

- Microservicios: rechazados inicialmente por costo/consistencia.
- CRUD monolítico sin límites: rechazado por acoplamiento.
- Serverless por función: no elegido como núcleo por transacciones y complejidad operativa.

## Consecuencias

Transacciones simples y entrega rápida; exige tests de arquitectura y disciplina modular. Se extrae un servicio solo ante escala, seguridad, ownership o resiliencia medibles.
