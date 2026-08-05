# Lecciones del proyecto

Este archivo se actualizará después de cada corrección del sponsor o de un hallazgo que cambie el enfoque del producto.

## Regla inicial: separar ubicación legal, operación y geometría productiva

- Disparador: el dominio rural mezcla con facilidad establecimiento fiscal, campo gestionado y lote geográfico.
- Regla: modelar estos conceptos por separado y versionar las geometrías y asignaciones productivas.
- Prevención: ningún dato productivo, animal, cultivo o costo quedará vinculado solo a coordenadas sin una entidad y período de vigencia.

## Regla: no adelantar facturación, offline ni IA genérica sin prioridad del productor

- Disparador: el sponsor confirmó que el MVP debe abarcar agricultura y ganadería, pero por ahora no requiere facturación ARCA ni operación offline; la IA debe enfocarse en clima y rotación ganadera.
- Regla: separar el alcance de dominio del alcance de integraciones y capacidades técnicas; incluir agricultura/ganadería no autoriza sumar todas las especializaciones, fiscalidad u offline.
- Prevención: antes de priorizar una capacidad transversal, registrar quién la necesita, qué decisión mejora y si pertenece al MVP, a preparación arquitectónica o a una fase posterior.

## Regla: las recomendaciones ganaderas requieren mediciones, no solo ubicación

- Disparador: se pidió recomendar tiempos de rotación y alimentación usando el contexto de la zona.
- Regla: clima y ubicación aportan contexto, pero no reemplazan disponibilidad/altura de forraje, superficie, composición y peso del rodeo, consumo objetivo, agua, suelo y observaciones del potrero.
- Prevención: diseñar recomendaciones con datos mínimos, supuestos visibles, confianza, límites y aprobación humana; abstenerse cuando falten datos críticos.

## Regla: cobertura nacional significa catálogo extensible, no lógica rígida por cada producción

- Disparador: el sponsor indicó que deben incluirse las producciones, cultivos, especies y categorías trabajadas en toda Argentina, sin limitarse al campo piloto.
- Regla: separar el catálogo nacional configurable de actividades del nivel de automatización especializada. Todo rubro puede registrarse con flujos comunes; cada cálculo técnico específico requiere reglas, datos y validación profesional propios.
- Prevención: versionar taxonomías y permitir valores administrables, procedencia y vigencia; no prometer que una enumeración está cerrada ni reutilizar reglas de un cultivo o especie en otra.

## Regla: una delegación explícita del sponsor evita ciclos de reconfirmación

- Disparador: el sponsor aclaró que es el owner del producto, delegó las decisiones operativas seguras y pidió ejecución agéntica sin solicitar aprobaciones repetidas.
- Regla: una vez que existe un ID explícito y el sponsor delega defaults dentro del alcance aprobado, registrar esas decisiones y avanzar autónomamente; pedir intervención solo ante una autoridad profesional, dato real o cambio de alcance que no pueda delegarse.
- Prevención: distinguir una decisión de producto delegable de una firma profesional no sustituible, y convertir la primera en evidencia trazable en vez de volver a preguntar.

## Regla: anticipar cuándo una tarea de discovery no crea la aplicación

- Disparador: el sponsor esperaba ver un frontend React y un backend .NET mientras la tarea seleccionada, `AGRO-DIS-001`, era un gate R0 de catálogo y gobierno.
- Regla: al iniciar una tarea R0, declarar de inmediato qué artefacto demostrable producirá, qué código productivo no pertenece a su alcance y cuál es la primera tarea posterior que sí crea esa parte de la aplicación.
- Prevención: antes de editar, contrastar el ID con su release, naturaleza y dependencias; no presentar evidencia documental como si fuera un vertical slice ni ocultar bootstrap de otra tarea dentro del spike.

## Regla: el líder elige la próxima tarea y comunica la decisión

- Disparador: el sponsor corrigió el ciclo de pedir un ID exacto después de haber delegado explícitamente la priorización y ejecución agéntica.
- Regla: cuando termina una tarea o una candidata está bloqueada, el líder audita readiness, elige la mejor siguiente tarea dentro del orden/dependencias y avanza informando el ID; no traslada al sponsor una decisión operativa ya delegada.
- Prevención: reservar preguntas para datos, credenciales, firmas profesionales o cambios de alcance realmente no delegables. La selección del ID y los defaults técnicos reversibles son responsabilidad del orquestador mientras se mantenga el límite de una tarea por vez.

## Regla: publicar el baseline autorizado antes de continuar el backlog

- Disparador: el sponsor aportó el repositorio Git vacío y pidió publicar frontend, backend y documentación antes de seguir con la siguiente tarea.
- Regla: cuando existe autorización explícita para Git y un remoto vacío verificado, preparar exclusiones de artefactos generados, revisar secretos y whitespace, crear un commit convencional y comprobar el hash remoto antes de iniciar el siguiente slice.
- Prevención: no dejar código funcional solamente en el workspace cuando el sponsor ya habilitó su publicación; tampoco mezclar la publicación inicial con una tarea nueva todavía no verificada.

## Regla: pnpm es el package manager del nuevo frontend

- Disparador: el sponsor pidió reemplazar npm por pnpm antes de iniciar la implementación frontend de `AGRO-DIS-004`.
- Regla: todo frontend nuevo usa pnpm y conserva `pnpm-lock.yaml`; no generar ni mantener `package-lock.json` en esos proyectos.
- Prevención: fijar `packageManager` en `package.json`, crear el lock con la versión disponible y ejecutar instalaciones verificables con `pnpm install --frozen-lockfile`.

## Regla: filtros reactivos y viewport de tabla estable

- Disparador: el sponsor pidió eliminar la lupa/aplicación manual al filtrar por texto, fecha u otros campos, limitar el loader a la tabla y evitar que el scroll horizontal solo sea accesible al final de listados largos.
- Regla: los filtros de tablas aplican automáticamente con debounce/cancelación cuando corresponda; el estado de carga conserva shell y controles utilizables y actualiza solo la región de resultados con estado accesible.
- Prevención: el componente de tabla debe ofrecer desplazamiento horizontal accesible desde el viewport visible —mediante contenedor sticky/sincronizado o layout equivalente—, conservar foco/posición y probar teclado, pantalla angosta, requests obsoletos y resultados vacíos. No crear un filtro global ni re-renderizar toda la página por cada cambio.
