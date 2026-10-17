# Preparación y ejecución verificable

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Para el monorepo usar `develop` confirmado S1 y las herramientas/versiones de [TEAM_START histórico con actualización autorizada](../TEAM_START.md). Preparación habitual, sin ejecutar estas operaciones como parte de cambios funcionales de este encargo:

```bash
npm --prefix PWA ci
npm --prefix PWA test
npm --prefix PWA run build
Server/mvnw -B -f Server/pom.xml verify
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
```

Java requiere JDK 21; Node 24 según manifest; abrir Unity/ con Editor 6000.3.23f1 y licencia válida. PlayMode/Player necesitan rutas de runtime preparadas y ambiente compatible; no convertir un test ignorado por falta de dependencias en PASS. No incluir rutas personales, SDK ni credenciales en el repositorio.

La distribución relativa Linux está descrita en [T020](../PHASE0_T020_CLEAN_PC_RUNBOOK.md); la preparación no sustituye ejecución en PC limpia. No ejecutar el juego como root ni abrir firewall amplio por conveniencia.

PWA de laboratorio S10: rama de tarea desde develop en el fork, `npm ci`, `npm test`, `npm run build`, `npm run lab`; abrir http://127.0.0.1:4173/lab.html. Node24. El build genera SW de assets compilados y manifest público. S4 upstream sigue sólo con main y sin esta integración; no trabajar sobre main ni prometer HTTPS móvil desde HTTP local.

Backend S9: rama feature/players-sqlite-increment del PR #14; con JDK21 ejecutar `./mvnw verify` y `java -jar target/backend-0.1.0-SNAPSHOT.jar --gorilla.storage.directory=/ruta/aislada/laboratorio`. En Windows usar mvnw.cmd. Escucha127.0.0.1:8090 por defecto. Los datos SQLite quedan fuera del JAR; el usuario final no instalará SQL por separado. Primera descarga requiere red; ejecución del JAR es local. [Dossier y scripts SQL](https://github.com/Josue1855/Gorilla-Escape-Backend/tree/3635186/docs/laboratorio). El instalador con JRE sigue pendiente.
