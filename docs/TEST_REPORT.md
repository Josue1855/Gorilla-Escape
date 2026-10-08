# Reporte de pruebas

> **Fuente operativa vigente — 2026-10-08:** GitHub Project/Issues/dependencias/AC. Checkpoint técnico Fase0 **PASS — SOFTWARE/LAB**, conservado; **Project Fase0/E0/milestone siguen OPEN**. Fase1 **BLOCKED / NOT STARTED** por #35→#34; los estados READY del audit anterior no autorizan iniciar según Project. [Reconciliación vigente](PHASE0_PROJECT_RECONCILIATION.md).

## Estado vigente — FINAL PHASE0 AUDIT, 2026-10-08

**PHASE 0 FINAL AUDIT: PASS — SOFTWARE/LAB.** Fase0 — Base/Spike: **COMPLETED — SOFTWARE/LAB**. Fase1 — Gorilla Smash Vertical Slice: **READY TO START**, no iniciada. Physical validation debt remains open and is required before final MVP acceptance. 3A product/physical onboarding: **DEFERRED / IN PROGRESS**. #94 **OPEN/DRAFT**; candidato local sin commit distinto del PR remoto. Sin nuevo código de producto, commit/push/merge ni fase posterior. [Audit y DoD](PHASE0_FINAL_AUDIT.md), [evidencia](evidence/phase0-final-audit-2026-10-08/gate-summary.json). Los estados previos siguientes son snapshots históricos.

> **Lectura de estado (auditoría 2026-10-08):** las fechas, estados y próximos pasos de este documento son snapshots históricos del incremento descrito. El estado consolidado vigente se registra en [PHASE0_FINAL_AUDIT.md](PHASE0_FINAL_AUDIT.md). Las decisiones de transporte anteriores están **superseded / replaced by DEC-010 Mobile Transport**; se conservan resultados y limitaciones originales.

## Criterio vigente de cierre Software/Lab — aprobado por PO, 2026-10-07

Fase 0 puede cerrar como **COMPLETED — SOFTWARE/LAB** únicamente cuando arquitectura base implementada, integrada y todos los gates automatizados/laboratorio requeridos estén PASS. Esta autorización no equivale a declarar el cierre ahora. iPhone 15/Safari y Android físicos, cadencia móvil real, movimiento humano con teléfono/cámara y latencia física quedan **NOT RUN / DEFERRED**, obligatorios antes de aceptación final del MVP. Windows: DEFERRED. Los intentos fallidos de acceso iPhone y toda evidencia anterior se conservan; no se convierten en PASS. No se vuelve a solicitar teléfono para avanzar.

Orden obligatorio: A comparador WebRTC aislado → B DEC-010 y un transporte → C sesión/admission/QR POC → D protocolo común → E input móvil lab/Java/IPC/Unity → F webcam PC/pose/Unity → G asociación y temporal → H infraestructura Fusion/fixtures → I 1–4 clientes → J resiliencia/regresión → K auditoría → L cierre. No gameplay final, reconocimiento facial, cámara móvil, backend cloud ni Fase 1 antes del cierre. PR #94 permanece DRAFT. Sin push/merge autorizado para este trabajo. Secure-context/onboarding sin configuración del jugador sigue como riesgo independiente; resultados lab no demuestran compatibilidad física.

**Estado actual:** cierre IN PROGRESS; A PASS (comparator lab); B DEC-010 selecciona RTC; C PARTIAL (modelo y unit tests); D–L NOT RUN en esta secuencia. Foundation/IPC y resultados de laboratorio históricos conservados. Fase 1 NOT STARTED; sólo será READY TO START después de auditoría PASS e integración requerida.


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

**Gate 2A desarrollo:** **PASS / MERGED**. El PO aprobó integración; [PR #91](https://github.com/Josue1855/Gorilla-Escape/pull/91) se integró mediante merge normal en develop `9fbb566e5f24af8f4e92fa3c139968161e832d46`, que contiene el commit validado `a17dfb2968f79e35d479a6912e40d3b5d55538bd` sin cambios de contenido. No se repiten ni alteran los resultados medidos por este cierre documental. PC oficial y Windows/backend final NOT RUN. Resiliencia avanzada, reconnect/restart/watchdog, Job Objects, flood/fault injection extensa y benchmark exhaustivo diferidos a autorización posterior, no eliminados de DEC-005. Parser Unity tiene validación básica; validación estricta completa/fuzzing pendientes de hardening. Diagnósticos editor conocidos siguen ENVIRONMENT / REQUIRES FOLLOW-UP #89. Sin 2B ni Fase1.


## IPC-002B — lifecycle y fallos básicos, 2026-10-06

**Resultado:** Incremento 2B **PASS / MERGED en Linux de desarrollo** mediante [PR #92](https://github.com/Josue1855/Gorilla-Escape/pull/92), merge normal autorizado por el PO; develop `7e3aa60aa53c65e7051f32477a77985fbe628907` contiene el candidato `88f1e309fc7adeffe6aa6499e195f53185ab36f9`. Árbol integrado idéntico y 105/105 hashes del inventario coincidentes. Las protecciones de develop permanecen intactas. Diseño [Accepted con precisiones del PO](PHASE0_IPC_2B_DESIGN.md). Base `9fbb566e5f24af8f4e92fa3c139968161e832d46`, rama `codex/phase0-ipc-2b-lifecycle`. [Procedimiento reproducible](PHASE0_IPC_2B_VALIDATION.md), [checks](evidence/unity-java-ipc-2b-2026-10-06/checks.json), [inventario SHA256 de 105 fuentes/contratos](evidence/unity-java-ipc-2b-2026-10-06/source-sha256.json), [hashes de build/JAR](evidence/unity-java-ipc-2b-2026-10-06/build-artifacts.json). El SHA de commit se informa en el PR/reporte de cierre; no inventar un hash autorreferencial en su propio archivo.

**Implementación:** seis estados aprobados y fallos activos siempre STOPPING→FAILED; PING/PONG de liveness 1 Hz sólo RUNNING, un pendiente/un lector, deadline PONG 2 s; cero restart/reconnect automático; hasta tres lanzamientos por ejecución Unity y reintento explícito después de cleanup completo. Generaciones aisladas y prueba real de callback exit tardío. Java adquiere FileLock antes de Spring/READY, libera finalmente y no borra el archivo; EOF se observa durante bootstrap. Cierre/cancellation sólo del hijo propio, streams/readers/CTS/handlers liberados; diagnóstico de desarrollo Player/Editor con Retry/Stop y atajos R/S. Sin cambio a codec/DTO/framing/token/autoridad ni dependencias.

### Build y conteos exactos

| Suite | Resultado | Alcance |
|---|---|---|
| Java 21 Maven verify | **13/13 PASS**, 0 fallos/errors/skips | 6 unit/contrato/codec/guard; 3 integración Spring HTTP; 4 tests con procesos JVM reales (readiness/framing/EOF, singleton, EOF temprano, kill/release/rearranque explícito). [Resumen sin properties/outputs crudos](evidence/unity-java-ipc-2b-2026-10-06/java-results.json) |
| Unity EditMode | **15/15 PASS**, 0 skips | Incluye 8 nuevos casos lifecycle/generación/budget y regresión codec/contrato/foundation. [XML normalizado](evidence/unity-java-ipc-2b-2026-10-06/editmode.xml) |
| Unity PlayMode | **10/10 PASS**, 0 skips | 6 tests con servidor Java real; 2 fixtures JVM para READY ausente/connect rechazado; 1 ruta inválida; 1 Bootstrap. [XML normalizado](evidence/unity-java-ipc-2b-2026-10-06/playmode.xml) |
| React regresión | **4/4 PASS**, build PASS | Fuentes PWA sin cambios |
| Player Linux development Mono | **PASS**, build 0 errores / 0 warnings | Unity 6000.3.23f1; 11 grupos de escenarios ejecutados, no 11 tests NUnit. [Eventos/resultados](evidence/unity-java-ipc-2b-2026-10-06/player-results.json) |
| Validaciones del repo | **PASS** | Foundation/links/management y diff sin whitespace |

### Matriz de fallos y recuperación

| Caso | Evidencia real aplicable | Resultado |
|---|---|---|
| Java muere en RUNNING | PlayMode + Player terminan exclusivamente su handle/child verificado | JAVA_EXITED, STOPPING→FAILED, cleanup completo, residual 0; sin autorestart |
| READY nunca llega | PlayMode/Player lanzan JVM helper que conserva pipes sin READY | READY_TIMEOUT; 15.02 s desde process-start observado hasta fallo/cleanup en Player. Helper identificado; no certificar interoperabilidad del servidor con él |
| TCP connect falla | PlayMode/Player con helper READY/endpoint cerrado real | CONNECT_FAILED; cleanup/residual 0, sin adoptar un proceso ajeno |
| TCP se pierde con Java vivo | PlayMode cierra el socket propio conectado al servidor real | CONNECTION_LOST; EOF cierra Java real con exit 0; Retry manual vuelve a READY/PONG |
| Java no responde al siguiente PING | Player SIGSTOP sobre child propio, luego SIGCONT tras STOPPING | CONNECTION_LOST; detección 3.04 s desde detención, incluye hasta 1 s de cadencia más deadline PONG 2 s; sin reconnect; cleanup/exit 0 |
| Unity cierra con Java activo | Player WM_DELETE_WINDOW durante RUNNING | Cleanup completo, Unity exit 0 / Java exit 0, residual 0; EOF→exit 65.37 ms |
| stdin EOF temprano | Java real + PlayMode/Player cancel STARTING | Cierre seguro después de refresh; nunca RUNNING en caso cancel temprano; Player Java exit 0, cleanup 1.88 s, residual 0 |
| Cancel RUNNING / Stop repetido | PlayMode + control S/Stop en Player | STOPPING→STOPPED, Java exit 0; Player cierre 64.22 ms, residual 0 |
| Singleton ocupado | Dos JVM reales; dos supervisores PlayMode; dos Players | Segundo exit 73, sin READY/Spring; dueño sigue vivo; ambos hijos terminan al cerrar sus owners, residual 0 |
| Lock stale/release | Unit Java + kill/release con otra JVM real | Archivo existente sin lock no bloquea; no borrar archivo ni matar por PID/puerto/lock |
| Flatpak ↔ host | Editor posee lock; JVM del host misma ruta | Exit 73 en 214.13 ms, sin READY ni proceso residual; [evidencia](evidence/unity-java-ipc-2b-2026-10-06/flatpak-lock.json) |
| Java propio no atiende EOF | Player detiene sólo su child con SIGSTOP | Espera EOF 10.04 s, kill propio observado, exit 137, forced=true, readers 1.17 ms; residual 0. No contar como graceful |
| Padre termina abruptamente | Harness mata su Player después de READY/PONG | Pipe EOF libera Java y deja residual 0. Unity exit -9 intencional; exitCode Java no observable desde padre muerto, no se inventa 0 |
| Recovery manual y límite | PlayMode/Player: tres lanzamientos con nuevas identidades/PONG | Dos retries explícitos tras cleanup; cuarto bloqueado; máximo un child válido, residual 0 al cerrar cada intento |
| Evento tardío | Unit reducer + callback real capturado del intento anterior, invocado durante nuevo RUNNING en PlayMode | Estado/child nuevos permanecen intactos |
| Cleanup y estados | Todos los casos anteriores + unit transición inválida/incompleta | Socket/streams/readers/exit observados, cleanupComplete; no FAILED directo desde estado activo; retry bloqueado si cleanup incompleto |

**Regresión 2A:** Unity→Java→READY→TCP IPv4 loopback→100 PING/PONG→EOF→clean exit **PASS**. Listeners efectivos de los hijos inspeccionados por inodos son únicamente TCP IPv4 **127.0.0.1** (HTTP e IPC). Smoke Player final N=100: startup **2.182 s**, RTT mínimo **0.910 ms**, P50 **1.600 ms**, P95 **1.994 ms**, máximo **3.504 ms**, errores **0**. Cierre Unity/Java normal exit 0, residual 0. Comparación de desarrollo con build 2A anterior en el mismo entorno: startup 1.873 s, mínimo 0.672 ms, P50 1.572 ms, P95 1.935 ms, máximo 2.343 ms, errores 0 ([registro](evidence/unity-java-ipc-2b-2026-10-06/baseline-2a-same-environment.json)). Variación observada no demuestra regresión material; no benchmarking exhaustivo ni gate rígido. Se conservan las métricas históricas 2A de IPC-002A. Nada se extrapola a teléfono→gameplay.

**Correcciones durante desarrollo:** primer PlayMode con JDK host por /run/host falló por symlinks de configuración /etc no visibles en Flatpak; sustituido por copia local de desarrollo del mismo JDK21 con enlaces resueltos. Se corrigió salida del diagnóstico y ejecución en segundo plano; la automatización descartó logs anteriores antes de cada lanzamiento y sustituyó clicks inestables Xwayland por los atajos visibles de la misma acción manual. Esas ejecuciones no acreditan PASS; resultados anteriores corresponden al candidato final, con inventario comprobado sin cambios de fuentes.

**Sanitización / límites:** sólo resúmenes JSON, XML sin outputs/properties y hashes revisados en Git; raw logs, JDK, helpers compilados y builds permanecen locales ignorados. No tokens/env dumps/payloads en evidencia. El build mantiene 0 warnings, pero existen diagnósticos del entorno Unity ya conocidos (licensing/debugger/build-server y registro nativo MemoryLeaks); no afirmar cero leaks globales. Windows, distribución JRE y PC oficial NOT RUN. Lock cooperativo y FS local; JVM congelada después de morir el padre requiere futura contención nativa, fuera de 2B. Watchdog, reconnect/restart automático, parser exhaustivo/fuzz/flood, performance extensiva, móvil/PWA/QR/sensores/cámara/gameplay/Input Fusion y Fase 1 no implementados. **2A PASS/MERGED; 2B PASS/MERGED; Incremento 2/Fase 0 IN PROGRESS; 2C/Fase 1 NOT STARTED.**

Checks posteriores del merge 2B en `7e3aa60aa53c65e7051f32477a77985fbe628907`: **management-validation PASS / java-react-foundation PASS**. Protecciones comparadas antes/después, idénticas. Cierre de integración; 2C no iniciado.

## IPC-002C — hardening y validación sostenida, 2026-10-06

**Resultado: PASS / MERGED Linux de desarrollo mediante PR #93.** Diseño [Accepted](PHASE0_IPC_2C_DESIGN.md) y [procedimiento reproducible](PHASE0_IPC_2C_VALIDATION.md). Fetch confirmó develop `7e3aa60aa53c65e7051f32477a77985fbe628907`; rama `codex/phase0-ipc-2c-hardening` desde ese SHA. Sin nuevas dependencias, rediseño del lifecycle ni cambios de fases futuras. Documentación local del merge 2B conservada.

| Gate / suite | Resultado y evidencia |
|---|---|
| Corpus Java/C# | **71/71 casos en ambos PASS**: tamaños uint32, truncación, UTF-8, documento único, fields/types/duplicates/escapes/identidades/sequence/token; [corpus común](../Shared/Protocol/ipc/cases/corpus.json) |
| Java verify | **22/22 PASS**, 0 failures/errors/skips; suites existentes 2A/2B conservadas; [resumen sin properties/outputs](evidence/unity-java-ipc-2c-2026-10-06/java-results.json) |
| EditMode | **19/19 PASS**, 0 skips; corpus/READY/concatenación/UTF-8 output/tamaño previo + regresión; [XML](evidence/unity-java-ipc-2c-2026-10-06/editmode.xml) |
| PlayMode | **12/12 PASS**, 0 skips; seis tests backend real, cuatro helpers JVM identificados, ruta inválida/Bootstrap; [XML](evidence/unity-java-ipc-2c-2026-10-06/playmode.xml) |
| React regresión | **4/4 PASS**, build PASS; sin cambios a PWA |
| Player Linux development Mono | Build **0 errores / 0 warnings**; binarios/hashes en [artifacts](evidence/unity-java-ipc-2c-2026-10-06/build-artifacts.json) |
| Input inválido aislado | Rechazos mediante socket/JVM Spring real, PING válido posterior en misma JVM, proceso vivo; sin payload/token en diagnósticos |
| Deadlines absolutos | Prefijo y cuerpo con goteo500ms terminan ≈2s total; write bloqueado en sockets reales con buffers reducidos sólo test termina por deadline2s, nueva conexión responde durante >2s |
| Presión determinista | **5 grupos reales PASS**: corpus rechazos, oversized100+invalid100, valid1000 consumidor lento+máximos100, parciales/goteo, productor sin lector≤1MiB; [recursos y RSS observada](evidence/unity-java-ipc-2c-2026-10-06/pressure-results.json) |
| Tareas / colas | Writer único, sin common pool, sin cola PONG/thread por PONG; máximo deadline pendiente1, final0, executor terminado; no body read para prefijo oversized; buffers fuente/cuerpo/output acotados y RTT list≤1024 |
| Player medición | **3 corridas independientes N=1000 PASS**, con 100 warmup cada una; métricas completas abajo y [JSON individual/estabilidad](evidence/unity-java-ipc-2c-2026-10-06/measurement-results.json), [CSV](evidence/unity-java-ipc-2c-2026-10-06/benchmark-runs.csv) |
| Estabilidad real | **601.24s RUNNING PASS**, heartbeat real1Hz; 601 enviados/recibidos/válidos (handshake1+heartbeats600), errores/timeouts/disconnects0; estado final STOPPED, Unity exit0/Java exit0, cleanupComplete, residual propio0 |
| Regresión Player 2A/2B | **11 grupos PASS**, no 11 tests NUnit; [resultados](evidence/unity-java-ipc-2c-2026-10-06/regression-player-results.json). Manual3 launches/cuarto bloqueado, singleton, cancel startup/RUNNING, EOF normal/abrupto, READY/refused fixtures, liveness/fallback propios; generaciones tardías también PASS PlayMode |
| Validadores | management/foundation PASS; diff --check PASS |

### Tres corridas independientes

Entorno [registrado](evidence/unity-java-ipc-2c-2026-10-06/environment.json): Unity6000.3.23f1/Mono, Java21, Jackson3.1.5 ya existente. JVM nueva por Player; filesystem warm sin desalojar caches, escritorio de desarrollo con posible carga breve de validación. Sin Editor Unity durante las tres corridas. N=1000 excluye warmup100; totales enviados/recibidos/válidos=1100. Duration incluye warmup restante+medición después de primer PONG; cadencia efectiva sobre intervalos de las1000 muestras. Ticks tardíos: >1ms respecto del turno previsto, incluido warmup restante; se publican, no se eliminan.

| Corrida | Startup ms | N | Sent / Received / Valid | Errors / Timeouts | RTT min ms | P50 ms | P95 ms | Max ms |
|---|---:|---:|---|---|---:|---:|---:|---:|
| 1 | 2134.0482 | 1000 | 1100 / 1100 / 1100 | 0 / 0 | 0.2580 | 1.0075 | 1.5617 | 3.5342 |
| 2 | 1912.9807 | 1000 | 1100 / 1100 / 1100 | 0 / 0 | 0.2698 | 0.9777 | 1.5687 | 3.1940 |
| 3 | 1899.5049 | 1000 | 1100 / 1100 / 1100 | 0 / 0 | 0.3096 | 0.9767 | 1.6134 | 3.1152 |

| Corrida | Duration s | Effective Hz | Late ticks | Shutdown ms | Exit Unity / Java | Own residual |
|---|---:|---:|---:|---:|---|---:|
| 1 | 21.9865932 | 50.0010696 | 212 | 50.0231 | 0 / 0 | 0 |
| 2 | 21.9844135 | 50.0001662 | 191 | 62.1348 | 0 / 0 | 0 |
| 3 | 21.9859263 | 49.9992382 | 224 | 76.2648 | 0 / 0 | 0 |

Todas cumplen P50≤5ms, P95≤10ms, máximo≤50ms y errores/timeouts nominales0. Nearest-rank por corrida, sin mezclar3000 muestras ni excluir picos. Tres launches no permiten declarar P95 estadísticamente significativo de startup. RTT worker incluye serialización/framing/validación/TCP/Java, no consumo de juego ni latencia one-way: **no extrapolar a teléfono→Java→Unity→gameplay**.

### Estabilidad, memoria y regresión

Estabilidad: startup1921.2768ms, operación601.2428525s, frecuencia efectiva0.9986053Hz, N heartbeat600, min0.3316/P501.0978/P951.7587/max3.3326ms, shutdown54.0375ms. Handshake aparte, STOPPED/exit0/cleanup completo/residual0. Todos los listeners propios observados: sólo IPv4 127.0.0.1. Errores inyectados de regresión son expectativas separadas del recorrido nominal; fallback propio exit137 no se presenta como cierre limpio y el exit code Java tras crash del padre no es observable, aunque su desaparición sí.

RSS muestreada cada5s: Unity244961280→266268672bytes, pico266268672; Java215642112→207876096bytes, pico215642112. Managed heap **NOT MEASURED**. Muestra inicial tras primer PONG, no heap estabilizado tras GC/JIT; serie completa conservada. No excede alerta max(64MiB,25%), pero eso no acredita ausencia global de fugas ni memory safety. Diagnósticos nativos/licensing/debugger/build-server del entorno Unity previamente registrados y deuda[#89](https://github.com/Josue1855/Gorilla-Escape/issues/89) se conservan. Los límites deterministas de producto sí se verifican como gate.

Smoke2A regresión N100: startup1395.0498ms, mínimo1.0118/P501.5640/P951.9586/máximo2.0892ms, errors0 y cleanExit. No reemplaza ni reescribe mediciones históricas de2A/2B.

Iteraciones descartadas: se corrigieron expectativas del harness (spy confundía lectura del prefijo con cuerpo, catch C# de InvalidDataException, token vigente en conexión nueva frente a reutilización post-handshake y supuesto de llenar TCP con≤1MiB). La prueba separada con buffers pequeños demuestra write deadline real sin alterar tuning productivo. Dos corridas iniciales Player de regresión no acreditaron PASS porque Xwayland no confirmó teclado/foco; se elevó/enfocó ventana, esperó evento y canceló startup mediante cierre normal que activa RequestStop. Candidato runtime no cambió tras medir; corrida final11 grupos y PlayMode12/12 acreditan regresión, no los intentos descartados.

**Identidad / sanitización:** [147 hashes de fuentes funcionales/tests/harness](evidence/unity-java-ipc-2c-2026-10-06/source-sha256.json); fuentes runtime y hashes JAR/Player/assemblies comprobados sin cambios después de las mediciones y al terminar todas las pruebas. Hash JAR `b43a17087e34cda7404952253dd4ae61615a4ae2f3b32489e8c5a3803d022961`. Sólo resúmenes JSON/CSV/XML sin properties/outputs secretos; raw logs/runtime/helpers/builds ignorados. Codec C# específico de probe, sin parser genérico ni dependencia nueva. No reconnect/restart automático, watchdog avanzado/nativo, Job Objects, arbitrary tree kill, TLS, malware defense, móvil/PWA/QR/sensores/cámara/gameplay/scoring/Input Fusion ni Fase1. Windows/PC oficial **NOT RUN**.

**Estados:** 2A PASS / MERGED; 2B PASS / MERGED; **2C PASS / MERGED; Incremento 2 — Unity ↔ Java IPC PASS / MERGED**; Fase 0 IN PROGRESS; Fase 1 NOT STARTED. Integración mediante merge normal autorizado del PR #93; sin trabajo posterior.

## Integración normal del Incremento 2C

[PR #93](https://github.com/Josue1855/Gorilla-Escape/pull/93) integrado mediante merge normal autorizado por el PO el 2026-10-07 01:03:09 UTC (2026-10-06 local). develop `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760` contiene el candidato validado `e957775b0a848a001d37e1a4770eb14103888d0c`; árbol completo idéntico y 147/147 hashes funcionales/test/harness coincidentes. Checks posteriores **Management validation PASS / Foundation validation PASS**. Protección efectiva comparada antes/después, sin cambios: PR obligatorio, checks estrictos `management-validation` y `java-react-foundation`, force push y eliminación deshabilitados, reglas aplicables a administradores y conversaciones resueltas obligatorias. Sin bypass ni cambios al candidato.

**Estado vigente:** 2A PASS / MERGED; 2B PASS / MERGED; 2C PASS / MERGED; **Incremento 2 — Unity ↔ Java IPC PASS / MERGED**; Fase 0 IN PROGRESS; Fase 1 NOT STARTED. Evidencia exacta de 2C conservada: Java22/22, EditMode19/19, PlayMode12/12, React4/4, build0errores/0warnings, corpus71casos, regresión Player11grupos, residual Java propio0, tres corridas independientes N1000 y estabilidad601.24s PASS; errores/timeouts/desconexiones nominales0. No combinar ni reinterpretar corridas ni extrapolar a teléfono → gameplay. Trabajo detenido tras integración.

## LAN-003A — HTTPS LAN, candidato automatizado 2026-10-06

**Resultado: implementación 3A IN PROGRESS; gate físico NOT RUN.** Diseño general
[Accepted](PHASE0_PHONE_LAN_ONBOARDING_DESIGN.md), autorización únicamente 3A. Base
`ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`, rama
`feature/mobile-phase0-lan-https-3a`. [Guía física/setup](PHASE0_PHONE_3A_VALIDATION.md).
Sin QR, sessionId/deviceId, heartbeat de presencia, estado CONNECTED, WS ni sensores.

Configuración externa opt-in de IPv4/interfaz confirmadas, default8443, TLS directo
Spring/PKCS12 con SAN validado. Rechaza IP/interfaz/configuración/password/certificado
inválidos, wildcard/IPv6/VPN/virtual; leaf/password fuera de cualquier repositorio Git.
Sin archivo móvil, perfil previo intacto. Unity hereda GORILLA_MOBILE_CONFIG al Java
propio; no cambia fuentes Unity ni READY/token/framing/codecs/contratos/lifecycle IPC.
Health diagnóstico añade identidad pública de lanzamiento, contador de peticiones y
secure; APIs no-store. React mide petición puntual, muestra HTTPS/secure context y
estado del worker, permite comprobar nuevamente; no crea sesión ni conexión continua.
Sin dependencias/framework adicionales y sin modificación de firewall/router/trust.

| Verificación | Resultado del candidato |
|---|---|
| Java regresión + TLS/LAN reales | **26/26 PASS**, sin fallos/errors/skips, JDK21 |
| TLS confiable / CA desconocida / hostname SAN | PASS con cliente real y hostname verification; sin trust-all |
| Perfil portátil CI TLS loopback | 3 PASS / 1 skip explícito del preflight LAN; el mismo caso sí pasó con configuración LAN |
| React / build Vite / empaquetado JAR | **5/5 PASS**, builds PASS |
| Assets/root/manifest/sw/API404/archivos privados no servidos | PASS desde Java HTTPS real |
| Player con Java propio y HTTPS LAN | PASS, FIRST_PONG real, shell/health, listeners LAN HTTPS y127.0.0.1 IPC |
| Cierre Player móvil | Unity exit0, Java exit0, cleanup completo, residual propio0 |
| Regresión Player2A/2B con JAR final | **11 grupos PASS**, singleton/manual recovery/EOF/cancellation/deadlines/cleanup, residual0 |
| UI en Chrome PC390px | PASS desde PWA empaquetada, HTTP loopback sólo para inspección UI |
| Foundation / gestión / diff | PASS; no fuentes Unity/Shared modificadas |
| Android/Chrome físico; iPhone/Safari físico | **NOT RUN** |
| CA demo instalada, TLS sin warnings físico, SW físico | **NOT RUN** |
| Primera carga sin cache con WAN desconectada; refresh/reapertura física | **NOT RUN** |
| Segundo dispositivo LAN intenta IPC | **NOT RUN**; rechazo desde propia IPv4 LAN de PC sí comprobado |

Evidencia sanitizada: [Java](evidence/phone-lan-https-3a-2026-10-06/java-lan-results.json),
[TLS portable](evidence/phone-lan-https-3a-2026-10-06/tls-portable-results.json),
[Player móvil](evidence/phone-lan-https-3a-2026-10-06/mobile-player-results.json),
[regresión](evidence/phone-lan-https-3a-2026-10-06/player-regression-results.json),
[React](evidence/phone-lan-https-3a-2026-10-06/react-results.json),
[pendientes físicos](evidence/phone-lan-https-3a-2026-10-06/physical-results.json),
[hashes fuentes](evidence/phone-lan-https-3a-2026-10-06/source-sha256.json) y
[artefactos](evidence/phone-lan-https-3a-2026-10-06/build-artifacts.json).
PKI de tests generada temporalmente fuera del repo y eliminada; nunca usada en teléfonos.
mkcert de demo todavía no instalado/preparado: origen/versión se registrarán al setup.

Corridas descartadas no acreditan gate: test nuevo utilizaba token en lugar de launchToken;
perfil portable de test repetía server.port y no arrancaba; ambos corregidos en tests.
Un lanzamiento concurrente de dos harness Player activó el singleton esperado: se repitió
secuencialmente con JAR final, once grupos PASS. Se conserva únicamente el resultado final
sanitizado; raw logs/builds/captura PC ignorados. Regresión no altera métricas históricas
2A/2B/2C ni las tres corridas independientes de2C. No se repite benchmark600s porque IPC
no cambió. EditMode/PlayMode y build Unity previos conservados, no declarados como nuevas
corridas. Diagnósticos nativos del entorno Unity #89 permanecen; no afirmar ausencia global
de fugas. Java mantiene avisos existentes de deprecación/Mockito/JVM; no declarar0warnings.

**Estados:** Incremento1 PASS/MERGED; Incremento2 PASS/MERGED; **3A IN PROGRESS,
gate físico NOT RUN**; 3B NOT STARTED; Incremento3 IN PROGRESS; Fase0 IN PROGRESS;
Fase1 NOT STARTED. Publicar borrador de PR para revisión, no integración automática.
Pendiente responsable con Android/iPhone: confirmar LAN, preparar CA pública, comprobar
Chrome/Safari y retirar WAN manteniendo Wi-Fi. No se inventan modelos/router ni métricas
URL→shell/confirmación físicas. No declarar3A PASS Linux+Android ni multiplataforma PASS.

### Requisito posterior DEC-006 — reinterpretación del gate, no de las mediciones

El PO exige PWA universal y Flutter Android opcional con HTTPS sin configurar certificados,
CA/perfiles, DNS o navegador del jugador. La automatización LAN-003A anterior se conserva
exacta como evidencia técnica de CA de test/IPC/hosting; **no demuestra cumplimiento de la
nueva UX/trust público**. Nuevo gate3A NOT RUN, estrategia CA móvil superada; #94 DRAFT.
[Rediseño Proposed](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) pendiente de aprobación.
Esta tarea sólo actualiza documentos: validación de gestión/enlaces y diff PASS; builds y
pruebas nuevas de aplicación no aplican, Flutter no implementado. Sin nuevos resultados
físicos ni modificación/reinterpretación de métricas previas.

### 3A — Adaptación FQDN y origen443 (software; gate físico pendiente)

Head previo confirmado `50e48418ff286dcd3096c92800fd47f602ad27f2`, #94 OPEN/DRAFT. Arquitectura técnica Accepted; sólo software autorizado. [Evidencia nueva sanitizada](evidence/phone-lan-fqdn-3a-2026-10-06/java-results.json) separada del candidato CA/SAN IP histórico, que conserva sus resultados exactos.

- Java JDK21 `verify` explícito LAN: **30/30 PASS**, sin skips; ocho casos TLS/configuración y22 regresiones existentes. SAN DNS correcto/incorrecto/IP-only, IP de escucha distinta del hostname, origen HTTPS443 estricto, cadena completa/incompleta/orden inválido, PKIX, root omitido con issuer conocido/desconocido, leaf expirado, password/permisos/origen inválidos, CA desconocida, hostname verification, puerto ocupado/no fallback, health/assets/IPC/EOF. Root de test no acredita confianza pública.
- TLS portable: **6 PASS / 2 SKIP de 8**; los dos preflight LAN se ejecutaron PASS en la corrida explícita anterior. Build Maven/JAR PASS; aviso previo de API deprecada en ProbeCodec sin modificación. No afirmar cero warnings globales ni nueva build Unity.
- React **5/5 PASS**, build PASS; siete archivos dist idénticos byte a byte dentro del JAR. Recursos locales inspeccionados sin CDN obligatorio. React no cambió.
- [Player real Unity→Java HTTPS DNS](evidence/phone-lan-fqdn-3a-2026-10-06/mobile-player-results.json): **PASS**, LAN8443 + IPC127.0.0.1, SNI/validación DNS real por conexión de test a IP explícita; Java hijo con UID normal y capacidades efectivas0, READY/PING/PONG, health/shell locales, Unity exit0, Java exit0, cleanup y propios residuales0. DNS del SO intacto;443/DNS router/teléfonos no probados.
- [Regresión Player2A/2B](evidence/phone-lan-fqdn-3a-2026-10-06/player-regression-results.json): **11 grupos PASS**, N100 nominal, muerte/recuperación manual/límite3, cancel, singleton, timeoutREADY/conexión, liveness/fallback/EOF; propios residuales0. Fixtures etiquetadas, crash de padre no permite observar exitCode Java. No extrapolar métricas IPC a móvil/gameplay; no repetir ni combinar métricas2C.
- [Intentos anteriores](evidence/phone-lan-fqdn-3a-2026-10-06/attempts.json): primer cliente Java no resolvía hostname ficticio, ajustado con ruta test directa y SNI; primer intento Player no reconoció acción UI en Xwayland, repetición secuencial del mismo harness PASS. Ninguno se oculta ni se considera PASS.

| Dimensión | Estado |
|---|---|
| Software / pruebas aplicables de PC | PASS |
| Procedimiento Linux443 | PREPARADO, no aplicado |
| Dominio/certificado público/router/DNS/redirección443 | Pendiente / NOT RUN |
| Android e iPhone físicos, sin CA ni DNS manual, primera carga sinWAN | NOT RUN |
| Gate3A | IN PROGRESS |

[Procedimiento vigente](PHASE0_PHONE_3A_VALIDATION.md) y [Linux443](PHASE0_PHONE_3A_LINUX_443.md). No compras/emisión pública/router/firewall/trust modificados. Hashes fuente/artefactos guardados; Player Unity previo reutilizado sin cambio Unity/Shared/IPC. Validación foundation/gestión y diff PASS. 3B/4A/4B/4C NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED. #94 DRAFT, sin merge.

### 3A — Android Emulator / Chrome, QA local

[Procedimiento reproducible](PHASE0_PHONE_3A_ANDROID_EMULATOR.md) y [resultados nuevos](evidence/android-emulator-3a-2026-10-06/results.json): **10 PASS / 2 BLOCKED / 1 SKIP**, API36/Chrome133.0.6943.137, KVM/headless en Pop!_OS24.04/COSMIC. Android boot, Chrome, React real, health200/no-store, interacción, recarga, restart navegador, error de conexión al cerrar Java real, rechazo de certificado no confiable, logcat y cleanup PASS. Java HTTP/TLS, Emulator y ADB exit0; residuales propios/helpers0. SDK ausente: [BLOCKED/exit2](evidence/android-emulator-3a-2026-10-06/missing-sdk-results.json), sin iniciar procesos.

Java iniciado por el harness de QA, no Unity; HTTP localhost por ADB sólo prueba componentes, no fallback del producto ni HTTPS público. Certificado efímero rechazado por Chrome sin bypass/CA instalada; confianza pública positiva y LAN/DNS443/Android/iPhone físicos siguen BLOCKED. QR/sesión/sensores/WSS/Flutter SKIP fuera de alcance. No sustituye pruebas históricas Unity/Java/React ni las declara reejecutadas.

[Intentos y hash exacto del harness](evidence/android-emulator-3a-2026-10-06/manifest.json): tres fallos de automatización conservados (dispositivo aún no disponible, first-run Chrome y espera tras restart), corregidos exclusivamente en QA; corrida final PASS. Evidencia seleccionada sanitizada; logs/perfiles/PKI ignorados y temporales. Sin instalaciones, cambios de runtime, red, DNS, firewall o confianza del sistema. **3A IN PROGRESS; #94 DRAFT**; estados posteriores sin cambio.


## Investigación móvil segura, 2026-10-06 — sin ejecución nueva

Actualización documental exclusivamente. WT/RTC, pin Safari, sensores reales, hotspot, webcam e Input Fusion: NOT RUN para la arquitectura propuesta. No nuevos resultados de producto ni nuevas métricas; no se repiten suites completas sin cambios runtime. Emulador Android histórico **10 PASS / 2 BLOCKED / 1 SKIP** conservado, sin reinterpretación como físico. IPC, HTTPS LAN, Unity y lifecycle previos conservan sus resultados/alcance. Ver [plan y criterios propuestos](PHASE0_SECURE_MOTION_SPIKE_PLAN.md).

Verificación GitHub de sólo lectura en esta tarea: #94 **OPEN/DRAFT**, head `0d940f77f533bb6780bc3f07169dce414ea21e54`; `management-validation` y `java-react-foundation` SUCCESS sobre ese head remoto. Documentos de investigación locales no publicados ni cubiertos por esos checks remotos. Validación documental local: management PASS (88 registros, 242 enlaces) y git diff --check PASS. Sin merge, commit ni push.


## SENSORS-SPIKE-001 — captura PWA aislada, 2026-10-07

**Alcance:** adquisición/diagnóstico y lifecycle del capturador; sin sensores enviados a Java/Unity ni gameplay. Node existente v24.19.0; ninguna dependencia instalada. Código, página y procedimiento en [guía](../PWA/spikes/sensors/README.md).

**Ejecutado:** `node --test PWA/spikes/sensors/capture.test.js` — 8/8 PASS, 0 FAIL/skip; `npm --prefix PWA test` — 5/5 PASS, 0 FAIL/skip. Primera corrida 8/8; tras validar rangos Euler/interval y ampliar casos de permiso rechazado, candidato local final volvió a pasar 8/8. Tests con EventTarget/permisos/reloj simulados: granted/denied/throws/rejects, contexto inseguro/API ausente, campos nulos/NaN/Infinity/tipo inválido, cambios orientación, cadencia/interrupción, hidden/visible, pagehide/stop/restart y resultado de permiso tardío cancelado. No latency/Hz físicos publicados ni build Player nuevo.

**Límites:** browser real de la página experimental, Android/iPhone físicos, frecuencia hardware, precisión, calibración/normalización, intención/FPR/FNR, webcam, RTC/WT y nueva confianza HTTPS NOT RUN. 50 Hz en fixture sólo verifica aritmética con timestamps sintéticos, no sensor real. No iniciar servidor, hosting ni emitir certificados. Memoria estructural: latest por fuente/contadores, sin cola/historial; no declaración de ausencia global de fugas.

**Conservación:** emulador histórico 10 PASS / 2 BLOCKED / 1 SKIP y todas las evidencias foundation/2A/2B/2C/HTTPS intactas. No repetir Java/Unity/Player porque sus fuentes no cambian. Capturador aislado no modifica su aceptación. Estados: #94 OPEN/DRAFT, 3A/Fase 0 IN PROGRESS, Fase 1 NOT STARTED. Los checks GitHub previos no cubren cambios locales sin commit del Spike.

**Corrida final exacta:** tras completar diagnóstico de accelerationIncludingGravity y aislamiento de permisos tardíos respecto de un nuevo Start, tests 8/8 PASS nuevamente. [Resultado sanitizado y hashes de los cuatro archivos del Spike](evidence/pwa-sensors-spike-2026-10-07/results.json). Regresión PWA 5/5 conserva su ejecución previa; fuentes productivas no cambiaron. Management PASS (88 registros, 261 enlaces locales), git diff --check PASS. Esta evidencia nueva no altera artifacts históricos.


## Evidencia adicional aislada 3A-T — 2026-10-07

Build del prototipo Maven Java 21 PASS (aviso API deprecated; no 0 warnings). Chrome desktop real: seis escenarios PASS en loopback y seis en dirección LAN del mismo host; invalid-input también comprobado dentro de sesiones positivas. Pin correcto, incorrecto y vencido diferenciados por etapa; AUTH y oversize diferenciados, ningún negativo por deadline. 20 reliable y 20 datagramas por sesión positiva; métricas por ruta y recovery en [informe](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#15-spike-ejecutable-3a-t--2026-10-07), JSON y hashes en [evidencia](evidence/webtransport-spike-2026-10-07/). EOF/exit 0, UDP rebinding y cero procesos propios residuales PASS. Primeros fallos de perfil de certificado documentados; no contarlos como éxito de seguridad.

LAN física, Android, iPhone, Windows y offline PWA NOT RUN. Upstream unit tests NOT RUN; pruebas costosas de componentes sin cambios no repetidas. No afirmar memoria sin fugas ni latencia móvil; evidencia 2A/2B/2C y emulador 10 PASS/2 BLOCKED/1 SKIP conservada sin reinterpretación. Spike local fuera del candidato remoto #94, aceptación 3A no cambia.


## Android WebTransport + componentes existentes — 2026-10-07

[Informe y métricas por sesión](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#16-android-emulator--validación-integrada-de-componentes-2026-10-07); [evidencia](evidence/webtransport-android-2026-10-07/). API36/Chrome133 existentes, emulador desechable: final **16/16 PASS**, React/Health/reload/restart/EOF/error, seis WT con TLS hash real/negativos, reconexión tras reload, eventos virtuales sensores y Stop, cleanup. Cero procesos propios residuales, exit 0 y UDP liberado. Reliable RTT máximo observado 65 ms, conservado; no gate de performance WT ni extrapolación móvil.

Initial 15 PASS de aserciones limitadas; strict-before-fix 13 PASS/1 FAIL por UI sensor stale, conservados separadamente. Corrección mínima del receptor Window de timers en el Spike de sensores existente, regresión unit **9/9 PASS** y Android final RUNNING/eventos/STOPPED PASS. Histórico sensor unit 8/8 y Android 10 PASS/2 BLOCKED/1 SKIP intactos. Sensores no conectados al transporte ni Unity. Físicos/HTTPS público/offline LAN NOT RUN. Capturas/JSON sanitizados; no instalaciones, CA, trust bypass o red modificada.

### Incremento aislado A/B y base C — 2026-10-07

Comparador RTC Chromium ↔ Java21 real: **PASS**, `webrtc-java 0.19.0`/JNI Linux fijados sólo en Spike. Corrida final `run04-reviewed.json`: 2400/2400 mensajes medidos, 0 pérdidas/timeouts/errores nominales; 1 peer, reconexión nueva y 4 peers activos. Padding32/1024, reliable ordered/unordered maxRetransmits0. Candidatos host UDP, sin iceServers/STUN/TURN. Negativos7 PASS: SDP inválido, oferta con ID duplicado aislada, payload inválido, quinto peer rechazado, desconexión aislada, credential incorrecta, oversized. Revisión detectó que rechazar una oferta con ID repetido podía cerrar el peer existente: corrección limitada al peer asignado por ese request y prueba de regresión real añadida; corridas anteriores conservadas.

Java startup observado264.52ms (un lanzamiento, no P95). RTT por escenario, sin combinar muestras: single N400, mayor P95 de sus subgrupos0.60ms/max1.50ms; recovery N400, mayor P950.90ms/max1.70ms; four N1600, mayor P951.00ms/max2.00ms. Subgrupos/tamaños/canales y métricas completas permanecen separados en JSON; esos máximos de percentiles no son un percentil combinado. No se comparan como benchmark equivalente con WT ni se extrapolan a teléfono/gameplay. Java exit0, peers residuales0, comandos pendientes0, navegador cerrado.

DEC-010 selecciona **WebRTC DataChannel** como dirección Software/Lab; no transporte productivo integrado aún. WT histórico permanece aislado. Sesión técnica Java: siete unit tests PASS para token one-use, cuatro players, expiración, liveness, desconexión/identity y reconnect con epoch. Regresión Java37 total:35 PASS/2 SKIP de variantes LAN,0 fallos/errores; no se cambian resultados históricos. Foundation wiring PASS; diff check PASS. No QR/endpoint de admisión implementado todavía; C PARTIAL, pipeline móvil/Unity, webcam/Fusion, multicliente productivo y auditoría final pendientes.

Evidencia: `docs/evidence/webrtc-comparator-2026-10-07/`; comando `tools/spikes/webrtc/run.mjs`; restricciones/dependencias `tools/spikes/webrtc/README.md`. #94 verificado OPEN/DRAFT, head0d940f77; sin commit/push/merge. Fase0 IN PROGRESS y Fase1 NOT STARTED. Físicos DEFERRED por decisión PO; ya no bloquean gates Software/Lab.

**Actualización del mismo incremento:** POC QR implementado con ZXing core3.5.3/Apache-2.0 (única nueva dependencia del servidor para QR), QR384×384 codificado y decodificado realmente en test; URL HTTPS con admission efímero en fragment, sin token IPC/clave/credencial permanente. Dominios `.invalid` son fixtures, no hosting desplegado ni concesión de infraestructura. Rechaza HTTP, userinfo, query/fragment previos; token consumido sólo una vez. Dos tests QR PASS. Credential de resume rota al reconectar y epoch anterior queda inválido; siete tests sesión PASS. Regresión final con interfaz LAN explícita: **Java39/39 PASS,0 SKIP,0 errores/fallos** (`technical-session-qr-java-final.json`). La corrida previa35PASS/2SKIP sigue conservada separadamente. C permanece PARTIAL: faltan endpoints/integración de admission/liveness con el transporte real/PWA; QR POC componente PASS. No pipeline móvil→Unity, webcam, asociación/Fusion ni auditoría final aún. No se declara Fase0 COMPLETED ni Fase1 READY.

## Sesión / QR / Gorilla Protocol / WebRTC / PhoneInput — Software/Lab, 2026-10-07

**PASS en Chrome real sintético/replay**. [Informe](PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md) y [evidencia dedicada](evidence/mobile-input-runtime-2026-10-07/).

| Prueba ejecutada | Resultado |
|---|---|
| Java, con interfaz LAN explícita | 48/48 PASS; 0 SKIP |
| Node: React, capturador y adaptador | 18/18 PASS |
| Unity EditMode / PlayMode real | 23/23 y 12/12 PASS |
| Build Linux | 0 errores / 0 warnings |
| Corpus móvil compartido | 17 casos PASS, más escenarios de secuencia/rate/aislamiento |
| Regresión Player 2A/2B | 11 grupos PASS |
| Regresión Player 2C | Tres N=1000 independientes y 601.2234171 s PASS |

El corpus IPC anterior de 71 casos conserva sus regresiones por las suites aplicables. 2C terminó STOPPED, exit 0, sin force y con cero procesos propios residuales. Errores/timeouts: 0.

Runtime end-to-end04: cuatro peers host/host UDP, sin STUN/TURN; QR PNG → decoder → JOIN real; PlayerIds 1–4; captura DOM sintética, replay y TOUCH; 18 casos negativos/lifecycle; recuperación manual; 643 observaciones Unity sin cruces de identidad. Carga nominal de 30/50/60 Hz, 60 muestras de movimiento por cliente/grupo y recibos adicionales de heartbeat contabilizados. Los 12 grupos RTT permanecen separados: P95 por grupo entre 2.7 y 5.3 ms, máximo individual 9.4 ms, cero errores/recibos faltantes nominales.

Ráfaga de 500 muestras: 498 coalesced. Presión de 400 mensajes: 209 ACK / 191 rechazos; cliente recuperado y otros activos. No extrapolar pérdida física ni benchmarks. Contadores Java: forwarded 643, coalesced 318, pending 0, rejected 202; incluyen control/presión.

Tramos UTC en la misma PC, incluyendo replay/presión: Java → Unity P50/P95 = 11/23 ms; generación del cliente → Unity = 13/25 ms, resolución de 1 ms. No son human latency, motion-to-photon ni latencia física.

Android nuevo **BLOCKED** (canales RTC no OPEN); Player/Java/Emulator/ADB exit 0 y cero residuales. Background/foreground móvil NOT RUN; iPhone/Android físicos/Windows DEFERRED. Histórico Android 16/16 y resultados WT/comparador intactos. Se conservan fallos iniciales Java/PlayMode/singleton y la primera estabilidad 2C con cierre forzado; la repetición fija pasó, sin confirmar causa definitiva. No se repite el comparador cerrado.

DEC-010 IMPLEMENTED FOR PHASE 0 LAB. Fase 0 / 3A IN PROGRESS, Fase 1 NOT STARTED. Camera/Input Fusion NOT STARTED. PR #94 DRAFT; sin commit/push/merge.


## Webcam PC / asociación / alineación / Fusion — Software/Lab, 2026-10-08 UTC

**PASS — SOFTWARE/LAB**: CameraInput, Player Lock, Temporal Alignment e Input Fusion infrastructure.
Se conserva runtime móvil PASS y se añade adaptador cámara local supervisado/fixtures; no gameplay.
[Informe completo y evidencia](PHASE0_CAMERA_FUSION_VALIDATION.md), DEC-012.

- Java48/48, PWA18/18, EditMode64/64, PlayMode16/16 finales PASS; Linux build0 errores/0 warnings, PWA build PASS.
- Corpus12 camera cases,21 escenarios fusion y8 trazas conceptuales; Player4Chrome RTC reales + camera replay PASS,
  turnos1–4, aislamiento, lost/recovery, phone disconnect/manual resume y freshness.
- unavailable/vision failure no detienen cuatro inputs móviles; worker exit2 esperado, parents exit0, propios residuales0.
- Regresión Player2A/2B11 grupos PASS; runtime mobile real PASS con18 negativos/lifecycle. Bench2C anteriores
  independientes/601.2234171s conservados sin nueva ejecución; JAR y fuentes de transporte/PhoneInput intactos,
  regressions reales codec/proceso Java/PlayMode repetidas. Getter de lectura nuevo únicamente en IpcProbeRunner.
- Replay:342 frames,336 recibidos,29.868FPS;50 evaluaciones Eligible; delta P50/P953/13ms,max14ms.
  Process→Unity P50/P9510/43ms,max202ms. Datos completos sin exclusiones en e2e-replay03.json.
- Webcam física:640×480,102 frames,100 recibidos,17.611FPS; processing P50/P9539.341/65.884ms,
  read P50/P957.655/36.538ms; process→Unity P50/P9510/157ms,max402ms. Confianza .00136–.00687,
  cero frames fiables bajo configuración lab; captura→detector→store y release PASS técnico parcial,
  persona/salida/retorno fiable NOT RUN.30FPS no acreditados, hardware dropped frames desconocidos.

Normales: Unity/Java/vision exit0, STOPPED, camera/detector released, sin forced y propios residuales0.
Intentos fallidos de compilación/tests/harness y early Python exit134 preservados con reparaciones y
repeticiones separadas; no alteran históricos. No global leak claim ni accuracy/latencia física deportiva.

iPhone/Android físicos, Windows, movimiento humano, fusión física, tuning final y packaging siguen DEFERRED;
Android Emulator RTC nuevo BLOCKED. Secure-context/onboarding aún deuda; no reabrir comparador/WT ni usar
cámara móvil. A–J del cierre Software/Lab disponen de gates; **siguiente único bloque: FINAL PHASE0 AUDIT**,
no ejecutado aquí. Fase0/3A IN PROGRESS, Fase1 NOT STARTED; PR94 OPEN/DRAFT head0d940f77, nueva implementación
sólo local sin commit/push/merge. Auditoría decidirá cierre/integración; no se declara COMPLETED ni READY Fase1.


**Candidato final del mismo incremento:** EditMode64/64 y build03 Linux0/0; e2e-replay04 PASS después de
conservar RawPhone/RawCamera/ages/quality aun con input no elegible. No cambia algoritmo temporal, detector,
lifecycle ni protocolo. Replay04: 329 frames/326 recibidos, 29.870FPS; 48 Eligible,
delta P50/P95 9/15ms,max15ms; process→Unity P50/P95 10/42ms,max210ms.
Corridas anteriores y webcam física se mantienen separadas, no se sustituyen/combinan. Propios residuales0,
Unity/Java/vision exit0, sin forced. Sólo FINAL PHASE0 AUDIT pendiente; no ejecutado.
