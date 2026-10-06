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

1. Definir PC oficial y validar la foundation en ese hardware. Validación Linux de desarrollo realizada en UNITY-001, abajo.
2. Probar instalación y actualización de PWA en Android/iPhone con HTTPS confiable.
3. En la tarea correspondiente del Spike, instrumentar y medir sensores, webcam, Player Lock, calibración y latencia P95; registrar hardware y resultados reales.

No declarar métricas ni aceptación de hardware hasta ejecutar estos procedimientos.

## PUB-001 — Verificación de la base publicada

**Fecha:** 2026-10-06.
**Build y tests locales:** React build y Maven verify con Java 21 PASS; cuatro tests web y cuatro Java PASS. Validación de foundation PASS (34 GUIDs únicos); manifiesto/documentación PASS (88 registros, 51 enlaces locales); diff sin errores de whitespace.
**Publicación:** main y develop comparten la base inicial. Estado de CI disponible en GitHub Actions; auditoría de backlog, relaciones y protecciones en docs/github/verification.json.
**Limitaciones:** no se ejecutaron nuevas pruebas físicas. Unity sigue pendiente de licencia, importación, tests y build; ningún resultado local acredita el Spike completo.

## UNITY-001A — Intento limpio y preparación de validación

**Fecha:** 2026-10-06. **Base:** develop e950bd8. **Alcance:** Incremento 1A, exclusivamente foundation y documentación; no cierra Incremento 1.
**Implementado:** inventario inspeccionado, intento limpio y procedimiento de importación, assets/lockfile, EditMode/PlayMode, build mínimo y cierre de Player en [PHASE0_UNITY_FOUNDATION](PHASE0_UNITY_FOUNDATION.md). Discrepancias de alcance, autoridad, IPC y protocolo identificadas como propuestas pendientes; sin nuevas decisiones aceptadas.
**Hardware:** PC de desarrollo Linux x86_64, kernel 7.1.5-76070105-generic; Unity 6000.3.23f1 (09d2ecc7fb28), módulos de PC instalados. PC oficial de presentación no confirmada.
**Importación:** copia nueva de Assets/Packages/ProjectSettings y Shared, sin Library previa. Comando exacto: `$UNITY_EDITOR -batchmode -nographics -quit -projectPath /tmp/gorilla-unity-clean-hat2_zjh/Unity -logFile /tmp/gorilla-unity-clean-hat2_zjh/import.log`. Código 198; `No valid Unity Editor license found. Please activate your license.` Resultado **BLOCKED**, no FAIL de código ni PASS de importación. Log local temporal; extracto sanitizado preservado en la guía.
**Build:** **NOT RUN** por dependencia de importación/licencia; comando preparado en la guía, no ejecutado.
**Tests Unity:** **NOT RUN** para EditMode y PlayMode; no XML ni conteos inventados.
**Checks ejecutados:** `python3 tools/validate_foundation.py` PASS (34 GUIDs inicialmente; 35 tras .meta generado externamente); `python3 tools/github/validate_management.py` PASS (88 registros, 53 enlaces locales válidos tras documentación); `git diff --check` PASS. Son checks estáticos/documentales, no tests Unity. Java/React no se repitieron: sin cambios en sus fuentes ni contratos.
**Cambio concurrente:** editor iniciado vía Hub sobre el checkout generó ProjectSettings/lockfile/.meta y modificó manifest/ProjectVersion. Se conservan sin aprobación ni integración. Lockfile Test Framework 1.6.0 frente a manifest 1.4.2; nuevos sdk/toolchain Linux 1.1.0 requieren explicación/revisión. No atribuirlos al cambio documental ni declarar importación completa. Segundo intento batch en la copia temporal: salida 198, mismo mensaje; log import-retry.log. Verificar entorno/licencia batch o ejecutar desde sesión válida del editor; no inferir el estado de licencia del Hub.
**Warnings:** arranque Unity informa licencia ausente; compilación, warnings de código y build no evaluables hasta importar. No se afirma ausencia de warnings del producto.
**Seguridad:** no dependencias, red de producto, sensores, cámara, CORS ni certificados añadidos. Log completo no versionado; extracto omite identificadores locales de licencia/máquina y red. Seguridad de gameplay **NOT RUN**.
**Rendimiento:** FPS, frame time, Hz, calibración, P50/P95/máximo de latencia, descartes/duplicados/antiguos/reconexiones **NOT RUN**. No se midieron sesiones ni Player Lock.
**Validación manual requerida:** activar licencia por el titular; ejecutar pasos 1–8 de la guía en copia limpia y PC con gráficos, revisar assets/lockfile, XML, build y tres aperturas/cierres normales. Identificar PC oficial y repetir allí. Teléfono/webcam/router no necesarios para 1A; sus pruebas siguen pendientes.
**Riesgos/deuda:** licencia bloqueante, compilación desconocida, hardware oficial sin identificar y acuerdos de Incremento 2 pendientes. **Aceptación:** inventario/copia limpia y checks estáticos PASS; import/paquetes BLOCKED; assets/tests/build/Player NOT RUN; sensores/cámara/red NOT APPLICABLE a 1A. **Siguiente incremento:** continuar Incremento 1 con licencia válida, sin ejecutar Incremento 2 o Fase 1 automáticamente.

## UNITY-001A-AUDIT — Reproducibilidad de archivos generados

**Fecha:** 2026-10-06. **Base exacta:** e950bd820ea2dd6ede265943a3b7702f445be467. **Rama:** feature/gameplay-phase0-unity-foundation-validation. **Alcance:** diagnóstico estático y documentación; no nuevos arranques, pruebas Unity ni cambios de versiones/archivos técnicos. Auditoría completa archivo por archivo en [PHASE0_UNITY_FOUNDATION](PHASE0_UNITY_FOUNDATION.md).
**Evidencia:** Test Framework directo depth 0/source builtin resuelto 1.6.0; editor instalado declara minimumVersion 1.6.0/mustBeBundled true y contiene ese paquete core. NUnit transitivo depth 1 resuelve core 2.0.5. Manifest actual y HEAD siguen declarando 1.4.2. SDK Linux y toolchain de host Linux directos 1.1.0 requieren sysroot.base transitiva 1.1.0; peticiones UPM add-dependency observadas, caller original no identificado.
**Archivos:** dos técnicos versionados modificados y 21 nuevos (19 settings, lockfile, .meta), preservados sin incorporar. Caches/estado de UI y ciertos identificadores generados no permiten asumir igualdad byte a byte o configuración oficial. Scene list sigue vacía.
**Licencia:** sesión interactiva Hub/Flatpak con entitlement resuelto; entorno batch distinto, últimos dos intentos 198. No se considera resuelto el bloqueo ni se copian tokens/licencias.
**Build/tests/rendimiento:** batch startup y clean import BLOCKED; EditMode/PlayMode, PC Build, Player execution, Performance y Windows NOT RUN. No hay nuevas métricas ni tests Unity.
**Seguridad:** inspección de procesos con autenticación sanitizada; logs completos y sus valores privados no incorporados; sin red de producto, cámara o sensores nuevos.
**Checks ejecutados:** `python3 tools/validate_foundation.py` PASS (35 GUIDs únicos); `python3 tools/github/validate_management.py` PASS (88 registros, 54 enlaces locales); revisión de whitespace PASS. Contraste SHA-256 de 78 archivos Unity/Shared entre inicio y cierre: PASS, cero cambios/eliminaciones. Escaneo de Packages/ProjectSettings: sin rutas absolutas del host detectadas. Estos checks no acreditan importación limpia.
**Recomendación:** C, corregir configuración antes de validar; propuesta de core 1.6.0 con el editor fijado y selección explícita de plataforma/backend/paquetes de host, pendiente de aprobación. Sin cambio de decisión, commit ni PR. **Siguiente paso único:** acordar configuración de foundation antes de modificar versión o ejecutar validación completa.

## UNITY-001 — Foundation de desarrollo

**Fecha:** 2026-10-06. **Rama:** feature/gameplay-phase0-unity-foundation-validation.
**SHA base:** e950bd820ea2dd6ede265943a3b7702f445be467; base de las ejecuciones previas al commit de cierre. El [inventario SHA-256](evidence/unity-foundation-2026-10-06/source-sha256.json) de 76 archivos identifica el contenido exacto validado; el cierre compara el contenido funcional exacto del commit con ese inventario.
**Editor / OS:** Unity 6000.3.23f1 (09d2ecc7fb28); Pop!_OS 24.04 LTS, Linux x86_64, kernel 7.1.5-76070105-generic. Player OpenGL: Mesa Intel Iris Xe Graphics (RPL-U), Mesa 26.1.6. PC oficial de presentación pendiente.
**Configuración:** Test Framework 1.6.0 core, NUnit 2.0.5 transitivo, IMGUI 1.0.0 transitivo, jsonserialize 1.0.0 directo y paquete local de contratos. Cinco paquetes en lockfile UPM; sin SDK/toolchain/sysroot Linux. Mono/Development sólo para validación local. DEC-003 y [guía vigente](UNITY_FOUNDATION_VALIDATION.md).
**Procedimiento / comandos exactos / horarios UTC:** [runs.json](evidence/unity-foundation-2026-10-06/runs.json). Launcher Flatpak del Hub, con opciones locales del editor que desactivan instalación y migración automática de toolchains. No tokens/credenciales copiados ni elusión de licencias. El comando directo histórico con salida 198 permanece BLOCKED.

| Orden / criterio | Resultado | Exit | Tests / artefacto esperado / evidencia |
|---|---|---|---|
| Clean import / compilación | PASS | 0 | Copia fuente nueva sin Library; runtime assembly generado; [extracto](evidence/unity-foundation-2026-10-06/import.log) |
| Resolución de paquetes | PASS | 0 | Cinco paquetes; manifest/lock idénticos al candidato, sin dependencias Linux |
| Escena / settings persistidos | PASS | 0 | Un Bootstrap, script/referencia correctos, 60 configurados, Build Settings y cero scripts faltantes; caso real EditMode |
| EditMode | PASS | 0 | 2 ejecutados, 2 Passed, 0 Failed/Skipped; [XML](evidence/unity-foundation-2026-10-06/editmode.xml), [log](evidence/unity-foundation-2026-10-06/editmode.log) |
| PlayMode | PASS | 0 | 1 ejecutado, 1 Passed, 0 Failed/Skipped; [XML](evidence/unity-foundation-2026-10-06/playmode.xml), [log](evidence/unity-foundation-2026-10-06/playmode.log) |
| Linux development build | PASS | 0 | BuildReport Succeeded, 0 errores/0 warnings de build; launcher ELF + UnityPlayer.so + datos/assembly; [hashes](evidence/unity-foundation-2026-10-06/build-artifacts.json), [log](evidence/unity-foundation-2026-10-06/build.log) |
| Player con gráficos | PASS | 0 al cerrar | Ventana observada, renderer OpenGL real y foundation ready; [log](evidence/unity-foundation-2026-10-06/player.log) |
| Player clean shutdown | PASS | 0 | WM_DELETE_WINDOW normal a ventana del Player, sin kill; foundation shutdown, cleanup de física/input y fin del proceso |
| Clean re-import del checkout final | PASS | 0 | Segunda copia sin Library/Temp/UserSettings: 76/76 archivos fuente idénticos, mismo manifest/lock/GUIDs/settings; drift=[]; [log](evidence/unity-foundation-2026-10-06/reimport.log) |
| Errores de código/proyecto en tests/build/Player | PASS | 0 | Sin errores de compilación ni tests fallidos ni excepción del Player |
| Diagnósticos conocidos del entorno | ENVIRONMENT / REQUIRES FOLLOW-UP | 0 | Deuda #89; no son fallo demostrado de Gorilla Escape ni puerta artificial de integración |
| Presentation PC | BLOCKED / NOT RUN | — | Hardware/plataforma oficial aún no definidos; Linux development PASS no acredita presentación |
| FPS real/frame time/latencia/memoria/leaks/sensores/webcam/Player Lock | NOT RUN | — | No medidos ni inferidos desde foundation; gameplay offline/integrado tampoco acreditado |

**Warnings y diagnósticos reales:** el log del editor etiqueta `[Licensing::Module] Error: Access token is unavailable; failed to update`, seguido de entitlement resuelto. La ejecución batch sí ocurre y produce artefactos; el diagnóstico no se oculta ni demuestra que el launcher directo del host esté arreglado. El cleanup del editor ejecuta build-server sobre un runtime sin SDK y comunica `No .NET SDKs were found`; debugger-agent no logra escuchar en un descriptor. No son errores de compilación del proyecto, pero impiden afirmar un log totalmente limpio. El Player de desarrollo emite registro nativo `MemoryLeaks`, allocatedMemory **68646 bytes** al salir; no hay crash/excepción, pero no se acredita cero fugas ni se atribuye sin análisis al código del proyecto. BuildReport 0 warnings no significa ausencia de estos diagnósticos de entorno.

**Preparación conservada:** [preparation-runs.json](evidence/unity-foundation-2026-10-06/preparation-runs.json) registra UPM AddAndRemove Success y placeholder determinista mediante PlayerSettings.PS4.passcode, con editor/SHA/OS/comandos/exit/log/hashes. Helpers locales excluidos de las fuentes finales; NUnit no añadido directamente. [Retirada UPM](evidence/unity-foundation-2026-10-06/remove-linux.log) y [setting de consola](evidence/unity-foundation-2026-10-06/console-placeholder.log).

**Exploración / fallos anteriores:** se observaron salidas 0 en importación vía Hub e inspecciones del editor; retirada UPM con callbacks interrumpidos por reload terminó siendo detenida (143); un build fue rechazado con 1 por proyecto aún abierto. La prueba inicial de assets ejecutó 2 casos, uno falló (exit 2) por settings=fileID 0; se corrigió NewScene antes de cargar settings. La casilla desactivada no impidió migración batch. Una prevalidación pasó suites/build pero el control de fuente detectó que Unity regeneraba el passcode de consola vacío; no se aceptó como reproducción idéntica. La sesión original reintrodujo paquetes y el control de cinco dependencias rechazó esa copia. Los logs/XML exploratorios bajo Unity/Temp fueron eliminados por el cleanup normal al cerrar ese editor; evidencia no conservada, no sostiene ningún PASS final. Se repitió íntegramente la secuencia con logs persistentes y configuración final. La prevalidación anterior al placeholder se conserva localmente en Unity/Logs/FoundationValidation/2026-10-06/evidence/pre-placeholder, sin sustituir los resultados definitivos.

**Conservación:** logs completos locales bajo Unity/Logs/FoundationValidation/2026-10-06/evidence; hashes de raw logs/XML en runs.json. docs/evidence contiene extractos revisados y XML normalizados ($REPO sustituye ruta local); no son logs completos. Build utilizable local en Unity/Builds/FoundationLinux, ignorado. Se conservó sólo configuración compartida necesaria, con revisión individual en la guía; PackageManagerSettings y otros defaults locales/para sistemas no usados están ignorados. Git/editorconfig preservan whitespace de serialización Unity, sin reescribir assets generados.

**Cierre autorizado:** Unity Foundation / Development environment **PASS**. El PO acepta integrar la foundation funcional sin convertir un log vacío en requisito adicional. Los diagnósticos quedan **ENVIRONMENT / REQUIRES FOLLOW-UP**, deuda [#89](https://github.com/Josue1855/Gorilla-Escape/issues/89), sin evidencia de error del código Gorilla Escape. Presentation PC **BLOCKED / NOT RUN**; rendimiento físico **NOT RUN**; Fase 0 **IN PROGRESS**; Incremento 2/Fase 1 **NOT STARTED**. Se prepara commit y PR a develop para revisión normal; no merge automático. Próximo paso recomendado, no ejecutado: definir PC/plataforma oficial y su aceptación de foundation.

**Verificación final del checkout:** nueva sesión concurrente agregó paquetes AI y toolchains después del re-import. Se preservaron sus manifest/lock/settings en el respaldo local concurrent-root-backup, se cerró normalmente y se restauró el par manifest/lock y ProjectSettings (incluido retiro del define de analytics introducido por AI) exactamente desde el resultado UPM validado. Los 76 hashes del checkout vuelven a coincidir con la ejecución definitiva; los settings AI se clasifican locales/rechazados. Diff completo revisado mediante índice temporal, sin cambiar el staging del usuario. Validación estática PASS: 38 GUIDs únicos; gestión documental PASS: 88 registros y enlaces locales válidos; sin errores de whitespace en el candidato completo.

## UNITY-001-CLOSE — Revisión final para commit y PR

**Fecha:** 2026-10-06. **Alcance:** cierre autorizado de foundation de desarrollo; documentación/evidencia sanitizada, sin cambios funcionales después de UNITY-001.
**Candidato exacto:** cotejo SHA-256 de los 76 archivos Unity/Shared contra el inventario ejecutado PASS. Configuración Unity 6000.3.23f1, Test Framework 1.6.0; NUnit sólo transitivo; cinco paquetes UPM; cero AI/toolchains Linux compartidos. Escena/settings/GUIDs/referencias verificados; 60 configurados, servicios cloud deshabilitados. Helpers, caches, backups, builds, estado UI/AI y logs raw excluidos. Historial 1A conservado.
**Checks finales:** python3 tools/validate_foundation.py PASS (38 GUIDs únicos); python3 tools/github/validate_management.py PASS (88 registros/enlaces válidos); git diff --cached --check PASS sobre todos los archivos del commit; sin secretos detectados. No se repite Unity porque el contenido funcional coincide exactamente con el ejecutado; las métricas físicas permanecen NOT RUN.
**Checks de regresión del flujo del repo:** npm --prefix PWA test PASS (4/4), npm --prefix PWA run build PASS con Node 24.19.0; JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 Server/mvnw -B -f Server/pom.xml verify PASS (4/4 tests, BUILD SUCCESS), OpenJDK 21.0.12.1, Maven 3.9.11. Primera invocación con JDK por defecto 25 también pasó 4/4, pero no sustituye la verificación explícita con JDK 21. Sin cambios Java/React. Warning de instrumentación dinámica Mockito/JVM existente; no modifica su clasificación ni configura nuevos flags.
**Publicación:** un commit coherente feat(unity): validate phase 0 foundation en la rama solicitada; PR a develop para revisión normal/checks. La revisión/integración queda pendiente de GitHub, sin merge automático ni cambio de protecciones. Deuda de entorno #89 abierta. Development PASS; Presentation PC BLOCKED / NOT RUN; rendimiento físico NOT RUN; Fase 0 IN PROGRESS; Incremento 2/Fase 1 NOT STARTED.


## IPC-002A — Unity ↔ Java mínimo real

2026-10-06. Base develop `2a635b607a286bb70d1457aa0cf34f7e85f17abe`, merge normal #90; commit validado ancestro y 76/76 hashes foundation idénticos. Protección efectiva clásica: approvals 1→0, única diferencia autorizada; PR y ambos checks/prohibiciones conservados, sin rulesets efectivos. Checks del merge develop PASS.

Implementación 2A opt-in: Unity inicia JAR Spring/Java21, configura loopback y puerto0, valida READY y primer PONG, intercambia frames uint32 big-endian UTF-8 <=4096, token efímero/identidades/sequence; workers con deadlines/cancellation y buffers acotados, shutdown por stdin EOF y exit observado. No crea estado competitivo Java ni modifica PWA móvil/gameplay.

**Build/tests:** Java21 Maven verify 8/8 PASS (incluye hijo JVM real, frames fragmentados, stdin EOF/exit0), React regresión 4/4 y build PASS sin cambios de fuentes, EditMode 7/7 y PlayMode 3/3 PASS con JVM real. Linux Mono development Player build PASS, 0 errores/0 advertencias. Player con gráficos/OpenGL y foundation shutdown, salida Unity0/Java0, listeners efectivos sólo127.0.0.1 y ningún Java propio residual al terminar. Error de ejecutable ausente cubierto como recuperable en PlayMode.

| Smoke (100 válidos después de 10 warmup) | Startup ms | RTT mínimo ms | P50 ms | P95 ms | Máximo ms | Errores |
|---|---:|---:|---:|---:|---:|---:|
| PlayMode final | 2278.170 | 0.404 | 0.567 | 0.718 | 1.988 | 0 |
| Player Linux final | 2353.197 | 0.586 | 0.795 | 1.441 | 1.889 | 0 |

Medición monotónica Unity worker, incluye codec/TCP ida y vuelta/Java; cadencia suave nominal20ms entre respuestas, no certificación50Hz. No incluye gameplay/frame, teléfono/LAN/sensores. No extrapolar a P95 end-to-end de v4 ni afirmar memoria/leaks/FPS físicos aprobados.

Evidencia sanitizada [validation.json](evidence/unity-java-ipc-2a-2026-10-06/validation.json), XML EditMode/PlayMode, extractos build/Player/PlayMode/Maven, listeners reales/exit y hashes JAR/Player en [player-result.json](evidence/unity-java-ipc-2a-2026-10-06/player-result.json), inventario de fuentes y cambio de protección antes/después. Raw logs, JDK copiado, snapshot, binaries y helpers locales ignorados. Primer intento fallido por symlinks JDK dentro de Flatpak y correcciones de medición/warning documentados en validation.json; pruebas afectadas repetidas sobre candidato final.

**Gate 2A desarrollo:** requisitos funcionales PASS; revisión/integración del PR de 2A pendiente. PC oficial y Windows/backend final NOT RUN. Resiliencia avanzada, reconnect/restart/watchdog, Job Objects, flood/fault injection extensa y benchmark exhaustivo diferidos a autorización posterior, no eliminados de DEC-005. Parser Unity tiene validación básica; validación estricta completa/fuzzing pendientes de hardening. Diagnósticos editor conocidos siguen ENVIRONMENT / REQUIRES FOLLOW-UP #89. Sin 2B ni Fase1.
