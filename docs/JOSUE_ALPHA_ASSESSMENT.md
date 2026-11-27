# Evaluación del módulo alpha

El prototipo está aislado en `Prototypes/JosueAlpha` para revisión. No sustituye los módulos oficiales y no debe fusionarse completo sin una decisión del equipo.

## Hallazgos

1. **Hay dos arquitecturas en paralelo.** El alpha usa un módulo Python local integrado, administrado por Unity y publicado opcionalmente con Cloudflare. La arquitectura acordada usa Unity + Java/Spring Boot + React/WebSocket y busca funcionar sin Internet durante gameplay.
2. **El alpha contiene valor de producto adelantado.** Ya existen escenas y controladores para conexión, menú, boliche, boxeo, béisbol y tambores. Conviene evaluarlos como vertical slices y migrarlos por partes, no reemplazar la base técnica de una sola vez.
3. **La versión de Unity no coincide.** El alpha usa 6000.6.0f1 y la base del equipo fija 6000.3.23f1. El equipo debe elegir una versión y registrar la decisión antes de mover escenas o paquetes.
4. **Los contratos de entrada son el punto de unión.** `MotionSession`, los paquetes de pose y movimiento, y `PlayerInput` deben converger en contratos compartidos. Los minijuegos no deberían depender directamente de HTTP, Cloudflare, MediaPipe o del teléfono.
5. **Cloudflare sirve para demos remotas, no como requisito final.** Es útil para probar sensores HTTPS con teléfonos, pero contradice el objetivo offline si se convierte en dependencia obligatoria.
6. **El empaquetado todavía requiere validación externa.** Unity prepara dependencias y administra procesos sin consolas, pero aún hace falta validar una instalación limpia y producir un Player de Windows.

## Camino recomendado

1. Ejecutar este alpha en una segunda PC con el procedimiento documentado y registrar versión de Windows, webcam, teléfono, navegador, FPS y latencia.
2. Resolver formalmente la versión única de Unity mediante una entrada en `docs/DECISIONS.md`.
3. Congelar un contrato JSON versionado para `PhoneInput`, `CameraInput` y `PlayerInput`; añadir fixtures y pruebas en Shared, Java, PWA y Unity.
4. Migrar primero la experiencia de conexión y un único vertical slice jugable. Banana Bowling es el mejor candidato porque ya reúne input, física, scoring, feedback y reinicio.
5. Sustituir gradualmente el transporte HTTP integrado por el host Java/WebSocket. Mantener Cloudflare detrás de un adaptador opcional para demos, con ruta LAN/local como comportamiento principal.
6. Encapsular Camera Tracker como proceso local con health check, inicio/cierre controlado y errores recuperables; Unity debe consumir solo observaciones normalizadas.
7. Después de estabilizar conexión + un juego, extraer sistemas reutilizables para calibración, turnos, puntuación, telemetría local y reconexión. Luego migrar los demás minijuegos uno por uno.

## Criterio para integrar

No integrar la carpeta alpha completa. Crear PR pequeños hacia `develop`, cada uno con un contrato o capacidad, pruebas aplicables y evidencia en hardware. Conservar esta rama como referencia hasta que cada pieza útil haya sido migrada o descartada explícitamente.
