# Backend de persistencia: frontera y desconocidos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Repositorio oficial creado y verificado por instrucción posterior del PO, fuente S7: [Gorilla-Escape-Backend](https://github.com/Josue1855/Gorilla-Escape-Backend), privado, `main` `e76371097f4bcd881fa081211ec8d0d78e308c0b`. Su árbol contiene únicamente README.md, docs/ARCHITECTURE.md y docs/DEVELOPMENT_PLAN.md. No hay entidades, migraciones, controllers de dominio, transacciones, tests ni aplicación ejecutable. Estado del desarrollo: PLANIFICADO. Motor, ORM y versiones: DESCONOCIDO.

El `Server/pom.xml` S1 confirma Java 21, Spring Boot 4.1.1, Web MVC/WebSocket, webrtc-java 0.19.0 y ZXing 3.5.3. No contiene JPA, driver SQL ni repositorios de persistencia; sus controllers son health/conectividad. Estas versiones no se atribuyen al futuro backend.

PLANIFICADO por S3: API REST, DTO/validación, persistencia, errores, observabilidad y laboratorio. PROPUESTO: módulos de participantes/sesiones/partidas/resultados, transacciones atómicas, autorización por rol y claves de idempotencia. [Modelo](DATABASE_DESIGN.md) y [contratos candidatos](API_CONTRACTS.md) necesitan revisión backend/Unity antes de implementación.

Para desbloquear implementación y laboratorio: acordar responsables/acceso del equipo y PR de ramas de tarea hacia develop; aprobar stack/motor/versiones; preparar en una tarea funcional separada manifiestos, esquema/migraciones y entorno aislado; ejecutar GET/POST e inspeccionar BD real. No copiar secretos ni deducir credenciales de archivos locales. Mientras falten implementación y ejecución real, no afirmar que el laboratorio extremo a extremo funciona.
