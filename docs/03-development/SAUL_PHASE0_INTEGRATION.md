# Jungle Party de Saúl: integración con Fase 0

Fecha local: 2026-10-26. Rama: `feature/saul-jungle-party-alpha`.

## Corrección posterior del inicio del producto

El usuario detectó que `Gorilla Escape → Play Gorilla Escape` todavía abría el menú Bootstrap de develop. Esto era una integración de navegación incompleta en el commit `a0fb822`, aunque el adaptador de transporte ya existía. [Corrección del punto de entrada](SAUL_PRODUCT_ENTRY.md): Connection inicia el producto; Bootstrap permanece como diagnóstico. Esta corrección incorpora el catálogo de nueve escenas y preparación del arranque desde el menú del Editor.

## Adaptación de mecánicas (2026-10-27)

La entrega separada de motion/pose descrita en el primer incremento fue sustituida por un frame fusionado tipado, autorización por juego y pausa técnica. [Estado vigente y matriz de los seis juegos](../02-gameplay/JUNGLE_FUSED_INPUT.md). Los resultados del primer incremento que aparecen más abajo son históricos y no sustituyen esta nueva verificación.

## Decisión y alcance

El usuario autorizó fusionar `develop` e incluir la migración de los juegos al transporte oficial, conservando lo mejor de ambas ramas. Base de Saúl: `43ef0b962705a03b02102d1a5e6462b3c9f6bcb3`; `develop` incorporado: `851e8dc3943129ec89210743c6cc0e494836d610`. No se integra esta rama hacia `develop` ni se modifica `main`.

La fusión textual no presentaba conflictos, pero el alpha había eliminado `_Project` y la dependencia Shared, usaba otro transporte y otra versión de Unity. Fusionar solamente la documentación habría dejado esa incompatibilidad intacta.

| Se conserva de Saúl | Se recupera/reutiliza de Fase 0 |
| --- | --- |
| Ocho escenas, modelos, materiales, recursos, mecánicas y sus GUID | `_Project`, Bootstrap, pruebas y contratos Shared por UPM local |
| Fachada de entrada `MotionSession` e interfaz `IPortableInputSource` | `PhoneInputStore`, `InputFusion` y `PlayerLock` |
| Apariencia de la pantalla de conexión | QR de Java por IPC autenticado, preparación del certificado y autorización del firewall |
| Unity 6000.6.0f1 y glTFast 6.19.0 | Supervisores `JavaProbeSupervisor` y `VisionSupervisor` sin reescribir su implementación |

No se degrada el proyecto de Saúl a Unity 6000.3.23f1. `validate_foundation.py` admite explícitamente ambas versiones como metadatos, sin certificar compatibilidad del motor. El Test Framework se conserva en 1.8.0, correspondiente al Editor 6000.6 instalado.

## Flujo implementado

```text
PWA oficial ── WebRTC ── Java supervisado
                            │
                    IPC autenticado / loopback
                            │
                      PhoneInputStore
                            │
Cámara ── VisionSupervisor ── InputFusion / PlayerLock
                            │
                   JungleInputAdapter
                            │
           PortableServiceHost → MotionSession → juegos
```

El adaptador usa únicamente el jugador 1, sin inferir identidades de otro jugador. Verifica muestras reales completas de gravedad, rotación y orientación; rechaza antigüedad superior a 250 ms, futuro superior a 20 ms, datos no finitos y secuencias repetidas. Mantiene m/s², grados y grados/s. La cámara requiere asociación, confianza y alineación vigentes; refleja X una sola vez para conservar la interacción de espejo del alpha. Una mano ausente conserva su indicador falso.

La PWA añade botones de confirmar/lanzar y recentrar. Usa el grupo `touch` existente en muestras reales: dos zonas virtuales (`x=0` y `x=1`, `y=0.5`) y `pressed`. Se conserva la política latest-only; los flancos se cuentan una vez por pulsación observada. Esto no implementa garantías de acción oficial de torneo. Las pulsaciones breves, los umbrales de las mecánicas y la equivalencia de ejes deben comprobarse físicamente.

`ConnectionScreen` ya no transmite la URL privada de admisión a `api.qrserver.com`. Dibuja módulos QR recibidos desde Java mediante el decodificador existente y permite alternar preparación/unión. No crea códigos aleatorios de sala. El botón de permiso solicita al mecanismo existente de Java la instalación de reglas; esta tarea no ejecutó elevación ni modificó el firewall de la PC.

Los launchers de diagnóstico no se autoarrancan sobre las escenas de juegos. La pantalla Bootstrap sólo lanza su menú en su propia escena. Si existe una sesión `QaRuntimeService`, el anfitrión de juegos rechaza arrancar otro propietario. El cierre espera las tareas y la limpieza de los supervisores antes de salir; la recuperación es manual, mantiene el límite global de tres lanzamientos Java y no crea una segunda política de reinicio.

`CameraTracker`, `MotionRelay` y el preparador PowerShell del alpha se conservan como referencia, pero la ruta migrada no los ejecuta. Tampoco fuerza un Quick Tunnel: el transporte utiliza la política AUTO/LAN de Fase 0.

## Reproducir las comprobaciones

Desde la raíz del repositorio:

```sh
npm --prefix PWA ci
npm --prefix PWA test
npm --prefix PWA run build
Server/mvnw -B -f Server/pom.xml verify
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
python3 -m unittest discover -s tools/connectivity -v
dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj --configuration Release
bash -n tools/setup-firewall.sh
sh -n tools/t062/run-qa.sh
sh -n tools/integration/run-jungle-editor.sh
```

El harness C# requiere .NET SDK 8 en desarrollo/CI; no añade un runtime al producto. Compila y ejecuta el adaptador real y las clases de contrato/asociación, sin reemplazarlos por simulaciones del algoritmo. Sus fixtures son sintéticos y no constituyen prueba física. CI Foundation ejecuta este harness.

Comprobación adicional de tipos contra referencias reales de un Editor instalado:

```sh
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/Unity/Editor/Data
```

Esta comprobación compila Shared, el runtime `_Project` y cinco archivos de la fachada/anfitrión/pantalla. No importa escenas, no compila glTFast ni el resto de gameplay, no genera Player ni sustituye los EditMode/PlayMode de Unity. Puede añadirse `--player` para comprobar también las rutas de preprocesador del Player; ambas variantes pasaron localmente. Los logs conservan las advertencias de obsolescencia y campos asignados por serialización.

## Inicio local

```sh
tools/integration/run-jungle-editor.sh /ruta/al/Editor/Unity
```

El script es para desarrolladores: utiliza JDK 21 y el JAR recién compilado, genera una credencial efímera sin imprimirla y configura las variables del mecanismo oficial. Abrir `Assets/Scenes/Connection.unity` y pulsar Play con licencia activa.

Para cámara, `GORILLA_PACKAGE_ROOT` debe apuntar a un paquete Fase 0 válido con `Vision/`, o usar `GORILLA_CAMERA_LAB=1` y las rutas explícitas `GORILLA_CAMERA_PYTHON`, `GORILLA_CAMERA_SCRIPT`, `GORILLA_CAMERA_MODEL`, `GORILLA_CAMERA_FIXTURE` y `GORILLA_CAMERA_MODE=physical`, según [CameraRuntimePaths](../../Unity/Assets/_Project/Scripts/CameraTracking/CameraRuntimePaths.cs). No se fabrican lecturas si falta ese paquete. Puede usarse `GORILLA_CAMERA_ENABLED=0` para un ensayo explícito sólo de teléfono; no demuestra los juegos dependientes de cámara.

El Player conserva las rutas relativas `Java/runtime/bin/java[.exe]` y `Java/server.jar`, y el paquete de cámara de Fase 0. El launcher/empaquetador oficial requiere revisión y reconstrucción del Player de esta rama antes de distribuirlo. No se declara un instalador único terminado ni soporte de cámara Windows completado.

## Resultado del primer incremento (histórico)

| Comprobación | Resultado local |
| --- | --- |
| Fusión de `develop` | Sin conflictos textuales; incompatibilidad arquitectónica detectada y adaptada |
| PWA | 71/71 pruebas; build Vite y shell offline correctos |
| Java | Maven verify: 112 pruebas, 108 ejecutadas sin fallos y 4 omitidas por requisitos de entorno |
| Adaptador C# | 15/15 escenarios de contratos, frescura, duplicados, flancos, espejo y asociación |
| Tipos C# con referencias Unity 6000.6 | Shared (6), runtime (24) y fachada (5 archivos): compilan en variantes Editor/Player; advertencias conservadas |
| Foundation | UPM local, GUID y contratos independientes del motor: PASS |
| Management | Backlog y enlaces locales: PASS |
| Firewall | 11/11 pruebas sin elevación |
| Dependencias web de producción | `npm audit --omit=dev --audit-level=high`: cero vulnerabilidades reportadas |
| Unity Editor completo / EditMode / PlayMode | PENDIENTE por decisión del usuario tras salida 198 sin licencia activa; no se ejecutaron tests de motor |
| Prueba física LAN, teléfono y cámara | NOT RUN para esta integración |
| Distribución Windows/Linux final | PENDIENTE de reconstrucción y aceptación; cámara Windows mantiene el pendiente de Fase 0 |

Los resultados se refieren a este incremento y no reutilizan capturas históricas como evidencia nueva. El ensayo anterior del prototipo Windows no valida el transporte migrado.

## Aceptación pendiente antes de integrar hacia develop

1. Activar la licencia e importar/compilar el proyecto completo con Unity 6000.6.0f1; resolver cualquier diferencia real de paquetes/API y ejecutar EditMode/PlayMode.
2. Construir un Player de esta rama con los runtimes oficiales y comprobar que el launcher abre Connection, no un ejecutable anterior.
3. Probar el QR de preparación y unión en LAN sin Internet; comprobar cancelación/aceptación del permiso de red.
4. Validar ejes, gravedad, cadencia, confirmar y recentrar en cada escena y teléfono real. Ajustar sólo con medidas verificadas.
5. Probar pérdida/reconexión del teléfono, pausa del navegador, cámara ausente, dos sujetos, cierre y reintento sin procesos huérfanos.
6. Medir latencia/rendimiento de los juegos y validar la distribución Windows con cámara oficial; revisar licencias de assets antes de publicación comercial.

La base de código está preparada para estas pruebas; la palabra «integrado» no equivale a producto perfecto ni a aceptación de Fase 0 cerrada.
