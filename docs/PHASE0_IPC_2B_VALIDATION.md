# Procedimiento de validación IPC 2B

Diseño [Accepted](PHASE0_IPC_2B_DESIGN.md). Linux de desarrollo, Unity 6000.3.23f1/Mono,
Java 21; no aceptación Windows ni hardware final. Las rutas siguientes son variables
locales de desarrollo, no distribución definitiva de JRE. Logs/runtime/builds quedan
bajo Unity/Logs o Unity/Builds ignorados; evidencia revisada/sanitizada bajo docs/evidence.

## Preparación

- Ejecutar `JAVA_HOME=/usr/lib/jvm/java-21-openjdk-amd64 Server/mvnw -B -f Server/pom.xml verify`.
- Java 21 del host tiene symlinks absolutos a /etc; dentro de Flatpak esos enlaces no
  bastan para Spring. Usar una copia de desarrollo con enlaces resueltos y accesible
  para ambos runtimes. Ejemplo Python estándar, sin dependencia del producto:

```python
import shutil
shutil.copytree('/usr/lib/jvm/java-21-openjdk-amd64',
                'Unity/Logs/Ipc2BValidation/jdk21',
                ignore_dangling_symlinks=True, dirs_exist_ok=True)
```

- Compilar `Server/src/test/helpers/LifecycleFixture.java` con javac21 en un directorio
  local ignorado; empaquetar con jar21 `--main-class LifecycleFixture` como
  `no-ready.jar` y copiar como `connect-refused.jar`. Son JVM deliberadamente
  incompletas de test; nunca certificar interoperabilidad real del servidor con ellas.
- Editor Flatpak: `flatpak run --command=<editor6000.3.23f1> com.unity.UnityHub`.
  Añadir `--env=UNITY_DISABLE_LINUX_TOOLCHAIN_MIGRATOR=1` y
  `--env=UNITY_DISABLE_LINUX_AUTO_TOOLCHAIN_INSTALLATION=1` para el entorno existente.
  Usar proyecto Unity de esta rama y `-batchmode -nographics -runTests` con
  `-testPlatform EditMode` o `PlayMode`, `-testResults <xml>` y `-logFile <log>`.
- Para PlayMode pasar antes del app-id Flatpak `--env=GORILLA_TEST_JAVA=<java21-resuelto>`,
  `--env=GORILLA_TEST_JAR=<JAR-real>` y `--env=GORILLA_TEST_FIXTURES=<carpeta-helpers>`.
  Se exige Java real; la falta de configuración no acredita PASS de integración.
- Build: `-batchmode -nographics -quit -executeMethod GorillaEscape.Editor.FoundationSetup.BuildDevelopmentLinux`.

## Player y operación manual

Configurar GORILLA_IPC_JAVA y GORILLA_IPC_JAR a rutas absolutas. Sin otro ajuste
se conserva smoke 2A N=100; GORILLA_IPC_SMOKE_EXIT=1 pide salida al completar cierre.
GORILLA_IPC_MODE=lifecycle mantiene RUNNING y ofrece Reintentar (R)/Detener (S) en diagnóstico.
Unity continúa supervisando en segundo plano durante esta prueba de desarrollo.
No cambiar escenas/settings. No secretos en argumentos. No flags de fault injection
productivos: fallos externos/harness de test, sólo sobre handles propios.

`tools/validate_ipc_2b_player.py` reproduce escenarios Player con libX11/libXtst
existentes y Python estándar, sin librerías de producto nuevas. Ejecutar con
`--player <build-linux> --java <java21> --jar <jar-real> --fixtures <helpers> --output <raw-ignorado>`.
El harness espera eventos reales con deadlines, acciona botones de recuperación
manual mediante sus atajos visibles y verifica ownership antes de señalizar el child (pidfd).
Comprueba salidas, recursos y procesos propios residuales después de cada escenario.
Cualquier fallo del harness o candidato se registra como fallo, nunca como éxito.

No ejecutar PlayMode y Player simultáneamente: comparten singleton por usuario/producto.
Para validar Flatpak↔host, mientras PlayMode posea el lock lanzar el JAR desde el host
con la misma ruta: debe salir 73 sin READY/Spring y no afectar el propietario.
Al finalizar, el archivo lock puede continuar existiendo y debe poder adquirirse.

## Evidencia y límites

Matriz y conteos exactos en TEST_REPORT. Conservar XML normalizados y JSON de estados,
deadlines/exit/cleanup sin tokens, dumps de entorno, stdout payloads ni logs crudos.
Inventario SHA256 de fuentes ejecutables/contratos permite verificar candidato final;
no inventar SHA de commit autorreferencial dentro de su propio inventario.
Métrica smoke IPC sólo referencia de desarrollo; comparar entorno de prueba con 2A
si varía materialmente, no aplicar gate rígido ni extrapolar a teléfono→gameplay.
No fuzz/flood, benchmark extensivo, restart/reconnect automático, watchdog avanzado,
Job Objects, móvil/PWA/sensores/cámara/gameplay/Input Fusion ni Fase 1.
EOF tras crash del padre funciona con JVM receptiva; una JVM congelada con padre
muerto y contención nativa multiplataforma permanecen fuera de 2B.
