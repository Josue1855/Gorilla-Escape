# Control React

Estado vigente y separación entre proyectos: [índice único](../docs/README.md) y [registro de estado](../docs/00-product/DEVELOPMENT_STATUS.md). Los resultados antiguos de este README conservan su alcance Software/Lab.

React + Vite. Componentes UI en src/; conexiones en src/connection/. El shell Gorilla Core coordina admisión RTC, permisos por gesto y captura real mediante src/input/sensors/capture.js; la vista health sigue disponible con ?view=health. El entry de spikes/sensors reexporta ese mismo módulo. El estado de validación física se registra por Issue, sin compatibilidad universal. Build incorpora recursos propios y service worker sin cachear APIs; no CDN.

Guía: [TEAM_START](../docs/TEAM_START.md). npm ci, npm test y npm run build dentro de PWA. Lockfile obligatorio. npm run dev mantiene host localhost y proxy Java:8080. Android/iPhone requieren pruebas HTTPS reales.

Estado y evidencia consolidada: [auditoría final](../docs/PHASE0_FINAL_AUDIT.md). Gate vigente de captura: [T010](../docs/PHASE0_T010_SENSOR_CAPTURE.md); Android físico NOT RUN y QA pendiente trazada a T019.

La [PWA de laboratorio del fork UI/UX](https://github.com/Josue1855/Dise-o_pwa/pull/1) ya integra jugadores GET/POST/SQLite; no sustituye este cliente RTC. Sus tests siete y offline IndexedDB son distintos de los tests técnicos de este monorepo. [Estado del laboratorio](../docs/04-testing/PWA_TEST_LAB.md).
