# Fase 0 — Histórico del Incremento 1A y auditoría

**Estado actual:** ver [UNITY_FOUNDATION_VALIDATION](UNITY_FOUNDATION_VALIDATION.md) y UNITY-001 en [TEST_REPORT](TEST_REPORT.md). DEC-003 y la continuación autorizada del Incremento 1 sustituyen los bloqueos y recomendaciones de configuración de este histórico. Las menciones de 1.4.2 describen la declaración anterior, no la configuración vigente.

**Fecha:** 2026-10-06. **Estado:** preparación documental realizada; validación Unity BLOCKED por licencia. No cierra el Incremento 1 ni Fase 0.
**Base inspeccionada:** develop, commit e950bd8; sin cambios locales, sincronizado con origin/develop. Rama de trabajo: feature/gameplay-phase0-unity-foundation-validation.
**Autoridad:** especificación v4 y decisiones aceptadas; reglas de desarrollo; documentos operativos como evidencia, sin sustituir requisitos. La solicitud actual limita este trabajo al Spike técnico y prohíbe iniciar Fase 1.

## Inventario verificado

| Elemento | Estado real | Evidencia / límite |
|---|---|---|
| Bootstrap y ApplicationSettings | Implementados como fuentes; no probados en Unity | Awake aplica frame rate; OnDestroy lo restaura. No es una sesión de juego |
| FoundationSetup | Preparado; no ejecutado | Menú de editor crea escena/settings y registra la escena para build |
| Tests Unity | Preparados; NOT RUN | Un test EditMode de fixture y un PlayMode de frame rate/restauración |
| Escena Bootstrap / ApplicationSettings.asset | Pendientes | No existen los assets generados ni sus .meta |
| Paquetes | Declarados; resolución BLOCKED | UPM local Shared/CSharp, Test Framework 1.4.2, jsonserialize 1.0.0 |
| packages-lock.json | Generado externamente durante el trabajo; no aprobado | Ausente al inicio; discrepancia de versión detectada en revisión final |
| Editor 6000.3.23f1 | Instalado; ejecución batch BLOCKED | Dos intentos batch terminan con código 198 por licencia; editor vía Hub abierto en paralelo, sin validación completa |
| Player PC / cierre / logs de Player | NOT RUN | No existe build que permita probarlos |
| Java y React | Implementados; probados anteriormente | BASE-001/PUB-001 en TEST_REPORT; no se volvieron a ejecutar sus suites en este cambio documental |
| Protocolo | DTOs y fixture implementados; detalles documentados parcialmente | No hay handler de juego, handshake ni sensores |
| IPC, cámara, Player Lock, métricas físicas | Pendientes y no probados | Guías de adaptadores; sin integración real |
| Hardware oficial | Pendiente de identificación | PC Linux de desarrollo accesible; no acredita PC oficial, teléfono, webcam o router |

La validación estática de archivos no demuestra importación, compilación ni integración física.

## Plan del incremento

Objetivo: obtener evidencia reproducible de la foundation antes de integrar red o cámara. Entregables 1A: inventario, intento limpio, registro del bloqueo, procedimiento y criterios verificables. Dependencias: licencia válida del editor fijado y módulo de build PC correspondiente. Riesgos: licencia; errores de compilación aún desconocidos; diferencias con la PC oficial. Hardware: PC de desarrollo para import/tests/build y PC oficial para aceptación del Player. Teléfonos y cámara no son necesarios en este incremento.

La continuación del Incremento 1 debe generar/revisar assets y lockfile, ejecutar EditMode/PlayMode, construir y abrir el Player y verificar su cierre. Cada ejecución debe registrar comando, código de salida, logs, XML, versiones y hardware. No aceptar un test por código de salida sin XML y casos ejecutados.

## Discrepancias antes de integrar

Estas son propuestas pendientes, no decisiones aceptadas. No se modifica DECISIONS ni la especificación.

| Discrepancia | Documentos / código afectados | ¿Bloquea 1A? | Propuesta para resolver antes del trabajo dependiente |
|---|---|---|---|
| §42 exige Vertical Slice de Smash como salida del Spike; §50 lo sitúa en Fase 1 | v4 §42/§50 y §44.12; DEVELOPMENT_PROGRESS | No | Mantener ahora foundation técnica según solicitud. Antes de cerrar Fase 0, acordar con PO la puerta técnica y la puerta del slice; actualizar criterios aprobados. No declarar §42 satisfecho |
| PC autoritativa no identifica runtime que confirma resultado; §5 menciona servidor | v4 §3/§5/§51; MONOREPO_PLAN; DTOs sin autoridad | No | Proponer Unity como único dueño de física/scoring/resultados y Java como transporte/host. Acordar confirmación y recuperación antes del Incremento 2; no implementar segundo scoring |
| IPC y ownership/lifecycle sin definir | MONOREPO_PLAN, TEAM_START; Network/README y servidor sin IPC | No | Comparar una conexión local Unity↔Java con alternativas locales; acordar dueño de procesos, readiness, timeout, fallos, recuperación y cleanup antes de programar |
| sensor minúscula frente a SENSOR y otros tipos mayúscula | v4 §8.2/§8.3; fixture sensor.json, DTOs y tests Java/JS/C# | No | Acordar convención canónica y estrategia ante tipos desconocidos; actualizar fixture y todos los consumidores juntos antes del handler |
| Unidades y reloj sin contrato completo | v4 §8–§10; timestamp de fixture y escalares DTO | No | Definir aceleración con/sin gravedad, unidad/ejes del gyro, convención quaternion y timestamp: unidad, origen, sincronización y validación de antigüedad. No inferirlos del número de ejemplo |
| sequence detecta atrasos pero no define alcance/reset | v4 §8.7; DTO/fixture | No | Acordar contador por jugador/dirección, límite y política al reconectar; distinguir duplicado, antiguo y salto sin perder acciones confirmadas |

Responsables de acuerdos: PO para alcance; Arturo/Hiram para autoridad, IPC y protocolo; Austin para reloj/coordenadas de cámara. La aprobación debe registrarse cuando exista; la propuesta no autoriza implementación.

### Cambios externos durante la revisión final

Se observó un editor iniciado vía Hub sobre el checkout mientras se redactaba esta guía. Generó ProjectSettings, packages-lock.json, Shared/CSharp/package.json.meta y cambios en manifest/ProjectVersion. Se preservan sin adoptar ni revertir. No son entregables de 1A ni prueban importación limpia completa, tests o build.

El lockfile resuelve Test Framework **1.6.0** aunque manifest declara **1.4.2** (TEAM_START/MONOREPO_PLAN). Manifest recibió com.unity.sdk.linux-x86_64 y com.unity.toolchain.linux-x86_64-linux **1.1.0**. Esto bloquea aprobar los paquetes actuales del Incremento 1, pero no la documentación 1A. Propuesta: investigar la resolución efectiva del editor fijado, explicar necesidad/alternativas/impacto de los paquetes Linux y acordar versiones antes de versionarlos. No corregir el lockfile a mano ni dar por aprobada una actualización automática.

La licencia de la sesión Hub no se diagnostica desde el fallo batch: pueden diferir sus entornos. El arranque batch se reintentó después de observar el editor; volvió a salir con 198 y el mismo mensaje (log local /tmp/gorilla-unity-clean-hat2_zjh/import-retry.log). Se debe verificar el entorno/licencia de esa invocación y, si procede, ejecutar las pruebas desde Test Runner en la sesión válida del editor.

## Evidencia del intento limpio

Se copiaron únicamente Unity/Assets, Unity/Packages, Unity/ProjectSettings y Shared a un directorio temporal nuevo. Sin Library, Temp ni UserSettings previos. Se comprobó que Library no existía antes del arranque. No se alteraron las fuentes del checkout.

Comando ejecutado (con timeout externo de 60 s, no alcanzado):

```sh
$UNITY_EDITOR -batchmode -nographics -quit -projectPath /tmp/gorilla-unity-clean-hat2_zjh/Unity -logFile /tmp/gorilla-unity-clean-hat2_zjh/import.log
```

Código de salida: **198**. Extracto mínimo del log:

```text
Unity Editor version:    6000.3.23f1 (09d2ecc7fb28)
No valid Unity Editor license found. Please activate your license.
```

Log completo local: /tmp/gorilla-unity-clean-hat2_zjh/import.log (temporal, no versionado; puede desaparecer al limpiar /tmp). Este extracto conserva la evidencia del bloqueo sin versionar identificadores de licencia/máquina ni datos de red. No demuestra un error del código ni una importación aprobada.

Condiciones: Linux x86_64, kernel 7.1.5-76070105-generic; editor en batch sin gráficos; módulos Linux/Windows/macOS presentes. No se activó licencia ni se modificaron credenciales. La PC oficial no está confirmada.

## Procedimiento pendiente con licencia válida

1. Activar una licencia válida con Unity Hub por el titular. Usar 6000.3.23f1. Cerrar cualquier editor que utilice el proyecto a verificar. No incluir credenciales en comandos ni logs compartidos.
2. Desde la raíz de la rama, preparar una copia limpia y conservar Shared como hermano de Unity. Los comandos siguientes son para Bash/Linux con Python 3; ejecutar cada paso por separado y detenerse ante cualquier error. No reutilizar resultados anteriores.

```sh
export GORILLA_UNITY_EDITOR=$UNITY_EDITOR
export GORILLA_VALIDATION_DIR="$(mktemp -d /tmp/gorilla-unity-validation-XXXXXX)"
python3 - <<'PY'
import os, shutil
from pathlib import Path
root = Path.cwd()
stage = Path(os.environ['GORILLA_VALIDATION_DIR'])
for name in ('Assets', 'Packages', 'ProjectSettings'):
    shutil.copytree(root / 'Unity' / name, stage / 'Unity' / name)
shutil.copytree(root / 'Shared', stage / 'Shared')
(stage / 'evidence').mkdir()
(stage / 'Unity' / 'Builds').mkdir()
assert not (stage / 'Unity' / 'Library').exists()
print(stage)
PY
"$GORILLA_UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$GORILLA_VALIDATION_DIR/Unity" -logFile "$GORILLA_VALIDATION_DIR/evidence/import.log"
echo "Import exit: $?"
```

3. Exigir salida 0, ausencia de errores de compilación/resolución y packages-lock.json generado. Revisar versiones y rutas locales contra manifest. Abrir la copia en el editor para inspeccionar Package Manager y Console. Si hay una dependencia/actualización nueva, resolverla dentro de las reglas del proyecto, sin adoptarla automáticamente.
4. Ejecutar Gorilla Escape → Create Foundation Scene en la copia limpia. Revisar Bootstrap.unity, su componente Bootstrap con ApplicationSettings asignado y TargetFrameRate 60, ApplicationSettings.asset y .meta. Revisar la escena habilitada en Build Profiles/Scene List. Guardar y cerrar el editor. No agregar minijuegos ni servicios vacíos. El menú conserva una escena existente: eso no demuestra que esté bien configurada.
5. Ejecutar los tests uno por uno. No agregar -quit a las invocaciones de tests: debe terminar el Test Runner. Si se bloquean, registrar timeout y terminar sólo el proceso de esa ejecución.

```sh
"$GORILLA_UNITY_EDITOR" -batchmode -nographics -projectPath "$GORILLA_VALIDATION_DIR/Unity" -runTests -testPlatform EditMode -testResults "$GORILLA_VALIDATION_DIR/evidence/editmode.xml" -logFile "$GORILLA_VALIDATION_DIR/evidence/editmode.log"
echo "EditMode exit: $?"
"$GORILLA_UNITY_EDITOR" -batchmode -nographics -projectPath "$GORILLA_VALIDATION_DIR/Unity" -runTests -testPlatform PlayMode -testResults "$GORILLA_VALIDATION_DIR/evidence/playmode.xml" -logFile "$GORILLA_VALIDATION_DIR/evidence/playmode.log"
echo "PlayMode exit: $?"
```

Exigir XML nuevos con al menos ProtocolContractTests.UnityReadsTheSameWireFixtureAsJavaAndReact y BootstrapTests.BootstrapAppliesAndRestoresConfiguredFrameRate ejecutados y aprobados, sin fallos ni casos omitidos que sustituyan los críticos. Repetir en el editor con gráficos; batch sin gráficos no acredita renderizado ni FPS. Conservar XML/logs y revisar warnings, no sólo el resumen.

6. Construir el Player Linux mínimo desde la copia con la escena habilitada:

```sh
"$GORILLA_UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$GORILLA_VALIDATION_DIR/Unity" -buildTarget StandaloneLinux64 -buildLinux64Player "$GORILLA_VALIDATION_DIR/Unity/Builds/GorillaEscape.x86_64" -logFile "$GORILLA_VALIDATION_DIR/evidence/build-linux.log"
echo "Build exit: $?"
```

Exigir salida 0, build sin errores y artefactos ejecutables. Para Windows, utilizar el editor/módulo correspondiente y Build Profiles → Windows → Build, con la misma escena; conservar el log de build. Una build Linux no acredita Windows ni la PC oficial.

7. En PC Linux con escritorio/gráficos, abrir el Player y registrar log:

```sh
"$GORILLA_VALIDATION_DIR/Unity/Builds/GorillaEscape.x86_64" -logFile "$GORILLA_VALIDATION_DIR/evidence/player.log"
echo "Player exit: $?"
```

Verificar una inicialización con el mensaje foundation ready, sin errores/exception. Esta escena vacía no ofrece una UI jugable; no asumir que una pantalla vacía representa gameplay funcional. Cerrar mediante el control de ventana del sistema; exigir salida normal y ausencia de crash/proceso residual de ese Player. Repetir apertura/cierre tres veces con logs separados. No forzar kill y declararlo cierre limpio. Si el cierre está bloqueado, registrar FAIL y corregir en un incremento revisable. No hay FPS o latencia medidos por este procedimiento.

8. Antes de incorporar archivos generados, revisar diff de escena, settings, sus .meta, ProjectSettings y lockfile; copiar sólo fuentes/configuración revisadas al checkout. No copiar Library, Temp, UserSettings, build ni logs sin revisar. Ejecutar validadores, tests y build sobre las fuentes finales; actualizar TEST_REPORT con hardware, conteos, comandos, código de salida y ubicación duradera de evidencia sanitizada. Repetir importación en otra copia limpia de esas fuentes finales para validar el lockfile versionado. Mantener PR revisado a develop; no integrar sin revisión.

Referencia de comandos: [Unity 6.3 CLI](https://docs.unity.com/en-us/engine/6000.3/manual/unity-editor/command-line-arguments/editor) y [Test Framework 1.4 CLI](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html). La guía está preparada, no ejecutada después del bloqueo.

## Criterios al cierre de 1A (históricos)

| Criterio | Estado | Evidencia necesaria restante |
|---|---|---|
| Inventario y copia limpia sin caché previa | PASS | Sólo preparación de validación |
| Importación/compilación Unity | BLOCKED | Licencia válida y log de importación correcta |
| Resolución de paquetes / lockfile revisado | BLOCKED | Importación y revisión del archivo generado |
| Escena Bootstrap y settings generados/verificados | NOT RUN | Editor, assets y referencias correctas |
| EditMode y PlayMode | NOT RUN | XML/logs nuevos con casos ejecutados |
| Build mínimo PC | NOT RUN | Log y artefactos del build |
| Arranque, logs y cierre limpio del Player | NOT RUN | Ejecución real en PC con gráficos |
| Hardware oficial | BLOCKED | Identificar y ejecutar en PC objetivo |
| Validación estática de foundation y documentos | PASS | Resultados en TEST_REPORT; no equivalen a tests Unity |
| Sensores, cámara, QR, red, FPS y latencia física | NOT APPLICABLE | Fuera de 1A; pendientes en Fase 0 |

## Seguridad, rendimiento y riesgos

Sin captura de cámara, sensores ni movimientos físicos. No se añadieron red, CORS, dependencias, certificados o secretos. Logs completos permanecen locales; compartir sólo evidencia revisada. Para futuros ensayos mantener teléfono sujeto, espacio libre y movimientos controlados. No hay validación de hardening o seguridad física de gameplay en 1A.

Rendimiento: **NOT RUN** para FPS Unity/webcam, sensores Hz, frame time, latencia P50/P95/máximo, calibración, descartes, duplicados, muestras antiguas y reconexiones. No se infiere rendimiento del tiempo de salida por licencia. Crashes y Player Lock de sesión: NOT RUN.

Riesgos reales: licencia del arranque batch bloqueante; compilación Unity no verificada; hardware oficial sin identificar; discrepancias de alcance/contrato y versiones de paquetes abiertas. Los archivos generados por la sesión externa requieren revisión independiente. No se versionan assets/lockfile fabricados ni se declara Done.

Próximo incremento recomendado: continuar Incremento 1 con licencia válida y ejecutar el procedimiento anterior. El Incremento 2 sólo debe comenzar después de la evidencia de foundation y de acuerdos de autoridad/IPC/lifecycle. No se ejecuta automáticamente.

## UNITY-001A-AUDIT — Diagnóstico de reproducibilidad

**Fecha:** 2026-10-06. Continuación exclusivamente documental de 1A; sin cambiar versiones, archivos técnicos, índice Git ni decisiones. Este diagnóstico precede al procedimiento de validación anterior: no ejecutarlo aún sobre el checkout abierto o sobre la configuración generada sin aprobar.

### Estado de partida y procesos

SHA exacto de HEAD/base: **e950bd820ea2dd6ede265943a3b7702f445be467**. Rama: **feature/gameplay-phase0-unity-foundation-validation**. OS observado: Pop!_OS, Linux x86_64, kernel 7.1.5-76070105-generic. Editor observado en ProjectVersion/log: **6000.3.23f1 (09d2ecc7fb28)**. No se ejecutó otro arranque ni una nueva importación durante esta auditoría.

Estado Git inicial: modificados Unity/Packages/manifest.json, Unity/ProjectSettings/ProjectVersion.txt, docs/DEVELOPMENT_PROGRESS.md y docs/TEST_REPORT.md; sin seguimiento el lockfile, 19 settings, Shared/CSharp/package.json.meta y esta guía. Las fuentes C#/asmdef, metadatos preexistentes y EditorSettings.asset no presentan diff. Los archivos de documentación proceden del trabajo previo, no del editor. No había escena Bootstrap ni ApplicationSettings.asset al inspeccionar.

Se detectó un editor principal, PID 94091, sobre este proyecto, iniciado vía Hub, y dos procesos de importación de assets (95860/95862), no dos editores interactivos independientes. También existe el cliente de licencia del Hub. Se preservaron todos los procesos; no se cerraron ni se lanzaron otros. La inspección registró solamente ejecutable, ruta de proyecto, rol y presencia de flags de Hub/autenticación, sin copiar sus valores secretos.

### Archivos generados por Unity

Origen observado: archivos que aparecieron durante la sesión externa del editor y resolución UPM; no hay trazabilidad para atribuir cada acción a una persona. La clasificación no aprueba su incorporación. Para archivos no versionados, se revisó su contenido completo como incorporación propuesta, no sólo git diff (que no los muestra).

| Archivo | Origen / contenido relevante | Clasificación |
|---|---|---|
| Unity/Packages/manifest.json | UPM agregó dos dependencias directas Linux; conservó 1.4.2 y el resto de entradas originales | EXPECTED BUT REQUIRES REVIEW |
| Unity/Packages/packages-lock.json | UPM: ocho paquetes resueltos; core 1.6.0 y tres paquetes del SDK/toolchain | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/ProjectVersion.txt | Editor añadió revisión 09d2ecc7fb28; no cambió 6000.3.23f1 | EXPECTED |
| Shared/CSharp/package.json.meta | PackageManifestImporter añadió GUID; no modificó package.json | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/AudioManager.asset | Defaults de audio; sin plugins asignados | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/ClusterInputManager.asset | Lista de entradas vacía | EXPECTED |
| Unity/ProjectSettings/DynamicsManager.asset | Defaults de física 3D, gravedad -9.81 y matriz de colisión | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/EditorBuildSettings.asset | Lista de escenas vacía; no está listo para build de foundation | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/GraphicsSettings.asset | Pipeline integrado, referencias internas; sin SRP personalizado | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/InputManager.asset | Ejes/botones legacy por defecto; no implementa sensores | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/MemorySettings.asset | Overrides -1 y mapa de plataformas vacío; no detecta RAM ni fija presupuesto probado | EXPECTED |
| Unity/ProjectSettings/MultiplayerManager.asset | Roles y despliegues deshabilitados | EXPECTED |
| Unity/ProjectSettings/NavMeshAreas.asset | Áreas y agente Humanoid por defecto; no implica navegación implementada | EXPECTED |
| Unity/ProjectSettings/PackageManagerSettings.asset | Registry oficial; estado de UI e instance IDs de la sesión incluidos | ENVIRONMENT-SPECIFIC |
| Unity/ProjectSettings/Physics2DSettings.asset | Defaults de física 2D y matriz de colisiones | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/PresetManager.asset | Sin presets por defecto | EXPECTED |
| Unity/ProjectSettings/ProjectSettings.asset | Player defaults: DefaultCompany/Unity; GUID de producto generado, campos de otras plataformas y scriptingBackend vacío | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/QualitySettings.asset | Seis niveles de calidad; defaults por plataforma, incluidos Linux/Windows vía Standalone; VSync requiere revisión antes de medir FPS | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/TagManager.asset | Layers/tags estándar, sin tags de producto | EXPECTED |
| Unity/ProjectSettings/TimeManager.asset | Timestep y límites por defecto; no constituye tuning aceptado | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/UnityConnectSettings.asset | Servicios, analytics, ads y reporting deshabilitados; URLs estándar presentes | EXPECTED BUT REQUIRES REVIEW |
| Unity/ProjectSettings/VFXManager.asset | Defaults, sin recursos personalizados | EXPECTED |
| Unity/ProjectSettings/VersionControlSettings.asset | Visible Meta Files; sin sistema externo activado | EXPECTED |

**Total:** dos archivos técnicos versionados modificados y 21 archivos técnicos nuevos sin seguimiento (19 settings, lockfile y .meta). EditorSettings.asset permanece intacto. Se verificaron los .meta originales mediante el validador: 35 GUIDs únicos incluyendo el nuevo. El GUID nuevo debe conservarse una vez aprobado, no regenerarse entre compañeros. Los GUIDs generados no prueban igualdad byte a byte entre importaciones nuevas.

Library, Temp, Logs y UserSettings son salidas locales ignoradas, **ENVIRONMENT-SPECIFIC**; no se incorporan. Library/PackageManager/ProjectCache sirve como evidencia de resolución, no como configuración versionable. No hay paquete Linux agregado dentro de ProjectSettings: QualitySettings contiene nombres de plataformas estándar, y el objetivo activo se guarda también en Library/EditorUserBuildSettings.asset (local, binario). Los defaults multiplataforma no acreditan que se hayan solicitado builds de esas plataformas.

No se encontraron rutas absolutas de usuario en Packages o ProjectSettings revisados. Sí hay datos generados no deterministas y estado de UI; por ello no debe adoptarse indiscriminadamente el conjunto. ProjectSettings incluye identificadores/campos de empaquetado para plataformas fuera del alcance; revisar antes de publicar, sin copiar sus valores al reporte ni editarlos manualmente para esta auditoría.

### Test Framework 1.4.2 frente a 1.6.0: causa técnica

1. **Directa:** la entrada exacta es `dependencies["com.unity.test-framework"]` del lockfile: version **1.6.0**, depth **0**, source **builtin**. No la introducen los paquetes Linux ni una dependencia transitiva.
2. **Requisito sin reescritura:** tanto `git show HEAD:Unity/Packages/manifest.json` como el manifest actual declaran **1.4.2**. El diff del manifest sólo agrega sdk/toolchain Linux. Es resolución efectiva distinta de declaración, no edición de esa versión.
3. **Regla del editor:** el archivo instalado `Editor/Data/Resources/PackageManager/Editor/manifest.json`, entrada packages.com.unity.test-framework (línea 824), declara `minimumVersion: 1.6.0`, `mustBeBundled: true`, `version: 1.6.0`. No es una recomendación de latest desde Internet.
4. **Paquete distribuido:** `Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.test-framework/package.json` declara version **1.6.0**, unity **6000.0**, unityRelease **44f1** y dependencias NUnit **2.0.3**, imgui/jsonserialize **1.0.0**.
5. **Resolución observada:** Library/PackageManager/ProjectCache, líneas 226–326: `isDirectDependency: 1`, version **1.6.0**, `minimumPackageVersion: 1.6.0`, ruta resuelta y `errors: []`. Editor.log lista 1.6.0 bajo Built-in packages tras crear el lockfile. Esto demuestra resolución en esa sesión, no validación completa del proyecto.
6. **NUnit:** el requisito de Test Framework es 2.0.3, pero la entrada raíz de NUnit resuelve **2.0.5**, depth **1**, source **builtin**. Editor/manifest.json, línea 238, fija mínimo/bundled **2.0.5**, coincidente con BuiltInPackages/com.unity.ext.nunit/package.json. Las dependencias dentro de una entrada son requisitos del paquete; las entradas raíz registran las versiones efectivas.
7. **depth:** 0 corresponde a una dependencia directa del proyecto; 1 a un paquete requerido por una directa. NUnit e imgui están a profundidad 1; jsonserialize tiene profundidad 0 porque ya lo declara directamente el proyecto, aunque Test Framework también lo requiere. No mide orden de ejecución, prioridad o número de consumidores.

La causa es el paquete **core** distribuido con el editor y su mínimo, no una actualización transitiva normal que pueda corregirse forzando el lockfile. Unity documenta que Test Framework es core y que su versión acompaña al editor, sin selección independiente desde Package Manager/API: [Test Framework 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/test-framework/test-framework-introduction.html), [Core packages](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-core.html).

**Predicción sustentada:** otra instalación íntegra de 6000.3.23f1 con los mismos metadatos/core, al resolver el mismo commit, debería usar Test Framework 1.6.0 y NUnit 2.0.5 aunque el manifest siga diciendo 1.4.2. No se ha ejecutado esa importación limpia; no se declara PASS de reproducibilidad. Tampoco se demuestra aquí que una instalación Windows de esa revisión tenga exactamente los mismos paquetes internos: comprobarla en el entorno del equipo.

### Paquetes Linux

| Paquete / versión | Quién lo requiere / profundidad | Propósito y dependencia del OS | Versionado y efecto Windows/build |
|---|---|---|---|
| com.unity.sdk.linux-x86_64 1.1.0 | Manifest actual; directa, depth 0; requiere sysroot.base 1.1.0 | Cabeceras/librerías del destino Linux x64; clase SysrootLinuxX86_64 identifica TargetPlatform linux/TargetArch x86_64 | Candidato sólo si Linux forma parte del build acordado. El destino Linux es independiente del host; útil también con un toolchain Windows→Linux correspondiente. No convierte un build Windows en Linux |
| com.unity.toolchain.linux-x86_64-linux 1.1.0 | Manifest actual; directa, depth 0; requiere sysroot.base 1.1.0 | LLVM/Clang/linker para host Linux x64 y destinos Linux; clase ToolchainLinuxX86_64 declara HostPlatform linux/HostArch x86_64 | Específico del host. No es ejecutable Windows ni sustituye toolchain win-x86_64-linux. Si se versiona como directa, también se restaurará en Windows: coste de descarga/caché y posible adición del toolchain apropiado al elegir Linux. Compatibilidad de importación Windows NOT RUN |
| com.unity.sysroot.base 1.1.0 | Requerido por los dos anteriores; transitiva, depth 1 | Código común para localizar, extraer y registrar sysroot/toolchain; no es por sí solo compilador ni SDK de un OS | Debe quedar en el lockfile si se aprueban sus consumidores; no necesita declararse como directa. Infraestructura compartida, sin habilitar automáticamente backend o cambiar target |

Evidencia local: package.json y README de cada paquete en Library/PackageCache; fuentes Editor/Unity.Sysroot.cs, Editor/Unity.Toolchain.cs y Editor/Unity.SysrootPackage.cs. Sus manifiestos exigen Unity 6000.3.0b8 o posterior; el editor instalado recomienda/fija mínimo 1.1.0 para estos nombres, pero `mustBeBundled: false`: se resuelven desde registry, no son el core de Test Framework.

El log UPM recuperado contiene dos peticiones `project:add-dependency` a las **12:09:02, America/Chihuahua (18:09:02Z)**: toolchain.linux-x86_64-linux y sdk.linux-x86_64, ambas sin versión explícita, seguidas de respuesta 200. Eso explica su carácter de dependencias directas y resolución a 1.1.0, compatible con metadatos del editor. El log no identifica el caller ni demuestra cuál fue la acción inicial que los agregó: **UNKNOWN** si fue selección de target, instalación automática o acción de UI en esa sesión. No atribuirla a una persona.

Unity documenta instalación automática del conjunto al elegir Linux con los requisitos de IL2CPP; es un mecanismo compatible con lo observado, **no prueba de que se activó ese mecanismo aquí**. El ProjectSettings generado tiene `scriptingBackend: {}`; no prueba selección explícita de IL2CPP. Las fuentes de los paquetes confirman su uso en la cadena IL2CPP. No se demuestra necesidad para el build mínimo Mono ni para Windows. Referencia: [Linux IL2CPP 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/linux-il2cpp-crosscompiler.html).

Ninguno se elimina ni se acepta ahora. Debe acordarse plataforma/backend de la foundation y verificar Windows antes de oficializar dependencias de host. Versionar manifiesto/lockfile revisados es distinto de versionar payloads, cachés o rutas de instalación; éstos siguen locales.

### Comparación de las tres fuentes

| Área | Configuración versionada e950bd8 | Archivos generados | Lo que espera/permite el editor observado |
|---|---|---|---|
| Test Framework | 1.4.2; sin lockfile | Manifest 1.4.2; lockfile/core 1.6.0 | Core/minimum 1.6.0 distribuido con 6000.3.23f1 |
| NUnit | No declarada directamente | Requisito 2.0.3; resolución 2.0.5 | Core/minimum 2.0.5 |
| Linux | Sin dependencias específicas; módulos locales no versionados | Dos directas 1.1.0 + base transitiva | Admite esas versiones; herramientas según host/target/backend, no obligatorias para todo proyecto PC |
| Settings | EditorVersion y EditorSettings mínimos | Defaults completos; build scenes vacío y datos de sesión/identidad | El editor serializa defaults; deben revisarse como configuración de producto, no como prueba de aceptación |
| Metadatos | GUIDs de assets/scripts versionados | Nuevo GUID de package.json | Identidad generada una vez y estable después de incorporarla; no igualdad entre nuevas generaciones |

**Conclusión técnica:** la pareja declarada editor 6000.3.23f1 / Test Framework 1.4.2 no representa el runner efectivo de este editor. No se fuerza 1.4.2 ni se acepta 1.6.0 por novedad. La configuración oficial candidata debe conservar el editor fijado y describir su core 1.6.0, con manifest/lockfile generados por el flujo soportado una vez aprobada la aclaración de versión. La incorporación debe seleccionar settings compartidos revisados; no copiar los generados indiscriminadamente. Pendiente comprobar clean import, estabilidad del lockfile y host Windows, y acordar Linux/backend.

Si se aprueba esa configuración, actualizar TEAM_START, MONOREPO_PLAN y Unity/Packages/manifest.json; generar/revisar packages-lock.json con Unity. Después ajustar guía/DEVELOPMENT_PROGRESS/TEST_REPORT a las ejecuciones reales y registrar DECISIONS si corresponde a modificación de la versión fijada. La v4 no fija 1.4.2, de modo que no necesita una edición de gameplay/alcance por ese motivo. Nada de esto se realizó en esta continuación.

### Licencia: diferencia de entornos

El editor interactivo usa `FLATPAK_ID=com.unity.UnityHub`; XDG_CONFIG_HOME, XDG_DATA_HOME y XDG_CACHE_HOME apuntan a `.var/app/com.unity.UnityHub/`. Fue iniciado por Hub con autenticación. Su log contiene `Successfully resolved entitlement details` y resolución UPM efectiva. Esto acredita una sesión interactiva que avanzó; no certifica tests ni la disponibilidad de licencia batch.

La sesión de comandos carece de esos overrides/FLATPAK_ID. Los dos logs batch previos contienen `Access token is unavailable; failed to update` y `No valid Unity Editor license found`, salida 198. Son contextos distintos de autenticación/configuración. No se ha probado que igualar entorno resuelva el problema: no se copiaron tokens/licencias ni se alteró entorno para eludir el bloqueo. Diagnóstico pendiente por la vía soportada de Unity Hub para el titular/licencia y la invocación batch.

Último comando batch real y evidencia, anteriores a esta auditoría:

```sh
$UNITY_EDITOR -batchmode -nographics -quit -projectPath /tmp/gorilla-unity-clean-hat2_zjh/Unity -logFile /tmp/gorilla-unity-clean-hat2_zjh/import-retry.log
```

Salida 198; líneas 86 y 96 contienen respectivamente los dos mensajes anteriores. Esta continuación sólo leyó los logs; no ejecutó el comando ni ningún otro arranque.

### Criterios de aceptación del diagnóstico

| Criterio | Estado |
|---|---|
| Auditoría de todos los cambios técnicos visibles y origen acotado | PASS |
| Identificación de causa core/minimum y directa/transitiva | PASS |
| Identificación de propósito y host/target de los tres paquetes Linux | PASS |
| Identificación exacta del caller/trigger inicial Linux (sin traza del emisor) | BLOCKED |
| Verificación de preservación de fuentes y archivos generados | PASS |
| Prueba de importación limpia reproducible del SHA base | BLOCKED |
| Unity batch startup | BLOCKED |
| Clean import | BLOCKED |
| EditMode Tests | NOT RUN |
| PlayMode Tests | NOT RUN |
| PC Build | NOT RUN |
| Player execution | NOT RUN |
| Performance | NOT RUN |
| Import/build del mismo editor en Windows | NOT RUN |
| Incorporación de archivos/versión oficial/commit/PR en esta tarea | NOT APPLICABLE |

La revisión estática no acredita ejecución completa. El diagnóstico de incompatibilidad declarada es una discrepancia de configuración, no un FAIL de tests no ejecutados.

### Recomendación y siguiente paso

**C. Corregir configuración antes de validar.** Hay evidencia suficiente para no presentar 1.4.2 como versión efectiva del runner en este editor. La corrección propuesta requiere aprobación antes de modificar esa versión fijada; no adopta en bloque los archivos generados ni descarta herramientas Linux sin definir su necesidad. Reproducibilidad completa sigue sin comprobarse.

**Único siguiente paso:** acordar/aprobar la configuración de foundation (core del editor, plataforma/backend PC y tratamiento de paquetes de host) a partir de esta auditoría. Después de ese acuerdo podrá prepararse la prueba limpia con licencia batch válida: no se ejecuta automáticamente aquí.
