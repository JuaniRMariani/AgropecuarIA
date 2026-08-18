# Decisiones — OwnerWorkspaceShellV1

Fecha: 2026-08-18. Estado: aceptado para desarrollo local integrado; `AGRO-FE-001` permanece `En curso`.

## Contexto y autoridad

- El shell sólo compone memberships `owner/active` presentes en la sesión actual. Nunca transforma un locator de UI en autorización.
- El contexto usa `?org=ABCDEF&view=fields|team|territory|account`. El prefijo visible se normaliza como los seis primeros caracteres hexadecimales del UUID sin guiones y en mayúsculas.
- El prefijo se resuelve exclusivamente contra las memberships vigentes. Cero o más de una coincidencia es inválido y no dispara requests tenant. Cada API recibe el UUID completo obtenido de la sesión.
- Con cero organizaciones se conserva onboarding. Con una se elige de forma determinista. Con varias se requiere selección explícita, salvo que la URL resuelva exactamente una membership.

## Navegación y aislamiento

- Sólo la organización activa puede cargar campos, co-owners e invitaciones. Territory y cuenta son referencias platform/account, pero el heading mantiene claro el contexto elegido.
- Cambiar organización aborta o invalida requests anteriores, desmonta estado tenant y mueve el foco al heading del workspace. Una respuesta tardía nunca repuebla la nueva organización.
- Reload y navegación back/forward restauran únicamente locators válidos. Las entradas propias llevan una posición acotada en `history.state`; si un guard rechaza un `popstate`, el shell compensa con `history.go` sin reescribir ni perder la entrada destino. Membership removida, sesión revocada o URL ambigua limpian el contenido anterior y vuelven a un estado neutral.
- Ningún UUID completo se renderiza en URL, selector, tarjetas, diálogos o mensajes.

## Formularios e intentos ambiguos

- Un borrador no enviado requiere confirmación accesible antes de cambiar de organización.
- Submit, in-progress y reconciliation-required bloquean el cambio. Un resultado ambiguo de remoción de co-owner (`offline`, error o `503`) conserva action/key y bloquea sólo el cambio de organización, sin impedir navegar entre vistas del mismo tenant. Un `429` es rechazo determinístico: limpia ese intento y no inmoviliza el workspace.
- Draft, key y contexto permanecen unidos a la organización del intento; no se trasladan a otra. Al liberar un guard o completar una navegación, el anuncio accesible anterior se limpia y se anuncia el contexto vigente.
- No se guardan datos de negocio en `localStorage` ni se simula autorización offline. `sessionStorage` sólo conserva los intentos one-shot ya aprobados por sus slices.

## UX, accesibilidad y límites

- Dirección visual: mesa de trabajo rural/editorial, reutilizando los tokens y componentes actuales; navegación clara y sobria, sin un rediseño ornamental del producto.
- El shell ofrece skip-link, landmarks, `aria-current`, foco visible, anuncios acotados y reflow sin overflow horizontal a 390 px.
- El gate local cubre Chromium desktop y Pixel 7. Firefox/WebKit, preferencias persistentes, telemetría nueva, navegación por roles no-owner, geometría/mapa, PWA/offline y certificación WCAG manual completa quedan fuera.
- No hay deploy en este incremento.
