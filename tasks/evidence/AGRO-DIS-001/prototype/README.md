# Prototipo React de Catálogo Nacional v1

Artefacto R0 aislado y descartable de `AGRO-DIS-001`. Valida búsqueda por código, nombre y alias; separación de niveles de soporte; responsive; teclado/foco; y estados loading, vacío, error y fuente desactualizada. No es el frontend productivo, no incluye autenticación, no persiste datos y no debe reutilizarse como bootstrap sin una tarea y revisión explícitas.

Las versiones se fijaron el 2026-08-04 contra el registro npm y la [guía oficial de instalación de Next.js](https://nextjs.org/docs/app/getting-started/installation): Next.js 16.3.0, React 19.2.8, TypeScript 5.9.3 y ESLint 9.39.5. Se fijan TypeScript 5.9 y ESLint 9 por compatibilidad declarada de los parsers/plugins incluidos en `eslint-config-next` 16.3.0. Node.js local 22.19.0 satisface el mínimo 20.9.

## Ejecución

```powershell
npm ci
npm run lint
npm run typecheck
npm test
npm run build
npm run dev
```

Abrir `http://localhost:3000`. El control “Escenario de fuente” permite reproducir estados normal, stale, error y loading; una búsqueda sin coincidencias demuestra el estado vacío.

`next-env.d.ts` es generado por Next.js 16 y alterna referencias internas entre `next dev` y `next build`; el validador comprueba su forma cuando existe, pero lo excluye del manifiesto SHA-256. Los 23 archivos fuente/config restantes sí deben coincidir exactamente. El orden de cierre reproducible es `npm run build` y luego el validador del catálogo.
