# Prototipo web de identidad — AGRO-DIS-003

Aplicación Next.js/React **descartable y local** para recorrer estados de identidad y selección de organización con datos sintéticos. No autentica usuarios, no persiste sesiones y no debe desplegarse.

## Verificación local

```powershell
npm ci --ignore-scripts --no-audit --no-fund
npm run lint
npm run typecheck
npm test
npm run build
npm run test:e2e
```

Para inspección manual después del build:

```powershell
npm run start -- --hostname 127.0.0.1 --port 3020
```

`test:e2e` inicia el build en loopback y recorre los estados en Chromium real con Playwright y axe. Los controles rotulados como simulados permiten recorrer 0/1/N organizaciones, indisponibilidad, linking, recovery, límites y revocación. No existe integración con un IdP ni API productiva.
