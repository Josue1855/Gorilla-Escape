# Incremento 1 — Unity Foundation

**Fecha:** 2026-10-06. **Estado:** Unity Foundation / Development environment **PASS**, cierre autorizado para commit y PR a develop; integración pendiente de revisión/checks. Presentation hardware **BLOCKED / NOT RUN**; rendimiento físico **NOT RUN**. Fase 0 **IN PROGRESS**, Incremento 2/Fase 1 **NOT STARTED**.
**Rama:** `feature/gameplay-phase0-unity-foundation-validation`.
**Base:** `e950bd820ea2dd6ede265943a3b7702f445be467`. Base de las ejecuciones previas al commit de cierre. El SHA identifica esa base; el inventario SHA-256 de los 76 archivos Unity/Shared identifica las fuentes realmente ejecutadas.
**Evidencia:** [UNITY-001](TEST_REPORT.md#unity-001--foundation-de-desarrollo) y [registro de ejecuciones](evidence/unity-foundation-2026-10-06/runs.json). [Histórico 1A/auditoría](PHASE0_UNITY_FOUNDATION.md) conservado; sus bloqueos/recomendaciones de versión quedan sustituidos por DEC-003 y estas ejecuciones.

## Configuración candidata revisada

| Elemento | Configuración | Justificación |
|---|---|---|
| Editor | 6000.3.23f1, revisión 09d2ecc7fb28 | Versión mantenida por instrucción del PO |
| Test Framework | 1.6.0, core/builtin, directo | MinimumVersion/bundled del editor; DEC-003 |
| Contratos | file:../../Shared/CSharp | Paquete propio ya existente |
| jsonserialize | 1.0.0, builtin, directo | Fixture/DTO existente |
| NUnit | 2.0.5, builtin, transitivo | Resolución core compatible; no entrada redundante en manifest |
| IMGUI | 1.0.0, builtin, transitivo | Dependencia del Test Framework |
| SDK/toolchain/sysroot Linux | Ausentes del manifest/lock compartido | No necesarios para este build Mono comprobado |
| Development validation | Linux x86_64, Mono, Development build | PC actual; no selecciona el backend/plataforma de presentación |
| Presentation validation | Pendiente | PC oficial no definida; NOT RUN |

El lockfile tiene cinco paquetes: tres directos y dos transitivos. Fue resuelto por Unity/UPM y copiado del resultado revisado; no se editaron sus entradas manualmente. Clean import y re-import mantienen su contenido exacto.

## Toolchains Linux y entorno del editor

Unity añadió sdk/toolchain al abrir una copia limpia y los volvió a añadir después de una retirada por UPM. La documentación describe la instalación automática al seleccionar Linux/IL2CPP y su desactivación en **Edit → Project Settings → Toolchain Management**, seguida de retirada en Package Manager. [Manual Unity](https://docs.unity3d.com/6000.3/Documentation/Manual/linux-il2cpp-crosscompiler.html).

La casilla se desactivó mediante eventos de interfaz del editor (EditorWindow.SendEvent); lectura diagnóstica confirmó false. El editor almacena esa opción mediante PlayerPrefs, no como configuración compartida del proyecto. Ese cambio local no bastó para impedir la migración en el siguiente batch. Se inspeccionó únicamente código/metadatos instalados del editor para identificar sus opciones de desactivación; no se invocaron setters privados.

Para **esta versión y este host** se usaron `UNITY_DISABLE_LINUX_TOOLCHAIN_MIGRATOR=1` y `UNITY_DISABLE_LINUX_AUTO_TOOLCHAIN_INSTALLATION=1`, opciones reconocidas por el editor instalado. Los logs confirman desactivación. Son detalles del procedimiento local, no dependencias ni política para todo el equipo; no están presentados como API pública estable para otras versiones. La vía documentada para una sesión interactiva sigue siendo la casilla y Package Manager. No atribuir reproducibilidad sin flags a estas ejecuciones.

La retirada se hizo con la API pública `UnityEditor.PackageManager.Client.AddAndRemove`, sin editar el lock. Se bloqueó temporalmente el reload de assemblies para conservar el callback de la petición hasta su conclusión; resultado Success y salida 0. El helper de preparación queda local. El build definitivo usa Mono y se ejecutó desde una copia nueva sin Library/PackageCache de proyecto y sin ninguno de los tres paquetes Linux. Esto prueba que no son necesarios para este build; no afirma compatibilidad con IL2CPP u otros hosts.

La sesión original del checkout reintrodujo paquetes durante la revisión; se cerró normalmente y se repitió la aceptación con el checkout estable. Una sesión posterior volvió a agregar toolchains y com.unity.ai.assistant/com.unity.ai.inference; se respaldaron manifest/lock/ProjectSettings/settings AI bajo Unity/Logs/FoundationValidation/2026-10-06/concurrent-root-backup, se cerró normalmente y se restauraron los archivos de paquetes exactamente desde la copia resuelta/validada por UPM. Comprobación final: 76/76 hashes fuente iguales al candidato ejecutado. Los paquetes AI no se adoptan ni se prueban como parte de foundation. No mantener un editor abierto sobre las fuentes mientras otra validación ajusta paquetes/settings.

## Licencia

El editor interactivo vía Hub resuelve entitlement. Los dos intentos históricos desde shell directo salieron 198: siguen siendo BLOCKED, no se reclasifican como PASS.

El Hub instalado es Flatpak. Ejecutar el editor con `flatpak run --command=... com.unity.UnityHub` conserva su entorno y permite batch licenciado, tests y build. No se copiaron tokens, archivos de autenticación ni licencias; no se pasaron secretos ni argumentos de activación. Los logs contienen aviso de token no disponible al actualizar, seguido de entitlement resuelto y ejecución efectiva. PASS se limita a este launcher, no al comando directo del host ni a otros equipos.

## Archivos compartidos y locales

| Archivo generado | Evaluación / incorporación candidata |
|---|---|
| Bootstrap.unity + .meta | Aceptados: un Bootstrap, script correcto, referencia a ApplicationSettings y escena habilitada |
| ApplicationSettings.asset + .meta | Aceptados: TargetFrameRate 60; no FPS medidos |
| FoundationSceneTests.cs.meta | Generado por Unity; acompaña test nuevo de assets persistidos |
| Shared/CSharp/package.json.meta | Aceptado: metadato del paquete local; preservado del checkout |
| packages-lock.json | Aceptado: cinco paquetes resueltos por UPM |
| ProjectVersion.txt | Aceptado: revisión exacta del mismo editor |
| ProjectSettings.asset | Aceptado: identidad/producto y configuración de Player, Mono para desarrollo; GUID de proyecto estable. Placeholder de consola no usada fijado a 32 ceros mediante PlayerSettings.PS4.passcode; no credencial privada ni elección de target consola. Vacío se regeneraba en build, por eso se comprobó estabilidad del placeholder |
| EditorBuildSettings.asset | Aceptado: Bootstrap habilitada y GUID coherente |
| QualitySettings.asset | Aceptado: defaults del editor, sin tuning ni pruebas físicas; VSync/default Ultra no prueban 60 FPS |
| GraphicsSettings.asset | Aceptado: pipeline incorporado y defaults, sin URP/plugin nuevo |
| DynamicsManager.asset | Aceptado: baseline PhysX del editor; no optimización/calibración validada |
| TimeManager.asset | Aceptado: timestep/defaults; no aceptación de métricas de física |
| TagManager.asset | Aceptado: tags/layers por defecto compartidos |
| InputManager.asset | Aceptado: baseline legacy del editor; sin nuevos controles físicos o gameplay |
| AudioManager.asset | Aceptado: configuración por defecto del Player; sin clips/integración nueva |
| UnityConnectSettings.asset | Aceptado: servicios de proyecto deshabilitados; no vinculación cloud |
| EditorSettings.asset | Ya versionado, sin cambio: Visible Meta Files y Force Text |
| ProjectSettings/Packages/com.unity.ai.assistant/Settings.json | Local/rechazado: paquete AI introducido por sesión concurrente, fuera de foundation; preservado/ignorado |
| PackageManagerSettings.asset | Local/rechazado: estado de UI/sesión e instance IDs; ignorado |
| SceneTemplateSettings.json | Local/rechazado: selección de plantillas del editor, innecesaria para escena ya guardada |
| ClusterInputManager.asset | Rechazado: sistema de cluster no usado; defaults regenerables |
| MemorySettings.asset | Rechazado: allocators por defecto, sin perfilado/tuning aprobado |
| MultiplayerManager.asset | Rechazado: defaults de multiplayer no usado; no implementación de red |
| NavMeshAreas.asset | Rechazado: navegación no usada |
| Physics2DSettings.asset | Rechazado: física 2D no usada |
| PresetManager.asset | Rechazado: sin presets compartidos |
| VFXManager.asset | Rechazado: VFX no usado |
| VersionControlSettings.asset | Rechazado: proveedor/default local redundante con EditorSettings |
| Library, Temp, Logs, UserSettings, Builds y helpers de diagnóstico | Locales/ignorados; nunca fuente compartida |

Los archivos locales generados se conservan en disco cuando existen; .gitignore evita adoptarlos accidentalmente. No se importaron indiscriminadamente settings. Los settings necesarios permanecieron idénticos durante la secuencia definitiva y la reimportación.

## Procedimiento reproducible de desarrollo

Usar una copia aislada de Unity/Assets, Unity/Packages, los ProjectSettings compartidos y Shared. Mantener la misma estructura relativa para el paquete C#. Excluir Library, Temp, UserSettings y Builds. Cerrar editores que usen esa copia; no borrar el Temp de una sesión ajena. Logs/XML deben estar fuera de Temp.

En este host, ejemplo del launcher (rutas de proyecto/evidencia deben ser absolutas):

```sh
GORILLA_EDITOR="/ruta/editor/6000.3.23f1/Editor/Unity"
GORILLA_PROJECT="/ruta/absoluta/copia/Unity"
GORILLA_EVIDENCE="/ruta/absoluta/evidencia"
flatpak run --env=UNITY_DISABLE_LINUX_TOOLCHAIN_MIGRATOR=1 \
  --env=UNITY_DISABLE_LINUX_AUTO_TOOLCHAIN_INSTALLATION=1 \
  --command="$GORILLA_EDITOR" com.unity.UnityHub \
  -batchmode -nographics -quit -projectPath "$GORILLA_PROJECT" \
  -logFile "$GORILLA_EVIDENCE/import.log"
```

Con ese mismo launcher, ejecutar secuencialmente:

1. Clean import con `-quit`; comprobar compilación, resolución de cinco paquetes y ausencia de drift.
2. Verificar escena/settings existentes. Para generarlos en un proyecto que aún no los tiene, `-quit -executeMethod GorillaEscape.Editor.FoundationSetup.CreateScene`. Abre escena nueva antes de cargar settings para evitar su descarga; nunca fabricar YAML/meta.
3. EditMode: `-runTests -testPlatform EditMode -testResults /ruta/editmode.xml`, **sin -quit**. Esperar XML con 2 casos Passed.
4. PlayMode: `-runTests -testPlatform PlayMode -testResults /ruta/playmode.xml`, **sin -quit**. Esperar XML con 1 caso Passed.
5. Build: `-quit -executeMethod GorillaEscape.Editor.FoundationSetup.BuildDevelopmentLinux`. Salida en Unity/Builds/FoundationLinux; exigir Succeeded y artefactos, no sólo exit 0.
6. Lanzar GorillaEscape.x86_64 con `-force-glcore -screen-fullscreen 0 -screen-width 640 -screen-height 360 -logFile /ruta/player.log`. Comprobar ventana, renderer OpenGL y foundation ready. Escena intencionalmente vacía: no cámara de juego ni gameplay.
7. Cerrar normalmente la ventana. En la automatización se envió WM_DELETE_WINDOW, sin kill al Player. Exigir salida 0, foundation shutdown y cleanup de módulos.
8. Otra copia nueva del mismo inventario fuente, sin Library/Temp/UserSettings; repetir import. Comparar los 76 SHA-256, manifest/lock, GUIDs/referencias y settings. Esto no prueba Windows/PC oficial ni una sesión de gameplay offline.

## Cambios de código y límites

FoundationSetup ahora crea la escena antes de cargar ApplicationSettings: NewScene descargaba el asset cargado anteriormente y se guardaba settings=fileID 0. La primera prueba de assets falló realmente; se corrigió el orden y la prueba pasó. Se guarda/importa el asset y se marca/guarda la escena mediante APIs del editor. Se añade build Linux/Mono explícito con evaluación de BuildReport.

FoundationSceneTests verifica el archivo persistido: un Bootstrap, ApplicationSettings correcta/60, escena incluida y cero scripts faltantes. El test del fixture y el PlayMode existentes se conservan. Bootstrap añade únicamente un mensaje OnApplicationQuit para evidencia de cierre. Sin gameplay, cámara, IPC, Java, WebSocket ni sensores.

Warnings: aviso de actualización de token del launcher, mensaje de cleanup `build-server` sin SDK del editor y depuración unable-to-listen observados; no impidieron compilación, XML ni build. El Player de desarrollo puede emitir un registro nativo MemoryLeaks al shutdown: no se afirma cero fugas ni perfilado de memoria. Relevancia y extractos de la ejecución definitiva en el reporte.

Las ejecuciones exploratorias alojadas en Unity/Temp fueron limpiadas al cerrar el editor original. Se registra esa pérdida; no sostienen la aceptación actual. La secuencia final fue repetida y sus logs/XML permanecen bajo Unity/Logs y los extractos revisados bajo docs/evidence.

**Clasificación de diagnósticos:** ENVIRONMENT / REQUIRES FOLLOW-UP en [deuda #89](https://github.com/Josue1855/Gorilla-Escape/issues/89). No hay evidencia que los atribuya al código Gorilla Escape. La instrucción de cierre del PO acepta la foundation de desarrollo y no exige un log vacío como puerta de integración. No se ocultan los mensajes ni se afirma que las métricas físicas hayan sido medidas.

**Cierre:** Development PASS para importación, compilación, paquetes, Bootstrap/settings, EditMode 2/2, PlayMode 1/1, build Linux/Mono, Player, cierre y reimportación reproducible. El contenido funcional del commit coincide con los 76 hashes ejecutados; sólo cambia documentación/evidencia sanitizada en este cierre. Los comandos de evidencia usan $REPO/$UNITY_EDITOR para sustituir rutas del host; hashes de archivos raw permiten cotejo local. Las opciones de toolchain son específicas de este host/editor, sin promesa de API estable ni requisito Windows/macOS.

**Próximo paso recomendado, no ejecutado:** decidir la PC/plataforma oficial y cerrar la aceptación de presentación restante del Incremento 1. Decisiones arquitectónicas previas al Incremento 2 sólo si el PO lo autoriza. FPS objetivo real, frame time, latencia, memoria/leaks, sensores, webcam y Player Lock NOT RUN. Fase 0 permanece en progreso.
