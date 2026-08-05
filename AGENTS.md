# Instrucciones del proyecto

- Leer `README.md`, `tasks/todo .md` y `tasks/lessons .md` antes de modificar el proyecto.
- Mantener la solución simple: monolito modular, contratos HTTP explícitos y PostgreSQL/PostGIS, salvo evidencia que justifique otra cosa.
- No inventar reglas fiscales, sanitarias ni contables; marcar supuestos y pedir validación profesional.
- Preservar trazabilidad: los hechos confirmados se revierten o rectifican; no se sobrescriben silenciosamente.
- Todo acceso a datos debe incluir aislamiento por organización y autorización por recurso.
- Ninguna función crítica debe depender de IA. Toda recomendación debe mostrar evidencia, fecha, supuestos y confianza.
- Implementar slices verticales con criterios de aceptación, pruebas, observabilidad y documentación.
- En tablas y tarjetas, mostrar UUID como los primeros 6 caracteres en mayúsculas y sin guiones; no exponer UUID completos salvo pedido expreso.
- Documentar plan y resultados en `tasks/todo .md`; registrar en `tasks/lessons .md` cada corrección del sponsor.
- Antes de cerrar una tarea, ejecutar los quality gates aplicables y registrar resultados y riesgos residuales.
- Commits, solo cuando el sponsor los pida, con formato inglés `type(module): message`.
