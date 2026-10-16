# Backend de persistencia: frontera y desconocidos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Repositorio, branch, commit, stack real y motor: **DESCONOCIDO / NO VERIFICADO**. El PO no conoce el enlace. No se inspeccionaron entidades, migraciones, controllers de dominio, transacciones ni tests de un backend separado.

El `Server/pom.xml` S1 confirma Java 21, Spring Boot 4.1.1, Web MVC/WebSocket, webrtc-java 0.19.0 y ZXing 3.5.3. No contiene JPA, driver SQL ni repositorios de persistencia; sus controllers son health/conectividad. Estas versiones no se atribuyen al futuro backend.

PLANIFICADO por S3: API REST, DTO/validación, persistencia, errores, observabilidad y laboratorio. PROPUESTO: módulos de participantes/sesiones/partidas/resultados, transacciones atómicas, autorización por rol y claves de idempotencia. [Modelo](DATABASE_DESIGN.md) y [contratos candidatos](API_CONTRACTS.md) necesitan revisión backend/Unity antes de implementación.

Para desbloquear: proporcionar URL oficial, permiso de lectura, rama de integración y SHA; inventariar build/manifest/configuración sanitizada; confirmar motor/versión, esquema/migraciones y entorno de pruebas aislado; ejecutar GET/POST e inspeccionar BD real. No copiar secretos ni deducir credenciales de archivos locales. Mientras falte, no afirmar que el laboratorio extremo a extremo funciona.
