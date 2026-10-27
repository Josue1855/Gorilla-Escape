# Corrección: Play Gorilla Escape abría el diagnóstico de develop

Fecha: 2026-10-26. Rama: `feature/saul-jungle-party-alpha`.

## Problema observado

El usuario informó que `Gorilla Escape → Play Gorilla Escape` mostraba únicamente el menú técnico de develop. La revisión confirmó que `ProductPlayMode.PlayProduct()` abría `Bootstrap.unity`, mientras el flujo migrado de Saúl comenzaba en `Connection.unity`. La integración del commit `a0fb822` había conectado contratos/supervisores, pero no ese punto de entrada.

Además, la configuración de escenas conservaba las ocho de Saúl y excluía Bootstrap, que las pruebas de Foundation requieren. El script de reparación del alpha volvía a imponer esa lista de ocho.

## Comportamiento corregido

- `Play Gorilla Escape` prepara el entorno de desarrollo, registra las escenas y abre **Connection** antes de iniciar Play.
- Desde Connection, el teléfono y los sensores habilitan el acceso al menú de los seis juegos del alpha.
- `Open Bootstrap` conserva el diagnóstico de Fase 0. Su botón Jugar permanece deshabilitado porque esa vista es técnica; no es la entrada al producto de esta rama.
- `ProductScenes` define una sola lista: Connection primero, las demás escenas del alpha y Bootstrap al final. El reparador de escenas y el comando de inicio utilizan ese catálogo. Se conservan los GUID de todas las escenas.
- El inicio explícito desde el menú busca Java empaquetado, JAVA_HOME o PATH, y usa el JAR de desarrollo recién compilado en `Server/target`, salvo rutas explícitas del operador. Si faltan archivos, explica el problema sin arrancar procesos ni ejecutar instalaciones.
- Un paquete Vision local existente puede aportar la cámara; no se reconstruye ni se certifica automáticamente ese paquete anterior. El código conserva las limitaciones de cámara Windows documentadas en la integración principal.
- El opt-in de servicios físicos ahora requiere también permiso del test runner: un flag heredado no debe arrancar hardware durante pruebas interactivas.
- Se conservan las dependencias de SDK/toolchain Linux 1.1.0 registradas por Unity, además de glTFast, Shared y Test Framework 1.8.0.

## Verificaciones realizadas

- Harness C# sin motor: **19/19**, incluyendo entrada Connection, catálogo sin duplicados, inmutabilidad del catálogo y aislamiento de servicios en pruebas.
- Comprobación contra referencias reales de Unity 6000.6: Shared (6 fuentes), runtime (25), gameplay (14), herramientas Editor de Fase 0 (3) y herramientas Editor del alpha (3), sin errores de compilación. Advertencias conservadas en los logs locales. Se comprobaron también los caminos de preprocesador del Player para Shared/runtime/gameplay.
- Configuración guardada: nueve escenas, Connection primero, Bootstrap incluido, archivos y GUID existentes.
- Foundation, enlaces/documentación y comprobación de espacios: PASS.
- No hubo cambios en Java, PWA ni sus protocolos en esta corrección; sus pruebas del incremento anterior conservan su alcance histórico.

No se ejecutó Unity Editor ni se entró automáticamente en Play durante esta corrección; se mantiene la validación de motor pendiente por la decisión anterior del usuario. La comprobación C# usa las DLL de paquetes importadas, no prueba su ejecución nativa, la importación de assets ni la conexión física. El reporte del usuario de un menú visible no se presenta como aceptación de este código nuevo.

## Reproducción

```sh
dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj --configuration Release
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/Unity/Editor/Data --project-scripts
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/Unity/Editor/Data --project-scripts --player
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
```

`--project-scripts` requiere las DLL de paquetes importadas en `Unity/Library/ScriptAssemblies`. No arranca Unity. Para probar la interfaz, salir de Play si está activo, esperar a que Unity recompile y usar **Gorilla Escape → Play Gorilla Escape**. Debe abrirse Connection, no el menú técnico. Probar después la conexión real y cada juego antes de declarar aceptación completa.
