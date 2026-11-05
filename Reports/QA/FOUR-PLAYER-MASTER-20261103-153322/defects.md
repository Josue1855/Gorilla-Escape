# Registro de Defectos Detectados
Laboratorio: FOUR-PLAYER-MASTER-20261103-153322

| ID | Severidad | Descripción | Causa Raíz | Estado |
|---|---|---|---|---|
| DEF-NAR-01 | P1 | Narrativa de cómic mencionaba 'LAS 4 PRUEBAS MVP' contradiciendo el catálogo de 10 disciplinas. | Texto legacy en Viñeta 5 de `SplashDirector.cs`. | CORREGIDO |
| DEF-AVD-01 | P0 | Emuladores Android abortaban al arrancar por carpetas stale `.lock` de ejecuciones anteriores. | Cierres abruptos dejaron `hardware-qemu.ini.lock`. | CORREGIDO |
