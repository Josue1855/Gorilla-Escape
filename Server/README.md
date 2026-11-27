# Servidor local Java

Estado vigente y separación entre proyectos: [índice único](../docs/README.md) y [registro de estado](../docs/00-product/DEVELOPMENT_STATUS.md). Los resultados antiguos de este README conservan su alcance Software/Lab.

Spring Boot Web MVC, JDK 21. Hosting/health y DTOs de protocolo; no base de datos, cloud ni gameplay. La salida PWA/dist se incorpora al JAR en build; ejecutar primero el build web.

Guía: [TEAM_START](../docs/TEAM_START.md). Tests unitarios de contrato e integración HTTP real; publicación frontend y recuperación 404. IPC supervisado loopback, sesión/admission/QR técnico, Gorilla Protocol y WebRTC DataChannel están implementados y validados Software/Lab. No calcula gameplay ni scoring. Hay candidato Linux con runtime relativo; ejecución en PC limpia y aceptación física final siguen pendientes. [Auditoría final](../docs/PHASE0_FINAL_AUDIT.md).

La persistencia de jugadores vive en [Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend/pull/14): SQLite/JDBC implementado y probado, fuera de este servicio técnico. Su empaquetado y supervisión dentro del videojuego aún no están implementados.
