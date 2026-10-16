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

PWA independiente S4: rama `main`, `npm ci`, `npm run dev` o `npm run build`. S5 ejecutó instalación reproducible con scripts de instalación desactivados en copia temporal y build PASS. No tiene script test; su build no empaqueta el SW. No prometer HTTPS móvil desde un servidor HTTP local por la mera existencia de Vite.

Backend separado S7: repositorio creado con documentación, sin manifiesto ni script de arranque. Preparación ejecutable BLOQUEADO hasta implementar stack/motor y entorno de datos ficticios aprobados. No ofrecer comandos Spring/JPA inventados.
