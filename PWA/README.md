# Control React

React + Vite. Componentes UI en src/; conexiones en src/connection/. El shell Gorilla Core coordina admisión RTC, permisos por gesto y captura real mediante src/input/sensors/capture.js; la vista health sigue disponible con ?view=health. El entry de spikes/sensors reexporta ese mismo módulo. El estado de validación física se registra por Issue, sin compatibilidad universal. Build incorpora recursos propios y service worker sin cachear APIs; no CDN.

Guía: [TEAM_START](../docs/TEAM_START.md). npm ci, npm test y npm run build dentro de PWA. Lockfile obligatorio. npm run dev mantiene host localhost y proxy Java:8080. Android/iPhone requieren pruebas HTTPS reales.

Estado y evidencia consolidada: [auditoría final](../docs/PHASE0_FINAL_AUDIT.md). Gate vigente de captura: [T010](../docs/PHASE0_T010_SENSOR_CAPTURE.md); Android físico NOT RUN y QA pendiente trazada a T019.
