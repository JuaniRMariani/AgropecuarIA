# EPIC-13 — Frontend, design system y accesibilidad

Objetivo: experiencia Next.js/React coherente, responsiva, PWA online, WCAG 2.2 AA y estados completos. Transversal de R1 a R6.

<a id="agro-fe-001"></a>

## AGRO-FE-001 — Entregar shell, navegación y sistema de diseño accesibles

- **Estado:** En curso. El sub-slice local `OwnerWorkspaceShellV1` organiza únicamente superficies owner ya existentes; roles adicionales, preferencias persistentes, matriz completa de navegadores y certificación WCAG del padre continúan pendientes.
- **Release, épica, prioridad y tamaño:** R1 · EPIC-13 · Must · L.
- **Owner y colaboradores:** Frontend/Diseño de Producto; Identidad, QA y Accesibilidad.
- **Resultado/valor esperado:** navegación por organización/campo/rol y componentes consistentes.
- **Historia/JTBD:** Como usuario, quiero orientarme y operar con móvil, teclado o lector.
- **Alcance incluido:** shell, selector de contexto/URL, tokens/componentes, foco, teclado, reflow, contraste y estados de sesión.
- **Fuera de alcance:** duplicar la autorización o el dominio en el cliente y persistir datos de negocio para uso offline.
- **Requisitos trazados:** RNF-UX-001–004; RNF-CON-003/004; RF-ID-005; Q-054/055/061.
- **Precondiciones y dependencias:** contratos de Identidad y decisión de arquitectura frontend.
- **Contrato/API/eventos afectados:** sesión, contexto efectivo y telemetría de navegación.
- **Datos, índices, migración y compatibilidad:** sin almacenamiento de dominio; preferencias versionadas de idioma, zona horaria y unidades.
- **Autenticación, autorización, tenant y auditoría:** la UI oculta lo no aplicable, pero el servidor decide; cambio de contexto y cierre de sesión seguros.
- **Frontend:** constituye el resultado de la tarea; estados carga, vacío, error, sesión vencida y sin red; UUID solo como `ABCDEF`.
- **Reglas e invariantes:** no mostrar UUID completos en tablas, tarjetas ni modales; español y presentación local con zonas IANA.
- **Criterios de aceptación:** Dado teclado, zoom o móvil, cuando el usuario navega, entonces foco, reflow, contexto y estado permanecen claros, y las rutas no autorizadas no revelan datos.
- **Casos negativos y bordes:** último campo eliminado, sesión vencida, 4G lento y navegador no soportado.
- **Estrategia de pruebas:** componentes, accesibilidad manual/automática, E2E por roles, diseño responsivo y matriz de navegadores.
- **Observabilidad:** Web Vitals/navigation/errors sin PII.
- **Seguridad y privacidad:** no tokens localStorage; CSP/CSRF contrato.
- **Performance/capacidad y límites:** presupuestos por ruta y shell rápido.
- **Feature flag, rollout, migración, rollback y recuperación:** adopción progresiva de componentes y estilos alternativos seguros.
- **Documentación:** sistema de diseño, navegación y decisiones de accesibilidad.
- **Comandos/evidencia esperados:** lint, typecheck, pruebas de componentes/accesibilidad y build cuando exista el proyecto.
- **Definition of Ready:** roles, contextos y matriz de navegadores definidos.
- **Definition of Done:** evidencia de shell WCAG y responsivo.
- **Bloqueos/preguntas:** Q-054/055/061.
- **Paralelizable:** tokens, shell y accesibilidad en archivos sin colisión.

<a id="agro-fe-002"></a>

## AGRO-FE-002 — Estandarizar contratos, formularios, tablas y estados

- **Release, épica, prioridad y tamaño:** R1 · EPIC-13 · Must · L.
- **Owner y colaboradores:** Frontend; Backend Architecture, QA, AppSec y Design.
- **Resultado/valor esperado:** cada slice maneja errores, conflictos e importaciones sin reconstruir patrones.
- **Historia/JTBD:** Como usuario, quiero formularios que no pierdan datos y tablas útiles en móvil.
- **Alcance incluido:** cliente API tipado; 401/403/404/409/412; idempotencia en UI; formularios/errores; tablas/listas/filtros/paginación; carga y progreso de trabajos.
- **Fuera de alcance:** capa API personalizada que duplique el backend y diseño móvil dependiente solo de desplazamiento horizontal.
- **Requisitos trazados:** RNF-UX-001–004; RNF-CON-001/002/004; RF-ADM-002/004; RF-DOC-001.
- **Precondiciones y dependencias:** FND-001–003 y FE-001.
- **Contrato/API/eventos afectados:** cliente OpenAPI, errores, idempotencia y estado de trabajos.
- **Datos, índices, migración y compatibilidad:** caché cliente segmentada por tenant, recurso y versión; sin persistencia offline de datos sensibles.
- **Autenticación, autorización, tenant y auditoría:** CSRF y vencimiento de sesión; el servidor no confía en el cliente.
- **Frontend:** estados carga, vacío, error, obsoleto, degradado, conflicto y sin red; tabla adaptada a lista; IDs abreviados.
- **Reglas e invariantes:** el reintento no duplica; el conflicto es explícito; los datos ingresados se conservan en memoria si la política lo permite.
- **Criterios de aceptación:** Dado un 409/412 o pérdida de red, cuando se envía, entonces el usuario ve opciones de recuperación sin confirmación falsa ni promesa de sincronización local.
- **Casos negativos y bordes:** doble clic, URL/sesión vencida, importación parcial, tabla grande y proveedor con dato obsoleto.
- **Estrategia de pruebas:** contrato, componentes, E2E, accesibilidad, red y seguridad.
- **Observabilidad:** errores API/UI, reintentos, abandono de formularios y Web Vitals sin PII.
- **Seguridad y privacidad:** sanitización, CSP, ausencia de secretos y caché mínima.
- **Performance/capacidad y límites:** paginación; virtualización solo si se justifica con mediciones; presupuestos de bundle.
- **Feature flag, rollout, migración, rollback y recuperación:** patrón adoptado incrementalmente; cliente compatible con N/N-1.
- **Documentación:** catálogo de interacciones, errores y estados.
- **Comandos/evidencia esperados:** futuros comandos configurados de calidad frontend.
- **Definition of Ready:** contratos/errores del backend y patrones UX definidos.
- **Definition of Done:** patrón reutilizado en slices reales de identidad y catálogo.
- **Bloqueos/preguntas:** topología de CSRF/sesión.
- **Paralelizable:** sí, entre componentes, contrato y pruebas.

<a id="agro-fe-003"></a>

## AGRO-FE-003 — Construir mapa y experiencias productivas adaptativas

- **Release, épica, prioridad y tamaño:** R2–R4 · EPIC-13 · Must · L.
- **Owner y colaboradores:** Frontend; GIS, Agricultura, Ganadería, Pastoreo, QA y Dominio.
- **Resultado/valor esperado:** mapa/formularios complejos accesibles y sin pantalla por especie/cultivo.
- **Historia/JTBD:** Como operador, quiero mapa y flujos guiados según soporte/perfil.
- **Alcance incluido:** isla MapLibre, alternativa sin mapa, formularios genéricos/por perfil guiados por esquema y estados de operaciones, agricultura, ganadería y pastoreo.
- **Fuera de alcance:** incluir MapLibre en todos los bundles, interacción solo mediante canvas e inaccesible y actividades hardcodeadas.
- **Requisitos trazados:** RF-GIS-002–007; RF-PRD-001–005; RF-OPS-001–005; RF-AGR-001–008/011; RF-GAN-001–017; RNF-PER-002.
- **Precondiciones y dependencias:** FE-001/002 y contratos de dominio.
- **Contrato/API/eventos afectados:** GIS/perfil/flujo contratos.
- **Datos, índices, migración y compatibilidad:** el estado cliente referencia versiones; sin cola offline.
- **Autenticación, autorización, tenant y auditoría:** cada ruta y recurso se autoriza en el servidor.
- **Frontend:** mapa, lista y formularios; densidad móvil; tacto/teclado; todos los estados degradados, de conflicto y evidencia.
- **Reglas e invariantes:** nivel de soporte visible; los bloqueos de seguridad no pueden evadirse mediante una CTA; UUID completo oculto.
- **Criterios de aceptación:** Dado un caso genérico, observado, estimado o bloqueado, cuando se opera con móvil/teclado, entonces aparecen el formulario, las acciones y una ruta equivalente sin mapa correctos.
- **Casos negativos y bordes:** cambio de perfil, conflicto geométrico, formulario grande, ausencia de clima/biomasa y pantalla estrecha.
- **Estrategia de pruebas:** componentes y E2E por release, accesibilidad manual, regresión visual y rendimiento.
- **Observabilidad:** errores de ruta/mapa, Web Vitals y embudo por capacidad.
- **Seguridad y privacidad:** sin fugas de coordenadas/tenant; sanitización del contenido guiado por esquemas.
- **Performance/capacidad y límites:** mapa ≤3 s p75 en 4G; división de código y listas acotadas.
- **Feature flag, rollout, migración, rollback y recuperación:** por dominio, perfil y tenant; alternativa genérica.
- **Documentación:** especificaciones UX, matriz de estados y alternativa accesible al mapa.
- **Comandos/evidencia esperados:** futuras evidencias de E2E, accesibilidad y build.
- **Definition of Ready:** prototipo UX, contratos y perfiles disponibles.
- **Definition of Done:** flujo de cada release accesible y responsivo.
- **Bloqueos/preguntas:** datos del piloto y selección de perfiles.
- **Paralelizable:** sí, por dominio, con owner del sistema de diseño.

<a id="agro-fe-004"></a>

## AGRO-FE-004 — Certificar PWA online y rendimiento frontend

- **Release, épica, prioridad y tamaño:** R2–R6 · EPIC-13 · Must · M.
- **Owner y colaboradores:** Frontend/SRE/QA; Product y AppSec.
- **Resultado/valor esperado:** aplicación instalable y honestamente online, usable con los objetivos de red rural.
- **Historia/JTBD:** Como usuario rural, quiero saber antes de confirmar si no hay red y evitar falsa pérdida/duplicación.
- **Alcance incluido:** instalación, feedback de conexión/proveedor, presupuestos por ruta/bundle, Web Vitals, matriz de navegadores/dispositivos y E2E críticos.
- **Fuera de alcance:** caché de negocio en service worker, sincronización mediante IndexedDB y mapas offline.
- **Requisitos trazados:** RF-GIS-010 exception; RNF-CON-001–004; RNF-PER-002/005; Q-061.
- **Precondiciones y dependencias:** FE-001–003 y DIS-007.
- **Contrato/API/eventos afectados:** conexión, estado y telemetría.
- **Datos, índices, migración y compatibilidad:** sin almacenamiento offline de dominio; solo activos estáticos seguros.
- **Autenticación, autorización, tenant y auditoría:** sin respuestas sensibles ni tokens en caché.
- **Frontend:** advertencia sin conexión antes de confirmar, estados de reconexión/reintento y aviso de instalación accesible.
- **Reglas e invariantes:** nunca afirmar persistencia o sincronización sin confirmación del servidor.
- **Criterios de aceptación:** Dada una pérdida de red, cuando el usuario confirma, entonces la UI bloquea o informa claramente el fallo, y un reintento posterior no duplica la operación.
- **Casos negativos y bordes:** 4G intermitente, proveedor caído frente a red caída, navegador antiguo y pestaña obsoleta.
- **Estrategia de pruebas:** perfiles de red, auditoría PWA, E2E de reintento, rendimiento y revisión de seguridad de caché.
- **Observabilidad:** Web Vitals, fallos de red y reintentos.
- **Seguridad y privacidad:** allow-list de caché, sin secretos ni datos sensibles.
- **Performance/capacidad y límites:** objetivos RNF y presupuestos por ruta.
- **Feature flag, rollout, migración, rollback y recuperación:** flag de instalación PWA; rollback seguro del manifiesto/service worker.
- **Documentación:** limitaciones del modo solo en línea y guía de soporte.
- **Comandos/evidencia esperados:** futuras verificaciones configuradas de build, E2E, rendimiento y PWA.
- **Definition of Ready:** matriz de dispositivos y redes definida.
- **Definition of Done:** objetivos demostrados y ausencia de comportamiento offline.
- **Bloqueos/preguntas:** Q-061.
- **Paralelizable:** pruebas continuas.
