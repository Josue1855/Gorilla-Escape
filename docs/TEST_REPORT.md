# Reporte de pruebas

## BASE-001 — Base compartida

**Fecha:** 2026-10-05.
**Alcance:** fuentes Unity, servidor local Java y PWA React; no completa Spike ni gameplay.
**Build:** React/Vite y Maven verify con Java 21.
**Tests automatizados:** cuatro web y cuatro Java (fixture Jackson/DTO, HTTP health, frontend empaquetado y 404 de API). Validación documental, backlog y metadatos/UPM mediante scripts versionados. Resultado de la copia limpia: React build PASS; Maven verify PASS; 4 tests web y 4 Java PASS; validación de foundation y gestión documental PASS. CI remoto y auditoría de Project se verifican después de la publicación.
**Validación de navegador anterior:** Chrome escritorio; React contacta Java y permite reintentar después de detener el servidor; shell disponible mediante service worker. No acredita instalación móvil ni gameplay offline.
**Unity:** editor 6000.3.23f1 instalado; creación batch bloqueada por licencia. Fuentes y tests EditMode/PlayMode preparados, no compilados ni ejecutados.
**Warnings conocidos:** instrumentación dinámica de Mockito en pruebas Java; advertencia de futuras restricciones de JVM.
**Fuente:** especificación v4 enmendada por DEC-002; SHA-256 63e0378f0785aa4e48e0a79e7ab28e7dcd342eb3e42ec50bcb61712f3f6b8a06.

## Validación manual pendiente

1. Con licencia válida, importar Unity, generar/revisar escena, settings y lockfile; ejecutar EditMode/PlayMode y construir Player PC.
2. Probar instalación y actualización de PWA en Android/iPhone con HTTPS confiable.
3. En la tarea correspondiente del Spike, instrumentar y medir sensores, webcam, Player Lock, calibración y latencia P95; registrar hardware y resultados reales.

No declarar métricas ni aceptación de hardware hasta ejecutar estos procedimientos.

## PUB-001 — Verificación de la base publicada

**Fecha:** 2026-10-06.
**Build y tests locales:** React build y Maven verify con Java 21 PASS; cuatro tests web y cuatro Java PASS. Validación de foundation PASS (34 GUIDs únicos); manifiesto/documentación PASS (88 registros, 51 enlaces locales); diff sin errores de whitespace.
**Publicación:** main y develop comparten la base inicial. Estado de CI disponible en GitHub Actions; auditoría de backlog, relaciones y protecciones en docs/github/verification.json.
**Limitaciones:** no se ejecutaron nuevas pruebas físicas. Unity sigue pendiente de licencia, importación, tests y build; ningún resultado local acredita el Spike completo.
